#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace KzBsv
{
    /// <summary>
    /// 
    /// This algorithm is incrementally efficient, the worst case cost of obtaining an incremental root hash
    /// is O(tree_height), not O(tx_count * log(tree_height)).
    /// 
    /// There is no protection currently from CVE-2012-2459 vulnerability (duplicated pairs of transactions).
    ///
    /// </summary>
    public class KzMerkleTree : IDisposable
    {

        public static KzUInt256 ComputeMerkleRoot(IEnumerable<KzTransaction> txs)
        {
            using (var mt = new KzMerkleTree())
            {
                mt.AddTransactions(txs);
                return mt.GetMerkleRoot();
            }
        }

        /// <summary>
        /// As transactions are added to a merkle tree, the path of left and right branches from the
        /// root of the tree to each transaction cycle through permutations just as the digits of a
        /// binary counter.
        /// The digits of the binary number given by the node count minus one is the path to the next
        /// node to be added.
        /// 0 - First transactions. All branches to the left.
        /// 1 - Second transaction, level 1 branch to the right.
        /// 10 - Third transaction, level 2 branch to the right, level 1 to the left.
        /// </summary>
        class KzMerkleTreeNode
        {
            /// <summary>
            /// Hash of left sub-tree (or transaction hash of leaf node, level 0).
            /// </summary>
            public KzUInt256 LeftHash;

            /// <summary>
            /// Hash of right sub-tree (or transaction hash of leaf node, level 0).
            /// THIS PROPERTY MUST BE IMMEDIATELY AFTER LeftHash
            /// </summary>
            public KzUInt256 RightHash;

            public Span<byte> LeftRightHashes {
                get {
                    unsafe {
                        fixed (KzUInt256* p = &LeftHash) {
                            byte* pb = (byte*)p;
                            var bytes = new Span<byte>(pb, 64);
                            return bytes;
                        }
                    }
                }
            }

            /// <summary>
            /// Reference to node for the tree level above this one or null.
            /// </summary>
            public KzMerkleTreeNode Parent;

            /// <summary>
            /// LeftOfParent and RightOfParent indicate whether this node is currently tracking a subtree
            /// to the left or right of its parent node.
            /// </summary>
            public bool LeftOfParent { get { return !RightOfParent; } set { RightOfParent = !value; } }
            public bool RightOfParent;

            /// <summary>
            /// HasLeft, HasRight, HasBoth indicate whether valid subtree hashes have been copied to this node's
            /// LeftHash, RightHash, and Hashes properties.
            /// </summary>
            public bool HasLeft;
            public bool HasRight;
            public bool HasBoth { get { return HasLeft && HasRight; } }

            /// <summary>
            /// New nodes are always created on the left branch from their parent node (when they get one).
            /// They always start with a valid left hash (either Tx hash or left subtree hash.
            /// They always start without a valid right hash.
            /// </summary>
            public KzMerkleTreeNode(KzUInt256 newLeftHash, KzMerkleTreeNode child)
            {
                SetLeftHash(newLeftHash);
                if (child != null)
                    child.Parent = this;
            }

            public void SetLeftHash(KzUInt256 hash)
            {
                LeftHash = hash;
                HasLeft = true;
            }

            public void SetRightHash(KzUInt256 hash)
            {
                RightHash = hash;
                HasRight = true;
            }
        }

        long _count = 0;
        List<KzMerkleTreeNode> _nodes = new List<KzMerkleTreeNode>();
        SHA256 _sha256;

        public KzMerkleTree()
        {
            _sha256 = SHA256.Create();
        }

        public void AddTransactions(IEnumerable<KzTransaction> txs)
        {
            foreach (var tx in txs) AddTransaction(tx);
        }

        /// <summary>
        /// Update the incremental state by one additional transaction hash.
        /// This creates at most one KzMerkleTreeNode per level of the tree.
        /// These are reused as subtrees fill up.
        /// </summary>
        /// <param name="tx"></param>
        void AddTransaction(KzTransaction tx)
        {
            _count++;
            var newHash = tx.TxId;
            if (_count == 1)
            {
                // First transaction.
                _nodes.Add(new KzMerkleTreeNode(newHash, null));
            } else
            {
                var n = _nodes[0];
                if (n.HasBoth)
                {
                    // Reuse previously filled nodes.
                    var n0 = n;
                    while (n?.HasBoth == true)
                    {
                        n.RightOfParent = !n.RightOfParent;
                        n.HasRight = false;
                        n.HasLeft = false;
                        n = n.Parent;
                    }
                    n0.SetLeftHash(newHash);
                }
                else
                {
                    // Complete leaf node, compute completed hashes and propagate upwards.
                    n.SetRightHash(newHash);
                    do
                    {
                        newHash = ComputeHash(n);
                        var np = n.Parent;
                        if (np == null)
                        {
                            _nodes.Add(new KzMerkleTreeNode(newHash, n));
                            break;
                        }
                        if (n.LeftOfParent)
                            np.SetLeftHash(newHash);
                        else
                            np.SetRightHash(newHash);
                        n = np;
                    } while (n.HasBoth);
                }
            }
        }

        KzUInt256 ComputeHash(KzMerkleTreeNode n)
        {
            // This ToArray call could be eliminated.
            var h = new KzUInt256();
            KzHashes.HASH256(n.LeftRightHashes, h.Span);
            return h;
        }

        /// <summary>
        /// Compute the full merkle tree root hash from the incremental state.
        /// Typically called after adding all the available transactions.
        /// Propagates hashes upwards from incomplete subtrees by copying left subtree hash when needed.
        /// Note that this copying leads to a vulnerability: CVE-2012-2459
        /// </summary>
        /// <returns></returns>
        public KzUInt256 ComputeHashMerkleRoot()
        {
            if (_count == 0)
                return KzUInt256.Zero;

            var n = _nodes[0];

            if (_count == 1)
                return n.LeftHash;

            Debug.Assert(!_nodes.Last().HasBoth);

            // Skip complete subtrees...
            while (n.HasBoth) n = n.Parent;

            // If only the last node is incomplete then
            // the whole left subtree is complete,
            // and there's nothing in the right subtree.
            if (n.Parent == null)
                return n.LeftHash;

            // Don't alter incremental state of tree hashes when computing partial results.
            var hasBoth = false;

            KzUInt256 newHash;

            do {
                if (!hasBoth)
                    n.LeftHash.Span.CopyTo(n.RightHash.Span);
                newHash = ComputeHash(n);
                var np = n.Parent;
                if (np != null)
                {
                    if (n.LeftOfParent)
                    {
                        np.LeftHash = newHash;
                        hasBoth = false;
                    }
                    else
                    {
                        np.RightHash = newHash;
                        hasBoth = true;
                    }
                }
                n = np;
            } while (n != null);

            return newHash;
        }

        public KzUInt256 GetMerkleRoot()
        {
            return ComputeHashMerkleRoot();
        }

        #region IDisposable Support
        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _sha256.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~KzMerkleTree() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}