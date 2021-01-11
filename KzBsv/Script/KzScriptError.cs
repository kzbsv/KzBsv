#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
namespace KzBsv
{
    public enum KzScriptError
    {
        OK = 0,
        UNKNOWN_ERROR,
        EVAL_FALSE,
        OP_RETURN,

        /* Max sizes */
        SCRIPT_SIZE,
        PUSH_SIZE,
        OP_COUNT,
        STACK_SIZE,
        SIG_COUNT,
        PUBKEY_COUNT,

        /* Operands checks */
        INVALID_OPERAND_SIZE,
        INVALID_NUMBER_RANGE,
        IMPOSSIBLE_ENCODING,
        INVALID_SPLIT_RANGE,
        SCRIPTNUM_OVERFLOW,
        SCRIPTNUM_MINENCODE,

        /* Failed verify operations */
        VERIFY,
        EQUALVERIFY,
        CHECKMULTISIGVERIFY,
        CHECKSIGVERIFY,
        NUMEQUALVERIFY,

        /* Logical/Format/Canonical errors */
        BAD_OPCODE,
        DISABLED_OPCODE,
        INVALID_STACK_OPERATION,
        INVALID_ALTSTACK_OPERATION,
        UNBALANCED_CONDITIONAL,

        /* Divisor errors */
        DIV_BY_ZERO,
        MOD_BY_ZERO,

        /* CHECKLOCKTIMEVERIFY and CHECKSEQUENCEVERIFY */
        NEGATIVE_LOCKTIME,
        UNSATISFIED_LOCKTIME,

        /* Malleability */
        SIG_HASHTYPE,
        SIG_DER,
        MINIMALDATA,
        SIG_PUSHONLY,
        SIG_HIGH_S,
        SIG_NULLDUMMY,
        PUBKEYTYPE,
        CLEANSTACK,
        MINIMALIF,
        SIG_NULLFAIL,

        /* softfork safeness */
        DISCOURAGE_UPGRADABLE_NOPS,

        /* misc */
        NONCOMPRESSED_PUBKEY,

        /* anti replay */
        ILLEGAL_FORKID,
        MUST_USE_FORKID,

        ERROR_COUNT
    }
}
