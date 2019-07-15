using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Secp256k1Net
{

    /// <summary>
    /// A pointer to a function that applies hash function to a point.
    /// Returns: 1 if a point was successfully hashed. 0 will cause ecdh to fail.
    /// </summary>
    /// <param name="output">Pointer to an array to be filled by the function.</param>
    /// <param name="x">Pointer to a 32-byte x coordinate.</param>
    /// <param name="y">Pointer to a 32-byte y coordinate.</param>
    /// <param name="data">Arbitrary data pointer that is passed through.</param>
    /// <returns>Returns: 1 if a point was successfully hashed. 0 will cause ecdh to fail.</returns>
    public delegate int EcdhHashFunction(Span<byte> output, Span<byte> x, Span<byte> y, IntPtr data);


    public unsafe class Secp256k1 : IDisposable
    {
        public IntPtr Context => _ctx;

        public const int SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH = 65;
        public const int SERIALIZED_COMPRESSED_PUBKEY_LENGTH = 33;
        public const int PUBKEY_LENGTH = 64;
        public const int PRIVKEY_LENGTH = 32;
        public const int UNSERIALIZED_SIGNATURE_SIZE = 65;
        public const int SERIALIZED_SIGNATURE_SIZE = 64;
        public const int SIGNATURE_LENGTH = 64;
        public const int HASH_LENGTH = 32;
        public const int SECRET_LENGTH = 32;


        readonly Lazy<secp256k1_context_create> secp256k1_context_create;
        readonly Lazy<secp256k1_context_randomize> secp256k1_context_randomize;
        readonly Lazy<secp256k1_context_destroy> secp256k1_context_destroy;
        readonly Lazy<secp256k1_ec_privkey_tweak_add> secp256k1_ec_privkey_tweak_add;
        readonly Lazy<secp256k1_ec_privkey_tweak_mul> secp256k1_ec_privkey_tweak_mul;
        readonly Lazy<secp256k1_ec_pubkey_tweak_add> secp256k1_ec_pubkey_tweak_add;
        readonly Lazy<secp256k1_ec_pubkey_tweak_mul> secp256k1_ec_pubkey_tweak_mul;
        readonly Lazy<secp256k1_ec_pubkey_create> secp256k1_ec_pubkey_create;
        readonly Lazy<secp256k1_ec_seckey_verify> secp256k1_ec_seckey_verify;
        readonly Lazy<secp256k1_ec_pubkey_serialize> secp256k1_ec_pubkey_serialize;
        readonly Lazy<secp256k1_ec_pubkey_parse> secp256k1_ec_pubkey_parse;
        readonly Lazy<secp256k1_ecdsa_signature_parse_compact> secp256k1_ecdsa_signature_parse_compact;
        readonly Lazy<secp256k1_ecdsa_recoverable_signature_parse_compact> secp256k1_ecdsa_recoverable_signature_parse_compact;
        readonly Lazy<secp256k1_ecdsa_recoverable_signature_serialize_compact> secp256k1_ecdsa_recoverable_signature_serialize_compact;
        readonly Lazy<secp256k1_ecdsa_sign_recoverable> secp256k1_ecdsa_sign_recoverable;
        readonly Lazy<secp256k1_ecdsa_signature_serialize_der> secp256k1_ecdsa_signature_serialize_der;
        readonly Lazy<secp256k1_ecdsa_sign> secp256k1_ecdsa_sign;
        readonly Lazy<secp256k1_ecdsa_recover> secp256k1_ecdsa_recover;
        //readonly Lazy<ecdsa_signature_parse_der_lax> ecdsa_signature_parse_der_lax;
        readonly Lazy<secp256k1_ecdsa_signature_normalize> secp256k1_ecdsa_signature_normalize;
        readonly Lazy<secp256k1_ecdsa_verify> secp256k1_ecdsa_verify;
        readonly Lazy<secp256k1_ecdh> secp256k1_ecdh;

        const string LIB = "secp256k1";

        public static string LibPath => _libPath.Value;
        static readonly Lazy<string> _libPath = new Lazy<string>(() => LibPathResolver.Resolve(LIB), true);
        static readonly Lazy<IntPtr> _libPtr = new Lazy<IntPtr>(() => LoadLibNative.LoadLib(_libPath.Value), true);

        IntPtr _ctx;

        public Secp256k1(bool sign = true, bool verify = true)
        {
            secp256k1_context_create = LazyDelegate<secp256k1_context_create>();
            secp256k1_context_destroy = LazyDelegate<secp256k1_context_destroy>();
            secp256k1_context_randomize = LazyDelegate<secp256k1_context_randomize>();
            secp256k1_ec_pubkey_create = LazyDelegate<secp256k1_ec_pubkey_create>();
            secp256k1_ec_pubkey_serialize = LazyDelegate<secp256k1_ec_pubkey_serialize>();
            secp256k1_ec_pubkey_parse = LazyDelegate<secp256k1_ec_pubkey_parse>();
            secp256k1_ec_seckey_verify = LazyDelegate<secp256k1_ec_seckey_verify>();
            secp256k1_ec_privkey_tweak_add = LazyDelegate<secp256k1_ec_privkey_tweak_add>();
            secp256k1_ec_privkey_tweak_mul = LazyDelegate<secp256k1_ec_privkey_tweak_mul>();
            secp256k1_ec_pubkey_tweak_add = LazyDelegate<secp256k1_ec_pubkey_tweak_add>();
            secp256k1_ec_pubkey_tweak_mul = LazyDelegate<secp256k1_ec_pubkey_tweak_mul>();
            secp256k1_ecdsa_recover = LazyDelegate<secp256k1_ecdsa_recover>();
            secp256k1_ecdsa_verify = LazyDelegate<secp256k1_ecdsa_verify>();
            secp256k1_ecdsa_sign = LazyDelegate<secp256k1_ecdsa_sign>();
            secp256k1_ecdsa_sign_recoverable = LazyDelegate<secp256k1_ecdsa_sign_recoverable>();
            secp256k1_ecdsa_signature_serialize_der = LazyDelegate<secp256k1_ecdsa_signature_serialize_der>();
            secp256k1_ecdsa_signature_normalize = LazyDelegate<secp256k1_ecdsa_signature_normalize>();
            secp256k1_ecdsa_signature_parse_compact = LazyDelegate<secp256k1_ecdsa_signature_parse_compact>();
            secp256k1_ecdsa_recoverable_signature_parse_compact = LazyDelegate<secp256k1_ecdsa_recoverable_signature_parse_compact>();
            secp256k1_ecdsa_recoverable_signature_serialize_compact = LazyDelegate<secp256k1_ecdsa_recoverable_signature_serialize_compact>();
            secp256k1_ecdh = LazyDelegate<secp256k1_ecdh>();
            //ecdsa_signature_parse_der_lax = LazyDelegate<ecdsa_signature_parse_der_lax>();

            var flags = Flags.SECP256K1_CONTEXT_NONE;
            if (sign) flags = flags | Flags.SECP256K1_CONTEXT_SIGN;
            if (verify) flags = flags | Flags.SECP256K1_CONTEXT_VERIFY;

            _ctx = secp256k1_context_create.Value((uint)flags);
        }

        Lazy<TDelegate> LazyDelegate<TDelegate>()
        {
            var symbol = SymbolNameCache<TDelegate>.SymbolName;
            return new Lazy<TDelegate>(() => LoadLibNative.GetDelegate<TDelegate>(_libPtr.Value, symbol), isThreadSafe: true);
        }

        public bool PubKeyTweakAdd(Span<byte> key, ReadOnlySpan<byte> tweak)
        {
            fixed (byte* keyPtr = &MemoryMarshal.GetReference(key), tweakPtr = &MemoryMarshal.GetReference(tweak)) {
                return 1 == secp256k1_ec_pubkey_tweak_add.Value(_ctx, keyPtr, tweakPtr);
            }
        }

        public bool PubKeyTweakMul(Span<byte> key, ReadOnlySpan<byte> tweak)
        {
            fixed (byte* keyPtr = &MemoryMarshal.GetReference(key), tweakPtr = &MemoryMarshal.GetReference(tweak)) {
                return 1 == secp256k1_ec_pubkey_tweak_mul.Value(_ctx, keyPtr, tweakPtr);
            }
        }

        public bool PrivKeyTweakAdd(Span<byte> key, ReadOnlySpan<byte> tweak)
        {
            fixed (byte* keyPtr = &MemoryMarshal.GetReference(key), tweakPtr = &MemoryMarshal.GetReference(tweak)) {
                return 1 == secp256k1_ec_privkey_tweak_add.Value(_ctx, keyPtr, tweakPtr);
            }
        }

        public bool PrivKeyTweakMul(Span<byte> key, ReadOnlySpan<byte> tweak)
        {
            fixed (byte* keyPtr = &MemoryMarshal.GetReference(key), tweakPtr = &MemoryMarshal.GetReference(tweak)) {
                return 1 == secp256k1_ec_privkey_tweak_mul.Value(_ctx, keyPtr, tweakPtr);
            }
        }

        /// <summary>
        /// Updates the context randomization to protect against side-channel leakage.
        ///
        /// While secp256k1 code is written to be constant-time no matter what secret
        /// values are, it's possible that a future compiler may output code which isn't,
        /// and also that the CPU may not emit the same radio frequencies or draw the same
        /// amount power for all values.
        ///
        /// This function provides a seed which is combined into the blinding value: that
        /// blinding value is added before each multiplication (and removed afterwards) so
        /// that it does not affect function results, but shields against attacks which
        /// rely on any input-dependent behaviour.
        ///
        /// You should call this after secp256k1_context_create or
        /// secp256k1_context_clone, and may call this repeatedly afterwards.
        /// </summary>
        /// <param name="seed32">32 bytes of random seed (Empty resets to initial state)</param>
        /// <returns> True if randomization successfully updated</returns>
        public bool Randomize(ReadOnlySpan<byte> seed32)
        {
            if (seed32 == Span<byte>.Empty)
                return secp256k1_context_randomize.Value(_ctx, null) == 1;

            if (seed32.Length < 32)
            {
                throw new ArgumentException($"{nameof(seed32)} must be 32 bytes");
            }

            fixed (byte* seed32Ptr = &MemoryMarshal.GetReference(seed32))
            {
                return secp256k1_context_randomize.Value(_ctx, seed32Ptr) == 1;
            }
        }

        /// <summary>
        /// Recover an ECDSA public key from a signature.
        /// </summary>
        /// <param name="publicKeyOutput">Output for the 64 byte recovered public key to be written to.</param>
        /// <param name="signature">The initialized signature that supports pubkey recovery.</param>
        /// <param name="message">The 32-byte message hash assumed to be signed.</param>
        /// <returns>
        /// True if the public key successfully recovered (which guarantees a correct signature).
        /// </returns>
        public bool Recover(Span<byte> publicKeyOutput, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> message)
        {
            if (publicKeyOutput.Length < PUBKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(publicKeyOutput)} must be {PUBKEY_LENGTH} bytes");
            }
            if (signature.Length < UNSERIALIZED_SIGNATURE_SIZE)
            {
                throw new ArgumentException($"{nameof(signature)} must be {UNSERIALIZED_SIGNATURE_SIZE} bytes");
            }
            if (message.Length < 32)
            {
                throw new ArgumentException($"{nameof(message)} must be 32 bytes");
            }

            fixed (byte* publicKeyPtr = &MemoryMarshal.GetReference(publicKeyOutput),
                sigPtr = &MemoryMarshal.GetReference(signature),
                msgPtr = &MemoryMarshal.GetReference(message))
            {
                return secp256k1_ecdsa_recover.Value(_ctx, publicKeyPtr, sigPtr, msgPtr) == 1;
            }
        }

        /// <summary>
        /// Verify an ECDSA secret key.
        /// </summary>
        /// <param name="secretKey">32-byte secret key.</param>
        /// <returns>True if secret key is valid, false if secret key is invalid.</returns>
        public bool SecretKeyVerify(ReadOnlySpan<byte> secretKey)
        {
            if (secretKey.Length < PRIVKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(secretKey)} must be {PRIVKEY_LENGTH} bytes");
            }

            fixed (byte* privKeyPtr = &MemoryMarshal.GetReference(secretKey))
            {
                return secp256k1_ec_seckey_verify.Value(_ctx, privKeyPtr) == 1;
            }
        }

        /// <summary>
        /// Gets the public key for a given private key.
        /// </summary>
        /// <param name="publicKeyOutput">Output for the 64 byte recovered public key to be written to.</param>
        /// <param name="privateKeyInput">The input private key to obtain the public key for.</param>
        /// <returns>
        /// True if the private key is valid and public key was obtained.
        /// </returns>
        public bool PublicKeyCreate(Span<byte> publicKeyOutput, ReadOnlySpan<byte> privateKeyInput)
        {
            if (publicKeyOutput.Length < PUBKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(publicKeyOutput)} must be {PUBKEY_LENGTH} bytes");
            }
            if (privateKeyInput.Length < PRIVKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(privateKeyInput)} must be {PRIVKEY_LENGTH} bytes");
            }

            fixed (byte* pubKeyPtr = &MemoryMarshal.GetReference(publicKeyOutput),
                privKeyPtr = &MemoryMarshal.GetReference(privateKeyInput))
            {
                return secp256k1_ec_pubkey_create.Value(_ctx, pubKeyPtr, privKeyPtr) == 1;
            }
        }

        /// <summary>
        /// Parse a signature in "lax DER" format
        ///
        /// This function will accept any valid DER encoded signature, even if the
        /// encoded numbers are out of range. In addition, it will accept signatures
        /// which violate the DER spec in various ways. Its purpose is to allow
        /// validation of the Bitcoin blockchain, which includes non-DER signatures
        /// from before the network rules were updated to enforce DER. Note that
        /// the set of supported violations is a strict subset of what OpenSSL will
        /// accept.
        ///
        /// After the call, sig will always be initialized. If parsing failed or the
        /// encoded numbers are out of range, signature validation with it is
        /// guaranteed to fail for every message and public key.
        ///
        /// This function is re-implemented from C code in lax_der_parsing.c
        /// https://github.com/MeadowSuite/secp256k1
        /// </summary>
        /// <param name="input">DER encoded signature to be parsed.</param>
        /// <returns>(ok, sig) tuple where ok is true if parse succeeded. sig is valid only if ok is true.</returns>
        public (bool ok, byte[] sig) ecdsa_signature_parse_der_lax(ReadOnlySpan<byte> input)
        {
            var tmpsig = new byte[64];
            var sig = new byte[64];
            var rlen = 0;
            var slen = 0;
            var overflow = false;

            fixed (byte* sigPtr = &MemoryMarshal.GetReference(sig.AsSpan()),
                tmpsigPtr = &MemoryMarshal.GetReference(tmpsig.AsSpan())) {

                /* Hack to initialize sig with a correctly-parsed but invalid signature. */
                secp256k1_ecdsa_signature_parse_compact.Value(_ctx, sigPtr, tmpsigPtr);

                var pos = 0;
                /* Sequence tag byte */
                if (pos == input.Length || input[pos] != 0x30) goto fail;
                pos++;

                /* Sequence length bytes */
                if (pos == input.Length) goto fail;
                var lenbyte = input[pos++];
                if ((lenbyte & 0x80) != 0) {
                    lenbyte -= 0x80;
                    if (pos + lenbyte > input.Length) goto fail;
                    pos += lenbyte;
                }

                /* Integer tag byte for R */
                if (pos == input.Length || input[pos] != 0x02) goto fail;
                pos++;

                /* Integer length for R */
                if (pos == input.Length) goto fail;
                lenbyte = input[pos++];
                if ((lenbyte & 0x80) != 0) {
                    lenbyte -= 0x80;
                    if (pos + lenbyte > input.Length) goto fail;
                    while (lenbyte > 0 && input[pos] == 0) {
                        pos++;
                        lenbyte--;
                    }
                    if (lenbyte >= sizeof(int)) goto fail;
                    rlen = 0;
                    while (lenbyte > 0) {
                        rlen = (rlen << 8) + input[pos];
                        pos++;
                        lenbyte--;
                    }
                } else {
                    rlen = lenbyte;
                }
                if (rlen > input.Length - pos) goto fail;
                var rpos = pos;
                pos += rlen;

                /* Integer tag byte for S */
                if (pos == input.Length || input[pos] != 0x02) goto fail;
                pos++;

                /* Integer length for S */
                if (pos == input.Length) goto fail;
                lenbyte = input[pos++];
                if ((lenbyte & 0x80) != 0) {
                    lenbyte -= 0x80;
                    if (pos + lenbyte > input.Length) goto fail;
                    while (lenbyte > 0 && input[pos] == 0) {
                        pos++;
                        lenbyte--;
                    }
                    if (lenbyte >= sizeof(int)) goto fail;
                    slen = 0;
                    while (lenbyte > 0) {
                        slen = (slen << 8) + input[pos];
                        pos++;
                        lenbyte--;
                    }
                } else {
                    slen = lenbyte;
                }
                if (slen > input.Length - pos) goto fail;
                var spos = pos;
                pos += slen;

                /* Ignore leading zeroes in R */
                while (rlen > 0 && input[rpos] == 0) {
                    rlen--;
                    rpos++;
                }
                /* Copy R value */
                if (rlen > 32) {
                    overflow = true;
                } else {
                    input.Slice(rpos, rlen).CopyTo(tmpsig.AsSpan().Slice(32 - rlen));
                }

                /* Ignore leading zeroes in S */
                while (slen > 0 && input[spos] == 0) {
                    slen--;
                    spos++;
                }
                /* Copy S value */
                if (slen > 32) {
                    overflow = true;
                } else {
                    input.Slice(spos, slen).CopyTo(tmpsig.AsSpan().Slice(64 - slen));
                }

                if (!overflow) {
                    overflow = 0 == secp256k1_ecdsa_signature_parse_compact.Value(_ctx, sigPtr, tmpsigPtr);
                }
                if (overflow) {
                    tmpsig.AsSpan().Fill(0);
                    secp256k1_ecdsa_signature_parse_compact.Value(_ctx, sigPtr, tmpsigPtr);
                }
                return (true, sig);

            fail:
                return (false, null);
            }
        }

        public bool PublicKeyVerify(ReadOnlySpan<byte> messageHash, ReadOnlySpan<byte> sigDer, ReadOnlySpan<byte> pubKeyData)
        {
            if (messageHash.Length < 32 || sigDer.Length == 0)
                return false;

            var pubKey = new byte[64];

            fixed (byte* pubKeyPtr = &MemoryMarshal.GetReference(pubKey.AsSpan()),
                pubKeyDataPtr = &MemoryMarshal.GetReference(pubKeyData)
                ) {

                if (0 == secp256k1_ec_pubkey_parse.Value(_ctx, pubKeyPtr, pubKeyDataPtr, (uint)pubKeyData.Length))
                    return false;

                var (ok, sig) = ecdsa_signature_parse_der_lax(sigDer);
                if (!ok)
                    return false;

                fixed (byte* messageHashPtr = &MemoryMarshal.GetReference(messageHash),
                    sigPtr = &MemoryMarshal.GetReference(sig.AsSpan())
                    ) {

                    //
                    // libsecp256k1's ECDSA verification requires lower-S signatures, which have
                    // not historically been enforced in Bitcoin, so normalize them first.
                    //
                    secp256k1_ecdsa_signature_normalize.Value(_ctx, sigPtr, sigPtr);
                    return 1 == secp256k1_ecdsa_verify.Value(_ctx, sigPtr, messageHashPtr, pubKeyPtr);
                }
            }
        }

        /// <summary>
        /// Create a ECDSA signature of messageHash using secretKey (private key) by call to secp256k1_ecdsa_sign.
        /// Return DER serialization of the signature by call to secp256k1_ecdsa_signature_serialize_der.
        /// </summary>
        /// <param name="messageHash">32 byte hash</param>
        /// <param name="secretKey">32 byte private key</param>
        /// <returns>DER format signature</returns>
        public (bool ok, byte[] sigDer) PrivateKeySign(ReadOnlySpan<byte> messageHash, ReadOnlySpan<byte> secretKey)
        {
            if (messageHash.Length < 32 || secretKey.Length < PRIVKEY_LENGTH)
                return (false, null);

            var sig = new byte[64];
            var sigDer = new byte[72];
            var sigDerLen = sigDer.Length;

            fixed (byte* sigPtr = &MemoryMarshal.GetReference(sig.AsSpan()),
                msgPtr = &MemoryMarshal.GetReference(messageHash),
                secPtr = &MemoryMarshal.GetReference(secretKey.Slice(secretKey.Length - 32)),
                sigDerPtr = &MemoryMarshal.GetReference(sigDer.AsSpan())) {

                var ret = secp256k1_ecdsa_sign.Value(_ctx, sigPtr, msgPtr, secPtr, IntPtr.Zero, null);
                if (ret != 1)
                    return (false, null);

                secp256k1_ecdsa_signature_serialize_der.Value(_ctx, sigDerPtr, ref sigDerLen, sigPtr);
            }

            if (sigDerLen < sigDer.Length)
                sigDer = sigDer.AsSpan().Slice(0, sigDerLen).ToArray();

            return (true, sigDer);
        }

        /// <summary>
        /// Parse a compact ECDSA signature (64 bytes + recovery id).
        /// </summary>
        /// <param name="signatureOutput">Output for the signature to be written to.</param>
        /// <param name="compactSignature">The 64-byte compact signature input.</param>
        /// <param name="recoveryID">The recovery id (0, 1, 2 or 3).</param>
        /// <returns>True when the signature could be parsed.</returns>
        public bool RecoverableSignatureParseCompact(Span<byte> signatureOutput, ReadOnlySpan<byte> compactSignature, int recoveryID)
        {
            if (signatureOutput.Length < UNSERIALIZED_SIGNATURE_SIZE)
            {
                throw new ArgumentException($"{nameof(signatureOutput)} must be 64 bytes");
            }
            if (compactSignature.Length < SERIALIZED_SIGNATURE_SIZE)
            {
                throw new ArgumentException($"{nameof(compactSignature)} must be 64 bytes");
            }

            fixed (byte* sigPtr = &MemoryMarshal.GetReference(signatureOutput),
                inputPtr = &MemoryMarshal.GetReference(compactSignature))
            {
                return secp256k1_ecdsa_recoverable_signature_parse_compact.Value(_ctx, sigPtr, inputPtr, recoveryID) == 1;
            }
        }

        /// <summary>
        /// Create a recoverable ECDSA signature.
        /// </summary>
        /// <param name="signatureOutput">Output where the signature will be placed.</param>
        /// <param name="messageHash">The 32-byte message hash being signed.</param>
        /// <param name="secretKey">A 32-byte secret key.</param>
        /// <returns>
        /// True if signature created, false if the nonce generation function failed, or the private key was invalid.
        /// </returns>
        public bool SignRecoverable(Span<byte> signatureOutput, Span<byte> messageHash, Span<byte> secretKey)
        {
            if (signatureOutput.Length < UNSERIALIZED_SIGNATURE_SIZE)
            {
                throw new ArgumentException($"{nameof(signatureOutput)} must be 65 bytes");
            }
            if (messageHash.Length < 32)
            {
                throw new ArgumentException($"{nameof(messageHash)} must be 32 bytes");
            }
            if (secretKey.Length < PRIVKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(secretKey)} must be 32 bytes");
            }

            fixed (byte* sigPtr = &MemoryMarshal.GetReference(signatureOutput),
                msgPtr = &MemoryMarshal.GetReference(messageHash),
                secPtr = &MemoryMarshal.GetReference(secretKey.Slice(secretKey.Length - 32)))
            {

                return secp256k1_ecdsa_sign_recoverable.Value(_ctx, sigPtr, msgPtr, secPtr, IntPtr.Zero, IntPtr.Zero) == 1;
            }
        }


        /// <summary>
        /// Serialize an ECDSA signature in compact format (64 bytes + recovery id).
        /// </summary>
        /// <param name="compactSignatureOutput">Output for the 64-byte array of the compact signature.</param>
        /// <param name="recoveryID">The recovery ID.</param>
        /// <param name="signature">The initialized signature.</param>
        public bool RecoverableSignatureSerializeCompact(Span<byte> compactSignatureOutput, out int recoveryID, Span<byte> signature)
        {
            if (compactSignatureOutput.Length < SERIALIZED_SIGNATURE_SIZE)
            {
                throw new ArgumentException($"{nameof(compactSignatureOutput)} must be {SERIALIZED_SIGNATURE_SIZE} bytes");
            }
            if (signature.Length < UNSERIALIZED_SIGNATURE_SIZE)
            {
                throw new ArgumentException($"{nameof(signature)} must be {UNSERIALIZED_SIGNATURE_SIZE} bytes");
            }

            int recID = 0;
            fixed (byte* compactSigPtr = &MemoryMarshal.GetReference(compactSignatureOutput),
                sigPtr = &MemoryMarshal.GetReference(signature))
            {
                var result = secp256k1_ecdsa_recoverable_signature_serialize_compact.Value(_ctx, compactSigPtr, ref recID, sigPtr);
                recoveryID = recID;

                return result == 1;
            }            
        }

        /// <summary>
        /// Complementary method is PublicKeyRecoverCompact.
        /// </summary>
        /// <param name="messageHash">32 byte hash</param>
        /// <param name="secretKey">32 byte private key</param>
        /// <param name="fCompressed"></param>
        /// <returns>Compact format signature</returns>
        public (bool ok, byte[] sigCompact) PrivateKeySignCompact(ReadOnlySpan<byte> messageHash, ReadOnlySpan<byte> secretKey, bool fCompressed)
        {
            if (messageHash.Length < 32 || secretKey.Length < PRIVKEY_LENGTH)
                return (false, null);

            var sig = new byte[64];
            var sigCompact = new byte[SERIALIZED_SIGNATURE_SIZE + 1];

            fixed (byte* sigPtr = &MemoryMarshal.GetReference(sig.AsSpan()),
                msgPtr = &MemoryMarshal.GetReference(messageHash),
                secPtr = &MemoryMarshal.GetReference(secretKey.Slice(secretKey.Length - 32)),
                sigCompactPtr = &MemoryMarshal.GetReference(sigCompact.AsSpan().Slice(1))) {

                var ret = secp256k1_ecdsa_sign_recoverable.Value(_ctx, sigPtr, msgPtr, secPtr, IntPtr.Zero, IntPtr.Zero);
                if (ret != 1)
                    return (false, null);

                var recID = -1;
                ret = secp256k1_ecdsa_recoverable_signature_serialize_compact.Value(_ctx, sigCompactPtr, ref recID, sigPtr);
                if (ret != 1 || recID == -1)
                    return (false, null);

                sigCompact[0] = (byte)(27 + recID + (fCompressed ? 4 : 0));

                return (true, sigCompact);
            }
        }

        /// <summary>
        /// Complementary method is PrivateKeySignCompact.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="sig"></param>
        /// <returns></returns>
        public (bool ok, byte[] vch) PublicKeyRecoverCompact(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> sig)
        {
            if (sig.Length != 65) goto fail;

            var recid = (sig[0] - 27) & 3;
            var fComp = ((sig[0] - 27) & 4) != 0;

            var compactSignature = sig.Slice(1);
            var signature = new byte[UNSERIALIZED_SIGNATURE_SIZE];
            if (!RecoverableSignatureParseCompact(signature, compactSignature, recid)) goto fail;

            var publicKey = new byte[PUBKEY_LENGTH];
            if (!Recover(publicKey, signature, hash)) goto fail;

            var vch = new byte[fComp ? SERIALIZED_COMPRESSED_PUBKEY_LENGTH : SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            if (!PublicKeySerialize(vch, publicKey, fComp ? Flags.SECP256K1_EC_COMPRESSED : Flags.SECP256K1_EC_UNCOMPRESSED)) goto fail;

            return (true, vch);

            fail:
            return (false, null);
        }


        /// <summary>
        /// Serialize a pubkey object into a serialized byte sequence.
        /// </summary>
        /// <param name="serializedPublicKeyOutput">65-byte (if compressed==0) or 33-byte (if compressed==1) output to place the serialized key in.</param>
        /// <param name="publicKey">The secp256k1_pubkey initialized public key.</param>
        /// <param name="flags">SECP256K1_EC_COMPRESSED if serialization should be in compressed format, otherwise SECP256K1_EC_UNCOMPRESSED.</param>
        public bool PublicKeySerialize(Span<byte> serializedPublicKeyOutput, ReadOnlySpan<byte> publicKey, Flags flags = Flags.SECP256K1_EC_UNCOMPRESSED)
        {
            bool compressed = flags.HasFlag(Flags.SECP256K1_EC_COMPRESSED);
            int serializedPubKeyLength = compressed ? SERIALIZED_COMPRESSED_PUBKEY_LENGTH : SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH;
            if (serializedPublicKeyOutput.Length < serializedPubKeyLength)
            {
                string compressedStr = compressed ? "compressed" : "uncompressed";
                throw new ArgumentException($"{nameof(serializedPublicKeyOutput)} ({compressedStr}) must be {serializedPubKeyLength} bytes");
            }
            if (publicKey.Length < PUBKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(publicKey)} must be {PUBKEY_LENGTH} bytes");
            }

            uint newLength = (uint)serializedPubKeyLength;

            fixed (byte* serializedPtr = &MemoryMarshal.GetReference(serializedPublicKeyOutput),
                pubKeyPtr = &MemoryMarshal.GetReference(publicKey))
            {
                var result = secp256k1_ec_pubkey_serialize.Value(_ctx, serializedPtr, ref newLength, pubKeyPtr, (uint) flags);
                return result == 1 && newLength == serializedPubKeyLength;
            }
        }

        /// <summary>
        /// Parse a variable-length public key into the pubkey object.
        /// This function supports parsing compressed (33 bytes, header byte 0x02 or
        /// 0x03), uncompressed(65 bytes, header byte 0x04), or hybrid(65 bytes, header
        /// byte 0x06 or 0x07) format public keys.
        /// </summary>
        /// <param name="publicKeyOutput">(Output) pointer to a pubkey object. If 1 is returned, it is set to a parsed version of input. If not, its value is undefined.</param>
        /// <param name="serializedPublicKey">Serialized public key.</param>
        /// <returns>True if the public key was fully valid, false if the public key could not be parsed or is invalid.</returns>
        public bool PublicKeyParse(Span<byte> publicKeyOutput, ReadOnlySpan<byte> serializedPublicKey)
        {
            var inputLen = serializedPublicKey.Length;
            if (inputLen != 33 && inputLen != 65)
            {
                throw new ArgumentException($"{nameof(serializedPublicKey)} must be 33 or 65 bytes");
            }
            if (publicKeyOutput.Length < PUBKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(publicKeyOutput)} must be {PUBKEY_LENGTH} bytes");
            }

            fixed (byte* pubKeyPtr = &MemoryMarshal.GetReference(publicKeyOutput),
                serializedPtr = &MemoryMarshal.GetReference(serializedPublicKey))
            {
                return secp256k1_ec_pubkey_parse.Value(_ctx, pubKeyPtr, serializedPtr, (uint) inputLen) == 1;
            }
        }

        public bool SignatureParseDerLax(Span<byte> signatureOutput, ReadOnlySpan<byte> input)
        {
            if (signatureOutput.Length < SIGNATURE_LENGTH)
            {
                throw new ArgumentException($"{nameof(signatureOutput)} must be {SIGNATURE_LENGTH} bytes");
            }

            var (ok, sig) = ecdsa_signature_parse_der_lax(input);
            if (ok)
                sig.CopyTo(signatureOutput);
            return ok;
        }

        /// <summary>
        /// Normalizes a signature and enforces a low-S.
        /// </summary>
        /// <param name="normalizedSignatureOutput">(Output) Signature to fill with the normalized form, or copy if the input was already normalized.</param>
        /// <param name="signatureInput">(Input) signature to check/normalize, can be identical to sigout</param>
        /// <returns>True if sigin was not normalized, false if it already was.</returns>
        public bool SignatureNormalize(Span<byte> normalizedSignatureOutput, ReadOnlySpan<byte> signatureInput)
        {
            if (!normalizedSignatureOutput.IsEmpty && normalizedSignatureOutput.Length < SIGNATURE_LENGTH)
            {
                throw new ArgumentException($"{nameof(normalizedSignatureOutput)} must be {SIGNATURE_LENGTH} bytes");
            }
            if (signatureInput.Length < SIGNATURE_LENGTH)
            {
                throw new ArgumentException($"{nameof(signatureInput)} must be {SIGNATURE_LENGTH} bytes");
            }

            fixed (byte* intPtr = &MemoryMarshal.GetReference(signatureInput)) {

                if (normalizedSignatureOutput.IsEmpty)
                    return secp256k1_ecdsa_signature_normalize.Value(_ctx, null, intPtr) == 1;

                fixed (byte* outPtr = &MemoryMarshal.GetReference(normalizedSignatureOutput)) {
                    return secp256k1_ecdsa_signature_normalize.Value(_ctx, outPtr, intPtr) == 1;
                }
            }
        }

        /// <summary>
        /// Verify an ECDSA signature.
        /// To avoid accepting malleable signatures, only ECDSA signatures in lower-S
        /// form are accepted.
        /// If you need to accept ECDSA signatures from sources that do not obey this
        /// rule, apply secp256k1_ecdsa_signature_normalize to the signature prior to
        /// validation, but be aware that doing so results in malleable signatures.
        /// For details, see the comments for that function.
        /// </summary>
        /// <param name="signature">The signature being verified.</param>
        /// <param name="messageHash">The 32-byte message hash being verified.</param>
        /// <param name="publicKey">An initialized public key to verify with.</param>
        /// <returns>True if correct signature, false if incorrect or unparseable signature.</returns>
        public bool Verify(Span<byte> signature, Span<byte> messageHash, Span<byte> publicKey)
        {
            if (signature.Length < SIGNATURE_LENGTH)
            {
                throw new ArgumentException($"{nameof(signature)} must be {SIGNATURE_LENGTH} bytes");
            }
            if (messageHash.Length < HASH_LENGTH)
            {
                throw new ArgumentException($"{nameof(messageHash)} must be {HASH_LENGTH} bytes");
            }
            if (publicKey.Length < PUBKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(publicKey)} must be {PUBKEY_LENGTH} bytes");
            }

            fixed (byte* sigPtr = &MemoryMarshal.GetReference(signature),
                msgPtr = &MemoryMarshal.GetReference(messageHash),
                pubPtr = &MemoryMarshal.GetReference(publicKey))
            {
                return secp256k1_ecdsa_verify.Value(_ctx, sigPtr, msgPtr, pubPtr) == 1;
            }
        }

        /// <summary>
        /// Create an ECDSA signature.
        /// The created signature is always in lower-S form. See
        /// secp256k1_ecdsa_signature_normalize for more details.
        /// </summary>
        /// <param name="signatureOutput">An array where the signature will be placed.</param>
        /// <param name="messageHash">The 32-byte message hash being signed.</param>
        /// <param name="secretKey">A 32-byte secret key. Or Empty to test signing.</param>
        /// <returns></returns>
        public bool Sign(Span<byte> signatureOutput, Span<byte> messageHash, Span<byte> secretKey)
        {
            if (signatureOutput.Length < SIGNATURE_LENGTH)
            {
                throw new ArgumentException($"{nameof(signatureOutput)} must be {SIGNATURE_LENGTH} bytes");
            }
            if (messageHash.Length < HASH_LENGTH)
            {
                throw new ArgumentException($"{nameof(messageHash)} must be {HASH_LENGTH} bytes");
            }
            if (secretKey.Length < PRIVKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(secretKey)} must be {PRIVKEY_LENGTH} bytes");
            }

            fixed (byte* sigPtr = &MemoryMarshal.GetReference(signatureOutput),
                msgPtr = &MemoryMarshal.GetReference(messageHash),
                secPtr = &MemoryMarshal.GetReference(secretKey))
            {
                return secp256k1_ecdsa_sign.Value(_ctx, sigPtr, msgPtr, secPtr, IntPtr.Zero, IntPtr.Zero.ToPointer()) == 1;
            }
        }

        /// <summary>
        /// Compute an EC Diffie-Hellman secret in constant time.
        /// </summary>
        /// <param name="resultOutput">A 32-byte array which will be populated by an ECDH secret computed from the point and scalar.</param>
        /// <param name="publicKey">A secp256k1_pubkey containing an initialized public key.</param>
        /// <param name="privateKey">A 32-byte scalar with which to multiply the point.</param>
        /// <returns>True if exponentiation was successful, false if scalar was invalid (zero or overflow).</returns>
        public bool Ecdh(Span<byte> resultOutput, Span<byte> publicKey, Span<byte> privateKey)
        {
            if (resultOutput.Length < SECRET_LENGTH)
            {
                throw new ArgumentException($"{nameof(resultOutput)} must be {SECRET_LENGTH} bytes");
            }
            if (publicKey.Length < PUBKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(publicKey)} must be {PUBKEY_LENGTH} bytes");
            }
            if (privateKey.Length < PRIVKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(privateKey)} must be {PRIVKEY_LENGTH} bytes");
            }

            fixed (byte* resPtr = &MemoryMarshal.GetReference(resultOutput),
                pubPtr = &MemoryMarshal.GetReference(publicKey),
                privPtr = &MemoryMarshal.GetReference(privateKey))
            {
                return secp256k1_ecdh.Value(_ctx, resPtr, pubPtr, privPtr, IntPtr.Zero, IntPtr.Zero) == 1;
            }
        }


        delegate int secp256k1_ecdh_hash_function(void* output, void* x, void* y, IntPtr data);

        /// <summary>
        /// Compute an EC Diffie-Hellman secret in constant time.
        /// </summary>
        /// <param name="resultOutput">A 32-byte array which will be populated by an ECDH secret computed from the point and scalar.</param>
        /// <param name="publicKey">A secp256k1_pubkey containing an initialized public key.</param>
        /// <param name="privateKey">A 32-byte scalar with which to multiply the point.</param>
        /// <param name="hashFunction">Pointer to a hash function. If null, sha256 is used.</param>
        /// <param name="data">Arbitrary data that is passed through.</param>
        /// <returns>True if exponentiation was successful, false if scalar was invalid (zero or overflow).</returns>
        public bool Ecdh(Span<byte> resultOutput, Span<byte> publicKey, Span<byte> privateKey, EcdhHashFunction hashFunction, IntPtr data)
        {
            if (resultOutput.Length < SECRET_LENGTH)
            {
                throw new ArgumentException($"{nameof(resultOutput)} must be {SECRET_LENGTH} bytes");
            }
            if (publicKey.Length < PUBKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(publicKey)} must be {PUBKEY_LENGTH} bytes");
            }
            if (privateKey.Length < PRIVKEY_LENGTH)
            {
                throw new ArgumentException($"{nameof(privateKey)} must be {PRIVKEY_LENGTH} bytes");
            }

            int outputLength = resultOutput.Length;

            secp256k1_ecdh_hash_function hashFunctionPtr = (void* output, void* x, void* y, IntPtr d) =>
            {
                var outputSpan = new Span<byte>(output, outputLength);
                var xSpan = new Span<byte>(x, 32);
                var ySpan = new Span<byte>(y, 32);
                return hashFunction(outputSpan, xSpan, ySpan, d);
            };

            GCHandle gch = GCHandle.Alloc(hashFunctionPtr);
            try
            {
                var fp = Marshal.GetFunctionPointerForDelegate(hashFunctionPtr);
                fixed (byte* resPtr = &MemoryMarshal.GetReference(resultOutput),
                    pubPtr = &MemoryMarshal.GetReference(publicKey),
                    privPtr = &MemoryMarshal.GetReference(privateKey))
                {
                    return secp256k1_ecdh.Value(_ctx, resPtr, pubPtr, privPtr, fp, data) == 1;
                }
            }
            finally
            {
                gch.Free();
            }
        }


        public void Dispose()
        {
            if (_ctx != IntPtr.Zero)
            {
                secp256k1_context_destroy.Value(_ctx);
                _ctx = IntPtr.Zero;
            }
        }



    }
}
