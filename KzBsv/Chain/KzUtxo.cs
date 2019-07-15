#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Collections.Generic;
using System.IO;

namespace KzBsv
{
    public class KzUtxo
    {
        UInt32 _heightAndIsCoinbase;

        public KzTxOut Out;

        public bool IsCoinbase { get { return (_heightAndIsCoinbase & 1) == 1; } set { _heightAndIsCoinbase = (uint)(Height << 1) + (value ? 1u : 0u); } }
        public int Height { get { return (int)(_heightAndIsCoinbase >> 1); } set { _heightAndIsCoinbase = (uint)(value << 1) + (IsCoinbase ? 1u : 0u); } }

        public void Write(BinaryWriter s)
        {
            s.Write(_heightAndIsCoinbase);
            Out.Write(s);
        }

        public void Read(BinaryReader s)
        {
            _heightAndIsCoinbase = s.ReadUInt32();
            Out.Read(s);
        }
    }
    
    public struct KzUtxoKey
    {
        public KzUInt256 TxId;
        public uint N;

        public override string ToString()
        {
            return $"{TxId} {N}";
        }

        public void Write(BinaryWriter s)
        {
            s.Write(N);
            s.Write(TxId.Span);
        }

        public void Read(BinaryReader s)
        {
            N = s.ReadUInt32();
            TxId.Read(s);
        }
    }

    public class KzUtxoKeyComparer : IEqualityComparer<KzUtxoKey>
    {
        public bool Equals(KzUtxoKey x, KzUtxoKey y)
        {
            return x.TxId == y.TxId && x.N == y.N;
        }

        public int GetHashCode(KzUtxoKey x)
        {
            return x.TxId.GetHashCode() ^ x.N.GetHashCode();
        }
    }
}
