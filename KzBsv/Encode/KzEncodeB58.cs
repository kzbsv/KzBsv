#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace KzBsv
{
    public class KzEncodeB58Check : KzEncode
    {
        /// <summary>
        /// Appends first 4 bytes of double SHA256 hash to bytes before standard Base58 encoding.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override string Encode(ReadOnlySpan<byte> bytes)
        {
            var checksum = KzHashes.HASH256(bytes);
            var buf = new byte[bytes.Length + 4];
            bytes.CopyTo(buf);
            checksum.Span.Slice(0, 4).CopyTo(buf.AsSpan().Slice(bytes.Length));
            return KzEncoders.B58.Encode(buf);
        }

        public override (bool ok, byte[] bytes) TryDecode(string encoded)
        {
            var (ok, bytes) = KzEncoders.B58.TryDecode(encoded);
            if (ok) {
                var span = bytes.AsSpan();
                var checksum = span.Slice(span.Length - 4);
                bytes = span.Slice(0, span.Length - 4).ToArray();
                var hash = KzHashes.HASH256(bytes);
                ok = checksum.SequenceEqual(hash.Span.Slice(0, 4));
            }
            return (ok, bytes);
        }
    }

    /// <summary>
    /// </summary>
    public class KzEncodeB58 : KzEncode
    {
        public static bool IsSpace(char c)
        {
            return c switch
            {
                ' ' => true,
                '\t' => true,
                '\n' => true,
                '\v' => true,
                '\f' => true,
                '\r' => true,
                _ => false
            };
        }

        /** All alphanumeric characters except for "0", "I", "O", and "l" */
        const string pszBase58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public override (bool ok, byte[] bytes) TryDecode(string encoded)
        {
            var s = encoded.AsSpan();

            while (!s.IsEmpty && IsSpace(s[0])) s = s.Slice(1);

            var zeroes = 0;
            var length = 0;

            while (!s.IsEmpty && s[0] == '1') { zeroes++; s = s.Slice(1); }

            // Allocate enough space in big-endian base256 representation.
            // log(58) / log(256), rounded up.
            var size = s.Length * 733 / 1000 + 1;
            var b256 = new byte[size];

            // Process the characters.
            while (!s.IsEmpty && !IsSpace(s[0])) {
                // Decode base58 character
                var carry = pszBase58.IndexOf(s[0]);
                if (carry < 0)
                    return (false, null);
                // Apply "b256 = b256 * 58 + carry".
                var i = 0;
                for (var it = 0; (carry != 0 || i < length) && it < b256.Length; it++, i++) {
                    carry += 58 * b256[it];
                    b256[it] = (byte)(carry % 256);
                    carry /= 256;
                }
                Debug.Assert(carry == 0);
                length = i;
                s = s.Slice(1);
            }
            // Skip trailing spaces.
            while (!s.IsEmpty && IsSpace(s[0])) s = s.Slice(1);
            if (!s.IsEmpty)
                return (false, null);

            // Skip trailing zeroes in b256.
            while (length > 0 && b256[length - 1] == 0) length--;

            // Result is zeroes times zero byte followed by b256[length - 1]...b256[0]
            var vch = new byte[zeroes + length];
            var nz = zeroes;
            while (zeroes-- > 0) vch[zeroes] = 0;
            while (length-- > 0) vch[nz++] = b256[length];

            return (true, vch);
        }

        public override string Encode(ReadOnlySpan<byte> bytes)
        {
            // Skip & count leading zeroes.
            var zeroes = 0;
            var length = 0;
            while (!bytes.IsEmpty && bytes[0] == 0) { bytes = bytes.Slice(1); zeroes++; }

            // Allocate enough space in big-endian base58 representation.
            // log(256) / log(58), rounded up.
            var size = bytes.Length * 138 / 100 + 1;
            var b58 = new byte[size];

            // Process the bytes.
            while (!bytes.IsEmpty) {
                var carry = (int)bytes[0];
                var i = 0;
                // Apply "b58 = b58 * 256 + ch".
                for (var it = 0; (carry != 0 || i < length) && it < b58.Length; it++, i++) {
                    carry += 256 * b58[it];
                    b58[it] = (byte)(carry % 58);
                    carry /= 58;
                }
                Debug.Assert(carry == 0);
                length = i;
                bytes = bytes.Slice(1);
            }

            // Skip trailing zeroes in b58.
            while (length > 0 && b58[length - 1] == 0) length--;

            // Translate the result into a string.
            // Result is zeroes times "1" followed by pszBase58 indexed by b58[length - 1]...b58[0]

            var sb = new StringBuilder();
            var nz = zeroes;
            while (zeroes-- > 0) sb.Append("1");
            while (length-- > 0) sb.Append(pszBase58[b58[length]]);
            return sb.ToString();
        }

    }
}
