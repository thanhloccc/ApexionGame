using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Types;
using Unity.Collections;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    public partial class SharedList<T> : SharedList<T, T>
        where T : unmanaged
    {
        public SharedList() : base()
        {
        }

        public SharedList(int capacity) : base(capacity)
        {
        }

        public SharedList([NotNull] params T[] source) : base(source)
        {
        }

        public SharedList(in ArraySegment<T> source) : base(source)
        {
        }

        public SharedList(in ReadOnlySpan<T> source) : base(source)
        {
        }

        public SharedList(in NativeArray<T> source) : base(source)
        {
        }

        public SharedList(in NativeSlice<T> source) : base(source)
        {
        }

        public SharedList([NotNull] ICollection<T> source) : base(source)
        {
        }

        public SharedList([NotNull] ICollection<T> source, int extraSize) : base(source, extraSize)
        {
        }

        public SharedList([NotNull] in SharedList<T, T> source) : base(source)
        {
        }
    }

    public partial class SharedList<T, TNative> : IList<T>, IReadOnlyList<T>, IIndexer<T>
        , IAsSpan<T>, IAsReadOnlySpan<T>, IToArray<T>
        , ICopyFromSpan<T>, ITryCopyFromSpan<T>
        , ICopyToSpan<T>, ITryCopyToSpan<T>
        , IAddRangeSpan<T>, IContains<T>
        , IClearable, IDisposable
        , IIncreaseCapacity, IHasCount
        where T : unmanaged
        where TNative : unmanaged
    {
        internal SharedArray<T, TNative> _buffer;
        internal SharedReference<int> _count;
        internal SharedReference<int> _version;

        // Native views share this live header but copy the buffer's safety handle.
        // Resize releases that handle, invalidating older checked views; views created
        // afterward use the refreshed pointer/capacity and the new owner handle.
        internal unsafe SharedListUnsafe<TNative>* _nativeData;

        public SharedList()
        {
            _buffer = new(0);
            _count = new(0);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList(int capacity)
        {
            _buffer = new(capacity);
            _count = new(0);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList([NotNull] params T[] source)
        {
            _buffer = new(source);
            _count = new(source.Length);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList(in ArraySegment<T> source)
        {
            _buffer = new(source);
            _count = new(source.Count);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList(in ReadOnlySpan<T> source)
        {
            _buffer = new(source);
            _count = new(source.Length);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList([NotNull] ICollection<T> source)
        {
            _buffer = new(source);
            _count = new(source.Count);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList([NotNull] ICollection<T> source, int extraSize)
        {
            _buffer = new(source, extraSize);
            _count = new(source.Count);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList(in NativeArray<TNative> source)
        {
            _buffer = new(source);
            _count = new(source.Length);
            _version = new(0);
            InitializeNativeData();
        }

        public SharedList(in NativeSlice<TNative> source)
        {
            _buffer = new(source);
            _count = new(source.Length);
            _version = new(0);
            InitializeNativeData();
        }

        ~SharedList()
        {
            Dispose();
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count.ValueRO;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
        }

        public bool IsReadOnly
            => false;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowIfIndexIsOutOfRange((uint)index < (uint)_count.ValueRO, ThrowHelper.CollectionType.SharedListWithNative);
                return _buffer.AsReadOnlySpan()[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ThrowHelper.ThrowIfIndexIsOutOfRange((uint)index < (uint)_count.ValueRO, ThrowHelper.CollectionType.SharedListWithNative);
                _version.ValueRW++;
                _buffer.AsSpan()[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SharedListNative<TNative>(SharedList<T, TNative> list)
            => list.AsNative();

        public void Dispose()
        {
            if (_buffer == null)
            {
                return;
            }

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                SharedListUnsafe<TNative>.Free(_nativeData, Allocator.Persistent);
                _nativeData = null;
            }

            _buffer.Dispose();
            _count.Dispose();
            _version.Dispose();

            _buffer = null;
            _count = null;
            _version = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
            => IndexOf(item, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item, int index)
            => IndexOf(item, index, _count.ValueRO - index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item, int index, int count)
        {
            ThrowIfIndexIsNegative(index >= 0);
            ThrowIfCountIsNegative(count >= 0);
            ThrowHelper.ThrowIfIndexSectionIsInvalid(index + count <= _count.ValueRO, ThrowHelper.CollectionType.SharedListWithNative);
            return Array.IndexOf(_buffer, item, index, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(in T item)
            => IndexOf(in item, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(in T item, int index)
            => IndexOf(in item, index, _count.ValueRO - index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(in T item, int index, int count)
        {
            ThrowIfIndexIsNegative(index >= 0);
            ThrowIfCountIsNegative(count >= 0);
            ThrowHelper.ThrowIfIndexSectionIsInvalid(index + count <= _count.ValueRO, ThrowHelper.CollectionType.SharedListWithNative);
            return Array.IndexOf(_buffer, item, index, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            _version.ValueRW++;

            ref var count = ref _count.ValueRW;

            if (count == _buffer.Length)
            {
                AllocateMore();
            }

            _buffer.AsSpan()[count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            _version.ValueRW++;

            ref var count = ref _count.ValueRW;

            if (count == _buffer.Length)
            {
                AllocateMore();
            }

            _buffer.AsSpan()[count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            _version.ValueRW++;

            ref var count = ref _count.ValueRW;

            ThrowHelper.ThrowIfInsertionIndexIsOutOfRange((uint)index <= (uint)count, ThrowHelper.CollectionType.SharedListWithNative);

            if (count == _buffer.Length)
            {
                AllocateMore();
            }

            var buffer = _buffer.AsManagedArray();
            Array.Copy(buffer, index, buffer, index + 1, count - index);
            ++count;

            _buffer.AsSpan()[index] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, in T item)
        {
            _version.ValueRW++;

            ref var count = ref _count.ValueRW;

            ThrowHelper.ThrowIfInsertionIndexIsOutOfRange((uint)index <= (uint)count, ThrowHelper.CollectionType.SharedListWithNative);

            if (count == _buffer.Length)
            {
                AllocateMore();
            }

            var buffer = _buffer.AsManagedArray();
            Array.Copy(buffer, index, buffer, index + 1, count - index);
            ++count;

            _buffer.AsSpan()[index] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index)
        {
            ThrowHelper.ThrowIfIndexIsOutOfRange((uint)index < (uint)_count.ValueRO, ThrowHelper.CollectionType.SharedListWithNative);
            return ref _buffer.AsSpan()[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange([NotNull] T[] items)
            => AddRange(items, items.Length);

        public void AddRange([NotNull] T[] items, int count)
        {
            _version.ValueRW++;

            if (count == 0)
            {
                return;
            }

            if (_buffer.Length - _count.ValueRO < count)
            {
                AllocateMore(checked(_count.ValueRO + count));
            }

            items.AsSpan()[..count].CopyTo(_buffer.AsSpan().Slice(_count.ValueRO, count));
            _count.ValueRW += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ReadOnlySpan<T> items)
            => AddRange(items, items.Length);

        public void AddRange(ReadOnlySpan<T> items, int count)
        {
            _version.ValueRW++;

            if (count == 0)
            {
                return;
            }

            if (_buffer.Length - _count.ValueRO < count)
            {
                AllocateMore(checked(_count.ValueRO + count));
            }

            items[..count].CopyTo(_buffer.AsSpan().Slice(_count.ValueRO, count));
            _count.ValueRW += count;
        }

        public void AddRange([NotNull] IEnumerable<T> collection)
        {
            if (collection is ICollection<T> c)
            {
                var count = c.Count;

                if (count > 0)
                {
                    if (_buffer.Length - _count.ValueRO < count)
                    {
                        AllocateMore(checked(_count.ValueRO + count));
                    }

                    c.CopyTo(_buffer.AsManagedArray(), _count.ValueRO);
                    _count.ValueRW += count;
                    _version.ValueRW++;
                }
            }
            else
            {
                using IEnumerator<T> en = collection.GetEnumerator();

                while (en.MoveNext())
                {
                    Add(en.Current);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item)
        {
            var count = _count.ValueRO;
            return count > 0 && Array.IndexOf(_buffer, item, 0, count) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _version.ValueRW++;
            _count.ValueRW = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] destination, int destinationIndex)
            => CopyTo(destination.AsSpan().Slice(destinationIndex, _count.ValueRO));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ReadOnlySpan<T> source)
            => CopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ReadOnlySpan<T> source, int length)
            => CopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => CopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).CopyFrom(destinationStartIndex, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(ReadOnlySpan<T> source)
            => TryCopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(ReadOnlySpan<T> source, int length)
            => TryCopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => TryCopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).TryCopyFrom(destinationStartIndex, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> destination)
            => CopyTo(0, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> destination, int length)
            => CopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int sourceStartIndex, Span<T> destination)
            => CopyTo(sourceStartIndex, destination, destination.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<T> destination)
            => TryCopyTo(0, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<T> destination, int length)
            => TryCopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(int sourceStartIndex, Span<T> destination)
            => TryCopyTo(sourceStartIndex, destination, destination.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(AsReadOnly());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
            => IncreaseCapacityTo(_buffer.Length + amount);

        public int IncreaseCapacityTo(int newCapacity)
        {
            _version.ValueRW++;

            if (newCapacity <= _buffer.Length)
            {
                return _buffer.Length;
            }

            ResizeBuffer(newCapacity);
            return _buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Peek()
            => ref _buffer.AsReadOnlySpan()[_count.ValueRO - 1];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Pop()
        {
            _version.ValueRW++;
            --_count.ValueRW;
            return ref _buffer.AsReadOnlySpan()[_count.ValueRO];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Push(T item)
        {
            Insert(_count.ValueRO, item);
            return _count.ValueRO - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Push(in T item)
        {
            Insert(_count.ValueRO, item);
            return _count.ValueRO - 1;
        }

        public bool Remove(T item)
        {
            _version.ValueRW++;

            var index = IndexOf(item);

            if ((uint)index >= (uint)_count.ValueRO)
            {
                return false;
            }

            if (index < --_count.ValueRW)
            {
                Array.Copy(_buffer, index + 1, _buffer, index, _count.ValueRO - index);
            }

            if (EncosyTypeExtensions.IsUnmanaged<T>() == false)
            {
                _buffer.AsSpan()[_count.ValueRO] = default;
            }

            return true;
        }

        public bool Remove(in T item)
        {
            _version.ValueRW++;

            var index = IndexOf(item);

            if ((uint)index >= (uint)_count.ValueRO)
            {
                return false;
            }

            if (index < --_count.ValueRW)
            {
                Array.Copy(_buffer, index + 1, _buffer, index, _count.ValueRO - index);
            }

            if (EncosyTypeExtensions.IsUnmanaged<T>() == false)
            {
                _buffer.AsSpan()[_count.ValueRO] = default;
            }

            return true;
        }

        public void RemoveAt(int index)
        {
            ThrowIfRemovalIndexIsOutOfRange((uint)index < (uint)_count.ValueRO);

            _version.ValueRW++;

            if (index < --_count.ValueRW)
            {
                var buffer = _buffer.AsManagedArray();
                Array.Copy(buffer, index + 1, buffer, index, _count.ValueRO - index);
            }
        }

        public void RemoveRange(int startIndex, int length)
        {
            var count = _count.ValueRO;

            ThrowIfStartIndexIsOutOfRange((uint)startIndex < (uint)count);

            var end = startIndex + length;

            ThrowIfRemovalRangeIsOutOfRange((uint)end <= (uint)count);

            _version.ValueRW++;

            if (length < 1)
            {
                return;
            }

            count = _count.ValueRW -= length;

            var buffer = _buffer.AsManagedArray();

            Array.Copy(buffer, startIndex + length, buffer, startIndex, count - startIndex);
        }

        public void RemoveAtSwapBack(int index)
        {
            ThrowIfRemovalIndexIsOutOfRange((uint)index < (uint)_count.ValueRO);

            _version.ValueRW++;

            if (index < --_count.ValueRW)
            {
                var buffer = _buffer.AsSpan();
                buffer[index] = buffer[_count.ValueRO];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            return AsReadOnlySpan().ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            _version.ValueRW++;
            return _buffer.AsSpan()[.._count.ValueRO];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return _buffer.AsReadOnlySpan()[.._count.ValueRO];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trim()
        {
            _version.ValueRW++;

            if (_count.ValueRO < _buffer.Length)
            {
                ResizeBuffer(_count.ValueRO);
            }
        }

        public Span<T> AddReplicate(int amount)
        {
            _version.ValueRW++;

            var oldCount = _count.ValueRO;
            var newCount = amount + oldCount;
            var offset = newCount - _buffer.Length;

            if (offset > 0)
            {
                AllocateMore(newCount);
            }

            var buffer = _buffer.AsSpan().Slice(oldCount, amount);
            buffer.Fill(default);
            _count.ValueRW = newCount;

            return buffer;
        }

        public Span<T> AddReplicate(T value, int amount)
        {
            _version.ValueRW++;

            var oldCount = _count.ValueRO;
            var newCount = amount + oldCount;
            var offset = newCount - _buffer.Length;

            if (offset > 0)
            {
                AllocateMore(newCount);
            }

            var buffer = _buffer.AsSpan().Slice(oldCount, amount);
            buffer.Fill(value);
            _count.ValueRW = newCount;

            return buffer;
        }

        public Span<T> AddReplicateNoInit(int amount)
        {
            _version.ValueRW++;

            var oldCount = _count.ValueRO;
            var newCount = amount + oldCount;
            var offset = newCount - _buffer.Length;

            if (offset > 0)
            {
                AllocateMore(newCount);
            }

            var buffer = _buffer.AsSpan().Slice(oldCount, amount);
            _count.ValueRW = newCount;

            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SharedList<T, TNative> Prefill(int amount)
        {
            var list = new SharedList<T, TNative>(amount);
            list.AddReplicate(amount);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SharedList<T, TNative> Prefill(T value, int amount)
        {
            var list = new SharedList<T, TNative>(amount);
            list.AddReplicate(value, amount);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CalcNewCapacity(int newSize)
        {
            newSize = Math.Max(4, newSize);
            return checked(((int)Math.Ceiling(newSize * 1.5f) / 4) * 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AllocateMore()
        {
            var newCapacity = CalcNewCapacity(_buffer.Length + 1);
            ResizeBuffer(newCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AllocateMore(int newSize)
        {
            ThrowHelper.ThrowIfNewCapacityIsInvalid(newSize > _buffer.Length);

            var newCapacity = CalcNewCapacity(newSize);
            ResizeBuffer(newCapacity);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfIndexIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
        {
            if (isWithinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("index is outside the range of valid indices for the SharedList<T>");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfIndexIsNegative([DoesNotReturnIf(false)] bool isNonNegative)
        {
            if (isNonNegative == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("index is less than 0");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfCountIsNegative([DoesNotReturnIf(false)] bool isNonNegative)
        {
            if (isNonNegative == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("count is less than 0");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSectionIsInvalid([DoesNotReturnIf(false)] bool isWithinRange)
        {
            if (isWithinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("index and count do not specify a valid section in the SharedList<T>");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfInsertionIndexIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
        {
            if (isWithinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("index is outside the range of valid indices for the SharedList<T>");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfRemovalIndexIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
        {
            if (isWithinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("out of bound index");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfStartIndexIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
        {
            if (isWithinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("out of bound start index");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfRemovalRangeIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
        {
            if (isWithinRange == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("out of bound length");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfNewSizeDoesNotExceedCapacity([DoesNotReturnIf(false)] bool exceedsCapacity)
        {
            if (exceedsCapacity == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("newSize is not greater than the current capacity");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeNativeData()
        {
            // SAFETY: The managed buffer and shared counters remain pinned for the native view lifetime.
            unsafe
            {
                _nativeData = SharedListUnsafe<TNative>.Alloc(
                      _buffer.GetUnsafeBufferPointer()
                    , _buffer.Length
                    , _count.GetUnsafeBufferPointer()
                    , _version.GetUnsafeBufferPointer()
                    , Allocator.Persistent
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RefreshNativeData()
        {
            // SAFETY: The shared native header is live and all pointers are refreshed from still-owned storage.
            unsafe
            {
                _nativeData->_buffer = _buffer.GetUnsafeBufferPointer();
                _nativeData->_capacity = _buffer.Length;
                _nativeData->_count = _count.GetUnsafeBufferPointer();
                _nativeData->_version = _version.GetUnsafeBufferPointer();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeBuffer(int newCapacity)
        {
            _buffer.Resize(newCapacity);
            RefreshNativeData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
