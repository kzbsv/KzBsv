#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Diagnostics;

namespace KzBsv
{

    public class KzExtPubKey : KzExtKey
    {
        KzPubKey pubkey;

        public KzPubKey PubKey => pubkey;

        public KzExtPubKey() { }

        public KzExtPubKey(ReadOnlySpan<byte> code) {
            Decode(code);
        }

        public static KzExtPubKey FromPriv(KzExtPrivKey priv)
        {
            var key = new KzExtPubKey {
                _depth = priv.Depth,
                _fingerprint = priv.Fingerprint,
                _child = priv.IndexWithHardened,
                _chaincode = priv.Chaincode,
                pubkey = priv.PrivKey.GetPubKey()
            };
            return key;
        }

        /// <summary>
        /// Computes the public key specified by a key path.
        /// At each derivation, there's a small chance the index specified will fail.
        /// If any generation fails, null is returned.
        /// </summary>
        /// <param name="kp"></param>
        /// <returns>null on derivation failure. Otherwise the derived private key.</returns>
        public KzExtPubKey Derive(KzKeyPath kp) => DeriveBase(kp) as KzExtPubKey;
        public KzExtPubKey Derive(int index) => DeriveBase(index, false) as KzExtPubKey;

        public override KzExtKey DeriveBase(int index, bool hardened)
        {
            var cek = new KzExtPubKey {
                _depth = (byte)(_depth + 1),
                _child = (uint)index | (hardened ? HardenedBit : 0)
            };
            pubkey.GetID().Span.Slice(0,4).CopyTo(cek._fingerprint.AsSpan());
            bool ok;
            (ok, cek.pubkey, cek._chaincode) = pubkey.Derive(cek._child, _chaincode);
            return ok ? cek : null;
        }

        public byte[] GetBytes() {
            var bytes = new byte[KzExtPubKey.BIP32_EXTKEY_SIZE];
            Encode(bytes);
            return bytes;
        }

        public override void Encode(Span<byte> code)
        {
            code[0] = _depth;
            _fingerprint.AsSpan().CopyTo(code.Slice(1, 4));
            code[5] = (byte)((_child >> 24) & 0xFF);
            code[6] = (byte)((_child >> 16) & 0xFF);
            code[7] = (byte)((_child >> 8) & 0xFF);
            code[8] = (byte)((_child >> 0) & 0xFF);
            _chaincode.Span.CopyTo(code.Slice(9, 32));
            var key = pubkey.ReadOnlySpan;
            Debug.Assert(key.Length == 33);
            key.CopyTo(code.Slice(41, 33));
        }

        public void Decode(ReadOnlySpan<byte> code)
        {
            _depth = code[0];
            code.Slice(1, 4).CopyTo(_fingerprint.AsSpan());
            _child = (uint)code[5] << 24 | (uint)code[6] << 16 | (uint)code[7] << 8 | (uint)(code[8]);
            code.Slice(9, 32).CopyTo(_chaincode.Span);
            pubkey = new KzPubKey();
            pubkey.Set(code.Slice(41, 33));
        }

        public KzB58ExtPubKey ToB58() => new KzB58ExtPubKey(this);
        public override string ToString() => ToB58().ToString();

        public override int GetHashCode() => base.GetHashCode() ^ pubkey.GetHashCode();
        public bool Equals(KzExtPubKey o) => (object)o != null && base.Equals(o) && pubkey == o.pubkey;
        public override bool Equals(object obj) => obj is KzExtPubKey && this == (KzExtPubKey)obj;
        public static bool operator ==(KzExtPubKey x, KzExtPubKey y) => object.ReferenceEquals(x, y) || (object)x == null && (object)y == null || x.Equals(y);
        public static bool operator !=(KzExtPubKey x, KzExtPubKey y) => !(x == y);
    }
}
