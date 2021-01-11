#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using DnsClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using KzBsv;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

namespace KzBsv
{
    /// <summary>
    /// This class implements the client side of the Paymail / BsvAlias protocol.
    /// 
    /// The capabilities of a given Paymail server implementation are retrieved and cached for each domain accessed.
    /// This is typically handled automatically when using the end purpose API methods: GetPubKey, GetOutputScript
    /// </summary>
    public class KzPaymailClient
    {
        HttpClient _HttpClient;

        object _cacheLock = new object();
        Dictionary<string, CapabilitiesResponse> _cache = new Dictionary<string, CapabilitiesResponse>();

        bool CacheTryGetValue(string domain, out CapabilitiesResponse ba)
        {
            lock (_cacheLock)
            {
                return _cache.TryGetValue(domain, out ba);
            }
        }

        void CacheUpdateValue(string domain, CapabilitiesResponse ba)
        {
            lock (_cacheLock)
            {
                _cache[domain] = ba;
            }
        }

        public KzPaymailClient()
        {
            _HttpClient = new HttpClient();
            _HttpClient.DefaultRequestHeaders.Accept.Clear();
            _HttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _HttpClient.DefaultRequestHeaders.Add("User-Agent", "KzPaymailClient");
        }

        public async Task<bool> DomainHasCapability(string domain, KzPaymail.Capability capability)
        {
            var id = KzPaymail.ToBrfcId(capability);
            var ba = await GetApiDescriptionFor(domain);
            if (ba == null || !ba.capabilities.ContainsKey(id))
                return false;
            var v = ba.capabilities[id].Value;
            return !v.Equals(false);
        }

        public async Task<KzPubKey> GetPubKey(string receiverHandle)
        {
            var uri = await GetIdentityUrl(receiverHandle);

            var r = await _HttpClient.GetAsync(uri);

            if (r.StatusCode == HttpStatusCode.OK)
            {
                var json = await r.Content.ReadAsStringAsync();
                var gpkr = JsonConvert.DeserializeObject<GetPubKeyResponse>(json);
                var pubkey = new KzPubKey(gpkr.pubkey);
                if (pubkey != null && pubkey.IsCompressed && new[] { 2, 3 }.Contains(pubkey.ReadOnlySpan[0]))
                    return pubkey;
            }

            return null;
        }

        public async Task<bool> VerifyPubKey(string receiverHandle, KzPubKey pubKey)
        {
            var uri = await GetVerifyUrl(receiverHandle, pubKey.ToHex());

            var r = await _HttpClient.GetAsync(uri);

            if (r.StatusCode == HttpStatusCode.OK)
            {
                var json = await r.Content.ReadAsStringAsync();
                var vpkr = JsonConvert.DeserializeObject<VerifyPubKeyResponse>(json);
                if (vpkr.pubkey == pubKey.ToHex())
                    return vpkr.match;
            }

            return false;
        }

        /// <summary>
        /// Implements brfc 759684b1a19a, paymentDestination: bsvalias Payment Addressing (Basic Address Resolution)
        /// 
        /// </summary>
        /// <param name="key">Private key with which to sign this request. If null, signature will be blank. Else, must match public key returned by GetPubKey(senderHandle).</param>
        /// <param name="receiverHandle"></param>
        /// <param name="senderHandle"></param>
        /// <param name="senderName"></param>
        /// <param name="amount"></param>
        /// <param name="purpose"></param>
        /// <returns></returns>
        public async Task<KzScript> GetOutputScript(KzPrivKey key, string receiverHandle, string senderHandle, string senderName = null, KzAmount? amount = null, string purpose = null)
        {
            if (!amount.HasValue) amount = KzAmount.Zero;
            var dt = DateTime.UtcNow.ToString("o");
            var message = $"{senderHandle}{amount.Value.Satoshis}{dt}{purpose}";
            var signature = key?.SignMessageToB64(message);

            // var ok = key.GetPubKey().VerifyMessage(message, signature);

            var request = new GetOutputScriptRequest
            {
                senderHandle = senderHandle,
                amount = amount.Value.Satoshis,
                dt = dt,
                purpose = purpose ?? "",
                senderName = senderName ?? "",
                signature = signature ?? ""
            };

            var jsonContent = JsonConvert.SerializeObject(request);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var uri = await GetAddressUrl(receiverHandle);

            var rm = await _HttpClient.PostAsync(uri, httpContent);

            if (rm.StatusCode == HttpStatusCode.OK)
            {
                var response = await rm.Content.ReadAsStringAsync();

                // e.g. {"output":"76a914bdfbe8a16162ba467746e382a081a1857831811088ac"} 

                var r = JsonConvert.DeserializeObject<GetOutputScriptResponse>(response);

                var s = new KzScript(r.output);

                return s;
            }

            if (rm.StatusCode == HttpStatusCode.NotFound)
                throw new ArgumentException($"Paymail \"{receiverHandle}\" was not found by this service.");

            throw new Exception($"Unhandled HTTP Post StatusCode {rm.StatusCode}.");
        }

        /// <summary>
        /// Verifies that the message was signed by the private key corresponding the paymail's public key.
        /// </summary>
        /// <param name="message">A copy of the message which was originally signed.</param>
        /// <param name="signature">The signature received for validation.</param>
        /// <param name="paymail">The paymail claiming to have signed the message.</param>
        /// <param name="pubkey">If known, the public key corresponding to the private key used by the paymail to sign messages.</param>
        /// <returns>(ok, pubkey) where ok is true only if both the public key and signature were confirmed as valid.
        /// If ok is true, the returned public key is valid and can be saved for future validations.
        /// </returns>
        public async Task<(bool ok, KzPubKey pubkey)> IsValidSignature(string message, string signature, string paymail, KzPubKey pubkey = null)
        {
            var (ok, alias, domain) = KzPaymail.Parse(paymail);

            if (!ok) goto fail;

            if (pubkey != null)
            {
                // If a pubkey is provided and the domain is capable, verify that it is correct
                // If it is not correct, forget the input value and attempt to obtain the valid key.
                if (await DomainHasCapability(domain, KzPaymail.Capability.verifyPublicKeyOwner))
                {
                    if (!await VerifyPubKey(paymail, pubkey))
                        pubkey = null;
                }
            }

            if (pubkey == null)
            {
                // Attempt to determine the correct pubkey for the paymail.
                if (await DomainHasCapability(domain, KzPaymail.Capability.pki))
                {
                    pubkey = await GetPubKey(paymail);
                }
            }

            if (pubkey == null) goto fail;

            ok = pubkey.VerifyMessage(message, signature);

            return (ok, pubkey);

            fail:
            return (false, pubkey);
        }

        public async Task<string> GetIdentityUrl(string paymail) => await GetCapabilityUrl(KzPaymail.Capability.pki, paymail);
        public async Task<string> GetAddressUrl(string paymail) => await GetCapabilityUrl(KzPaymail.Capability.paymentDestination, paymail);
        public async Task<string> GetVerifyUrl(string paymail, string pubkey) => await GetCapabilityUrl(KzPaymail.Capability.verifyPublicKeyOwner, paymail, pubkey);

        public async Task EnsureCapability(string domain, KzPaymail.Capability capability)
        {
            if (!await DomainHasCapability(domain, capability))
                throw new InvalidOperationException($"Unknown capability \"{capability}\" for \"{domain}\"");
        }

        async Task<CapabilitiesResponse> GetApiDescriptionFor(string domain, bool ignoreCache = false)
        {
            if (!ignoreCache && CacheTryGetValue(domain, out CapabilitiesResponse ba))
                return ba;

            var hostname = domain;
            var dns = new LookupClient();
            var r2 = await dns.QueryAsync($"_bsvalias._tcp.{domain}", QueryType.SRV);
            if (!r2.HasError && r2.Answers.Count == 1) {
                var srv = r2.Answers[0] as DnsClient.Protocol.SrvRecord;
                hostname = srv.Target.Value[0..^1] + ":" + srv.Port;
            }

            var r = await _HttpClient.GetAsync($"https://{hostname}/.well-known/bsvalias");
            if (r.StatusCode == HttpStatusCode.OK) {
                var json = await r.Content.ReadAsStringAsync();
                var jo = JObject.Parse(json);
                ba = new CapabilitiesResponse();
                ba.bsvalias = jo["bsvalias"].Value<string>();
                foreach (var c in jo["capabilities"].Children<JProperty>())
                    ba.capabilities.Add(c.Name, c.Value as JValue);
                CacheUpdateValue(domain, ba);

                return ba;
            }

            return null;
        }

        async Task<string> GetCapabilityUrl(KzPaymail.Capability capability, string paymail, string pubkey = null)
        {
            var (ok, alias, domain) = KzPaymail.Parse(paymail);
            if (!ok)
                return null;

            await EnsureCapability(domain, capability);
            var ba = await GetApiDescriptionFor(domain);
            var url = ba.capabilities[KzPaymail.ToBrfcId(capability)].Value<string>();
            url = url.Replace("{alias}", alias).Replace("{domain.tld}", domain);
            if (pubkey != null)
                url = url.Replace("{pubkey}", pubkey);
            return url;
        }

        class CapabilitiesResponse
        {
            public string bsvalias { get; set; }
            public Dictionary<string, JValue> capabilities { get; set; } = new Dictionary<string, JValue>();
        }

        class GetPubKeyResponse
        {
            public string bsvalias { get; set; }
            public string handle { get; set; }
            public string pubkey { get; set; }
        }

        class VerifyPubKeyResponse
        {
            public string handle { get; set; }
            public string pubkey { get; set; }
            public bool match { get; set; }
        }

        class GetOutputScriptRequest
        {
            public string senderHandle { get; set; }
            public long amount { get; set; }
            public string dt { get; set; }
            public string purpose { get; set; }
            public string senderName { get; set; }
            public string signature { get; set; }
        }

        class GetOutputScriptResponse
        {
            public string output { get; set; }
        }

    }
}
