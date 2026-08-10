using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Common;
using Unity.Jobs;

namespace EncosyTower.Collections.Unsafe
{
    public partial struct QueueUnsafe<T>
        : IDisposable
        , IEnumerable<T>
        , IEnumerable
        , IReadOnlyCollection<T>
        , IHasCapacity
        , IHasCount
        , IIncreaseCapacity
        , IClearable
        , IIsCreated
        , IToArray<T>
        , ICopyToSpan<T>
        , ITryCopyToSpan<T>
#if UNITY_COLLECTIONS
        , Unity.Collections.INativeDisposable
#endif
        where T : unmanaged
    {
        internal BufferUnsafe<T> _buffer;
        internal int _head;
        internal int _tail;
        internal int _count;
        internal int _version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueueUnsafe(int capacity, AllocatorStrategy allocator)
            : this()
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(capacity >= 0);
            ThrowHelper.ThrowIfAllocatorIsInvalid(allocator.IsValid);
            _buffer = new BufferUnsafe<T>(capacity, allocator, clear: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueueUnsafe(ReadOnlySpan<T> source, AllocatorStrategy allocator)
            : this(source.Length, allocator)
        {
            source.CopyTo(_buffer.AsSpan());
            _count = source.Length;
            _tail = _count == Capacity ? 0 : _count;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.IsCreated;
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Capacity;
        }

        public void Enqueue(T item)
        {
            EnsureCapacity(_count + 1);
            _buffer.AsSpan()[_tail] = item;
            MoveNext(ref _tail);
            _count++;
            _version++;
        }

        public void Enqueue(in T item)
        {
            EnsureCapacity(_count + 1);
            _buffer.AsSpan()[_tail] = item;
            MoveNext(ref _tail);
            _count++;
            _version++;
        }

        public void EnqueueRange(ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }

            EnsureCapacity(checked(_count + items.Length));
            CopyIntoRing(items);
            _count += items.Length;
            _version++;
        }

        public T Dequeue()
        {
            ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.QueueUnsafe);
            var result = _buffer.AsReadOnlySpan()[_head];
            MoveNext(ref _head);
            _count--;
            _version++;
            return result;
        }

        public bool TryDequeue(out T value)
        {
            if (_count == 0)
            {
                value = default;
                return false;
            }
            value = Dequeue();
            return true;
        }

        public readonly T Peek()
        {
            ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.QueueUnsafe);
            return _buffer.AsReadOnlySpan()[_head];
        }

        public readonly bool TryPeek(out T value)
        {
            if (_count == 0)
            {
                value = default;
                return false;
            }
            value = _buffer.AsReadOnlySpan()[_head];
            return true;
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _count = 0;
            _version++;
        }

        public readonly T[] ToArray()
        {
            var result = new T[_count];
            CopyRingTo(0, result, _count);
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

        public int EnsureCapacity(int newSize)
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(newSize >= 0);

            if (newSize > Capacity)
            {
                newSize = Math.Max(4, newSize);
                var capacity = checked(((int)Math.Ceiling(newSize * 1.5f) / 4) * 4);
                SetCapacity(capacity);
            }

            return Capacity;
        }

        public int IncreaseCapacityBy(int amount)
            => IncreaseCapacityTo(checked(Capacity + amount));

        public int IncreaseCapacityTo(int newCapacity)
        {
            ThrowHelper.ThrowIfCapacityBelowCount(newCapacity >= _count);

            if (newCapacity != Capacity)
            {
                SetCapacity(newCapacity);
            }

            return Capacity;
        }

        public void Trim()
            => SetCapacity(_count);

        private void SetCapacity(int newCapacity)
        {
            ThrowHelper.ThrowIfCapacityBelowCount(newCapacity >= _count);
            if (newCapacity == Capacity)
            {
                return;
            }

            var newBuffer = new BufferUnsafe<T>(newCapacity, _buffer._allocator, clear: false);

            if (_count > 0)
            {
                var destination = newBuffer.AsSpan();

                if (_head < _tail)
                {
                    _buffer.AsReadOnlySpan().Slice(_head, _count).CopyTo(destination);
                }
                else
                {
                    var first = Capacity - _head;
                    _buffer.AsReadOnlySpan().Slice(_head, first).CopyTo(destination);

                    if (_count > first)
                    {
                        _buffer.AsReadOnlySpan()[..(_count - first)].CopyTo(destination[first..]);
                    }
                }
            }

            _buffer.Dispose();
            _buffer = newBuffer;
            _head = 0;
            _tail = _count == newCapacity ? 0 : _count;
            _version++;
        }

        private void CopyIntoRing(ReadOnlySpan<T> source)
        {
            var first = Math.Min(source.Length, Capacity - _tail);
            source[..first].CopyTo(_buffer.AsSpan().Slice(_tail, first));
            if (source.Length > first)
            {
                source[first..].CopyTo(_buffer.AsSpan()[..(source.Length - first)]);
            }
            _tail += source.Length;
            if (_tail >= Capacity)
            {
                _tail -= Capacity;
            }
        }

        private readonly void CopyRingTo(int sourceStartIndex, Span<T> destination, int length)
        {
            if (length == 0)
            {
                return;
            }

            var capacity = Capacity;
            var start = _head + sourceStartIndex;
            if (start >= capacity)
            {
                start -= capacity;
            }

            var first = Math.Min(length, capacity - start);
            _buffer.AsReadOnlySpan().Slice(start, first).CopyTo(destination);

            if (length > first)
            {
                _buffer.AsReadOnlySpan()[..(length - first)].CopyTo(destination[first..]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void MoveNext(ref int index)
        {
            if (++index == Capacity)
            {
                index = 0;
            }
        }

        public readonly ReadOnly AsReadOnly()
            => new(this);

        public readonly Enumerator GetEnumerator()
            => new(AsReadOnly());

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Dispose()
        {
            _buffer.Dispose();
            _head = _tail = _count = _version = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var result = _buffer.Dispose(inputDeps);
            _head = _tail = _count = _version = 0;
            return result;
        }

        /// <safety>The returned header owns the allocation and must be freed exactly once with Free.</safety>
        internal static unsafe QueueUnsafe<T>* Alloc(int capacity, AllocatorStrategy allocator)
        {
            // SAFETY: The allocator returns storage for one QueueUnsafe header and its owned buffer.
            unsafe
            {
                var data = allocator.Allocate<QueueUnsafe<T>>();
                *data = new QueueUnsafe<T>(capacity, allocator);
                return data;
            }
        }

        /// <safety>The returned header owns a copy of source and must be freed exactly once with Free.</safety>
        internal static unsafe QueueUnsafe<T>* Alloc(
              ReadOnlySpan<T> source
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The allocator returns storage for one QueueUnsafe header and its owned buffer.
            unsafe
            {
                var data = allocator.Allocate<QueueUnsafe<T>>();
                *data = new QueueUnsafe<T>(source, allocator);
                return data;
            }
        }

        /// <safety>data must be a live header allocated by Alloc and must not be used after this call.</safety>
        internal static unsafe void Free(QueueUnsafe<T>* data)
        {
            if (data == null)
            {
                return;
            }
            // SAFETY: data is the live owner header; its allocator is copied before Dispose resets it.
            unsafe
            {
                var allocator = data->_buffer._allocator;
                data->Dispose();
                allocator.Free(data);
            }
        }
    }
}
