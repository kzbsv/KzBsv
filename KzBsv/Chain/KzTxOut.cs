#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.IO;

namespace KzBsv
{
    public struct KzTxOut
    {
        Int64 _value;
        KzScript _scriptPubKey;

        public Int64 Value => _value;
        public KzScript ScriptPubKey => _scriptPubKey;

        public static KzTxOut Null => new KzTxOut() { _value = -1 };

        public KzTxOut(Int64 value, KzScript scriptPubKey) { _value = value; _scriptPubKey = scriptPubKey; }

        public bool IsNull => _value == -1;

        public bool TryReadTxOut(ref SequenceReader<byte> r)
        {
            if (!r.TryReadLittleEndian(out _value)) goto fail;
            if (!_scriptPubKey.TryReadScript(ref r)) goto fail;

            return true;
        fail:
            return false;
        }

        public void Write(BinaryWriter s)
        {
            s.Write(_value);
            _scriptPubKey.Write(s);
        }

        public void Read(BinaryReader s)
        {
            _value = s.ReadInt64();
            _scriptPubKey.Read(s);
        }

        public override string ToString()
        {
            return $"{new KzAmount(_value)} {_scriptPubKey}";
        }

        public IKzWriter AddTo(IKzWriter writer)
        {
            writer
                .Add(_value)
                .Add(_scriptPubKey)
                ;
            return writer;
        }
    }
}
