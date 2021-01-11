#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace KzBsv
{
    public enum KzDeploymentPos
    {
        DEPLOYMENT_TESTDUMMY,
        // Deployment of BIP68, BIP112, and BIP113.
        DEPLOYMENT_CSV,
        // 
        // 
        /// <summary>
        /// NOTE: Also add new deployments to VersionBitsDeploymentInfo in versionbits.cpp
        /// </summary>
        MAX_VERSION_BITS_DEPLOYMENTS
    };

    public struct KzBIP9Deployment
    {
        /** Bit position to select the particular bit in nVersion. */
        public int bit;
        /** Start MedianTime for version bits miner confirmation. Can be a date in
         * the past */
        public Int64 nStartTime;
        /** Timeout/expiry MedianTime for the deployment attempt. */
        public Int64 nTimeout;
    };

    public class KzConsensus
    {
        public KzConsensus()
        {
            vDeployments = new KzBIP9Deployment[(int)KzDeploymentPos.MAX_VERSION_BITS_DEPLOYMENTS];
        }

        public KzUInt256 hashGenesisBlock;
        public int nSubsidyHalvingInterval;
        /** Block height and hash at which BIP34 becomes active */
        public int BIP34Height;
        public KzUInt256 BIP34Hash;
        /** Block height at which BIP65 becomes active */
        public int BIP65Height;
        /** Block height at which BIP66 becomes active */
        public int BIP66Height;
        /** Block height at which UAHF kicks in */
        public int uahfHeight;
        /** Block height at which the new DAA becomes active */
        public int daaHeight;
        /**
         * Minimum blocks including miner confirmation of the total of 2016 blocks
         * in a retargeting period, (nPowTargetTimespan / nPowTargetSpacing) which
         * is also used for BIP9 deployments.
         * Examples: 1916 for 95%, 1512 for testchains.
         */
        public UInt32 nRuleChangeActivationThreshold;
        public UInt32 nMinerConfirmationWindow;

        public KzBIP9Deployment[] vDeployments;

        /** Proof of work parameters */
        public KzUInt256 powLimit;
        public bool fPowAllowMinDifficultyBlocks;
        public bool fPowNoRetargeting;
        public Int64 nPowTargetSpacing;
        public Int64 nPowTargetTimespan;
        public KzUInt256 nMinimumChainWork;
        public KzUInt256 defaultAssumeValid;


        public Int64 DifficultyAdjustmentInterval => nPowTargetTimespan / nPowTargetSpacing;

        /// <summary>
        /// satoshis per coin. Main is 100 million satoshis per coin.
        /// </summary>
        public readonly Int64 SatoshisPerCoin = 100_000_000L;

        /// <summary>
        /// Initial block reward. Main is 50 coins, 5 billion satoshis.
        /// </summary>
        public readonly Int64 RewardInitial = 5_000_000_000L;

        /// <summary>
        /// How many blocks between reductions in the block reward rate.
        /// </summary>
        public int RewardHalvingInterval => nSubsidyHalvingInterval;

        public readonly string GenesisBlockHash = "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f";

        /// <summary>
        /// Maximum number of bytes pushable to the stack
        /// </summary>
        public readonly UInt32 ScriptMaxElementSize = 520;
        /// <summary>
        /// Maximum number of non-push operations per script
        /// </summary>
        public readonly int ScriptMaxOpsPer = 500;
        /// <summary>
        /// Maximum number of public keys per multisig
        /// </summary>
        public readonly int ScriptMaxPubKeysPerMultiSig = 20;
        /// <summary>
        /// Maximum script length in bytes. 
        /// </summary>
        public readonly int ScriptMaxSize = 10000;
        /// <summary>
        /// Threshold for nLockTime: below this value it is interpreted as block number,
        /// otherwise as UNIX timestamp. Thresold is Tue Nov 5 00:53:20 1985 UTC
        /// </summary>
        public readonly UInt32 LocktimeThreshold = 500000000U;

        public UInt32 MAX_SCRIPT_ELEMENT_SIZE => ScriptMaxElementSize;
        public int MAX_OPS_PER_SCRIPT => ScriptMaxOpsPer;
        public int MAX_SCRIPT_SIZE => ScriptMaxSize;
        public int MAX_PUBKEYS_PER_MULTISIG => ScriptMaxPubKeysPerMultiSig;
#if false
            /// Block height and hash at which BIP34 becomes active
            public int BIP34Height;
            KzHash256 BIP34Hash;

            /// Block height at which BIP65 becomes active
            public int BIP65Height;

            /// Block height at which BIP66 becomes active
            public int BIP66Height;

            /// Block height at which UAHF kicks in

            public int uahfHeight;

            /// Block height at which the new DAA becomes active
            public int daaHeight;

            //
            // Minimum blocks including miner confirmation of the total of 2016 blocks
            // in a retargeting period, (nPowTargetTimespan / nPowTargetSpacing) which
            // is also used for BIP9 deployments.
            // Examples: 1916 for 95%, 1512 for testchains.
            //
            UInt32 nRuleChangeActivationThreshold;
            UInt32 nMinerConfirmationWindow;

            //BIP9Deployment vDeployments[MAX_VERSION_BITS_DEPLOYMENTS];

            // Proof of work parameters
            KzUInt256 powLimit;

            bool fPowAllowMinDifficultyBlocks;

            bool fPowNoRetargeting;

            UInt64 nPowTargetSpacing;

            UInt64 nPowTargetTimespan;

            //UInt64 DifficultyAdjustmentInterval() const {
            //    return nPowTargetTimespan / nPowTargetSpacing;
            //}

            KzUInt256 nMinimumChainWork;

            KzUInt256 defaultAssumeValid;
#endif
        KzBlock CreateGenesisBlock(string pszTimestamp,
                                 KzScript genesisOutputScript,
                                 UInt32 nTime, UInt32 nNonce,
                                 UInt32 nBits, Int32 nVersion,
                                 Int64 genesisReward)
        {
            var txs = new KzTransaction[] {
                new KzTransaction(
                    version: 1,
                    vin: new KzTxIn[] {
                        new KzTxIn(
                            prevout: new KzOutPoint(KzUInt256.Zero, -1),
                            scriptSig: new KzScript(""),
                            sequence: 0 )
                    },
                    vout: new KzTxOut[] {
                        new KzTxOut(
                            value: 0,
                            scriptPub: new KzScript("")
                            )
                    },
                    lockTime: 0
                )
            };
            var hashMerkleRoot = KzMerkleTree.ComputeMerkleRoot(txs);
            var genesis = new KzBlock(
                txs: txs,
                version: 1,
                hashPrevBlock: KzUInt256.Zero,
                hashMerkleRoot: hashMerkleRoot,
                time: 1231006506,
                bits: 0x1d00ffff,
                nonce: 2083236893
                );
            return genesis;
        }

#if false
        CMutableTransaction txNew;
        txNew.nVersion = 1;
    txNew.vin.resize(1);
    txNew.vout.resize(1);
    txNew.vin[0].scriptSig =
        CScript() << 486604799 << CScriptNum(4)
                  << std::vector<uint8_t>((const uint8_t*)pszTimestamp,
                                          (const uint8_t*)pszTimestamp +
                                              strlen(pszTimestamp));
    txNew.vout[0].nValue = genesisReward;
    txNew.vout[0].scriptPubKey = genesisOutputScript;

    CBlock genesis;
        genesis.nTime = nTime;
    genesis.nBits = nBits;
    genesis.nNonce = nNonce;
    genesis.nVersion = nVersion;
    genesis.vtx.push_back(MakeTransactionRef(std::move(txNew)));
    genesis.hashPrevBlock.SetNull();
    genesis.hashMerkleRoot = BlockMerkleRoot(genesis);
    return genesis;
54 68 65 20 54 69 6D 65 73 20 30 33 2F 4A 61 6E 2F 32 30 30 39 20 43 68 61 6E 63 65 6C 6C 6F 72 20 6F 6E 20 62 72 69 6E 6B 20 6F 66 20 73 65 63 6F 6E 64 20 62 61 69 6C 6F 75 74 20 66 6F 72 20 62 61 6E 6B 73  
#endif
    }

}
