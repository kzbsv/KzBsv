#region Copyright
// Copyright (c) 2019 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace KzBsv
{
    public class KzStack<T> where T: struct
    {
        T[] _array;
        int _count;

        const int DefaultCapacity = 4;

        public KzStack()
        {
            _array = new T[DefaultCapacity];
        }

        public KzStack(int capacity)
        {
            _array = new T[capacity];
        }

        public int Count => _count;

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                Array.Clear(_array, 0, _count);
            }
            _count = 0;
        }

        public void TrimExcess()
        {
            int threshold = (int)(((double)_array.Length) * 0.9);
            if (_count < threshold) {
                Array.Resize(ref _array, _count);
            }
        }

        public T Peek()
        {
            return _array[_count - 1];
        }

        public bool TryPeek(out T result)
        {
            if (_count == 0) {
                result = default;
                return false;
            }
            result = _array[_count - 1];
            return true;
        }

        public T Pop()
        {
            var item = _array[--_count];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                _array[_count] = default;     // Free memory quicker.
            }
            return item;
        }

        public bool TryPop(out T result)
        {
            if (_count == 0) {
                result = default;
                return false;
            }

            result = _array[--_count];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                _array[_count] = default;     // Free memory quicker.
            }
            return true;
        }

        public void Push(T item)
        {
            if (_count == _array.Length)
                Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            _array[_count++] = item;
        }

        public T[] ToArray()
        {
            return _array.AsSpan(0, _count).ToArray();
        }

        public void Drop2()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                _array[--_count] = default;
                _array[--_count] = default;
            } else {
                _count -= 2;
            }
        }

        public void Dup2()
        {
            // (x1 x2 -- x1 x2 x1 x2)
            if (_count + 2 > _array.Length)
                Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            _array[_count++] = _array[_count - 3];
            _array[_count++] = _array[_count - 3];
        }

        public bool Contains(T v)
        {
            return _count != 0 && Array.LastIndexOf(_array, v, _count - 1) != -1;
        }

        public void Dup3()
        {
            // (x1 x2 x3 -- x1 x2 x3 x1 x2 x3)
            if (_count + 3 > _array.Length)
                Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            _array[_count++] = _array[_count - 4];
            _array[_count++] = _array[_count - 4];
            _array[_count++] = _array[_count - 4];
        }

        public void Over()
        {
            // (x1 x2 -- x1 x2 x1)
            if (_count + 1 > _array.Length)
                Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            _array[_count++] = _array[_count - 3];
        }

        public void Over2()
        {
            // (x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2)
            if (_count + 2 > _array.Length)
                Array.Resize(ref _array, (_array.Length == 0) ? DefaultCapacity : 2 * _array.Length);
            _array[_count++] = _array[_count - 5];
            _array[_count++] = _array[_count - 5];
        }

        public void Rot()
        {
            // (x1 x2 x3 -- x2 x3 x1)
            var x1 = _array[_count - 3];
            _array[_count - 3] = _array[_count - 2];
            _array[_count - 2] = _array[_count - 1];
            _array[_count - 1] = x1;
        }

        public void Rot2()
        {
            // (x1 x2 x3 x4 x5 x6 -- x3 x4 x5 x6 x1 x2)
            var x1 = _array[_count - 6];
            var x2 = _array[_count - 5];
            _array[_count - 6] = _array[_count - 4];
            _array[_count - 5] = _array[_count - 3];
            _array[_count - 4] = _array[_count - 2];
            _array[_count - 3] = _array[_count - 1];
            _array[_count - 2] = x1;
            _array[_count - 1] = x2;
        }

        public void Swap()
        {
            // (x1 x2 -- x2 x1)
            var x1 = _array[_count - 2];
            _array[_count - 2] = _array[_count - 1];
            _array[_count - 1] = x1;
        }

        public void Swap2()
        {
            // (x1 x2 x3 x4 -- x3 x4 x1 x2)
            var x1 = _array[_count - 4];
            var x2 = _array[_count - 3];
            _array[_count - 4] = _array[_count - 2];
            _array[_count - 3] = _array[_count - 1];
            _array[_count - 2] = x1;
            _array[_count - 1] = x2;
        }

        public void Nip()
        {
            // (x1 x2 -- x2)
            _array[_count - 2] = _array[_count - 1];
            _count--;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _array[_count] = default;
        }

        public void Tuck()
        {
            // (x1 x2 -- x2 x1 x2)
            var x2 = _array[_count - 1];
            _array[_count - 1] = _array[_count - 2];
            _array[_count - 2] = x2;
            Push(x2);
        }

        public void Roll(int n)
        {
            // (xn ... x2 x1 x0 - xn-1 ... x2 x1 x0 xn)
            var xni = _count - 1 - n;
            var xn = _array[xni];
            Array.Copy(_array, xni + 1, _array, xni, n);
            _array[_count - 1] = xn;
        }

        public void Pick(int n)
        {
            // (xn ... x2 x1 x0 - xn ... x2 x1 x0 xn)
            Push(_array[_count - 1 - n]);
        }
    }
}
