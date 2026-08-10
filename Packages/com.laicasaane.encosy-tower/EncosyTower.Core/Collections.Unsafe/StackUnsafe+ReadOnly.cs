using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Buffers;
using EncosyTower.Common;

namespace EncosyTower.Collections.Unsafe
{
    public partial struct StackUnsafe<T>
        where T : unmanaged
    {
        public readonly struct ReadOnly
            : IReadOnlyCollection<T>
            , IHasCapacity
            , IHasCount
            , IIsCreated
            , IToArray<T>
            , ICopyToSpan<T>
            , ITryCopyToSpan<T>
        {
            internal readonly BufferUnsafe<T>.ReadOnly _buffer;
            internal readonly int _count;
            internal readonly int _version;

            internal ReadOnly(in StackUnsafe<T> source)
            {
                _buffer = source._buffer.AsReadOnly();
                _count = source._count;
                _version = source._version;
            }

            public readonly bool IsCreated
                => _buffer.IsCreated;
            public readonly int Count
                => _count;
            public readonly int Capacity
                => _buffer.Capacity;

            public readonly T Peek()
            {
                ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.StackUnsafe);
                return _buffer[_count - 1];
            }

            public readonly bool TryPeek(out T value)
            {
                if (_count == 0)
                {
                    value = default;
                    return false;
                }
                value = _buffer[_count - 1];
                return true;
            }

            public readonly T[] ToArray()
            {
                var result = new T[_count];
                CopyTopFirstTo(0, result, _count);
                return result;
            }

            public readonly void CopyTo(Span<T> destination)
                => CopyTo(0, destination);

            public readonly void CopyTo(Span<T> destination, int length)
                => CopyTo(0, destination, length);

            public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
                => CopyTo(sourceStartIndex, destination, destination.Length);

            public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
            {
                ThrowHelper.ThrowIfSourceStartIndexIsInvalid((uint)sourceStartIndex <= (uint)_count);
                ThrowHelper.ThrowIfSourceLengthIsInvalid((uint)length <= (uint)(_count - sourceStartIndex));
                ThrowHelper.ThrowIfDestinationLengthIsInvalid((uint)length <= (uint)destination.Length);
                CopyTopFirstTo(sourceStartIndex, destination, length);
            }

            public readonly bool TryCopyTo(Span<T> destination)
                => TryCopyTo(0, destination);

            public readonly bool TryCopyTo(Span<T> destination, int length)
                => TryCopyTo(0, destination, length);

            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => TryCopyTo(sourceStartIndex, destination, destination.Length);

            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
            {
                if (
                    (uint)sourceStartIndex > (uint)_count
                    || (uint)length > (uint)(_count - sourceStartIndex)
                    || (uint)length > (uint)destination.Length
                )
                {
                    return false;
                }

                CopyTopFirstTo(sourceStartIndex, destination, length);
                return true;
            }

            private readonly void CopyTopFirstTo(
                  int sourceStartIndex
                , Span<T> destination
                , int length
            )
            {
                var top = _count - 1 - sourceStartIndex;
                for (var i = 0; i < length; i++)
                {
                    destination[i] = _buffer[top - i];
                }
            }

            public readonly Enumerator GetEnumerator()
                => new(this);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public static implicit operator ReadOnly(in StackUnsafe<T> source)
                => new(source);
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _stack;
            private int _index;
            private T _current;

            internal Enumerator(in ReadOnly stack)
            {
                _stack = stack;
                _index = stack.Count - 1;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_index >= 0)
                {
                    _current = _stack._buffer[_index--];
                    return true;
                }
                _current = default;
                return false;
            }

            public readonly T Current
                => _current;

            readonly object IEnumerator.Current
                => Current;

            public void Reset()
            {
                _index = _stack.Count - 1;
                _current = default;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
