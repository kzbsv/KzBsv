#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace KzBsv
{

    public static class KzExtensionsSequenceReader
    {

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCopyToA(ref this SequenceReader<byte> reader, ref KzUInt256 destination) {
			var span = destination.Span;
			if (reader.TryCopyTo(span)) {
				reader.Advance(span.Length);
				return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCopyToA<T>(ref this SequenceReader<T> reader, Span<T> destination) where T : unmanaged, IEquatable<T> {
			if (reader.TryCopyTo(destination)) {
				reader.Advance(destination.Length);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Reads an <see cref="UInt32"/> as little endian.
		/// </summary>
		/// <returns>False if there wasn't enough data for an <see cref="UInt32"/>.</returns>
		public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out uint value) {
			var b = reader.TryReadLittleEndian(out int v);
			value = (uint)v;
			return b;
		}

		/// <summary>
		/// Reads an <see cref="UInt16"/> as little endian.
		/// </summary>
		/// <returns>False if there wasn't enough data for an <see cref="UInt16"/>.</returns>
		public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out UInt16 value) {
			var b = reader.TryReadLittleEndian(out short v);
			value = (UInt16)v;
			return b;
		}

		/// <summary>
		/// Reads an <see cref="UInt64"/> as little endian.
		/// </summary>
		/// <returns>False if there wasn't enough data for an <see cref="UInt64"/>.</returns>
		public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out UInt64 value) {
			var b = reader.TryReadLittleEndian(out long v);
			value = (UInt64)v;
			return b;
		}

        /// <summary>
        /// Reads an <see cref="UInt64"/> as in bitcoin varint format.
        /// </summary>
        /// <returns>False if there wasn't enough data for an <see cref="UInt64"/>.</returns>
        public static bool TryReadVarint(ref this SequenceReader<byte> reader, out long value)
            => KzVarInt.TryRead(ref reader, out value);

	}
}
