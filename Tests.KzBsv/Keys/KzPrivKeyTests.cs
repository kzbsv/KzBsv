#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.IO;
using Xunit;
using System.Linq;
using KzBsv;
using System.Text;

namespace Tests.KzBsv.Keys
{
    public class KzPrivKeyTests
    {
        [Fact]
        public void FromHexAndB58()
        {
            var hex = "906977a061af29276e40bf377042ffbde414e496ae2260bbf1fa9d085637bfff";
            var b58 = "L24Rq5hPWMexw5mQi7tchYw6mhtr5ApiHZMN8KJXCkskEv7bTV61";

            var key1 = new KzPrivKey(hex);
            var key2 = KzPrivKey.FromB58(b58);
            Assert.Equal(key1, key2);
            Assert.Equal(hex, key1.ToHex());
            Assert.Equal(b58, key1.ToB58().ToString());
            Assert.Equal(b58, key1.ToString());
            Assert.Equal(hex, key2.ToHex());
            Assert.Equal(b58, key2.ToB58().ToString());
            Assert.Equal(b58, key2.ToString());
        }

        const string strSecret1 = "5HxWvvfubhXpYYpS3tJkw6fq9jE9j18THftkZjHHfmFiWtmAbrj";
        const string strSecret2 = "5KC4ejrDjv152FGwP386VD1i2NYc5KkfSMyv1nGy1VGDxGHqVY3";
        const string strSecret1C = "Kwr371tjA9u2rFSMZjTNun2PXXP3WPZu2afRHTcta6KxEUdm1vEw";
        const string strSecret2C = "L3Hq7a8FEQwJkW1M2GNKDW28546Vp5miewcCzSqUD9kCAXrJdS3g";
        const string addr1 = "1QFqqMUD55ZV3PJEJZtaKCsQmjLT6JkjvJ";
        const string addr2 = "1F5y5E5FMc5YzdJtB9hLaUe43GDxEKXENJ";
        const string addr1C = "1NoJrossxPBKfCHuJXT4HadJrXRE9Fxiqs";
        const string addr2C = "1CRj2HyM1CXWzHAXLQtiGLyggNT9WQqsDs";

        const string strAddressBad = "1HV9Lc3sNHZxwj4Zk6fB38tEmBryq2cBiF";

        [Fact]
        public void KzB58PrivKeyTests()
        {
            var bsecret1 = new KzB58PrivKey();
            var bsecret2 = new KzB58PrivKey();
            var bsecret1C = new KzB58PrivKey();
            var bsecret2C = new KzB58PrivKey();
            var baddress1 = new KzB58PrivKey();

            Assert.True(bsecret1.SetString(strSecret1));
            Assert.True(bsecret2.SetString(strSecret2));
            Assert.True(bsecret1C.SetString(strSecret1C));
            Assert.True(bsecret2C.SetString(strSecret2C));
            Assert.False(baddress1.SetString(strAddressBad));

            var key1 = bsecret1.GetKey();
            Assert.False(key1.IsCompressed);
            var key2 = bsecret2.GetKey();
            Assert.False(key2.IsCompressed);
            var key1C = bsecret1C.GetKey();
            Assert.True(key1C.IsCompressed);
            var key2C = bsecret2C.GetKey();
            Assert.True(key2C.IsCompressed);

            var pubkey1 = key1.GetPubKey();
            var pubkey2 = key2.GetPubKey();
            var pubkey1C = key1C.GetPubKey();
            var pubkey2C = key2C.GetPubKey();

            var a = pubkey1.ToAddress();

            Assert.True(key1.VerifyPubKey(pubkey1));
            Assert.False(key1.VerifyPubKey(pubkey1C));
            Assert.False(key1.VerifyPubKey(pubkey2));
            Assert.False(key1.VerifyPubKey(pubkey2C));

            Assert.False(key1C.VerifyPubKey(pubkey1));
            Assert.True(key1C.VerifyPubKey(pubkey1C));
            Assert.False(key1C.VerifyPubKey(pubkey2));
            Assert.False(key1C.VerifyPubKey(pubkey2C));

            Assert.False(key2.VerifyPubKey(pubkey1));
            Assert.False(key2.VerifyPubKey(pubkey1C));
            Assert.True(key2.VerifyPubKey(pubkey2));
            Assert.False(key2.VerifyPubKey(pubkey2C));

            Assert.False(key2C.VerifyPubKey(pubkey1));
            Assert.False(key2C.VerifyPubKey(pubkey1C));
            Assert.False(key2C.VerifyPubKey(pubkey2));
            Assert.True(key2C.VerifyPubKey(pubkey2C));

            for (var n = 0; n < 16; n++) {
                var strMsg = $"Very secret message {n}: 11";
                var hashMsg = KzHashes.HASH256(Encoding.ASCII.GetBytes(strMsg));

                // normal signatures

                var (ok1, sign1) = key1.Sign(hashMsg);
                var (ok1C, sign1C) = key1C.Sign(hashMsg);
                var (ok2, sign2) = key2.Sign(hashMsg);
                var (ok2C, sign2C) = key2C.Sign(hashMsg);
                Assert.True(ok1 && ok1C && ok2 && ok2C);

                Assert.True(pubkey1.Verify(hashMsg, sign1));
                Assert.True(pubkey1.Verify(hashMsg, sign1C));
                Assert.False(pubkey1.Verify(hashMsg, sign2));
                Assert.False(pubkey1.Verify(hashMsg, sign2C));

                Assert.True(pubkey1C.Verify(hashMsg, sign1));
                Assert.True(pubkey1C.Verify(hashMsg, sign1C));
                Assert.False(pubkey1C.Verify(hashMsg, sign2));
                Assert.False(pubkey1C.Verify(hashMsg, sign2C));

                Assert.False(pubkey2.Verify(hashMsg, sign1));
                Assert.False(pubkey2.Verify(hashMsg, sign1C));
                Assert.True(pubkey2.Verify(hashMsg, sign2));
                Assert.True(pubkey2.Verify(hashMsg, sign2C));

                Assert.False(pubkey2C.Verify(hashMsg, sign1));
                Assert.False(pubkey2C.Verify(hashMsg, sign1C));
                Assert.True(pubkey2C.Verify(hashMsg, sign2));
                Assert.True(pubkey2C.Verify(hashMsg, sign2C));

                // compact signatures (with key recovery)

                var (cok1, csign1) = key1.SignCompact(hashMsg);
                var (cok1C, csign1C) = key1C.SignCompact(hashMsg);
                var (cok2, csign2) = key2.SignCompact(hashMsg);
                var (cok2C, csign2C) = key2C.SignCompact(hashMsg);
                Assert.True(cok1 && cok1C && cok2 && cok2C);

                var (rok1, rkey1) = KzPubKey.FromRecoverCompact(hashMsg, csign1);
                var (rok2, rkey2) = KzPubKey.FromRecoverCompact(hashMsg, csign2);
                var (rok1C, rkey1C) = KzPubKey.FromRecoverCompact(hashMsg, csign1C);
                var (rok2C, rkey2C) = KzPubKey.FromRecoverCompact(hashMsg, csign2C);
                Assert.True(rok1 && rok2 && rok1C && rok2C);

                Assert.True(rkey1 == pubkey1);
                Assert.True(rkey2 == pubkey2);
                Assert.True(rkey1C == pubkey1C);
                Assert.True(rkey2C == pubkey2C);
            }
        }
    }
}
