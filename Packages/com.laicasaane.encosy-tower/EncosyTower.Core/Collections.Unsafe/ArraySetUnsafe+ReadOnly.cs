using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Common;

namespace EncosyTower.Collections.Unsafe
{
    partial struct ArraySetUnsafe<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
            => new(this);

        public readonly struct ReadOnly : IIsCreated, IHasCount, IHasCapacity
        {
            internal readonly BufferUnsafe<ArrayMapNode<T>>.ReadOnly _valuesInfo;
            internal readonly BufferUnsafe<T>.ReadOnly _values;
            internal readonly BufferUnsafe<int>.ReadOnly _buckets;

            internal readonly ulong _fastModBucketsMultiplier;
            internal readonly int _freeValueCellIndex;
            internal readonly int _version;

            public ReadOnly(ArraySetUnsafe<T> set)
            {
                _valuesInfo = set._valuesInfo.AsReadOnly();
                _values = set._values.AsReadOnly();
                _buckets = set._buckets.AsReadOnly();
                _fastModBucketsMultiplier = set._fastModBucketsMultiplier;
                _freeValueCellIndex = set._freeValueCellIndex;
                _version = set._version;
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _valuesInfo.IsCreated && _values.IsCreated && _buckets.IsCreated;
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _values.Capacity;
            }

            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _freeValueCellIndex;
            }

            public readonly ReadOnlySpan<T> Items
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _values.AsReadOnlySpan()[.._freeValueCellIndex];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ArraySetUnsafeReadOnlyEnumerator<T> GetEnumerator()
                => new(this);

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
                => new CopyToSpan<T>(Items).CopyTo(sourceStartIndex, destination, length);

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
                => new CopyToSpan<T>(Items).TryCopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool Contains(T value)
                => TryFindIndex(value, out _);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool Contains(in T value)
                => TryFindIndex(value, out _);

            // Indices are stored with an offset of 1 so that 0 represents a missing entry in the bucket list.
            //When read the offset must be offset by -1 again to be the real one. In this way
            // This avoids initializing the array to -1.

            //WARNING this method must stay stateless (not relying on states that can change, it's ok to read
            //constant states) because it will be used in multithreaded parallel code
            private readonly bool TryFindIndex(in T value, out int index)
            {
                ThrowHelper.ThrowIfBucketsAreUninitialized(
                    _buckets.Capacity > 0,
                    ThrowHelper.CollectionType.ArraySetUnsafeReadOnly
                );

                var hash = value.GetHashCode();
                var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Capacity, _fastModBucketsMultiplier);
                var valueIndex = _buckets[bucketIndex] - 1;

                // An existing value must still be checked against the requested key.
                while (valueIndex != -1)
                {
                    //Comparer<T>.default needs to create a new comparer, so it is much slower
                    //than assuming that Equals is implemented through IEquatable
                    ref readonly var node = ref _valuesInfo[valueIndex];
                    if (node._hashcode == hash && node.key.Equals(value))
                    {
                        //this is the one
                        index = valueIndex;
                        return true;
                    }

                    valueIndex = node._previous;
                }

                index = 0;
                return false;
            }

        }
    }

    public struct ArraySetUnsafeReadOnlyEnumerator<T> : IEnumerator<T>, IIsValid
        where T : unmanaged, IEquatable<T>
    {
        private readonly ArraySetUnsafe<T>.ReadOnly _set;
        private readonly int _version;

        private int _index;

        public ArraySetUnsafeReadOnlyEnumerator(in ArraySetUnsafe<T>.ReadOnly set) : this()
        {
            _set = set;
            _index = -1;
            _version = set._version;
        }

        public readonly bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _set.IsCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ThrowHelper.ThrowIfEnumeratorIsInvalid(IsValid);
            ThrowHelper.ThrowIfSetIsBeingIterated(_version == _set._version);

            if (_index < _set.Count - 1)
            {
                ++_index;
                return true;
            }

            return false;
        }

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _set._valuesInfo[_index].key;
        }

        readonly object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
        }
    }
}
