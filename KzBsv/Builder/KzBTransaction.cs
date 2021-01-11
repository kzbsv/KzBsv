#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Linq;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;

namespace KzBsv
{

    /// <summary>
    /// Support dynamic construction of new Bitcoin transactions.
    /// See <see cref="KzTransaction"/> for serializing and sending.
    /// </summary>
    public class KzBTransaction
    {
        public Int32 Version = 1;
        public List<KzBTxIn> Vin = new List<KzBTxIn>();
        public List<KzBTxOut> Vout = new List<KzBTxOut>();
        public UInt32 LockTime = 0;

        public KzUInt256? HashTx;

        public KzAmount? CurrentFee => Vin.Where(i => i.PrevOut.N >= 0).Sum(i => i.Value) - Vout.Sum(o => o.Value);

        public KzBTransaction() { }

        public KzBTransaction(KzTransaction tx)
        {
            Version = tx.Version;
            Vin = tx.Vin.Cast<KzBTxIn>().ToList();
            Vout = tx.Vout.Cast<KzBTxOut>().ToList();
            LockTime = tx.LockTime;
            HashTx = tx.HashTx;
            if (HashTx.Value == KzUInt256.Zero) HashTx = null;
        }

        public static KzBTransaction P2PKH
            ( IEnumerable<(KzPubKey pubKey, long value, byte[] hashTxBytes, int n, byte[] scriptPub)> from
            , IEnumerable<(KzPubKey pubKey, long value)> to
            )
        {
            var r = new KzBTransaction();
            foreach (var i in from) r.AddInP2PKH(i.pubKey, i.value, i.hashTxBytes.ToKzUInt256(), i.n, new KzScript(i.scriptPub));
            foreach (var o in to) r.AddOutP2PKH(o.pubKey, o.value);
            return r;
        }

        public void AddInP2PKH(KzPubKey pubKey, KzAmount value, KzUInt256 txId, int n, KzScript scriptPub, UInt32 sequence = KzTxIn.SEQUENCE_FINAL)
        {
            Vin.Add(KzBTxIn.FromP2PKH(pubKey, value, txId, n, scriptPub, sequence));
        }

        public static KzBTransaction P2PKH(IEnumerable<(KzPubKey pubKey, KzTransaction tx, int n)> from, IEnumerable<(KzPubKey pubKey, long value)> to)
        {
            var r = new KzBTransaction();
            foreach (var i in from) r.AddInP2PKH(i.pubKey, i.tx, i.n);
            foreach (var o in to) r.AddOutP2PKH(o.pubKey, o.value);
            return r;
        }

        public void AddInP2PKH(KzPubKey pubKey, KzTransaction tx, int n, UInt32 sequence = KzTxIn.SEQUENCE_FINAL)
        {
            Vin.Add(KzBTxIn.FromP2PKH(pubKey, tx, n, sequence));
        }

        public void AddIn((KzTransaction tx, KzTxOut o, int i) txOut)
        {
            throw new NotImplementedException();
        }

        public void AddIn(KzOutPoint prevout, KzScript scriptSig, UInt32 sequence = KzTxIn.SEQUENCE_FINAL)
        {
            throw new NotImplementedException();
            //Vin.Add(new KzTxInBuilder { Prevout = prevout, ScriptSig., sequence));
        }

        public void AddOutP2PKH(KzPubKey pubKey, KzAmount value)
        {
            Vout.Add(KzBTxOut.ToP2PKH(pubKey, value));
        }

        public void AddOut(KzScript scriptPubKey, long nValue)
        {
            throw new NotImplementedException();
            //Vout.Add(new KzTxOut(nValue, scriptPubKey));
        }

        public KzTransaction ToTransaction() => new KzTransaction(this);

        public ReadOnlySequence<byte> ToReadOnlySequence()
        {
            throw new NotImplementedException();
            //return new ReadOnlySequence<byte>();
        }

        public ReadOnlySequence<byte> ToSequence()
        {
            throw new NotImplementedException();
            //return new ReadOnlySequence<byte>();
        }

        /// <summary>
        /// Find all the OP_RETURN outputs followed by a matching protocol identifier pushdata (20 bytes long).
        /// For each match, return the KzBTxOut and a trimmed array the remaining ScriptPub KzBOp's.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public IEnumerable<(KzBTxOut o, KzBOp[] data)> FindPushDataByProtocol(KzUInt160 protocol)
        {
            var val = protocol.ToBytes();

            foreach (var o in Vout) {
                var ops = o.ScriptPub.Ops;
                if (ops.Count > 2
                    && ops[0].Op.Code == KzOpcode.OP_RETURN
                    && ops[1].Op.Code == KzOpcode.OP_PUSH20
                    && ops[1].Op.Data.Sequence.CompareTo(val) == 0)
                    yield return (o, ops.Skip(2).ToArray());
            }
        }

        public bool CheckSignatures(IEnumerable<KzPrivKey> privKeys = null) {
            return Sign(privKeys, confirmExistingSignatures: true);
        }

        public bool Sign(IEnumerable<KzPrivKey> privKeys = null, bool confirmExistingSignatures = false)
        {
            var signedOk = true;
            var sigHashType = new KzSigHashType(KzSigHash.ALL | KzSigHash.FORKID);
            var tx = ToTransaction();
            var nIn = -1;
            foreach (var i in Vin) {
                nIn++;
                var scriptSig = i.ScriptSig;
                if (scriptSig.Ops.Count == 2) {
                    var pubKey = new KzPubKey();
                    pubKey.Set(scriptSig.Ops[1].Op.Data.ToSpan());
                    if (pubKey.IsValid) {
                        var privKey = i.PrivKey ?? privKeys?.FirstOrDefault(k => k.GetPubKey() == pubKey);
                        if (privKey != null) {
                            var value = i.Value ?? i.PrevOutTx?.Vout[i.PrevOutN].Value ?? 0L;
                            var sigHash = KzScriptInterpreter.ComputeSignatureHash(i.ScriptPub, tx, nIn, sigHashType, value, KzScriptFlags.ENABLE_SIGHASH_FORKID);
                            var (ok, sig) = privKey.Sign(sigHash);
                            if (ok) {
                                var sigWithType = new byte[sig.Length + 1];
                                sig.CopyTo(sigWithType.AsSpan());
                                sigWithType[^1] = (byte)sigHashType.rawSigHashType;
                                var op = KzOp.Push(sigWithType.AsSpan());
                                if (confirmExistingSignatures)
                                    signedOk &= op == scriptSig.Ops[0].Op;
                                else
                                    scriptSig.Ops[0] = op;
                            } else signedOk = false;
                        } else signedOk = false;
                    } else signedOk = false;
                }
            }
            return signedOk;
#if false
            var tx = txb.ToTransaction();
                var bytes = tx.ToBytes();
                var hex = bytes.ToHex();

                var (ok, sig) = privKey1h11.Sign(sigHash);
            //var (ok, sig) = privKey1h11.SignCompact(sigHash);
            if (ok) {
                var sigWithType = new byte[sig.Length + 1];
                sig.CopyTo(sigWithType.AsSpan());
                sigWithType[^1] = (byte)sigHashType.rawSigHashType;
                txb.Vin[0].ScriptSig.Ops[0] = KzOp.Push(sigWithType.AsSpan());
            }
#endif
        }
    }


}
