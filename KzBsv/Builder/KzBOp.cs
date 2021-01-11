#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    /// <summary>
    /// The KzScriptBuilder maintains a list of builder Ops "BOps".
    /// The primary reason is to allow raw bytes to be added to a script.
    /// </summary>
    public struct KzBOp
    {
        /// <summary>
        /// true if no changes to this operation will happen
        /// false if this operation still needs to be changed
        /// false is typically used for data placeholders
        /// </summary>
        public bool IsFinal;

        /// <summary>
        /// If IsRaw is true, ignore Op.Code and just add Op.Data bytes to script.
        /// </summary>
        public bool IsRaw;

        /// <summary>
        /// KzOp is the standard script opcode plus data bytes struct.
        /// If IsRaw is true, ignore Op.Code and just add Op.Data bytes to script.
        /// </summary>
        public KzOp Op;

        public KzBOp(KzOp op) : this() { IsFinal = true; Op = op; }

        public KzBOp(KzValType data) : this() { IsFinal = true; IsRaw = true; Op = new KzOp(KzOpcode.OP_NOP, data); }

        public long Length => IsRaw ? Op.Data.Length : Op.Length;

        public static implicit operator KzBOp(KzOp op)
        {
            return new KzBOp { IsFinal = true,  Op = op };
        }

        public bool TryCopyTo(ref Span<byte> span)
        {
            if (IsRaw) {
                var len = (int)Length;
                if (len > span.Length) goto fail;
                Op.Data.Sequence.CopyTo(span.Slice(0, len));
                span = span.Slice(len);
            } else {
                if (!Op.TryCopyTo(ref span)) goto fail;
            }
            return true;

        fail:
            return false;
        }

        public string ToVerboseString()
        {
            if (IsRaw)
                return Kz.Hex.Encode(Op.Data.Sequence);

            return Op.ToVerboseString();
        }

        public override string ToString()
        {
            return ToVerboseString();
        }

        public KzPubKey ToPubKey() {
            var pubKey = new KzPubKey();
            pubKey.Set(Op.Data.ToSpan());
            return pubKey.IsValid ? pubKey : null;
        }
    }
}
