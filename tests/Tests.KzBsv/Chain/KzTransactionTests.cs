#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using KzBsv;
using System;
using Xunit;

namespace Tests.KzBsv
{
    public class KzTransactionTests
    {

        KzBTransaction BuildCreditingTransaction(KzScript scriptPubKey, long nValue)
        {
            var tx = new KzBTransaction { LockTime = 0, Version = 1, };
            tx.AddIn( new KzOutPoint(), Kz.Script().Push(0).Push(0) );
            tx.AddOut(scriptPubKey, nValue);
            return tx;
        }

        KzBTransaction BuildSpendingTransaction(KzScript scriptSig, KzBTransaction txCredit)
        {
            var tx = new KzBTransaction { LockTime = 0, Version = 1, };
            tx.AddIn( new KzOutPoint(), Kz.Script().Push(0).Push(0) );
            //tx.AddOut(scriptPubKey, nValue);
            return tx;
        }

        [Fact]
        public void Basic()
        {

        }
    }
}
