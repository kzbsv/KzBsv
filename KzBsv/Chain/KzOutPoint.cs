#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    /// <summary>
    /// Closely mirrors the data and layout of a Bitcoin transaction input's previous output reference as stored in each block.
    /// Focus is on performance when processing large numbers of transactions, including blocks of transactions.
    /// </summary>
    public struct KzOutPoint
    {
        KzUInt256 _HashTx;
        Int32 _N;

        public KzUInt256 HashTx => _HashTx;
        public Int32 N => _N;

        public KzOutPoint(KzUInt256 hashTx, Int32 n) { _HashTx = hashTx; _N = n; }

        public bool TryReadOutPoint(ref SequenceReader<byte> r)
        {
            if (!r.TryCopyToA(ref _HashTx)) goto fail;
            if (!r.TryReadLittleEndian(out _N)) goto fail;

            return true;
        fail:
            return false;
        }

        public IKzWriter AddTo(IKzWriter w)
        {
            w.Add(_HashTx).Add(_N);
            return w;
        }
    }
}
