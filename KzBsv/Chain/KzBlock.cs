#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace KzBsv
{

    /// <summary>
    /// Closely mirrors the data and layout of a serialized Bitcoin block.
    /// Focus is on efficiency when processing large blocks.
    /// In particular, script data is stored as <see cref="ReadOnlySequence{Byte}"/> allowing large scripts to
    /// remain in whatever buffers were originally used. No script parsing data is maintained. 
    /// Not intended for making dynamic changes to a block (mining).
    /// </summary>
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

        public bool TryParseBlock(ref ReadOnlySequence<byte> ros, int height, IKzBlockParser bp)
        {
            var r = new SequenceReader<byte>(ros);
            if (!TryParseBlock(ref r, height, bp)) goto fail;

            ros = ros.Slice(r.Consumed);

            return true;
        fail:
            return false;
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

        public bool TryParseBlock(ref SequenceReader<byte> r, int height, IKzBlockParser bp)
        {
            var offset = r.Consumed;

            if (!TryReadBlockHeader(ref r)) goto fail;

            Height = height;

            bp.BlockStart(this, offset);

            if (!r.TryReadVarint(out long count)) goto fail;

            _txs = new KzTransaction[count];

            for (var i = 0L; i < count; i++)
            {
                var t = new KzTransaction();
                _txs[i] = t;
                if (!t.TryParseTransaction(ref r, bp)) goto fail;
            }

            if (!VerifyMerkleRoot()) goto fail;

            bp.BlockParsed(this, r.Consumed);

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
                    foreach (var op in o.ScriptPub.Decode()) {
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
