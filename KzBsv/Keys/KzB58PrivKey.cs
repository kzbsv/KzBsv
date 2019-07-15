#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Diagnostics;
using System.Linq;

namespace KzBsv
{
    public class KzB58PrivKey : KzB58Data
    {
        public void SetKey(KzPrivKey privKey)
        {
            Debug.Assert(privKey.IsValid);
            SetData(Kz.SECRET_KEY, privKey.ReadOnlySpan, privKey.IsCompressed);
        }

        public KzPrivKey GetKey()
        {
            var data = Data;
            Debug.Assert(data.Length >= 32);
            var isCompressed = data.Length > 32 && data[32] == 1;
            var privKey = new KzPrivKey(data.Slice(0, 32), isCompressed);
            return privKey;
        }

        public bool IsValid {
            get {
                var d = Data;
                var fExpectedFormat = d.Length == 32 || d.Length == 33 && d[^1] == 1;
                var v = Version;
                var fCorrectVersion = v.SequenceEqual(Kz.SECRET_KEY);
                return fExpectedFormat && fCorrectVersion;
            }
        }

        public bool SetString(string b58) => base.SetString(b58, Kz.SECRET_KEY.Length) && IsValid;

        public KzB58PrivKey() { }
        public KzB58PrivKey(KzPrivKey privKey) { SetKey(privKey); }
        public KzB58PrivKey(string b58) { SetString(b58); }
    }
}
