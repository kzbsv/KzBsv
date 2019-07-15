#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System.Globalization;

namespace KzBsv
{
    public struct KzAmount
    {
        /// <summary>
        /// long.MaxValue is 9_223_372_036_854_775_807
        /// max satoshis         2_100_000_000_000_000  (2.1 gigamegs :-)
        /// </summary>
        long _satoshis;

        public static KzAmount Zero = new KzAmount(0);

        public long Satoshis { get => _satoshis; private set => _satoshis = value; }

        public KzAmount(long satoshis) => _satoshis = satoshis;

        public KzAmount(ulong satoshis) { checked { _satoshis = (long)satoshis; } }

        /// <summary>
        /// decimal has 28-29 significant digts with a exponent range to shift that either
        /// all to the left of the decimal or to the right.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="unit"></param>
        public KzAmount(decimal amount, BitcoinUnit unit) { checked { _satoshis = (long)(amount * (long)unit); } }

        public KzAmount(long amount, BitcoinUnit unit) { checked { _satoshis = amount * (long)unit; } }

        public KzAmount(ulong amount, BitcoinUnit unit) { checked { _satoshis = (long)amount * (long)unit; } }

        public static implicit operator KzAmount(long value) => new KzAmount(value);
        public static implicit operator KzAmount(ulong value) => new KzAmount(checked((long)value));
        public static implicit operator long(KzAmount value) => value.Satoshis;
        public static implicit operator ulong(KzAmount value) => checked((ulong)value.Satoshis);

        public override string ToString()
        {
            var s = $"{_satoshis:000000}";
            return $"{s[..^5]}.{s[^5..]}mBSV";
        }
    }

    /// <summary>
    /// How many satoshis to each unit.
    /// </summary>
    public enum BitcoinUnit : long
    {
        BSV = 100000000,
        mBSV = 100000,
        Bit = 100,
        Satoshi = 1
    }
}
