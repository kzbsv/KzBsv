#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace KzBsv {
    public interface IKzAmountExchangeRate {
        public decimal ConvertFromAmount(KzAmount value);
        public KzAmount ConvertToAmount(decimal value);
        //public DateTime AsOfWhen { get; }
        //public void UpdateExchangeRate();
    }

    public struct KzAmount : IComparable<KzAmount>, IComparable
    {
        public static IKzAmountExchangeRate ExchangeRate { get; set; }

        /// <summary>
        /// long.MaxValue is 9_223_372_036_854_775_807
        /// max satoshis         2_100_000_000_000_000  (2.1 gigamegs :-)
        /// </summary>
        long _satoshis;

        public static KzAmount Zero = new KzAmount(0L);
        /// <summary>
        /// This is a value slightly higher than the maximum number of satoshis that will ever be in circulation: 21 million coins, 2.1 gigameg satoshis.
        /// 2_100_000_000_000_000
        /// </summary>
        public static KzAmount MaxValue = new KzAmount(2_100_000_000_000_000);
        /// <summary>
        /// This is the negated value slightly higher than the maximum number of satoshis that will ever be in circulation: -21 million coins, -2.1 gigameg satoshis.
        /// -2_100_000_000_000_000
        /// </summary>
        public static KzAmount MinValue = new KzAmount(-2_100_000_000_000_000);

        public long Satoshis { get => _satoshis; private set => _satoshis = value; }

        public KzAmount(long satoshis) => _satoshis = satoshis;

        public KzAmount(ulong satoshis) { checked { _satoshis = (long)satoshis; } }

        public static KzAmount? TryParse(string text, KzBitcoinUnit unit) {
            return decimal.TryParse(text.Replace("_", ""), out var value) ? new KzAmount(value, unit) : (KzAmount?)null;
        }

        /// <summary>
        /// decimal has 28-29 significant digts with a exponent range to shift that either
        /// all to the left of the decimal or to the right.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="unit"></param>
        public KzAmount(decimal amount, KzBitcoinUnit unit) { checked { _satoshis = (long)(amount * (long)unit); } }

        public KzAmount(long amount, KzBitcoinUnit unit) { checked { _satoshis = amount * (long)unit; } }

        public KzAmount(ulong amount, KzBitcoinUnit unit) { checked { _satoshis = (long)amount * (long)unit; } }

        public static implicit operator KzAmount(long value) => new KzAmount(value);
        public static implicit operator KzAmount(ulong value) => new KzAmount(checked((long)value));
        public static implicit operator long(KzAmount value) => value.Satoshis;
        public static implicit operator ulong(KzAmount value) => checked((ulong)value.Satoshis);

        public override string ToString() => ToString(group: true, units: false, unit: KzBitcoinUnit.mBSV);

        public decimal ToBSV() => (decimal)Satoshis / (long)(KzBitcoinUnit.BSV);

        public string ToString(bool group, bool units, KzBitcoinUnit unit = KzBitcoinUnit.mBSV)
        {
            // Satoshis
            // 2_100_000_000_000_000
            // mBSV
            // 21_000_000_000.000_00
            // BSV
            // 21_000_000.000_000_00
            var s = _satoshis;
            var m = false;
            if (s < 0) {
                m = true;
                s = -s;
            }
            var f = s % (long)unit;
            var i = s / (long)unit;
            var r = unit switch {
             KzBitcoinUnit.BSV     => $"{(m ? "-" : " ")}{i:#,0}.{f:000_000_00}",
             KzBitcoinUnit.mBSV    => $"{(m ? "-" : " ")}{i:#,0}.{f:000_00}",
             KzBitcoinUnit.Bit     => $"{(m ? "-" : " ")}{i:#,0}.{f:00}",
             KzBitcoinUnit.Satoshi => $"{(m ? "-" : " ")}{i:#,0}",
             _ => throw new NotImplementedException()
            };
            r = r.Replace(',', '_');
            if (!group) r = r.Replace("_", "");
            if (units) r = r += $" {unit}";
            return r;
        }

        public static string ToString(long value) => new KzAmount(value).ToString();

        public override int GetHashCode() => _satoshis.GetHashCode();
        public override bool Equals(object obj) => obj is KzAmount && this == (KzAmount)obj;
        public bool Equals(KzAmount o) => _satoshis == o._satoshis;
        public static bool operator ==(KzAmount x, KzAmount y) => x.Equals(y);
        public static bool operator !=(KzAmount x, KzAmount y) => !(x == y);

        public static bool operator >(KzAmount x, KzAmount y) => x.CompareTo(y) > 0;
        public static bool operator <(KzAmount x, KzAmount y) => x.CompareTo(y) < 0;

        public static bool operator >=(KzAmount x, KzAmount y) => x.CompareTo(y) >= 0;
        public static bool operator <=(KzAmount x, KzAmount y) => x.CompareTo(y) <= 0;

        public int CompareTo([AllowNull] KzAmount other) => Satoshis.CompareTo(other.Satoshis);

        public int CompareTo(object obj) {
            return obj switch
            {
                KzAmount a => CompareTo(a),
                long l => Satoshis.CompareTo(l),
                ulong ul => Satoshis.CompareTo(ul),
                int i => Satoshis.CompareTo(i),
                uint ui => Satoshis.CompareTo(ui),
                _ => throw new NotImplementedException()
            };
        }

        public static KzAmount operator -(KzAmount a, KzAmount b) => a + -b;
        public static KzAmount operator +(KzAmount a, long b) => a + new KzAmount(b);
        public static KzAmount operator +(KzAmount a, KzAmount b) => (a.Satoshis + b.Satoshis).ToKzAmount();
        public static KzAmount operator -(KzAmount a) => (-a.Satoshis).ToKzAmount();
    }
}
