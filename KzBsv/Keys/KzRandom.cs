#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Secp256k1Net;
using System;
using System.Buffers;
using System.Linq;
using System.Security.Cryptography;

namespace KzBsv
{
    /// <summary>
    /// 
    /// </summary>
    public static class KzRandom
    {
        /// <summary>
        /// Centralized source of cryptocgraphically strong random entropy.
        /// </summary>
        /// <param name="entropy">Output bytes.</param>
        public static void GetStrongRandBytes(Span<byte> entropy)
        {
            GetStrongRandBytes(entropy.Length).CopyTo(entropy);
        }

        /// <summary>
        /// Centralized source of cryptocgraphically strong random entropy.
        /// </summary>
        /// <param name="length">How many bytes.</param>
        public static Span<byte> GetStrongRandBytes(int length)
        {
            var buf = new byte[length];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buf);
            return buf;
        }

        static object _RandomLock = new object();
        static Random _Random = new Random();

        /// <summary>
        /// Returns a non-cryptographically strong random number
        /// greater than or equal to zero
        /// less than one.
        /// </summary>
        /// <returns></returns>
        public static double NextDouble() {
            lock (_RandomLock) {
                return _Random.NextDouble();
            }
        }

        /// <summary>
        /// Returns a non-cryptographically strong random integer
        /// in the range from low to high.
        /// </summary>
        /// <returns></returns>
        public static int InRange(int low, int high) {
            lock (_RandomLock) {
                return _Random.Next(low, high);
            }
        }
    }
}
