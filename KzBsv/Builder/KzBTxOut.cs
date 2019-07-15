#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
namespace KzBsv
{
    public class KzBTxOut
    {
        public KzAmount Value;
        public KzBScript ScriptPub = new KzBScript();
        
        public KzPubKey PubKey;

        public KzBTxOut() { }

        public KzBTxOut(KzTxOut txOut)
        {
            Value = txOut.Value;
            ScriptPub.Set(txOut.ScriptPubKey);
        }

        public static KzBTxOut ToP2PKH(KzPubKey pubKey, KzAmount value)
        {
            var pub = KzBScript.NewPubP2PKH(pubKey.ToHash160());

            var r = new KzBTxOut {
                Value = value,
                ScriptPub = pub,
                PubKey = pubKey
            };
            return r;
        }

        public KzTxOut ToTxOut()
        {
            return new KzTxOut(Value, ScriptPub);
        }
    }


}
