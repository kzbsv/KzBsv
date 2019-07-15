#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using KzBsv;

namespace Tests.KzBsv
{
    class SequenceSegment<T> : ReadOnlySequenceSegment<T> where T : struct
    {
        public SequenceSegment(T[] array, int start, int length, SequenceSegment<T> prev = null)
        {
            Memory = new Memory<T>(array, start, length);
            if (prev != null) {
                RunningIndex = prev.RunningIndex + prev.Memory.Length;
                prev.Next = this;
            }
        }

    }

    public class KzHashesTests
    {
        [Fact]
        public void pbkdf2_hmac_sha512()
        {
            // Test cases produced by calls like:
            // hashlib.pbkdf2_hmac('sha512', 'foo'.encode('utf-8'), b'electrum', 2048).hex()

            var password = "foo".UTF8ToBytes();
            var salt = "electrum".UTF8ToBytes();
            var iterations = 1;
            var hash = KzHashes.pbkdf2_hmac_sha512(password, salt, iterations);

            Assert.Equal("03a9f1065daccbd328c4a7b8b5b123f11fac370c5503dd95ed42274d1d177c8b513f089afd4e9cfc56cda62d2849318e07a5737ada481f6e5b78e4d9b30e8d60", hash.ToHex());

            iterations = 2;
            hash = KzHashes.pbkdf2_hmac_sha512(password, salt, iterations);
            Assert.Equal("2c55fe7d6b439a9fde70667d571963eccfe2fcf399457498e0def61598fff5ae8934eafd335b4b290bff5430224e77238c005a07c58f9c3fe5d3b356699b2e65", hash.ToHex());

            iterations = 3;
            hash = KzHashes.pbkdf2_hmac_sha512(password, salt, iterations);
            Assert.Equal("759c446ff5496297a45bd949efc33343811fb23b6a752d04a5f242265aa0dcb91a32fbad3e714c77a27d8e9217fffc5ddcae0f6df4c6e674a22284bbf566eecc", hash.ToHex());
            
            iterations = 2048;
            hash = KzHashes.pbkdf2_hmac_sha512(password, salt, iterations);

            Assert.Equal("3406c404303c1d8e279dd4e56b8428bf3c1154c6991a458aa7934030b0abb1aa493a7a436322a8a5419d32ef06962d58449db57c4782c3db89c75c003bc6b338", hash.ToHex());
        }

        [Fact]
        public void electrumsv_PubKey_with_custom_words()
        {
            var seed = "bachelor nominee surprise visa oak negative anxiety observe catch sibling act hawk";
            var customWords = "fred fruitbat";
            var pub = "xpub661MyMwAqRbcH576xNa2EBv7NKLFqeGhwxqMVjLe7oKpQhdMEUjitTZzhEJRX2vAz8xn9x7V8vDhAJ87EoSgBoqNi5jCrdSrdwad6tb2trR";
            var seedHash = "f53fcf294737ded129179824d437b9f393d00afddaabc58918e15cef9246bbdcbae20af66ae15420a66c2e7115b3066296dc3d9e893d3a41bff9fe47ca702f1d";
            var iterations = 2048;

            var hash = KzHashes.pbkdf2_hmac_sha512(seed.UTF8ToBytes(), $"electrum{customWords}".UTF8ToBytes(), iterations);
            var key = KzExtPrivKey.Master(hash.Span);

            Assert.Equal(seedHash, hash.ToHex());
            Assert.Equal(pub, key.GetExtPubKey().ToString());
            Assert.Equal(key, KzElectrumSv.GetMasterPrivKey(seed, customWords));
        }    

        [Fact]
        public void electrumsv_PubKey_without_custom_words()
        {
            var seed = "police off kit fee village rather kind when turn crowd fun that";
            var customWords = (string)null;
            var pub = "xpub661MyMwAqRbcGAH5Lmmp6Tq7qtwnDfkJnMjz96AsLZTJZbHyGZe4CL792x3Eu1cNVt7BVXTAQWeQb2HBgrbcd3QZKc8M2UcX2tXy7RkLGXx";
            var seedHash = "df4ec2782c76449989f780483b57775fc6ac66c2c274abc27566b5427875529881faf0aac27aea4b294ea27e5213a44f28f8ef4fae66cd070e76bd6a6adeb08f";
            var iterations = 2048;

            var hash = KzHashes.pbkdf2_hmac_sha512(seed.UTF8ToBytes(), $"electrum{customWords}".UTF8ToBytes(), iterations);
            var key = KzExtPrivKey.Master(hash.Span);

            Assert.Equal(seedHash, hash.ToHex());
            Assert.Equal(pub, key.GetExtPubKey().ToString());
            Assert.Equal(key, KzElectrumSv.GetMasterPrivKey(seed));
        }

        [Fact]
        public void WriterHash()
        {
            using var hw = new KzWriterHash();
            var empty = hw.GetHashFinal().ToHex();
            Assert.Equal("5df6e0e2761359d30a8275058e299fcc0381534545f55cf43e41983f5d4c9456", empty);

            hw.Add("abc".ASCIIToBytes());
            var abc = hw.GetHashFinal().ToHex();
            Assert.Equal("4f8b42c22dd3729b519ba6f68d2da7cc5b2d606d05daed5ad5128cc03e6c6358", abc);

            hw.Add("de188941a3375d3a8a061e67576e926d".HexToBytes());
            var rc4_16 = hw.GetHashFinal().ToHex();
            Assert.Equal("2182d3fe9882fd597d25daf6a85e3a574e5a9861dbc75c13ce3f47fe98572246", rc4_16);

            hw.Add("56f47c020000000008516365655252516a".HexToBytes());
            var idx2 = hw.GetHashFinal().ToString();
            Assert.Equal("1e05cef9f4a9bbf62f9293b5466087b467ed3326d1f03af72f2d4ff2cf66797a", idx2);
        }

        [Fact]
        public void Partial()
        {
            var data = "Some random stuff for testing purposes.".ASCIIToBytes();
            KzUInt256 h1;
            KzUInt256 h2;

            KzUInt256 refonce;
            KzUInt256 reftwice;

            using (var alg = new SHA256Managed()) {
                alg.TransformFinalBlock(data, 0, data.Length);
                refonce = h1 = alg.Hash.ToKzUInt256();
                alg.TransformFinalBlock(data, 0, data.Length);
                h2 = alg.Hash.ToKzUInt256();
                Assert.Equal(h1, h2);

                alg.TransformFinalBlock(h1.ToBytes(), 0, 32);
                reftwice = alg.Hash.ToKzUInt256();
            }

            using (var alg = new SHA256Managed()) {
                alg.TransformBlock(data, 0, data.Length - 12, null, 0);
                alg.TransformFinalBlock(data, data.Length - 12, 12);
                h1 = alg.Hash.ToKzUInt256();
                Assert.Equal(refonce, h1);
            }

            using (var alg = new SHA256Managed()) {
                alg.TransformBlock(data, 0, data.Length, null, 0);
                alg.TransformFinalBlock(data, 0, 0);
                h1 = alg.Hash.ToKzUInt256();
                Assert.Equal(refonce, h1);
            }

            using (var hw = new KzWriterHash()) {
                hw.Add(data[..12]);
                hw.Add(data[12..]);
                h1 = hw.GetHashFinal();
                Assert.Equal(reftwice, h1);
            }
        }

        [Fact]
        public void Sha256Ros()
        {
            var d1 = new byte[4 * 1024 * 1024];
            //var d1 = new byte[900];
            (new Random(42)).NextBytes(d1);
            var h1 = new KzUInt256();
            new SHA256Managed().ComputeHash(d1).AsSpan().CopyTo(h1.Span);

            var ams = new [] {
                new int[] {},
                new int[] { 300 },
                new int[] { 1024 },
                new [] { 983, 5, 1, 2 * 1024 * 1024 + 7 },
            };
            foreach (var am in ams) {
                var ms = am.ToList();
                ms.Add(d1.Length - ms.Sum());

                var segs = new List<SequenceSegment<byte>>();
                var offset = 0;
                foreach (var m in ms) {
                    var prev = segs.Count > 0 ? segs.Last() : null;
                    var seg = new SequenceSegment<byte>(d1, offset, m, prev);
                    offset += m;
                    segs.Add(seg);
                }

                var ros = new ReadOnlySequence<byte>(segs[0], 0, segs.Last(), segs.Last().Memory.Length);

                var h2 = new KzUInt256();
                KzHashes.SHA256(ros, h2.Span);

                Assert.Equal(h1.ToString(), h2.ToString());
            }
        }

    }
}
