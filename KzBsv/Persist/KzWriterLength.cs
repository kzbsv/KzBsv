#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    public class KzWriterLength : IKzWriter
    {
        public long Length;

        public IKzWriter Add(ReadOnlySpan<byte> data) { Length += data.Length; return this; }
        public IKzWriter Add(ReadOnlySequence<byte> data) { Length += data.Length; return this; }

        public IKzWriter Add(UInt64 v) { Length += 8; return this; }
        public IKzWriter Add(Int64 v) { Length += 8; return this; }
        public IKzWriter Add(UInt32 v) { Length += 4; return this; } 
        public IKzWriter Add(Int32 v) { Length += 4; return this; } 
        public IKzWriter Add(byte v) { Length += 1; return this; }
        public IKzWriter Add(KzUInt160 v) { Length += 20; return this; } 
        public IKzWriter Add(KzUInt256 v) { Length += 32; return this; } 
        public IKzWriter Add(KzUInt512 v) { Length += 64; return this; } 
    }
}
