using System;
using EncosyTower.Buffers;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Unsafe
{
    internal partial struct SharedQueueUnsafe<T>
        where T : unmanaged
    {
        internal unsafe T* _buffer;
        internal int _capacity;
        internal unsafe int* _head;
        internal unsafe int* _tail;
        internal unsafe int* _count;
        internal unsafe int* _version;

        internal static unsafe SharedQueueUnsafe<T>* Alloc(
              T* buffer
            , int capacity
            , int* head
            , int* tail
            , int* count
            , int* version
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The allocator returns storage for a header whose pointers are all supplied by the
            // owning shared object.
            unsafe
            {
                var data = allocator.Allocate<SharedQueueUnsafe<T>>();
                *data = new SharedQueueUnsafe<T>
                {
                    _buffer = buffer,
                    _capacity = capacity,
                    _head = head,
                    _tail = tail,
                    _count = count,
                    _version = version,
                };
                return data;
            }
        }

        internal static unsafe void Free(SharedQueueUnsafe<T>* data, AllocatorStrategy allocator)
        {
            if (data == null)
            {
                return;
            }
            // SAFETY: data is the single header allocated by Alloc and is freed after the owner stops exposing it.
            unsafe
            {
                allocator.Free(data);
            }
        }

        internal int Capacity
        {
            get
            {
                // SAFETY: The owning collection keeps the native header live while this property reads its state.
                unsafe
                {
                    return _capacity;
                }
            }
        }

        internal int Count
        {
            get
            {
                // SAFETY: The owning collection keeps the native header live while this property reads its state.
                unsafe
                {
                    return *_count;
                }
            }
        }

        internal void Enqueue(T item)
        {
            // SAFETY: The owning shared queue pins the buffer and validates the fixed-capacity contract
            // before forwarding.
            unsafe
            {
                var count = *_count;
                ThrowHelper.ThrowIfCapacityIsImmutable(
                    count < _capacity,
                    ThrowHelper.CollectionType.SharedQueueUnsafe
                );
                UnsafeUtility.ArrayElementAsRef<T>(_buffer, *_tail) = item;
                MoveNext(_tail);
                *_count = count + 1;
                (*_version)++;
            }
        }

        internal void Enqueue(in T item)
        {
            // SAFETY: The owning shared queue pins the buffer and validates the fixed-capacity contract
            // before forwarding.
            unsafe
            {
                var count = *_count;
                ThrowHelper.ThrowIfCapacityIsImmutable(
                    count < _capacity,
                    ThrowHelper.CollectionType.SharedQueueUnsafe
                );
                UnsafeUtility.ArrayElementAsRef<T>(_buffer, *_tail) = item;
                MoveNext(_tail);
                *_count = count + 1;
                (*_version)++;
            }
        }

        internal T Dequeue()
        {
            // SAFETY: The shared owner pins the backing storage and the count check bounds the head index.
            unsafe
            {
                ThrowHelper.ThrowIfEmpty(*_count > 0, ThrowHelper.CollectionType.SharedQueueUnsafe);
                var result = UnsafeUtility.ArrayElementAsRef<T>(_buffer, *_head);
                MoveNext(_head);
                (*_count)--;
                (*_version)++;
                return result;
            }
        }

        internal bool TryDequeue(out T result)
        {
            if (Count == 0)
            {
                result = default;
                return false;
            }
            result = Dequeue();
            return true;
        }

        internal T Peek()
        {
            // SAFETY: The shared owner pins the backing storage and the count check bounds the head index.
            unsafe
            {
                ThrowHelper.ThrowIfEmpty(*_count > 0, ThrowHelper.CollectionType.SharedQueueUnsafe);
                return UnsafeUtility.ArrayElementAsRef<T>(_buffer, *_head);
            }
        }

        internal bool TryPeek(out T result)
        {
            if (Count == 0)
            {
                result = default;
                return false;
            }
            result = Peek();
            return true;
        }

        internal void Clear()
        {
            // SAFETY: The owner supplies live scalar pointers for the lifetime of the header.
            unsafe
            {
                *_head = *_tail = *_count = 0;
                (*_version)++;
            }
        }

        internal T[] ToArray()
        {
            var result = new T[Count];
            CopyLinearTo(result);
            return result;
        }

        internal void CopyTo(Span<T> destination)
            => CopyTo(0, destination);

        internal void CopyTo(Span<T> destination, int length)
            => CopyTo(0, destination, length);

        internal void CopyTo(int sourceStartIndex, Span<T> destination)
            => CopyTo(sourceStartIndex, destination, destination.Length);

        internal void CopyTo(int sourceStartIndex, Span<T> destination, int length)
        {
            var count = Count;
            ThrowHelper.ThrowIfSourceStartIndexIsInvalid((uint)sourceStartIndex <= (uint)count);
            ThrowHelper.ThrowIfSourceLengthIsInvalid((uint)length <= (uint)(count - sourceStartIndex));
            ThrowHelper.ThrowIfDestinationLengthIsInvalid((uint)length <= (uint)destination.Length);
            CopyRingTo(sourceStartIndex, destination, length);
        }

        internal bool TryCopyTo(Span<T> destination)
            => TryCopyTo(0, destination);

        internal bool TryCopyTo(Span<T> destination, int length)
            => TryCopyTo(0, destination, length);

        internal bool TryCopyTo(int sourceStartIndex, Span<T> destination)
            => TryCopyTo(sourceStartIndex, destination, destination.Length);

        internal bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
        {
            var count = Count;
            if (
                (uint)sourceStartIndex > (uint)count
                || (uint)length > (uint)(count - sourceStartIndex)
                || (uint)length > (uint)destination.Length
            )
            {
                return false;
            }

            CopyRingTo(sourceStartIndex, destination, length);
            return true;
        }

        private void CopyLinearTo(Span<T> destination)
            => CopyRingTo(0, destination, Count);

        private void CopyRingTo(int sourceStartIndex, Span<T> destination, int length)
        {
            if (length == 0)
            {
                return;
            }
            // SAFETY: The caller-validated range and the ring pointers describe a live, bounded sequence
            // in the pinned buffer.
            unsafe
            {
                var start = *_head + sourceStartIndex;
                if (start >= _capacity)
                {
                    start -= _capacity;
                }

                var first = Math.Min(length, _capacity - start);
                new ReadOnlySpan<T>(_buffer + start, first).CopyTo(destination);
                if (length > first)
                {
                    new ReadOnlySpan<T>(_buffer, length - first).CopyTo(destination[first..]);
                }
            }
        }

        private unsafe void MoveNext(int* index)
        {
            // SAFETY: index points to one of the live scalar fields owned by this header.
            unsafe
            {
                if (++(*index) == _capacity)
                {
                    *index = 0;
                }
            }
        }
    }
}
