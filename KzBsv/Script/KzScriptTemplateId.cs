#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion

namespace KzBsv {
    public enum KzScriptTemplateId : int {
        /// <summary>
        /// An unspendable OP_RETURN of unknown protocol
        /// </summary>
        OpRet = -1,
        /// <summary>
        /// Script is not one of the enumerated script templates.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// [pubkey in long or short format] OP_CHECKSIG
        /// </summary>
        P2PK = 1,
        /// <summary>
        /// OP_DUP OP_HASH160 [20 byte hashed pubkey] OP_EQUALVERIFY OP_CHECKSIG
        /// PubKey is moved to ScriptSig.
        /// Hash160 value can be converted into checksummed bitcoin address formats.
        /// </summary>
        P2PKH = 2,
        /// <summary>
        /// OP_0 OP_RETURN OP_PUSH4 ...
        /// </summary>
        OpRetPush4 = 3,
        /// <summary>
        /// Pay to script hash
        /// </summary>
        P2SH = 4,
        /// <summary>
        /// B:// protocol
        /// </summary>
        OpRetB = 5,
        /// <summary>
        /// Bcat protocol
        /// </summary>
        OpRetBcat = 6,
        /// <summary>
        /// Bcat part protocol
        /// </summary>
        OpRetBcatPart = 7
    }
}
