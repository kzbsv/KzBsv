#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace KzBsv
{
    /// <summary>
    /// Script verification flags 
    /// </summary>
    [Flags]
    public enum KzScriptFlags : UInt32 {
        VERIFY_NONE = 0,

        /// <summary>
        /// Evaluate P2SH subscripts (softfork safe, BIP16).
        /// </summary>
        VERIFY_P2SH = (1U << 0),

        /// <summary>
        /// Passing a non-strict-DER signature or one with undefined hashtype to a
        /// checksig operation causes script failure. Evaluating a pubkey that is not
        /// (0x04 + 64 bytes) or (0x02 or 0x03 + 32 bytes) by checksig causes script
        /// failure.
        /// </summary>
        VERIFY_STRICTENC = (1U << 1),

        /// <summary>
        /// Passing a non-strict-DER signature to a checksig operation causes script
        /// failure (softfork safe, BIP62 rule 1)
        /// </summary>
        VERIFY_DERSIG = (1U << 2),

        /// <summary>
        /// Passing a non-strict-DER signature or one with S > order/2 to a checksig
        /// operation causes script failure
        /// (softfork safe, BIP62 rule 5).
        /// </summary>
        VERIFY_LOW_S = (1U << 3),

        /// <summary>
        /// verify dummy stack item consumed by CHECKMULTISIG is of zero-length
        /// (softfork safe, BIP62 rule 7).
        /// </summary>
        VERIFY_NULLDUMMY = (1U << 4),

        /// <summary>
        /// Using a non-push operator in the scriptSig causes script failure
        /// (softfork safe, BIP62 rule 2).
        /// </summary>
        VERIFY_SIGPUSHONLY = (1U << 5),

        /// <summary>
        /// Require minimal encodings for all push operations (OP_0... OP_16,
        /// OP_1NEGATE where possible, direct pushes up to 75 bytes, OP_PUSHDATA up
        /// to 255 bytes, OP_PUSHDATA2 for anything larger). Evaluating any other
        /// push causes the script to fail (BIP62 rule 3). In addition, whenever a
        /// stack element is interpreted as a number, it must be of minimal length
        /// (BIP62 rule 4).
        /// (softfork safe)
        /// </summary>
        VERIFY_MINIMALDATA = (1U << 6),

        /// <summary>
        /// Discourage use of NOPs reserved for upgrades (NOP1-10)
        ///
        /// Provided so that nodes can avoid accepting or mining transactions
        /// containing executed NOP's whose meaning may change after a soft-fork,
        /// thus rendering the script invalid; with this flag set executing
        /// discouraged NOPs fails the script. This verification flag will never be a
        /// mandatory flag applied to scripts in a block. NOPs that are not executed,
        /// e.g.  within an unexecuted IF ENDIF block, are *not* rejected.
        /// </summary>
        VERIFY_DISCOURAGE_UPGRADABLE_NOPS = (1U << 7),

        /// <summary>
        /// Require that only a single stack element remains after evaluation. This
        /// changes the success criterion from "At least one stack element must
        /// remain, and when interpreted as a boolean, it must be true" to "Exactly
        /// one stack element must remain, and when interpreted as a boolean, it must
        /// be true".
        /// (softfork safe, BIP62 rule 6)
        /// Note: CLEANSTACK should never be used without P2SH or WITNESS.
        /// </summary>
        VERIFY_CLEANSTACK = (1U << 8),

        /// <summary>
        /// Verify CHECKLOCKTIMEVERIFY
        ///
        /// See BIP65 for details.
        /// </summary>
        VERIFY_CHECKLOCKTIMEVERIFY = (1U << 9),

        /// <summary>
        /// support CHECKSEQUENCEVERIFY opcode
        ///
        /// See BIP112 for details
        /// </summary>
        VERIFY_CHECKSEQUENCEVERIFY = (1U << 10),

        /// <summary>
        /// Require the argument of OP_IF/NOTIF to be exactly 0x01 or empty vector
        /// </summary>
        VERIFY_MINIMALIF = (1U << 13),

        /// <summary>
        /// Signature(s) must be empty vector if an CHECK(MULTI)SIG operation failed
        /// </summary>
        VERIFY_NULLFAIL = (1U << 14),

        /// <summary>
        /// Public keys in scripts must be compressed
        /// </summary>
        VERIFY_COMPRESSED_PUBKEYTYPE = (1U << 15),

        /// <summary>
        /// Do we accept signature using SIGHASH_FORKID
        /// </summary>
        ENABLE_SIGHASH_FORKID = (1U << 16),
    }
}
