#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace KzBsv {

	/// <summary>
	/// BIP39 based support for converting binary data of specific lengths into sequences of words to facilitate written record keeping and verbal transmission.
	/// </summary>
	public partial class KzMnemonic
    {
        static Lazy<Dictionary<Languages, string[]>> _WordLists = null;

        /// <summary>
        /// Lazy initialized Dictionary of standard Languages word lists.
        /// Each word list contains 2048 words and encodes 11 bits of data or checksum per word.
        /// </summary>
        public static Dictionary<Languages, string[]> WordLists => _WordLists.Value;

        static KzMnemonic()
        {
            _WordLists = new Lazy<Dictionary<Languages, string[]>>(() => {
                var wl = new Dictionary<Languages, string[]>();
                LoadWordLists(wl);
                return wl;
            }, true);
        }

        /// <summary>
        /// Space separated word list. Each word encodes 11 bits. Words are all in Language and are contained in WordList.
        /// In addition to encoding Entropy, Words also encodes a checksum to catch transcription errors.
        /// </summary>
        public string Words { get; private set; }
        /// <summary>
        /// What human language is being used by Words and WordList.
        /// </summary>
        public Languages Language { get; private set; }
        /// <summary>
        /// A list of 2048 words. The index of each word, taken as an 11 bit value, is used to encode Entropy and checksum binary data.
        /// </summary>
        public string[] WordList { get; private set; }
        /// <summary>
        /// The binary data for which Words is a transcription safe encoding.
        /// </summary>
        public byte[] Entropy { get; private set; }

        public string ToHex() => KzEncoders.Hex.Encode(Entropy);
        public string ToDigitsBase10() => new BigInteger(Entropy.Concat(new byte[1]).ToArray()).ToString();
        public string ToDigitsBase6() => ToDigitsBase6(Entropy);

        /// <summary>
        /// Create a new KzMnemonic from a desired entropy length in bits.
        /// length should be a multiple of 32.
        /// </summary>
        /// <param name="length">Optional length in bits, default is 128. Should be a multiple of 32.</param>
        /// <param name="language">Optional language to use, default is English.</param>
        public static KzMnemonic FromLength(int length = 128, Languages language = Languages.English) => new KzMnemonic(length, language);
        public static KzMnemonic FromLength(int length, string[] wordList, Languages language = Languages.Unknown) => new KzMnemonic(length, wordList, language);

        /// <summary>
        /// Create a new KzMnemonic from a sequence of words.
        /// </summary>
        /// <param name="words">Space serparated words that encode Entropy and checksum.</param>
        /// <param name="language">Optional language key to use in WordLists.</param>
        public static KzMnemonic FromWords(string words, Languages language = Languages.Unknown) => new KzMnemonic(words, language);
        public static KzMnemonic FromWords(string words, string[] wordList, Languages language = Languages.Unknown) => new KzMnemonic(words, wordList, language);

        /// <summary>
        /// Create a new KzMnemonic from given Entropy.
        /// </summary>
        /// <param name="entropy">Binary data to encode.</param>
        /// <param name="language">Optional language key to select WordList from WordLists. Defaults to English.</param>
        public static KzMnemonic FromEntropy(Span<byte> entropy, Languages language = Languages.English) => new KzMnemonic(entropy, language);
        public static KzMnemonic FromEntropy(Span<byte> entropy, string[] wordList, Languages language = Languages.Unknown) => new KzMnemonic(entropy, wordList, language);

        /// <summary>
        /// Create a new KzMnemonic from given entropy encoded as base 6 string of digits. e.g. Die rolls.
        /// </summary>
        /// <param name="base6">Entropy encoded as base 6 string. Use either digits 1-6 or 0-5.</param>
        /// <param name="length">Target Entropy length in bits.</param>
        /// <param name="language">Optional language key to select WordList from WordLists. Defaults to English.</param>
        public static KzMnemonic FromBase6(string base6, int length = 128, Languages language = Languages.English) => new KzMnemonic(Base6ToEntropy(base6, length), language);
        public static KzMnemonic FromBase6(string base6, int length, string[] wordList, Languages language = Languages.Unknown) => new KzMnemonic(Base6ToEntropy(base6, length), wordList, language);

        public static KzMnemonic FromBase10(string base10, int length = 128, Languages language = Languages.English) => new KzMnemonic(Base10ToEntropy(base10, length), language);
        public static KzMnemonic FromBase10(string base10, int length, string[] wordList, Languages language = Languages.Unknown) => new KzMnemonic(Base10ToEntropy(base10, length), wordList, language);

        /// <summary>
        /// Create a new KzMnemonic from a desired entropy length in bits.
        /// length should be a multiple of 32.
        /// </summary>
        /// <param name="length">Entropy length in bits. Should be a multiple of 32.</param>
        /// <param name="wordList">string[] of 2048 unique words.</param>
        /// <param name="language">optional Languages key to use. Defaults to Unknown.</param>
        public KzMnemonic(int length, string[] wordList, Languages language = Languages.Unknown)
        {
            Entropy = new byte[length / 8];
            KzRandom.GetStrongRandBytes(Entropy);

            Language = language;
            WordList = wordList;
            Words = ConvertDataToWords(Entropy, WordList);
        }

        /// <summary>
        /// Create a new KzMnemonic from a desired entropy length in bits.
        /// length should be a multiple of 32.
        /// </summary>
        /// <param name="length">Optional length in bits, default is 128. Should be a multiple of 32.</param>
        /// <param name="language">Optional language to use, default is english.</param>
        public KzMnemonic(int length = 128, Languages language = Languages.English) : this(length, WordLists[language], language) { } 

        /// <summary>
        /// Create a new KzMnemonic from a sequence of words.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="wordList"></param>
        /// <param name="language"></param>
        public KzMnemonic(string words, string[] wordList, Languages language = Languages.Unknown)
        {
            Words = words.Normalize(NormalizationForm.FormKD);
            if (wordList != null) {
                Language = language;
                WordList = wordList;
            } else if (language != Languages.Unknown) {
                Language = language;
                WordList = WordLists[Language];
            } else
                (Language, WordList) = GetWordList(words);

            Entropy = GetEntropy(Words, WordList);
        }

        /// <summary>
        /// Create a new KzMnemonic from a sequence of words.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="language"></param>
        public KzMnemonic(string words, Languages language = Languages.Unknown) : this(words, null, language) { }

        /// <summary>
        /// Create a new KzMnemonic from given Entropy.
        /// </summary>
        /// <param name="entropy">Binary data to encode.</param>
        /// <param name="wordList"></param>
        /// <param name="language">Optional language key. Defaults to Unknown.</param>
        public KzMnemonic(Span<byte> entropy, string[] wordList, Languages language = Languages.Unknown)
        {
            Entropy = entropy.ToArray();
            Language = language;
            WordList = wordList;
            Words = ConvertDataToWords(Entropy, WordList);
        }

        /// <summary>
        /// Create a new KzMnemonic from given Entropy.
        /// </summary>
        /// <param name="entropy">Binary data to encode.</param>
        /// <param name="language">Optional language key to select WordList from WordLists. Defaults to English.</param>
        public KzMnemonic(Span<byte> entropy, Languages language = Languages.English) : this(entropy, WordLists[language], language) { }

        static string ConvertDataToWords(Span<byte> entropy, string[] wordList)
        {
            var checksum = GetChecksum(entropy);

            var bin = ConvertBytestoBinaryString(entropy) + checksum;

            var words = BinaryStringToWords(bin, wordList);

            return words;
        }

        /// <summary>
        /// Returns the validated data as bytes from the given words and wordList.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="wordList"></param>
        /// <returns>Returns the validated data as bytes from the given words and wordList.</returns>
        public static byte[] GetEntropy(string words, string[] wordList)
        {
            var bin = WordsToBinaryString(words, wordList);

            if (bin == null) return null;

            var (data, appendedChecksum) = BinaryStringToDataAndChecksum(bin);

            var dataChecksum = GetChecksum(data);

            if (appendedChecksum != dataChecksum) return null;

            return data;
        }

        static bool BelongsToWordList(string words, string[] wordList)
        {
            words = words.Normalize(NormalizationForm.FormKD);
            foreach (var word in words.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
                if (!wordList.Contains(word)) return false;
            }
            return true;
        }

        static (Languages, string[]) GetWordList(string words)
        {
            foreach (var wlk in WordLists.Keys) {
                var wl = WordLists[wlk];
                if (BelongsToWordList(words, wl)) return (wlk, wl);
            }
            return (Languages.Unknown, null);
        }

        /// <summary>
        /// The checksum is a substring of the binary representation of the SHA256 hash of entropy.
        /// For every four bytes of entropy, one additional bit of the hash is used.
        /// </summary>
        /// <param name="entropy"></param>
        /// <returns></returns>
        public static string GetChecksum(ReadOnlySequence<byte> entropy)
        {
            var hash = KzHashes.SHA256(entropy);
            var bits = (int)entropy.Length * 8;
            var cs = bits / 32;

            var sb = new StringBuilder();
            foreach (var b in hash.Span) {
                sb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
                cs -= 8;
                if (cs <= 0) break;
            }
            if (cs < 0) sb.Length += cs;

            return sb.ToString();
        }

        /// <summary>
        /// The checksum is a substring of the binary representation of the SHA256 hash of entropy.
        /// For every four bytes of entropy, one additional bit of the hash is used.
        /// </summary>
        /// <param name="entropy"></param>
        /// <returns></returns>
        public static string GetChecksum(byte[] entropy) => GetChecksum(new ReadOnlySequence<byte>(entropy));

        public static string GetChecksum(Span<byte> entropy) => GetChecksum(new ReadOnlySequence<byte>(entropy.ToArray()));

        /// <summary>
        /// Returns words converted into a binary string of "0" and "1" based on wordList.
        /// If wordList is specified, then it is used.
        /// Otherwise the wordList is selected based on the words provided.
        /// If a wordList can't be determined, null is returned.
        /// If a word is not found in wordList, null is returned.
        /// </summary>
        /// <param name="words">A sequence of space separated words from wordList, or one of the standard WordLists</param>
        /// <param name="wordList">Optional wordList to be used.</param>
        /// <returns>Returns words converted into a binary string of "0" and "1" based on wordList.</returns>
        public static string WordsToBinaryString(string words, string[] wordList = null)
        {
            words = words.Normalize(NormalizationForm.FormKD);
            wordList = wordList ?? GetWordList(words).Item2;

            if (wordList == null) return null;

            var bin = "";
            foreach (var w in words.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
                var i = Array.IndexOf(wordList, w);
                if (i < 0) return null;
                bin += Convert.ToString(i, 2).PadLeft(11, '0');
            }

            return bin;
        }

        public static string BinaryStringToWords(string bin, string[] wordList)
        {
            var words = new StringBuilder();
            for (var j = 0; j < bin.Length; j += 11) {
                var i = Convert.ToInt16(bin.Substring(j, 11), 2);
                if (j > 0) words.Append(" ");
                words.Append(wordList[i]);
            }
            return words.ToString();
        }

        /// <summary>
        /// Converts a binary string of "0" and "1" into a byte[].
        /// Length of string must be a multiple of 8.
        /// </summary>
        /// <param name="dataBits"></param>
        /// <returns>dataBits converted to byte array.</returns>
        public static byte[] ConvertBinaryStringToBytes(string dataBits)
        {
            var data = new byte[dataBits.Length / 8];
            for (var i = 0; i < data.Length; i++) {
                data[i] = Convert.ToByte(dataBits.Substring(i * 8, 8), 2);
            }
            return data;
        }

        /// <summary>
        /// Converts data byte[] to a binary string of "0" and "1".
        /// </summary>
        /// <param name="data"></param>
        /// <returns>data byte[] converted to a binary string.</returns>
        public static string ConvertBytestoBinaryString(Span<byte> data)
        {
            var dataBits = "";
            foreach (var b in data) {
                dataBits += Convert.ToString(b, 2).PadLeft(8, '0');
            }
            return dataBits;
        }

        /// <summary>
        /// Splits a binary string into its data and checksum parts.
        /// Converts the data to an array of bytes.
        /// Returns (data as byte[], checksum as binary string).
        /// </summary>
        /// <param name="bin">Binary string to be split.</param>
        /// <returns>Returns (data as byte[], checksum as binary string).</returns>
        public static (byte[], string) BinaryStringToDataAndChecksum(string bin)
        {
            var cs = bin.Length / 33; // one bit of checksum for every 32 bits of data.
            var checksum = bin.Substring(bin.Length - cs);
            var dataBits = bin.Substring(0, bin.Length - cs);

            var data = ConvertBinaryStringToBytes(dataBits);

            return (data, checksum);
        }

        /// <summary>
        /// Returns true if words encode binary data with a valid checksum.
        /// If wordList is specified, then it is used.
        /// Otherwise the wordList is selected based on the words provided.
        /// If a wordList can't be determined, false is returned.
        /// </summary>
        /// <param name="words">A sequence of space separated words from wordList, or one of the standard WordLists</param>
        /// <param name="wordList">Optional wordList to be used.</param>
        /// <returns>Returns true if words encode binary data with a valid checksum.</returns>
        public static bool IsValid(string words, string[] wordList = null)
        {
            var bin = WordsToBinaryString(words, wordList);

            if (bin == null) return false;

            var (data, appendedChecksum) = BinaryStringToDataAndChecksum(bin);

            var dataChecksum = GetChecksum(data);

            return appendedChecksum == dataChecksum;
        }

        public override string ToString()
        {
            return Words;
        }

        /// <summary>
        /// Returns low order digits of a BigInteger as a byte array length / 8 bytes.
        /// CAUTION: Will pad array with zero bytes if number is small.
        /// </summary>
        /// <param name="big"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        static byte[] BigIntegerToEntropy(BigInteger big, int length = 128)
        {
            if (length % 8 != 0)
                throw new ArgumentException("length must be a multiple of eight.");
            var bytes = big.ToByteArray();
            var count = length / 8;
            if (bytes.Length > count)
                bytes = bytes.Take(count).ToArray();
            if (bytes.Length < count)
                bytes = new byte[count - bytes.Length].Concat(bytes).ToArray();
            return bytes;
        }

        /// <summary>
        /// Returns low order digits of a BigInteger as a byte array length / 8 bytes.
        /// CAUTION: Will pad array with zero bytes if number is small.
        /// </summary>
        /// <param name="big"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        static string BigIntegerToBase6(BigInteger big)
        {
            return "";
        }

        /// <summary>
        /// Converts a string of base 6 digits to a BitInteger.
        /// The string can use either digits 1-6, or 0-5.
        /// This is implemented by treating 6 and 0 as the same value 0.
        /// </summary>
        /// <param name="base6"></param>
        /// <returns></returns>
        public static BigInteger Base6ToBigInteger(string base6) {
            var n = new BigInteger(0);
            foreach (var c in base6.AsEnumerable<char>()) {
                var d = c - '0';
                if (d == 6) d = 0;
                if (d < 0 || d >= 6)
                    throw new ArgumentException();
                n = n * 6 + d;
            }
            return n;
        }

        public static BigInteger Base10ToBigInteger(string base10)
        {
            var bn = BigInteger.Parse(base10);
            return bn;
        }

        public static string ToDigitsBase10(byte[] bytes)
        {
            var bn = new BigInteger(bytes.Concat(new byte[1]).ToArray());
            return bn.ToString();
        }

        public static string ToDigitsBase6(byte[] bytes)
        {
            var bn = new BigInteger(bytes.Concat(new byte[1]).ToArray());
            var sb = new List<char>();
            while (bn > 0) {
                var r = (int)(bn % 6);
                bn = bn / 6;
                sb.Add((char)('0' + r));
            }
            sb.Reverse();
            return new string(sb.ToArray());
        }
        
        /// <summary>
        /// Returns the entropy as a byte[] from a string of base 6 digits.
        /// Verifies that there are at least length / Log2(6) rounded up digits in string.
        /// This is 50 digits for 128 bits, 100 digits for 256 bits.
        /// The string can use either digits 1-6, or 0-5.
        /// This is implemented by treating 6 and 0 as the same value 0.
        /// </summary>
        /// <param name="base6">The string can use either digits 1-6, or 0-5.</param>
        /// <param name="length">Optional entropy length in bits. Must be a multiple of 8.</param>
        /// <returns>Returns the entropy as a byte[] from a string of base 6 digits.</returns>
        static byte[] Base6ToEntropy(string base6, int length = 128)
        {
            var needDigits = (int)Math.Ceiling(length / Math.Log2(6));
            if (base6.Length < needDigits)
                throw new ArgumentException($"For {length} bits of entropy, at least {needDigits} digits of base 6 are needed.");
            return BigIntegerToEntropy(Base6ToBigInteger(base6), length);
        }

        /// <summary>
        /// Returns the entropy as a byte[] from a string of base 6 digits.
        /// Verifies that there are at least length / Log2(6) rounded up digits in string.
        /// This is 50 digits for 128 bits, 100 digits for 256 bits.
        /// The string must use digits 0-9.
        /// </summary>
        /// <param name="base10">The string must use digits 0-9.</param>
        /// <param name="length">Optional entropy length in bits. Must be a multiple of 8.</param>
        /// <returns>Returns the entropy as a byte[] from a string of base 10 digits.</returns>
        static byte[] Base10ToEntropy(string base10, int length = 128)
        {
            var needDigits = (int)Math.Ceiling(length / Math.Log2(10));
            if (base10.Length < needDigits)
                throw new ArgumentException($"For {length} bits of entropy, at least {needDigits} digits of base 10 are needed.");
            return BigIntegerToEntropy(Base10ToBigInteger(base10), length);
        }
    }
}
