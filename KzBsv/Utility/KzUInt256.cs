#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace KzBsv
{
    [JsonConverter(typeof(JsonConverterKzUInt256))]
    public struct KzUInt256 : IComparable<KzUInt256>
    {
        public UInt64 n0;
        public UInt64 n1;
        public UInt64 n2;
        public UInt64 n3;

        public void Set_n0(UInt64 v) => n0 = v;
        public void Set_n1(UInt64 v) => n1 = v;
        public void Set_n2(UInt64 v) => n2 = v;
        public void Set_n3(UInt64 v) => n3 = v;

        public int Length => 32;

		public KzUInt256(ReadOnlySpan<byte> span, bool reverse = false) : this()
        {
            if (span.Length < 32)
                throw new ArgumentException("32 bytes are required.");
            span.Slice(0, 32).CopyTo(Span);
            if (reverse)
                Span.Reverse();
        }

		public KzUInt256(UInt64 v0 = 0, UInt64 v1 = 0, UInt64 v2 = 0, UInt64 v3 = 0)
		{
            n0 = v0;
            n1 = v1;
            n2 = v2;
            n3 = v3;
		}

        public KzUInt256(string hex, bool firstByteFirst = false) : this()
        {
            (firstByteFirst ? Kz.Hex : Kz.HexR).TryDecode(hex, Span);
        }

        public static KzUInt256 Zero { get; } = new KzUInt256(0);
        public static KzUInt256 One { get; } = new KzUInt256(1);

        public ReadOnlySpan<byte> ReadOnlySpan => Span;

        public Span<UInt64> Span64 {
            get {
                unsafe {
                    fixed (UInt64* p = &n0) {
                        UInt64* pb = (UInt64*)p;
                        var uint64s = new Span<UInt64>(pb, 4);
                        return uint64s;
                    }
                }
            }
        }

        public Span<UInt32> Span32 {
            get {
                unsafe {
                    fixed (UInt64* p = &n0) {
                        UInt32* pb = (UInt32*)p;
                        var uint32s = new Span<UInt32>(pb, 8);
                        return uint32s;
                    }
                }
            }
        }

        public Span<byte> Span {
            get {
                unsafe {
                    fixed (UInt64* p = &n0) {
                        byte* pb = (byte*)p;
                        var bytes = new Span<byte>(pb, 32);
                        return bytes;
                    }
                }
            }
        }

        public void Read(BinaryReader s)
        {
            s.Read(Span);
        }

        public KzUInt160 ToHash160() => KzHashes.HASH160(ReadOnlySpan);
        public BigInteger ToBN() => new BigInteger(ReadOnlySpan, isUnsigned:true, isBigEndian:true);

        public byte[] ToBytes(bool reverse = false) {
            var bytes = Span.ToArray();
            if (reverse)
                bytes.AsSpan().Reverse();
            return bytes;
        }

        public void ToBytes(Span<byte> destination, bool reverse = false) {
            if (destination.Length < 32)
                throw new ArgumentException("32 byte destination is required.");
            Span.CopyTo(destination);
            if (reverse)
                destination.Reverse();
        }

        /// <summary>
        /// The bytes appear in big-endian order, as a large hexadecimally encoded number.
        /// </summary>
        /// <returns></returns>
		public override string ToString() => Kz.HexR.Encode(Span);
        /// <summary>
        /// The bytes appear in little-endian order, first byte in memory first.
        /// But the high nibble, first hex digit, of the each byte still apears before the low nibble (big-endian by nibble order).
        /// Equivalent to ToHex.
        /// </summary>
        /// <returns></returns>
		public string ToStringFirstByteFirst() => Kz.Hex.Encode(Span);
        /// <summary>
        /// The bytes appear in little-endian order, first byte in memory first.
        /// But the high nibble, first hex digit, of the each byte still apears before the low nibble (big-endian by nibble order).
        /// </summary>
        /// <returns></returns>
		public string ToHex() => Kz.Hex.Encode(Span);

        public override int GetHashCode() => n0.GetHashCode() ^ n1.GetHashCode() ^ n2.GetHashCode() ^ n3.GetHashCode();
        public override bool Equals(object obj) => obj is KzUInt256 && this == (KzUInt256)obj;
        public bool Equals(KzUInt256 o) => n0 == o.n0 && n1 == o.n1 && n2 == o.n2 && n3 == o.n3;
        public static bool operator ==(KzUInt256 x, KzUInt256 y) => x.Equals(y);
        public static bool operator !=(KzUInt256 x, KzUInt256 y) => !(x == y);

        public static bool operator >(KzUInt256 x, KzUInt256 y) => x.CompareTo(y) > 0;
        public static bool operator <(KzUInt256 x, KzUInt256 y) => x.CompareTo(y) < 0;

        public static bool operator >=(KzUInt256 x, KzUInt256 y) => x.CompareTo(y) >= 0;
        public static bool operator <=(KzUInt256 x, KzUInt256 y) => x.CompareTo(y) <= 0;

        public int CompareTo(KzUInt256 o)
        {
            var r = n3.CompareTo(o.n3);
            if (r == 0) r = n2.CompareTo(o.n2);
            if (r == 0) r = n1.CompareTo(o.n1);
            if (r == 0) r = n0.CompareTo(o.n0);
            return r;
        }

        public static KzUInt256 operator <<(KzUInt256 a, int shift) {
            const int WIDTH = 4;
            var r = KzUInt256.Zero;
            var rpn = r.Span64;
            var apn = a.Span64;
            int k = shift / 64;
            shift = shift % 64;
            for (int i = 0; i < WIDTH; i++) {
                if (i + k + 1 < WIDTH && shift != 0)
                    rpn[i + k + 1] |= (apn[i] >> (64 - shift));
                if (i + k < WIDTH)
                    rpn[i + k] |= (apn[i] << shift);
            }
            return r;
        }

        public static KzUInt256 operator >>(KzUInt256 a, int shift) {
            const int WIDTH = 4;
            var r = KzUInt256.Zero;
            var rpn = r.Span64;
            var apn = a.Span64;
            int k = shift / 64;
            shift = shift % 64;
            for (int i = 0; i < WIDTH; i++) {
                if (i - k - 1 >= 0 && shift != 0)
                    rpn[i - k - 1] |= (apn[i] << (64 - shift));
                if (i - k >= 0)
                    rpn[i - k] |= (apn[i] >> shift);
            }
            return r;
        }

        public static KzUInt256 operator ~(KzUInt256 v) {
            v.n0 = ~v.n0;
            v.n1 = ~v.n1;
            v.n2 = ~v.n2;
            v.n3 = ~v.n3;
            return v;
        }

        public int bits() {
            const int WIDTH = 8;
            var pn = Span32;
            for (int pos = WIDTH - 1; pos >= 0; pos--) {
                if (pn[pos] != 0) {
                    for (int bits = 31; bits > 0; bits--) {
                        if ((pn[pos] & (1 << bits)) != 0) return 32 * pos + bits + 1;
                    }
                    return 32 * pos + 1;
                }
            }
            return 0;
        }

        public static KzUInt256 operator ++(KzUInt256 a) {
            const int WIDTH = 4;
            var apn = a.Span64;
            int i = 0;
            while (++apn[i] == 0 && i < WIDTH - 1)
                i++;
            return a;
        }

        public static KzUInt256 operator -(KzUInt256 a, KzUInt256 b) => a + -b;
        public static KzUInt256 operator +(KzUInt256 a, UInt64 b) => a + new KzUInt256(b);
        public static KzUInt256 operator +(KzUInt256 a, KzUInt256 b) {
            const int WIDTH = 8;
            UInt64 carry = 0;
            var r = KzUInt256.Zero;
            var rpn = r.Span32;
            var apn = a.Span32;
            var bpn = b.Span32;
            for (int i = 0; i < WIDTH; i++) {
                UInt64 n = carry + apn[i] + bpn[i];
                rpn[i] = (UInt32)(n & 0xffffffff);
                carry = n >> 32;
            }
            return r;
        }

        public static KzUInt256 operator -(KzUInt256 a) {
            const int WIDTH = 4;
            var r = KzUInt256.Zero;
            var rpn = r.Span64;
            var apn = a.Span64;
            for (int i = 0; i < WIDTH; i++)
                rpn[i] = ~apn[i];
            r++;
            return r;
        }

        public static KzUInt256 operator |(KzUInt256 a, KzUInt256 b) {
            const int WIDTH = 4;
            var r = KzUInt256.Zero;
            var rpn = r.Span64;
            var apn = a.Span64;
            var bpn = b.Span64;
            for (int i = 0; i<WIDTH; i++)
                rpn[i] = apn[i] | bpn[i];
            return r;
        }

        public static KzUInt256 operator /(KzUInt256 a, KzUInt256 b) {
            var num = a;
            var div = b;
            var r = KzUInt256.Zero;

            int num_bits = num.bits();
            int div_bits = div.bits();
            if (div_bits == 0) throw new ArgumentException("Division by zero");
            // the result is certainly 0.
            if (div_bits > num_bits)
                return r;

            var rpn = r.Span32;
            int shift = num_bits - div_bits;
            // shift so that div and num align.
            div <<= shift;
            while (shift >= 0) {
                if (num >= div) {
                    num -= div;
                    // set a bit of the result.
                    rpn[shift / 32] |= (UInt32)(1 << (shift & 31));
                }
                // shift back.
                div >>= 1;
                shift--;
            }
            // num now contains the remainder of the division.
            return r;
        }

#if false
        template <unsigned int BITS> unsigned int base_uint<BITS>::bits() const {
            for (int pos = WIDTH - 1; pos >= 0; pos--) {
                if (pn[pos]) {
                    for (int bits = 31; bits > 0; bits--) {
                        if (pn[pos] & 1 << bits) return 32 * pos + bits + 1;
                    }
                    return 32 * pos + 1;
                }
            }
            return 0;
        }

        base_uint<BITS> &base_uint<BITS>::operator<<=(unsigned int shift) {
            base_uint<BITS> a(*this);
            for (int i = 0; i < WIDTH; i++)
                pn[i] = 0;
            int k = shift / 32;
            shift = shift % 32;
            for (int i = 0; i < WIDTH; i++) {
                if (i + k + 1 < WIDTH && shift != 0)
                    pn[i + k + 1] |= (a.pn[i] >> (32 - shift));
                if (i + k < WIDTH) pn[i + k] |= (a.pn[i] << shift);
            }
            return *this;
        }

        template<unsigned int BITS>
        base_uint<BITS> &base_uint<BITS>::operator>>=(unsigned int shift) {
            base_uint<BITS> a(*this);
            for (int i = 0; i < WIDTH; i++)
                pn[i] = 0;
            int k = shift / 32;
            shift = shift % 32;
            for (int i = 0; i < WIDTH; i++) {
                if (i - k - 1 >= 0 && shift != 0)
                    pn[i - k - 1] |= (a.pn[i] << (32 - shift));
                if (i - k >= 0) pn[i - k] |= (a.pn[i] >> shift);
            }
            return *this;
        }
#endif
        }

    class JsonConverterKzUInt256 : JsonConverter<KzUInt256>
    {
        public override KzUInt256 ReadJson(JsonReader reader, Type objectType, KzUInt256 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var s = (string)reader.Value;
            return new KzUInt256(s);
        }

        public override void WriteJson(JsonWriter writer, KzUInt256 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
