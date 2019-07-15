#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KzBsv
{

    public class KzBScript
    {
        /// <summary>
        /// true if no more additions or removals from the operations will occur,
        /// but note that individual operations may still NOT be final.
        /// false by default.
        /// </summary>
        protected bool _isFinal;

        /// <summary>
        /// true if script is associated with a scriptPub.
        /// false if script is associated with a scriptSig.
        /// null if script purpose is unknown.
        /// </summary>
        protected bool? _isPub;

        /// <summary>
        /// If the script implements a known template, this will be the template type.
        /// Otherwise it will be Unkown.
        /// </summary>
        protected KzBScriptType _type;

        /// <summary>
        /// The sequence of operations where each operation is an opcode and optional data.
        /// To support testing and unimplemented features, an operation's IsRaw flag can be set in
        /// which case the opcode is ignored and the data is treated as unparsed script code.
        /// </summary>
        protected List<KzBOp> _ops = new List<KzBOp>();

        public List<KzBOp> Ops => _ops;

        /// <summary>
        /// true when no more additions, deletions or changes to existing operations will occur.
        /// </summary>
        public bool IsFinal => _isFinal && _ops.All(op => op.IsFinal);
        public bool IsPub { get => _isPub == true; set => _isPub = value ? (bool?)true : null; }
        public bool IsSig { get => _isPub == false; set => _isPub = value ? (bool?)false : null; }
        public KzBScriptType Type => _type;
        public long Length => _ops.Sum(o => o.Length);

        public static KzBScriptPubP2PKH NewPubP2PKH(KzUInt160 pubKeyHash) => new KzBScriptPubP2PKH(pubKeyHash);
        public static KzBScriptSigP2PKH NewSigP2PKH(KzPubKey pubKey) => new KzBScriptSigP2PKH(pubKey);
        public static (KzBScriptPubP2PKH pub, KzBScriptSigP2PKH sig) NewP2PKH(KzPubKey pubKey)
            => (new KzBScriptPubP2PKH(pubKey.ToHash160()), new KzBScriptSigP2PKH(pubKey));

        public KzBScript() { }

        public KzBScript Clear() { _ops.Clear(); return this; }

        public KzBScript Set(KzScript script) { _ops.Clear(); return Add(script); }

        public KzBScript Add(KzOpcode opc) { _ops.Add(new KzOp(opc)); return this; }
        public KzBScript Add(KzOpcode opc, KzValType v) { _ops.Add(new KzOp(opc, v)); return this; }
        public KzBScript Add(KzBOp bop) { _ops.Add(bop); return this; }
        public KzBScript Add(KzScript script) { _ops.AddRange(script.ToBOps()); return this; }
        public KzBScript Add(string hex) { return Add(hex.ToKzScript()); }
        public KzBScript Add(byte[] raw) { _ops.Add(new KzBOp(new KzValType(raw))); return this; }

        /// <summary>
        /// Push a zero as a non-final placeholder.
        /// </summary>
        /// <returns></returns>
        public KzBScript Push() => Add(new KzBOp { IsFinal = false, IsRaw = false, Op = new KzOp(KzOpcode.OP_0) });
        public KzBScript Push(ReadOnlySpan<byte> data) { _ops.Add(KzOp.Push(data)); return this; }
        public KzBScript Push(long v) { _ops.Add(KzOp.Push(v)); return this; }

        public KzScript ToScript() => new KzScript(ToBytes());

        public byte[] ToBytes()
        {
            var bytes = new byte[Length];
            var span = bytes.AsSpan();
            foreach (var op in _ops) {
                op.TryCopyTo(ref span);
            }
            return bytes;
        }

        public string ToHex() => ToBytes().ToHex();

        public override string ToString()
        {
            return string.Join(' ', _ops.Select(o => o.ToVerboseString()));
        }

        public string ToTemplateString()
        {
            var sb = new StringBuilder();
            foreach (var bop in _ops) {
                var op = bop.Op;
                var len = op.Data.Length;
                if (len == 0)
                    sb.Append($"{op.CodeName} ");
                else {
                    sb.Append($"[{op.Data.Length}] ");
                }
            }
            if (sb.Length > 0)
                sb.Length--;
            return sb.ToString();
        }

        /// <summary>
        /// Converts hex and ascii strings to a specific byte count, if len has a value and disagrees it is an error.
        /// Converts integer values to little endian bytes where the most significant bit is set if negative.
        /// For integer values, if len has a value, the result is expanded if necessary. If len is too small it is an error.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        static byte[] ParseCompactValueToBytes(string s, uint? len = null) => ParseLiteralValueToBytes(s, len).bytes;

        /// <summary>
        /// Parses signed decimals, hexadecimal strings prefixed with 0x, and ascii strings enclosed in single quotes.
        /// Each format is converted to a byte array.
        /// Converts hex and ascii strings to a specific byte count, if len has a value and disagrees it is an error.
        /// Converts integer values to little endian bytes where the most significant bit is set if negative.
        /// For integer values, if len has a value, the result is expanded if necessary. If len is too small it is an error.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="len"></param>
        /// <returns>Tuple of the parsed byte[] data and a boolean true if the literal was specified in hexadecimal.
        /// Returns null for bytes if can't be parsed as a literal.</returns>
        static (byte[] bytes, bool isHex) ParseLiteralValueToBytes(string s, uint? len = null)
        {
            var bytes = (byte[])null;
            var isHex = false;

            if (s.StartsWith("'") && s.EndsWith("'")) {
                s = s.Substring(1, s.Length - 2);
                if (s.Contains("'"))
                    throw new InvalidOperationException();
                bytes = Encoding.ASCII.GetBytes(s);
            } else if (s.StartsWith("0x")) {
                isHex = true;
                bytes = Kz.Hex.Decode(s.Substring(2));
            } else if (long.TryParse(s, out long v)) {
                bytes = KzScriptNum.Serialize(v);
            }
            if (len.HasValue && bytes != null && len.Value != bytes.Length)
                throw new InvalidOperationException();
            return (bytes, isHex);
        }

        /// <summary>
        /// Parses format used by script_tests.json file shared with C++ bitcoin-sv codebase.
        /// Primary difference is that hex literals are never treated as push data.
        /// Hex literals are also treated as unparsed bytes. e.g. multiple opcodes in a single literal.
        /// The use of "OP" before a literal is not used to create opcodes from literals.
        /// Instead, single byte hex literals are interpreted as opcodes directly.
        /// Test scripts also wish to encode invalid scripts to make sure the interpreter will
        /// catch the errors.
        /// </summary>
        /// <param name="testScript"></param>
        /// <returns></returns>
        public static KzBScript ParseTestScript(string testScript)
        {
            var sb = new KzBScript();
            var ps = testScript.Split(' ', StringSplitOptions.RemoveEmptyEntries).AsSpan();
            while (ps.Length > 0) {
                var arg = 0;
                var (bytes, isHex) = ParseLiteralValueToBytes(ps[arg]);
                if (bytes != null) {
                    if (isHex)
                        // Hex literals are treated as raw, unparsed bytes added to the script.
                        sb.Add(bytes);
                    else
                        sb.Push(bytes);
                } else {
                    var data = (byte[])null;
                    if (!Enum.TryParse<KzOpcode>("OP_" + ps[arg], out KzOpcode opcode))
                        throw new InvalidOperationException();
                    if (opcode > KzOpcode.OP_0 && opcode < KzOpcode.OP_PUSHDATA1) {
                        // add next single byte value to op.
                        arg++;
                        data = ParseCompactValueToBytes(ps[arg]);
                        if (data == null) {
                            // Put this arg back. Treat missing data as zero length.
                            data = new byte[0];
                            arg--;
                        }
                    } else if (opcode >= KzOpcode.OP_PUSHDATA1 && opcode <= KzOpcode.OP_PUSHDATA4) {
                        // add next one, two, or four byte value as length of following data value to op.
                        arg++;
                        var lengthBytes = ParseCompactValueToBytes(ps[arg]);
                        var len = 0u;
                        if (!BitConverter.IsLittleEndian)
                            throw new NotSupportedException();
                        if (opcode == KzOpcode.OP_PUSHDATA1) {
                            // add next one byte value as length of following data value to op.
                            if (lengthBytes.Length != 1)
                                throw new InvalidOperationException();
                            len = lengthBytes[0];
                        } else if (opcode == KzOpcode.OP_PUSHDATA2) {
                            // add next two byte value as length of following data value to op.
                            if (lengthBytes.Length != 2)
                                throw new InvalidOperationException();
                            len = BitConverter.ToUInt16(lengthBytes);
                        } else if (opcode == KzOpcode.OP_PUSHDATA4) {
                            // add next four byte value as length of following data value to op.
                            if (lengthBytes.Length != 4)
                                throw new InvalidOperationException();
                            len = BitConverter.ToUInt32(lengthBytes);
                        }
                        if (len > 0) {
                            arg++;
                            data = arg < ps.Length ? ParseCompactValueToBytes(ps[arg], len) : new byte[0];
                        }
                    }
                    if (data == null)
                        sb.Add(opcode);
                    else
                        sb.Add(opcode, new KzValType(data));
                }
                ps = ps.Slice(Math.Min(arg + 1, ps.Length));
            }
            return sb;
        }

                    //if (!isOp && ps[arg] == "OP") {
                    //    arg++;
                    //    var opcodeBytes = ParseCompactValueToBytes(ps[arg]);
                    //    if (opcodeBytes == null || opcodeBytes.Length > 1)
                    //        throw new InvalidOperationException();
                    //    op = (KzOpcode)opcodeBytes[0];
                    //}
        public static KzBScript ParseCompact(string compactScript)
        {
            var sb = new KzBScript();
            var ps = compactScript.Split(' ', StringSplitOptions.RemoveEmptyEntries).AsSpan();
            while (ps.Length > 0) {
                var s = ps[0];
                var bytes = ParseCompactValueToBytes(s);
                if (bytes != null) {
                    sb.Push(bytes);
                    ps = ps.Slice(1);
                } else if (Enum.TryParse<KzOpcode>("OP_" + s, out KzOpcode op)) {
                    var args = 1;
                    var data = (byte[])null;
                    if (op > KzOpcode.OP_0 && op < KzOpcode.OP_PUSHDATA1) {
                        // add next single byte value to op.
                        args = 2;
                        data = ParseCompactValueToBytes(ps[1]);
                        if (data.Length >= (int)KzOpcode.OP_PUSHDATA1)
                            throw new InvalidOperationException();
                    } else if (op >= KzOpcode.OP_PUSHDATA1 && op <= KzOpcode.OP_PUSHDATA4) {
                        // add next one, two, or four byte value as length of following data value to op.
                        args = 2;
                        var lengthBytes = ParseCompactValueToBytes(ps[1]);
                        var len = 0u;
                        if (!BitConverter.IsLittleEndian)
                            throw new NotSupportedException();
                        if (op == KzOpcode.OP_PUSHDATA1) {
                            // add next one byte value as length of following data value to op.
                            if (lengthBytes.Length != 1)
                                throw new InvalidOperationException();
                            len = lengthBytes[0];
                        } else if (op == KzOpcode.OP_PUSHDATA2) {
                            // add next two byte value as length of following data value to op.
                            if (lengthBytes.Length != 2)
                                throw new InvalidOperationException();
                            len = BitConverter.ToUInt16(lengthBytes);
                        } else if (op == KzOpcode.OP_PUSHDATA4) {
                            // add next four byte value as length of following data value to op.
                            if (lengthBytes.Length != 4)
                                throw new InvalidOperationException();
                            len = BitConverter.ToUInt32(lengthBytes);
                        }
                        if (len > 0) {
                            args = 3;
                            data = ParseCompactValueToBytes(ps[2], len);
                        }
                    }
                    if (data == null)
                        sb.Add(op);
                    else
                        sb.Add(op, new KzValType(data));
                    ps = ps.Slice(args);
                } else
                    throw new InvalidOperationException();
            }
            return sb;
        }

        public static implicit operator KzScript(KzBScript sb) => sb.ToScript();
    }
}
