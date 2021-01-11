#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace KzBsv {
    public class KzApiCoinMarketCap {

        string API_KEY = "b54bcf4d-1bca-4e8e-9a24-22ff2c3d462c";

        HttpClient _HttpClient;

        public KzApiCoinMarketCap(string api_key) {
            API_KEY = api_key;

            _HttpClient = new HttpClient();
            _HttpClient.DefaultRequestHeaders.Accept.Clear();
            _HttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _HttpClient.DefaultRequestHeaders.Add("User-Agent", "KzApiCoinMarketCap");
            _HttpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", api_key);
        }

        public async Task<string> LatestListings() {
            var url = $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest?start=1&limit=5000&convert=USD";

            var json = await _HttpClient.GetStringAsync(url);
            /*
            {
                "status": {
                    "timestamp": "2019-12-09T20:52:15.002Z",
                    "error_code": 0,
                    "error_message": null,
                    "elapsed": 217,
                    "credit_count": 12,
                    "notice": null
                },
                "data": [
                    {
                        "id": 1,
                        "name": "Bitcoin",
                        "symbol": "BTC",
                        "slug": "bitcoin",
                        "num_market_pairs": 7721,
                        "date_added": "2013-04-28T00:00:00.000Z",
                        "tags": [
                            "mineable"
                        ],
                        "max_supply": 21000000,
                        "circulating_supply": 18092737,
                        "total_supply": 18092737,
                        "platform": null,
                        "cmc_rank": 1,
                        "last_updated": "2019-12-09T20:51:37.000Z",
                        "quote": {
                            "USD": {
                                "price": 7400.54230927,
                                "volume_24h": 17531781840.1686,
                                "percent_change_1h": -0.685321,
                                "percent_change_24h": -2.09101,
                                "percent_change_7d": 1.02466,
                                "market_cap": 133896065658.99478,
                                "last_updated": "2019-12-09T20:51:37.000Z"
                            }
                        }
                    },
                    {
                        "id": 1027,
                        "name": "Ethereum",
                        "symbol": "ETH",
                        "slug": "ethereum",
                        "num_market_pairs": 5268,
                        "date_added": "2015-08-07T00:00:00.000Z",
                        "tags": [
                            "mineable"
                        ],
                        "max_supply": null,
                        "circulating_supply": 108857249.624,
                        "total_supply": 108857249.624,
                        "platform": null,
                        "cmc_rank": 2,
                        "last_updated": "2019-12-09T20:51:23.000Z",
                        "quote": {
                            "USD": {
                                "price": 148.420503045,
                                "volume_24h": 6773088025.36599,
                                "percent_change_1h": -0.0833446,
                                "percent_change_24h": -1.85883,
                                "percent_change_7d": -0.134673,
                                "market_cap": 16156647749.289217,
                                "last_updated": "2019-12-09T20:51:23.000Z"
                            }
                        }
                    }
                ]
            } */
            return json;
        }

    }
}
