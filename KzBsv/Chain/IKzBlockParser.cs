#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion

namespace KzBsv {
    public interface IKzBlockParser {
        void BlockStart(KzBlockHeader bh, long offset);
        void BlockParsed(KzBlockHeader bh, long offset);
        void TxStart(KzTransaction t, long offset);
        void TxParsed(KzTransaction t, long offset);
        void TxOutStart(KzTxOut to, long offset);
        void TxOutParsed(KzTxOut to, long offset);
        void TxInStart(KzTxIn ti, long offset);
        void TxInParsed(KzTxIn ti, long offset);
        void ScriptStart(KzScript s, long offset);
        void ScriptParsed(KzScript s, long offset);
    }
}
