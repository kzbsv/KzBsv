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
    public class KzBase58Tests
    {
        class TC { public bool ok; public string hex; public string b58; }

        TC[] tcs = new TC[] {
            new TC { ok = true, hex = "", b58 = "" },
            new TC { ok = true, hex = "61", b58 = "2g" },
            new TC { ok = true, hex = "626262", b58 = "a3gV" },
            new TC { ok = true, hex = "636363", b58 = "aPEr" },
            new TC { ok = true, hex = "73696d706c792061206c6f6e6720737472696e67", b58 = "2cFupjhnEsSn59qHXstmK2ffpLv2" },
            new TC { ok = true, hex = "00eb15231dfceb60925886b67d065299925915aeb172c06647", b58 = "1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L" },
            new TC { ok = true, hex = "516b6fcd0f", b58 = "ABnLTmg" },
            new TC { ok = true, hex = "bf4f89001e670274dd", b58 = "3SEo3LWLoPntC" },
            new TC { ok = true, hex = "572e4794", b58 = "3EFU7m" },
            new TC { ok = true, hex = "ecac89cad93923c02321", b58 = "EJDM8drfXA6uyA" },
            new TC { ok = true, hex = "10c8511e", b58 = "Rt5zm" },
            new TC { ok = true, hex = "00000000000000000000", b58 = "1111111111" }
        };

        [Fact]
        public void TestCases()
        {
            var h = KzEncoders.Hex;
            var e = KzEncoders.B58;
            var buf = new byte[256];
            foreach (var tc in tcs) {
                var hex = h.Decode(tc.hex);
                var span = buf.AsSpan();
                var ok = e.TryDecode(tc.b58, ref span);
                Assert.Equal(tc.ok, ok);
                if (ok) {
                    Assert.Equal(hex, span.ToArray());
                    Assert.Equal(tc.b58, e.Encode(span));
                }
            }
        }
    }

    public class KzBase58CheckTests
    {
        class TC { public bool ok; public byte ver; public string hex; public string b58; }

        TC[] tcs = new TC[] {
            new TC { ok = false, b58 = "1AGNa15ZQXAZagFiqJ2i7Z2DPU2J6hW62i", hex = "65a16059864a2fdbc7c99a4723a8395bc6f188eb", ver = 0 },
            new TC { ok = true, b58 = "1AGNa15ZQXAZUgFiqJ2i7Z2DPU2J6hW62i", hex = "65a16059864a2fdbc7c99a4723a8395bc6f188eb", ver = 0 },
            new TC { ok = true, b58 = "3CMNFxN1oHBc4R1EpboAL5yzHGgE611Xou", hex = "74f209f6ea907e2ea48f74fae05782ae8a665257", ver = 5 },
            new TC { ok = true, b58 = "mo9ncXisMeAoXwqcV5EWuyncbmCcQN4rVs", hex = "53c0307d6851aa0ce7825ba883c6bd9ad242b486", ver = 111 },
            new TC { ok = true, b58 = "2N2JD6wb56AfK4tfmM6PwdVmoYk2dCKf4Br", hex = "6349a418fc4578d10a372b54b45c280cc8c4382f", ver = 196 },
            new TC { ok = true, b58 = "5Kd3NBUAdUnhyzenEwVLy9pBKxSwXvE9FMPyR4UKZvpe6E3AgLr", hex = "eddbdc1168f1daeadbd3e44c1e3f8f5a284c2029f78ad26af98583a499de5b19", ver = 128 },
            new TC { ok = true, b58 = "9213qJab2HNEpMpYNBa7wHGFKKbkDn24jpANDs2huN3yi4J11ko", hex = "36cb93b9ab1bdabf7fb9f2c04f1b9cc879933530ae7842398eef5a63a56800c2", ver = 239 },
            new TC { ok = true, b58 = "1Ax4gZtb7gAit2TivwejZHYtNNLT18PUXJ", hex = "6d23156cbbdcc82a5a47eee4c2c7c583c18b6bf4", ver = 0 },
            new TC { ok = true, b58 = "3QjYXhTkvuj8qPaXHTTWb5wjXhdsLAAWVy", hex = "fcc5460dd6e2487c7d75b1963625da0e8f4c5975", ver = 5 },
            new TC { ok = true, b58 = "n3ZddxzLvAY9o7184TB4c6FJasAybsw4HZ", hex = "f1d470f9b02370fdec2e6b708b08ac431bf7a5f7", ver = 111 },
            new TC { ok = true, b58 = "2NBFNJTktNa7GZusGbDbGKRZTxdK9VVez3n", hex = "c579342c2c4c9220205e2cdc285617040c924a0a", ver = 196 },
            new TC { ok = true, b58 = "5K494XZwps2bGyeL71pWid4noiSNA2cfCibrvRWqcHSptoFn7rc", hex = "a326b95ebae30164217d7a7f57d72ab2b54e3be64928a19da0210b9568d4015e", ver = 128 },
            new TC { ok = true, b58 = "93DVKyFYwSN6wEo3E2fCrFPUp17FtrtNi2Lf7n4G3garFb16CRj", hex = "d6bca256b5abc5602ec2e1c121a08b0da2556587430bcf7e1898af2224885203", ver = 239 },
            new TC { ok = true, b58 = "1C5bSj1iEGUgSTbziymG7Cn18ENQuT36vv", hex = "7987ccaa53d02c8873487ef919677cd3db7a6912", ver = 0 },
            new TC { ok = true, b58 = "3AnNxabYGoTxYiTEZwFEnerUoeFXK2Zoks", hex = "63bcc565f9e68ee0189dd5cc67f1b0e5f02f45cb", ver = 5 },
            new TC { ok = true, b58 = "n3LnJXCqbPjghuVs8ph9CYsAe4Sh4j97wk", hex = "ef66444b5b17f14e8fae6e7e19b045a78c54fd79", ver = 111 },
            new TC { ok = true, b58 = "2NB72XtkjpnATMggui83aEtPawyyKvnbX2o", hex = "c3e55fceceaa4391ed2a9677f4a4d34eacd021a0", ver = 196 },
            new TC { ok = true, b58 = "5KaBW9vNtWNhc3ZEDyNCiXLPdVPHCikRxSBWwV9NrpLLa4LsXi9", hex = "e75d936d56377f432f404aabb406601f892fd49da90eb6ac558a733c93b47252", ver = 128 },
            new TC { ok = true, b58 = "927CnUkUbasYtDwYwVn2j8GdTuACNnKkjZ1rpZd2yBB1CLcnXpo", hex = "44c4f6a096eac5238291a94cc24c01e3b19b8d8cef72874a079e00a242237a52", ver = 239 },
            new TC { ok = true, b58 = "1Gqk4Tv79P91Cc1STQtU3s1W6277M2CVWu", hex = "adc1cc2081a27206fae25792f28bbc55b831549d", ver = 0 },
            new TC { ok = true, b58 = "33vt8ViH5jsr115AGkW6cEmEz9MpvJSwDk", hex = "188f91a931947eddd7432d6e614387e32b244709", ver = 5 },
            new TC { ok = true, b58 = "mhaMcBxNh5cqXm4aTQ6EcVbKtfL6LGyK2H", hex = "1694f5bc1a7295b600f40018a618a6ea48eeb498", ver = 111 },
            new TC { ok = true, b58 = "2MxgPqX1iThW3oZVk9KoFcE5M4JpiETssVN", hex = "3b9b3fd7a50d4f08d1a5b0f62f644fa7115ae2f3", ver = 196 },
            new TC { ok = true, b58 = "5HtH6GdcwCJA4ggWEL1B3jzBBUB8HPiBi9SBc5h9i4Wk4PSeApR", hex = "091035445ef105fa1bb125eccfb1882f3fe69592265956ade751fd095033d8d0", ver = 128 },
            new TC { ok = true, b58 = "92xFEve1Z9N8Z641KQQS7ByCSb8kGjsDzw6fAmjHN1LZGKQXyMq", hex = "b4204389cef18bbe2b353623cbf93e8678fbc92a475b664ae98ed594e6cf0856", ver = 239 },
            new TC { ok = true, b58 = "1JwMWBVLtiqtscbaRHai4pqHokhFCbtoB4", hex = "c4c1b72491ede1eedaca00618407ee0b772cad0d", ver = 0 },
            new TC { ok = true, b58 = "3QCzvfL4ZRvmJFiWWBVwxfdaNBT8EtxB5y", hex = "f6fe69bcb548a829cce4c57bf6fff8af3a5981f9", ver = 5 },
            new TC { ok = true, b58 = "mizXiucXRCsEriQCHUkCqef9ph9qtPbZZ6", hex = "261f83568a098a8638844bd7aeca039d5f2352c0", ver = 111 },
            new TC { ok = true, b58 = "2NEWDzHWwY5ZZp8CQWbB7ouNMLqCia6YRda", hex = "e930e1834a4d234702773951d627cce82fbb5d2e", ver = 196 },
            new TC { ok = true, b58 = "5KQmDryMNDcisTzRp3zEq9e4awRmJrEVU1j5vFRTKpRNYPqYrMg", hex = "d1fab7ab7385ad26872237f1eb9789aa25cc986bacc695e07ac571d6cdac8bc0", ver = 128 },
            new TC { ok = true, b58 = "91cTVUcgydqyZLgaANpf1fvL55FH53QMm4BsnCADVNYuWuqdVys", hex = "037f4192c630f399d9271e26c575269b1d15be553ea1a7217f0cb8513cef41cb", ver = 239 },
            new TC { ok = true, b58 = "19dcawoKcZdQz365WpXWMhX6QCUpR9SY4r", hex = "5eadaf9bb7121f0f192561a5a62f5e5f54210292", ver = 0 },
            new TC { ok = true, b58 = "37Sp6Rv3y4kVd1nQ1JV5pfqXccHNyZm1x3", hex = "3f210e7277c899c3a155cc1c90f4106cbddeec6e", ver = 5 },
            new TC { ok = true, b58 = "myoqcgYiehufrsnnkqdqbp69dddVDMopJu", hex = "c8a3c2a09a298592c3e180f02487cd91ba3400b5", ver = 111 },
            new TC { ok = true, b58 = "2N7FuwuUuoTBrDFdrAZ9KxBmtqMLxce9i1C", hex = "99b31df7c9068d1481b596578ddbb4d3bd90baeb", ver = 196 },
            new TC { ok = true, b58 = "5KL6zEaMtPRXZKo1bbMq7JDjjo1bJuQcsgL33je3oY8uSJCR5b4", hex = "c7666842503db6dc6ea061f092cfb9c388448629a6fe868d068c42a488b478ae", ver = 128 },
            new TC { ok = true, b58 = "93N87D6uxSBzwXvpokpzg8FFmfQPmvX4xHoWQe3pLdYpbiwT5YV", hex = "ea577acfb5d1d14d3b7b195c321566f12f87d2b77ea3a53f68df7ebf8604a801", ver = 239 },
            new TC { ok = true, b58 = "13p1ijLwsnrcuyqcTvJXkq2ASdXqcnEBLE", hex = "1ed467017f043e91ed4c44b4e8dd674db211c4e6", ver = 0 },
            new TC { ok = true, b58 = "3ALJH9Y951VCGcVZYAdpA3KchoP9McEj1G", hex = "5ece0cadddc415b1980f001785947120acdb36fc", ver = 5 }
        };

        [Fact]
        public void TestCases()
        {
            var h = KzEncoders.Hex;
            var e = KzEncoders.B58Check;
            foreach (var tc in tcs) {
                var hex = h.Decode(tc.hex);
                var (ok, buf) = e.TryDecode(tc.b58);
                Assert.Equal(tc.ok, ok);
                if (ok) {
                    var ver = buf[0];
                    var data = buf.AsSpan().Slice(1).ToArray();
                    Assert.Equal(tc.ver, ver);
                    Assert.Equal(hex, data);
                    Assert.Equal(tc.b58, e.Encode(buf));
                }
            }
        }
    }
}
