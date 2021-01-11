#region Copyright
// Copyright (c) 2021 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;

namespace KzBsv {

    /// <summary>
    /// In practice, the value of an amount is often required in terms of a non-Bitcoin fiat or foreign currency.
    /// There are three quantities
    /// </summary>
    public class KzAmountV {
        Values _SetOrder;
        KzAmount _Amount;
        KzCurrencyTicker _FiatTicker;
        decimal _FiatValue;
        KzExchangeRate _Rate;

        public bool HasAll => _SetOrder > Values.R;
        public bool HasAmount => _SetOrder > Values.R || _SetOrder == Values.S;
        public bool HasRate => _SetOrder > Values.R || _SetOrder == Values.R;
        public bool HasFiat => _SetOrder > Values.R || _SetOrder == Values.F;

        public bool HasComputedAmount => _SetOrder == Values.FR || _SetOrder == Values.RF || _SetOrder == Values.ZF;
        public bool HasComputedFiat => _SetOrder == Values.RS || _SetOrder == Values.SR || _SetOrder == Values.ZS;
        public bool HasComputedRate => _SetOrder == Values.FS || _SetOrder == Values.SF;

        public bool HasSetAmount => _SetOrder == Values.S || _SetOrder == Values.SR || _SetOrder == Values.SF || _SetOrder == Values.RS || _SetOrder == Values.FS || _SetOrder == Values.ZS;
        public bool HasSetFiat => _SetOrder == Values.F || _SetOrder == Values.FR || _SetOrder == Values.FS || _SetOrder == Values.RF || _SetOrder == Values.SF || _SetOrder == Values.ZF;
        public bool HasSetRate => _SetOrder == Values.R || _SetOrder == Values.RS || _SetOrder == Values.RF || _SetOrder == Values.SR || _SetOrder == Values.FR;

        public Values ValueSetOrder { get => _SetOrder; set => _SetOrder = value; }

        public KzAmount? Amount { get => HasAmount ? _Amount : (KzAmount?)null; set => _Amount = value ?? KzAmount.Zero; }

        public long? Satoshis => HasAmount ? _Amount.Satoshis : (long?)null;

        public KzExchangeRate Rate { get => HasRate ? _Rate : null; set => _Rate = value; }

        public KzCurrencyTicker FiatTicker { get => _FiatTicker; set => _FiatTicker = value; }

        public decimal? FiatValue { get => HasFiat ? _FiatValue : (decimal?)null; set => _FiatValue = value ?? decimal.Zero; }

        public KzAmountV() { ResetValue(); }

        public KzAmountV(KzAmount amount) {
            ResetValue();
            SetAmount(amount);
        }

        public KzAmountV(KzExchangeRate rate, decimal fiatValue) {
            ResetValue();
            SetRate(rate);
            SetFiatValue(fiatValue);
        }

        public KzAmountV(KzAmount amount, KzCurrencyTicker fiatTicker, decimal fiatValue) {
            ResetValue();
            SetAmount(amount);
            SetFiatTicker(fiatTicker);
            SetFiatValue(fiatValue);
        }

        public void ResetValue() {
            // Update to pull default from gloval preferences.
            var defaultTicker = KzCurrencyTicker.USD;
            (_SetOrder, _Amount, _FiatTicker, _FiatValue, _Rate) = (Values.None, KzAmount.Zero, defaultTicker, decimal.Zero, null);
        }

        /// <summary>
        /// Some sequences of set values are sufficient to fully constrain the least recently set value.
        /// </summary>
        void UpdateConstrainedValues() {
            switch (_SetOrder) {
                case Values.None:
                case Values.S:
                case Values.F:
                case Values.R:
                    // Nothing to update if less than two values have been set.
                    break;
                case Values.SF:
                case Values.FS:
                    // Satoshis (Value) and Fiat (ToValue,ToTicker) are set, check and compute ExchangeRate
                    _Rate = new KzExchangeRate {
                        OfTicker = KzCurrencyTicker.BSV,
                        ToTicker = _FiatTicker,
                        When = DateTime.UtcNow,
                        Rate = _FiatValue / _Amount.ToBSV()
                    };
                    break;
                case Values.SR:
                case Values.RS:
                    // Satoshis and ExchangeRate are set, check and compute Fiat (ToValue,ToTicker)
                    (_FiatValue, _FiatTicker) = (_Rate.ConvertOfValue(_Amount), _Rate.ToTicker);
                    break;
                case Values.FR:
                case Values.RF:
                    // Fiat (ToValue,ToTicker) and ExchangeRate are set, check and compute Satoshis (Value)
                    _Amount = new KzAmount(Math.Round(_Rate.ConvertToValue(_FiatValue), 8, MidpointRounding.AwayFromZero), KzBitcoinUnit.BSV);
                    break;
                case Values.ZS:
                    _FiatValue = decimal.Zero;
                    break;
                case Values.ZF:
                    _Amount = KzAmount.Zero;
                    break;
                default: throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Set a specific bitcoin amount.
        /// The amount must be in the range <see cref="KzAmount.MinValue"/> to <see cref="KzAmount.MaxValue"/>.
        /// If the amount is zero, it constrains the Fiat value to be zero as well, but leaves Rate as it was.
        /// </summary>
        /// <param name="amount"></param>
        public void SetAmount(KzAmount? amount) {
            if (amount.HasValue) {
                if (amount > KzAmount.MaxValue)
                    throw new ArgumentException("Maximum value exceeded.");
                if (amount < KzAmount.MinValue)
                    throw new ArgumentException("Minimum value exceeded.");
                _Amount = amount.Value;
                // Update _SetOrder to reflect a new Satoshi value.
                var isZero = _Amount == KzAmount.Zero;
                switch (_SetOrder) {
                    case Values.None:
                    case Values.S:
                    case Values.ZS:
                    case Values.ZF:
                        _SetOrder = isZero ? Values.ZS : Values.S;
                        break;
                    case Values.F:
                    case Values.SF:
                    case Values.RF:
                    case Values.FS:
                        _SetOrder = isZero ? Values.ZS : Values.FS;
                        break;
                    case Values.R:
                    case Values.SR:
                    case Values.FR:
                    case Values.RS:
                        _SetOrder = Values.RS;
                        break;
                    default: throw new NotImplementedException();
                }
            } else {
                _Amount = KzAmount.Zero;
                // Update _SetOrder to reflect the loss of Amount Satoshis.
                switch (_SetOrder) {
                    case Values.None:
                    case Values.S:
                    case Values.ZS:
                        _SetOrder = Values.None;
                        break;
                    case Values.F:
                    case Values.R:
                    case Values.RF:
                    case Values.FR:
                    case Values.ZF:
                        // No change.
                        break;
                    case Values.FS:
                    case Values.SF:
                        _SetOrder = Values.F;
                        break;
                    case Values.SR:
                    case Values.RS:
                        _SetOrder = Values.R;
                        break;
                    default: throw new NotImplementedException();
                }
            }
            UpdateConstrainedValues();
        }

        public void SetFiatTicker(KzCurrencyTicker fiatTicker) {
            _FiatTicker = fiatTicker;
        }

        /// <summary>
        /// Set a specific fiat or foreign currency value, or clears a previously set value.
        /// If the fiat value is zero, it constrains the bitcoin amount to be zero as well, but leaves Rate as it was.
        /// If the fiat value is null, clears fiat constraints on value.
        /// </summary>
        /// <param name="fiat"></param>
        public void SetFiatValue(decimal? fiatValue) {
            if (fiatValue.HasValue) {
                _FiatValue = fiatValue.Value;
                // Update _SetOrder to reflect a new Fiat/Foreign value.
                var isZero = _FiatValue == decimal.Zero;
                switch (_SetOrder) {
                    case Values.None:
                    case Values.F:
                    case Values.ZS:
                    case Values.ZF:
                        _SetOrder = isZero ? Values.ZF : Values.F;
                        break;
                    case Values.S:
                    case Values.FS:
                    case Values.RS:
                    case Values.SF:
                        _SetOrder = isZero ? Values.ZF : Values.SF;
                        break;
                    case Values.R:
                    case Values.SR:
                    case Values.FR:
                    case Values.RF:
                        _SetOrder = Values.RF;
                        break;
                    default: throw new NotImplementedException();
                }
            } else {
                // Retain the ToTicker as the best default even when clearing value.
                _FiatValue = decimal.Zero;
                // Update _SetOrder to reflect the loss of Fiat/Foreign value.
                switch (_SetOrder) {
                    case Values.None:
                    case Values.F:
                    case Values.ZF:
                        _SetOrder = Values.None;
                        break;
                    case Values.S:
                    case Values.R:
                    case Values.RS:
                    case Values.SR:
                    case Values.ZS:
                        // No change.
                        break;
                    case Values.FS:
                    case Values.SF:
                        _SetOrder = Values.S;
                        break;
                    case Values.FR:
                    case Values.RF:
                        _SetOrder = Values.R;
                        break;
                    default: throw new NotImplementedException();
                }
            }
            UpdateConstrainedValues();
        }

        /// <summary>
        /// Set a specific exchange rate, or clears a previously set value.
        /// A zero exchange rate is treated as a null value, clearing exchange rate constraints.
        /// </summary>
        /// <param name="rate"></param>
        public void SetRate(KzExchangeRate rate) {
            rate.CheckOfTickerIsBSV();
            if (rate != null && rate.Rate == decimal.Zero)
                rate = null;
            _Rate = rate;
            if (rate != null) {
                // Update _SetOrder to reflect a new exchange Rate value.
                switch (_SetOrder) {
                    case Values.None:
                    case Values.R:
                        _SetOrder = Values.R;
                        break;
                    case Values.S:
                    case Values.FS:
                    case Values.RS:
                    case Values.SR:
                    case Values.ZS:
                        _SetOrder = Values.SR;
                        break;
                    case Values.F:
                    case Values.SF:
                    case Values.RF:
                    case Values.FR:
                    case Values.ZF:
                        _SetOrder = Values.FR;
                        break;
                    default: throw new NotImplementedException();
                }
            } else {
                // Update _SetOrder to reflect the loss of exchange Rate value.
                switch (_SetOrder) {
                    case Values.None:
                    case Values.R:
                        _SetOrder = Values.None;
                        break;
                    case Values.S:
                    case Values.F:
                    case Values.SF:
                    case Values.FS:
                        // No change.
                        break;
                    case Values.SR:
                    case Values.RS:
                        _SetOrder = _Amount == KzAmount.Zero ? Values.ZS : Values.S;
                        break;
                    case Values.RF:
                    case Values.FR:
                        _SetOrder = _FiatValue == decimal.Zero ? Values.ZF : Values.F;
                        break;
                    default: throw new NotImplementedException();
                }
            }
            UpdateConstrainedValues();
        }

        public static implicit operator KzAmountV(KzAmount value) => new KzAmountV(value);

        /// <summary>
        /// Used to record how a transaction output value is constrained.
        /// Supports specifying the value by any valid combination of:
        /// <list type="table">
        /// <item><term>S</term><description><see cref="KzAmount"/> value in satoshis.</description></item>
        /// <item><term>F</term><description>(ToValue,ToTicker) Fiat or Foreign value.</description></item>
        /// <item><term>R</term><description>BSV to Fiat or Foreign exchange Rate.</description></item>
        /// </list>
        /// <para>For user interface support, the order in which these are specified
        /// can be tracked to support automatic consistency by knowing which
        /// value to compute from the two values most recently specified.</para>
        /// <para>The word Fiat here also includes Foreign currency and non-BSV digital assets.</para>
        /// </summary>
        public enum Values : byte {
            /// <summary>
            /// No value constraints have been set.
            /// This does not mean the value is unknown, only that set order consistency isn't used.
            /// </summary>
            None = 00,
            /// <summary>
            /// Only non-zero Satoshi value has been set. Fiat value and exchange Rate are unknown. Valid transaction output value.
            /// </summary>
            S = 01,
            /// <summary>
            /// Only non-zero Fiat value has been set. Satoshi value and exchange Rate are unknown. Invalid transaction output value.
            /// </summary>
            F = 02,
            /// <summary>
            /// Only non-zero exchange rate has been set. Satoshi value and Fiat value are unknown. Invalid transaction output value.
            /// </summary>
            R = 03,
            /// <summary>
            /// Satoshi value, then Fiat value were set. Exchange Rate was computed from them. Valid transaction output value.
            /// </summary>
            SF = 12,
            /// <summary>
            /// Satoshi value, then exchange Rate were set. Fiat value was computed from them. Valid transaction output value.
            /// </summary>
            SR = 13,
            /// <summary>
            /// Fiat value, then Satoshi value were set. Exchange Rate was computed from them. Valid transaction output value.
            /// </summary>
            FS = 21,
            /// <summary>
            /// Fiat value, then exchange Rate were set. Satoshi value was computed from them. Valid transaction output value.
            /// </summary>
            FR = 23,
            /// <summary>
            /// Exchange Rate, then Satoshi value were set. Fiat value was computed from them. Valid transaction output value.
            /// </summary>
            RS = 31,
            /// <summary>
            /// Exchange Rate, then Fiat value were set. Satoshi value was computed from them. Valid transaction output value.
            /// </summary>
            RF = 32,
            /// <summary>
            ///  A zero Satoshi value has been set. Fiat value is zero and exchange Rate is null. Valid transaction output value.
            /// </summary>
            ZS = 41,
            /// <summary>
            ///  A zero Fiat value has been set. Satoshi value is zero and exchange Rate is null. Valid transaction output value.
            /// </summary>
            ZF = 42
        }

    }
}
