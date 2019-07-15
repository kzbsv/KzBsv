#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using Xunit;
using KzBsv;

namespace Tests.KzBsv
{
    public class KzReadOnlySequenceTests
    {
        [Fact]
        public void TestRemoveSlice()
        {
            var a = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var ros = new ReadOnlySequence<byte>(a);
            Assert.Equal(new byte[] { 1, 2 }, ros.RemoveSlice(2, 12).ToArray());
            Assert.Equal(new byte[] { 11, 12 }, ros.RemoveSlice(0, 10).ToArray());
            Assert.Equal(new byte[] { 1, 12 }, ros.RemoveSlice(1, 11).ToArray());
            var r1 = ros.RemoveSlice(1, 5);
            var r2 = r1.RemoveSlice(3, 7);
            Assert.Equal(new byte[] { 1, 6, 7, 12 }, r2.ToArray());
        }
    }
}
