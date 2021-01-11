#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
namespace KzBsv
{
    public class KzB58ExtPubKey : KzB58Data
    {
        public void SetKey(KzExtPubKey pubKey)
        {
            var prefix = Kz.EXT_PUBLIC_KEY;
            var data = new byte[prefix.Length + KzExtKey.BIP32_EXTKEY_SIZE];
            prefix.CopyTo(data.Slice(0, prefix.Length));
            pubKey.Encode(data.Slice(prefix.Length));
            SetData(data, prefix.Length);
        }

        public KzExtPubKey GetKey()
        {
            var pubKey = new KzExtPubKey();
            if (Data.Length == KzExtKey.BIP32_EXTKEY_SIZE) {
                pubKey.Decode(Data);
            }
            return pubKey;
        }

        public bool SetString(string b58) => base.SetString(b58, Kz.EXT_PUBLIC_KEY.Length);

        public KzB58ExtPubKey() { }
        public KzB58ExtPubKey(KzExtPubKey pubKey) { SetKey(pubKey); }
        public KzB58ExtPubKey(string b58) { SetString(b58); }

        public static KzExtPubKey GetKey(string b58) => new KzB58ExtPubKey(b58).GetKey();
    }
}
