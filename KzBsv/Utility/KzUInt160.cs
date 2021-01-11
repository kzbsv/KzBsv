#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Newtonsoft.Json;
using System;
using System.IO;
using System.Numerics;

namespace KzBsv
{

    [JsonConverter(typeof(JsonConverterKzUInt160))]
    public struct KzUInt160 : IComparable<KzUInt160>
    {
        public UInt64 n0;
        public UInt64 n1;
        public UInt32 n2;

        public void Set_n0(UInt64 v) => n0 = v;
        public void Set_n1(UInt64 v) => n1 = v;
        public void Set_n2(UInt32 v) => n2 = v;

        public int Length => 20;

		public KzUInt160(ReadOnlySpan<byte> span, bool reverse = false) : this()
        {
            if (span.Length < 20)
                throw new ArgumentException("20 bytes are required.");
            span.Slice(0, 20).CopyTo(Span);
            if (reverse)
                Span.Reverse();
        }

		public KzUInt160(UInt32 v0 = 0, UInt32 v1 = 0, UInt32 v2 = 0, UInt32 v3 = 0, UInt32 v4 = 0)
		{
            n0 = v0 + ((UInt64)v1 << 32);
            n1 = v2 + ((UInt64)v3 << 32);
            n2 = v4;
		}

		public KzUInt160(UInt64 v0 = 0, UInt64 v1 = 0, UInt32 v2 = 0)
		{
            n0 = v0;
            n1 = v1;
            n2 = v2;
		}

        public KzUInt160(string hex, bool firstByteFirst = false) : this()
        {
            (firstByteFirst ? Kz.Hex : Kz.HexR).TryDecode(hex, Span);
        }

        public static KzUInt160 Zero { get; } = new KzUInt160(0);
        public static KzUInt160 One { get; } = new KzUInt160(1);

        public ReadOnlySpan<byte> ReadOnlySpan => Span;

        public Span<byte> Span {
            get {
                unsafe {
                    fixed (UInt64* p = &n0) {
                        byte* pb = (byte*)p;
                        var bytes = new Span<byte>(pb, 20);
                        return bytes;
                    }
                }
            }
        }

        public void Read(BinaryReader s)
        {
            s.Read(Span);
        }

        public string ToPubKeyAddress() => KzEncoders.B58Check.Encode(Kz.PUBKEY_ADDRESS, ReadOnlySpan);
        public BigInteger ToBN() => new BigInteger(ReadOnlySpan, isUnsigned:true, isBigEndian:true);
        public byte[] ToBytes() => Span.ToArray();

        public void ToBytes(Span<byte> destination, bool reverse = false) {
            if (destination.Length < 20)
                throw new ArgumentException("20 byte destination is required.");
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

        public override int GetHashCode() => n0.GetHashCode() ^ n1.GetHashCode() ^ n2.GetHashCode();
        public override bool Equals(object obj) => obj is KzUInt160 && this == (KzUInt160)obj;
        public bool Equals(KzUInt160 o) => n0 == o.n0 && n1 == o.n1 && n2 == o.n2;
        public static bool operator ==(KzUInt160 x, KzUInt160 y) => x.Equals(y);
        public static bool operator !=(KzUInt160 x, KzUInt160 y) => !(x == y);

        int IComparable<KzUInt160>.CompareTo(KzUInt160 o)
        {
            var r = n2.CompareTo(o.n2);
            if (r == 0) r = n1.CompareTo(o.n1);
            if (r == 0) r = n0.CompareTo(o.n0);
            return r;
        }

    }

    class JsonConverterKzUInt160 : JsonConverter<KzUInt160>
    {
        public override KzUInt160 ReadJson(JsonReader reader, Type objectType, KzUInt160 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var s = (string)reader.Value;
            return new KzUInt160(s);
        }

        public override void WriteJson(JsonWriter writer, KzUInt160 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
