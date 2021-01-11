#region Copyright
// Copyright (c) 2020 TonesNotes
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
        KzScript _scriptPub;

        public Int64 Value => _value;
        public KzScript ScriptPub => _scriptPub;

        public static KzTxOut Null => new KzTxOut() { _value = -1 };

        public KzTxOut(Int64 value, KzScript scriptPub) { _value = value; _scriptPub = scriptPub; }

        public bool IsNull => _value == -1;

        public bool TryParseTxOut(ref SequenceReader<byte> r, IKzBlockParser bp)
        {
            if (!r.TryReadLittleEndian(out _value)) goto fail;

            bp.TxOutStart(this, r.Consumed);

            if (!_scriptPub.TryParseScript(ref r, bp)) goto fail;

            bp.TxOutParsed(this, r.Consumed);

            return true;
        fail:
            return false;
        }

        public bool TryReadTxOut(ref SequenceReader<byte> r)
        {
            if (!r.TryReadLittleEndian(out _value)) goto fail;
            if (!_scriptPub.TryReadScript(ref r)) goto fail;

            return true;
        fail:
            return false;
        }

        public void Write(BinaryWriter s)
        {
            s.Write(_value);
            _scriptPub.Write(s);
        }

        public void Read(BinaryReader s)
        {
            _value = s.ReadInt64();
            _scriptPub.Read(s);
        }

        public override string ToString()
        {
            return $"{new KzAmount(_value)} {_scriptPub}";
        }

        public IKzWriter AddTo(IKzWriter writer)
        {
            writer
                .Add(_value)
                .Add(_scriptPub)
                ;
            return writer;
        }
    }
}
