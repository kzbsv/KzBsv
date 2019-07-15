#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    public struct KzTxIn
    {
        /// <summary>
        /// Setting nSequence to this value for every input in a transaction disables nLockTime.
        /// </summary>
        public const UInt32 SEQUENCE_FINAL = 0xffff_ffff;

        KzOutPoint _prevout;
        KzScript _scriptSig;
        UInt32 _sequence;

        KzUtxo _utxo;

        public KzOutPoint PrevOut => _prevout;
        public KzScript ScriptSig => _scriptSig;
        public UInt32 Sequence => _sequence;

        public KzUtxo Utxo { get => _utxo; set => _utxo = value; }

        public KzTxIn(KzOutPoint prevout, KzScript scriptSig, UInt32 sequence)
        {
            _prevout = prevout;
            _scriptSig = scriptSig;
            _sequence = sequence;
            _utxo = null;
        }

        public bool TryReadTxIn(ref SequenceReader<byte> r)
        {
            if (!_prevout.TryReadOutPoint(ref r)) goto fail;
            if (!_scriptSig.TryReadScript(ref r)) goto fail;
            if (!r.TryReadLittleEndian(out _sequence)) goto fail;

            return true;
        fail:
            return false;
        }

        public IKzWriter AddTo(IKzWriter writer)
        {
            writer
                .Add(_prevout)
                .Add(_scriptSig)
                .Add(_sequence);
            return writer;
        }
    }
}
