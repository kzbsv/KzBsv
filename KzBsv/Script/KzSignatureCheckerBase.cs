#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
namespace KzBsv
{
    public class KzSignatureCheckerBase
    {
        public virtual bool CheckSig(KzValType scriptSig, KzValType vchPubKey, KzScript scriptCode, KzScriptFlags flags) => false;

        public virtual bool CheckLockTime(KzScriptNum nLockTime) => false;

        public virtual bool CheckSequence(KzScriptNum nSequence) => false;
    }
}
