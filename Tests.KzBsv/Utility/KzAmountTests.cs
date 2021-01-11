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

    public class KzAmountTests
    {
        [Fact]
        public void ToStringTest()
        {
            var tcs = new[] {
                new { g = true, units = false, unit = KzBitcoinUnit.BSV, v = 0L, s = " 0.000_000_00" },
                new { g = true, units = false, unit = KzBitcoinUnit.BSV, v = 1L, s = " 0.000_000_01" },
                new { g = true, units = false, unit = KzBitcoinUnit.BSV, v = -1L, s = "-0.000_000_01" },
                new { g = true, units = false, unit = KzBitcoinUnit.BSV, v = 123456789L, s = " 1.234_567_89" },
                new { g = true, units = false, unit = KzBitcoinUnit.BSV, v = -123456789L, s = "-1.234_567_89" },
                new { g = true, units = false, unit = KzBitcoinUnit.BSV, v = 2100000000000000L, s = " 21_000_000.000_000_00" },
                new { g = true, units = false, unit = KzBitcoinUnit.BSV, v = -2100000000000000L, s = "-21_000_000.000_000_00" },
                new { g = true, units = true, unit = KzBitcoinUnit.BSV, v = 0L, s = " 0.000_000_00 BSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.BSV, v = 1L, s = " 0.000_000_01 BSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.BSV, v = -1L, s = "-0.000_000_01 BSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.BSV, v = 123456789L, s = " 1.234_567_89 BSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.BSV, v = -123456789L, s = "-1.234_567_89 BSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.BSV, v = 2100000000000000L, s = " 21_000_000.000_000_00 BSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.BSV, v = -2100000000000000L, s = "-21_000_000.000_000_00 BSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.BSV, v = 0L, s = " 0.00000000 BSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.BSV, v = 1L, s = " 0.00000001 BSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.BSV, v = -1L, s = "-0.00000001 BSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.BSV, v = 123456789L, s = " 1.23456789 BSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.BSV, v = -123456789L, s = "-1.23456789 BSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.BSV, v = 2100000000000000L, s = " 21000000.00000000 BSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.BSV, v = -2100000000000000L, s = "-21000000.00000000 BSV" },

                new { g = true, units = false, unit = KzBitcoinUnit.mBSV, v = 0L, s = " 0.000_00" },
                new { g = true, units = false, unit = KzBitcoinUnit.mBSV, v = 1L, s = " 0.000_01" },
                new { g = true, units = false, unit = KzBitcoinUnit.mBSV, v = -1L, s = "-0.000_01" },
                new { g = true, units = false, unit = KzBitcoinUnit.mBSV, v = 123456789L, s = " 1_234.567_89" },
                new { g = true, units = false, unit = KzBitcoinUnit.mBSV, v = -123456789L, s = "-1_234.567_89" },
                new { g = true, units = false, unit = KzBitcoinUnit.mBSV, v = 2100000000000000L, s = " 21_000_000_000.000_00" },
                new { g = true, units = false, unit = KzBitcoinUnit.mBSV, v = -2100000000000000L, s = "-21_000_000_000.000_00" },
                new { g = true, units = true, unit = KzBitcoinUnit.mBSV, v = 0L, s = " 0.000_00 mBSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.mBSV, v = 1L, s = " 0.000_01 mBSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.mBSV, v = -1L, s = "-0.000_01 mBSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.mBSV, v = 123456789L, s = " 1_234.567_89 mBSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.mBSV, v = -123456789L, s = "-1_234.567_89 mBSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.mBSV, v = 2100000000000000L, s = " 21_000_000_000.000_00 mBSV" },
                new { g = true, units = true, unit = KzBitcoinUnit.mBSV, v = -2100000000000000L, s = "-21_000_000_000.000_00 mBSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.mBSV, v = 0L, s = " 0.00000 mBSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.mBSV, v = 1L, s = " 0.00001 mBSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.mBSV, v = -1L, s = "-0.00001 mBSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.mBSV, v = 123456789L, s = " 1234.56789 mBSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.mBSV, v = -123456789L, s = "-1234.56789 mBSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.mBSV, v = 2100000000000000L, s = " 21000000000.00000 mBSV" },
                new { g = false, units = true, unit = KzBitcoinUnit.mBSV, v = -2100000000000000L, s = "-21000000000.00000 mBSV" },

                new { g = true, units = false, unit = KzBitcoinUnit.Satoshi, v = 0L, s = " 0" },
                new { g = true, units = false, unit = KzBitcoinUnit.Satoshi, v = 1L, s = " 1" },
                new { g = true, units = false, unit = KzBitcoinUnit.Satoshi, v = -1L, s = "-1" },
                new { g = true, units = false, unit = KzBitcoinUnit.Satoshi, v = 123456789L, s = " 123_456_789" },
                new { g = true, units = false, unit = KzBitcoinUnit.Satoshi, v = -123456789L, s = "-123_456_789" },
                new { g = true, units = false, unit = KzBitcoinUnit.Satoshi, v = 2100000000000000L, s = " 2_100_000_000_000_000" },
                new { g = true, units = false, unit = KzBitcoinUnit.Satoshi, v = -2100000000000000L, s = "-2_100_000_000_000_000" },
                new { g = true, units = true, unit = KzBitcoinUnit.Satoshi, v = 0L, s = " 0 Satoshi" },
                new { g = true, units = true, unit = KzBitcoinUnit.Satoshi, v = 1L, s = " 1 Satoshi" },
                new { g = true, units = true, unit = KzBitcoinUnit.Satoshi, v = -1L, s = "-1 Satoshi" },
                new { g = true, units = true, unit = KzBitcoinUnit.Satoshi, v = 123456789L, s = " 123_456_789 Satoshi" },
                new { g = true, units = true, unit = KzBitcoinUnit.Satoshi, v = -123456789L, s = "-123_456_789 Satoshi" },
                new { g = true, units = true, unit = KzBitcoinUnit.Satoshi, v = 2100000000000000L, s = " 2_100_000_000_000_000 Satoshi" },
                new { g = true, units = true, unit = KzBitcoinUnit.Satoshi, v = -2100000000000000L, s = "-2_100_000_000_000_000 Satoshi" },
                new { g = false, units = true, unit = KzBitcoinUnit.Satoshi, v = 0L, s = " 0 Satoshi" },
                new { g = false, units = true, unit = KzBitcoinUnit.Satoshi, v = 1L, s = " 1 Satoshi" },
                new { g = false, units = true, unit = KzBitcoinUnit.Satoshi, v = -1L, s = "-1 Satoshi" },
                new { g = false, units = true, unit = KzBitcoinUnit.Satoshi, v = 123456789L, s = " 123456789 Satoshi" },
                new { g = false, units = true, unit = KzBitcoinUnit.Satoshi, v = -123456789L, s = "-123456789 Satoshi" },
                new { g = false, units = true, unit = KzBitcoinUnit.Satoshi, v = 2100000000000000L, s = " 2100000000000000 Satoshi" },
                new { g = false, units = true, unit = KzBitcoinUnit.Satoshi, v = -2100000000000000L, s = "-2100000000000000 Satoshi" },
            };

            foreach (var tc in tcs) {
                var v = tc.v.ToKzAmount();
                var s = v.ToString(group: tc.g, units: tc.units, unit: tc.unit);
                Assert.Equal(tc.s, s);
            }
        }
    }
}
