#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.IO;
using Xunit;
using KzBsv;

namespace Tests.KzBsv
{
    public class KzEncodeTests
    {
        [Fact]
        public void Hex()
        {
            var hex = KzEncoders.Hex;

            var b0 = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0 };
            var s0 = "123456789abcdef0";
            var s0u = "123456789ABCDEF0";

            Assert.Equal(hex.Encode(b0), s0);
            Assert.Equal(hex.Decode(s0), b0);
            Assert.Equal(hex.Decode(s0u), b0);

            var hexr = KzEncoders.HexReverse;

            var b0r = new byte[] { 0x10, 0x32, 0x54, 0x76, 0x98, 0xba, 0xdc, 0xfe };
            var s0r = "fedcba9876543210";
            Assert.Equal(hexr.Encode(b0r), s0r);
            Assert.Equal(hexr.Decode(s0r), b0r);
        }
    }
}
