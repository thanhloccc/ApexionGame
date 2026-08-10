using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Extensions;
using EncosyTower.Common;
using EncosyTower.Types;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    /// <summary>
    /// Represents a list that does not own the internal buffer and related data.
    /// Instead, it relies on an <see cref="IBufferProvider{TBuffer, T}"/>
    /// to provide access to the buffer, count, and version.
    /// Effectively, anything implementing <see cref="IBufferProvider{TBuffer, T}"/> can be used
    /// as the external state for this list.
    /// </summary>
    public readonly partial struct ListProxy<TProvider, TBuffer, T> : IReadOnlyList<T>, IIndexer<T>
        , IAsSpan<T>, IAsReadOnlySpan<T>, IToArray<T>
        , ICopyFromSpan<T>, ITryCopyFromSpan<T>
        , ICopyToSpan<T>, ITryCopyToSpan<T>
        , IAddRangeSpan<T>
        , IClearable, IIncreaseCapacity, IHasCount, IIsCreated
        where TProvider : IBufferProvider<TBuffer, T>
        where TBuffer : IBuffer<T>
    {
        public readonly TProvider Provider;

        private readonly ByteBool _isCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ListProxy([NotNull] TProvider provider)
        {
            Provider = provider;
            _isCreated = true;
        }

        public ListProxy([NotNull] TProvider provider, int capacity)
        {
            Provider = provider;
            _isCreated = true;

            if (capacity > provider.Buffer.Capacity)
            {
                AllocateMore(capacity);
            }
        }

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isCreated;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Capacity;
        }

        public bool IsReadOnly
            => false;

#pragma warning disable IDE1006 // Naming Styles
        internal ref TBuffer _buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfProviderIsNull(Provider != null);
                return ref Provider.Buffer;
            }
        }

        internal ref int _count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfProviderIsNull(Provider != null);
                return ref Provider.Count;
            }
        }

        internal ref int _version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfProviderIsNull(Provider != null);
                return ref Provider.Version;
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowIfIndexIsOutOfRange((uint)index < (uint)_count, ThrowHelper.CollectionType.ListProxy);
                return _buffer[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ThrowHelper.ThrowIfIndexIsOutOfRange((uint)index < (uint)_count, ThrowHelper.CollectionType.ListProxy);
                _version++;
                _buffer[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            _version++;

            if (_count == _buffer.Capacity)
            {
                AllocateMore();
            }

            _buffer[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            _version++;

            if (_count == _buffer.Capacity)
            {
                AllocateMore();
            }

            _buffer[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            ThrowHelper.ThrowIfInsertionIndexIsOutOfRange((uint)index <= (uint)_count, ThrowHelper.CollectionType.ListProxy);

            _version++;

            if (_count == _buffer.Capacity)
            {
                AllocateMore();
            }

            CopyBuffer(index, index + 1, _count - index);
            ++_count;

            _buffer[index] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, in T item)
        {
            ThrowHelper.ThrowIfInsertionIndexIsOutOfRange((uint)index <= (uint)_count, ThrowHelper.CollectionType.ListProxy);

            _version++;

            if (_count == _buffer.Capacity)
            {
                AllocateMore();
            }

            CopyBuffer(index, index + 1, _count - index);
            ++_count;

            _buffer[index] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index)
        {
            ThrowHelper.ThrowIfIndexIsOutOfRange((uint)index < (uint)_count, ThrowHelper.CollectionType.ListProxy);
            return ref _buffer[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange([NotNull] T[] items)
            => AddRange(items, items.Length);

        public void AddRange([NotNull] T[] items, int count)
        {
            _version++;

            if (count == 0)
            {
                return;
            }

            if (_buffer.Capacity - _count < count)
            {
                AllocateMore(checked(_count + count));
            }

            items.AsSpan()[..count].CopyTo(_buffer.AsSpan().Slice(_count, count));
            _count += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ReadOnlySpan<T> items)
            => AddRange(items, items.Length);

        public void AddRange(ReadOnlySpan<T> items, int count)
        {
            _version++;

            if (count == 0)
            {
                return;
            }

            if (_buffer.Capacity - _count < count)
            {
                AllocateMore(checked(_count + count));
            }

            items[..count].CopyTo(_buffer.AsSpan().Slice(_count, count));
            _count += count;
        }

        public void AddRange([NotNull] IEnumerable<T> items)
        {
            switch (items)
            {
                case IReadOnlyList<T> list:
                    AddRangeFromReadOnlyList(list);
                    return;

                case IList<T> list:
                    AddRangeFromList(list);
                    return;

                case IReadOnlyIndexer<T> indexer when items is IHasCount || items is IHasLength:
                    AddRangeFromIndexer(items, indexer);
                    return;

                default:
                    AddRangeFromCollection(items);
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _version++;

            var shouldClear = false;
            ShouldClear(ref shouldClear);

            if (shouldClear)
            {
                ClearBuffer(0, _buffer.Capacity);
            }

            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] destination, int destinationIndex)
            => CopyTo(destination.AsSpan().Slice(destinationIndex, _count));

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
        public BufferProviderEnumerator<TProvider, TBuffer, T> GetEnumerator()
            => new(Provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
            => IncreaseCapacityTo(_buffer.Capacity + amount);

        public int IncreaseCapacityTo(int newCapacity)
        {
            _version++;

            if (newCapacity <= _buffer.Capacity)
            {
                return _buffer.Capacity;
            }

            _buffer.Resize(newCapacity, true);
            return _buffer.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Peek()
            => ref _buffer[_count - 1];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Pop()
        {
            _version++;
            --_count;
            return ref _buffer[_count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Push(T item)
        {
            Insert(_count, item);
            return _count - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Push(in T item)
        {
            Insert(_count, item);
            return _count - 1;
        }

        public void RemoveRange(int startIndex, int length)
        {
            var count = _count;

            ThrowIfStartIndexIsOutOfRange(startIndex < count);

            var end = startIndex + length;

            ThrowIfRemovalRangeIsOutOfRange(end <= count);

            _version++;

            if (length < 1)
            {
                return;
            }

            count = _count -= length;

            if (startIndex < count)
            {
                CopyBuffer(startIndex + length, startIndex, count - startIndex);
            }

            var shouldClear = false;
            ShouldClear(ref shouldClear);

            if (shouldClear)
            {
                ClearBuffer(count, length);
            }
        }

        public void RemoveAtSwapBack(int index)
        {
            ThrowIfSwapBackIndexIsOutOfRange(index < _count);

            _version++;

            var copyFrom = --_count;
            _buffer[index] = _buffer[copyFrom];

            var shouldClear = false;
            ShouldClear(ref shouldClear);

            if (shouldClear)
            {
                _buffer[copyFrom] = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
            => AsReadOnlySpan().ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            _version++;
            return _buffer.AsSpan()[.._count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
            => _buffer.AsSpan()[.._count];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trim()
        {
            _version++;

            if (_count < _buffer.Capacity)
            {
                _buffer.Resize(_count);
            }
        }

        public Span<T> AddReplicate(int amount)
        {
            _version++;

            var oldCount = _count;
            var newCount = amount + oldCount;
            var offset = newCount - _buffer.Capacity;

            if (offset > 0)
            {
                AllocateMore(newCount);
            }

            var buffer = _buffer.AsSpan().Slice(oldCount, amount);
            buffer.Fill(default);
            _count = newCount;

            return buffer;
        }

        public Span<T> AddReplicate(T value, int amount)
        {
            _version++;

            var oldCount = _count;
            var newCount = amount + oldCount;
            var offset = newCount - _buffer.Capacity;

            if (offset > 0)
            {
                AllocateMore(newCount);
            }

            var buffer = _buffer.AsSpan().Slice(oldCount, amount);
            buffer.Fill(value);
            _count = newCount;

            return buffer;
        }

        public Span<T> AddReplicateNoInit(int amount)
        {
            _version++;

            var oldCount = _count;
            var newCount = amount + oldCount;
            var offset = newCount - _buffer.Capacity;

            if (offset > 0)
            {
                AllocateMore(newCount);
            }

            var buffer = _buffer.AsSpan().Slice(oldCount, amount);
            _count = newCount;

            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ListProxy<TProvider, TBuffer, T> Prefill(
              [NotNull] TProvider provider
            , int amount
        )
        {
            var list = new ListProxy<TProvider, TBuffer, T>(provider, amount);
            list.AddReplicate(amount);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ListProxy<TProvider, TBuffer, T> Prefill(
              [NotNull] TProvider provider
            , T value
            , int amount
        )
        {
            var list = new ListProxy<TProvider, TBuffer, T>(provider, amount);
            list.AddReplicate(value, amount);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ListProxy<TProvider, TBuffer, T> PrefillNoInit(
              [NotNull] TProvider provider
            , int amount
        )
        {
            var list = new ListProxy<TProvider, TBuffer, T>(provider, amount);
            list.AddReplicateNoInit(amount);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CalcNewCapacity(int newSize)
        {
            newSize = Math.Max(4, newSize);
            return ((int)Math.Ceiling(newSize * 1.5f) / 4) * 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AllocateMore()
        {
            var newCapacity = CalcNewCapacity(_buffer.Capacity + 1);
            _buffer.Resize(newCapacity, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AllocateMore(int newSize)
        {
            ThrowHelper.ThrowIfNewCapacityIsInvalid(newSize > _buffer.Capacity);

            var newCapacity = CalcNewCapacity(newSize);
            _buffer.Resize(newCapacity, true);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfProviderIsNull([DoesNotReturnIf(false)] bool isInitialized)
        {
            if (isInitialized == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("ListProxy<TProvider, TBuffer, T> is not initialized");
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
                => new("index is outside the range of valid indexes for the ListProxy<TProvider, TBuffer, T>");
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
                => new("index is outside the range of valid indexes for the ListProxy<TProvider, TBuffer, T>");
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
        private static void ThrowIfSwapBackIndexIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
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
        internal void CopyBuffer(int sourceIndex, int destinationIndex, int length)
        {
            if (length < 1)
            {
                return;
            }

            var span = _buffer.AsSpan();
            span.Slice(sourceIndex, length).CopyTo(span.Slice(destinationIndex, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ClearBuffer(int index, int length)
        {
            if (length < 1)
            {
                return;
            }

            _buffer.AsSpan().Slice(index, length).Clear();
        }

#if UNITY_BURST
        [Unity.Burst.BurstDiscard]
#endif
        internal static void ShouldClear(ref bool result)
        {
            result = EncosyTypeExtensions.IsUnmanaged<T>() == false;
        }

        private void AddRangeFromReadOnlyList(IReadOnlyList<T> items)
        {
            var length = items.Count;

            if (length > 0)
            {
                if (_buffer.Capacity - _count < length)
                {
                    AllocateMore(checked(_count + length));
                }

                var span = _buffer.AsSpan().Slice(_count, length);

                for (int i = 0; i < length; i++)
                {
                    span[i] = items[i];
                }

                _count += length;
                _version++;
            }
        }

        private void AddRangeFromList(IList<T> items)
        {
            var length = items.Count;

            if (length > 0)
            {
                if (_buffer.Capacity - _count < length)
                {
                    AllocateMore(checked(_count + length));
                }

                var span = _buffer.AsSpan().Slice(_count, length);

                for (int i = 0; i < length; i++)
                {
                    span[i] = items[i];
                }

                _count += length;
                _version++;
            }
        }

        private void AddRangeFromIndexer(IEnumerable<T> items, IReadOnlyIndexer<T> indexer)
        {
            var length = 0;

            if (items is IHasCount hasCount)
            {
                length = hasCount.Count;
            }
            else if (items is IHasLength hasLength)
            {
                length = hasLength.Length;
            }

            if (length > 0)
            {
                if (_buffer.Capacity - _count < length)
                {
                    AllocateMore(checked(_count + length));
                }

                var span = _buffer.AsSpan().Slice(_count, length);

                for (int i = 0; i < length; i++)
                {
                    span[i] = indexer[i];
                }

                _count += length;
                _version++;
            }
        }

        private void AddRangeFromCollection(IEnumerable<T> items)
        {
            if (items is ICollection<T> c)
            {
                var count = c.Count;

                if (count > 0)
                {
                    if (_buffer.Capacity - _count < count)
                    {
                        AllocateMore(checked(_count + count));
                    }
                }
            }

            using IEnumerator<T> en = items.GetEnumerator();

            while (en.MoveNext())
            {
                Add(en.Current);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new BufferProviderEnumerator<TProvider, TBuffer, T>(Provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new BufferProviderEnumerator<TProvider, TBuffer, T>(Provider);
    }
}
