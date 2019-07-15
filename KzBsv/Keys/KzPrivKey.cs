#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Secp256k1Net;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace KzBsv
{
    /// <summary>
    /// 
    /// </summary>
    public class KzPrivKey
    {
        bool fCompressed;
        KzUInt256 keydata;

        Flags IsCompressedFlag => fCompressed ? Flags.SECP256K1_EC_COMPRESSED : Flags.SECP256K1_EC_UNCOMPRESSED;

        bool fValid;
        public bool IsValid => fValid;

        int PubKeyLength => Secp256k1.PUBKEY_LENGTH;
        int SerializedPubKeyLength => fCompressed ? Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH : Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH;

        /// <summary>
        /// True if the corresponding public key is compressed.
        /// </summary>
        public bool IsCompressed => fCompressed;

        public ReadOnlySpan<byte> ReadOnlySpan => keydata.ReadOnlySpan;

        public BigInteger BN => keydata.ToBN();

        static bool Check(ReadOnlySpan<byte> vch)
        {
            using (var secp256k1 = new Secp256k1()) {
                return secp256k1.SecretKeyVerify(vch);
            }
        }

        public KzPrivKey() { }

        public KzPrivKey(ReadOnlySpan<byte> span, bool compressed = true) { Set(span, compressed); }

        public KzPrivKey(KzUInt256 v, bool compressed = true) { Set(v.ReadOnlySpan, compressed); }

        public KzPrivKey(string hex, bool compressed = true) : this(new KzUInt256(hex, firstByteFirst:true), compressed) { }

        public static KzPrivKey FromHex(string hex, bool compressed = true) => new KzPrivKey(new KzUInt256(hex, firstByteFirst: true), compressed);
        public static KzPrivKey FromB58(string b58) => new KzB58PrivKey(b58).GetKey();
        public static KzPrivKey FromWIF(string wif) => new KzB58PrivKey(wif).GetKey();

        public void Set(ReadOnlySpan<byte> data, bool compressed = true)
        {
            if (data.Length != keydata.Length || !Check(data))
                fValid = false;
            else {
                data.CopyTo(keydata.Span);
                fCompressed = compressed;
                fValid = true;
            }
        }

        public void MakeNewKey(bool compressed)
        {
            do {
                KzRandom.GetStrongRandBytes(keydata.Span);
            } while (!Check(keydata.ReadOnlySpan));
            fValid = true;
            fCompressed = compressed;
        }

        public KzPubKey GetPubKey()
        {
            Trace.Assert(fValid);
            var pubKeySecp256k1 = new byte[PubKeyLength];
            var ok = secp256k1.PublicKeyCreate(pubKeySecp256k1, keydata.ReadOnlySpan);
            Trace.Assert(ok);
            var pubKey = new KzPubKey(fCompressed);
            secp256k1.PublicKeySerialize(pubKey.Span, pubKeySecp256k1, IsCompressedFlag);
            Trace.Assert(pubKey.IsValid);
            return pubKey;
        }

        /// <summary>
        /// The complement function is KzPubKey's RecoverCompact or KzPubKey.FromRecoverCompact.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public (bool ok, byte[] sig) SignCompact(KzUInt256 hash)
        {
            if (!fValid) return (false, null);

            var (ok, sig) = secp256k1.PrivateKeySignCompact(hash.Span, keydata.Span, IsCompressed);

            return (ok, sig);
        }

        public (bool ok, byte[] sig) Sign(KzUInt256 hash)
        {
            if (!fValid) return (false, null);

            var (ok, sig) = secp256k1.PrivateKeySign(hash.Span, keydata.Span);

            return (ok, sig);
        }

        /// <summary>
        /// Verify thoroughly whether a private key and a public key match.
        /// This is done using a different mechanism than just regenerating it.
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public bool VerifyPubKey(KzPubKey pubkey)
        {
            if (pubkey.IsCompressed != fCompressed)
                return false;

            var rnd = KzRandom.GetStrongRandBytes(8).ToArray();
            var str = "Bitcoin key verification\n";

            var hash = KzHashes.HASH256(Encoding.ASCII.GetBytes(str).Concat(rnd).ToArray());

            var (ok, sig) = Sign(hash);

            if (!ok) return false;

            return pubkey.Verify(hash, sig);
        }

        public (bool ok, KzPrivKey keyChild, KzUInt256 ccChild) Derive(uint nChild, KzUInt256 cc)
        {
            if (!IsValid || !IsCompressed) goto fail;

            var vout = new byte[64];

            if (nChild < HardenedBit) {
                // Not hardened.
                var pubkey = GetPubKey();
                Debug.Assert(pubkey.ReadOnlySpan.Length == 33);
                KzHashes.BIP32Hash(cc, nChild, pubkey.ReadOnlySpan[0], pubkey.ReadOnlySpan.Slice(1), vout);
            } else {
                // Hardened.
                Debug.Assert(keydata.Span.Length == 32);
                KzHashes.BIP32Hash(cc, nChild, 0, keydata.Span, vout);
            }

            var sout = vout.AsSpan();
            var ccChild = new KzUInt256();
            sout.Slice(32, 32).CopyTo(ccChild.Span);

            var dataChild = new KzUInt256();
            keydata.Span.CopyTo(dataChild.Span);

            var ok = secp256k1.PrivKeyTweakAdd(dataChild.Span, sout.Slice(0, 32));
            if (!ok) goto fail;
            var keyChild = new KzPrivKey(dataChild);
            return (ok, keyChild, ccChild);

        fail:
            return (false, null, KzUInt256.Zero);
        }

        public string ToHex() => keydata.ToStringFirstByteFirst();
        public KzB58PrivKey ToB58() => new KzB58PrivKey(this);
        public override string ToString() => ToB58().ToString();

        public override int GetHashCode() => keydata.GetHashCode();
        public bool Equals(KzPrivKey o) => (object)o != null && fCompressed.Equals(o.fCompressed) && keydata.Equals(o.keydata);
        public override bool Equals(object obj) => obj is KzPrivKey && this == (KzPrivKey)obj;
        public static bool operator ==(KzPrivKey x, KzPrivKey y) => object.ReferenceEquals(x, y) || (object)x == null && (object)y == null || x.Equals(y);
        public static bool operator !=(KzPrivKey x, KzPrivKey y) => !(x == y);

        const uint HardenedBit = 0x80000000;

        static Lazy<Secp256k1> lazySecp256k1 = null;
        static Secp256k1 secp256k1 => lazySecp256k1.Value;

        static KzPrivKey()
        {
            lazySecp256k1 = new Lazy<Secp256k1>(() => {
                var ctx = new Secp256k1(sign: true, verify: false);
                ctx.Randomize(KzRandom.GetStrongRandBytes(32));
                return ctx;
            }, true);
        }

        static KzEncode _hex = KzEncoders.Hex;

    }
}
