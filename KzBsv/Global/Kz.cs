#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace KzBsv
{
    public enum KzChain
    {
        unkown,
        main,
        test,
        regtest,
        stn
    }

    public enum KzBase58Type : int
    {
        PUBKEY_ADDRESS,
        SCRIPT_ADDRESS,
        SECRET_KEY,
        EXT_PUBLIC_KEY,
        EXT_SECRET_KEY,

        MAX_BASE58_TYPES
    };

    public static class Kz
    {
        /// <summary>
        /// Encodes a sequence of bytes as hexadecimal digits where:
        /// First byte first: The encoded string begins with the first byte.
        /// Character 0 corresponds to the high nibble of the first byte. 
        /// Character 1 corresponds to the low nibble of the first byte. 
        /// </summary>
        public static KzEncodeHex Hex => KzEncoders.Hex;

        /// <summary>
        /// Encodes a sequence of bytes as hexadecimal digits where:
        /// Last byte first: The encoded string begins with the last byte.
        /// Character 0 corresponds to the high nibble of the last byte. 
        /// Character 1 corresponds to the low nibble of the last byte. 
        /// </summary>
        public static KzEncodeHexReverse HexR => KzEncoders.HexReverse;

        public static string MasterBip32Key = "Bitcoin seed";

        public static KzBScript Script() => new KzBScript();
        public static KzBTransaction Tx() => new KzBTransaction();

        /// <summary>
        /// Base58 encoding prefix for public key addresses for the active network.
        /// </summary>
        public static ReadOnlySpan<byte> PUBKEY_ADDRESS => Params.Base58Prefix(KzBase58Type.PUBKEY_ADDRESS);
        /// <summary>
        /// Base58 encoding prefix for script addresses for the active network.
        /// </summary>
        public static ReadOnlySpan<byte> SCRIPT_ADDRESS => Params.Base58Prefix(KzBase58Type.SCRIPT_ADDRESS);
        /// <summary>
        /// Base58 encoding prefix for private keys for the active network.
        /// </summary>
        public static ReadOnlySpan<byte> SECRET_KEY => Params.Base58Prefix(KzBase58Type.SECRET_KEY);
        /// <summary>
        /// Base58 encoding prefix for extended public keys for the active network.
        /// </summary>
        public static ReadOnlySpan<byte> EXT_PUBLIC_KEY => Params.Base58Prefix(KzBase58Type.EXT_PUBLIC_KEY);
        /// <summary>
        /// Base58 encoding prefix for extended private keys for the active network.
        /// </summary>
        public static ReadOnlySpan<byte> EXT_SECRET_KEY => Params.Base58Prefix(KzBase58Type.EXT_SECRET_KEY);

        public static void CreateChainParams(KzChain chain)
        {
            lock (chainLock) {
                if (Kz.chain != KzChain.unkown)
                    throw new InvalidOperationException("The chain has already been selected.");
                Kz.chain = chain;
            }
        }

        static KzChainParams CreateChainParams()
        {
            var chain = KzChain.unkown;
            lock (chainLock) {
                if (Kz.chain == KzChain.unkown) Kz.chain = KzChain.main;
                chain = Kz.chain;
            }
            return chain switch
            {
                KzChain.main => new KzMainParams(),
                KzChain.regtest => new KzRegTestParams(),
                KzChain.stn => new KzStnParams(),
                KzChain.test => new KzTestNetParams(),
                _ => (KzChainParams)null
            };
        }

        static object chainLock = new object();
        static KzChain chain = KzChain.unkown;
        static Lazy<KzChainParams> lazyParams;

        static Kz()
        {
            lazyParams = new Lazy<KzChainParams>(() => CreateChainParams(), true);
        }

        public static KzChainParams Params => lazyParams.Value;

        public static KzConsensus Consensus => Params.Consensus;
    }

    public class KzMainParams : KzChainParams
    {
        public KzMainParams() : base()
        {
            strNetworkID = "main";

            Consensus = new KzConsensus() {
                nSubsidyHalvingInterval = 210000,
                BIP34Height = 227931,
                BIP34Hash = new KzUInt256("000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                // 000000000000000004c2b624ed5d7756c508d90fd0da2c7c679febfa6c4735f0
                BIP65Height = 388381,
                // 00000000000000000379eaa19dce8c9b722d46ae6a57c2f1a988119488b50931
                BIP66Height = 363725,
                powLimit = new KzUInt256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
                // two weeks
                nPowTargetTimespan = 14 * 24 * 60 * 60,
                nPowTargetSpacing = 10 * 60,
                fPowAllowMinDifficultyBlocks = false,
                fPowNoRetargeting = false,
                // 95% of 2016
                nRuleChangeActivationThreshold = 1916,
                // nPowTargetTimespan / nPowTargetSpacing
                nMinerConfirmationWindow = 2016,

                // The best chain should have at least this much work.
                nMinimumChainWork = new KzUInt256("000000000000000000000000000000000000000000a0f3064330647e2f6c4828"),

                // By default assume that the signatures in ancestors of this block are valid.
                defaultAssumeValid = new KzUInt256("000000000000000000e45ad2fbcc5ff3e85f0868dd8f00ad4e92dffabe28f8d2"),

                // August 1, 2017 hard fork
                uahfHeight = 478558,

                // November 13, 2017 hard fork
                daaHeight = 504031,
            };

            Consensus.vDeployments[(int)KzDeploymentPos.DEPLOYMENT_TESTDUMMY] = new KzBIP9Deployment() {
                bit = 28,
                nStartTime = 1199145601,    // January 1, 2008
                nTimeout = 1230767999       // December 31, 2008
            };

            // Deployment of BIP68, BIP112, and BIP113.
            Consensus.vDeployments[(int)KzDeploymentPos.DEPLOYMENT_CSV] = new KzBIP9Deployment() {
                bit = 0,
                nStartTime = 1462060800,    // May 1st, 2016
                nTimeout = 1493596800       // May 1st, 2017
            };

            base58Prefixes[(int)KzBase58Type.PUBKEY_ADDRESS] = new byte[] { (0) };
            base58Prefixes[(int)KzBase58Type.SCRIPT_ADDRESS] = new byte[] { (5) };
            base58Prefixes[(int)KzBase58Type.SECRET_KEY] = new byte[] { (128) };
            base58Prefixes[(int)KzBase58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            base58Prefixes[(int)KzBase58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
        }
    }

    public class KzStnParams : KzChainParams
    {
        public KzStnParams() : base()
        {
            strNetworkID = "stn";

            base58Prefixes[(int)KzBase58Type.PUBKEY_ADDRESS] = new byte[] { (111) };
            base58Prefixes[(int)KzBase58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
            base58Prefixes[(int)KzBase58Type.SECRET_KEY] = new byte[] { (239) };
            base58Prefixes[(int)KzBase58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
            base58Prefixes[(int)KzBase58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
        }
    }

    public class KzTestNetParams : KzChainParams
    {
        public KzTestNetParams() : base()
        {
            strNetworkID = "test";

			base58Prefixes[(int)KzBase58Type.PUBKEY_ADDRESS] = new byte[] { (111) };
			base58Prefixes[(int)KzBase58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
			base58Prefixes[(int)KzBase58Type.SECRET_KEY] = new byte[] { (239) };
			base58Prefixes[(int)KzBase58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
			base58Prefixes[(int)KzBase58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
        }
    }

    public class KzRegTestParams : KzChainParams
    {
        public KzRegTestParams() : base()
        {
            strNetworkID = "regtest";

			base58Prefixes[(int)KzBase58Type.PUBKEY_ADDRESS] = new byte[] { (111) };
			base58Prefixes[(int)KzBase58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
			base58Prefixes[(int)KzBase58Type.SECRET_KEY] = new byte[] { (239) };
			base58Prefixes[(int)KzBase58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
			base58Prefixes[(int)KzBase58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
        }
    }

}
