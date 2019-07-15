#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Diagnostics;

namespace KzBsv
{
    public class KzBTxIn
    {
        public KzUInt256 TxId;
        public int N;
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
        /// The funding transaction referenced by TxId.
        /// It may not be available and the value is null in some circumstances.
        /// </summary>
        public KzTransaction Tx = null;

        public KzPubKey PubKey;

        public KzOutPoint Prevout { get => new KzOutPoint(TxId, N); set { TxId = value.TxId; N = value.N; } }

        public KzBTxIn() { }

        public KzBTxIn(KzTxIn txIn)
        {
            TxId = txIn.PrevOut.TxId;
            N = (int)txIn.PrevOut.N;
            ScriptSig.Set(txIn.ScriptSig);
            Sequence = txIn.Sequence;
        }

        public static KzBTxIn FromP2PKH(KzPubKey pubKey, KzAmount value, KzUInt256 txId, int n, KzScript scriptPub, UInt32 sequence = KzTxIn.SEQUENCE_FINAL)
        {
            var (pub, sig) = KzBScript.NewP2PKH(pubKey);
            var ok = pub.ToScript() == scriptPub;
            Debug.Assert(ok);

            var r = new KzBTxIn {
                TxId = txId,
                N = n,
                ScriptSig = sig,
                Sequence = sequence,
                Value = value,
                ScriptPub = pub,
                PubKey = pubKey
            };
            return r;
        }

        public static KzBTxIn FromP2PKH(KzPubKey pubKey, KzTransaction tx, int n, UInt32 sequence = KzTxIn.SEQUENCE_FINAL)
        {
            Debug.Assert(tx.TxId != KzUInt256.Zero);
            var o = tx.Vout[n];
            var r = FromP2PKH(pubKey, o.Value, tx.TxId, n, o.ScriptPubKey, sequence);
            r.Tx = tx;
            return r;
        }

        public KzTxIn ToTxIn()
        {
            return new KzTxIn(Prevout, ScriptSig, Sequence);
        }

    }


}
