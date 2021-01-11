#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KzBsv
{
    public static class KzExtensions
    {
        public static KzAmount ToKzAmount(this long value) => new KzAmount(value);
        public static KzAmount ToKzAmount(this decimal amount, KzBitcoinUnit unit) => new KzAmount(amount, unit);

        public static KzUInt256 ToKzUInt256(this ReadOnlySpan<byte> span) => new KzUInt256(span);
        public static KzUInt256 ToKzUInt256(this string hex, bool firstByteFirst = false) => new KzUInt256(hex, firstByteFirst);
        public static KzScript ToKzScript(this string hex) => new KzScript(hex);

        /// <summary>
        /// Decode a script segment and convert the Ops to builder BOps.
        /// </summary>
        /// <param name="script"></param>
        /// <returns>Sequence of builder BOps</returns>
        public static IEnumerable<KzBOp> ToBOps(this KzScript script) => script.Decode().Select(o => new KzBOp(o));

        /// <summary>
        /// Returns a KzUInt256 initialized with up to 32 bytes from data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static KzUInt256 ToKzUInt256(this Span<byte> data)
        {
            var v = new KzUInt256();
            data.CopyTo(v.Span);
            return v;
        }

        /// <summary>
        /// Throws ArgumentException if a is null or not 32 bytes long.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static KzUInt256 ToKzUInt256(this byte[] a)
        {
            if (a?.Length != 32) throw new ArgumentException();
            var i = new KzUInt256();
            a.AsSpan().CopyTo(i.Span);
            return i;
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static Span<byte> AsSpan(this ref Int64 i)
        {
            unsafe {
                fixed (Int64* p = &i) {
                    byte* pb = (byte*)p;
                    var bytes = new Span<byte>(pb, 8);
                    return bytes;
                }
            }
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> AsReadOnlySpan(this ref Int64 i)
        {
            unsafe {
                fixed (Int64* p = &i) {
                    byte* pb = (byte*)p;
                    var bytes = new Span<byte>(pb, 8);
                    return bytes;
                }
            }
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static Span<byte> AsSpan(this ref UInt64 i)
        {
            unsafe {
                fixed (UInt64* p = &i) {
                    byte* pb = (byte*)p;
                    var bytes = new Span<byte>(pb, 8);
                    return bytes;
                }
            }
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> AsReadOnlySpan(this ref UInt64 i)
        {
            unsafe {
                fixed (UInt64* p = &i) {
                    byte* pb = (byte*)p;
                    var bytes = new Span<byte>(pb, 8);
                    return bytes;
                }
            }
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static Span<byte> AsSpan(this ref Int32 i)
        {
            unsafe {
                fixed (Int32* p = &i) {
                    byte* pb = (byte*)p;
                    var bytes = new Span<byte>(pb, 4);
                    return bytes;
                }
            }
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> AsReadOnlySpan(this ref Int32 i, bool bigEndian = false)
        {
            if (BitConverter.IsLittleEndian == !bigEndian)
            {
                unsafe
                {
                    fixed (Int32* p = &i)
                    {
                        byte* pb = (byte*)p;
                        var bytes = new Span<byte>(pb, 4);
                        return bytes;
                    }
                }
            } else
            {
                var bytes = i.AsReadOnlySpan(!bigEndian).ToArray();
                Array.Reverse(bytes);
                return bytes;
            }
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static Span<byte> AsSpan(this ref UInt32 i)
        {
            unsafe {
                fixed (UInt32* p = &i) {
                    byte* pb = (byte*)p;
                    var bytes = new Span<byte>(pb, 4);
                    return bytes;
                }
            }
        }

        /// <summary>
        /// Returns access to an integer as a span of bytes.
        /// Reflects the endianity of the underlying implementation.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> AsReadOnlySpan(this ref UInt32 i)
        {
            unsafe {
                fixed (UInt32* p = &i) {
                    byte* pb = (byte*)p;
                    var bytes = new Span<byte>(pb, 4);
                    return bytes;
                }
            }
        }

        // These two extensions had to be added between .NET Core 3.0 preview 4 and 5:
        // probably temporary or a configuration thing...
        public static Span<byte> Slice(this Span<byte> s, Index startIndex) => s.Slice(startIndex.GetOffset(s.Length));
        public static Span<byte> Slice(this Span<byte> s, Range range) { var (o, l) = range.GetOffsetAndLength(s.Length); return s.Slice(o, l); }

        public static Span<byte> Slice(this byte[] a, Index startIndex) => a.AsSpan().Slice(startIndex);
        public static Span<byte> Slice(this byte[] a, Range range) => a.AsSpan().Slice(range);
        public static Span<byte> Slice(this byte[] a, int start) => a.AsSpan().Slice(start);
        public static Span<byte> Slice(this byte[] a, int start, int length) => a.AsSpan().Slice(start, length);

        public static int GetHashCodeOfValues(this IEnumerable<byte> a)
        {
            if (a == null) return 0;
            var h = 17;
            foreach (var b in a) h = h * 31 + b;
            return h;
        }

        public static bool IsNullOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);
        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);
        /// <summary>
        /// IsNullOrWhiteSpace: String is null, empty, or only white space characters.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsBlank(this string s) => string.IsNullOrWhiteSpace(s);
        /// <summary>
        /// !IsNullOrWhiteSpace: String contains non white space characters.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNotBlank(this string s) => !string.IsNullOrWhiteSpace(s);

        public static byte[] HexToBytes(this string s) => KzEncoders.Hex.Decode(s);
        public static byte[] HexRToBytes(this string s) => KzEncoders.HexReverse.Decode(s);
        public static byte[] ASCIIToBytes(this string s) => Encoding.ASCII.GetBytes(s);
        public static byte[] UTF8ToBytes(this string s) => Encoding.UTF8.GetBytes(s);
        public static byte[] UTF8NFKDToBytes(this string s) => Encoding.UTF8.GetBytes(s.Normalize(NormalizationForm.FormKD));
        public static byte[] Base64ToBytes(this string s) => Convert.FromBase64String(s);

        public static string ToUTF8(this byte[] a) => Encoding.UTF8.GetString(a);

        public static string ToHex(this Span<byte> s) => KzEncoders.Hex.Encode(s);
        public static string ToHex(this ReadOnlySequence<byte> s) => KzEncoders.Hex.Encode(s);
        public static string ToHex(this ReadOnlySpan<byte> s) => KzEncoders.Hex.Encode(s);
        public static string ToHex(this byte[] a) => KzEncoders.Hex.Encode(a);
        public static string ToHexR(this byte[] a) => KzEncoders.HexReverse.Encode(a);

        public static int ToInt32LittleEndian(this Span<byte> s) => ConvertToInt32LittleEndian(s);
        public static int ToInt32LittleEndian(this ReadOnlySpan<byte> s) => ConvertToInt32LittleEndian(s);

        public static int ToInt32BigEndian(this Span<byte> s) => ConvertToInt32BigEndian(s);
        public static int ToInt32BigEndian(this ReadOnlySpan<byte> s) => ConvertToInt32BigEndian(s);

        static int ConvertToInt32LittleEndian(ReadOnlySpan<byte> s) {
            if (s.Length < 4) throw new InvalidOperationException("Requires four bytes");
            return s[0] + (s[1] << 8) + (s[2] << 16) + (s[3] << 24);
        }

        static int ConvertToInt32BigEndian(ReadOnlySpan<byte> s) {
            if (s.Length < 4) throw new InvalidOperationException("Requires four bytes");
            return s[3] + (s[2] << 8) + (s[1] << 16) + (s[0] << 24);
        }

        public static byte[] AsVarIntBytes(this int v) => KzVarInt.AsBytes(v);
        public static byte[] AsVarIntBytes(this long v) => KzVarInt.AsBytes(v);

        public static DateTime DateTimeFromUnixTime(this long seconds) => DateTime.UnixEpoch + TimeSpan.FromSeconds(seconds);

        public static (List<T> t, List<T> f) Partition<T>(this IEnumerable<T> s, Func<T, bool> predicate)
        {
            var f = new List<T>();
            var t = new List<T>();
            foreach (var i in s) if (predicate(i)) t.Add(i); else f.Add(i);
            return (t, f);
        }
    }
}
