#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Security.Cryptography;

namespace KzBsv
{
    public class KzWriterHash : IDisposable, IKzWriter
    {
        SHA256Managed _alg = new SHA256Managed();

        public void Dispose()
        {
            _alg.Dispose();
        }

        public KzUInt256 GetHashFinal()
        {
            var hash = _alg.GetHashFinal();
            _alg.TransformFinalBlock(hash, 0, hash.Length);
            hash = _alg.Hash;
            return hash.ToKzUInt256();
        }

        public IKzWriter Add(ReadOnlySpan<byte> data) { _alg.TransformBlock(data); return this; }
        public IKzWriter Add(ReadOnlySequence<byte> data) { _alg.TransformBlock(data); return this; }

        public IKzWriter Add(UInt64 v) { _alg.TransformBlock(v.AsReadOnlySpan()); return this; }
        public IKzWriter Add(Int64 v) { _alg.TransformBlock(v.AsReadOnlySpan()); return this; }
        public IKzWriter Add(UInt32 v) { _alg.TransformBlock(v.AsReadOnlySpan()); return this; } 
        public IKzWriter Add(Int32 v) { _alg.TransformBlock(v.AsReadOnlySpan()); return this; } 
        public IKzWriter Add(byte v) { _alg.TransformBlock(new byte[] { v }); return this; }
        public IKzWriter Add(KzUInt160 v) { _alg.TransformBlock(v.ReadOnlySpan); return this; } 
        public IKzWriter Add(KzUInt256 v) { _alg.TransformBlock(v.ReadOnlySpan); return this; } 
        public IKzWriter Add(KzUInt512 v) { _alg.TransformBlock(v.ReadOnlySpan); return this; } 

        public KzWriterHash Add(string ascii) {
            _alg.TransformBlock(KzVarInt.AsBytes(ascii.Length));
            _alg.TransformBlock(ascii.ASCIIToBytes());
            return this;
        } 
    }
}
