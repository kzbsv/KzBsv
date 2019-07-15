#region Copyright
// Copyright (c) 2019 TonesNotes
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
    }
}
