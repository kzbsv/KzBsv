#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Diagnostics;

namespace KzBsv
{
    public struct KzValType
    {
        static KzEncodeHex _hex = KzEncoders.Hex;

        ReadOnlySequence<byte> _sequence;

        public ReadOnlySequence<byte> Sequence => _sequence;

        public static KzValType None = new KzValType();

        public long Length => Sequence.Length;

        public byte FirstByte => _sequence.First.Span[0];
        public byte LastByte => _sequence.Slice(_sequence.Length - 1).First.Span[0];

        public ReadOnlySequence<byte> Slice(long start, int length) => _sequence.Slice(start, length);

        public override string ToString()
        {
            return _hex.Encode(_sequence);
        }
        public KzValType(ReadOnlySequence<byte> sequence) : this()
        {
            _sequence = sequence;
        }

        public KzValType(byte[] bytes) : this(new ReadOnlySequence<byte>(bytes)) { }

        public SequenceReader<byte> GetReader()
        {
            return new SequenceReader<byte>(Sequence);
        }

        public ReadOnlySpan<byte> ToSpan()
        {
            var r = GetReader();

            if (r.UnreadSpan.Length == _sequence.Length)
                return r.UnreadSpan;

            var bytes = new byte[_sequence.Length];
            if (!r.TryCopyTo(bytes))
                throw new InvalidOperationException();
            return bytes;
        }

        /// <summary>
        /// Return first four bytes as a big endian integer.
        /// </summary>
        /// <returns></returns>
        public UInt32 AsUInt32BigEndian() {
            var r = GetReader();
            if (r.TryReadBigEndian(out Int32 v) == false)
                throw new InvalidOperationException();
            return (UInt32)v;
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[_sequence.Length];
            if (!GetReader().TryCopyTo(bytes))
                throw new InvalidOperationException();
            return bytes;
        }

        public bool ToBool()
        {
            var r = GetReader();
            var v = (byte)0;
            while (r.TryRead(out v)) {
                if (v != 0)
                    break;
            }
            // False is zero or negative zero
            return v != 0 && (v != 0x80 || r.Remaining != 0); 

#if false
            bool CastToBool(const valtype &vch) {
                for (size_t i = 0; i<vch.size(); i++) {
                    if (vch[i] != 0) {
                        // Can be negative zero
                        if (i == vch.size() - 1 && vch[i] == 0x80) {
                            return false;
                        }
                        return true;
                    }
                }
                return false;
            }
#endif
        }

        public KzScriptNum ToScriptNum(bool fRequireMinimal = false)
        {
            return new KzScriptNum(ToSpan(), fRequireMinimal);
        }

        public Int32 ToInt32() => new KzScriptNum(ToSpan()).getint();

        public KzValType BitAnd(KzValType b)
        {
            if (Length != b.Length) throw new InvalidOperationException();
            var sa = ToSpan();
            var sb = b.ToSpan();
            var r = new byte[sa.Length];
            for (var i = 0; i < sa.Length; i++) {
                r[i] = (byte)(sa[i] & sb[i]);
            }
            return new KzValType(r);
        }

        public KzValType BitOr(KzValType b)
        {
            if (Length != b.Length) throw new InvalidOperationException();
            var sa = ToSpan();
            var sb = b.ToSpan();
            var r = new byte[sa.Length];
            for (var i = 0; i < sa.Length; i++) {
                r[i] = (byte)(sa[i] | sb[i]);
            }
            return new KzValType(r);
        }

        public KzValType BitXor(KzValType b)
        {
            if (Length != b.Length) throw new InvalidOperationException();
            var sa = ToSpan();
            var sb = b.ToSpan();
            var r = new byte[sa.Length];
            for (var i = 0; i < sa.Length; i++) {
                r[i] = (byte)(sa[i] ^ sb[i]);
            }
            return new KzValType(r);
        }

        public KzValType BitInvert()
        {
            var sa = ToSpan();
            var r = new byte[sa.Length];
            for (var i = 0; i < sa.Length; i++) {
                r[i] = (byte)(~sa[i]);
            }
            return new KzValType(r);
        }

        static byte[] maskLShift = new byte[] { 0xFF, 0x7F, 0x3F, 0x1F, 0x0F, 0x07, 0x03, 0x01 };
        static byte[] maskRShift = new byte[] { 0xFF, 0xFE, 0xFC, 0xF8, 0xF0, 0xE0, 0xC0, 0x80 };

        public KzValType LShift(int n)
        {
            var bit_shift = n % 8;
            var byte_shift = n / 8;

            var mask = maskLShift[bit_shift];
            var overflow_mask = (byte)~mask;

            var x = ToSpan();
            var r = new byte[Length];
            for (int i = r.Length - 1; i >= 0; i--) {
                int k = i - byte_shift;
                if (k >= 0) {
                    var val = (byte)(x[i] & mask);
                    val <<= bit_shift;
                    r[k] |= val;
                }

                if (k - 1 >= 0) {
                    var carryval = (byte)(x[i] & overflow_mask);
                    carryval >>= 8 - bit_shift;
                    r[k - 1] |= carryval;
                }
            }
            return new KzValType(r);
        }

        public KzValType RShift(int n)
        {
            var bit_shift = n % 8;
            var byte_shift = n / 8;

            var mask = maskRShift[bit_shift];
            var overflow_mask = (byte)~mask;

            var x = ToSpan();
            var r = new byte[Length];
            for (int i = 0; i < r.Length; i++) {
                var k = i + byte_shift;
                if (k < r.Length) {
                    var val = (byte)(x[i] & mask);
                    val >>= bit_shift;
                    r[k] |= val;
                }

                if (k + 1 < r.Length) {
                    var carryval = (byte)(x[i] & overflow_mask);
                    carryval <<= 8 - bit_shift;
                    r[k + 1] |= carryval;
                }
            }
            return new KzValType(r);
        }

        public bool BitEquals(KzValType x2)
        {
            if (Length != x2.Length) return false;
            var s1 = ToSpan();
            var s2 = x2.ToSpan();
            for (var i = 0; i < s1.Length; i++)
                if (s1[i] != s2[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Returns this value as a number resized to a specified size.
        /// If size is null, attempts to encode as the minimum size.
        /// The current value's most significant bit is assumed to be the sign bit.
        /// There can be any amount of zero byte padding between the sign bit and
        /// the first set numeric value bit.
        /// A single "padding" zero byte is required if the x80 bit is set on the byte following it (to keep the value positive).
        /// Effectively adjusts the position of the sign bit after adjusting zero byte padding.
        /// </summary>
        /// <param name="size">Target size or null to minimally encode.</param>
        /// <returns>Tuple of (bin, ok) where:
        /// bin may be original value if size matches, otherwise new storage is allocated. None on failure.
        /// ok is true on success. False if can't fit in size.
        /// </returns>
        public (KzValType bin, bool ok) NumResize(uint? size = null)
        {
            var data = ToSpan();
            var (tooLong, isNeg, extraBytes) = KzScriptNum.EvaluateAsNum(data);

            if (size == null && tooLong) goto fail;

            var length = (uint)data.Length - extraBytes;
            size ??= length;

            if (length > size) goto fail;

            if (Length == size) return (this, true);

            var bytes = new byte[(int)size];

            if (size > 0) {
                data.Slice(0, (int)length).CopyTo(bytes.AsSpan());

                // Remove the sign bit, add padding 0x00 bytes, restore the sign bit.
                // If number is positive, they start cleared, nothing to do.
                if (isNeg) {
                    // Move the set sign bit.
                    // Only clear the old bit in new array if we copied the byte.
                    if (extraBytes == 0) bytes[length - 1] &= 0x7f;
                    // Only set the new bit in the new array if the value isn't zero.
                    // Always convert negative zero to positive zero.
                    if (extraBytes < data.Length) bytes[^1] |= 0x80;
                }
            }

            return (new KzValType(bytes), true);

            fail:
            return (None, false);
        }

        /// <summary>
        /// Returns this value as a number resized to a specified size.
        /// The current value's most significant bit is assumed to be the sign bit.
        /// There can be any amount of zero byte padding between the sign bit and
        /// the first set numeric value bit.
        /// A single "padding" zero byte is required if the x80 bit is set on the byte following it (to keep the value positive).
        /// Effectively adjusts the position of the sign bit after adjusting zero byte padding.
        /// </summary>
        /// <param name="size"></param>
        /// <returns>Tuple of (bin, ok) where:
        /// bin may be original value if size matches, otherwise new storage is allocated. None on failure.
        /// ok is true on success. False if can't fit in size.
        /// </returns>
        public (KzValType bin, bool ok) Num2Bin(uint size) => NumResize(size);

        /// <summary>
        /// Returns this value as a number resized to the minimal size.
        /// The current value's most significant bit is assumed to be the sign bit.
        /// There can be any amount of zero byte padding between the sign bit and
        /// the first set numeric value bit.
        /// A single "padding" zero byte is required if the x80 bit is set on the byte following it (to keep the value positive).
        /// Effectively adjusts the position of the sign bit after adjusting zero byte padding.
        /// Fails if length of minimally encoded value exceeds the current maximum number size: KzScriptNum.MAXIMUM_ELEMENT_SIZE.
        /// </summary>
        /// <returns>Tuple of (num, ok) where:
        /// num may be original value if already minimally encoded, otherwise new storage is allocated. None on failure.
        /// ok is true on success. False if can't fit in KzScriptNum.MAXIMUM_ELEMENT_SIZE.
        /// </returns>
        public (KzValType num, bool ok) Bin2Num() => NumResize();

        /// <summary>
        /// Assumes size is greater than current length.
        /// Copies bytes to new larger array and moves the sign bit.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public KzValType SignExtend(int size)
        {
            Trace.Assert(size >= Length);
            var r = new byte[size];
            var s = ToSpan();
            s.CopyTo(r);
            var isNeg = (s[^1] & 0x80) != 0;
            r[s.Length - 1] &= 0x7f;
            if (isNeg) r[^1] |= 0x80;
            return new KzValType(r);
        }

        public KzValType Cat(KzValType vch2)
        {
            var r = new byte[Length + vch2.Length];
            var s1 = ToSpan();
            var s2 = vch2.ToSpan();
            var sr = r.AsSpan();
            s1.CopyTo(sr);
            s2.CopyTo(sr.Slice(s1.Length));
            return new KzValType(r);
        }

        public (KzValType x1, KzValType x2) Split(int position)
        {
            var s = ToSpan();
            var x1 = new KzValType(s.Slice(0, position).ToArray());
            var x2 = new KzValType(s.Slice(position).ToArray());
            return (x1, x2);
        }

        public override int GetHashCode() => _sequence.GetHashCode();
        public override bool Equals(object obj) => obj is KzValType && this == (KzValType)obj;
        public bool Equals(KzValType op) => _sequence.ToSpan().SequenceEqual(op._sequence.ToSpan());
        public static bool operator ==(KzValType x, KzValType y) => x.Equals(y);
        public static bool operator !=(KzValType x, KzValType y) => !(x == y);
    }
}
