#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace KzBsv {
    public class KzRpcClient {
		string _auth;
		Uri _uri;

        public Uri Uri => _uri;

        public KzRpcClient(string auth, Uri uri) {
			_auth = auth;
			_uri = uri;
		}

		public HttpWebRequest SendCommandRaw(string method, params object[] args) {
			var address = _uri.AbsoluteUri;

			var webRequest = (HttpWebRequest)WebRequest.Create(address);
			webRequest.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(_auth));
			webRequest.Method = "POST";
			webRequest.KeepAlive = false;
			webRequest.Timeout = (int)TimeSpan.FromSeconds(100).TotalMilliseconds;

			var sw = new StringWriter();
			var jw = new JsonTextWriter(sw);
			jw.WriteStartObject();
			jw.WritePropertyName("jsonrpc");
			jw.WriteValue("1.0");
			jw.WritePropertyName("id");
			jw.WriteValue(1);
			jw.WritePropertyName("method");
			jw.WriteValue(method);
			jw.WritePropertyName("params");
			jw.WriteStartArray();
			if (args != null) {
				foreach (var arg in args) WriteValue(jw, arg);
			}
			jw.WriteEndArray();
			jw.WriteEndObject();
			sw.Flush();
			var json = sw.ToString();
			var content = Encoding.UTF8.GetBytes(json);

			webRequest.ContentLength = content.Length;
			var rs = webRequest.GetRequestStream();
			rs.Write(content, 0, content.Length);
			rs.Flush();

			return webRequest;
		}

		public JObject SendCommand(string method, params object[] args) {
			var webRequest = SendCommandRaw(method, args);
			var webResponse = webRequest.GetResponse();
			var rs2 = webResponse.GetResponseStream();
			var jo = JObject.Load(new JsonTextReader(new StreamReader(rs2, Encoding.UTF8)));
			return jo;
		}

		public KzRpcResponse<T> SendCommand<T>(string method, params object[] args) {
			var webRequest = SendCommandRaw(method, args);
            try {
                var webResponse = webRequest.GetResponse();
                using (var sr = new StreamReader(webResponse.GetResponseStream())) {
                    var json = sr.ReadToEnd();
                    var result = JsonConvert.DeserializeObject<KzRpcResponse<T>>(json);
                    return result;
                }
            } catch (Exception ex) {
				return new KzRpcResponse<T> {
					error = ex.Message
				};
            }
        }

		public KzUInt256 GetBestBlockHash() {
			var jo = SendCommand("getbestblockhash");
			var error = jo.GetValue("error") as JObject;
            if (error != null) throw new InvalidDataException(error.ToString());
			var hex = jo.GetValue("result").Value<string>();
            return new KzUInt256(hex);
		}

		public KzBlockchainInfo GetBlockchainInfo() {
			return new KzBlockchainInfo(SendCommand("getblockchaininfo"));
		}

		public KzRpcResponse<KzRpcPeerInfo[]> GetPeerInfo() => SendCommand<KzRpcPeerInfo[]>("getpeerinfo");

		public KzRpcResponse<object> SetBan(string addr) => SendCommand<object>("setban", addr.Split(':')[0], "add");

		public string GetBlockHash(long height) {
			return SendCommand("getblockhash", height).GetValue("result").Value<string>();
		}

		public void GetTransaction(string hash) {
			var r = SendCommand("getrawtransaction", hash, true);
            var s = r.ToString();
        }

        public KzRpcBlockRaw GetBlockRaw(KzUInt256 hash) => GetBlockRaw(hash.ToString());
		public KzRpcBlockRaw GetBlockRaw(string hash) {
			return new KzRpcBlockRaw(SendCommand("getblock", hash, false));
		}

		public KzRpcBlock GetBlock(string hash) {
			return new KzRpcBlock(SendCommand("getblock", hash));
		}

        public KzRpcBlockRaw GetBlockHeaderRaw(KzUInt256 hash) => GetBlockHeaderRaw(hash.ToString());
		public KzRpcBlockRaw GetBlockHeaderRaw(string hash) {
			return new KzRpcBlockRaw(SendCommand("getblockheader", hash, false));
		}

        public (bool ok, KzUInt256 txId) SendRawTransaction(string signedHex, bool allowHighFees = false)
        {
            try {
                var jo = SendCommand("sendrawtransaction", signedHex, allowHighFees);
                var error = jo.GetValue("error") as JObject;
                if (error == null) {
                    var result = jo.GetValue("result").Value<string>();
                    var txId = result.ToKzUInt256(firstByteFirst:true);
                    return (true, txId);
                }
            }
            catch (Exception e) {
				var m = e.Message;
            }
            return (false, KzUInt256.Zero);
        }

        public KzRpcBlock GetBlock(long height) => GetBlock(GetBlockHash(height));

		void WriteValue(JsonTextWriter writer, object obj)
		{
			if(obj is JToken)
			{
				((JToken)obj).WriteTo(writer);
			}
			else if(obj is Array)
			{
				writer.WriteStartArray();
				foreach(var x in (Array)obj)
				{
					writer.WriteValue(x);
				}
				writer.WriteEndArray();
			}
			else if(obj is KzUInt256)
			{
				writer.WriteValue(obj.ToString());
			}
			else
			{
				writer.WriteValue(obj);
			}
		}

	}

	public class KzRpcBlockRaw {
		public JObject error { get; }
		public long id { get; }
		public string raw { get; }
		public byte[] GetBytes() => KzEncoders.Hex.Decode(raw);


		public KzRpcBlockRaw() {
		}

		public KzRpcBlockRaw(JObject jo) {
			error = jo.GetValue("error") as JObject;
			if (error == null) {
				id = jo.GetValue("id").Value<long>();
				var result = jo.GetValue("result");
				raw = result.Value<string>();
			}
		}
	}

	public class KzRpcBlock {
		public JObject error { get; }
		public long id { get; }
		public string hash { get; }
		public long confirmations { get; }
		public long size { get; }
		public long height { get; }
		public long version { get; }
		public string versionHex { get; }
		public string merkleroot { get; }
		public string[] tx { get; }
		public long time { get; }
		public long mediantime { get; }
		public long nonce { get; }
		public string bits { get; }
		public double difficulty { get; }
		public string chainwork { get; }
		public string nextblockhash { get; }

		public KzRpcBlock() {
		}

		public KzRpcBlock(JObject jo) {
			error = jo.GetValue("error") as JObject;
			if (error == null) {
				id = jo.GetValue("id").Value<long>();
				var result = jo.GetValue("result");
				jo = result as JObject;
				hash = jo.GetValue("hash").Value<string>();
				confirmations = jo.GetValue("confirmations").Value<long>();
				size = jo.GetValue("size").Value<long>();
				height = jo.GetValue("height").Value<long>();
				version = jo.GetValue("version").Value<long>();
				versionHex = jo.GetValue("versionHex").Value<string>();
				merkleroot = jo.GetValue("merkleroot").Value<string>();
				var jotx = jo.GetValue("tx") as JArray;
				tx = new string[jotx.Count];
				for (var i = 0; i < jotx.Count; i++)
					tx[i] = jotx[i].Value<string>();
				time = jo.GetValue("time").Value<long>();
				mediantime = jo.GetValue("mediantime").Value<long>();
				nonce = jo.GetValue("nonce").Value<long>();
				bits = jo.GetValue("bits").Value<string>();
				difficulty = jo.GetValue("difficulty").Value<double>();
				chainwork = jo.GetValue("chainwork").Value<string>();
				nextblockhash = jo.GetValue("nextblockhash").Value<string>();
			}
		}
	}

	public class KzBlockchainInfo {
		public JObject error { get; }
		public long id { get; }
		public string chain { get; }
		public long blocks { get; }
		public long headers { get; }
		public string bestblockhash { get; }
		public double difficulty { get; }
		public long mediantime { get; }
		public double verificationprogress { get; }
		public bool pruned { get; }
		public JToken softforks { get; }
		public JToken bip9_softforks { get; }

		public KzBlockchainInfo(JObject jo) {
			error = jo.GetValue("error") as JObject;
			if (error == null) {
				id = jo.GetValue("id").Value<long>();
				jo = jo.GetValue("result") as JObject;
				chain = jo.GetValue("chain").Value<string>();
				blocks = jo.GetValue("blocks").Value<long>();
				headers = jo.GetValue("headers").Value<long>();
				bestblockhash = jo.GetValue("bestblockhash").Value<string>();
				difficulty = jo.GetValue("difficulty").Value<double>();
				mediantime = jo.GetValue("mediantime").Value<long>();
				verificationprogress = jo.GetValue("verificationprogress").Value<double>();
				pruned = jo.GetValue("pruned").Value<bool>();
				softforks = jo.GetValue("softforks");
				bip9_softforks = jo.GetValue("bip9_softforks");
			}
		}
	}

	public class KzRpcResponse<T> {
		public string error { get; set; }
		public long id { get; set; }
		public T result { get; set; }
	}

}
