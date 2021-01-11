#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.IO;

namespace KzBsv {

    public struct KzUtxoKey {

        public KzUInt256 TxId;
        public Int32 N;

        public override string ToString() {
            return $"{TxId} {N}";
        }

        public void Write(BinaryWriter s) {
            s.Write(N);
            s.Write(TxId.Span);
        }

        public void Read(BinaryReader s) {
            N = s.ReadInt32();
            TxId.Read(s);
        }

        public override int GetHashCode() => TxId.GetHashCode() ^ N;
        public override bool Equals(object obj) => obj is KzUtxoKey && this == (KzUtxoKey)obj;
        public bool Equals(KzUtxoKey o) => TxId == o.TxId && N == o.N;
        public static bool operator ==(KzUtxoKey x, KzUtxoKey y) => x.Equals(y);
        public static bool operator !=(KzUtxoKey x, KzUtxoKey y) => !(x == y);
    }
}
