#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Secp256k1Net;
using System;
using System.Buffers;
using System.Linq;

namespace KzBsv
{
    /// <summary>
    /// A KzPubKey is fundamentally an array of bytes in one of these states:
    /// null: Key is invalid.
    /// byte[33]: Key is compressed.
    /// byte[65]: Key is uncompressed.
    /// </summary>
    public class KzPubKey
    {
        /// <summary>
        /// A KzPubKey is fundamentally an array of bytes in one of these states:
        /// null: Key is invalid.
        /// byte[33]: Key is compressed.
        /// byte[65]: Key is uncompressed.
        /// </summary>
        byte[] _vch = null;

        /// <summary>
        /// Creates a copy of this key.
        /// </summary>
        /// <returns></returns>
        public KzPubKey Clone() {
            var clone = new KzPubKey();
            if (_vch != null)
                clone._vch = _vch.ToArray();
            return clone;
        }

        public KzUInt256 GetX()
        {
            return new KzUInt256(_vch.Slice(1, 32));
        }

        /// <summary>
        /// True if key is stored in an array of 33 bytes.
        /// False if invalid or uncompressed.
        /// </summary>
        public bool IsCompressed => _vch?.Length == 33;

        /// <summary>
        /// True if key is defined and either compressed or uncompressed.
        /// False if array of bytes is null.
        /// </summary>
        public bool IsValid => _vch != null;

        /// <summary>
        /// Compute the length of a pubkey with a given first byte.
        /// </summary>
        /// <param name="firstByte">First byte of KzPubKey Span.</param>
        /// <returns>0, 33, or 65</returns>
        static int PredictLength(byte firstByte)
        {
            if (firstByte == 2 || firstByte == 3) return 33;
            if (firstByte == 4 || firstByte == 6 || firstByte == 7) return 65;
            return 0;
        }

        public static int MinLength => 33;
        public static int MaxLength => 65;

        public ReadOnlySpan<byte> ReadOnlySpan => _vch;
        public Span<byte> Span => _vch;

        public byte[] GetBytes() => ReadOnlySpan.ToArray();

        public void Set(ReadOnlySpan<byte> data)
        {
            var len = data.Length == 0 ? 0 : PredictLength(data[0]);
            if (len > 0 && len == data.Length) {
                _vch = new byte[data.Length];
                data.CopyTo(_vch.AsSpan());
            } else
                Invalidate();
        }

        public KzPubKey()
        {
            Invalidate();
        }

        public KzPubKey(bool compressed)
        {
            _vch = new byte[compressed ? 33 : 65];
        }

        public KzPubKey(ReadOnlySpan<byte> bytes) : this()
        {
            if (bytes.Length > 0 && bytes.Length == PredictLength(bytes[0]))
            {
                _vch = new byte[bytes.Length];
                bytes.CopyTo(_vch);
            }
        }

        public KzPubKey(string hex) : this()
        {
            try
            {
                var vch = hex.HexToBytes();
                if ((vch.Length == 33 || vch.Length == 65) && vch.Length == PredictLength(vch[0]))
                    _vch = vch;
            }
            catch { }
        }

        void Invalidate()
        {
            _vch = null;
        }

        /// <summary>
        /// Check whether a signature is normalized (lower-S).
        /// </summary>
        /// <param name="vchSig"></param>
        /// <returns></returns>
        public static bool CheckLowS(ReadOnlySequence<byte> vchSig)
        {
            var sig = new byte[64];
            var input = vchSig.ToSpan();

            using (var secp256k1 = new Secp256k1()) {

                if (!secp256k1.SignatureParseDerLax(sig, input)) return false;
                return !secp256k1.SignatureNormalize(Span<byte>.Empty, input);
            }
        }

        /// <summary>
        /// The complement function is KzPrivKey's SignCompact.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="sig"></param>
        /// <returns></returns>
        public bool RecoverCompact(KzUInt256 hash, ReadOnlySpan<byte> sig)
        {
            var (ok, vch) = secp256k1.PublicKeyRecoverCompact(hash.Span, sig);

            if (!ok)
                Invalidate();
            else
                _vch = vch;

            return ok;
        }

        public static (bool ok, KzPubKey key) FromRecoverCompact(KzUInt256 hash, ReadOnlySpan<byte> sig)
        {
            var key = new KzPubKey();
            var ok = key.RecoverCompact(hash, sig);
            if (!ok) key = null;
            return (ok, key);
        }

        public bool Verify(KzUInt256 hash, ReadOnlySpan<byte> sig)
        {
            if (!IsValid || sig.Length == 0) return false;

            return secp256k1.PublicKeyVerify(hash.Span, sig, _vch.AsSpan());
        }

        /// <summary>
        /// RIPEMD160 applied to SHA256 of the 33 or 65 public key bytes.
        /// </summary>
        /// <returns>20 byte hash as a KzUInt160</returns>
        public KzUInt160 GetID() => ToHash160();
        /// <summary>
        /// RIPEMD160 applied to SHA256 of the 33 or 65 public key bytes.
        /// </summary>
        /// <returns>20 byte hash as a KzUInt160</returns>
        public KzUInt160 ToHash160() => KzHashes.HASH160(ReadOnlySpan);

        public string ToAddress() => KzEncoders.B58Check.Encode(Kz.PUBKEY_ADDRESS, ToHash160().Span);
		public string ToHex() => _vch != null ? _hex.Encode(_vch) : "<invalid>";

        public override string ToString() => ToAddress();

        public (bool ok, KzPubKey keyChild, KzUInt256 ccChild) Derive(uint nChild, KzUInt256 cc)
        {
            if (!IsValid || !IsCompressed || nChild >= HardenedBit) goto fail;

            var vout = new byte[64];
            KzHashes.BIP32Hash(cc, nChild, ReadOnlySpan[0], ReadOnlySpan.Slice(1), vout);

            var sout = vout.AsSpan();
            var ccChild = new KzUInt256();
            sout.Slice(32, 32).CopyTo(ccChild.Span);

            var pkbs = new byte[64];
            if (!secp256k1.PublicKeyParse(pkbs.AsSpan(), ReadOnlySpan)) goto fail;

            if (!secp256k1.PubKeyTweakAdd(pkbs.AsSpan(), sout.Slice(0, 32))) goto fail;

            var dataChild = new byte[33];
            if (!secp256k1.PublicKeySerialize(dataChild.AsSpan(), pkbs, Flags.SECP256K1_EC_COMPRESSED)) goto fail;

            var keyChild = new KzPubKey(true);
            dataChild.AsSpan().CopyTo(keyChild.Span);

            return (true, keyChild, ccChild);

        fail:
            return (false, null, KzUInt256.Zero);
        }

        public override int GetHashCode() => _vch.GetHashCodeOfValues();
        public bool Equals(KzPubKey o) => (object)o != null && Enumerable.SequenceEqual(_vch, o._vch);
        public override bool Equals(object obj) => obj is KzPubKey && this == (KzPubKey)obj;
        public static bool operator ==(KzPubKey x, KzPubKey y) => object.ReferenceEquals(x, y) || (object)x == null && (object)y == null || x.Equals(y);
        public static bool operator !=(KzPubKey x, KzPubKey y) => !(x == y);

        const uint HardenedBit = 0x80000000;

        static KzEncode _hex = KzEncoders.Hex;
        
        static Lazy<Secp256k1> lazySecp256k1 = null;
        static Secp256k1 secp256k1 => lazySecp256k1.Value;

        static KzPubKey()
        {
            lazySecp256k1 = new Lazy<Secp256k1>(() => {
                var ctx = new Secp256k1(sign: true, verify: true);
                ctx.Randomize(KzRandom.GetStrongRandBytes(32));
                return ctx;
            }, true);
        }
    }
}
