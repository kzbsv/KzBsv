#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KzBsv
{
    public class KzExtPrivKey : KzExtKey
    {
        KzPrivKey _privkey = new KzPrivKey();

        public KzPrivKey PrivKey => _privkey;

        public KzExtPrivKey SetMaster(string passPhrase, IEnumerable<KzKeyPath> required = null)
        {
            var e = KzMnemonic.FromWords(passPhrase).Entropy;
            if (e == null || e.Length < 32)
                throw new ArgumentException($"{nameof(passPhrase)} must provide at least 32 bytes of BIP39 mnemonic entropy.");
            return SetMaster(e, required);
        }

        public KzExtPrivKey SetMaster(ReadOnlySpan<byte> seed, IEnumerable<KzKeyPath> required = null)
        {
            var vout = KzHashes.HMACSHA512(Encoding.ASCII.GetBytes(Kz.SetMasterSeed), seed).ReadOnlySpan;
            return SetMaster(vout.Slice(0, 32).ToKzUInt256(), vout.Slice(32, 32).ToKzUInt256(), required);
        }

        public KzExtPrivKey SetMaster(KzUInt512 vout, IEnumerable<KzKeyPath> required = null)
        {
            return SetMaster(vout.ReadOnlySpan.Slice(0, 32).ToKzUInt256(), vout.ReadOnlySpan.Slice(32, 32).ToKzUInt256(), required);
        }

        public KzExtPrivKey SetMaster(KzUInt256 privkey, KzUInt256 chaincode, IEnumerable<KzKeyPath> required = null)
        {
            _privkey = new KzPrivKey(privkey);
            _chaincode = chaincode;
            _depth = 0;
            _child = 0;
            _fingerprint = 0;

            if (_privkey == null || !_privkey.IsValid) goto fail;

            // Verify that all the required derivation paths yield valid keys.
            if (required != null)
                foreach (var r in required) if (Derive(r) == null) goto fail;

            return this;

        fail:
            return null;
        }

        public static KzExtPrivKey Master(string passPhrase, IEnumerable<KzKeyPath> required = null) => new KzExtPrivKey().SetMaster(passPhrase, required);
        public static KzExtPrivKey Master(ReadOnlySpan<byte> seed, IEnumerable<KzKeyPath> required = null) => new KzExtPrivKey().SetMaster(seed, required);
        public static KzExtPrivKey Master(KzUInt256 privkey, KzUInt256 chaincode, IEnumerable<KzKeyPath> required = null) => new KzExtPrivKey().SetMaster(privkey, chaincode, required);
        public static KzExtPrivKey Master(KzUInt512 vout, IEnumerable<KzKeyPath> required = null) => new KzExtPrivKey().SetMaster(vout, required);
            
        /// <summary>
        /// BIP32 uses "Neuter" to describe adding the extended key information to the public key
        /// associated with an extended private key.
        /// </summary>
        /// <returns></returns>
        public KzExtPubKey GetExtPubKey() => KzExtPubKey.FromPriv(this);
        public KzPubKey GetPubKey() => _privkey.GetPubKey();

        /// <summary>
        /// Computes the private key specified by a key path.
        /// At each derivation, there's a small chance the index specified will fail.
        /// If any generation fails, null is returned.
        /// </summary>
        /// <param name="kp"></param>
        /// <returns>null on derivation failure. Otherwise the derived private key.</returns>
        public KzExtPrivKey Derive(KzKeyPath kp) => DeriveBase(kp) as KzExtPrivKey;
        public KzExtPrivKey Derive(int index, bool hardened) => DeriveBase(index, hardened) as KzExtPrivKey;
        public KzExtPrivKey Derive(uint indexWithHardened) => Derive((int)(indexWithHardened & ~HardenedBit), (indexWithHardened & HardenedBit) != 0);

        public override KzExtKey DeriveBase(int index, bool hardened)
        {
            Trace.Assert(index >= 0);
            var cek = new KzExtPrivKey {
                _depth = (byte)(_depth + 1),
                _child = (uint)index | (hardened ? HardenedBit : 0)
            };
            _privkey.GetPubKey().GetID().Span.Slice(0,4).CopyTo(cek._fingerprint.AsSpan());
            bool ok;
            (ok, cek._privkey, cek._chaincode) = _privkey.Derive(cek._child, _chaincode);
            return ok ? cek : null;
        }

        public override void Encode(Span<byte> code)
        {
            code[0] = _depth;
            var s = _fingerprint.AsSpan();
            s.CopyTo(code.Slice(1, 4));
            code[5] = (byte)((_child >> 24) & 0xFF);
            code[6] = (byte)((_child >> 16) & 0xFF);
            code[7] = (byte)((_child >> 8) & 0xFF);
            code[8] = (byte)((_child >> 0) & 0xFF);
            _chaincode.Span.CopyTo(code.Slice(9, 32));
            code[41] = 0;
            var key = _privkey.ReadOnlySpan;
            Debug.Assert(key.Length == 32);
            key.CopyTo(code.Slice(42, 32));
        }

        public void Decode(ReadOnlySpan<byte> code)
        {
            _depth = code[0];
            code.Slice(1, 4).CopyTo(_fingerprint.AsSpan());
            _child = (uint)code[5] << 24 | (uint)code[6] << 16 | (uint)code[7] << 8 | (uint)(code[8]);
            code.Slice(9, 32).CopyTo(_chaincode.Span);
            _privkey.Set(code.Slice(42, 32), true);
        }

        public KzB58ExtPrivKey ToB58() => new KzB58ExtPrivKey(this);
        public override string ToString() => ToB58().ToString();

        public override int GetHashCode() => base.GetHashCode() ^ _privkey.GetHashCode();
        public bool Equals(KzExtPrivKey o) => (object)o != null && base.Equals(o) && _privkey == o._privkey;
        public override bool Equals(object obj) => obj is KzExtPrivKey && this == (KzExtPrivKey)obj;
        public static bool operator ==(KzExtPrivKey x, KzExtPrivKey y) => object.ReferenceEquals(x, y) || (object)x == null && (object)y == null || x.Equals(y);
        public static bool operator !=(KzExtPrivKey x, KzExtPrivKey y) => !(x == y);
    }
}
