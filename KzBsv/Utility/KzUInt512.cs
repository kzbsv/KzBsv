#region Copyright
// Copyright (c) 2019 TonesNotes
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
    [JsonConverter(typeof(JsonConverterKzUInt512))]
    public struct KzUInt512
    {
        public UInt64 n0;
        UInt64 n1;
        UInt64 n2;
        UInt64 n3;
        UInt64 n4;
        UInt64 n5;
        UInt64 n6;
        UInt64 n7;

        public int Length => 64;

		public KzUInt512(UInt64 v0 = 0, UInt64 v1 = 0, UInt64 v2 = 0, UInt64 v3 = 0, UInt64 v4 = 0, UInt64 v5 = 0, UInt64 v6 = 0, UInt64 v7 = 0)
		{
            n0 = v0;
            n1 = v1;
            n2 = v2;
            n3 = v3;
            n4 = v4;
            n5 = v5;
            n6 = v6;
            n7 = v7;
		}

        public KzUInt512(string hex, bool firstByteFirst = false) : this()
        {
            (firstByteFirst ? _hex : _hexRev).TryDecode(hex, Span);
        }

        public static KzUInt512 Zero { get; } = new KzUInt512(0);
        public static KzUInt512 One { get; } = new KzUInt512(1);

        static KzEncode _hex = KzEncoders.Hex;
        static KzEncode _hexRev = KzEncoders.HexReverse;

        public ReadOnlySpan<byte> ReadOnlySpan => Span;

        public Span<byte> Span {
            get {
                unsafe {
                    fixed (UInt64* p = &n0) {
                        byte* pb = (byte*)p;
                        var bytes = new Span<byte>(pb, 64);
                        return bytes;
                    }
                }
            }
        }

        public void Read(BinaryReader s)
        {
            s.Read(Span);
        }

        public BigInteger ToBN() => new BigInteger(ReadOnlySpan);
        public byte[] ToBytes() => Span.ToArray();

        /// <summary>
        /// The bytes appear in big-endian order, as a large hexadecimally encoded number.
        /// </summary>
        /// <returns></returns>
		public override string ToString() => _hexRev.Encode(Span);
        /// <summary>
        /// The bytes appear in little-endian order, first byte in memory first.
        /// But the high nibble, first hex digit, of the each byte still apears before the low nibble (big-endian by nibble order).
        /// </summary>
        /// <returns></returns>
		public string ToStringFirstByteFirst() => _hex.Encode(Span);
        /// <summary>
        /// The bytes appear in little-endian order, first byte in memory first.
        /// But the high nibble, first hex digit, of the each byte still apears before the low nibble (big-endian by nibble order).
        /// </summary>
        /// <returns></returns>
		public string ToHex() => Kz.Hex.Encode(Span);

        public override int GetHashCode() => n0.GetHashCode() ^ n1.GetHashCode() ^ n2.GetHashCode() ^ n3.GetHashCode();
        public override bool Equals(object obj) => obj is KzUInt512 && this == (KzUInt512)obj;
        public bool Equals(KzUInt512 o) => n0 == o.n0 && n1 == o.n1 && n2 == o.n2 && n3 == o.n3 && n4 == o.n4 && n5 == o.n5 && n6 == o.n6 && n7 == o.n7;
        public static bool operator ==(KzUInt512 x, KzUInt512 y) => x.Equals(y);
        public static bool operator !=(KzUInt512 x, KzUInt512 y) => !(x == y);

        public static KzUInt512 operator ^(KzUInt512 x, KzUInt512 y)
        {
            var r = new KzUInt512();
            r.n0 = x.n0 ^ y.n0;
            r.n1 = x.n1 ^ y.n1;
            r.n2 = x.n2 ^ y.n2;
            r.n3 = x.n3 ^ y.n3;
            r.n4 = x.n4 ^ y.n4;
            r.n5 = x.n5 ^ y.n5;
            r.n6 = x.n6 ^ y.n6;
            r.n7 = x.n7 ^ y.n7;
            return r;
        }
    }

    class JsonConverterKzUInt512 : JsonConverter<KzUInt512>
    {
        public override KzUInt512 ReadJson(JsonReader reader, Type objectType, KzUInt512 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var s = (string)reader.Value;
            return new KzUInt512(s);
        }

        public override void WriteJson(JsonWriter writer, KzUInt512 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
