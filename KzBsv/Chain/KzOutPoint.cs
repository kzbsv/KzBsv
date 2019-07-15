#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    public struct KzOutPoint
    {
        KzUInt256 _txid;
        Int32 _n;

        public KzUInt256 TxId => _txid;
        public Int32 N => _n;

        public KzOutPoint(KzUInt256 txid, Int32 n) { _txid = txid; _n = n; }

        public bool TryReadOutPoint(ref SequenceReader<byte> r)
        {
            if (!r.TryCopyToA(ref _txid)) goto fail;
            if (!r.TryReadLittleEndian(out _n)) goto fail;

            return true;
        fail:
            return false;
        }

        public IKzWriter AddTo(IKzWriter w)
        {
            w.Add(_txid).Add(_n);
            return w;
        }
    }
}
