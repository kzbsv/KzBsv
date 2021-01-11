#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Linq;

namespace KzBsv
{
    /// <summary>
    /// The string begins with the first byte.
    /// Encodes a sequence of bytes as hexadecimal digits where:
    /// Character 0 corresponds to the high nibble of the first byte. 
    /// Character 1 corresponds to the low nibble of the first byte. 
    /// </summary>
    public class KzEncodeHex : KzEncode
    {
        protected static readonly string[] _byteToChs = Enumerable.Range(0, 256).Select(v => v.ToString("x2")).ToArray();
        protected static readonly int[] _CharToNibble;

        static KzEncodeHex()
        {
            _CharToNibble = new int['f' + 1];
            for (var i = 0; i < 'a'; i++) _CharToNibble[i] = -1;
            for (var i = 0; i < 10; i++) _CharToNibble[i + '0'] = i;
            for (var i = 0; i < 6; i++) {
                _CharToNibble[i + 'a'] = i + 10;
                _CharToNibble[i + 'A'] = i + 10;
            }
        }

        public override string Encode(ReadOnlySequence<byte> bytes)
        {
            var s = new char[bytes.Length * 2];
            var i = 0;
            foreach (var m in bytes) {
                foreach (var b in m.Span) {
                    var chs = _byteToChs[b];
                    s[i++] = chs[0];
                    s[i++] = chs[1];
                }
            }
            return new String(s);
        }

        public override string Encode(ReadOnlySpan<byte> bytes)
        {
            var s = new char[bytes.Length * 2];
            var i = 0;
            foreach (var b in bytes) {
                var chs = _byteToChs[b];
                s[i++] = chs[0];
                s[i++] = chs[1];
            }
            return new String(s);
        }

        protected static int CharToNibble(char c)
        {
            return c > _CharToNibble.Length ? -1 : _CharToNibble[c];
        }

        public override bool TryDecode(string hex, Span<byte> bytes)
        {
            if (hex.Length % 2 == 1 || hex.Length / 2 > bytes.Length)
                return false;

            if (hex.Length / 2 < bytes.Length)
                bytes.Slice(hex.Length / 2).Fill(0);

            for (int i = 0, j = 0; i < hex.Length;) {
                var a = CharToNibble(hex[i++]);
                var b = CharToNibble(hex[i++]);
                if (a == -1 || b == -1) goto fail;
                bytes[j++] = (byte)(((int)a << 4) | (int)b);
            }
            return true;
        fail:
            return false;
        }

        public override (bool, byte[]) TryDecode(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            var span = bytes.AsSpan();
            var ok = TryDecode(hex, span);
            return (ok, bytes);
        }
    }

    /// <summary>
    /// Encodes a sequence of bytes as hexadecimal digits where:
    /// The string begins with the last byte.
    /// Character 0 corresponds to the high nibble of the last byte. 
    /// Character 1 corresponds to the low nibble of the last byte. 
    /// </summary>
    public class KzEncodeHexReverse : KzEncodeHex
    {
        public override string Encode(ReadOnlySequence<byte> bytes)
        {
            var s = new char[bytes.Length * 2];
            var i = s.Length;
            foreach (var m in bytes) {
                foreach (var b in m.Span) {
                    var chs = _byteToChs[b];
                    s[--i] = chs[1];
                    s[--i] = chs[0];
                }
            }
            return new String(s);
        }

        public override string Encode(ReadOnlySpan<byte> bytes)
        {
            var s = new char[bytes.Length * 2];
            var i = s.Length;
            foreach (var b in bytes) {
                var chs = _byteToChs[b];
                s[--i] = chs[1];
                s[--i] = chs[0];
            }
            return new String(s);
        }

        public override bool TryDecode(string hex, Span<byte> bytes)
        {
            if (hex.Length % 2 == 1)
                throw new ArgumentException("Invalid hex bytes string.", nameof(hex));
            if (hex.Length != bytes.Length * 2)
                throw new ArgumentException("Length mismatch.", nameof(bytes));

            for (int i = 0, j = bytes.Length; i < hex.Length;) {
                var a = CharToNibble(hex[i++]);
                var b = CharToNibble(hex[i++]);
                if (a == -1 || b == -1) goto fail;
                bytes[--j] = (byte)(((int)a << 4) | (int)b);
            }
            return true;
        fail:
            return false;
        }

    }
}
