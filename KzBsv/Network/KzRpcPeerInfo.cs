#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion

namespace KzBsv {

    /// <summary>
    /// 
    /// </summary>
    public class KzRpcPeerInfo {
		/// <summary>
		/// Peer index
		/// </summary>
        public int id { get; set; }

		/// <summary>
		///  The ip address and port of the peer
		/// </summary>
        public string addr { get; set; }

		/// <summary>
		///  local address
		/// </summary>
        public string addrlocal { get; set; }

		/// <summary>
		///  The services offered
		/// </summary>
        public string services { get; set; }

		/// <summary>
		///  Whether peer has asked us to relay transactions to it
		/// </summary>
        public bool relaytxes { get; set; }

		/// <summary>
		///  The time in seconds since epoch (Jan 1 1970 GMT) of the last send
		/// </summary>
        public long lastsend { get; set; }

		/// <summary>
		///  The time in seconds since epoch (Jan 1 1970 GMT) of the last receive
		/// </summary>
        public long lastrecv { get; set; }

		/// <summary>
		///  Current size of queued messages for sending
		/// </summary>
        public long sendsize { get; set; }

		/// <summary>
		///  Are we paused for sending
		/// </summary>
        public bool pausesend { get; set; }

		/// <summary>
		///  Are we paused for receiving
		/// </summary>
        public bool pauserecv { get; set; }

		/// <summary>
		///  The total bytes sent
		/// </summary>
        public long bytessent { get; set; }

		/// <summary>
		///  The total bytes received
		/// </summary>
        public long bytesrecv { get; set; }

		/// <summary>
		///  The spot average download bandwidth from this node (bytes/sec)
		/// </summary>
        public long spotrecvbw { get; set; }

		/// <summary>
		///  The 1 minute average download bandwidth from this node (bytes/sec)
		/// </summary>
        public long minuterecvbw { get; set; }

		/// <summary>
		///  The connection time in seconds since epoch (Jan 1 1970 GMT)
		/// </summary>
        public long conntime { get; set; }

		/// <summary>
		///  The time offset in seconds
		/// </summary>
        public long timeoffset { get; set; }

		/// <summary>
		///  ping time (if available)
		/// </summary>
        public double pingtime { get; set; }

		/// <summary>
		///  minimum observed ping time (if any at all)
		/// </summary>
        public double minping { get; set; }

		/// <summary>
		///  ping wait (if non-zero)
		/// </summary>
		public double? pingwait { get; set; }

		/// <summary>
		///  The peer version, such as 7001
		/// </summary>
        public long version { get; set; }

		/// <summary>
		///  The string version
		/// </summary>
        public string subver { get; set; }

		/// <summary>
		///  Inbound (true) or Outbound (false)
		/// </summary>
        public bool inbound { get; set; }

		/// <summary>
		///  Whether connection was due to addnode and is using an addnode slot
		/// </summary>
        public bool addnode { get; set; }

		/// <summary>
		///  The starting height (block) of the peer
		/// </summary>
        public int startingheight { get; set; }

		/// <summary>
		///  The number of queued transaction inventory msgs we have for this peer
		/// </summary>
        public long txninvsize { get; set; }

		/// <summary>
		///  The ban score
		/// </summary>
        public long banscore { get; set; }

		/// <summary>
		///  The last header we have in common with this peer
		/// </summary>
        public int synced_headers { get; set; }

		/// <summary>
		///  The last block we have in common with this peer
		/// </summary>
        public int synced_blocks { get; set; }

		/// <summary>
		///  The heights of blocks we're currently asking from this peer
		/// </summary>
		public int[] inflight { get; set; }

		/// <summary>
		///  Whether the peer is whitelisted
		/// </summary>
        public bool whitelisted { get; set; }

		/// <summary>
		///  The total bytes sent aggregated by message type
		/// </summary>
		public ByMsgCounts bytessent_per_msg { get; set; }

		/// <summary>
		///  The total bytes received aggregated by message type
		/// </summary>
		public ByMsgCounts bytesrecv_per_msg { get; set; }


        public KzRpcPeerInfo() { }

		/// <summary>
		/// The total bytes aggregated by message type
		/// </summary>
        public struct ByMsgCounts {
            public long addr { get; set; }
            public long feefilter { get; set; }
            public long getaddr { get; set; }
            public long getdata { get; set; }
            public long getheaders { get; set; }
            public long headers { get; set; }
            public long inv { get; set; }
            public long notfound { get; set; }
            public long ping { get; set; }
            public long pong { get; set; }
            public long protoconf { get; set; }
            public long reject { get; set; }
            public long sendcmpct { get; set; }
            public long sendheaders { get; set; }
            public long tx { get; set; }
            public long verack { get; set; }
            public long version { get; set; }
        }
    }
#if false
{"result":
	[
		{
			"id":3,
			"addr":"47.52.110.56:8333",
			"addrlocal":"24.91.86.78:58824",
			"services":"0000000000000025",
			"relaytxes":true,
			"lastsend":1588420402,
			"lastrecv":1588420402,
			"sendsize":0,
			"pausesend":false,
			"pauserecv":false,
			"bytessent":57536017,
			"bytesrecv":212139762,
			"spotrecvbw":435,
			"minuterecvbw":581,
			"conntime":1588116991,
			"timeoffset":0,
			"pingtime":0.248226,
			"minping":0.238588,
			"version":70015,
			"subver":"/Bitcoin SV:1.0.1/",
			"inbound":false,
			"addnode":false,
			"startingheight":632649,
			"txninvsize":0,
			"banscore":0,
			"synced_headers":633154,
			"synced_blocks":633154,
			"inflight":[],
			"whitelisted":false,
			"bytessent_per_msg":
            {
                "addr":55,
                "feefilter":32,
                "getaddr":24,
                "getdata":12993258,
                "getheaders":69146,
                "headers":10644009,
                "inv":33450204,
                "notfound":204464,
                "ping":80864,
                "pong":80864,
                "protoconf":29,
                "reject":83,
                "sendcmpct":33,
                "sendheaders":24,
                "tx":12776,
                "verack":24,
                "version":128
            },
			"bytesrecv_per_msg":
            {
                "addr":30522,
                "cmpctblock":233226,
                "feefilter":32,
                "getdata":206840,
                "getheaders":69146,
                "headers":10644115,
                "inv":78700979,
                "notfound":5692,
                "ping":80864,
                "pong":80864,
                "protoconf":29,
                "sendcmpct":33,
                "sendheaders":24,
                "tx":122087244,
                "verack":24,
                "version":128
            }
		},
	],
	"error":null,
	"id":1
}
#endif

}
