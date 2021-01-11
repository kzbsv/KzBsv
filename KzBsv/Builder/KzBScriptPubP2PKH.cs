#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
namespace KzBsv
{
    public class KzBScriptPubP2PKH : KzBScript
    {
        public KzBScriptPubP2PKH(KzUInt160 pubKeyHash)
        {
            IsPub = true;
            _TemplateId = KzScriptTemplateId.P2PKH;
            this
                .Add(KzOpcode.OP_DUP)
                .Add(KzOpcode.OP_HASH160)
                .Push(pubKeyHash.Span)
                .Add(KzOpcode.OP_EQUALVERIFY)
                .Add(KzOpcode.OP_CHECKSIG)
                ;
        }
    }

    public class KzBScriptSigP2PKH : KzBScript
    {
        public KzBScriptSigP2PKH(KzPubKey pubKey)
        {
            IsPub = false;
            _TemplateId = KzScriptTemplateId.P2PKH;
            this
                .Push(new byte[72]) // This will become the CHECKSIG signature
                .Push(pubKey.Span)
                ;
        }
    }
}
