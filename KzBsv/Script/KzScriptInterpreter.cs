#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace KzBsv
{
    [Flags]
    public enum KzSigHash : byte {
        UNSUPPORTED = 0,
        ALL = 1,
        NONE = 2,
        SINGLE = 3,
        FORKID = 0x40,
        ANYONECANPAY = 0x80,
    }

    /// <summary>
    /// Base signature hash types
    /// Base sig hash types not defined in this enum may be used, but they will be
    /// represented as UNSUPPORTED.  See transaction
    /// c99c49da4c38af669dea436d3e73780dfdb6c1ecf9958baa52960e8baee30e73 for an
    /// example where an unsupported base sig hash of 0 was used.
    /// </summary>
    public enum KzBaseSigHashType : byte {
        UNSUPPORTED = 0,
        ALL = KzSigHash.ALL,
        NONE = KzSigHash.NONE,
        SINGLE = KzSigHash.SINGLE
    };

    /// <summary>
    /// 
    /// </summary>
    public class KzSigHashType
    {
        UInt32 _sigHash;

        KzSigHash SigHash => (KzSigHash)_sigHash;

        public KzSigHashType() { _sigHash = (uint)KzSigHash.ALL; }

        public KzSigHashType(KzSigHash sigHash) { _sigHash = (uint)sigHash; }

        public KzSigHashType(uint sigHash) { _sigHash = sigHash; }

        public bool isDefined {
            get {
                var baseType = SigHash & ~(KzSigHash.FORKID | KzSigHash.ANYONECANPAY);
                return baseType >= KzSigHash.ALL && baseType <= KzSigHash.SINGLE;
            }
        }

        public bool hasForkId => (SigHash & KzSigHash.FORKID) != 0;

        public bool hasAnyoneCanPay => (SigHash & KzSigHash.ANYONECANPAY) != 0;

        public UInt32 rawSigHashType => _sigHash;

        public KzSigHashType withBaseType(KzBaseSigHashType baseSigHashType) {
            return new KzSigHashType((_sigHash & ~(uint)0x1f) | (uint)baseSigHashType);
        }

        public KzSigHashType withForkValue(UInt32 forkId) {
            return new KzSigHashType((forkId << 8) | (_sigHash & 0xff));
        }

        public KzSigHashType withForkId(bool forkId = true) {
            return new KzSigHashType((_sigHash & ~(uint)KzSigHash.FORKID) | (forkId ? (uint)KzSigHash.FORKID : 0));
        }

        public KzSigHashType withAnyoneCanPay(bool anyoneCanPay = true) {
            return new KzSigHashType((_sigHash & ~(uint)KzSigHash.ANYONECANPAY) | (anyoneCanPay ? (uint)KzSigHash.ANYONECANPAY : 0));
        }

        public KzBaseSigHashType getBaseType() { return (KzBaseSigHashType)((int)_sigHash & 0x1f); }

        public bool isBaseNone => ((int)_sigHash & 0x1f) == (int)KzBaseSigHashType.NONE;

        public bool isBaseSingle => ((int)_sigHash & 0x1f) == (int)KzBaseSigHashType.SINGLE;

        public UInt32 getForkValue() { return (UInt32)_sigHash >> 8; }
    }

    public static partial class KzScriptInterpreter
    {

        static KzSigHashType GetHashType(KzValType vchSig) => new KzSigHashType(vchSig.Length == 0 ? KzSigHash.UNSUPPORTED : (KzSigHash)vchSig.LastByte);

        static void CleanupScriptCode(KzScript scriptCode, KzValType vchSig, KzScriptFlags flags)
        {
            // Drop the signature in scripts when SIGHASH_FORKID is not used.
            var sigHashType = GetHashType(vchSig);
            if ((flags & KzScriptFlags.ENABLE_SIGHASH_FORKID) == 0 || !sigHashType.hasForkId) {
                scriptCode.FindAndDelete(vchSig);
            }
        }

        static bool CheckPubKeyEncoding(KzValType vchPubKey, KzScriptFlags flags, ref KzScriptError serror)
        {
            if ((flags & KzScriptFlags.VERIFY_STRICTENC) != 0 && !IsCompressedOrUncompressedPubKey(vchPubKey))
                return set_error(out serror, KzScriptError.PUBKEYTYPE);

            // Only compressed keys are accepted when
            // SCRIPT_VERIFY_COMPRESSED_PUBKEYTYPE is enabled.

            if ((flags & KzScriptFlags.VERIFY_COMPRESSED_PUBKEYTYPE) != 0 && !IsCompressedPubKey(vchPubKey))
                return set_error(out serror, KzScriptError.NONCOMPRESSED_PUBKEY);

            return true;
        }

        static bool IsCompressedOrUncompressedPubKey(KzValType vchPubKey)
        {
            var length = vchPubKey.Length;
            var first = vchPubKey.FirstByte;

            if (length < 33) {
                //  Non-canonical public key: too short
                return false;
            }
            if (first == 0x04) {
                if (length != 65) {
                    //  Non-canonical public key: invalid length for uncompressed key
                    return false;
                }
            } else if (first == 0x02 || first == 0x03) {
                if (length != 33) {
                    //  Non-canonical public key: invalid length for compressed key
                    return false;
                }
            } else {
                //  Non-canonical public key: neither compressed nor uncompressed
                return false;
            }
            return true;
        }

        public static KzScriptFlags ParseFlags(string flags)
        {
#if false
            var map = new Dictionary<string, KzScriptFlags>();
            map.Add("NONE", KzScriptFlags.VERIFY_NONE);
            map.Add("P2SH", KzScriptFlags.VERIFY_P2SH);
            map.Add("STRICTENC", KzScriptFlags.VERIFY_STRICTENC);
            map.Add("DERSIG", KzScriptFlags.VERIFY_DERSIG);
            map.Add("LOW_S", KzScriptFlags.VERIFY_LOW_S);
            map.Add("SIGPUSHONLY", KzScriptFlags.VERIFY_SIGPUSHONLY);
            map.Add("MINIMALDATA", KzScriptFlags.VERIFY_MINIMALDATA);
            map.Add("NULLDUMMY", KzScriptFlags.VERIFY_NULLDUMMY);
            map.Add("DISCOURAGE_UPGRADABLE_NOPS", KzScriptFlags.VERIFY_DISCOURAGE_UPGRADABLE_NOPS);
            map.Add("CLEANSTACK", KzScriptFlags.VERIFY_CLEANSTACK);
            map.Add("MINIMALIF", KzScriptFlags.VERIFY_MINIMALIF);
            map.Add("NULLFAIL", KzScriptFlags.VERIFY_NULLFAIL);
            map.Add("CHECKLOCKTIMEVERIFY", KzScriptFlags.VERIFY_CHECKLOCKTIMEVERIFY);
            map.Add("CHECKSEQUENCEVERIFY", KzScriptFlags.VERIFY_CHECKSEQUENCEVERIFY);
            map.Add("COMPRESSED_PUBKEYTYPE", KzScriptFlags.VERIFY_COMPRESSED_PUBKEYTYPE);
            map.Add("SIGHASH_FORKID", KzScriptFlags.ENABLE_SIGHASH_FORKID);
            map.Add("REPLAY_PROTECTION", KzScriptFlags.ENABLE_REPLAY_PROTECTION);
#endif
            var fs = flags.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var sfs = (KzScriptFlags)0;
            foreach (var f in fs) {
                var sfn = Enum.GetNames(typeof(KzScriptFlags)).Single(n => n.Contains(f));
                var sf = Enum.Parse<KzScriptFlags>(sfn);
                sfs |= sf;
            }
            return sfs;
        }

        static bool IsCompressedPubKey(KzValType vchPubKey)
        {
            var length = vchPubKey.Length;
            var first = vchPubKey.FirstByte;

            if (length != 33) {
                //  Non-canonical public key: invalid length for compressed key
                return false;
            }
            if (first != 0x02 && first != 0x03) {
                //  Non-canonical public key: invalid prefix for compressed key
                return false;
            }
            return true;
        }

        public static KzUInt256 ComputeSignatureHash(
            KzScript scriptCode, KzTransaction txTo, int nIn, KzSigHashType sigHashType, KzAmount amount, KzScriptFlags flags = KzScriptFlags.ENABLE_SIGHASH_FORKID
            )
        {
            if (sigHashType.hasForkId && (flags & KzScriptFlags.ENABLE_SIGHASH_FORKID) != 0) {
                var hashPrevouts = new KzUInt256();
                var hashSequence = new KzUInt256();
                var hashOutputs = new KzUInt256();

                if (!sigHashType.hasAnyoneCanPay) {
                    hashPrevouts = GetPrevoutHash(txTo);
                }

                var baseNotSingleOrNone =
                    (sigHashType.getBaseType() != KzBaseSigHashType.SINGLE) &&
                    (sigHashType.getBaseType() != KzBaseSigHashType.NONE);

                if (!sigHashType.hasAnyoneCanPay && baseNotSingleOrNone) {
                    hashSequence = GetSequenceHash(txTo);
                }

                if (baseNotSingleOrNone) {
                    hashOutputs = GetOutputsHash(txTo);
                } else if ((sigHashType.getBaseType() == KzBaseSigHashType.SINGLE) && (nIn < txTo.Vout.Length)) {
                    using var hw = new KzWriterHash();
                    hw.Add(txTo.Vout[nIn]);
                    hashOutputs = hw.GetHashFinal();
                }

                {
                    using var writer = new KzWriterHash();
                    writer
                        // Version
                        .Add(txTo.Version)
                        // Input prevouts/nSequence (none/all, depending on flags)
                        .Add(hashPrevouts)
                        .Add(hashSequence)
                        // The input being signed (replacing the scriptSig with scriptCode +
                        // amount). The prevout may already be contained in hashPrevout, and the
                        // nSequence may already be contain in hashSequence.
                        .Add(txTo.Vin[nIn].PrevOut)
                        .Add(scriptCode)
                        .Add(amount.Satoshis)
                        .Add(txTo.Vin[nIn].Sequence)
                        // Outputs (none/one/all, depending on flags)
                        .Add(hashOutputs)
                        // Locktime
                        .Add(txTo.LockTime)
                        // Sighash type
                        .Add(sigHashType.rawSigHashType)
                        ;
                    return writer.GetHashFinal();
                }
            }

            if (nIn >= txTo.Vin.Length) {
                //  nIn out of range
                return KzUInt256.One;
            }

            // Check for invalid use of SIGHASH_SINGLE
            if ((sigHashType.getBaseType() == KzBaseSigHashType.SINGLE) && (nIn >= txTo.Vout.Length)) {
                //  nOut out of range
                return KzUInt256.One;
            }

            {
                // Original digest algorithm...
                var hasACP = sigHashType.hasAnyoneCanPay;
                var nInputs = hasACP ? 1 : txTo.Vin.Length;
                using var writer = new KzWriterHash();
                // Start with the version...
                writer.Add(txTo.Version);
                // Add Input(s)...
                if (hasACP) {
                    // AnyoneCanPay serializes only the input being signed.
                    var i = txTo.Vin[nIn];
                    writer
                        .Add((byte)1)
                        .Add(i.PrevOut)
                        .Add(scriptCode, withoutCodeSeparators: true)
                        .Add(i.Sequence);
                } else {
                    // Non-AnyoneCanPay case. Process all inputs but handle input being signed in its own way.
                    var isSingleOrNone = sigHashType.isBaseSingle || sigHashType.isBaseNone;
                    writer.Add(txTo.Vin.Length.AsVarIntBytes());
                    for (var nInput = 0; nInput < txTo.Vin.Length; nInput++) {
                        var i = txTo.Vin[nInput];
                        writer.Add(i.PrevOut);
                        if (nInput != nIn)
                            writer.Add(KzScript.None);
                        else
                            writer.Add(scriptCode, withoutCodeSeparators: true);
                        if (nInput != nIn && isSingleOrNone)
                            writer.Add((int)0);
                        else
                            writer.Add(i.Sequence);
                    }
                }
                // Add Output(s)...
                var nOutputs = sigHashType.isBaseNone ? 0 : sigHashType.isBaseSingle ? nIn + 1 : txTo.Vout.Length;
                writer.Add(nOutputs.AsVarIntBytes());
                for (var nOutput = 0; nOutput < nOutputs; nOutput++) {
                    if (sigHashType.isBaseSingle && nOutput != nIn)
                        writer.Add(KzTxOut.Null);
                    else
                        writer.Add(txTo.Vout[nOutput]);
                }
                // Finish up...
                writer
                    .Add(txTo.LockTime)
                    .Add(sigHashType.rawSigHashType)
                    ;
                return writer.GetHashFinal();
            }
        }

        static KzUInt256 GetPrevoutHash(KzTransaction txTo)
        {
            using (var hw = new KzWriterHash()) {
                foreach (var i in txTo.Vin) hw.Add(i.PrevOut);
                return hw.GetHashFinal();
            }
        }

        static KzUInt256 GetSequenceHash(KzTransaction txTo)
        {
            using (var hw = new KzWriterHash()) {
                foreach (var i in txTo.Vin) hw.Add(i.Sequence);
                return hw.GetHashFinal();
            }
        }

        static KzUInt256 GetOutputsHash(KzTransaction txTo)
        {
            using (var hw = new KzWriterHash()) {
                foreach (var o in txTo.Vout) hw.Add(o);
                return hw.GetHashFinal();
            }
        }

        static bool CheckSignatureEncoding(KzValType vchSig, KzScriptFlags flags, ref KzScriptError serror)
        {
            // Empty signature. Not strictly DER encoded, but allowed to provide a
            // compact way to provide an invalid signature for use with CHECK(MULTI)SIG
            if (vchSig.Length == 0) {
                return true;
            }
            if ((flags & (KzScriptFlags.VERIFY_DERSIG | KzScriptFlags.VERIFY_LOW_S | KzScriptFlags.VERIFY_STRICTENC)) != 0
                && !IsValidSignatureEncoding(vchSig)) {
                return set_error(out serror, KzScriptError.SIG_DER);
            }
            if ((flags & KzScriptFlags.VERIFY_LOW_S) != 0 &&
                !IsLowDERSignature(vchSig, ref serror)) {
                // serror is set
                return false;
            }
            if ((flags & KzScriptFlags.VERIFY_STRICTENC) != 0) {
                var ht = GetHashType(vchSig);
                if (!ht.isDefined) return set_error(out serror, KzScriptError.SIG_HASHTYPE);
                bool usesForkId = ht.hasForkId;
                bool forkIdEnabled = (flags & KzScriptFlags.ENABLE_SIGHASH_FORKID) != 0;
                if (!forkIdEnabled && usesForkId) return set_error(out serror, KzScriptError.ILLEGAL_FORKID);
                if (forkIdEnabled && !usesForkId) return set_error(out serror, KzScriptError.MUST_USE_FORKID);
            }
            return true;
        }

        private static bool IsLowDERSignature(KzValType vchSig, ref KzScriptError serror)
        {
            if (!IsValidSignatureEncoding(vchSig)) return set_error(out serror, KzScriptError.SIG_DER);

            var sigInput = vchSig.Slice(0, (int)vchSig.Length - 1);

            if (!KzPubKey.CheckLowS(sigInput)) return set_error(out serror, KzScriptError.SIG_HIGH_S);

            return true;
        }

        /**
         * A canonical signature exists of: <30> <total len> <02> <len R> <R> <02> <len
         * S> <S> <hashtype>, where R and S are not negative (their first byte has its
         * highest bit not set), and not excessively padded (do not start with a 0 byte,
         * unless an otherwise negative number follows, in which case a single 0 byte is
         * necessary and even required).
         *
         * See https://bitcointalk.org/index.php?topic=8392.msg127623#msg127623
         *
         * This function is consensus-critical since BIP66.
         */
        static bool IsValidSignatureEncoding(KzValType vchSig)
        {
            // Format: 0x30 [total-length] 0x02 [R-length] [R] 0x02 [S-length] [S]
            // [sighash]
            // * total-length: 1-byte length descriptor of everything that follows,
            // excluding the sighash byte.
            // * R-length: 1-byte length descriptor of the R value that follows.
            // * R: arbitrary-length big-endian encoded R value. It must use the
            // shortest possible encoding for a positive integers (which means no null
            // bytes at the start, except a single one when the next byte has its
            // highest bit set).
            // * S-length: 1-byte length descriptor of the S value that follows.
            // * S: arbitrary-length big-endian encoded S value. The same rules apply.
            // * sighash: 1-byte value indicating what data is hashed (not part of the
            // DER signature)

            // Minimum and maximum size constraints.
            var length = vchSig.Length;
            if (length < 9) return false;
            if (length > 73) return false;

            var sig = vchSig.ToSpan();

            // A signature is of type 0x30 (compound).
            if (sig[0] != 0x30) return false;

            // Make sure the length covers the entire signature.
            if (sig[1] != sig.Length - 3) return false;

            // Extract the length of the R element.
            var lenR = sig[3];

            // Make sure the length of the S element is still inside the signature.
            if (5 + lenR >= sig.Length) return false;

            // Extract the length of the S element.
            var lenS = sig[5 + lenR];

            // Verify that the length of the signature matches the sum of the length
            // of the elements.
            if (lenR + lenS + 7 != sig.Length) return false;

            // Check whether the R element is an integer.
            if (sig[2] != 0x02) return false;

            // Zero-length integers are not allowed for R.
            if (lenR == 0) return false;

            // Negative numbers are not allowed for R.
            if ((sig[4] & 0x80) != 0) return false;

            // Null bytes at the start of R are not allowed, unless R would otherwise be
            // interpreted as a negative number.
            if (lenR > 1 && (sig[4] == 0) && (sig[5] & 0x80) == 0) return false;

            // Check whether the S element is an integer.
            if (sig[lenR + 4] != 0x02) return false;

            // Zero-length integers are not allowed for S.
            if (lenS == 0) return false;

            // Negative numbers are not allowed for S.
            if ((sig[lenR + 6] & 0x80) != 0) return false;

            // Null bytes at the start of S are not allowed, unless S would otherwise be
            // interpreted as a negative number.
            if (lenS > 1 && (sig[lenR + 6] == 0x00) && (sig[lenR + 7] & 0x80) == 0) {
                return false;
            }

            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool set_success(out KzScriptError ret)
        {
            ret = KzScriptError.OK;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool set_error(out KzScriptError serror, KzScriptError error)
        {
            serror = error;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsValidMaxOpsPerScript(int nOpCount) => nOpCount <= Kz.Consensus.MAX_OPS_PER_SCRIPT;

        static bool IsOpcodeDisabled(KzOpcode opcode, KzScriptFlags flags)
        {
            switch (opcode) {
                case KzOpcode.OP_2MUL:
                case KzOpcode.OP_2DIV:
                    // Disabled opcodes.
                    return true;

                default:
                    break;
            }

            return false;
        }

        static bool CheckMinimalPush(ref KzOp op)
        {
            var opcode = op.Code;
            var dataSize = op.Data.Length;
            if (dataSize == 0) {
                // Could have used OP_0.
                return opcode == KzOpcode.OP_0;
            }
            var b0 = op.Data.Sequence.First.Span[0];
            if (dataSize == 1 && b0 >= 1 && b0 <= 16) {
                // Could have used OP_1 .. OP_16.
                return (int)opcode == (int)KzOpcode.OP_1 + (b0 - 1);
            }
            if (dataSize == 1 && b0 == 0x81) {
                // Could have used OP_1NEGATE.
                return opcode == KzOpcode.OP_1NEGATE;
            }
            if (dataSize <= 75) {
                // Could have used a direct push (opcode indicating number of bytes
                // pushed + those bytes).
                return (int)opcode == dataSize;
            }
            if (dataSize <= 255) {
                // Could have used OP_PUSHDATA.
                return opcode == KzOpcode.OP_PUSHDATA1;
            }
            if (dataSize <= 65535) {
                // Could have used OP_PUSHDATA2.
                return opcode == KzOpcode.OP_PUSHDATA2;
            }
            return true;
        }

        /// <summary>
        /// Modeled on Bitcoin-SV interpreter.cpp 0.1.1 lines 1866-1945
        /// </summary>
        /// <param name="scriptSig"></param>
        /// <param name="scriptPub"></param>
        /// <param name="flags"></param>
        /// <param name="checker"></param>
        /// <param name="serror"></param>
        /// <returns></returns>
        public static bool VerifyScript(KzScript scriptSig, KzScript scriptPub, KzScriptFlags flags, KzSignatureCheckerBase checker, out KzScriptError serror)
        {
            set_error(out serror, KzScriptError.UNKNOWN_ERROR);

            if ((flags & KzScriptFlags.ENABLE_SIGHASH_FORKID) != 0) {
                flags |= KzScriptFlags.VERIFY_STRICTENC;
            }

            if ((flags & KzScriptFlags.VERIFY_SIGPUSHONLY) != 0 && !scriptSig.IsPushOnly()) {
                return set_error(out serror, KzScriptError.SIG_PUSHONLY);
            }

            var stack = new KzStack<KzValType>();
            if (!EvalScript(stack, scriptSig, flags, checker, out serror))
                return false;

            if (!EvalScript(stack, scriptPub, flags, checker, out serror))
                return false;

            if (stack.Count == 0)
                return set_error(out serror, KzScriptError.EVAL_FALSE);

            if (stack.Peek().ToBool() == false)
                return set_error(out serror, KzScriptError.EVAL_FALSE);

            return set_success(out serror);
        }

        static readonly KzValType vchZero = new KzValType(new byte[0]);
        static readonly KzValType vchFalse = new KzValType(new byte[0]);
        static readonly KzValType vchTrue = new KzValType(new byte[] { 1 });

        /// <summary>
        /// Modeled on Bitcoin-SV interpreter.cpp 0.1.1 lines 384-1520
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="script"></param>
        /// <param name="flags"></param>
        /// <param name="checker"></param>
        /// <param name="serror"></param>
        /// <returns></returns>
        public static bool EvalScript(KzStack<KzValType> stack, KzScript script, KzScriptFlags flags, KzSignatureCheckerBase checker, out KzScriptError serror)
        {
            var ros = script.Sequence;
            var pc = ros.Start;
            var pend = ros.End;
            var pbegincodehash = ros.Start;
            KzOp op = new KzOp();
            var vfExec = new KzStack<bool>();
            var altStack = new KzStack<KzValType>();

            set_error(out serror, KzScriptError.UNKNOWN_ERROR);

            if (script.Length > Kz.Params.Consensus.ScriptMaxSize)
                return set_error(out serror, KzScriptError.SCRIPT_SIZE);

            var nOpCount = 0;
            var fRequireMinimal = (flags & KzScriptFlags.VERIFY_MINIMALDATA) != 0;

            try {
                while (ros.Length > 0) {
                    var fExec = vfExec.Contains(false) == false;

                    if (!op.TryReadOp(ref ros)) {
                        return set_error(out serror, KzScriptError.BAD_OPCODE);
                    }

                    if (op.Data.Length > Kz.Params.Consensus.MAX_SCRIPT_ELEMENT_SIZE) {
                        return set_error(out serror, KzScriptError.PUSH_SIZE);
                    }

                    if (op.Code > KzOpcode.OP_16) {
                        ++nOpCount;
                        if (!IsValidMaxOpsPerScript(nOpCount)) {
                            return set_error(out serror, KzScriptError.OP_COUNT);
                        }
                    }

                    // Some opcodes are disabled.
                    if (IsOpcodeDisabled(op.Code, flags)) {
                        return set_error(out serror, KzScriptError.DISABLED_OPCODE);
                    }

                    if (fExec && 0 <= op.Code && op.Code <= KzOpcode.OP_PUSHDATA4) {
                        if (fRequireMinimal &&
                            !CheckMinimalPush(ref op)) {
                            return set_error(out serror, KzScriptError.MINIMALDATA);
                        }
                        stack.Push(op.Data);
                        // ( -- value)
                    } else if (fExec || (KzOpcode.OP_IF <= op.Code && op.Code <= KzOpcode.OP_ENDIF)) {
                        switch (op.Code) {
                            //
                            // Push value
                            //
                            case KzOpcode.OP_1NEGATE:
                            case KzOpcode.OP_1:
                            case KzOpcode.OP_2:
                            case KzOpcode.OP_3:
                            case KzOpcode.OP_4:
                            case KzOpcode.OP_5:
                            case KzOpcode.OP_6:
                            case KzOpcode.OP_7:
                            case KzOpcode.OP_8:
                            case KzOpcode.OP_9:
                            case KzOpcode.OP_10:
                            case KzOpcode.OP_11:
                            case KzOpcode.OP_12:
                            case KzOpcode.OP_13:
                            case KzOpcode.OP_14:
                            case KzOpcode.OP_15:
                            case KzOpcode.OP_16: {
                                    var sn = new KzScriptNum((int)op.Code - (int)KzOpcode.OP_1 + 1);
                                    stack.Push(sn.ToValType());
                                    // ( -- value)
                                }
                                break;

                            //
                            // Control
                            //
                            case KzOpcode.OP_NOP:
                                break;
                            case KzOpcode.OP_CHECKLOCKTIMEVERIFY:
                                break;
                            case KzOpcode.OP_CHECKSEQUENCEVERIFY:
                                break;

                            case KzOpcode.OP_NOP1:
                            case KzOpcode.OP_NOP4:
                            case KzOpcode.OP_NOP5:
                            case KzOpcode.OP_NOP6:
                            case KzOpcode.OP_NOP7:
                            case KzOpcode.OP_NOP8:
                            case KzOpcode.OP_NOP9:
                            case KzOpcode.OP_NOP10: {
                                    if ((flags & KzScriptFlags.VERIFY_DISCOURAGE_UPGRADABLE_NOPS) != 0) {
                                        return set_error(out serror, KzScriptError.DISCOURAGE_UPGRADABLE_NOPS);
                                    }
                                }
                                break;

                            case KzOpcode.OP_IF:
                            case KzOpcode.OP_NOTIF: {
                                    // <expression> if [statements] [else [statements]]
                                    // endif
                                    var fValue = false;
                                    if (fExec) {
                                        if (stack.Count < 1) {
                                            return set_error(out serror, KzScriptError.UNBALANCED_CONDITIONAL);
                                        }
                                        var vch = stack.Pop();
                                        if ((flags & KzScriptFlags.VERIFY_MINIMALIF) != 0) {
                                            if (vch.Length > 1 || vch.Length == 1 && vch.GetReader().CurrentSpan[0] != 1) {
                                                stack.Push(vch);
                                                return set_error(out serror, KzScriptError.MINIMALIF);
                                            }
                                        }
                                        fValue = vch.ToBool();
                                        if (op.Code == KzOpcode.OP_NOTIF) {
                                            fValue = !fValue;
                                        }
                                    }
                                    vfExec.Push(fValue);
                                }
                                break;

                            case KzOpcode.OP_ELSE: {
                                    if (vfExec.Count < 1) {
                                        return set_error(out serror, KzScriptError.UNBALANCED_CONDITIONAL);
                                    }
                                    vfExec.Push(!vfExec.Pop());
                                }
                                break;

                            case KzOpcode.OP_ENDIF: {
                                    if (vfExec.Count < 1) {
                                        return set_error(out serror, KzScriptError.UNBALANCED_CONDITIONAL);
                                    }
                                    vfExec.Pop();
                                }
                                break;

                            case KzOpcode.OP_VERIFY: {
                                    // (true -- ) or
                                    // (false -- false) and return
                                    if (stack.Count < 1) {
                                        return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    }
                                    var vch = stack.Pop();
                                    bool fValue = vch.ToBool();
                                    if (!fValue) {
                                        stack.Push(vch);
                                        return set_error(out serror, KzScriptError.VERIFY);
                                    }
                                }
                                break;

                            case KzOpcode.OP_RETURN:
                                return set_error(out serror, KzScriptError.OP_RETURN);

                            //
                            // Stack ops
                            //
                            case KzOpcode.OP_TOALTSTACK: {
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    altStack.Push(stack.Pop());
                                }
                                break;

                            case KzOpcode.OP_FROMALTSTACK: {
                                    if (altStack.Count < 1) return set_error(out serror, KzScriptError.INVALID_ALTSTACK_OPERATION);
                                    stack.Push(altStack.Pop());
                                }
                                break;

                            case KzOpcode.OP_2DROP: {
                                    // (x1 x2 -- )
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Drop2();
                                }
                                break;

                            case KzOpcode.OP_2DUP: {
                                    // (x1 x2 -- x1 x2 x1 x2)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Dup2();
                                }
                                break;

                            case KzOpcode.OP_3DUP: {
                                    // (x1 x2 x3 -- x1 x2 x3 x1 x2 x3)
                                    if (stack.Count < 3) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Dup3();
                                }
                                break;

                            case KzOpcode.OP_2OVER: {
                                    // (x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2)
                                    if (stack.Count < 4) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Over2();
                                }
                                break;

                            case KzOpcode.OP_2ROT: {
                                    // (x1 x2 x3 x4 x5 x6 -- x3 x4 x5 x6 x1 x2)
                                    if (stack.Count < 6) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Rot2();
                                }
                                break;

                            case KzOpcode.OP_2SWAP: {
                                    // (x1 x2 x3 x4 -- x3 x4 x1 x2)
                                    if (stack.Count < 4) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Swap2();
                                }
                                break;

                            case KzOpcode.OP_IFDUP: {
                                    // (x - 0 | x x)
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var vch = stack.Peek();
                                    if (vch.ToBool())
                                        stack.Push(vch);
                                }
                                break;

                            case KzOpcode.OP_DEPTH: {
                                    // -- stacksize
                                    stack.Push(new KzScriptNum(stack.Count).ToValType());
                                }
                                break;

                            case KzOpcode.OP_DROP: {
                                    // (x -- )
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Pop();
                                }
                                break;

                            case KzOpcode.OP_DUP: {
                                    // (x -- x x)
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Push(stack.Peek());
                                }
                                break;

                            case KzOpcode.OP_NIP: {
                                    // (x1 x2 -- x2)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Nip();
                                }
                                break;

                            case KzOpcode.OP_OVER: {
                                    // (x1 x2 -- x1 x2 x1)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Over();
                                }
                                break;

                            case KzOpcode.OP_PICK:
                            case KzOpcode.OP_ROLL: {
                                    // (xn ... x2 x1 x0 n - xn ... x2 x1 x0 xn)
                                    // (xn ... x2 x1 x0 n - ... x2 x1 x0 xn)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var n = stack.Pop().ToScriptNum(fRequireMinimal).getint();
                                    if (n < 0 || n >= stack.Count) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    if (op.Code == KzOpcode.OP_ROLL)
                                        stack.Roll(n);
                                    else
                                        stack.Pick(n);
                                }
                                break;

                            case KzOpcode.OP_ROT: {
                                    // (x1 x2 x3 -- x2 x3 x1)
                                    if (stack.Count < 3) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Rot();
                                }
                                break;

                            case KzOpcode.OP_SWAP: {
                                    // (x1 x2 -- x2 x1)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Swap();
                                }
                                break;

                            case KzOpcode.OP_TUCK: {
                                    // (x1 x2 -- x2 x1 x2)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Tuck();
                                }
                                break;

                            case KzOpcode.OP_SIZE: {
                                    // (in -- in size)
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var sn = new KzScriptNum(stack.Peek().Length);
                                    stack.Push(sn.ToValType());
                                }
                                break;

                            //
                            // Bitwise logic
                            //
                            case KzOpcode.OP_AND:
                            case KzOpcode.OP_OR:
                            case KzOpcode.OP_XOR: {
                                    // (x1 x2 - out)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var x2 = stack.Pop();
                                    var x1 = stack.Pop();

                                    // Inputs must be the same size
                                    if (x1.Length != x2.Length) return set_error(out serror, KzScriptError.INVALID_OPERAND_SIZE);

                                    // To avoid allocating, we modify vch1 in place.
                                    switch (op.Code) {
                                        case KzOpcode.OP_AND:
                                            stack.Push(x1.BitAnd(x2));
                                            break;
                                        case KzOpcode.OP_OR:
                                            stack.Push(x1.BitOr(x2));
                                            break;
                                        case KzOpcode.OP_XOR:
                                            stack.Push(x1.BitXor(x2));
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                break;

                            case KzOpcode.OP_INVERT: {
                                    // (x -- out)
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    stack.Push(stack.Pop().BitInvert());
                                }
                                break;

                            case KzOpcode.OP_LSHIFT:
                            case KzOpcode.OP_RSHIFT: {
                                    // (x n -- out)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var nvt = stack.Pop();
                                    var n = nvt.ToInt32();
                                    if (n < 0) {
                                        stack.Push(nvt);
                                        return set_error(out serror, KzScriptError.INVALID_NUMBER_RANGE);
                                    }
                                    var x = stack.Pop();
                                    var r = op.Code == KzOpcode.OP_LSHIFT ? x.LShift(n) : x.RShift(n);
                                    stack.Push(r);
                                }
                                break;

                            case KzOpcode.OP_EQUAL:
                            case KzOpcode.OP_EQUALVERIFY:
                                // case OP_NOTEQUAL: // use OP_NUMNOTEQUAL
                                {
                                    // (x1 x2 - bool)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var x2 = stack.Pop();
                                    var x1 = stack.Pop();

                                    var fEqual = x1.BitEquals(x2); // (vch1 == vch2);
                                    // OP_NOTEQUAL is disabled because it would be too
                                    // easy to say something like n != 1 and have some
                                    // wiseguy pass in 1 with extra zero bytes after it
                                    // (numerically, 0x01 == 0x0001 == 0x000001)
                                    // if (opcode == OP_NOTEQUAL)
                                    //    fEqual = !fEqual;
                                    stack.Push(fEqual ? vchTrue : vchFalse);
                                    if (op.Code == KzOpcode.OP_EQUALVERIFY) {
                                        if (fEqual)
                                            stack.Pop();
                                        else
                                            return set_error(out serror, KzScriptError.EQUALVERIFY);
                                    }
                                }
                                break;

                            //
                            // Numeric
                            //
                            case KzOpcode.OP_1ADD:
                            case KzOpcode.OP_1SUB:
                            case KzOpcode.OP_NEGATE:
                            case KzOpcode.OP_ABS:
                            case KzOpcode.OP_NOT:
                            case KzOpcode.OP_0NOTEQUAL: {
                                    // (in -- out)
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var bn = stack.Pop().ToScriptNum(fRequireMinimal);
                                    switch (op.Code) {
                                        case KzOpcode.OP_1ADD:
                                            bn += KzScriptNum.One;
                                            break;
                                        case KzOpcode.OP_1SUB:
                                            bn -= KzScriptNum.One;
                                            break;
                                        case KzOpcode.OP_NEGATE:
                                            bn = -bn;
                                            break;
                                        case KzOpcode.OP_ABS:
                                            if (bn < KzScriptNum.Zero) {
                                                bn = -bn;
                                            }
                                            break;
                                        case KzOpcode.OP_NOT:
                                            bn = (bn == KzScriptNum.Zero);
                                            break;
                                        case KzOpcode.OP_0NOTEQUAL:
                                            bn = (bn != KzScriptNum.Zero);
                                            break;
                                        default:
                                            return set_error(out serror, KzScriptError.BAD_OPCODE);
                                    }
                                    stack.Push(bn.ToValType());
                                }
                                break;

                            case KzOpcode.OP_ADD:
                            case KzOpcode.OP_SUB:
                            case KzOpcode.OP_MUL:
                            case KzOpcode.OP_DIV:
                            case KzOpcode.OP_MOD:
                            case KzOpcode.OP_BOOLAND:
                            case KzOpcode.OP_BOOLOR:
                            case KzOpcode.OP_NUMEQUAL:
                            case KzOpcode.OP_NUMEQUALVERIFY:
                            case KzOpcode.OP_NUMNOTEQUAL:
                            case KzOpcode.OP_LESSTHAN:
                            case KzOpcode.OP_GREATERTHAN:
                            case KzOpcode.OP_LESSTHANOREQUAL:
                            case KzOpcode.OP_GREATERTHANOREQUAL:
                            case KzOpcode.OP_MIN:
                            case KzOpcode.OP_MAX: {
                                    // (x1 x2 -- out)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var bn2 = stack.Pop().ToScriptNum(fRequireMinimal);
                                    var bn1 = stack.Pop().ToScriptNum(fRequireMinimal);
                                    var bn = new KzScriptNum(0);
                                    switch (op.Code) {
                                        case KzOpcode.OP_ADD:
                                            bn = bn1 + bn2;
                                            break;

                                        case KzOpcode.OP_SUB:
                                            bn = bn1 - bn2;
                                            break;

                                        case KzOpcode.OP_MUL:
                                            bn = bn1 * bn2;
                                            break;

                                        case KzOpcode.OP_DIV:
                                            // denominator must not be 0
                                            if (bn2 == 0) return set_error(out serror, KzScriptError.DIV_BY_ZERO);
                                            bn = bn1 / bn2;
                                            break;

                                        case KzOpcode.OP_MOD:
                                            // divisor must not be 0
                                            if (bn2 == 0) return set_error(out serror, KzScriptError.MOD_BY_ZERO);
                                            bn = bn1 % bn2;
                                            break;

                                        case KzOpcode.OP_BOOLAND:
                                            bn = (bn1 != KzScriptNum.Zero && bn2 != KzScriptNum.Zero);
                                            break;
                                        case KzOpcode.OP_BOOLOR:
                                            bn = (bn1 != KzScriptNum.Zero || bn2 != KzScriptNum.Zero);
                                            break;
                                        case KzOpcode.OP_NUMEQUAL:
                                            bn = (bn1 == bn2);
                                            break;
                                        case KzOpcode.OP_NUMEQUALVERIFY:
                                            bn = (bn1 == bn2);
                                            break;
                                        case KzOpcode.OP_NUMNOTEQUAL:
                                            bn = (bn1 != bn2);
                                            break;
                                        case KzOpcode.OP_LESSTHAN:
                                            bn = (bn1 < bn2);
                                            break;
                                        case KzOpcode.OP_GREATERTHAN:
                                            bn = (bn1 > bn2);
                                            break;
                                        case KzOpcode.OP_LESSTHANOREQUAL:
                                            bn = (bn1 <= bn2);
                                            break;
                                        case KzOpcode.OP_GREATERTHANOREQUAL:
                                            bn = (bn1 >= bn2);
                                            break;
                                        case KzOpcode.OP_MIN:
                                            bn = (bn1 < bn2 ? bn1 : bn2);
                                            break;
                                        case KzOpcode.OP_MAX:
                                            bn = (bn1 > bn2 ? bn1 : bn2);
                                            break;
                                        default:
                                            return set_error(out serror, KzScriptError.BAD_OPCODE);
                                    }
                                    stack.Push(bn.ToValType());

                                    if (op.Code == KzOpcode.OP_NUMEQUALVERIFY) {
                                        var vch = stack.Pop();
                                        if (!vch.ToBool()) {
                                            stack.Push(vch);
                                            return set_error(out serror, KzScriptError.NUMEQUALVERIFY);
                                        }
                                    }
                                }
                                break;

                            case KzOpcode.OP_WITHIN: {
                                    // (x1 min2 max3 -- out)
                                    if (stack.Count < 3) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var bn3 = stack.Pop().ToScriptNum(fRequireMinimal);
                                    var bn2 = stack.Pop().ToScriptNum(fRequireMinimal);
                                    var bn1 = stack.Pop().ToScriptNum(fRequireMinimal);
                                    var bn = new KzScriptNum(0);
                                    bool fValue = (bn2 <= bn1 && bn1 < bn3);
                                    stack.Push(fValue ? vchTrue : vchFalse);
                                }
                                break;

                            //
                            // Crypto
                            //
                            case KzOpcode.OP_RIPEMD160:
                            case KzOpcode.OP_SHA1:
                            case KzOpcode.OP_SHA256:
                            case KzOpcode.OP_HASH160:
                            case KzOpcode.OP_HASH256: {
                                    // (in -- hash)
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var vch = stack.Pop();
                                    var data = (byte[])null;
                                    switch (op.Code) {
                                        case KzOpcode.OP_SHA1:
                                            data = new byte[20];
                                            KzHashes.SHA1(vch.Sequence, data);
                                            break;
                                        case KzOpcode.OP_RIPEMD160:
                                            data = new byte[20];
                                            KzHashes.RIPEMD160(vch.Sequence, data);
                                            break;
                                        case KzOpcode.OP_HASH160:
                                            data = new byte[20];
                                            KzHashes.HASH160(vch.Sequence, data);
                                            break;
                                        case KzOpcode.OP_SHA256:
                                            data = new byte[32];
                                            KzHashes.SHA256(vch.Sequence, data);
                                            break;
                                        case KzOpcode.OP_HASH256:
                                            data = new byte[32];
                                            KzHashes.HASH256(vch.Sequence, data);
                                            break;
                                        default:
                                            return set_error(out serror, KzScriptError.BAD_OPCODE);
                                    }
                                    stack.Push(new KzValType(data));
                                }
                                break;

                            case KzOpcode.OP_CODESEPARATOR: {
                                    // Hash starts after the code separator
                                    pbegincodehash = ros.Start;
                                }
                                break;

                            case KzOpcode.OP_CHECKSIG:
                            case KzOpcode.OP_CHECKSIGVERIFY: {
                                    // (sig pubkey -- bool)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);
                                    var vchPubKey = stack.Pop();
                                    var vchSig = stack.Pop();

                                    if (!CheckSignatureEncoding(vchSig, flags, ref serror) ||
                                        !CheckPubKeyEncoding(vchPubKey, flags, ref serror)) {
                                        // serror is set
                                        return false;
                                    }

                                    // Subset of script starting at the most recent
                                    // codeseparator
                                    var scriptCode = script.Slice(pbegincodehash, pend);

                                    // Remove signature for pre-fork scripts
                                    CleanupScriptCode(scriptCode, vchSig, flags);

                                    bool fSuccess = checker.CheckSig(vchSig, vchPubKey, scriptCode, flags);

                                    if (!fSuccess && (flags & KzScriptFlags.VERIFY_NULLFAIL) != 0 && vchSig.Length > 0) {
                                        return set_error(out serror, KzScriptError.SIG_NULLFAIL);
                                    }

                                    stack.Push(fSuccess ? vchTrue : vchFalse);
                                    if (op.Code == KzOpcode.OP_CHECKSIGVERIFY) {
                                        if (fSuccess) {
                                            stack.Pop();
                                        } else {
                                            return set_error(out serror, KzScriptError.CHECKSIGVERIFY);
                                        }
                                    }
                                }
                                break;

                            //
                            // Byte string operations
                            //
                            case KzOpcode.OP_CAT: {
                                    // (x1 x2 -- out)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);

                                    var x2 = stack.Pop();
                                    var x1 = stack.Pop();
                                    if (x1.Length + x2.Length > Kz.Consensus.MAX_SCRIPT_ELEMENT_SIZE) return set_error(out serror, KzScriptError.PUSH_SIZE);

                                    stack.Push(x1.Cat(x2));
                                }
                                break;

                            case KzOpcode.OP_SPLIT: {
                                    // (data position -- x1 x2)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);

                                    var position = stack.Pop().ToScriptNum(fRequireMinimal).getint();
                                    var data = stack.Pop();

                                    // Make sure the split point is apropriate.
                                    if (position < 0 || position > data.Length)
                                        return set_error(out serror, KzScriptError.INVALID_SPLIT_RANGE);

                                    var (x1, x2) = data.Split(position);
                                    stack.Push(x1);
                                    stack.Push(x2);
                                }
                                break;

                            //
                            // Conversion operations
                            //
                            case KzOpcode.OP_NUM2BIN: {
                                    // (in size -- out)
                                    if (stack.Count < 2) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);

                                    var size = stack.Pop().ToScriptNum(fRequireMinimal).getint();
                                    if (size < 0 || size > Kz.Consensus.MAX_SCRIPT_ELEMENT_SIZE)
                                        return set_error(out serror, KzScriptError.PUSH_SIZE);

                                    var num = stack.Pop();

                                    var (bin, ok) = num.Num2Bin((uint)size);

                                    if (!ok) return set_error(out serror, KzScriptError.IMPOSSIBLE_ENCODING);

                                    stack.Push(bin);
                                }
                                break;

                            case KzOpcode.OP_BIN2NUM: {
                                    // (in -- out)
                                    if (stack.Count < 1) return set_error(out serror, KzScriptError.INVALID_STACK_OPERATION);

                                    var bin = stack.Pop();

                                    var (num, ok) = bin.Bin2Num();

                                    if (!ok) return set_error(out serror, KzScriptError.INVALID_NUMBER_RANGE);

                                    stack.Push(num);
                                }
                                break;

                            default:
                                return set_error(out serror, KzScriptError.BAD_OPCODE);
                        }
                    }

                    if (stack.Count + altStack.Count > 1000) return set_error(out serror, KzScriptError.STACK_SIZE);
                }
            }
            catch (KzScriptNum.OverflowError) {
                return set_error(out serror, KzScriptError.SCRIPTNUM_OVERFLOW);
            }
            catch (KzScriptNum.MinEncodeError) {
                return set_error(out serror, KzScriptError.SCRIPTNUM_MINENCODE);
            }
            catch {
                return set_error(out serror, KzScriptError.UNKNOWN_ERROR);
            }

            if (vfExec.Count != 0) {
                return set_error(out serror, KzScriptError.UNBALANCED_CONDITIONAL);
            }

            return set_success(out serror);
        }
    }
}
