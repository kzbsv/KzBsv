#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;

namespace KzBsv
{
    public abstract class KzExtKey
    {
        public const uint HardenedBit = 0x80000000;
        public const int BIP32_EXTKEY_SIZE = 74;

        protected byte _depth;
        protected Int32 _fingerprint;
        protected uint _child;
        protected KzUInt256 _chaincode;

        public byte Depth => _depth;

        /// <summary>
        /// First four bytes of the corresponding public key's HASH160 which is also called its key ID.
        /// </summary>
        public int Fingerprint => _fingerprint;

        public bool Hardened => _child >= HardenedBit;

        /// <summary>
        /// Always excludes the HardenedBit.
        /// </summary>
        public int Index => (int)(_child & ~HardenedBit);
        
        public uint IndexWithHardened => _child;

        public KzUInt256 Chaincode => _chaincode;

        public abstract void Encode(Span<byte> code);

        public abstract KzExtKey DeriveBase(int index, bool hardened);

        /// <summary>
        /// Computes the key specified by a key path.
        /// At each derivation, there's a small chance the index specified will fail.
        /// If any generation fails, null is returned.
        /// </summary>
        /// <param name="kp"></param>
        /// <returns>null on derivation failure. Otherwise the derived private key.</returns>
        public KzExtKey DeriveBase(KzKeyPath kp)
        {
            var k = this;
            foreach (var i in kp.Indices) {
                k = k.DeriveBase((int)(i & ~HardenedBit), (i & HardenedBit) != 0);
                if (k == null) break;
            }
            return k;
        }

        public override int GetHashCode() => _depth.GetHashCode() ^ _fingerprint.GetHashCode() ^ _child.GetHashCode() ^ _chaincode.GetHashCode();
        public bool Equals(KzExtKey o) => (object)o != null && _depth == o._depth && _fingerprint == o._fingerprint && _child == o._child && _chaincode == o._chaincode;
        public override bool Equals(object obj) => obj is KzExtKey && this == (KzExtKey)obj;
        public static bool operator ==(KzExtKey x, KzExtKey y) => object.ReferenceEquals(x, y) || (object)x == null && (object)y == null || x.Equals(y);
        public static bool operator !=(KzExtKey x, KzExtKey y) => !(x == y);
    }
}
