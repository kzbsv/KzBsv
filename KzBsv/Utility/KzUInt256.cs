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
    [JsonConverter(typeof(JsonConverterKzUInt256))]
    public struct KzUInt256
    {
        public UInt64 n0;
        UInt64 n1;
        UInt64 n2;
        UInt64 n3;

        public int Length => 32;

		public KzUInt256(ReadOnlySpan<byte> span) : this()
        {
            if (span.Length < 32)
                throw new ArgumentException("32 bytes are required.");
            span.Slice(0, 32).CopyTo(Span);
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
        public byte[] ToBytes() => Span.ToArray();

        /// <summary>
        /// The bytes appear in big-endian order, as a large hexadecimally encoded number.
        /// </summary>
        /// <returns></returns>
		public override string ToString() => Kz.HexR.Encode(Span);
        /// <summary>
        /// The bytes appear in little-endian order, first byte in memory first.
        /// But the high nibble, first hex digit, of the each byte still apears before the low nibble (big-endian by nibble order).
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
