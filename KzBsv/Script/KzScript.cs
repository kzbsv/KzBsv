#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KzBsv {

    [JsonConverter(typeof(JsonConverterKzScript))]
    public struct KzScript
    {
        ReadOnlySequence<byte> _script;

        public ReadOnlySequence<byte> Sequence => _script;

        public static KzScript None => new KzScript();

        static KzEncode _hex = KzEncoders.Hex;

        public long Length => _script.Length;

        public IKzWriter AddTo(IKzWriter writer, bool withoutCodeSeparators = false)
        {
            if (withoutCodeSeparators) {

                var ops = Decode().Where(o => o.Code != KzOpcode.OP_CODESEPARATOR).ToArray();
                writer.Add(ops.Length.AsVarIntBytes());
                foreach (var op in ops) writer.Add(op);

            } else {

                writer
                    .Add(_script.Length.AsVarIntBytes())
                    .Add(_script);

            }
            return writer;
        }

        public void Read(BinaryReader s)
        {
            var count = s.ReadInt32();
            if (count == -1)
                _script = ReadOnlySequence<byte>.Empty;
            else {
                var bytes = new byte[count];
                s.Read(bytes);
                _script = new ReadOnlySequence<byte>(bytes);
            }
        }

        public void Write(BinaryWriter s)
        {
            if (_script.IsEmpty)
                s.Write((Int32)(-1));
            else {
                s.Write((Int32)_script.Length);
                foreach (var m in _script)
                    s.Write(m.Span);
            }
        }

        public KzScript Slice(SequencePosition start, SequencePosition end) => new KzScript(_script.Slice(start, end));


        KzScript(ReadOnlySequence<byte> script) : this()
        {
            _script = script;
        }

        public KzScript(byte[] script) : this(new ReadOnlySequence<byte>(script)) { }

        public KzScript(string hex) : this(_hex.Decode(hex)) { }

        public bool IsPushOnly()
        {
            var ros = _script;
            var op = new KzOp();

            while (ros.Length > 0)
            {
                if (!op.TryReadOp(ref ros)) return false;
                // Note that IsPushOnly() *does* consider OP_RESERVED to be a push-type
                // opcode, however execution of OP_RESERVED fails, so it's not relevant
                // to P2SH/BIP62 as the scriptSig would fail prior to the P2SH special
                // validation code being executed.
                if (op.Code > KzOpcode.OP_16) return false;
            }
            return true;
        }

        public static (bool ok, KzScript script) ParseHex(string rawScriptHex, bool withoutLength = false)
        {
            var bytes = rawScriptHex.HexToBytes();
            var s = new KzScript();
            var ros = new ReadOnlySequence<byte>(bytes);
            var sr = new SequenceReader<byte>(ros);
            return (s.TryReadScript(ref sr, withoutLength), s);
        }

        public int FindAndDelete(KzValType vchSig)
        {
            int nFound = 0;
            var s = _script;
            var r = s;
            if (vchSig.Length == 0) return nFound;

            var op = new KzOp();
            var consumed = 0L;
            var offset = 0L;

            var o = vchSig.Sequence;
            var oLen = o.Length;

            do {
                offset += consumed;
                while (s.StartsWith(o)) {
                    r = r.RemoveSlice(offset, oLen);
                    s = s.Slice(oLen);
                    ++nFound;
                }
            } while (op.TryReadOp(ref s, out consumed));

            _script = r;
            return nFound;
#if false
            CScript result;
            iterator pc = begin(), pc2 = begin();
            opcodetype opcode;

            do {
                result.insert(result.end(), pc2, pc);
                while (static_cast<size_t>(end() - pc) >= b.size() &&
                       std::equal(b.begin(), b.end(), pc)) {
                    pc = pc + b.size();
                    ++nFound;
                }
                pc2 = pc;
            } while (GetOp(pc, opcode));

            if (nFound > 0) {
                result.insert(result.end(), pc2, end());
                *this = result;
            }
#endif
        }

        /// <summary>
        /// Decode script opcodes and push data.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KzOp> Decode()
        {
            var ros = _script;

            while (ros.Length > 0)
            {
                var op = new KzOp();
                if (!op.TryReadOp(ref ros)) goto fail;
                yield return op;
            }

        fail:
            ;
        }

        public bool TryParseScript(ref SequenceReader<byte> r, IKzBlockParser bp, bool withoutLength = false)
        {

            var length = r.Remaining;

            if (!withoutLength && !r.TryReadVarint(out length)) goto fail;

            bp.ScriptStart(this, r.Consumed);

            if (r.Remaining < length) goto fail;

            _script = r.Sequence.Slice(r.Position, length);
            r.Advance(length);

            bp.ScriptParsed(this, r.Consumed);

            return true;
        fail:
            return false;
        }

        public bool TryReadScript(ref SequenceReader<byte> r, bool withoutLength = false)
        {
            var length = r.Remaining;

            if (!withoutLength && !r.TryReadVarint(out length)) goto fail;

            if (r.Remaining < length) goto fail;

            _script = r.Sequence.Slice(r.Position, length);
            r.Advance(length);

            return true;
        fail:
            return false;
        }

        public string ToHexString()
        {
            return _hex.Encode(_script);
        }

        public string ToTemplateString()
        {
            var sb = new StringBuilder();
            foreach (var op in Decode()) {
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
        /// Return the Template String representation of script bytes.
        /// If the returned string does not include all the script opcodes, either because the scriptLen or limitLen
        /// arguments are greater than zero, or if the script sequence ends with an incomplete multibyte opcode,
        /// then "..." is appended following the last complete opcode.
        ///
        /// scriptLen argument should be used when the actual script is longer than the script sequence provided,
        /// which must then be a subsequence from the start of the script.
        /// If greater than zero it may be longer than the sequence provided in which case "..." will be appended
        /// after the last opcode.
        ///
        /// limitLen argument stops converting opcodes to their template string format after processing this many bytes.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="scriptLen">How long the entire script is, or zero.</param>
        /// <param name="limitLen">How many bytes to process, or zero.</param>
        /// <returns></returns>
        public static string ToTemplateString(byte[] script, long scriptLen = 0, long limitLen = 0) {
            if (script == null)
                return scriptLen > 0 ? "..." : "";
            return ToTemplateString(new ReadOnlySequence<byte>(script), scriptLen, limitLen);
        }

        /// <summary>
        /// Return the Template String representation of script bytes.
        /// If the returned string does not include all the script opcodes, either because the scriptLen or limitLen
        /// arguments are greater than zero, or if the script sequence ends with an incomplete multibyte opcode,
        /// then "..." is appended following the last complete opcode.
        ///
        /// scriptLen argument should be used when the actual script is longer than the script sequence provided,
        /// which must then be a subsequence from the start of the script.
        /// If greater than zero it may be longer than the sequence provided in which case "..." will be appended
        /// after the last opcode.
        ///
        /// limitLen argument stops converting opcodes to their template string format after processing this many bytes.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="scriptLen">How long the entire script is, or zero.</param>
        /// <param name="limitLen">How many bytes to process, or zero.</param>
        /// <returns></returns>
        public static string ToTemplateString(ReadOnlySequence<byte> script, long scriptLen = 0, long limitLen = 0) {
            var ros = script;
            if (limitLen == 0) limitLen = long.MaxValue;
            var ok = true;
            var count = 0L;
            var sb = new StringBuilder();

            while (ros.Length > 0 && ok && limitLen > count)
            {
                var op = new KzOp();
                ok = op.TryReadOp(ref ros, out var consumed);
                count += consumed;
                if (ok && limitLen >= count) {
                    var len = op.Data.Length;
                    if (len == 0)
                        sb.Append($"{op.CodeName} ");
                    else {
                        sb.Append($"[{op.Data.Length}] ");
                    }
                }
            }
            if (sb.Length > 0)
                sb.Length--;
            if (scriptLen == 0) scriptLen = count;
            if (!ok || limitLen < count || count < scriptLen) {
                sb.Append("...");
            }
            return sb.ToString();
        }

        public string ToVerboseString()
        {
            return string.Join(' ', Decode().Select(op => op.ToVerboseString()));
        }

        public override string ToString()
        {
            return string.Join(' ', Decode().Select(op => op.ToVerboseString()));
        }

        public static KzScript ParseCompact(string compactScript) => KzBScript.ParseCompact(compactScript).ToScript();

        public static KzScript ParseTestScript(string testScript) => KzBScript.ParseTestScript(testScript).ToScript();

        public override int GetHashCode() => _script.GetHashCode();
        public override bool Equals(object obj) => obj is KzScript && this == (KzScript)obj;
        public bool Equals(KzScript o) => Length == o.Length && _script.CompareTo(o._script) == 0;
        public static bool operator ==(KzScript x, KzScript y) => x.Equals(y);
        public static bool operator !=(KzScript x, KzScript y) => !(x == y);

        /// <summary>
        /// Template 1 P2PK
        /// [65] OP_CHECKSIG
        /// update TxOuts set TemplateId = 1 where ScriptPubLen = 67 and substring(ScriptPubBuf0, 1, 1) = 0x41 and substring(ScriptPubBuf0, 67, 1) = 0xAC
        /// </summary>
        /// <param name="len"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsP2PK(ReadOnlySpan<byte> script) {
            return script.Length == 67 && script[0] == 0x41 && script[66] == 0xAC;
        }

        /// <summary>
        /// Template 2 P2PKH
        /// 0x76A91412AB8DC588CA9D5787DDE7EB29569DA63C3A238C88AC 
        /// OP_DUP OP_HASH160 [20] OP_EQUALVERIFY OP_CHECKSIG
        /// update TxOuts set TemplateId = 2 where ScriptPubLen = 25 and substring(ScriptPubBuf0, 1, 3) = 0x76A914 and substring(ScriptPubBuf0, 24, 2) = 0x88AC
        /// </summary>
        /// <param name="len"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsP2PKH(ReadOnlySpan<byte> script) {
            return script.Length == 25 && script[0] == 0x76 && script[1] == 0xA9 && script[2] == 0x14 && script[23] == 0x88 && script[24] == 0xAC;
        }

        /// <summary>
        /// Template 3 OpRetPush4
        /// OP_0 OP_RETURN [4] ...
        /// update TxOuts set TemplateId = 3 where ScriptPubLen >= 7 and substring(ScriptPubBuf0, 1, 3) = 0x006A04
        /// </summary>
        /// <param name="len"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsOpRetPush4(ReadOnlySpan<byte> script) {
            return script.Length >= 7 && script[0] == 0x00 && script[1] == 0x6A && script[2] == 0x04;
        }

        static byte[] _OpRetBPrefix = new byte[] { 0x6a, 0x22, 0x31, 0x39, 0x48, 0x78, 0x69, 0x67, 0x56, 0x34, 0x51, 0x79, 0x42, 0x76, 0x33, 0x74, 0x48, 0x70, 0x51, 0x56, 0x63, 0x55, 0x45, 0x51, 0x79, 0x71, 0x31, 0x70, 0x7a, 0x5a, 0x56, 0x64, 0x6f, 0x41, 0x75, 0x74 };

        /// <summary>
        /// 0x6a2231394878696756345179427633744870515663554551797131707a5a56646f417574
        /// 0x006a2231394878696756345179427633744870515663554551797131707a5a56646f417574
        /// </summary>
        /// <param name="len"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsOpRetB(ReadOnlySpan<byte> script) {
            if (script.Length <= _OpRetBPrefix.Length + 1)
                return false;
            var o = script[0] == 0 ? 1 : 0;
            return script.Slice(o, _OpRetBPrefix.Length).SequenceEqual(_OpRetBPrefix);
        }

        static byte[] _OpRetBcatPrefix = new byte[] { 0x6a, 0x22, 0x31, 0x35, 0x44, 0x48, 0x46, 0x78, 0x57, 0x5a, 0x4a, 0x54, 0x35, 0x38, 0x66, 0x39, 0x6e, 0x68, 0x79, 0x47, 0x6e, 0x73, 0x52, 0x42, 0x71, 0x72, 0x67, 0x77, 0x4b, 0x34, 0x57, 0x36, 0x68, 0x34, 0x55, 0x70 };

        /// <summary>
        /// 0x6a22313544484678575a4a54353866396e6879476e735242717267774b34573668345570
        /// 0x006a22313544484678575a4a54353866396e6879476e735242717267774b34573668345570
        /// </summary>
        /// <param name="len"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsOpRetBcat(ReadOnlySpan<byte> script) {
            if (script.Length <= _OpRetBcatPrefix.Length + 1)
                return false;
            var o = script[0] == 0 ? 1 : 0;
            return script.Slice(o, _OpRetBcatPrefix.Length).SequenceEqual(_OpRetBcatPrefix);
        }

        static byte[] _OpRetBcatPartPrefix = new byte[] { 0x6a, 0x22, 0x31, 0x43, 0x68, 0x44, 0x48, 0x7a, 0x64, 0x64, 0x31, 0x48, 0x34, 0x77, 0x53, 0x6a, 0x67, 0x47, 0x4d, 0x48, 0x79, 0x6e, 0x64, 0x5a, 0x6d, 0x36, 0x71, 0x78, 0x45, 0x44, 0x47, 0x6a, 0x71, 0x70, 0x4a, 0x4c };

        /// <summary>
        /// 0x6a2231436844487a646431483477536a67474d48796e645a6d3671784544476a71704a4c
        /// 0x006a2231436844487a646431483477536a67474d48796e645a6d3671784544476a71704a4c
        /// </summary>
        /// <param name="len"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsOpRetBcatPart(ReadOnlySpan<byte> script) {
            if (script.Length <= _OpRetBcatPartPrefix.Length + 1)
                return false;
            var o = script[0] == 0 ? 1 : 0;
            return script.Slice(o, _OpRetBcatPartPrefix.Length).SequenceEqual(_OpRetBcatPartPrefix);
        }

        /// <summary>
        /// Returns true if the script is an unspendable OP_RETURN.
        /// Prior to the Genesys upgrade (block 620538), an OP_RETURN script could never eveluate to true.
        /// After Genesys, the value at the top of the stack when executing an OP_RETURN determines the script result.
        /// Therefore, after Genesys, a value of zero is pushed before the OP_RETURN (which may be followed by arbitrary push datas)
        /// to create an unspendable output.
        /// Unspendable outputs can be safely pruned by transaction processors.
        /// Unspendable outputs can always be retrieved for a price from archive services.
        /// </summary>
        /// <param name="len">The full transaction output script length in bytes. </param>
        /// <param name="script">The initial buffer of the transaction output script. Typically up to 256 bytes or so of the script.</param>
        /// <param name="height">The block height of the transaction containing the output script.</param>
        /// <returns></returns>
        public static bool IsOpReturn(ReadOnlySpan<byte> script, int? height = null) {
            var result = false;
            if (script.Length > 0 && script[0] == 0x6a) {
                if (height <= 620538) {
                    result = true;
                }
            } else if (script.Length > 1 && script[1] == 0x6a && script[0] == 0) {
                result = true;
            }
            return result;
        }

        public static (bool unspendable, KzScriptTemplateId templateId) ParseKnownScriptPubTemplates(ReadOnlySpan<byte> scriptPubBuf0, int? height) {

            // Check for OP_RETURN outputs, these are unspendable and are flagged with a -1 SpentByTxId value.
            // After Genesis, bare OP_RETURN is spendable (anything that pushes true on sig script can spend.
            var unspendable = IsOpReturn(scriptPubBuf0, height);
            
            KzScriptTemplateId templateId;

            if (unspendable) {
                templateId
                    = IsOpRetPush4(scriptPubBuf0) ? KzScriptTemplateId.OpRetPush4
                    : IsOpRetB(scriptPubBuf0) ? KzScriptTemplateId.OpRetB
                    : IsOpRetBcat(scriptPubBuf0) ? KzScriptTemplateId.OpRetBcat
                    : IsOpRetBcatPart(scriptPubBuf0) ? KzScriptTemplateId.OpRetBcatPart
                    : KzScriptTemplateId.OpRet;
            } else {
                // Spendable
                templateId
                    = IsP2PK(scriptPubBuf0) ? KzScriptTemplateId.P2PK
                    : IsP2PKH(scriptPubBuf0) ? KzScriptTemplateId.P2PKH
                    : KzScriptTemplateId.Unknown;
            }

            return (unspendable, templateId);
        }

        /// <summary>
        /// Without height, returns true only for OP_0 OP_RETURN pattern.
        /// With height, pre 620538 blocks also treat just a bare OP_RETURN to be unspendable.
        /// </summary>
        /// <returns></returns>
        public bool IsOpReturn(int? height = null) => IsOpReturn(_script.FirstSpan, height);

        public static (bool ok, KzSigHash sh, byte[] r, byte[] s, KzPubKey pk) IsCheckSigScript(byte[] scriptSigBytes) => IsCheckSigScript(new ReadOnlySequence<byte>(scriptSigBytes));
            
        public static (bool ok, KzSigHash sh, byte[] r, byte[] s, KzPubKey pk) IsCheckSigScript(ReadOnlySequence<byte> scriptSigBytes) {
            var ros = scriptSigBytes;
            var (ok1, op1) = KzOp.TryRead(ref ros, out var consumed1);
            var (ok2, op2) = KzOp.TryRead(ref ros, out var consumed2);
            if (!ok1 || !ok2 || consumed1 + consumed2 != scriptSigBytes.Length) goto fail;
            if (op2.Data.Length < KzPubKey.MinLength || op2.Data.Length > KzPubKey.MaxLength) goto fail;

            var pubkey = new KzPubKey(op2.Data.ToBytes());
            if (!pubkey.IsValid) goto fail;

            var sig = op1.Data.ToSpan();
            if (sig.Length < 7 || sig[0] != 0x30 || sig[2] != 0x02) goto fail;
            var lenDER = sig[1];
            var lenR = sig[3];
            if (sig.Length != lenDER + 3 || sig.Length - 1 < lenR + 5 || sig[4 + lenR] != 0x02) goto fail;
            var lenS = sig[lenR + 5];
            if (sig.Length != lenR + lenS + 7) goto fail;

            var sh = (KzSigHash)(sig[sig.Length - 1]);

            var r = sig.Slice(4, lenR).ToArray();
            var s = sig.Slice(6 + lenR, lenS).ToArray();

            return (true, sh, r, s, pubkey);

        fail:
            return (false, KzSigHash.UNSUPPORTED, null, null, null);
        }

    }

    class JsonConverterKzScript : JsonConverter<KzScript>
    {
        public override KzScript ReadJson(JsonReader reader, Type objectType, KzScript existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var s = (string)reader.Value;
            return new KzScript(s);
        }

        public override void WriteJson(JsonWriter writer, KzScript value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToHexString());
        }
    }
}
