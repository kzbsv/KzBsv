#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Security.Cryptography;

namespace KzBsv
{
    public static partial class KzHashes
    {
        public static void RIPEMD160(this ReadOnlySequence<byte> data, Span<byte> hash)
        {
            KzRipeMD160.RIPEMD160(data, hash);
        }

        public static KzUInt160 RIPEMD160(this ReadOnlySequence<byte> data)
        {
            var h = new KzUInt160();
            RIPEMD160(data, h.Span);
            return h;
        }

        public static void SHA1(this ReadOnlySequence<byte> data, Span<byte> hash)
        {
            using (var sha = new SHA1Managed()) {
                sha.TransformFinalBlock(data, hash);
            }
        }

        public static KzUInt160 SHA1(this ReadOnlySequence<byte> data)
        {
            var hash = new KzUInt160();
            SHA1(data, hash.Span);
            return hash;
        }

        public static void SHA256(this ReadOnlySequence<byte> data, Span<byte> hash)
        {
            using (var sha = new SHA256Managed()) {
                sha.TransformFinalBlock(data, hash);
            }
        }

        public static KzUInt256 SHA256(this ReadOnlySequence<byte> data)
        {
            var hash = new KzUInt256();
            SHA256(data, hash.Span);
            return hash;
        }

        public static void SHA512(this ReadOnlySequence<byte> data, Span<byte> hash)
        {
            using (var sha = new SHA512Managed()) {
                sha.TransformFinalBlock(data, hash);
            }
        }

        /// <summary>
        /// Computes double SHA256 of data: SHA256(SHA256(data))
        /// </summary>
        /// <param name="data">Input: bytes to be hashed.</param>
        /// <param name="hash">Output: SHA256 of SHA256 of data.</param>
        public static void HASH256(this ReadOnlySequence<byte> data, Span<byte> hash)
        {
            var h1 = new KzUInt256();
            using (var sha = new SHA256Managed()) {
                TransformFinalBlock(sha, data, h1.Span);
                TransformFinalBlock(sha, h1.Span, hash);
            }
        }

        /// <summary>
        /// Computes double SHA256 of data: SHA256(SHA256(data))
        /// </summary>
        /// <param name="data">Input: bytes to be hashed.</param>
        /// <returns>SHA256 of SHA256 of data.</returns>
        public static KzUInt256 HASH256(this ReadOnlySequence<byte> data)
        {
            var h2 = new KzUInt256();
            data.HASH256(h2.Span);
            return h2;
        }

        /// <summary>
        /// Computes RIPEMD160 of SHA256 of data: RIPEMD160(SHA256(data))
        /// </summary>
        /// <param name="data">Input: bytes to be hashed.</param>
        /// <param name="hash">Output: RIPEMD160 of SHA256 of data.</param>
        public static void HASH160(this ReadOnlySequence<byte> data, Span<byte> hash)
        {
            var h = data.SHA256();
            RIPEMD160(h.Span, hash);
        }

        /// <summary>
        /// Computes RIPEMD160 of SHA256 of data: RIPEMD160(SHA256(data))
        /// </summary>
        /// <param name="data">Input: bytes to be hashed.</param>
        /// <returns>KzHash160 RIPEMD160 of SHA256 of data.</returns>
        public static KzUInt160 HASH160(this ReadOnlySequence<byte> data)
        {
            return RIPEMD160(SHA256(data).Span);
        }

        public static void HMACSHA256(this ReadOnlySpan<byte> key, ReadOnlySequence<byte> data, Span<byte> hash)
        {
            new HMACSHA256(key.ToArray()).TransformFinalBlock(data, hash);
        }

        public static KzUInt256 HMACSHA256(this ReadOnlySpan<byte> key, ReadOnlySequence<byte> data)
        {
            var h = new KzUInt256();
            new HMACSHA256(key.ToArray()).TransformFinalBlock(data, h.Span);
            return h;
        }

        public static void HMACSHA512(this ReadOnlySpan<byte> key, ReadOnlySequence<byte> data, Span<byte> hash)
        {
            new HMACSHA512(key.ToArray()).TransformFinalBlock(data, hash);
        }

        public static KzUInt512 HMACSHA512(this ReadOnlySpan<byte> key, ReadOnlySequence<byte> data)
        {
            var h = new KzUInt512();
            new HMACSHA512(key.ToArray()).TransformFinalBlock(data, h.Span);
            return h;
        }

        public static byte[] ComputeHash(this HashAlgorithm alg, ReadOnlySequence<byte> buffer)
        {
            var hash = new byte[alg.HashSize];
            alg.TransformFinalBlock(buffer, hash);
            return hash;
        }

        public static void TransformFinalBlock(this HashAlgorithm alg, ReadOnlySequence<byte> data, Span<byte> hash)
        {
            var length = data.Length;
            byte[] array = ArrayPool<byte>.Shared.Rent((int)Math.Min(MaxBufferSize, length));
            try {
                var offset = 0L;
                foreach (var m in data) {
                    var mOff = 0;
                    do {
                        var mLen = Math.Min(array.Length, m.Length - mOff);
                        m.Span.Slice(mOff, mLen).CopyTo(array);
                        mOff += mLen;
                        offset += mLen;
                        if (offset < length) {
                            alg.TransformBlock(array, 0, mLen, null, 0);
                        } else {
                            alg.TransformFinalBlock(array, 0, mLen);
                            alg.Hash.CopyTo(hash);
                        }
                    } while (mOff < m.Length);
                }
            }
            finally {
                Array.Clear(array, 0, array.Length);
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public static void TransformBlock(this HashAlgorithm alg, ReadOnlySequence<byte> data)
        {
            var length = data.Length;
            byte[] array = ArrayPool<byte>.Shared.Rent((int)Math.Min(MaxBufferSize, length));
            try {
                var offset = 0L;
                foreach (var m in data) {
                    var mOff = 0;
                    do {
                        var mLen = Math.Min(array.Length, m.Length - mOff);
                        m.Span.Slice(mOff, mLen).CopyTo(array);
                        mOff += mLen;
                        offset += mLen;
                        alg.TransformBlock(array, 0, mLen, null, 0);
                    } while (mOff < m.Length);
                }
            }
            finally {
                Array.Clear(array, 0, array.Length);
                ArrayPool<byte>.Shared.Return(array);
            }
        }

    }
}
