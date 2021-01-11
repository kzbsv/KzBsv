#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KzBsv {

    public class KzApiWhatsOnChain
    {
        HttpClient _HttpClient;

        public class ByAddressUnspent
        {
            public int height;
            public int tx_pos;
            public string tx_hash;
            public long value;
        }

        public KzApiWhatsOnChain()
        {
            _HttpClient = new HttpClient();
            _HttpClient.DefaultRequestHeaders.Accept.Clear();
            _HttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _HttpClient.DefaultRequestHeaders.Add("User-Agent", "KzApiWhatsOnChain");
        }

        async public Task<List<ByAddressUnspent>> GetUnspentTransactionsByAddress(string address)
        {
            var url = $"https://api.whatsonchain.com/v1/bsv/{Kz.Params.strNetworkID}/address/{address}/unspent";

            var json = await _HttpClient.GetStringAsync(url);

            var unspent = JsonConvert.DeserializeObject<List<ByAddressUnspent>>(json);

            return unspent;
        }

        public class ScriptSig
        {
            public string asm;
            public string hex;
        }

        public class Vin
        {
            public string txid;
            public int vout;
            public ScriptSig scriptSig;
            public uint sequence;
            public string coinbase;
        }

        public class ScriptPubKey
        {
            public string asm;
            public string hex;
            public int reqSigs;
            public int type;
            public string[] addresses;
            public string opReturn;
        }
        public class Vout
        {
            public decimal value;
            public int n;
            public string txid;
            public int vout;
            public ScriptSig scriptSig;
            public uint sequence;
            public string coinbase;
        }

        public class Transaction
        {
            public string hex;
            public string txid;
            public string hash;
            public int size;
            public int version;
            public uint locktime;
            public Vin[] vin;
            public Vout[] vout;
            public string blockhash;
            public int confirmations;
            public long time;
            public long blocktime;
        }

        public class ExchangeRate
        {
            public string currency;
            public decimal rate;
        }

        async public Task<KzTransaction> GetTransactionsByHash(KzUInt256 txId)
        {
            var url = $"https://api.whatsonchain.com/v1/bsv/{Kz.Params.strNetworkID}/tx/hash/{txId}";

            var json = await _HttpClient.GetStringAsync(url);

            var woctx = JsonConvert.DeserializeObject<Transaction>(json);

            var tx = new KzTransaction();
            var ros = new ReadOnlySequence<byte>(woctx.hex.HexToBytes());
            if (!tx.TryReadTransaction(ref ros))
                tx = null;
            return tx;
        }

        async public Task<decimal> GetExchangeRate()
        {
            var url = $"https://api.whatsonchain.com/v1/bsv/{Kz.Params.strNetworkID}/exchangerate";

            var json = await _HttpClient.GetStringAsync(url);

            // json == {"currency":"USD","rate":"174.04999999999998"}

            var er = JsonConvert.DeserializeObject<ExchangeRate>(json);

            return er.rate;
        }
    }
}
