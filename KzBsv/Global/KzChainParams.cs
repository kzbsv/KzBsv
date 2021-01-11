#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;

namespace KzBsv
{
    public class KzChainParams
    {
        public KzConsensus Consensus { get; protected set; }
        public string strNetworkID { get; protected set; }
        protected byte[][] base58Prefixes;

        protected KzChainParams()
        {
            base58Prefixes = new byte[(int)KzBase58Type.MAX_BASE58_TYPES][];
        }

        public ReadOnlySpan<byte> Base58Prefix(KzBase58Type type) => base58Prefixes[(int)type].AsSpan();
    }

}
