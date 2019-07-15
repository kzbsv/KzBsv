#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    public struct KzVarInt
    {
        public long Value;

        public int Length => GetInfo(Value).length;
        public byte Prefix => GetInfo(Value).prefix;

        public bool TryRead(ref SequenceReader<byte> reader) => TryRead(ref reader, out Value);

        public byte[] AsBytes() => AsBytes(Value);

        public static byte[] AsBytes(long value)
        {
            var (len, prefix) = GetInfo(value);
            var bytes = new byte[len];
            var s = value.AsReadOnlySpan();
            switch (len) {
                case 1:
                    bytes[0] = s[0];
                    break;
                case 2:
                    bytes[0] = prefix;
                    bytes[1] = s[0];
                    bytes[2] = s[1];
                    break;
                case 4:
                    bytes[0] = prefix;
                    bytes[1] = s[0];
                    bytes[2] = s[1];
                    bytes[3] = s[2];
                    bytes[4] = s[3];
                    break;
                case 8:
                    bytes[0] = prefix;
                    bytes[1] = s[0];
                    bytes[2] = s[1];
                    bytes[3] = s[2];
                    bytes[4] = s[3];
                    bytes[5] = s[4];
                    bytes[6] = s[5];
                    bytes[7] = s[6];
                    bytes[8] = s[7];
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return bytes;
        }

        public static (int length, byte prefix) GetInfo(long value)
        {
            var len = 1;
            var prefix = (byte)0;
            var uv = (ulong)value;
            if (uv <= 0xfc) goto done;
            if (uv <= 0xffff) { len = 2; prefix = 0xfd; goto done; }
            if (uv <= 0xffff_ffff) { len = 4; prefix = 0xfe; goto done; }
            len = 8; prefix = 0xff;
        done:
            return (len, prefix);
        }

		/// <summary>
		/// Reads an <see cref="UInt64"/> as in bitcoin varint format.
		/// </summary>
		/// <returns>False if there wasn't enough data for an <see cref="UInt64"/>.</returns>
		public static bool TryRead(ref SequenceReader<byte> reader, out long value) {
			value = 0L;
			var b = reader.TryRead(out byte b0);
			if (!b) return false;
			if (b0 <= 0xfc) {
				value = b0;
			} else if (b0 == 0xfd) {
				b = reader.TryReadLittleEndian(out UInt16 v16);
				value = v16;
			} else if (b0 == 0xfe) {
				b = reader.TryReadLittleEndian(out UInt32 v32);
				value = v32;
			} else {
				b = reader.TryReadLittleEndian(out value);
			}
			return b;
		}
    }
}
