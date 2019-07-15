#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;

namespace KzBsv
{
    public static class KzEncoders
    {
        static Lazy<KzEncodeHex> lazyHex;
        static Lazy<KzEncodeHexReverse> lazyHexReverse;
        static Lazy<KzEncodeB58> lazyB58;
        static Lazy<KzEncodeB58Check> lazyB58Check;

        static KzEncoders()
        {
            lazyHex = new Lazy<KzEncodeHex>(() => new KzEncodeHex(), true);
            lazyHexReverse = new Lazy<KzEncodeHexReverse>(() => new KzEncodeHexReverse(), true);
            lazyB58 = new Lazy<KzEncodeB58>(() => new KzEncodeB58(), true);
            lazyB58Check = new Lazy<KzEncodeB58Check>(() => new KzEncodeB58Check(), true);
        }

        /// <summary>
        /// Encodes a sequence of bytes as hexadecimal digits where:
        /// First byte first: The encoded string begins with the first byte.
        /// Character 0 corresponds to the high nibble of the first byte. 
        /// Character 1 corresponds to the low nibble of the first byte. 
        /// </summary>
        public static KzEncodeHex Hex => lazyHex.Value;

        /// <summary>
        /// Encodes a sequence of bytes as hexadecimal digits where:
        /// Last byte first: The encoded string begins with the last byte.
        /// Character 0 corresponds to the high nibble of the last byte. 
        /// Character 1 corresponds to the low nibble of the last byte. 
        /// </summary>
        public static KzEncodeHexReverse HexReverse => lazyHexReverse.Value;

        /// <summary>
        /// Base58 encoder.
        /// </summary>
        public static KzEncodeB58 B58 => lazyB58.Value;

        /// <summary>
        /// Base58 plus checksum encoder.
        /// Checksum is first 4 bytes of double SHA256 hash of byte sequence.
        /// Checksum is appended to byte sequence.
        /// </summary>
        public static KzEncodeB58Check B58Check => lazyB58Check.Value;
    }

}
