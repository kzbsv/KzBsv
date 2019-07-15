#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Linq;
using System.Security.Cryptography;

namespace KzBsv
{

    public class KzTransaction
    {

        /// Essential fields of a Bitcoin SV transaction.
        
        Int32 _version;
        KzTxIn[] _vin = new KzTxIn[0];
        KzTxOut[] _vout = new KzTxOut[0];
        UInt32 _lockTime;

        /// The following fields are computed or external, not essential.

        KzUInt256 _txId;
        Int64 _valueIn;
        Int64 _valueOut;

        /// Public access to essential header fields.

        public Int32 Version => _version;
        public UInt32 LockTime => _lockTime;

        public KzTxIn[] Vin => _vin;
        public KzTxOut[] Vout => _vout;

        /// Public access to computed or external, not essential.

        public KzUInt256 TxId => _txId;
        public Int64 ValueIn { get => _valueIn; set => _valueIn = value; }
        public Int64 ValueOut { get => _valueOut; set => _valueOut = value; }

        public KzTransaction() { }

        public KzTransaction(Int32 version, KzTxIn[] vin, KzTxOut[] vout, UInt32 lockTime)
        {
            _version = version;
            _vin = vin;
            _vout = vout;
            _lockTime = lockTime;
        }

        public KzTransaction(KzBTransaction tb)
        {
            _version = tb.Version;
            _vin = tb.Vin.Select(i => i.ToTxIn()).ToArray();
            _vout = tb.Vout.Select(o => o.ToTxOut()).ToArray();
            _lockTime = tb.LockTime;
        }

        public bool TryReadTransaction(ref ReadOnlySequence<byte> ros)
        {
            var r = new SequenceReader<byte>(ros);
            if (!TryReadTransaction(ref r)) goto fail;

            ros = ros.Slice(r.Consumed);

            return true;
        fail:
            return false;
        }

        public bool TryReadTransaction(ref SequenceReader<byte> r)
        {
            var start = r.Position;

            if (!r.TryReadLittleEndian(out _version)) goto fail;
            if (!r.TryReadVarint(out long countIn)) goto fail;

            _vin = new KzTxIn[countIn];
            for (var i = 0L; i < countIn; i++)
            {
                ref var txin = ref _vin[i];
                if (!txin.TryReadTxIn(ref r)) goto fail;
            }

            if (!r.TryReadVarint(out long countOut)) goto fail;

            _vout = new KzTxOut[countOut];
            for (var i = 0L; i < countOut; i++)
            {
                ref var txout = ref _vout[i];
                if (!txout.TryReadTxOut(ref r)) goto fail;
            }

            if (!r.TryReadLittleEndian(out _lockTime)) goto fail;

            var end = r.Position;

            // Compute the transaction hash.
            var txBytes = r.Sequence.Slice(start, end).ToArray();
            using (var sha256 = SHA256.Create())
            {
                var hash1 = sha256.ComputeHash(txBytes);
                var hash2 = sha256.ComputeHash(hash1);
                hash2.CopyTo(_txId.Span);
            }

            return true;

        fail:
            return false;
        }

        public static KzTransaction ParseHex(string rawTxHex)
        {
            var bytes = rawTxHex.HexToBytes();
            var tx = new KzTransaction();
            var ros = new ReadOnlySequence<byte>(bytes);
            if (!tx.TryReadTransaction(ref ros)) tx = null;
            return tx;
        }

        public override string ToString()
        {
            return TxId.ToString();
        }

        public IKzWriter AddTo(IKzWriter writer)
        {
            writer
                .Add(_version)
                .Add(_vin.Length.AsVarIntBytes())
                ;
            foreach (var txIn in _vin)
                writer
                    .Add(txIn)
                    ;
            writer
                .Add(_vout.Length.AsVarIntBytes())
                ;
            foreach (var txOut in _vout)
                writer
                    .Add(txOut)
                    ;
            writer
                .Add(_lockTime)
                ;
            return writer;
        }

        public byte[] ToBytes()
        {
            var wl = new KzWriterLength();
            wl.Add(this);
            var length = wl.Length;
            var bytes = new byte[length];
            var wm = new KzWriterMemory(new Memory<byte>(bytes));
            wm.Add(this);
            return bytes;
        }
    }
}
