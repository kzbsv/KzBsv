using System;
using System.Collections.Generic;
using System.Text;

namespace KzBsv.Network
{
    /**
     * nServices flags.
     */
    public enum ServiceFlags : long
    {
        // Nothing
        NODE_NONE = 0,
        // NODE_NETWORK means that the node is capable of serving the block chain.
        // It is currently set by all Bitcoin SV nodes, and is unset by SPV clients
        // or other peers that just want network services but don't provide them.
        NODE_NETWORK = (1 << 0),
        // NODE_GETUTXO means the node is capable of responding to the getutxo
        // protocol request. Bitcoin SV does not support this but a patch set
        // called Bitcoin XT does. See BIP 64 for details on how this is
        // implemented.
        NODE_GETUTXO = (1 << 1),
        // NODE_BLOOM means the node is capable and willing to handle bloom-filtered
        // connections. Bitcoin SV nodes used to support this by default, without
        // advertising this bit, but no longer do as of protocol version 70011 (=
        // NO_BLOOM_VERSION)
        NODE_BLOOM = (1 << 2),
        // NODE_XTHIN means the node supports Xtreme Thinblocks. If this is turned
        // off then the node will not service nor make xthin requests.
        NODE_XTHIN = (1 << 4),
        // NODE_BITCOIN_CASH means the node supports Bitcoin Cash and the
        // associated consensus rule changes.
        // This service bit is intended to be used prior until some time after the
        // UAHF activation when the Bitcoin Cash network has adequately separated.
        // TODO: remove (free up) the NODE_BITCOIN_CASH service bit once no longer
        // needed.
        NODE_BITCOIN_CASH = (1 << 5),

        // Bits 24-31 are reserved for temporary experiments. Just pick a bit that
        // isn't getting used, or one not being used much, and notify the
        // bitcoin-development mailing list. Remember that service bits are just
        // unauthenticated advertisements, so your code must be robust against
        // collisions and other cases where nodes may be advertising a service they
        // do not actually support. Other service bits should be allocated via the
        // BIP process.
    }

    class KzNodeStats
    {
        long nodeid;
        ServiceFlags nServices;
        bool fRelayTxes;
        long nLastSend;
        long nLastRecv;
        long nTimeConnected;
        long nTimeOffset;
        string addrName;
        int nVersion;
        string cleanSubVer;
        bool fInbound;
        bool fAddnode;
        int nStartingHeight;
        long nSendBytes;
        //mapMsgCmdSize mapSendBytesPerMsgCmd;
        long nRecvBytes;
        //mapMsgCmdSize mapRecvBytesPerMsgCmd;
        bool fWhitelisted;
        double dPingTime;
        double dPingWait;
        double dMinPing;
        // What this peer sees as my address
        string addrLocal;
        //CAddress addr;
        uint nInvQueueSize;
    };

}
