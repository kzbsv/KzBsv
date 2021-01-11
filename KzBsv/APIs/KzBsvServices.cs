using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KzBsv {

    /// <summary>
    /// This class is intended to keep applications independent of specific service APIs.
    /// Clients (applications) can configure an instance to use specific or default service implementations.
    /// This class encapsulates service provider selection logic, usage limits, and authentication. 
    /// For some APIs such as GetExchangeRate, averaging service responses may be preferred.
    /// For some APIs such as SendTransaction, one or mulitple providers may be preferred for fee rate or privacy reasons.
    /// </summary>
    public class KzBsvServices {
        // Providers
        KzApiWhatsOnChain _WhatsOnChain;
        List<KzMerchantClient> _MapiClients = new List<KzMerchantClient>();

        TimeSpan _MinimumExchangeRateAgeLimit = TimeSpan.FromSeconds(1.0);
        TimeSpan _DefaultExchangeRateAgeLimit = TimeSpan.FromMinutes(10.0);
        Mutex _UpdateMutexExchangeRateUSD = new Mutex();
        KzExchangeRate? _ExchangeRateUSD;

        public TimeSpan MinimumExchangeRateAgeLimit { get => _MinimumExchangeRateAgeLimit; set => _MinimumExchangeRateAgeLimit = value; }
        public TimeSpan DefaultExchangeRateAgeLimit { get => _DefaultExchangeRateAgeLimit; set => _DefaultExchangeRateAgeLimit = value; }

        public Func<KzExchangeRate, KzExchangeRate> LogNewExchangeRate { get; set; }

        ~KzBsvServices() {
            _UpdateMutexExchangeRateUSD.Dispose();
        }

        public KzBsvServices() {
        }

        public async Task<KzExchangeRate> GetExchangeRateUSD(TimeSpan? ageLimit = null) {

            ageLimit ??= _DefaultExchangeRateAgeLimit;
            if (ageLimit < _MinimumExchangeRateAgeLimit)
                ageLimit = _MinimumExchangeRateAgeLimit;

            if (!(_ExchangeRateUSD?.When + ageLimit > DateTime.UtcNow)) {

                if (_UpdateMutexExchangeRateUSD.WaitOne()) {
                    try {
                        if (!(_ExchangeRateUSD?.When + ageLimit > DateTime.UtcNow)) {
                            _WhatsOnChain ??= new KzApiWhatsOnChain();
                            var rate = await _WhatsOnChain.GetExchangeRate();
                            _ExchangeRateUSD = new KzExchangeRate {
                                OfTicker = KzCurrencyTicker.BSV,
                                ToTicker = KzCurrencyTicker.USD,
                                When = DateTime.UtcNow,
                                Rate = rate
                            };
                            if (LogNewExchangeRate != null)
                                _ExchangeRateUSD = LogNewExchangeRate?.Invoke(_ExchangeRateUSD);
                        }
                    } finally {
                        _UpdateMutexExchangeRateUSD.ReleaseMutex();
                    }
                }
            }
            return _ExchangeRateUSD;
        }

        public async Task<KzAmount> ComputeFee((long tx, long data) txLengths) {
            /// This method should allow for selection between multiple mapi providers,
            /// and cache fee quotations,
            /// and allow for selected fee quotation to tie to same mapi provider for sending transaction.
            var mapi = KzMerchantClient.GetClient();
            var feeQuote = await mapi.GetFeeQuote();
            var fee = KzMerchantClient.ComputeFee(txLengths, feeQuote.MiningRates);
            return fee;
        }
    }
}
