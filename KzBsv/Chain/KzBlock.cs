#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace KzBsv
{

    public class KzBlock : KzBlockHeader
    {
        KzTransaction[] _txs;

        public KzTransaction[] Txs => _txs;

        public KzBlock() { }

        public KzBlock(
            KzTransaction[] txs,
            Int32 version,
            KzUInt256 hashPrevBlock,
            KzUInt256 hashMerkleRoot,
            UInt32 time,
            UInt32 bits,
            UInt32 nonce
            ) : base(
                    version,
                    hashPrevBlock,
                    hashMerkleRoot,
                    time,
                    bits,
                    nonce
                )
        {
            _txs = txs;
        }

        public bool TryReadBlock(ref ReadOnlySequence<byte> ros)
        {
            var r = new SequenceReader<byte>(ros);
            if (!TryReadBlock(ref r)) goto fail;

            ros = ros.Slice(r.Consumed);

            return true;
        fail:
            return false;
        }

        public bool TryReadBlock(ref SequenceReader<byte> r)
        {
            if (!TryReadBlockHeader(ref r)) goto fail;

            if (!r.TryReadVarint(out long count)) goto fail;

            _txs = new KzTransaction[count];

            for (var i = 0L; i < count; i++)
            {
                var t = new KzTransaction();
                _txs[i] = t;
                if (!t.TryReadTransaction(ref r)) goto fail;
            }

            if (!VerifyMerkleRoot()) goto fail;

            return true;
        fail:
            return false;
        }

        KzUInt256 ComputeMerkleRoot() => KzMerkleTree.ComputeMerkleRoot(_txs);

        bool VerifyMerkleRoot() => ComputeMerkleRoot() == HashMerkleRoot;

        public IEnumerable<(KzTransaction tx, KzTxOut o, int i)> GetOutputsSendingToAddresses(KzUInt160[] addresses)
        {
            var v = new KzUInt160();
            foreach (var tx in Txs) {
                foreach (var o in tx.Vout) {
                    foreach (var op in o.ScriptPubKey.Decode()) {
                        if (op.Code == KzOpcode.OP_PUSH20) {
                            op.Data.ToSpan().CopyTo(v.Span);
                            var i = Array.BinarySearch<KzUInt160>(addresses, v);
                            if (i >= 0) {
                                yield return (tx, o, i);
                            }
                        }
                    }
                }
            }
        }

    }
}
