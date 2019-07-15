#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Linq;
using System.Security.Cryptography;

namespace KzBsv
{
    public static partial class KzHashes
    {
        public static void GetHashFinal(this HashAlgorithm alg, Span<byte> hash)
        {
            var data = new byte[0];
            alg.TransformFinalBlock(data, 0, 0);
            alg.Hash.CopyTo(hash);
        }

        public static byte[] GetHashFinal(this HashAlgorithm alg)
        {
            var data = new byte[0];
            alg.TransformFinalBlock(data, 0, 0);
            return alg.Hash;
        }

        public static void TransformFinalBlock(this HashAlgorithm alg, byte[] data, int start, int length, Span<byte> hash)
        {
            alg.TransformFinalBlock(data, start, length);
            alg.Hash.CopyTo(hash);
        }

        /// <summary>
        /// Hash used to implement BIP 32 key derivations.
        /// </summary>
        /// <param name="chainCode"></param>
        /// <param name="nChild"></param>
        /// <param name="header"></param>
        /// <param name="data"></param>
        /// <param name="output">512 bit, 64 byte hash.</param>
        public static void BIP32Hash(KzUInt256 chainCode, uint nChild, byte header, ReadOnlySpan<byte> data, Span<byte> output)
        {
            var len = data.Length;
            var buf = new byte[1 + len + 4]; // header, data, nChild
            var s = buf.AsSpan();
            s[0] = header;
            data.CopyTo(s.Slice(1, len));
            var num = s.Slice(1 + len, 4);
            num[0] = (byte)((nChild >> 24) & 0xFF);
            num[1] = (byte)((nChild >> 16) & 0xFF);
            num[2] = (byte)((nChild >> 8) & 0xFF);
            num[3] = (byte)((nChild >> 0) & 0xFF);

            HMACSHA512(chainCode.Span, s, output);
        }

        /// <summary>
        /// Duplicates Python hashlib's pbkdf2_hmac for hash_name = 'sha512' and dklen = None
        ///
        /// Performance can be improved by precomputing _trans_36 and _trans_5c.
        /// Unlike Python's hash functions, .NET doesn't currently support copying state between blocks.
        /// This results in having to recompute hash of innerSeed and outerSeed on each iteration.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <param name="iterations"></param>
        /// <returns></returns>
        public static KzUInt512 pbkdf2_hmac_sha512(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations)
        {
            if (iterations < 1)
                throw new ArgumentException();

            var _password = password.ToArray();

            using var inner = new SHA512Managed();
            using var outer = new SHA512Managed();

            var blocksize = 128; // match python hashlib's sha512 blocksize.

            if (_password.Length > blocksize)
            {
                inner.TransformFinalBlock(_password, 0, _password.Length);
                _password = inner.Hash;
                //inner.Initialize();
            }

            if (_password.Length < blocksize)
                Array.Resize(ref _password, blocksize);

            var _trans_36 = new byte[256];
            var _trans_5c = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                _trans_36[i] = (byte)(i ^ 0x36);
                _trans_5c[i] = (byte)(i ^ 0x5c);
            }

            var innerSeed = _password.Select(pb => _trans_36[pb]).ToArray();
            var outerSeed = _password.Select(pb => _trans_5c[pb]).ToArray();

            var hash = new KzUInt512();
            var xhash = new KzUInt512();

            var data = new byte[salt.Length + 4];
            salt.CopyTo(data);
            var loop = 1;
            loop.AsReadOnlySpan(bigEndian: true).CopyTo(data.AsSpan(salt.Length));
            var dataSpan = data.AsSpan();

            for (var i = 0; i < iterations; i++)
            {
                inner.TransformBlock(innerSeed);
                outer.TransformBlock(outerSeed);
                inner.TransformFinalBlock(dataSpan, hash.Span);
                outer.TransformFinalBlock(hash.Span, hash.Span);
                dataSpan = hash.Span;
                xhash = i == 0 ? hash : xhash ^ hash;
            }

            return xhash;
        }

        const int MaxBufferSize = 1 << 20; // Max ArrayPool<byte>.Shared buffer size.
    }
}
