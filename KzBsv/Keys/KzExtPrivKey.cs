#region Copyright
// Copyright (c) 2020 TonesNotes
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

        /// <summary>
        /// Sets this extended private key to be a master (depth 0) with the given private key and chaincode and verifies required key paths.
        /// </summary>
        /// <param name="privkey">Master private key.</param>
        /// <param name="chaincode">Master chaincode.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <returns>Returns this key unless required key paths aren't valid for specified key.</returns>
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

        /// <summary>
        /// Sets this extended private key to be a master (depth 0) with the private key and chaincode set from the single 512 bit vout parameter.
        /// Master private key will be set to the first 256 bits.
        /// Chaincode will be set from the last 256 bits.
        /// </summary>
        /// <param name="vout">Master private key will be set to the first 256 bits. Chaincode will be set from the last 256 bits.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the specified key or returns null.</param>
        /// <returns>Returns this key unless required key paths aren't valid for specified key.</returns>
        public KzExtPrivKey SetMaster(KzUInt512 vout, IEnumerable<KzKeyPath> required = null)
        {
            return SetMaster(vout.ReadOnlySpan.Slice(0, 32).ToKzUInt256(), vout.ReadOnlySpan.Slice(32, 32).ToKzUInt256(), required);
        }

        /// <summary>
        /// Sets Bip32 private key.
        /// Uses a single invocation of HMACSHA512 to generate 512 bits of entropy with which to set master private key and chaincode.
        /// </summary>
        /// <param name="hmacData">Sequence of bytes passed as hmacData to HMACSHA512 along with byte encoding of hmacKey.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <param name="hmacKey">Default is current global Kz.MasterBip32Key which may default to "Bitcoin seed".</param>
        /// <returns>Returns this key unless required key paths aren't valid for generated key.</returns>
        public KzExtPrivKey SetMasterBip32(ReadOnlySpan<byte> hmacData, IEnumerable<KzKeyPath> required = null, string hmacKey = null)
        {
            hmacKey = hmacKey ?? Kz.MasterBip32Key;
            var vout = KzHashes.HMACSHA512(hmacKey.UTF8NFKDToBytes(), hmacData);
            return SetMaster(vout, required);
        }

        /// <summary>
        /// Sets hybrid Bip32 / Bip39 private key.
        /// Uses only a single Bip32 style use of HMACSHA512 starting with at least 32 bytes of Bip39 entropy from mnemonicWords.
        /// </summary>
        /// <param name="mnemonicWords">Must be at least 32 bytes of Bip39 mnemonic word entropy with valid checksum.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <param name="hmacKey">Default is current global Kz.MasterBip32Key which may default to "Bitcoin seed".</param>
        /// <returns>Returns this key unless required key paths aren't valid for generated key.</returns>
        public KzExtPrivKey SetMasterBip32(string mnemonicWords, IEnumerable<KzKeyPath> required = null, string hmacKey = null)
        {
            var e = KzMnemonic.FromWords(mnemonicWords).Entropy;
            if (e == null || e.Length < 32)
                throw new ArgumentException($"{nameof(mnemonicWords)} must provide at least 32 bytes of BIP39 mnemonic entropy.");
            return SetMasterBip32(e, required, hmacKey);
        }

        /// <summary>
        /// Computes 512 bit Bip39 seed.
        /// passphrase, password, and passwordPrefix are converted to bytes using UTF8 KD normal form encoding.
        /// </summary>
        /// <param name="passphrase">arbitrary passphrase (typically mnemonic words with checksum but not necessarily)</param>
        /// <param name="password">password and passwordPrefix are combined to generate salt bytes.</param>
        /// <param name="passwordPrefix">password and passwordPrefix are combined to generate salt bytes. Default is "mnemonic".</param>
        /// <returns>Computes 512 bit Bip39 seed.</returns>
        public static KzUInt512 Bip39Seed(string passphrase, string password = null, string passwordPrefix = "mnemonic") {
            return KzHashes.pbkdf2_hmac_sha512(passphrase.UTF8NFKDToBytes(), $"{passwordPrefix}{password}".UTF8NFKDToBytes(), 2048);
        }

        /// <summary>
        /// Sets this extended private key per Bip39.
        /// passphrase, password, and passwordPrefix are converted to bytes using UTF8 KD normal form encoding.
        /// </summary>
        /// <param name="passphrase">arbitrary passphrase (typically mnemonic words with checksum but not necessarily)</param>
        /// <param name="password">password and passwordPrefix are combined to generate salt bytes.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <param name="passwordPrefix">password and passwordPrefix are combined to generate salt bytes. Default is "mnemonic".</param>
        /// <returns>Returns this key unless required key paths aren't valid for generated key.</returns>
        public KzExtPrivKey SetMasterBip39(string passphrase, string password = null, IEnumerable<KzKeyPath> required = null, string passwordPrefix = "mnemonic") {
            var seed = Bip39Seed(passphrase, password, passwordPrefix).ReadOnlySpan;
            return SetMasterBip32(seed, required);
        }

        /// <summary>
        /// Returns a new extended private key to be a master (depth 0) with the given private key and chaincode and verifies required key paths.
        /// </summary>
        /// <param name="privkey">Master private key.</param>
        /// <param name="chaincode">Master chaincode.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <returns>Returns new key unless required key paths aren't valid for specified key in which case null is returned.</returns>
        public static KzExtPrivKey Master(KzUInt256 privkey, KzUInt256 chaincode, IEnumerable<KzKeyPath> required = null)
            => new KzExtPrivKey().SetMaster(privkey, chaincode, required);

        /// <summary>
        /// Returns a new extended private key to be a master (depth 0) with the private key and chaincode set from the single 512 bit vout parameter.
        /// Master private key will be set to the first 256 bits.
        /// Chaincode will be set from the last 256 bits.
        /// </summary>
        /// <param name="vout">Master private key will be set to the first 256 bits. Chaincode will be set from the last 256 bits.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the specified key or returns null.</param>
        /// <returns>Returns new key unless required key paths aren't valid for specified key in which case null is returned.</returns>
        public static KzExtPrivKey Master(KzUInt512 vout, IEnumerable<KzKeyPath> required = null)
            => new KzExtPrivKey().SetMaster(vout, required);

        /// <summary>
        /// Returns a new Bip32 private key.
        /// Uses a single invocation of HMACSHA512 to generate 512 bits of entropy with which to set master private key and chaincode.
        /// </summary>
        /// <param name="hmacData">Sequence of bytes passed as hmacData to HMACSHA512 along with byte encoding of hmacKey.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <param name="hmacKey">Default is current global Kz.MasterBip32Key which may default to "Bitcoin seed".</param>
        /// <returns>Returns new key unless required key paths aren't valid for specified key in which case null is returned.</returns>
        public static KzExtPrivKey MasterBip32(ReadOnlySpan<byte> hmacData, IEnumerable<KzKeyPath> required = null, string hmacKey = null)
            => new KzExtPrivKey().SetMasterBip32(hmacData, required, hmacKey);

        /// <summary>
        /// Returns a new hybrid Bip32 / Bip39 private key.
        /// Uses only a single Bip32 style use of HMACSHA512 starting with at least 32 bytes of Bip39 entropy from mnemonicWords.
        /// </summary>
        /// <param name="mnemonicWords">Must be at least 32 bytes of Bip39 mnemonic word entropy with valid checksum.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <param name="hmacKey">Default is current global Kz.MasterBip32Key which may default to "Bitcoin seed".</param>
        /// <returns>Returns new key unless required key paths aren't valid for specified key in which case null is returned.</returns>
        public static KzExtPrivKey MasterBip32(string mnemonicWords, IEnumerable<KzKeyPath> required = null, string hmacKey = null)
            => new KzExtPrivKey().SetMasterBip32(mnemonicWords, required, hmacKey);

        /// <summary>
        /// Returns a new extended private key per Bip39.
        /// passphrase, password, and passwordPrefix are converted to bytes using UTF8 KD normal form encoding.
        /// </summary>
        /// <param name="passphrase">arbitrary passphrase (typically mnemonic words with checksum but not necessarily)</param>
        /// <param name="password">password and passwordPrefix are combined to generate salt bytes.</param>
        /// <param name="required">if not null, each key path will be verified as valid on the generated key or returns null.</param>
        /// <param name="passwordPrefix">password and passwordPrefix are combined to generate salt bytes. Default is "mnemonic".</param>
        /// <returns>Returns new key unless required key paths aren't valid for specified key in which case null is returned.</returns>
        public static KzExtPrivKey MasterBip39(string passphrase, string password = null, IEnumerable<KzKeyPath> required = null, string passwordPrefix = "mnemonic")
            => new KzExtPrivKey().SetMasterBip39(passphrase, password, required, passwordPrefix);
            
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
        public KzExtPrivKey Derive(int index, bool hardened = false) => DeriveBase(index, hardened) as KzExtPrivKey;
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
