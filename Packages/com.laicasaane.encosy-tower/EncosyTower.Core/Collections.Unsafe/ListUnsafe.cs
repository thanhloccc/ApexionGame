using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace EncosyTower.Collections.Unsafe
{
    public partial struct ListUnsafe<T>
        : IDisposable
        , IEnumerable<T>
        , IEnumerable
        , IReadOnlyList<T>
        , IIndexer<T>
        , IAsSpan<T>
        , IAsReadOnlySpan<T>
        , IToArray<T>
        , ICopyFromSpan<T>
        , ITryCopyFromSpan<T>
        , ICopyToSpan<T>
        , ITryCopyToSpan<T>
        , IAddRangeSpan<T>
        , IHasCapacity
        , IHasCount
        , IIncreaseCapacity
        , IClearable
        , IIsCreated
        , IRefIndexer<T>
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged
    {
        internal BufferUnsafe<T> _buffer;
        internal int _count;
        internal int _version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ListUnsafe(int capacity, AllocatorStrategy allocator)
            : this()
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(capacity >= 0);
            ThrowHelper.ThrowIfAllocatorIsInvalid(allocator.IsValid);
            _buffer = new BufferUnsafe<T>(capacity, allocator, clear: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ListUnsafe(ReadOnlySpan<T> source, AllocatorStrategy allocator)
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

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowIfIndexIsOutOfRange(
                      (uint)index < (uint)_count
                    , ThrowHelper.CollectionType.ListUnsafe
                );
                return ref _buffer[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index)
        {
            return ref this[index];
        }

        T IReadOnlyList<T>.this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[index];
        }

        T IReadOnlyIndexer<T>.this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[index];
        }

        T IIndexer<T>.this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            EnsureCapacity(_count + 1);
            _buffer.AsSpan()[_count++] = item;
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            EnsureCapacity(_count + 1);
            _buffer.AsSpan()[_count++] = item;
            _version++;
        }

        public void Insert(int index, T item)
        {
            ThrowHelper.ThrowIfInsertionIndexIsOutOfRange(
                  (uint)index <= (uint)_count
                , ThrowHelper.CollectionType.ListUnsafe
            );
            EnsureCapacity(_count + 1);
            Move(index, index + 1, _count - index);
            _buffer.AsSpan()[index] = item;
            _count++;
            _version++;
        }

        public void Insert(int index, in T item)
        {
            ThrowHelper.ThrowIfInsertionIndexIsOutOfRange(
                  (uint)index <= (uint)_count
                , ThrowHelper.CollectionType.ListUnsafe
            );
            EnsureCapacity(_count + 1);
            Move(index, index + 1, _count - index);
            _buffer.AsSpan()[index] = item;
            _count++;
            _version++;
        }

        public void AddRange(ReadOnlySpan<T> items)
            => AddRange(items, items.Length);

        public void AddRange(ReadOnlySpan<T> items, int count)
        {
            ThrowHelper.ThrowIfSourceCountIsInvalid((uint)count <= (uint)items.Length);

            if (count == 0)
            {
                return;
            }

            EnsureCapacity(checked(_count + count));
            items[..count].CopyTo(_buffer.AsSpan().Slice(_count, count));
            _count += count;
            _version++;
        }

        public void CopyFrom(ReadOnlySpan<T> source)
            => CopyFrom(0, source);

        public void CopyFrom(ReadOnlySpan<T> source, int length)
            => CopyFrom(0, source, length);

        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => CopyFrom(destinationStartIndex, source, source.Length);

        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).CopyFrom(destinationStartIndex, source, length);

        public bool TryCopyFrom(ReadOnlySpan<T> source)
            => TryCopyFrom(0, source);

        public bool TryCopyFrom(ReadOnlySpan<T> source, int length)
            => TryCopyFrom(0, source, length);

        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => TryCopyFrom(destinationStartIndex, source, source.Length);

        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).TryCopyFrom(destinationStartIndex, source, length);

        public void CopyTo(T[] destination, int destinationIndex)
            => CopyTo(destination.AsSpan().Slice(destinationIndex, _count));

        public void CopyTo(Span<T> destination)
            => CopyTo(0, destination);

        public void CopyTo(Span<T> destination, int length)
            => CopyTo(0, destination, length);

        public void CopyTo(int sourceStartIndex, Span<T> destination)
            => CopyTo(sourceStartIndex, destination, destination.Length);

        public void CopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

        public bool TryCopyTo(Span<T> destination)
            => TryCopyTo(0, destination);

        public bool TryCopyTo(Span<T> destination, int length)
            => TryCopyTo(0, destination, length);

        public bool TryCopyTo(int sourceStartIndex, Span<T> destination)
            => TryCopyTo(sourceStartIndex, destination, destination.Length);

        public bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

        public void Clear()
        {
            _count = 0;
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastClear()
            => Clear();

        public void RemoveAt(int index)
        {
            ThrowHelper.ThrowIfIndexIsOutOfRange(
                  (uint)index < (uint)_count
                , ThrowHelper.CollectionType.ListUnsafe
            );
            Move(index + 1, index, _count - index - 1);
            _count--;
            _version++;
        }

        public void RemoveRange(int startIndex, int length)
        {
            ThrowHelper.ThrowIfStartIndexIsOutOfRange((uint)startIndex < (uint)_count);
            ThrowHelper.ThrowIfSourceLengthIsInvalid((uint)length <= (uint)(_count - startIndex));
            if (length == 0)
            {
                return;
            }

            Move(startIndex + length, startIndex, _count - startIndex - length);
            _count -= length;
            _version++;
        }

        public void RemoveAtSwapBack(int index)
        {
            ThrowHelper.ThrowIfIndexIsOutOfRange(
                  (uint)index < (uint)_count
                , ThrowHelper.CollectionType.ListUnsafe
            );
            _count--;
            if (index != _count)
            {
                _buffer.AsSpan()[index] = _buffer.AsReadOnlySpan()[_count];
            }
            _version++;
        }

        public readonly ref readonly T Peek()
        {
            ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.ListUnsafe);
            return ref _buffer.AsReadOnlySpan()[_count - 1];
        }

        public ref readonly T Pop()
        {
            ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.ListUnsafe);
            _version++;
            return ref _buffer.AsReadOnlySpan()[--_count];
        }

        public int Push(T item)
        {
            Add(item);
            return _count - 1;
        }

        public int Push(in T item)
        {
            Add(in item);
            return _count - 1;
        }

        public Span<T> AddReplicate(int amount)
        {
            ThrowHelper.ThrowIfAmountIsInvalid(amount > 0);
            var oldCount = _count;
            EnsureCapacity(checked(oldCount + amount));
            _count += amount;
            _version++;
            var result = _buffer.AsSpan().Slice(oldCount, amount);
            result.Clear();
            return result;
        }

        public Span<T> AddReplicate(T value, int amount)
        {
            ThrowHelper.ThrowIfAmountIsInvalid(amount > 0);
            var oldCount = _count;
            EnsureCapacity(checked(oldCount + amount));
            _count += amount;
            _version++;
            var result = _buffer.AsSpan().Slice(oldCount, amount);
            result.Fill(value);
            return result;
        }

        public Span<T> AddReplicateNoInit(int amount)
        {
            ThrowHelper.ThrowIfAmountIsInvalid(amount > 0);
            var oldCount = _count;
            EnsureCapacity(checked(oldCount + amount));
            _count += amount;
            _version++;
            return _buffer.AsSpan().Slice(oldCount, amount);
        }

        public int IncreaseCapacityBy(int amount)
            => IncreaseCapacityTo(checked(Capacity + amount));

        public int IncreaseCapacityTo(int newCapacity)
        {
            ThrowHelper.ThrowIfCapacityBelowCount(newCapacity >= _count);

            if (newCapacity <= _buffer.Capacity)
            {
                return _buffer.Capacity;
            }

            if (_buffer.IsCreated)
            {
                _buffer.Resize(newCapacity, copyContent: true, memClear: false);
            }
            else
            {
                _buffer.Alloc(newCapacity, _buffer._allocator, memClear: false);
            }

            _version++;
            return newCapacity;
        }

        public void Trim()
        {
            if (Capacity == _count)
            {
                return;
            }

            _buffer.Resize(_count, copyContent: true, memClear: false);
            _version++;
        }

        public readonly Span<T> AsSpan()
            => _buffer.AsSpan()[.._count];

        public readonly ReadOnlySpan<T> AsReadOnlySpan()
            => _buffer.AsReadOnlySpan()[.._count];

        public readonly T[] ToArray()
            => AsReadOnlySpan().ToArray();

        public readonly Enumerator GetEnumerator()
            => new(AsReadOnly());

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public readonly ReadOnly AsReadOnly()
            => new(this);

        public ListUnsafe<U> Reinterpret<U>()
            where U : unmanaged
        {
            ThrowHelper.ThrowIfTypesHaveDifferentSize(
                UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>()
            );
            return new ListUnsafe<U>
            {
                _buffer = _buffer.Reinterpret<U>(),
                _count = _count,
                _version = _version,
            };
        }

        private void EnsureCapacity(int newSize)
        {
            if (newSize <= Capacity)
            {
                return;
            }

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
        }

        private void Move(int sourceIndex, int destinationIndex, int length)
        {
            if (length <= 0)
            {
                return;
            }
            // SAFETY: Both ranges are inside the live buffer and MemMove handles overlap.
            unsafe
            {
                UnsafeUtility.MemMove(
                      (byte*)_buffer.GetUnsafePtr() + destinationIndex * UnsafeUtility.SizeOf<T>()
                    , (byte*)_buffer.GetUnsafePtr() + sourceIndex * UnsafeUtility.SizeOf<T>()
                    , length * (long)UnsafeUtility.SizeOf<T>()
                );
            }
        }

        public void Dispose()
        {
            _buffer.Dispose();
            _count = 0;
            _version = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var result = _buffer.Dispose(inputDeps);
            _count = 0;
            _version = 0;
            return result;
        }

        /// <safety>The returned header owns the allocation and must be freed exactly once with Free.</safety>
        internal static unsafe ListUnsafe<T>* Alloc(int capacity, AllocatorStrategy allocator)
        {
            // SAFETY: The allocator returns storage for one ListUnsafe header and the header owns its buffer.
            unsafe
            {
                var data = allocator.Allocate<ListUnsafe<T>>();
                *data = new ListUnsafe<T>(capacity, allocator);
                return data;
            }
        }

        /// <safety>The returned header owns a copy of source and must be freed exactly once with Free.</safety>
        internal static unsafe ListUnsafe<T>* Alloc(
              ReadOnlySpan<T> source
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The allocator returns storage for one ListUnsafe header and the header owns its buffer.
            unsafe
            {
                var data = allocator.Allocate<ListUnsafe<T>>();
                *data = new ListUnsafe<T>(source, allocator);
                return data;
            }
        }

        /// <safety>data must be a live header allocated by Alloc and must not be used after this call.</safety>
        internal static unsafe void Free(ListUnsafe<T>* data)
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
