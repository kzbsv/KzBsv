#region Copyright
// Copyright (c) 2019 TonesNotes
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
        UInt32 n0;
        UInt32 n1;
        UInt32 n2;
        UInt32 n3;
        UInt32 n4;

		public KzUInt160(UInt32 v0 = 0, UInt32 v1 = 0, UInt32 v2 = 0, UInt32 v3 = 0, UInt32 v4 = 0)
		{
            n0 = v0;
            n1 = v1;
            n2 = v2;
            n3 = v3;
            n4 = v4;
		}

		public KzUInt160(UInt64 v0 = 0, UInt64 v1 = 0, UInt32 v2 = 0)
		{
            n0 = (UInt32)v0;
            n1 = (UInt32)(v0 >> 32);
            n2 = (UInt32)v1;
            n3 = (UInt32)(v1 >> 32);
            n4 = v2;
		}

        public KzUInt160(string hex, bool firstByteFirst = false) : this()
        {
            (firstByteFirst ? _hex : _hexRev).TryDecode(hex, Span);
        }

        public static KzUInt160 Zero { get; } = new KzUInt160(0);
        public static KzUInt160 One { get; } = new KzUInt160(1);

        static KzEncode _hex = KzEncoders.Hex;
        static KzEncode _hexRev = KzEncoders.HexReverse;

        public ReadOnlySpan<byte> ReadOnlySpan => Span;

        public Span<byte> Span {
            get {
                unsafe {
                    fixed (UInt32* p = &n0) {
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
        public BigInteger ToBN() => new BigInteger(ReadOnlySpan);
        public byte[] ToBytes() => Span.ToArray();

		public override string ToString() => _hexRev.Encode(Span);
		public string ToStringFirstByteFirst() => _hex.Encode(Span);

        public override int GetHashCode() => n0.GetHashCode() ^ n1.GetHashCode() ^ n2.GetHashCode() ^ n3.GetHashCode() ^ n4.GetHashCode();
        public override bool Equals(object obj) => obj is KzUInt160 && this == (KzUInt160)obj;
        public bool Equals(KzUInt160 o) => n0 == o.n0 && n1 == o.n1 && n2 == o.n2 && n3 == o.n3 && n4 == o.n4;

        int IComparable<KzUInt160>.CompareTo(KzUInt160 o)
        {
            var r = n0.CompareTo(o.n0);
            if (r == 0) r = n1.CompareTo(o.n1);
            if (r == 0) r = n2.CompareTo(o.n2);
            if (r == 0) r = n3.CompareTo(o.n3);
            if (r == 0) r = n4.CompareTo(o.n4);
            return r;
        }

        public static bool operator ==(KzUInt160 x, KzUInt160 y) => x.Equals(y);
        public static bool operator !=(KzUInt160 x, KzUInt160 y) => !(x == y);
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
