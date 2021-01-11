#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using KzBsv;

namespace Tests.KzBsv.Utility
{

    public class KzUInt256Tests
    {
        [Fact]
        public void Division() {
            var tcs = new[] {
                new {
                    a = "0000000000000000000000000000000000000000000000000000000000000008",
                    b = "0000000000000000000000000000000000000000000000000000000000000004",
                    r = "0000000000000000000000000000000000000000000000000000000000000002"
                },
                new {
                    a = "0000000000000000000000000000000000000000000000000800000000000000",
                    b = "0000000000000000000000000000000000000000000000000000000000040000",
                    r = "0000000000000000000000000000000000000000000000000000020000000000"
                },
                new {
                    a = "7D1DE5EAF9B156D53208F033B5AA8122D2d2355d5e12292b121156cfdb4a529c",
                    b = "00000000000000000000000000000000000000000000000AD7133AC1977FA2B7",
                    r = "00000000000000000b8ac01106981635d9ed112290f8895545a7654dde28fb3a"
                },
            };
            foreach (var tc in tcs) {
                var a = tc.a.ToKzUInt256();
                var b = tc.b.ToKzUInt256();
                var r = tc.r.ToKzUInt256();
                var t = a / b;
                Assert.Equal(r, t);
            }

            var R1L = "7D1DE5EAF9B156D53208F033B5AA8122D2d2355d5e12292b121156cfdb4a529c".ToKzUInt256();
            var D1L = "00000000000000000000000000000000000000000000000AD7133AC1977FA2B7".ToKzUInt256();
            var D2L = "0000000000000000000000000000000000000000000000000000000ECD751716".ToKzUInt256();
            var ZeroL = "0000000000000000000000000000000000000000000000000000000000000000".ToKzUInt256();
            var OneL = "0000000000000000000000000000000000000000000000000000000000000001".ToKzUInt256();
            var MaxL = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff".ToKzUInt256();

            Assert.Equal("00000000000000000b8ac01106981635d9ed112290f8895545a7654dde28fb3a".ToKzUInt256(), R1L / D1L);
            Assert.Equal("000000000873ce8efec5b67150bad3aa8c5fcb70e947586153bf2cec7c37c57a".ToKzUInt256(), R1L / D2L);
            Assert.Equal(R1L, R1L / OneL);
            Assert.Equal(ZeroL, R1L / MaxL);
            Assert.Equal(new KzUInt256(2), MaxL / R1L);

#if false
            BOOST_CHECK(R1L / OneL == R1L);
            BOOST_CHECK(R1L / MaxL == ZeroL);
            BOOST_CHECK(MaxL / R1L == 2);
            BOOST_CHECK_THROW(R1L / ZeroL, uint_error);
            BOOST_CHECK(
                (R2L / D1L).ToString() ==
                "000000000000000013e1665895a1cc981de6d93670105a6b3ec3b73141b3a3c5");
            BOOST_CHECK(
                (R2L / D2L).ToString() ==
                "000000000e8f0abe753bb0afe2e9437ee85d280be60882cf0bd1aaf7fa3cc2c4");
            BOOST_CHECK(R2L / OneL == R2L);
            BOOST_CHECK(R2L / MaxL == ZeroL);
            BOOST_CHECK(MaxL / R2L == 1);
            BOOST_CHECK_THROW(R2L / ZeroL, uint_error);
#endif
        }

        [Fact]
        public void LeftShift() {
            var a = new KzUInt256(1);
            Assert.Equal(new KzUInt256(1, 0, 0, 0), a);
            Assert.Equal(new KzUInt256(0, 1, 0, 0), a << 64);
            Assert.Equal(new KzUInt256(0, 4, 0, 0), a << 66);
            Assert.Equal(new KzUInt256(0, 0, 1, 0), a << (64 + 64));
            Assert.Equal(new KzUInt256(0, 0, 4, 0), a << (66 + 64));
            Assert.Equal(new KzUInt256(0, 0, 0, 1), a << (64 + 64 + 64));
            Assert.Equal(new KzUInt256(0, 0, 0, 4), a << (66 + 64 + 64));
        }

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
#pragma warning disable CS0649
            public UInt64 i2;
            public UInt64 i3;
            public UInt64 i4;
            public UInt64 i5;
#pragma warning restore CS0649
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
