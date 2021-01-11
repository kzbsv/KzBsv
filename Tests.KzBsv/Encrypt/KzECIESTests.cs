#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.IO;
using Xunit;
using System.Linq;
using KzBsv;
using System.Security.Cryptography;

namespace Tests.KzBsv.Encrypt
{
    public class KzECIESTests
    {
        /*
        class TC { public bool ok; public string hex; public string b58; }

        TC[] tcs = new TC[] {
            new TC { },
        };
        */

        [Fact]
        public void BsvPrivateKeyTests()
        {
            var hex = "96c132224121b509b7d0a16245e957d9192609c5637c6228311287b1be21627a";
            //var hex2 = "8080808080808080808080808080808080808080808080808080808080808080";
            var wifLivenet = "L2Gkw3kKJ6N24QcDuH4XDqt9cTqsKTVNDGz1CRZhk9cq4auDUbJy";

            var privKey = new KzPrivKey(hex);
            var privKey2 = KzPrivKey.FromWIF(wifLivenet);
            Assert.Equal(privKey, privKey2);
        }

        [Fact]
        public void AesJsTests()
        {
            var encrypted = new byte[] { 215, 238, 74, 62, 188, 204, 110, 226, 60, 165, 249, 53, 192, 105, 170, 242 };
            //            [11, 46, 168, 90, 158, 182, 37, 132, 9, 110, 109, 228, 252, 198, 9, 2]
            var iv = new byte[] { 235, 86, 206, 143, 225, 253, 82, 192, 220, 64, 112, 106, 26, 194, 193, 218 };
            var key = new byte[] { 101, 134, 119, 140, 213, 160, 234, 3, 148, 221, 195, 153, 240, 69, 253, 255 };
            var plaintext = new byte[] { 168, 8, 221, 252, 21, 193, 149, 122, 213, 17, 252, 154, 186, 66, 168, 129 }; 
            //[173, 10, 220, 38, 56, 150, 251, 88, 72, 82, 231, 198, 251, 106, 85, 180]

            using var aes = new AesCryptoServiceProvider();
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            aes.IV = iv;
            var r = (byte[])null;

            var e = aes.CreateEncryptor();
            var r1 = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

            using (var ms = new MemoryStream()) {
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
                    cs.Write(plaintext);
                }
                r = ms.ToArray();
            }
            Assert.Equal(encrypted, r);
        }

        [Fact]
        public void TestCases()
        {
            var message = "attack at dawn";

            var aliceKey = KzPrivKey.FromWIF("L1Ejc5dAigm5XrM3mNptMEsNnHzS7s51YxU7J61ewGshZTKkbmzJ");
            var bobKey = KzPrivKey.FromWIF("KxfxrUXSMjJQcb3JgnaaA6MqsrKQ1nBSxvhuigdKRyFiEm6BZDgG");

            {
                var encrypted = "0339e504d6492b082da96e11e8f039796b06cd4855c101e2492a6f10f3e056a9e712c732611c6917ab5c57a1926973bc44a1586e94a783f81d05ce72518d9b0a80e2e13c7ff7d1306583f9cc7a48def5b37fbf2d5f294f128472a6e9c78dede5f5";
                // encrypted broken down: 
                // priv.pubkey 0339e504d6492b082da96e11e8f039796b06cd4855c101e2492a6f10f3e056a9e7
                // ivbuf       12c732611c6917ab5c57a1926973bc44
                // encrypted   a1586e94a783f81d05ce72518d9b0a80
                // sig         e2e13c7ff7d1306583f9cc7a48def5b37fbf2d5f294f128472a6e9c78dede5f5

                var alice = new KzEcies { PrivateKey = aliceKey, PublicKey = bobKey.GetPubKey() };
                var bob = new KzEcies { PrivateKey = bobKey };

                var ciphertext = alice.Encrypt(message);
                Assert.Equal(encrypted, ciphertext.ToHex());

                var decryptedtext = bob.DecryptToUTF8(ciphertext);
                Assert.Equal(message, decryptedtext);
            }

            {
                var encrypted = "0339e504d6492b082da96e11e8f039796b06cd4855c101e2492a6f10f3e056a9e712c732611c6917ab5c57a1926973bc44a1586e94a783f81d05ce72518d9b0a80e2e13c7f";
                // encrypted broken down: 
                // priv.pubkey 0339e504d6492b082da96e11e8f039796b06cd4855c101e2492a6f10f3e056a9e7
                // ivbuf       12c732611c6917ab5c57a1926973bc44
                // encrypted   a1586e94a783f81d05ce72518d9b0a80
                // sig         e2e13c7f

                var alice = new KzEcies { PrivateKey = aliceKey, PublicKey = bobKey.GetPubKey(), ShortTag = true };
                var bob = new KzEcies { PrivateKey = bobKey, ShortTag = true };

                var ciphertext = alice.Encrypt(message);
                Assert.Equal(encrypted, ciphertext.ToHex());

                var decryptedtext = bob.DecryptToUTF8(ciphertext);
                Assert.Equal(message, decryptedtext);
            }

            {
                var encrypted = "12c732611c6917ab5c57a1926973bc44a1586e94a783f81d05ce72518d9b0a80e2e13c7ff7d1306583f9cc7a48def5b37fbf2d5f294f128472a6e9c78dede5f5";
                // encrypted broken down: 
                // priv.pubkey 
                // ivbuf       12c732611c6917ab5c57a1926973bc44
                // encrypted   a1586e94a783f81d05ce72518d9b0a80
                // sig         e2e13c7ff7d1306583f9cc7a48def5b37fbf2d5f294f128472a6e9c78dede5f5

                var alice = new KzEcies { PrivateKey = aliceKey, PublicKey = bobKey.GetPubKey(), NoKey = true };
                var bob = new KzEcies { PrivateKey = bobKey, PublicKey = aliceKey.GetPubKey(), NoKey = true };

                var ciphertext = alice.Encrypt(message);
                Assert.Equal(encrypted, ciphertext.ToHex());

                var decryptedtext = bob.DecryptToUTF8(ciphertext);
                Assert.Equal(message, decryptedtext);
            }

            {
                var encrypted = "12c732611c6917ab5c57a1926973bc44a1586e94a783f81d05ce72518d9b0a80e2e13c7f";
                // encrypted broken down: 
                // priv.pubkey 
                // ivbuf       12c732611c6917ab5c57a1926973bc44
                // encrypted   a1586e94a783f81d05ce72518d9b0a80
                // sig         e2e13c7f

                var alice = new KzEcies { PrivateKey = aliceKey, PublicKey = bobKey.GetPubKey(), NoKey = true, ShortTag = true };
                var bob = new KzEcies { PrivateKey = bobKey, PublicKey = aliceKey.GetPubKey(), NoKey = true, ShortTag = true };

                var ciphertext = alice.Encrypt(message);
                Assert.Equal(encrypted, ciphertext.ToHex());

                var decryptedtext = bob.DecryptToUTF8(ciphertext);
                Assert.Equal(message, decryptedtext);
            }
        }
    }
}
