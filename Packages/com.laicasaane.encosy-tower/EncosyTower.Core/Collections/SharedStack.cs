using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;
using Unity.Collections;

namespace EncosyTower.Collections
{
    public partial class SharedStack<T> : SharedStack<T, T>
        where T : unmanaged
    {
        public SharedStack()
            : base()
        {
        }

        public SharedStack(int capacity)
            : base(capacity)
        {
        }

        public SharedStack(ReadOnlySpan<T> source)
            : base(source)
        {
        }

        public SharedStack([NotNull] ICollection<T> source)
            : base(source)
        {
        }
    }

    public partial class SharedStack<T, TNative>
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
        internal SharedReference<int> _count;
        internal SharedReference<int> _version;
        internal unsafe SharedStackUnsafe<TNative>* _nativeData;

        public SharedStack()
        {
            _buffer = new(0);
            _count = new(0);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedStack(int capacity)
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(capacity >= 0);
            _buffer = new(capacity);
            _count = new(0);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedStack(ReadOnlySpan<T> source)
            : this(source.Length)
        {
            source.CopyTo(_buffer.AsSpan());
            _count.ValueRW = source.Length;
            RefreshNativeData();
        }

        public SharedStack([NotNull] ICollection<T> source)
            : this(source.Count)
        {
            source.CopyTo(_buffer.AsManagedArray(), 0);
            _count.ValueRW = source.Count;
            RefreshNativeData();
        }

        ~SharedStack()
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

        public void Push(T item)
        {
            _version.ValueRW++;
            if (_count.ValueRO == Capacity)
            {
                AllocateMore(checked(_count.ValueRO + 1));
            }

            var index = _count.ValueRO;
            _buffer.AsSpan()[index] = item;
            _count.ValueRW = index + 1;
        }

        public void Push(in T item)
        {
            _version.ValueRW++;
            if (_count.ValueRO == Capacity)
            {
                AllocateMore(checked(_count.ValueRO + 1));
            }

            var index = _count.ValueRO;
            _buffer.AsSpan()[index] = item;
            _count.ValueRW = index + 1;
        }

        public void PushRange(ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }

            _version.ValueRW++;
            var old = Count;
            var required = checked(old + items.Length);

            if (required > Capacity)
            {
                AllocateMore(required);
            }

            items.CopyTo(_buffer.AsSpan()[old..]);
            _count.ValueRW = required;
        }

        public T Pop()
        {
            ThrowHelper.ThrowIfEmpty(Count > 0, ThrowHelper.CollectionType.SharedStack);
            _version.ValueRW++;
            return _buffer.AsReadOnlySpan()[--_count.ValueRW];
        }

        public bool TryPop(out T value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }
            value = Pop();
            return true;
        }

        public T Peek()
        {
            ThrowHelper.ThrowIfEmpty(Count > 0, ThrowHelper.CollectionType.SharedStack);
            return _buffer.AsReadOnlySpan()[Count - 1];
        }

        public bool TryPeek(out T value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }
            value = Peek();
            return true;
        }

        public void Clear()
        {
            _count.ValueRW = 0;
            _version.ValueRW++;
        }

        public T[] ToArray()
        {
            var result = new T[Count];
            CopyTopFirstTo(0, result, result.Length);
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
            CopyTopFirstTo(sourceStartIndex, destination, length);
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

            CopyTopFirstTo(sourceStartIndex, destination, length);
            return true;
        }

        private void CopyTopFirstTo(int sourceStartIndex, Span<T> destination, int length)
        {
            var span = _buffer.AsReadOnlySpan();
            var top = Count - 1 - sourceStartIndex;
            for (var i = 0; i < length; i++)
            {
                destination[i] = span[top - i];
            }
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
        public SharedStackNative<TNative> AsNative()
        {
            // SAFETY: The native view borrows the live header and shared buffer safety handle.
            unsafe
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                return new SharedStackNative<TNative>(_nativeData, _buffer.GetSafetyHandle());
#else
                return new SharedStackNative<TNative>(_nativeData);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SharedStackNative<TNative>(SharedStack<T, TNative> stack)
            => stack.AsNative();

        public void Dispose()
        {
            if (_buffer == null)
            {
                return;
            }
            // SAFETY: The header is freed before the pinned owner buffers are disposed.
            unsafe
            {
                SharedStackUnsafe<TNative>.Free(_nativeData, Allocator.Persistent);
                _nativeData = null;
            }
            _buffer.Dispose();
            _count.Dispose();
            _version.Dispose();
            _buffer = null;
            _count = null;
            _version = null;
        }

        private void InitializeNativeData()
        {
            // SAFETY: SharedArray and SharedReference keep all supplied pointers valid while the owner lives.
            unsafe
            {
                _nativeData = SharedStackUnsafe<TNative>.Alloc(
                      _buffer.GetUnsafeBufferPointer()
                    , _buffer.Length
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
                _nativeData->_count = _count.GetUnsafeBufferPointer();
                _nativeData->_version = _version.GetUnsafeBufferPointer();
            }
        }

        private void AllocateMore(int newSize)
        {
            newSize = Math.Max(4, newSize);
            var capacity = checked(((int)Math.Ceiling(newSize * 1.5f) / 4) * 4);
            ResizeBuffer(capacity);
        }

        private void ResizeBuffer(int capacity)
        {
            _buffer.Resize(capacity);
            RefreshNativeData();
        }
    }
}
