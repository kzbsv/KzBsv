#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Linq;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Generic;

namespace KzBsv
{
    public class KzSequenceSegment<T> : ReadOnlySequenceSegment<T>
    {
        public KzSequenceSegment(T[] array, int start = 0, int length = -1)
        {
            if (length == -1) length = array.Length - start;
            Memory = new Memory<T>(array, start, length);
        }

        public KzSequenceSegment(ReadOnlyMemory<T> memory, KzSequenceSegment<T> prev = null)
        {
            Memory = memory;
        }

        public KzSequenceSegment<T> Append(KzSequenceSegment<T> nextSegment)
        {
            Trace.Assert(nextSegment.RunningIndex == 0);
            Next = nextSegment;
            nextSegment.RunningIndex = RunningIndex + nextSegment.Memory.Length;
            return nextSegment;
        }

        public KzSequenceSegment<T> Append(T[] array, int start = 0, int length = -1)
            => Append(new KzSequenceSegment<T>(array, start, length));

        public KzSequenceSegment<T> Append(ReadOnlyMemory<T> memory)
        {
            var segment = new KzSequenceSegment<T>(memory) {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }

        public ReadOnlySequence<T> ToSequence()
        {
            var last = this;
            while (last.Next != null) last = last.Next as KzSequenceSegment<T>;
            return new ReadOnlySequence<T>(this, 0, last, last.Memory.Length - 1);
        }
    }

    public static class KzExtensionsReadOnlySequence
    {
        public static ReadOnlySequence<byte> ToSequence(this ReadOnlySpan<byte> span) => new ReadOnlySequence<byte>(span.ToArray());

        /// <summary>
        /// Run down both sequences as long as the bytes are equal.
        /// If we've run out of a bytes, return -1, a is less than b.
        /// If we've run out of b bytes, return 1, a is greater than b.
        /// If both are simultaneously out, they are equal, return 0.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareTo(this ReadOnlySequence<byte> a, ReadOnlySequence<byte> b)
        {
            var ac = a.Length;
            var bc = b.Length;
            var ae = a.GetEnumerator();
            var be = b.GetEnumerator();
            var aok = ae.MoveNext();
            var bok = be.MoveNext();
            var ai = -1;
            var bi = -1;
            var aspan = ReadOnlySpan<byte>.Empty;
            var bspan = ReadOnlySpan<byte>.Empty;
            while (aok && bok) {
                if (ai == -1) { aspan = ae.Current.Span; ai = 0; }
                if (bi == -1) { bspan = be.Current.Span; bi = 0; }
                if (ai >= aspan.Length) { ai = -1; aok = ae.MoveNext(); }
                if (bi >= bspan.Length) { bi = -1; bok = ae.MoveNext(); }
                if (ai == -1 || bi == -1) continue;
                if (aspan[ai++] != bspan[bi++]) break;
            }
            return aok ? 1 : bok ? -1 : 0;
        }

        /// <summary>
        /// Run down both sequences as long as the bytes are equal.
        /// If we've run out of a bytes, return -1, a is less than b.
        /// If we've run out of b bytes, return 1, a is greater than b.
        /// If both are simultaneously out, they are equal, return 0.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareTo(this ReadOnlySequence<byte> a, ReadOnlySpan<byte> b)
        {
            var ac = a.Length;
            var bc = b.Length;
            var ae = a.GetEnumerator();
            var aok = ae.MoveNext();
            var bok = true;
            var ai = -1;
            var bi = 0;
            var aspan = ReadOnlySpan<byte>.Empty;
            var bspan = b;
            while (aok && bok) {
                if (ai == -1) { aspan = ae.Current.Span; ai = 0; }
                if (ai >= aspan.Length) { ai = -1; aok = ae.MoveNext(); }
                if (bi >= bspan.Length) { bi = -1; bok = false; }
                if (ai == -1 || bi == -1) continue;
                if (aspan[ai++] != bspan[bi++]) break;
            }
            return aok ? 1 : bok ? -1 : 0;
        }

        public static int CompareTo(this ReadOnlySpan<byte> a, ReadOnlySequence<byte> b)
        {
            var ac = a.Length;
            var bc = b.Length;
            var be = b.GetEnumerator();
            var aok = true;
            var bok = be.MoveNext();
            var ai = 0;
            var bi = -1;
            var aspan = a;
            var bspan = ReadOnlySpan<byte>.Empty;
            while (aok && bok) {
                if (bi == -1) { bspan = be.Current.Span; bi = 0; }
                if (ai >= aspan.Length) { ai = -1; aok = false; }
                if (bi >= bspan.Length) { bi = -1; bok = be.MoveNext(); }
                if (ai == -1 || bi == -1) continue;
                if (aspan[ai++] != bspan[bi++]) break;
            }
            return aok ? 1 : bok ? -1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> ToSpan(ref this ReadOnlySequence<byte> sequence)
        {
            if (sequence.IsSingleSegment)
                return new SequenceReader<byte>(sequence).UnreadSpan;

            return sequence.ToArray();
        }

        /// <summary>
        /// Returns true if sequence starts with other sequence, or if other sequence length is zero.
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool StartsWith(ref this ReadOnlySequence<byte> sequence, ReadOnlySequence<byte> other)
        {
            var s = sequence;
            var o = other;
            var oLen = o.Length;

            if (oLen > s.Length) return false;

            while (oLen > 0) {
                var sMem = sequence.First;
                var oMem = other.First;
                var len = Math.Min(sMem.Length, oMem.Length);
                if (!sMem.Span.Slice(0, len).SequenceEqual(oMem.Span.Slice(0, len))) return false;
                s = s.Slice(len);
                o = o.Slice(len);
                oLen = o.Length;
            }
            return true;
        }

        /// <summary>
        /// Returns a new ReadOnlySequence with a slice removed.
        /// </summary>
        /// <param name="sequence">Sequence from which to remove a slice.</param>
        /// <param name="start">Start index of slice to remove.</param>
        /// <param name="end">End index of slice to remove</param>
        /// <returns></returns>
        public static ReadOnlySequence<byte> RemoveSlice(ref this ReadOnlySequence<byte> sequence, long start, long end)
            => sequence.RemoveSlice(sequence.GetPosition(start), sequence.GetPosition(end));

        /// <summary>
        /// Returns a new ReadOnlySequence with a slice removed.
        /// </summary>
        /// <param name="sequence">Sequence from which to remove a slice.</param>
        /// <param name="start">Start of slice to remove.</param>
        /// <param name="end">End of slice to remove</param>
        /// <returns></returns>
        public static ReadOnlySequence<byte> RemoveSlice(ref this ReadOnlySequence<byte> sequence, SequencePosition start, SequencePosition end)
        {
            var before = sequence.Slice(sequence.Start, start);
            var after = sequence.Slice(end, sequence.End);

            if (before.Length == 0)
                return after;

            if (after.Length == 0)
                return before;

            // Join before and after sequences.

            var typeBefore = before.GetSequenceType();
            var typeAfter = after.GetSequenceType();

            if (typeBefore != typeAfter)
                throw new InvalidOperationException();

            var first = (KzSequenceSegment<byte>)null;
            var last = (KzSequenceSegment<byte>)null;
            switch (typeBefore) {
                case KzSequenceType.Segment: {
                        foreach (var m in before) {
                            if (last == null)
                                last = first = new KzSequenceSegment<byte>(m);
                            else
                                last = last.Append(m);
                        }
                        foreach (var m in after) {
                            last = last.Append(m);
                        }
                    }
                    break;
                case KzSequenceType.MemoryManager:
                case KzSequenceType.Array:
                case KzSequenceType.String: {
                        first = new KzSequenceSegment<byte>(before.First);
                        last = first.Append(after.First);
                    }
                    break;
                default: throw new NotImplementedException();
            }
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        public const int FlagBitMask = 1 << 31;
        public const int IndexBitMask = ~FlagBitMask;

        static KzSequenceType GetSequenceType(ref this ReadOnlySequence<byte> sequence)
        {
            int startIndex = sequence.Start.GetInteger();
            int endIndex = sequence.End.GetInteger();

            KzSequenceType type;
            if (startIndex >= 0)
                if (endIndex >= 0)
                    type = KzSequenceType.Segment;
                else
                    type = KzSequenceType.Array;
            else
                if (endIndex >= 0)
                type = KzSequenceType.MemoryManager;
            else
                type = KzSequenceType.String;
            return type;
        }
    }

    public enum KzSequenceType
    {
        Segment,
        Array,
        MemoryManager,
        String
    }

}
