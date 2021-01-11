#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Xunit;
using KzBsv;
using System;

namespace Tests.KzBsv.Extensions {
    public class KzBsvExtensionsTests {
        [Fact]
        public void ToInt32() {
            var bytes = new byte[] { 0xAC, 0x1E, 0xED, 0x88 }.AsSpan();

            var le = bytes.ToInt32LittleEndian();
            var be = bytes.ToInt32BigEndian();

            Assert.Equal(unchecked((Int32)(0x88ED1EAC)), le);
            Assert.Equal(unchecked((Int32)(0xAC1EED88)), be);
        }
    }
}
