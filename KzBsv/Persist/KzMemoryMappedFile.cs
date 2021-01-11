#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace KzBsv
{
    /// <summary>
    /// Modeled on System.IO.MemoryMappedFile pared down to essentials with Span based access.
    /// Purpose is to support IO to very large files without unnecessary object creation.
    /// https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files
    /// </summary>
    public unsafe class KzMemoryMappedFile : IDisposable
    {
        readonly string _fileName;

        readonly long _fileLength;

        readonly MemoryMappedFile _mmf;

        readonly MemoryMappedViewAccessor _mmva;

        readonly byte* _originPtr = null;

        long _lastSpanOffset;
        int _lastSpanLength;

        bool _disposed;

        public string FileName => _fileName;
        public long FileLength => _fileLength;

        public KzMemoryMappedFile(MemoryMappedFile mmf)
        {
            _mmf = mmf;
            _mmva = _mmf.CreateViewAccessor();
            _mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref _originPtr);
            _disposed = false;
            _fileName = null;
            // Capacity seems to be actual file length rounded up to block size (4096).
            // On close, only bytes up to actual file length will be saved.
            _fileLength = _mmva.Capacity;
        }

        public KzMemoryMappedFile(string fileName) : this(MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null))
        {
            _fileName = fileName;
        }

        public KzMemoryMappedFile(string fileName, long fileLength) : this(MemoryMappedFile.CreateFromFile(fileName, FileMode.OpenOrCreate, null, fileLength))
        {
            _fileName = fileName;
            _fileLength = fileLength;
        }

        /// <summary>
        /// Returns a span to whole file.
        /// Will fail if file is larger than int.MaxValue (4+GB).
        /// Map spans to subsets of very large files as the offset is a long.
        /// </summary>
        /// <returns></returns>
        public Span<byte> GetSpan()
        {
            if (_disposed)
                throw new InvalidOperationException();

            if (_fileLength >= int.MaxValue)
                throw new InvalidOperationException("File too large.");

            _lastSpanOffset = 0L;
            _lastSpanLength = (int)_fileLength;

            return new Span<byte>(_originPtr, (int)_fileLength);
        }

        public Span<byte> GetSpan(long offset, int length)
        {
            if (_disposed)
                throw new InvalidOperationException();

            if (offset < 0 || offset > _fileLength)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (length < 0 || offset + length > _fileLength)
                throw new ArgumentOutOfRangeException(nameof(length));

            _lastSpanOffset = offset;
            _lastSpanLength = length;

            return new Span<byte>(_originPtr + offset, length);
        }

        public void Flush()
        {
            _mmva.Flush();
        }

        public void FlushLastSpan()
        {
            Flush(_lastSpanOffset, _lastSpanLength);
        }

        public void Flush(Span<byte> span)
        {
            fixed (byte* spanByte0 = &span[0]) {

                var offset = spanByte0 - _originPtr;
                var length = span.Length;

                Flush(offset, length);
            }
        }

        public void Flush(long offset, int length)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                // Based on the underlying implementation of MemoryMappedFile
                // See https://github.com/dotnet/corefx/blob/master/src/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedView.Unix.cs
                MyInterop.Sys.MSync((IntPtr)(_originPtr + offset), (ulong)length,
                    MyInterop.Sys.MemoryMappedSyncFlags.MS_SYNC | MyInterop.Sys.MemoryMappedSyncFlags.MS_INVALIDATE);

                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // Based on the underlying implementation of MemoryMappedFile
                // See https://github.com/dotnet/corefx/blob/master/src/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedView.Windows.cs
                MyInterop.Kernel32.FlushViewOfFile((IntPtr)(_originPtr + offset), (UIntPtr)length);

                // See https://docs.microsoft.com/en-us/windows/desktop/FileIO/file-buffering
                // See https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-flushfilebuffers
                MyInterop.Kernel32.FlushFileBuffers(_mmva.SafeMemoryMappedViewHandle);

                return;
            }

            throw new NotSupportedException($"Platform not supported: {RuntimeInformation.OSDescription}.");
        }

        void ReleaseUnmanagedResources()
        {
            _mmva.SafeMemoryMappedViewHandle.ReleasePointer();
        }

        void Dispose(bool disposing)
        {
            if (!_disposed) {
                ReleaseUnmanagedResources();
                if (disposing) {
                    _mmf?.Dispose();
                    _mmva?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~KzMemoryMappedFile()
        {
            Dispose(false);
        }

        internal class MyInterop
        {
            internal class Kernel32
            {
                // See
                // https://github.com/dotnet/corefx/blob/master/src/Common/src/Interop/Windows/Interop.Libraries.cs
                // https://github.com/dotnet/corefx/blob/master/src/Common/src/Interop/Windows/kernel32/Interop.FlushViewOfFile.cs
                [DllImport("kernel32.dll", SetLastError = true)]
                internal extern static bool FlushViewOfFile(IntPtr lpBaseAddress, UIntPtr dwNumberOfBytesToFlush);

                // See
                // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/Interop/Windows/Kernel32/Interop.FlushFileBuffers.cs
                [DllImport("kernel32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                internal extern static bool FlushFileBuffers(SafeHandle hHandle);
            }

            internal static class Sys
            {
                // See 
                // https://github.com/dotnet/corefx/blob/master/src/Common/src/Interop/Unix/Interop.Libraries.cs
                // https://github.com/dotnet/corefx/blob/master/src/Common/src/Interop/Unix/System.Native/Interop.MSync.cs
                [Flags]
                internal enum MemoryMappedSyncFlags
                {
                    MS_ASYNC = 0x1,
                    MS_SYNC = 0x2,
                    MS_INVALIDATE = 0x10,
                }
                [DllImport("System.Native", EntryPoint = "SystemNative_MSync", SetLastError = true)]
                internal static extern int MSync(IntPtr addr, ulong len, MemoryMappedSyncFlags flags);
            }
        }
    }
}
