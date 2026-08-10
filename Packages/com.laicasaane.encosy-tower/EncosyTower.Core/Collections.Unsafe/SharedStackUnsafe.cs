using System;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Unsafe
{
    internal partial struct SharedStackUnsafe<T>
        where T : unmanaged
    {
        internal unsafe T* _buffer;
        internal int _capacity;
        internal unsafe int* _count;
        internal unsafe int* _version;

        internal static unsafe SharedStackUnsafe<T>* Alloc(
              T* buffer
            , int capacity
            , int* count
            , int* version
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The allocator returns storage for a header whose pointers are supplied by the owning
            // shared stack.
            unsafe
            {
                var data = allocator.Allocate<SharedStackUnsafe<T>>();
                *data = new SharedStackUnsafe<T>
                {
                    _buffer = buffer,
                    _capacity = capacity,
                    _count = count,
                    _version = version,
                };
                return data;
            }
        }

        internal static unsafe void Free(SharedStackUnsafe<T>* data, AllocatorStrategy allocator)
        {
            if (data == null)
            {
                return;
            }
            // SAFETY: data is the single header allocated by Alloc and is freed exactly once by its owner.
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

        internal void Push(T item)
        {
            // SAFETY: The owner pins the buffer and the fixed-capacity check bounds the write.
            unsafe
            {
                var count = *_count;
                ThrowHelper.ThrowIfCapacityIsImmutable(
                    count < _capacity,
                    ThrowHelper.CollectionType.SharedStackUnsafe
                );
                _buffer[count] = item;
                *_count = count + 1;
                (*_version)++;
            }
        }

        internal void Push(in T item)
        {
            // SAFETY: The owner pins the buffer and the fixed-capacity check bounds the write.
            unsafe
            {
                var count = *_count;
                ThrowHelper.ThrowIfCapacityIsImmutable(
                    count < _capacity,
                    ThrowHelper.CollectionType.SharedStackUnsafe
                );
                _buffer[count] = item;
                *_count = count + 1;
                (*_version)++;
            }
        }

        internal T Pop()
        {
            // SAFETY: The count check bounds the returned top element in the pinned buffer.
            unsafe
            {
                ThrowHelper.ThrowIfEmpty(*_count > 0, ThrowHelper.CollectionType.SharedStackUnsafe);
                var result = _buffer[--(*_count)];
                (*_version)++;
                return result;
            }
        }

        internal bool TryPop(out T result)
        {
            if (Count == 0)
            {
                result = default;
                return false;
            }
            result = Pop();
            return true;
        }

        internal T Peek()
        {
            // SAFETY: The count check bounds the top element in the pinned buffer.
            unsafe
            {
                ThrowHelper.ThrowIfEmpty(*_count > 0, ThrowHelper.CollectionType.SharedStackUnsafe);
                return _buffer[*_count - 1];
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
                *_count = 0;
                (*_version)++;
            }
        }

        internal T[] ToArray()
        {
            var result = new T[Count];
            CopyTopFirstTo(0, result, result.Length);
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
            CopyTopFirstTo(sourceStartIndex, destination, length);
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

            CopyTopFirstTo(sourceStartIndex, destination, length);
            return true;
        }

        private void CopyTopFirstTo(int sourceStartIndex, Span<T> destination, int length)
        {
            // SAFETY: The caller-validated range bounds each top-first read in the pinned buffer.
            unsafe
            {
                var top = *_count - 1 - sourceStartIndex;
                for (var i = 0; i < length; i++)
                {
                    destination[i] = _buffer[top - i];
                }
            }
        }
    }
}
