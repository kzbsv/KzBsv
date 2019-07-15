#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.IO;
using Xunit;
using KzBsv;

namespace Tests.KzBsv
{
    public class KzMnemonicTests
    {
        [Fact]
        public void Base6AndBase10()
        {
            //var e = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var e = new byte[] { 0, 1 };
            var s10 = KzMnemonic.ToDigitsBase10(e);
            var s6 = KzMnemonic.ToDigitsBase6(e);
            var bn10 = KzMnemonic.Base10ToBigInteger(s10);
            var bn6 = KzMnemonic.Base6ToBigInteger(s6);
        }

        [Fact]
        public void WordListsComplete()
        {
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.English].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.English][0] == "abandon");
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.Spanish].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.Spanish][0] == "ábaco");
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.French].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.French][0] == "abaisser");
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.Italian].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.Italian][0] == "abaco");
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.Japanese].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.Japanese][0] == "あいこくしん");
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.PortugueseBrazil].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.PortugueseBrazil][0] == "abacate");
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.ChineseSimplified].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.ChineseSimplified][0] == "的");
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.ChineseTraditional].Length == 2048);
            Assert.True(KzMnemonic.WordLists[KzMnemonic.Languages.ChineseTraditional][0] == "的");
        }

        [Fact]
        public void IsValid()
        {
            Assert.True(KzMnemonic.IsValid("afirmar diseño hielo fideo etapa ogro cambio fideo toalla pomelo número buscar"));

            Assert.False(KzMnemonic.IsValid("afirmar diseño hielo fideo etapa ogro cambio fideo hielo pomelo número buscar"));

            Assert.False(KzMnemonic.IsValid("afirmar diseño hielo fideo etapa ogro cambio fideo hielo pomelo número oneInvalidWord"));

            Assert.False(KzMnemonic.IsValid("totally invalid phrase"));

            Assert.True(KzMnemonic.IsValid("caution opprimer époque belote devenir ficeler filleul caneton apologie nectar frapper fouiller"));
        }

        [Fact]
        public void Constructors()
        {
            var words = "afirmar diseño hielo fideo etapa ogro cambio fideo toalla pomelo número buscar";
            var m1 = new KzMnemonic(words);
            Assert.Equal(KzMnemonic.Languages.Spanish, m1.Language);
            Assert.Equal(m1.Words, KzMnemonic.FromWords(words).Words);

            var m2 = new KzMnemonic(m1.Entropy, m1.Language);
            Assert.Equal(m1.Words, m2.Words);
            Assert.Equal(m2.Words, KzMnemonic.FromEntropy(m1.Entropy, m1.Language).Words);

            var m3 = new KzMnemonic(new byte[] { 5, 40, 161, 175, 172, 69, 19, 67, 74, 26, 196, 233, 87, 10, 119, 18 }, KzMnemonic.Languages.Spanish);
            Assert.Equal(m1.Words, m3.Words);

            var m4 = new KzMnemonic(length:256);
            Assert.Equal(24, m4.Words.Split(' ').Length);
            Assert.Equal(24, KzMnemonic.FromLength(256).Words.Split(' ').Length);

        }

        [Fact]
        public void WordListLength()
        {
            Assert.Equal(12, new KzMnemonic(32 * 4).Words.Split(' ').Length);
            Assert.Equal(15, new KzMnemonic(32 * 5).Words.Split(' ').Length);
            Assert.Equal(18, new KzMnemonic(32 * 6).Words.Split(' ').Length);
            Assert.Equal(21, new KzMnemonic(32 * 7).Words.Split(' ').Length);
            Assert.Equal(24, new KzMnemonic(32 * 8).Words.Split(' ').Length);
        }

        [Fact]
        public void ToStringIsWords()
        {
            var m = new KzMnemonic();
            Assert.Equal(m.Words, m.ToString());
        }

        [Fact]
        public void FromBase6()
        {
            var rolls1 = "10000000000000000000000000000000000000000000000002";
            var m1 = KzMnemonic.FromBase6(rolls1);
            Assert.Equal("acoustic abandon abandon abandon anchor cancel pole advance naive alpha noodle slogan", m1.Words);

            var rolls2 = "20433310335200331223501035145525323501554453150402";
            var m2 = KzMnemonic.FromBase6(rolls2);
            Assert.Equal("little jar barrel spatial tenant business manual cabin pig nerve trophy purity", m2.Words);

            var rolls3 = "2043331033520033122350103533025405142024330443100234401130333301433333523345145525323501554453150402";
            var m3 = KzMnemonic.FromBase6(rolls3, 256);
            Assert.Equal("little jar crew spice goat sell journey behind used choose eyebrow property audit firm later blind invite fork camp shock floor reduce submit bronze", m3.Words);
        }
    }
}
