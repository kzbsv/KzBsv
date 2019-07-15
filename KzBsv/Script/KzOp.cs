#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;

namespace KzBsv
{
    public struct KzOp {
        KzOpcode _code;
        KzValType _data;

        public KzOpcode Code => _code;
        public KzValType Data => _data;

        public string CodeName => GetOpName(_code);

        public int LengthBytesCount => _code == KzOpcode.OP_PUSHDATA1 ? 1 : _code == KzOpcode.OP_PUSHDATA2 ? 2 : _code == KzOpcode.OP_PUSHDATA4 ? 4 : 0;

        public long Length => 1 + _data.Length + LengthBytesCount;

        public KzOp(KzOpcode code, KzValType data) { _code = code; _data = data; }

        public KzOp(KzOpcode code) { _code = code; _data = KzValType.None; }

        public static KzOp Push(ReadOnlySpan<byte> data)
        {
            var code = KzOpcode.OP_INVALIDOPCODE;
            var val = KzValType.None;

            if (data.Length == 1 && data[0] <= 16) {
                code = data[0] == 0 ? KzOpcode.OP_0 : (KzOpcode)(data[0] - 1 + (int)KzOpcode.OP_1);
            } else {
                if (data.Length < (int)KzOpcode.OP_PUSHDATA1) {
                    code = (KzOpcode)data.Length;
                } else if (data.Length <= 0xff) {
                    code = KzOpcode.OP_PUSHDATA1;
                } else if (data.Length <= 0xffff) {
                    code = KzOpcode.OP_PUSHDATA2;
                } else {
                    code = KzOpcode.OP_PUSHDATA4;
                }
                val = new KzValType(data.ToArray());
            }
            var op = new KzOp(code, val);
            return op;
        }

        public static KzOp Push(long v)
        {
            var code = KzOpcode.OP_INVALIDOPCODE;
            var val = KzValType.None;

            if (v == -1) {
                code = KzOpcode.OP_1NEGATE;
            } else if (v >= 0 && v <= 16) {
                code = v == 0 ? KzOpcode.OP_0 : (KzOpcode)(v - 1 + (int)KzOpcode.OP_1);
            } else {
                var bytes = BitConverter.GetBytes(v).AsSpan();
                if (v <= 0xff) {
                    code = KzOpcode.OP_PUSH1;
                    val = new KzValType(bytes.Slice(0, 1).ToArray());
                } else if (v <= 0xffff) {
                    code = KzOpcode.OP_PUSH2;
                    val = new KzValType(bytes.Slice(0, 2).ToArray());
                } else if (v <= 0xffffff) {
                    code = KzOpcode.OP_PUSH3;
                    val = new KzValType(bytes.Slice(0, 3).ToArray());
                } else {
                    code = KzOpcode.OP_PUSH4;
                    val = new KzValType(bytes.Slice(0, 4).ToArray());
                }
            }
            var op = new KzOp(code, val);
            return op;
        }

        public bool TryCopyTo(ref Span<byte> span)
        {
            var length = Length;
            if (length > span.Length)
                return false;
            span[0] = (byte)_code;
            span = span.Slice(1);
            length = _data.Length;
            if (_code >= KzOpcode.OP_PUSHDATA1 && _code <= KzOpcode.OP_PUSHDATA4) {
                if (!BitConverter.IsLittleEndian) return false;
                var lengthBytes = BitConverter.GetBytes((uint)_data.Length).AsSpan(0, LengthBytesCount);
                lengthBytes.CopyTo(span);
                span = span.Slice(lengthBytes.Length);
            }
            if (length > 0) {
                _data.GetReader().TryCopyTo(span.Slice(0, (int)_data.Length));
                span = span.Slice((int)length);
            }
            return true;
        }

        public IKzWriter AddTo(IKzWriter w)
        {
            w.Add((byte)_code);
            if (_code >= KzOpcode.OP_PUSHDATA1 && _code <= KzOpcode.OP_PUSHDATA4) {
                var lengthBytes = BitConverter.GetBytes((uint)_data.Length).AsSpan(0, LengthBytesCount);
                w.Add(lengthBytes);
            }
            if (_data.Length > 0)
                w.Add(_data.Sequence);
            return w;
        }

        public byte[] GetBytes()
        {
            var bytes = new byte[Length];
            bytes[0] = (byte)_code;
            if (bytes.Length > 1)
                _data.GetReader().TryCopyTo(bytes.AsSpan().Slice(1));
            return bytes;
        }

        /*
            // script.h lines 527-562
            bool GetOp2(const_iterator &pc, opcodetype &opcodeRet,
                std::vector<uint8_t> *pvchRet) const {
                opcodeRet = OP_INVALIDOPCODE;
                if (pvchRet) pvchRet->clear();
                if (pc >= end()) return false;

                // Read instruction
                if (end() - pc < 1) return false;
                unsigned int opcode = *pc++;

                // Immediate operand
                if (opcode <= OP_PUSHDATA4) {
                    unsigned int nSize = 0;
                    if (opcode < OP_PUSHDATA1) {
                        nSize = opcode;
                    } else if (opcode == OP_PUSHDATA1) {
                        if (end() - pc < 1) return false;
                        nSize = *pc++;
                    } else if (opcode == OP_PUSHDATA2) {
                        if (end() - pc < 2) return false;
                        nSize = ReadLE16(&pc[0]);
                        pc += 2;
                    } else if (opcode == OP_PUSHDATA4) {
                        if (end() - pc < 4) return false;
                        nSize = ReadLE32(&pc[0]);
                        pc += 4;
                    }
                    if (end() - pc < 0 || (unsigned int)(end() - pc) < nSize)
                        return false;
                    if (pvchRet) pvchRet->assign(pc, pc + nSize);
                    pc += nSize;
                }

                opcodeRet = (opcodetype)opcode;
                return true;
            }
        */

        public bool TryReadOp(ref ReadOnlySequence<byte> ros) => TryReadOp(ref ros, out _);

        public bool TryReadOp(ref ReadOnlySequence<byte> ros, out long consumed)
        {
            consumed = 0L;
            var r = new SequenceReader<byte>(ros);
            if (!TryReadOp(ref r)) goto fail;

            consumed = r.Consumed;
            ros = ros.Slice(r.Consumed);

            return true;
        fail:
            return false;
        }

        public bool TryReadOp(ref SequenceReader<byte> r)
        {
            _code = KzOpcode.OP_INVALIDOPCODE;
            _data = KzValType.None;

            if (!r.TryRead(out byte opcode)) goto fail;

            _code = (KzOpcode)opcode;

            // Opcodes OP_0 and OP_1 to OP_16 are single byte opcodes that push the corresponding value.
            // Opcodes from zero to 0x4b [0..75] are single byte push commands where the value is the number of bytes to push.
            // Opcode 0x4c (76) takes the next byte as the count and should be used for pushing [76..255] bytes.
            // Opcode 0x4d (77) takes the next two bytes. Used for pushing [256..65536] bytes.
            // Opcode 0x4e (78) takes the next four bytes. Used for pushing [65537..4,294,967,296] bytes.
            
            if (opcode <= (byte)KzOpcode.OP_PUSHDATA4) {
                var nSize = 0U;
                if (opcode < (byte)KzOpcode.OP_PUSHDATA1) {
                    nSize = opcode;
                } else if (opcode == (byte)KzOpcode.OP_PUSHDATA1) {
                    if (!r.TryRead(out byte size1)) goto fail;
                    nSize = size1;
                } else if (opcode == (byte)KzOpcode.OP_PUSHDATA2) {
                    if (!r.TryReadLittleEndian(out UInt16 size2)) goto fail;
                    nSize = size2;
                } else if (opcode == (byte)KzOpcode.OP_PUSHDATA4) {
                    if (!r.TryReadLittleEndian(out UInt32 size4)) goto fail;
                    nSize = size4;
                }
                if (nSize >= 0) {
                    if (r.Remaining < nSize) goto fail;
                    _data = new KzValType(r.Sequence.Slice(r.Position, (Int32)nSize));
                    r.Advance(nSize);
                }
            }
            return true;

        fail:
            return false;
        }

        public static string GetOpName(KzOpcode opcode)
        {
            return opcode switch
            {
                // push value
                KzOpcode.OP_0 => "0",
                KzOpcode.OP_PUSHDATA1 => "OP_PUSHDATA1",
                KzOpcode.OP_PUSHDATA2 => "OP_PUSHDATA2",
                KzOpcode.OP_PUSHDATA4 => "OP_PUSHDATA4",
                KzOpcode.OP_1NEGATE => "-1",
                KzOpcode.OP_RESERVED => "OP_RESERVED",
                KzOpcode.OP_1 => "1",
                KzOpcode.OP_2 => "2",
                KzOpcode.OP_3 => "3",
                KzOpcode.OP_4 => "4",
                KzOpcode.OP_5 => "5",
                KzOpcode.OP_6 => "6",
                KzOpcode.OP_7 => "7",
                KzOpcode.OP_8 => "8",
                KzOpcode.OP_9 => "9",
                KzOpcode.OP_10 => "10",
                KzOpcode.OP_11 => "11",
                KzOpcode.OP_12 => "12",
                KzOpcode.OP_13 => "13",
                KzOpcode.OP_14 => "14",
                KzOpcode.OP_15 => "15",
                KzOpcode.OP_16 => "16",

                // control
                KzOpcode.OP_NOP => "OP_NOP",
                KzOpcode.OP_VER => "OP_VER",
                KzOpcode.OP_IF => "OP_IF",
                KzOpcode.OP_NOTIF => "OP_NOTIF",
                KzOpcode.OP_VERIF => "OP_VERIF",
                KzOpcode.OP_VERNOTIF => "OP_VERNOTIF",
                KzOpcode.OP_ELSE => "OP_ELSE",
                KzOpcode.OP_ENDIF => "OP_ENDIF",
                KzOpcode.OP_VERIFY => "OP_VERIFY",
                KzOpcode.OP_RETURN => "OP_RETURN",

                // stack ops
                KzOpcode.OP_TOALTSTACK => "OP_TOALTSTACK",
                KzOpcode.OP_FROMALTSTACK => "OP_FROMALTSTACK",
                KzOpcode.OP_2DROP => "OP_2DROP",
                KzOpcode.OP_2DUP => "OP_2DUP",
                KzOpcode.OP_3DUP => "OP_3DUP",
                KzOpcode.OP_2OVER => "OP_2OVER",
                KzOpcode.OP_2ROT => "OP_2ROT",
                KzOpcode.OP_2SWAP => "OP_2SWAP",
                KzOpcode.OP_IFDUP => "OP_IFDUP",
                KzOpcode.OP_DEPTH => "OP_DEPTH",
                KzOpcode.OP_DROP => "OP_DROP",
                KzOpcode.OP_DUP => "OP_DUP",
                KzOpcode.OP_NIP => "OP_NIP",
                KzOpcode.OP_OVER => "OP_OVER",
                KzOpcode.OP_PICK => "OP_PICK",
                KzOpcode.OP_ROLL => "OP_ROLL",
                KzOpcode.OP_ROT => "OP_ROT",
                KzOpcode.OP_SWAP => "OP_SWAP",
                KzOpcode.OP_TUCK => "OP_TUCK",

                // splice ops
                KzOpcode.OP_CAT => "OP_CAT",
                KzOpcode.OP_SPLIT => "OP_SPLIT",
                KzOpcode.OP_NUM2BIN => "OP_NUM2BIN",
                KzOpcode.OP_BIN2NUM => "OP_BIN2NUM",
                KzOpcode.OP_SIZE => "OP_SIZE",

                // bit logic
                KzOpcode.OP_INVERT => "OP_INVERT",
                KzOpcode.OP_AND => "OP_AND",
                KzOpcode.OP_OR => "OP_OR",
                KzOpcode.OP_XOR => "OP_XOR",
                KzOpcode.OP_EQUAL => "OP_EQUAL",
                KzOpcode.OP_EQUALVERIFY => "OP_EQUALVERIFY",
                KzOpcode.OP_RESERVED1 => "OP_RESERVED1",
                KzOpcode.OP_RESERVED2 => "OP_RESERVED2",

                // numeric
                KzOpcode.OP_1ADD => "OP_1ADD",
                KzOpcode.OP_1SUB => "OP_1SUB",
                KzOpcode.OP_2MUL => "OP_2MUL",
                KzOpcode.OP_2DIV => "OP_2DIV",
                KzOpcode.OP_NEGATE => "OP_NEGATE",
                KzOpcode.OP_ABS => "OP_ABS",
                KzOpcode.OP_NOT => "OP_NOT",
                KzOpcode.OP_0NOTEQUAL => "OP_0NOTEQUAL",
                KzOpcode.OP_ADD => "OP_ADD",
                KzOpcode.OP_SUB => "OP_SUB",
                KzOpcode.OP_MUL => "OP_MUL",
                KzOpcode.OP_DIV => "OP_DIV",
                KzOpcode.OP_MOD => "OP_MOD",
                KzOpcode.OP_LSHIFT => "OP_LSHIFT",
                KzOpcode.OP_RSHIFT => "OP_RSHIFT",
                KzOpcode.OP_BOOLAND => "OP_BOOLAND",
                KzOpcode.OP_BOOLOR => "OP_BOOLOR",
                KzOpcode.OP_NUMEQUAL => "OP_NUMEQUAL",
                KzOpcode.OP_NUMEQUALVERIFY => "OP_NUMEQUALVERIFY",
                KzOpcode.OP_NUMNOTEQUAL => "OP_NUMNOTEQUAL",
                KzOpcode.OP_LESSTHAN => "OP_LESSTHAN",
                KzOpcode.OP_GREATERTHAN => "OP_GREATERTHAN",
                KzOpcode.OP_LESSTHANOREQUAL => "OP_LESSTHANOREQUAL",
                KzOpcode.OP_GREATERTHANOREQUAL => "OP_GREATERTHANOREQUAL",
                KzOpcode.OP_MIN => "OP_MIN",
                KzOpcode.OP_MAX => "OP_MAX",
                KzOpcode.OP_WITHIN => "OP_WITHIN",

                // crypto
                KzOpcode.OP_RIPEMD160 => "OP_RIPEMD160",
                KzOpcode.OP_SHA1 => "OP_SHA1",
                KzOpcode.OP_SHA256 => "OP_SHA256",
                KzOpcode.OP_HASH160 => "OP_HASH160",
                KzOpcode.OP_HASH256 => "OP_HASH256",
                KzOpcode.OP_CODESEPARATOR => "OP_CODESEPARATOR",
                KzOpcode.OP_CHECKSIG => "OP_CHECKSIG",
                KzOpcode.OP_CHECKSIGVERIFY => "OP_CHECKSIGVERIFY",
                KzOpcode.OP_CHECKMULTISIG => "OP_CHECKMULTISIG",
                KzOpcode.OP_CHECKMULTISIGVERIFY => "OP_CHECKMULTISIGVERIFY",

                // expansion
                KzOpcode.OP_NOP1 => "OP_NOP1",
                KzOpcode.OP_CHECKLOCKTIMEVERIFY => "OP_CHECKLOCKTIMEVERIFY",
                KzOpcode.OP_CHECKSEQUENCEVERIFY => "OP_CHECKSEQUENCEVERIFY",
                KzOpcode.OP_NOP4 => "OP_NOP4",
                KzOpcode.OP_NOP5 => "OP_NOP5",
                KzOpcode.OP_NOP6 => "OP_NOP6",
                KzOpcode.OP_NOP7 => "OP_NOP7",
                KzOpcode.OP_NOP8 => "OP_NOP8",
                KzOpcode.OP_NOP9 => "OP_NOP9",
                KzOpcode.OP_NOP10 => "OP_NOP10",

                KzOpcode.OP_INVALIDOPCODE => "OP_INVALIDOPCODE",

                // Note:
                //  The template matching params OP_SMALLINTEGER/etc are defined in
                //  opcodetype enum as kind of implementation hack, they are *NOT*
                //  real opcodes. If found in real Script, just let the default:
                //  case deal with them.

                _ => "OP_UNKNOWN"
            };
        }

        static KzEncode _hex = KzEncoders.Hex;

        public string ToVerboseString()
        {
            var s = _code.ToString();

            var len = Data.Length;
            if (len > 0)
                s += " " + _hex.Encode(Data.Sequence);
            return s;
        }

        public override string ToString()
        {
            var s = string.Empty;

            var len = Data.Length;
            if (len == 0)
                s = CodeName;
            else if (len < 100)
                s = _hex.Encode(Data.Sequence);
            else {
                var start = _hex.Encode(Data.Sequence.Slice(0, 32));
                var end = _hex.Encode(Data.Sequence.Slice(len - 32));
                s = $"{start}...[{Data.Length} bytes]...{end}";
            }
            return s;
        }
    }
}
