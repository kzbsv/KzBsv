#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.IO;
using Xunit;
using System.Linq;
using KzBsv;

namespace Tests.KzBsv
{
    public class KzPubKeyTests
    {
        [Fact]
        public void FromPrivKey()
        {
            var privhex = "906977a061af29276e40bf377042ffbde414e496ae2260bbf1fa9d085637bfff";
            var pubhex = "02a1633cafcc01ebfb6d78e39f687a1f0995c62fc95f51ead10a02ee0be551b5dc";

            var privkey = KzPrivKey.FromHex(privhex);
            var pubkey = privkey.GetPubKey();
            Assert.Equal(privhex, privkey.ToHex());
            Assert.Equal(pubhex, pubkey.ToHex());
            Assert.True(privkey.VerifyPubKey(pubkey));
        }
    }
}
