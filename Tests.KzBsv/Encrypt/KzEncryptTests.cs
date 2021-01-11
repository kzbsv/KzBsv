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
using System.Security.Cryptography;

namespace Tests.KzBsv.Encrypt
{
    public class KzEncryptTests
    {
        [Fact]
        public void AesEncryptTests()
        {
            var msg = "all good men must act";
            var data1 = msg.UTF8ToBytes();
            var password = "really strong password...;-)";

            var key = KzEncrypt.KeyFromPassword(password);

            {
                var edata1 = KzEncrypt.AesEncrypt(data1, key);
                var ddata1 = KzEncrypt.AesDecrypt(edata1, key);
                Assert.Equal(data1, ddata1);
                Assert.Equal(msg, ddata1.ToUTF8());
            }

            {
                var iv = KzEncrypt.GenerateIV(key, data1);
                var edata1 = KzEncrypt.AesEncrypt(data1, key, iv, noIV: true);
                var ddata1 = KzEncrypt.AesDecrypt(edata1, key, iv);
                Assert.Equal(data1, ddata1);
                Assert.Equal(msg, ddata1.ToUTF8());
            }
        }
    }
}
