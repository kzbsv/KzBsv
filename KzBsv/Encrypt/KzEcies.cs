#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KzBsv
{
    /// <summary>
    /// Two parties set matching pairs of priv and pub keys.
    /// e.g. Alice sets her priv key and Bob's pub key. Bob sets his own priv key and Alice's pub key.
    /// The key pairs allow the derivation of two shared keys: kE and kM.
    /// kE is used as the encryption / decryption key.
    /// kM is used to sign the encrypted data to verify that what is received is what was sent.
    /// By default, the sender's pub key can be sent as part of the message.
    /// Set NoKey = true to omit sender's public key from the message sent. 
    /// Set ShortTag = true to reduce the content verification signature from 32 to 4 bytes.
    /// </summary>
    public class KzEcies
    {

        KzPrivKey _privKey;
        KzPubKey _pubKey;

        public KzPrivKey PrivateKey { get => _privKey; set { _privKey = value; UpdatekEkM(); } }

        public KzPubKey PublicKey { get => _pubKey; set { _pubKey = value; UpdatekEkM(); } }

        public bool ShortTag { get; set; }
        public bool NoKey { get; set; }

        KzUInt256 _kE;
        KzUInt256 _kM;

        /// <summary>
        /// Two parties set matching pairs of priv and pub keys.
        /// e.g. Alice sets her priv key and Bob's pub key. Bob sets his own priv key and Alice's pub key.
        /// And the values of _kE and _kM will be equal.
        /// _kE is used as the encryption / decryption key.
        /// _kM is used to sign the encrypted data to verify that what is received is what was sent.
        /// </summary>
        void UpdatekEkM()
        {
            if (_privKey != null && _pubKey != null && _pubKey.IsValid) {
                using var secp = new Secp256k1();
                var k = _pubKey.Clone();
                // Multiply the public key as an elliptic curve point by the private key a big number: 
                var bn = _privKey.BN;
                var pkbs = new byte[64];
                if (!secp.PublicKeyParse(pkbs.AsSpan(), _pubKey.ReadOnlySpan)) goto fail;
                if (!secp.PubKeyTweakMul(pkbs.AsSpan(), _privKey.ReadOnlySpan)) goto fail;
                // Hash the X coordinate of the resulting elliptic curve point.
                var x = pkbs.Slice(0, 32);
                x.Reverse();
                var xhex = x.ToArray().ToHex();
                var h = KzHashes.SHA512(x).ReadOnlySpan;
                _kE = new KzUInt256(h.Slice(0, 32));
                _kM = new KzUInt256(h.Slice(32, 32));
            fail:
                ;
            }
        }

        public byte[] Encrypt(string message) => Encrypt(message.UTF8ToBytes());

        public string DecryptToUTF8(ReadOnlySpan<byte> data) => Decrypt(data).ToUTF8();

        public byte[] Encrypt(ReadOnlySpan<byte> data)
        {
            var iv = KzEncrypt.GenerateIV(_privKey.ReadOnlySpan, data);

            var c = KzEncrypt.AesEncrypt(data, _kE.ToBytes(), iv);
            //var c = AESCBC_Encrypt(data.ToArray(), _kE.ToBytes(), iv);
            var d = KzHashes.HMACSHA256(_kM.ReadOnlySpan, c).ReadOnlySpan;
            if (ShortTag) d = d.Slice(0, 4);

            var key = NoKey ? Span<byte>.Empty : _privKey.GetPubKey().ReadOnlySpan;
            var len = key.Length + c.Length + d.Length;
            var bytes = new byte[len];
            var result = bytes.AsSpan();
            key.CopyTo(result.Slice(0));
            c.CopyTo(result.Slice(key.Length));
            d.CopyTo(result.Slice(key.Length + c.Length));
            return bytes;
        }

        public byte[] Decrypt(ReadOnlySpan<byte> data) {
            // data is either:
            // NoKey == true
            // 16 byte IV, <encrypted data>, 4 (ShortTag) or 32 byte d (signature)
            // NoKey == false
            // 33 byte pub key, 16 byte IV, <encrypted data>, 4 (ShortTag) or 32 byte d (signature)
            var key = NoKey ? Span<byte>.Empty : data.Slice(0, 33);
            var cd = NoKey ? data : data.Slice(33);
            var dlen = ShortTag ? 4 : 32;
            var d = cd.Slice(cd.Length - dlen);
            var c = cd.Slice(0, cd.Length - dlen);

            if (!NoKey) {
                // The data includes the sender's public key. Use it.
                PublicKey = new KzPubKey(key);
            }

            var d1 = KzHashes.HMACSHA256(_kM.ReadOnlySpan, c).ReadOnlySpan;
            if (ShortTag)
                d1 = d1.Slice(0, 4);

            var ok = d.ToHex() == d1.ToHex();
            if (!ok) {
                // Signature fails.
                return null;
            }

            var r = KzEncrypt.AesDecrypt(c, _kE.ToBytes());

            return r;
        }
    }
}
