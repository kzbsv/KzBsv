#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Diagnostics;

namespace KzBsv
{
    public class KzBTxIn
    {
        KzPubKey _PubKey = null;
        KzPrivKey _PrivKey = null;

        /// <summary>
        /// Must be set if PrivKey is not provided until signing operation.
        /// </summary>
        public KzPubKey PubKey { get => _PrivKey?.GetPubKey() ?? _PubKey; set => _PubKey = value; }

        /// <summary>
        /// If provided, PubKey does not need to be set. Signing can be done without providing it again.
        /// </summary>
        public KzPrivKey PrivKey { get => _PrivKey; set => _PrivKey = value; }

        public KzUInt256 PrevOutHashTx;
        public int PrevOutN;
        public KzBScript ScriptSig = new KzBScript();
        public UInt32 Sequence = KzTxIn.SEQUENCE_FINAL;

        /// <summary>
        /// This is the Value of the referenced Prevout.
        /// The amount this input contributes to funding this transaction.
        /// </summary>
        public KzAmount? Value;

        /// <summary>
        /// This is the ScriptPub of the referenced Prevout.
        /// Used to sign and verify this input.
        /// </summary>
        public KzBScript ScriptPub;

        /// <summary>
        /// The funding transaction referenced by HashTx.
        /// It may not be available and the value is null in some circumstances.
        /// </summary>
        public KzTransaction PrevOutTx = null;

        public KzOutPoint PrevOut { get => new KzOutPoint(PrevOutHashTx, PrevOutN); set { PrevOutHashTx = value.HashTx; PrevOutN = value.N; } }

        public KzBTxIn() { }

        public KzBTxIn(KzTxIn txIn)
        {
            PrevOutHashTx = txIn.PrevOut.HashTx;
            PrevOutN = (int)txIn.PrevOut.N;
            ScriptSig.Set(txIn.ScriptSig);
            Sequence = txIn.Sequence;
        }

        public static KzBTxIn FromP2PKH(KzPrivKey privKey, KzAmount value, KzUInt256 hashTx, int n, KzScript scriptPub, UInt32 sequence = KzTxIn.SEQUENCE_FINAL) {
            var r = FromP2PKH(privKey.GetPubKey(), value, hashTx, n, scriptPub, sequence);
            r.PrivKey = privKey;
            return r;
        }

        public static KzBTxIn FromP2PKH(KzPubKey pubKey, KzAmount value, KzUInt256 prevOutHashTx, int prevOutN, KzScript scriptPub, UInt32 sequence = KzTxIn.SEQUENCE_FINAL)
        {
            var (pub, sig) = KzBScript.NewP2PKH(pubKey);
            var ok = pub.ToScript() == scriptPub;
            Debug.Assert(ok);

            var r = new KzBTxIn {
                PrevOutHashTx = prevOutHashTx,
                PrevOutN = prevOutN,
                ScriptSig = sig,
                Sequence = sequence,
                Value = value,
                ScriptPub = pub,
                PubKey = pubKey
            };
            return r;
        }

        public static KzBTxIn FromP2PKH(KzPubKey pubKey, KzTransaction prevOutTx, int prevOutN, UInt32 sequence = KzTxIn.SEQUENCE_FINAL)
        {
            Debug.Assert(prevOutTx.HashTx != KzUInt256.Zero);
            var o = prevOutTx.Vout[prevOutN];
            var r = FromP2PKH(pubKey, o.Value, prevOutTx.HashTx, prevOutN, o.ScriptPub, sequence);
            r.PrevOutTx = prevOutTx;
            return r;
        }

        public KzTxIn ToTxIn()
        {
            return new KzTxIn(PrevOut, ScriptSig, Sequence);
        }

    }


}
