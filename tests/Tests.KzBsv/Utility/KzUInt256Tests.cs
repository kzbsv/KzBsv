#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using KzBsv;

namespace Tests.KzBsv
{

    public class KzUInt256Tests
    {
        [Fact]
        public void HexByteOrder()
        {
            var hex = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210";
            var fbf = new byte[] { 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10, 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10, 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10, 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10 };
            var fbl = fbf.Reverse().ToArray();

            var ifbl = new KzUInt256(hex, firstByteFirst: false);
            var ifbf = new KzUInt256(hex, firstByteFirst: true);
            Assert.Equal(ifbl.ReadOnlySpan.ToArray(), fbl);
            Assert.Equal(ifbf.ReadOnlySpan.ToArray(), fbf);

            Assert.Equal(hex, ifbl.ToString());
            Assert.Equal(hex, ifbf.ToStringFirstByteFirst());
        }

        [Fact]
        public void ByteAccess()
        {
            var i = new KzUInt256();
            var s = i.Span;
            s[0] = 0x21;
            s[31] = 0xfe;
            var str = i.ToString();
            Assert.Equal("fe00000000000000000000000000000000000000000000000000000000000021", str);
        }

        class SafetySmallObj
        {
            public UInt64 i1;
#pragma warning disable CS00649
            public UInt64 i2;
            public UInt64 i3;
            public UInt64 i4;
            public UInt64 i5;
#pragma warning restore CS00649
            public UInt64 a0;
            public UInt64 a1;

            public KzUInt256 l0;

            public SafetySmallObj()
            {
                unsafe {
                    fixed (UInt64* p = &i1) {
                        a0 = (UInt64)p;
                    }
                }
            }
            public bool HasMoved()
            {
                unsafe {
                    fixed (UInt64* p = &i1) {
                        a1 = (UInt64)p;
                        return a1 != a0;
                    }
                }
            }
        }

        /// <summary>
        /// Confirm that internal reference within Span providing bytewise access
        /// is properly remapped when GC relocates the instance.
        /// </summary>
        [Fact]
        public void SpanAccessSafety()
        {

            UInt64 GetAddress(ref KzUInt256 v)
            {
                unsafe {
                    fixed (KzUInt256* p = &v) {
                        return (UInt64)p;
                    }
                }
            }

            var hold = new List<SafetySmallObj>();

            while (true) {
                for (var i = 0; i < 1000; i++) {
                    var o1 = new SafetySmallObj();
                    var o2 = new SafetySmallObj();
                    hold.Add(o2);
                    o2.l0.n0 = (uint)hold.Count;
                    var o3 = new SafetySmallObj();
                    var o4 = new SafetySmallObj();
                    var o5 = new SafetySmallObj();
                }
                if (hold.Any(o => o.HasMoved())) {
                    var o = hold.First(t => t.HasMoved());
                    break;
                }
                var a0 = GetAddress(ref hold[0].l0);
                var span0 = hold[0].l0.Span;
                GC.Collect();
                var a1 = GetAddress(ref hold[0].l0);
                var span1 = hold[0].l0.Span;
                if (hold.Any(o => o.HasMoved())) {
                    var o = hold.First(t => t.HasMoved());
                    var c = hold.Count(t => t.HasMoved());
                    var i = 1u;
                    foreach (var ot in hold) {
                        Assert.True(ot.l0.n0 == i, "Unexpected initialization.");
                        var b = (byte)i;
                        Assert.True(ot.l0.Span[0] == b, "Span access failed.");
                        i++;
                    }
                    Assert.True(a0 != a1, "Test instance wasn't moved.");
                    Assert.True(span0[0] == span1[0] && span0[0] == 1, "Internal ref wasn't updated as expected.");
                    break;
                }
                Assert.True(hold.Count < 10000, "GC failed to relocate test objects.");
            }
        }
    }
}
