#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion

using System;

namespace KzBsv
{
    public static class KzMessage
    {
        static string _messageMagic = "Bitcoin Signed Message:\n";

        static KzUInt256 GetMessageHash(ReadOnlySpan<byte> message)
        {
            var messagehash = KzHashes.SHA256(message).ToHex();
            return new KzWriterHash().Add(_messageMagic).Add(messagehash).GetHashFinal();
        }

        public static byte[] SignMessage(this KzPrivKey key, ReadOnlySpan<byte> message)
        {
            var (ok, sig) = key.SignCompact(GetMessageHash(message));
            return ok ? sig : null;
        }

        public static string SignMessageToB64(this KzPrivKey key, ReadOnlySpan<byte> message)
        {
            var sigBytes = SignMessage(key, message);
            return sigBytes == null ? null : Convert.ToBase64String(sigBytes);
        }

        public static byte[] SignMessage(this KzPrivKey key, string message) => SignMessage(key, message.UTF8ToBytes());
        public static string SignMessageToB64(this KzPrivKey key, string message) => SignMessageToB64(key, message.UTF8ToBytes());

        public static KzPubKey RecoverPubKeyFromMessage(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        {
            var (ok, key) = KzPubKey.FromRecoverCompact(GetMessageHash(message), signature);
            return ok ? key : null;
        }

        public static bool VerifyMessage(this KzPubKey key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        {
            var rkey = RecoverPubKeyFromMessage(message, signature);
            return rkey != null && rkey == key;
        }

        public static bool VerifyMessage(this KzUInt160 keyID, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        {
            var rkey = RecoverPubKeyFromMessage(message, signature);
            return rkey != null && rkey.GetID() == keyID;
        }

        public static bool VerifyMessage(this KzPubKey key, string message, string signature) => VerifyMessage(key, message.UTF8ToBytes(), Convert.FromBase64String(signature));
        public static bool VerifyMessage(this KzUInt160 keyID, string message, string signature) => VerifyMessage(keyID, message.UTF8ToBytes(), Convert.FromBase64String(signature));
    }
}
