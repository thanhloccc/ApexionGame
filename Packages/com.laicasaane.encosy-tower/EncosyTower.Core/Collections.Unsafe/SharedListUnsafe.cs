using System;
using EncosyTower.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Unsafe
{
    internal struct SharedListUnsafe<T>
        where T : unmanaged
    {
        internal unsafe T* _buffer;
        internal int _capacity;
        internal unsafe int* _count;
        internal unsafe int* _version;

        internal static unsafe SharedListUnsafe<T>* Alloc(
              T* buffer
            , int capacity
            , int* count
            , int* version
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The allocator returns writable storage for the complete unmanaged struct.
            unsafe
            {
                var data = allocator.Allocate<SharedListUnsafe<T>>();
                *data = new SharedListUnsafe<T> {
                    _buffer = buffer,
                    _capacity = capacity,
                    _count = count,
                    _version = version,
                };
                return data;
            }
        }

        internal static unsafe void Free(SharedListUnsafe<T>* data, AllocatorStrategy allocator)
        {
            if (data == null)
            {
                return;
            }

            // SAFETY: The pointer was allocated by the matching allocator and is freed once by its owner.
            unsafe
            {
                allocator.Free(data);
            }
        }

        internal readonly int Capacity
            => _capacity;

        internal readonly int Count
        {
            get
            {
                // SAFETY: The owning shared list keeps the count storage live while the header is borrowed.
                unsafe
                {
                    return *_count;
                }
            }
        }

        internal readonly int Version
        {
            get
            {
                // SAFETY: The owning shared list keeps the version storage live while the header is borrowed.
                unsafe
                {
                    return *_version;
                }
            }
        }

        internal T this[int index]
        {
            get
            {
                ThrowHelper.ThrowIfIndexIsOutOfRange(
                      (uint)index < (uint)Count
                    , ThrowHelper.CollectionType.SharedListUnsafe
                );

                // SAFETY: The index check bounds the access to the live shared buffer.
                unsafe
                {
                    return _buffer[index];
                }
            }
            set
            {
                ThrowHelper.ThrowIfIndexIsOutOfRange(
                      (uint)index < (uint)Count
                    , ThrowHelper.CollectionType.SharedListUnsafe
                );

                IncrementVersion();

                // SAFETY: The index check bounds the access to the live shared buffer.
                unsafe
                {
                    _buffer[index] = value;
                }
            }
        }

        internal void Add(T item)
        {
            IncrementVersion();

            var count = Count;
            ThrowHelper.ThrowIfCapacityIsImmutable(
                  count < _capacity
                , ThrowHelper.CollectionType.SharedListUnsafe
            );

            // SAFETY: The capacity check leaves one live buffer slot available for the appended value.
            unsafe
            {
                _buffer[count] = item;
                *_count = count + 1;
            }
        }

        internal void Add(in T item)
        {
            IncrementVersion();

            var count = Count;
            ThrowHelper.ThrowIfCapacityIsImmutable(
                  count < _capacity
                , ThrowHelper.CollectionType.SharedListUnsafe
            );

            // SAFETY: The capacity check leaves one live buffer slot available for the appended value.
            unsafe
            {
                _buffer[count] = item;
                *_count = count + 1;
            }
        }

        internal void Insert(int index, T item)
        {
            IncrementVersion();

            var count = Count;
            ThrowHelper.ThrowIfInsertionIndexIsOutOfRange(
                  (uint)index <= (uint)count
                , ThrowHelper.CollectionType.SharedListUnsafe
            );
            ThrowHelper.ThrowIfCapacityIsImmutable(
                  count < _capacity
                , ThrowHelper.CollectionType.SharedListUnsafe
            );

            GetBufferSpan().Slice(index, count - index).CopyTo(GetBufferSpan()[(index + 1)..]);
            SetCount(count + 1);
            GetBufferSpan()[index] = item;
        }

        internal void Insert(int index, in T item)
        {
            Insert(index, item);
        }

        internal ref T ElementAt(int index)
        {
            ThrowHelper.ThrowIfIndexIsOutOfRange(
                  (uint)index < (uint)Count
                , ThrowHelper.CollectionType.SharedListUnsafe
            );

            // SAFETY: The index check bounds the returned reference to the live shared buffer.
            unsafe
            {
                return ref _buffer[index];
            }
        }

        internal void AddRange(ReadOnlySpan<T> items)
            => AddRange(items, items.Length);

        internal void AddRange(ReadOnlySpan<T> items, int count)
        {
            IncrementVersion();

            if (count == 0)
            {
                return;
            }

            var currentCount = Count;
            ThrowHelper.ThrowIfCapacityIsImmutable(
                  _capacity - currentCount >= count
                , ThrowHelper.CollectionType.SharedListUnsafe
            );

            items[..count].CopyTo(GetBufferSpan().Slice(currentCount, count));
            SetCount(currentCount + count);
        }

        internal void AddRange(in NativeSlice<T> items)
            => AddRange(in items, items.Length);

        internal void AddRange(in NativeSlice<T> items, int count)
        {
            IncrementVersion();

            if (count == 0)
            {
                return;
            }

            var currentCount = Count;
            ThrowHelper.ThrowIfCapacityIsImmutable(
                  _capacity - currentCount >= count
                , ThrowHelper.CollectionType.SharedListUnsafe
            );

            var destination = ConvertFrom(GetBufferSpan().Slice(currentCount, count));

            items.Slice(0, count).CopyTo(destination);
            SetCount(currentCount + count);

            return;

            static NativeArray<T> ConvertFrom(Span<T> source)
            {
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray(source, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var safety = AtomicSafetyHandle.GetTempMemoryHandle();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);
#endif

                return array;
            }
        }

        internal void CopyFrom(ReadOnlySpan<T> source)
            => CopyFrom(0, source);

        internal void CopyFrom(ReadOnlySpan<T> source, int length)
            => CopyFrom(0, source, length);

        internal void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => CopyFrom(destinationStartIndex, source, source.Length);

        internal void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).CopyFrom(destinationStartIndex, source, length);

        internal bool TryCopyFrom(ReadOnlySpan<T> source)
            => TryCopyFrom(0, source);

        internal bool TryCopyFrom(ReadOnlySpan<T> source, int length)
            => TryCopyFrom(0, source, length);

        internal bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => TryCopyFrom(destinationStartIndex, source, source.Length);

        internal bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).TryCopyFrom(destinationStartIndex, source, length);

        internal void CopyTo(Span<T> destination)
            => CopyTo(0, destination);

        internal void CopyTo(Span<T> destination, int length)
            => CopyTo(0, destination, length);

        internal void CopyTo(int sourceStartIndex, Span<T> destination)
            => CopyTo(sourceStartIndex, destination, destination.Length);

        internal void CopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

        internal bool TryCopyTo(Span<T> destination)
            => TryCopyTo(0, destination);

        internal bool TryCopyTo(Span<T> destination, int length)
            => TryCopyTo(0, destination, length);

        internal bool TryCopyTo(int sourceStartIndex, Span<T> destination)
            => TryCopyTo(sourceStartIndex, destination, destination.Length);

        internal bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

        internal void Clear()
        {
            IncrementVersion();
            SetCount(0);
        }

        internal ref readonly T Peek()
        {
            ThrowHelper.ThrowIfEmpty(Count > 0, ThrowHelper.CollectionType.SharedListUnsafe);

            // SAFETY: Count identifies the last occupied slot in the live shared buffer.
            unsafe
            {
                return ref _buffer[Count - 1];
            }
        }

        internal ref readonly T Pop()
        {
            ThrowHelper.ThrowIfEmpty(Count > 0, ThrowHelper.CollectionType.SharedListUnsafe);

            IncrementVersion();
            var count = Count - 1;
            SetCount(count);

            // SAFETY: The decremented count identifies the removed last slot in the live buffer.
            unsafe
            {
                return ref _buffer[count];
            }
        }

        internal int Push(T item)
        {
            Insert(Count, item);
            return Count - 1;
        }

        internal int Push(in T item)
        {
            Insert(Count, in item);
            return Count - 1;
        }

        internal void RemoveAt(int index)
        {
            var count = Count;
            ThrowHelper.ThrowIfRemovalIndexIsOutOfRange((uint)index < (uint)count);

            IncrementVersion();

            count--;
            SetCount(count);

            if (index < count)
            {
                GetBufferSpan().Slice(index + 1, count - index).CopyTo(GetBufferSpan()[index..]);
            }
        }

        internal void RemoveRange(int startIndex, int length)
        {
            var count = Count;
            ThrowHelper.ThrowIfStartIndexIsOutOfRange((uint)startIndex < (uint)count);

            var end = startIndex + length;
            ThrowHelper.ThrowIfRemovalRangeIsOutOfRange((uint)end <= (uint)count);

            IncrementVersion();

            if (length < 1)
            {
                return;
            }

            count -= length;
            SetCount(count);
            GetBufferSpan().Slice(end, count - startIndex).CopyTo(GetBufferSpan()[startIndex..]);
        }

        internal void RemoveAtSwapBack(int index)
        {
            var count = Count;
            ThrowHelper.ThrowIfRemovalIndexIsOutOfRange((uint)index < (uint)count);

            IncrementVersion();

            count--;
            SetCount(count);

            if (index < count)
            {
                GetBufferSpan()[index] = GetBufferSpan()[count];
            }
        }

        internal readonly T[] ToArray()
            => AsReadOnlySpan().ToArray();

        internal Span<T> AsSpan()
        {
            IncrementVersion();
            return GetBufferSpan()[..Count];
        }

        internal readonly ReadOnlySpan<T> AsReadOnlySpan()
            => GetBufferSpan()[..Count];

        internal Span<T> AddReplicate(int amount)
        {
            var buffer = AddReplicateNoInit(amount);
            buffer.Clear();
            return buffer;
        }

        internal Span<T> AddReplicate(T value, int amount)
        {
            var buffer = AddReplicateNoInit(amount);
            buffer.Fill(value);
            return buffer;
        }

        internal Span<T> AddReplicateNoInit(int amount)
        {
            IncrementVersion();

            var oldCount = Count;
            var newCount = amount + oldCount;
            ThrowHelper.ThrowIfCapacityIsImmutable(
                  newCount <= _capacity
                , ThrowHelper.CollectionType.SharedListUnsafe
            );

            var result = GetBufferSpan().Slice(oldCount, amount);
            SetCount(newCount);
            return result;
        }

        internal void IncrementVersion()
        {
            // SAFETY: The owning shared list keeps the version storage live while the header is borrowed.
            unsafe
            {
                (*_version)++;
            }
        }

        private readonly Span<T> GetBufferSpan()
        {
            // SAFETY: The owner pins a buffer with exactly the capacity recorded in this live header.
            unsafe
            {
                return new Span<T>(_buffer, _capacity);
            }
        }

        private void SetCount(int count)
        {
            // SAFETY: The owning shared list keeps the count storage live while the header is borrowed.
            unsafe
            {
                *_count = count;
            }
        }
    }
}
