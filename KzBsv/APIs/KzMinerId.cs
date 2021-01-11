#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion

using Newtonsoft.Json;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace KzBsv {
    public class KzMinerId {
        static uint _ProtocolPrefix = 0xAC1EED88;

        public static UInt32 ProtocolUInt32 => _ProtocolPrefix;
        public static Int32 ProtocolInt32 => unchecked((Int32)_ProtocolPrefix);

        string _StaticJson;
        byte[] _StaticSig;
        string _DynamicJson;
        byte[] _DynamicSig;

        MinerIdStatic _Static;

        public string Version => _Static?.version;
        public int? Height => _Static?.height;

        public KzPubKey PubKey { get; set; }
        public byte[] PubKeyBytes { get; set; }

        public KzPubKey PrevPubKey { get; set; }
        public byte[] PrevPubKeyBytes { get; set; }

        public string ValidityCheckHashTxHex => _Static?.vctx.txid;
        public int? ValidityCheckOutN => _Static?.vctx.vout;

        public string ContactName => _Static?.minerContact?.name;
        public string ContactEmail => _Static?.minerContact?.email;

        public string MapiEndpoint => _Static?.minerContact?.merchantAPIEndPoint;

        public static KzMinerId Parse(KzScript scriptPub) {
            var m = (KzMinerId)null;
            var count = -1;
            foreach (var op in scriptPub.Decode()) {
                count++;
                switch (count) {
                    case 0: if (op.Code != KzOpcode.OP_0) goto fail; break;
                    case 1: if (op.Code != KzOpcode.OP_RETURN) goto fail; break;
                    case 2: if (op.Code != KzOpcode.OP_PUSH4 || op.Data.AsUInt32BigEndian() != _ProtocolPrefix) goto fail;
                        m = new KzMinerId();
                        break;
                    case 3: m._StaticJson = op.GetDataBytes().ToUTF8(); break;
                    case 4: m._StaticSig = op.GetDataBytes(); break;
                    case 5: m._DynamicJson = op.GetDataBytes().ToUTF8(); break;
                    case 6: m._DynamicSig = op.GetDataBytes(); break;
                    default: break; // Ignore additional ops.
                }
            }
            if (m._StaticJson == null || m._StaticSig == null) goto fail;

            m._Static = JsonConvert.DeserializeObject<MinerIdStatic>(m._StaticJson);

            if (!m.Verify()) goto fail;

            return m;

        fail:
            return null;
        }

        bool Verify() {

            var s = _Static;

            var message = $"{s.prevMinerId}{s.minerId}{s.vctx.txid}";
            var verifyHash = KzHashes.SHA256(message.UTF8ToBytes());
            var verifySignature = s.prevMinerIdSig.HexToBytes();

            PrevPubKeyBytes = s.prevMinerId.HexToBytes();
            PrevPubKey= new KzPubKey(PrevPubKeyBytes);

            var verified = PrevPubKey.IsValid && PrevPubKey.Verify(verifyHash, verifySignature);

            if (verified) {
                PubKeyBytes = s.minerId.HexToBytes();
                PubKey = new KzPubKey(PubKeyBytes);
                verified = PubKey.IsValid;
            }

            return verified;
        }

        public class Vctx {
            public string txid;
            public int vout;
        }

        public class MinerContact {
            public string name;
            public string merchantAPIEndPoint;
            public string email;
        }

        public class MinerIdStatic {
#region Sample MinerId Static Document JSON:
#if false
{
    "version":"0.1",
    "height":"631762",
    "prevMinerId":"03e92d3e5c3f7bd945dfbf48e7a99393b1bfb3f11f380ae30d286e7ff2aec5a270",
    "prevMinerIdSig":"3045022100d76360e4d21331ca86f018c046e57c938f1977507473335360be37048cae1af302200be660454021bf9464e99f5a9581a98c9cf495407598c59b4734b2fdb482bf97",
    "minerId":"03e92d3e5c3f7bd945dfbf48e7a99393b1bfb3f11f380ae30d286e7ff2aec5a270",
    "vctx":{
        "txId":"579b435925a90ee39a37be3b00b9061e74c30c82413f6d0a2098e1bea7a2515f",
        "vout":0
    },
    "minerContact":{
        "email":"info@taal.com",
        "name":"TAAL Distributed Information Technologies",
        "merchantAPIEndPoint":"https://merchantapi.taal.com/"
    }
}
#endif
#endregion
            public string version;
            public int height;

            public string prevMinerId;
            public string prevMinerIdSig;
            public string dynamicMinerId;

            public string minerId;

            public Vctx vctx;

            public MinerContact minerContact;

            public string extensions;

        }
    }
}
