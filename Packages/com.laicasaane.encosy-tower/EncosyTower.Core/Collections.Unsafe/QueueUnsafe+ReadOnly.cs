using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Buffers;
using EncosyTower.Common;

namespace EncosyTower.Collections.Unsafe
{
    public partial struct QueueUnsafe<T>
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
            internal readonly int _head;
            internal readonly int _tail;
            internal readonly int _count;
            internal readonly int _version;

            internal ReadOnly(in QueueUnsafe<T> source)
            {
                _buffer = source._buffer.AsReadOnly();
                _head = source._head;
                _tail = source._tail;
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
                ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.QueueUnsafe);
                return _buffer[_head];
            }

            public readonly bool TryPeek(out T value)
            {
                if (_count == 0)
                {
                    value = default;
                    return false;
                }
                value = _buffer[_head];
                return true;
            }

            public readonly T[] ToArray()
            {
                var result = new T[_count];
                CopyLinearTo(result);
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
                CopyRingTo(sourceStartIndex, destination, length);
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

                CopyRingTo(sourceStartIndex, destination, length);
                return true;
            }

            private readonly void CopyLinearTo(Span<T> destination)
                => CopyRingTo(0, destination, _count);

            private readonly void CopyRingTo(int sourceStartIndex, Span<T> destination, int length)
            {
                if (length == 0)
                {
                    return;
                }

                var capacity = _buffer.Capacity;
                var start = _head + sourceStartIndex;
                if (start >= capacity)
                {
                    start -= capacity;
                }

                var first = Math.Min(length, capacity - start);
                _buffer.AsReadOnlySpan().Slice(start, first).CopyTo(destination);
                if (length > first)
                {
                    _buffer.AsReadOnlySpan().Slice(0, length - first).CopyTo(destination[first..]);
                }
            }

            public readonly Enumerator GetEnumerator()
                => new(this);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public static implicit operator ReadOnly(in QueueUnsafe<T> source)
                => new(source);
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _queue;
            private int _index;
            private T _current;

            internal Enumerator(in ReadOnly queue)
            {
                _queue = queue;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if ((uint)_index < (uint)_queue.Count)
                {
                    var index = _queue._head + _index++;
                    if (index >= _queue.Capacity)
                    {
                        index -= _queue.Capacity;
                    }

                    _current = _queue._buffer[index];
                    return true;
                }
                _index = _queue.Count + 1;
                _current = default;
                return false;
            }

            public readonly T Current
                => _current;

            readonly object IEnumerator.Current
                => Current;

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
