using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Common;
using Unity.Jobs;

namespace EncosyTower.Collections.Unsafe
{
    public partial struct StackUnsafe<T>
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
        internal int _count;
        internal int _version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackUnsafe(int capacity, AllocatorStrategy allocator)
            : this()
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(capacity >= 0);
            ThrowHelper.ThrowIfAllocatorIsInvalid(allocator.IsValid);
            _buffer = new BufferUnsafe<T>(capacity, allocator, clear: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackUnsafe(ReadOnlySpan<T> source, AllocatorStrategy allocator)
            : this(source.Length, allocator)
        {
            source.CopyTo(_buffer.AsSpan());
            _count = source.Length;
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

        public void Push(T item)
        {
            EnsureCapacity(_count + 1);
            _buffer.AsSpan()[_count++] = item;
            _version++;
        }

        public void Push(in T item)
        {
            EnsureCapacity(_count + 1);
            _buffer.AsSpan()[_count++] = item;
            _version++;
        }

        public void PushRange(ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }

            EnsureCapacity(checked(_count + items.Length));
            items.CopyTo(_buffer.AsSpan()[_count..]);
            _count += items.Length;
            _version++;
        }

        public T Pop()
        {
            ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.StackUnsafe);
            _version++;
            return _buffer.AsReadOnlySpan()[--_count];
        }

        public bool TryPop(out T value)
        {
            if (_count == 0)
            {
                value = default;
                return false;
            }
            value = Pop();
            return true;
        }

        public readonly T Peek()
        {
            ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.StackUnsafe);
            return _buffer.AsReadOnlySpan()[_count - 1];
        }

        public readonly bool TryPeek(out T value)
        {
            if (_count == 0)
            {
                value = default;
                return false;
            }
            value = _buffer.AsReadOnlySpan()[_count - 1];
            return true;
        }

        public void Clear()
        {
            _count = 0;
            _version++;
        }

        public int EnsureCapacity(int newSize)
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(newSize >= 0);

            if (newSize <= Capacity)
            {
                return Capacity;
            }

            _version++;

            newSize = Math.Max(4, newSize);
            var capacity = checked(((int)Math.Ceiling(newSize * 1.5f) / 4) * 4);

            if (_buffer.IsCreated)
            {
                _buffer.Resize(capacity, copyContent: true, memClear: false);
            }
            else
            {
                _buffer.Alloc(capacity, _buffer._allocator, memClear: false);
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
                if (_buffer.IsCreated)
                {
                    _buffer.Resize(newCapacity, copyContent: true, memClear: false);
                }
                else
                {
                    _buffer.Alloc(newCapacity, _buffer._allocator, memClear: false);
                }

                _version++;
            }

            return Capacity;
        }

        public void Trim()
        {
            if (Capacity != _count)
            {
                if (_buffer.IsCreated)
                {
                    _buffer.Resize(_count, copyContent: true, memClear: false);
                }
                _version++;
            }
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

        private readonly void CopyTopFirstTo(int sourceStartIndex, Span<T> destination, int length)
        {
            var span = _buffer.AsReadOnlySpan();
            var top = _count - 1 - sourceStartIndex;
            for (var i = 0; i < length; i++)
            {
                destination[i] = span[top - i];
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
            _count = _version = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var result = _buffer.Dispose(inputDeps);
            _count = _version = 0;
            return result;
        }

        /// <safety>The returned header owns the allocation and must be freed exactly once with Free.</safety>
        internal static unsafe StackUnsafe<T>* Alloc(int capacity, AllocatorStrategy allocator)
        {
            // SAFETY: The allocator returns storage for one StackUnsafe header and its owned buffer.
            unsafe
            {
                var data = allocator.Allocate<StackUnsafe<T>>();
                *data = new StackUnsafe<T>(capacity, allocator);
                return data;
            }
        }

        /// <safety>The returned header owns a copy of source and must be freed exactly once with Free.</safety>
        internal static unsafe StackUnsafe<T>* Alloc(
              ReadOnlySpan<T> source
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The allocator returns storage for one StackUnsafe header and its owned buffer.
            unsafe
            {
                var data = allocator.Allocate<StackUnsafe<T>>();
                *data = new StackUnsafe<T>(source, allocator);
                return data;
            }
        }

        /// <safety>data must be a live header allocated by Alloc and must not be used after this call.</safety>
        internal static unsafe void Free(StackUnsafe<T>* data)
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
