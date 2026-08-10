using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Extensions;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    partial class SharedArrayMap<TKey, TValue, TValueNative>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnly AsReadOnly()
            => new(this);

        public readonly partial struct ReadOnly : IHasCapacity, IHasCount, ITryGetValue<TKey, TValue>, IIsCreated
        {
            private static readonly SharedArrayMap<TKey, TValue, TValueNative> s_emptyOwner = new();
            private static readonly ReadOnly s_empty = new(s_emptyOwner);

            internal readonly NativeArray<ArrayMapNode<TKey>>.ReadOnly _valuesInfo;
            internal readonly NativeArray<TValue>.ReadOnly _values;
            internal readonly NativeArray<int>.ReadOnly _buckets;

            internal readonly NativeArray<ulong>.ReadOnly _fastModBucketsMultiplier;
            internal readonly NativeArray<uint>.ReadOnly _collisions;
            internal readonly NativeArray<int>.ReadOnly _freeValueCellIndex;
            internal readonly NativeArray<int>.ReadOnly _version;
            internal readonly unsafe SharedArrayMapUnsafe<TKey, TValueNative>* _nativeData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            internal readonly AtomicSafetyHandle _nativeSafety;
#endif

            public ReadOnly(SharedArrayMap<TKey, TValue, TValueNative> map)
            {
                _valuesInfo = map._valuesInfo.AsNativeArray().AsReadOnly();
                _values = map._values.AsNativeArray().Reinterpret<TValue>().AsReadOnly();
                _buckets = map._buckets.AsNativeArray().AsReadOnly();
                _freeValueCellIndex = map._freeValueCellIndex.AsNativeArray().AsReadOnly();
                _version = map._version.AsNativeArray().AsReadOnly();
                _collisions = map._collisions.AsNativeArray().AsReadOnly();
                _fastModBucketsMultiplier = map._fastModBucketsMultiplier.AsNativeArray().AsReadOnly();
                // SAFETY: The read-only view borrows the live map header without taking ownership.
                unsafe
                {
                    _nativeData = map._nativeData;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                _nativeSafety = map._values.GetSafetyHandle();
#endif
            }

            public static ReadOnly Empty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => s_empty;
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _valuesInfo.IsCreated && _values.IsCreated && _buckets.IsCreated;
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _values.Length;
            }

            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _freeValueCellIndex[0];
            }

            public readonly KeyEnumerable Keys
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(this);
            }

            public readonly NativeSliceReadOnly<TValue> Values
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _values.Slice(0, _freeValueCellIndex[0]);
            }

            internal readonly int Version
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _version[0];
            }

            public TValue this[TKey key]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _values[GetIndex(key)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(SharedArrayMap<TKey, TValue, TValueNative> map)
                => map is not null ? map.AsReadOnly() : Empty;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly SharedArrayMapReadOnlyKeyValueEnumerator<TKey, TValue, TValueNative> GetEnumerator()
                => new(this);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool ContainsKey(TKey key)
            {
                return TryFindIndex(key, out _);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryGetValue(TKey key, out TValue result)
            {
                if (TryFindIndex(key, out var findIndex))
                {
                    result = _values[findIndex];
                    return true;
                }

                result = default;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly int GetIndex(TKey key)
            {
                var found = TryFindIndex(key, out var findIndex);

                //Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
                ThrowHelper.ThrowIfKeyIsNotFound(found);

                return findIndex;
            }

            // Indices are stored with an offset of 1 so that 0 represents a missing entry in the bucket list.
            //When read the offset must be offset by -1 again to be the real one. In this way
            // This avoids initializing the array to -1.

            //WARNING this method must stay stateless (not relying on states that can change, it's ok to read
            //constant states) because it will be used in multithreaded parallel code
            public readonly bool TryFindIndex(TKey key, out int findIndex)
            {
                ThrowIfBucketsAreNotInitialized(_buckets.Length > 0);

                var hash = key.GetHashCode();
                var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Length, _fastModBucketsMultiplier[0]);
                var valueIndex = _buckets[bucketIndex] - 1;

                // An existing value must still be checked against the requested key.
                while (valueIndex != -1)
                {
                    //Comparer<TKey>.default needs to create a new comparer, so it is much slower
                    //than assuming that Equals is implemented through IEquatable
                    var node = _valuesInfo[valueIndex];

                    if (node._hashcode == hash && node.key.Equals(key))
                    {
                        //this is the one
                        findIndex = valueIndex;
                        return true;
                    }

                    valueIndex = node._previous;
                }

                findIndex = 0;
                return false;
            }

            [HideInCallstack, StackTraceHidden]
            [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
            [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
            [Conditional(UNITY_COLLECTIONS_CHECKS)]
            private static void ThrowIfBucketsAreNotInitialized([DoesNotReturnIf(false)] bool areInitialized)
            {
                if (areInitialized == false)
                {
                    throw CreateException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                static InvalidOperationException CreateException()
                    => new("Map arrays are not correctly initialized (0 size)");
            }

            public readonly struct KeyEnumerable : IEnumerable<TKey>, IIsValid
            {
                private readonly ReadOnly _map;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public KeyEnumerable(in ReadOnly map)
                {
                    _map = map;
                }

                public bool IsValid
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _map.IsCreated;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public KeyEnumerator GetEnumerator()
                    => new(_map);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
                    => GetEnumerator();

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                IEnumerator IEnumerable.GetEnumerator()
                    => GetEnumerator();
            }

            public struct KeyEnumerator : IEnumerator<TKey>, IIsValid
            {
                private readonly ReadOnly _map;
                private readonly int _version;

                private int _index;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public KeyEnumerator(in ReadOnly map) : this()
                {
                    _map = map;
                    _index = -1;
                    _version = map.Version;
                }

                public readonly bool IsValid
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _map.IsCreated;
                }

                public readonly TKey Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _map._valuesInfo.AsReadOnlySpan()[_index].key;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    ThrowHelper.ThrowIfEnumeratorIsInvalid(IsValid);
                    ThrowHelper.ThrowIfMapIsBeingIterated(_version == _map.Version);

                    if (_index < _map.Count - 1)
                    {
                        ++_index;
                        return true;
                    }

                    return false;
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

                readonly object IEnumerator.Current
                    => Current;
            }
        }
    }

    public struct SharedArrayMapReadOnlyKeyValueEnumerator<TKey, TValue, TValueNative>
        : IEnumerator<SharedArrayMapReadOnlyKeyValuePair<TKey, TValue, TValueNative>>, IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TValueNative : unmanaged
    {
        private readonly SharedArrayMap<TKey, TValue, TValueNative>.ReadOnly _map;
        private readonly int _version;

        private int _index;

        public SharedArrayMapReadOnlyKeyValueEnumerator(
            in SharedArrayMap<TKey, TValue, TValueNative>.ReadOnly map
        ) : this()
        {
            _map = map;
            _index = -1;
            _version = map.Version;
        }

        public readonly bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _map.IsCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ThrowHelper.ThrowIfEnumeratorIsInvalid(IsValid);
            ThrowHelper.ThrowIfMapIsBeingIterated(_version == _map.Version);

            if (_index >= _map.Count - 1)
            {
                return false;
            }

            ++_index;
            return true;
        }

        public readonly SharedArrayMapReadOnlyKeyValuePair<TKey, TValue, TValueNative> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_map._valuesInfo.AsReadOnlySpan()[_index].key, _map._values, _index);
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

    public readonly struct SharedArrayMapReadOnlyKeyValuePair<TKey, TValue, TValueNative> : IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TValueNative : unmanaged
    {
        private readonly NativeArray<TValue>.ReadOnly _mapValues;
        private readonly TKey _key;
        private readonly int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedArrayMapReadOnlyKeyValuePair(in TKey key, NativeArray<TValue>.ReadOnly mapValues, int index)
        {
            _mapValues = mapValues;
            _index = index;
            _key = key;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _mapValues.IsCreated;
        }

        public TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _key;
        }

        public readonly ref readonly TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _mapValues.AsReadOnlySpan()[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }
    }
}
