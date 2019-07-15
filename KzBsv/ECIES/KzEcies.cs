#region Copyright
// Copyright (c) 2019 TonesNotes
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
                //var ok = secp.PubKeyTweakMul(k.Span, bn.ToByteArray());
                // < Buffer 1c c3 95 9f 09 76 5b 99 d8 32 34 96 bd 1b 27 12 6c 0d cc 6c a3 69 ec 66 b6 cd ef 32 43 01 61 4a >
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

        public byte[] Encrypt(string message) => Encrypt(message.ASCIIToBytes());

        public byte[] Encrypt(ReadOnlySpan<byte> data)
        {
            var ivbuf = KzHashes.HMACSHA256(_privKey.ReadOnlySpan, data).ReadOnlySpan.Slice(0, 16);

            var c = AESCBC_Encrypt(data.ToArray(), _kE.ToBytes(), ivbuf.ToArray());
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

        // Encrypts the message (String or Buffer).
        // Optional `ivbuf` contains 16-byte Buffer to be used in AES-CBC.
        // By default, `ivbuf` is computed deterministically from message and private key using HMAC-SHA256.
        // Deterministic IV enables end-to-end test vectors for alternative implementations.
        // Note that identical messages have identical ciphertexts. If your protocol does not include some
        // kind of a sequence identifier inside the message *and* it is important to not allow attacker to learn
        // that message is repeated, then you should use custom IV.
        // For random IV, pass `Random.getRandomBuffer(16)` for the second argument.

        byte[] AESCBC_Encrypt(byte[] data, byte[] cipherkey, byte[] iv)
        {
            using var aes = new AesCryptoServiceProvider();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            aes.Key = cipherkey;
            aes.IV = iv;
            var r = (byte[])null;

            using (var ms = new MemoryStream()) {
                ms.Write(iv);
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(cipherkey, iv), CryptoStreamMode.Write)) {
                        cs.Write(data);
                }
                r = ms.ToArray();
            }
            return r;
        }
    }
}
