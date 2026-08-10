using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;
using Unity.Collections;

namespace EncosyTower.Collections
{
    public partial class SharedQueue<T> : SharedQueue<T, T>
        where T : unmanaged
    {
        public SharedQueue()
            : base()
        {
        }

        public SharedQueue(int capacity)
            : base(capacity)
        {
        }

        public SharedQueue(ReadOnlySpan<T> source)
            : base(source)
        {
        }

        public SharedQueue([NotNull] ICollection<T> source)
            : base(source)
        {
        }
    }

    public partial class SharedQueue<T, TNative>
        : IReadOnlyCollection<T>
        , IHasCapacity, IIncreaseCapacity
        , IHasCount
        , IClearable
        , IDisposable
        , ICopyToSpan<T>
        , ITryCopyToSpan<T>
        where T : unmanaged
        where TNative : unmanaged
    {
        internal SharedArray<T, TNative> _buffer;
        internal SharedReference<int> _head;
        internal SharedReference<int> _tail;
        internal SharedReference<int> _count;
        internal SharedReference<int> _version;
        internal unsafe SharedQueueUnsafe<TNative>* _nativeData;

        public SharedQueue()
        {
            _buffer = new(0);
            _head = new(0);
            _tail = new(0);
            _count = new(0);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedQueue(int capacity)
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(capacity >= 0);
            _buffer = new(capacity);
            _head = new(0);
            _tail = new(0);
            _count = new(0);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedQueue(ReadOnlySpan<T> source)
            : this(source.Length)
        {
            source.CopyTo(_buffer.AsSpan());
            _count.ValueRW = source.Length;
            _tail.ValueRW = _count.ValueRO == Capacity ? 0 : _count.ValueRO;
            RefreshNativeData();
        }

        public SharedQueue([NotNull] ICollection<T> source)
            : this(source.Count)
        {
            source.CopyTo(_buffer.AsManagedArray(), 0);
            _count.ValueRW = source.Count;
            _tail.ValueRW = _count.ValueRO == Capacity ? 0 : _count.ValueRO;
            RefreshNativeData();
        }

        ~SharedQueue()
            => Dispose();

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer != null;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count?.ValueRO ?? 0;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer?.Length ?? 0;
        }

        public void Enqueue(T item)
        {
            _version.ValueRW++;
            if (_count.ValueRO == Capacity)
            {
                AllocateMore(checked(_count.ValueRO + 1));
            }

            _buffer.AsSpan()[_tail.ValueRO] = item;
            MoveNext(_tail, Capacity);
            _count.ValueRW++;
        }

        public void Enqueue(in T item)
        {
            _version.ValueRW++;
            if (_count.ValueRO == Capacity)
            {
                AllocateMore(checked(_count.ValueRO + 1));
            }

            _buffer.AsSpan()[_tail.ValueRO] = item;
            MoveNext(_tail, Capacity);
            _count.ValueRW++;
        }

        public void EnqueueRange(ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }

            _version.ValueRW++;
            var required = checked(_count.ValueRO + items.Length);
            if (required > Capacity)
            {
                AllocateMore(required);
            }

            var first = Math.Min(items.Length, Capacity - _tail.ValueRO);
            items[..first].CopyTo(_buffer.AsSpan().Slice(_tail.ValueRO, first));
            if (items.Length > first)
            {
                items[first..].CopyTo(_buffer.AsSpan()[..(items.Length - first)]);
            }

            _tail.ValueRW += items.Length;
            if (_tail.ValueRO >= Capacity)
            {
                _tail.ValueRW -= Capacity;
            }

            _count.ValueRW += items.Length;
        }

        public T Dequeue()
        {
            ThrowHelper.ThrowIfEmpty(_count.ValueRO > 0, ThrowHelper.CollectionType.SharedQueue);
            var result = _buffer.AsReadOnlySpan()[_head.ValueRO];
            MoveNext(_head, Capacity);
            _count.ValueRW--;
            _version.ValueRW++;
            return result;
        }

        public bool TryDequeue(out T value)
        {
            if (_count.ValueRO == 0)
            {
                value = default;
                return false;
            }
            value = Dequeue();
            return true;
        }

        public T Peek()
        {
            ThrowHelper.ThrowIfEmpty(_count.ValueRO > 0, ThrowHelper.CollectionType.SharedQueue);
            return _buffer.AsReadOnlySpan()[_head.ValueRO];
        }

        public bool TryPeek(out T value)
        {
            if (_count.ValueRO == 0)
            {
                value = default;
                return false;
            }
            value = Peek();
            return true;
        }

        public void Clear()
        {
            _head.ValueRW = 0;
            _tail.ValueRW = 0;
            _count.ValueRW = 0;
            _version.ValueRW++;
        }

        public T[] ToArray()
        {
            var result = new T[Count];
            CopyLinearTo(result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> destination)
            => CopyTo(0, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> destination, int length)
            => CopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int sourceStartIndex, Span<T> destination)
            => CopyTo(sourceStartIndex, destination, destination.Length);

        public void CopyTo(int sourceStartIndex, Span<T> destination, int length)
        {
            var count = Count;
            ThrowHelper.ThrowIfSourceStartIndexIsInvalid((uint)sourceStartIndex <= (uint)count);
            ThrowHelper.ThrowIfSourceLengthIsInvalid((uint)length <= (uint)(count - sourceStartIndex));
            ThrowHelper.ThrowIfDestinationLengthIsInvalid((uint)length <= (uint)destination.Length);
            CopyRingTo(sourceStartIndex, destination, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<T> destination)
            => TryCopyTo(0, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<T> destination, int length)
            => TryCopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(int sourceStartIndex, Span<T> destination)
            => TryCopyTo(sourceStartIndex, destination, destination.Length);

        public bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
            => IncreaseCapacityTo(checked(Capacity + amount));

        public int IncreaseCapacityTo(int capacity)
        {
            ThrowHelper.ThrowIfCapacityBelowCount(capacity >= Count);

            if (capacity > Capacity)
            {
                ResizeBuffer(capacity);
            }

            return Capacity;
        }

        public void Trim()
        {
            if (Capacity > Count)
            {
                ResizeBuffer(Count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnly AsReadOnly()
            => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(AsReadOnly());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedQueueNative<TNative> AsNative()
        {
            // SAFETY: The native view borrows the live header and the shared buffer's safety handle.
            unsafe
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                return new SharedQueueNative<TNative>(_nativeData, _buffer.GetSafetyHandle());
#else
                return new SharedQueueNative<TNative>(_nativeData);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SharedQueueNative<TNative>(SharedQueue<T, TNative> queue)
            => queue.AsNative();

        public void Dispose()
        {
            if (_buffer == null)
            {
                return;
            }
            // SAFETY: The header is freed before the pinned owner buffers are disposed.
            unsafe
            {
                SharedQueueUnsafe<TNative>.Free(_nativeData, Allocator.Persistent);
                _nativeData = null;
            }
            _buffer.Dispose();
            _head.Dispose();
            _tail.Dispose();
            _count.Dispose();
            _version.Dispose();
            _buffer = null;
            _head = null;
            _tail = null;
            _count = null;
            _version = null;
        }

        private void InitializeNativeData()
        {
            // SAFETY: SharedArray and SharedReference keep all supplied pointers valid while the owner lives.
            unsafe
            {
                _nativeData = SharedQueueUnsafe<TNative>.Alloc(
                      _buffer.GetUnsafeBufferPointer()
                    , _buffer.Length
                    , _head.GetUnsafeBufferPointer()
                    , _tail.GetUnsafeBufferPointer()
                    , _count.GetUnsafeBufferPointer()
                    , _version.GetUnsafeBufferPointer()
                    , Allocator.Persistent
                );
            }
        }

        private void RefreshNativeData()
        {
            // SAFETY: The header remains live and all pointers are refreshed from current pinned storage.
            unsafe
            {
                _nativeData->_buffer = _buffer.GetUnsafeBufferPointer();
                _nativeData->_capacity = _buffer.Length;
                _nativeData->_head = _head.GetUnsafeBufferPointer();
                _nativeData->_tail = _tail.GetUnsafeBufferPointer();
                _nativeData->_count = _count.GetUnsafeBufferPointer();
                _nativeData->_version = _version.GetUnsafeBufferPointer();
            }
        }

        private void AllocateMore(int newSize)
        {
            newSize = Math.Max(4, newSize);
            var capacity = checked(((int)Math.Ceiling(newSize * 1.5f) / 4) * 4);

            Linearize();
            ResizeBuffer(capacity);
        }

        private void ResizeBuffer(int newCapacity)
        {
            Linearize();
            _buffer.Resize(newCapacity);
            RefreshNativeData();
            _tail.ValueRW = Count == Capacity ? 0 : Count;
        }

        private void Linearize()
        {
            if (Count == 0 || _head.ValueRO == 0)
            {
                return;
            }

            var values = ToArray();
            values.AsSpan().CopyTo(_buffer.AsSpan());
            _head.ValueRW = 0;
            _tail.ValueRW = Count == Capacity ? 0 : Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyLinearTo(Span<T> destination)
            => CopyRingTo(0, destination, Count);

        private void CopyRingTo(int sourceStartIndex, Span<T> destination, int length)
        {
            if (length == 0)
            {
                return;
            }

            var capacity = Capacity;
            var start = _head.ValueRO + sourceStartIndex;
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

        private static void MoveNext(SharedReference<int> index, int capacity)
        {
            var value = index.ValueRO + 1;
            if (value == capacity)
            {
                value = 0;
            }

            index.ValueRW = value;
        }
    }
}
