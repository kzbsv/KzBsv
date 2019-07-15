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
