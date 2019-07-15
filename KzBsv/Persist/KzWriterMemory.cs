#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    public class KzWriterMemory : IKzWriter
    {
        public int Length;
        public Memory<byte> Memory;

        public KzWriterMemory(Memory<byte> memory) { Memory = memory; }

        public IKzWriter Add(ReadOnlySpan<byte> data) { data.CopyTo(Memory.Span.Slice(Length)); Length += (int)data.Length; return this; }
        public IKzWriter Add(ReadOnlySequence<byte> data) { data.CopyTo(Memory.Span.Slice(Length)); Length += (int)data.Length; return this; }

        public IKzWriter Add(UInt64 v) { v.AsSpan().CopyTo(Memory.Span.Slice(Length)); Length += 8; return this; }
        public IKzWriter Add(Int64 v) { v.AsSpan().CopyTo(Memory.Span.Slice(Length)); Length += 8; return this; }
        public IKzWriter Add(UInt32 v) { v.AsSpan().CopyTo(Memory.Span.Slice(Length)); Length += 4; return this; } 
        public IKzWriter Add(Int32 v) { v.AsSpan().CopyTo(Memory.Span.Slice(Length)); Length += 4; return this; } 
        public IKzWriter Add(byte v) { Memory.Span[Length] = v; Length += 1; return this; }
        public IKzWriter Add(KzUInt160 v) { v.Span.CopyTo(Memory.Span.Slice(Length)); Length += 20; return this; } 
        public IKzWriter Add(KzUInt256 v) { v.Span.CopyTo(Memory.Span.Slice(Length)); Length += 32; return this; } 
        public IKzWriter Add(KzUInt512 v) { v.Span.CopyTo(Memory.Span.Slice(Length)); Length += 64; return this; } 
    }
}
