// https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/Dictionaries/SveltoDictionary.cs

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Common;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Unsafe
{
    internal struct SharedArrayMapUnsafe<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal unsafe ArrayMapNode<TKey>* _valuesInfo;
        internal int _valuesInfoCapacity;
        internal unsafe TValue* _values;
        internal int _valuesCapacity;
        internal unsafe int* _buckets;
        internal int _bucketsCapacity;

        internal unsafe ulong* _fastModBucketsMultiplier;
        internal unsafe uint* _collisions;
        internal unsafe int* _freeValueCellIndex;
        internal unsafe int* _version;

        internal static unsafe SharedArrayMapUnsafe<TKey, TValue>* Alloc(
              ArrayMapNode<TKey>* valuesInfo
            , int valuesInfoCapacity
            , TValue* values
            , int valuesCapacity
            , int* buckets
            , int bucketsCapacity
            , ulong* fastModBucketsMultiplier
            , uint* collisions
            , int* freeValueCellIndex
            , int* version
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The allocator returns writable storage for the complete unmanaged struct.
            unsafe
            {
                var data = allocator.Allocate<SharedArrayMapUnsafe<TKey, TValue>>();
                *data = new SharedArrayMapUnsafe<TKey, TValue> {
                    _valuesInfo = valuesInfo,
                    _valuesInfoCapacity = valuesInfoCapacity,
                    _values = values,
                    _valuesCapacity = valuesCapacity,
                    _buckets = buckets,
                    _bucketsCapacity = bucketsCapacity,
                    _fastModBucketsMultiplier = fastModBucketsMultiplier,
                    _collisions = collisions,
                    _freeValueCellIndex = freeValueCellIndex,
                    _version = version,
                };
                return data;
            }
        }

        internal static unsafe void Free(
              SharedArrayMapUnsafe<TKey, TValue>* data
            , AllocatorStrategy allocator
        )
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

        internal int Capacity
            => _valuesCapacity;

        internal int BucketCapacity
            => _bucketsCapacity;

        internal int Count
        {
            get
            {
                // SAFETY: The owning map keeps the count storage live while this header is borrowed.
                unsafe
                {
                    return *_freeValueCellIndex;
                }
            }
        }

        internal int Version
        {
            get
            {
                // SAFETY: The owning map keeps the version storage live while this header is borrowed.
                unsafe
                {
                    return *_version;
                }
            }
        }

        internal TKey KeyAt(int index)
            => GetValuesInfoSpan()[index].key;

        internal ref readonly TValue ValueAt(int index)
            => ref GetValuesSpan()[index];

        internal TValue this[TKey key]
        {
            get => GetValuesSpan()[GetIndex(key)];
            set
            {
                AddValue(key, out var index);
                GetValuesSpan()[index] = value;
            }
        }

        internal void Add(TKey key, in TValue value)
        {
            var itemAdded = AddValue(key, out var index);
            ThrowHelper.ThrowIfKeyIsPresent(itemAdded);

            if (itemAdded)
            {
                GetValuesSpan()[index] = value;
            }
        }

        internal bool TryAdd(TKey key, in TValue value)
            => TryAdd(key, in value, out _);

        internal bool TryAdd(TKey key, in TValue value, out int index)
        {
            var itemAdded = AddValue(key, out index);

            if (itemAdded)
            {
                GetValuesSpan()[index] = value;
            }

            return itemAdded;
        }

        internal void Clear()
        {
            if (Count == 0)
            {
                return;
            }

            IncrementVersion();
            SetCount(0);
            GetBucketsSpan().Clear();
        }

        internal void CopyTo(
              Span<ArrayMapNode<TKey>> valuesInfo
            , Span<TValue> values
            , Span<int> buckets
            , out int count
            , out uint collisions
            , out ulong fastModBucketsMultiplier
        )
        {
            GetValuesInfoSpan().CopyTo(valuesInfo);
            GetValuesSpan().CopyTo(values);
            GetBucketsSpan().CopyTo(buckets);
            count = Count;
            collisions = (uint)GetCollisions();
            fastModBucketsMultiplier = GetFastModBucketsMultiplier();
        }

        internal bool ContainsKey(TKey key)
            => TryFindIndex(key, out _);

        internal bool TryGetValue(TKey key, out TValue result)
        {
            if (TryFindIndex(key, out var findIndex))
            {
                result = GetValuesSpan()[findIndex];
                return true;
            }

            result = default;
            return false;
        }

        internal ref TValue GetOrAdd(TKey key)
        {
            var values = GetValuesSpan();

            if (TryFindIndex(key, out var findIndex))
            {
                IncrementVersion();
                return ref values[findIndex];
            }

            AddValue(key, out findIndex);
            values[findIndex] = default;
            return ref values[findIndex];
        }

        internal ref TValue GetOrAdd(TKey key, out int index)
        {
            var values = GetValuesSpan();

            if (TryFindIndex(key, out index))
            {
                IncrementVersion();
                return ref values[index];
            }

            AddValue(key, out index);
            return ref values[index];
        }

        internal ref TValue GetValueByRef(TKey key)
        {
            var found = TryFindIndex(key, out var findIndex);
            ThrowHelper.ThrowIfKeyIsNotFound(found);

            IncrementVersion();
            return ref GetValuesSpan()[findIndex];
        }

        internal bool Remove(TKey key)
            => Remove(key, out _, out _);

        internal bool Remove(TKey key, out int index, out TValue value)
        {
            var buckets = GetBucketsSpan();
            var valuesInfo = GetValuesInfoSpan();
            var values = GetValuesSpan();
            var fastModBucketsMultiplier = GetFastModBucketsMultiplier();

            var hash = key.GetHashCode();
            var bucketIndex = (int)Reduce(
                  (uint)hash
                , (uint)buckets.Length
                , fastModBucketsMultiplier
            );
            var indexToValueToRemove = buckets[bucketIndex] - 1;
            var itemAfterCurrentOne = -1;

            while (indexToValueToRemove != -1)
            {
                ref var node = ref valuesInfo[indexToValueToRemove];

                if (node._hashcode == hash && key.Equals(node.key))
                {
                    if (buckets[bucketIndex] - 1 == indexToValueToRemove)
                    {
                        buckets[bucketIndex] = node._previous + 1;
                    }
                    else
                    {
                        ThrowIfNextNodeIsMissing(itemAfterCurrentOne != -1);
                        valuesInfo[itemAfterCurrentOne]._previous = node._previous;
                    }

                    break;
                }

                itemAfterCurrentOne = indexToValueToRemove;
                indexToValueToRemove = node._previous;
            }

            if (indexToValueToRemove == -1)
            {
                index = default;
                value = default;
                return false;
            }

            IncrementVersion();
            index = indexToValueToRemove;

            var lastValueCellIndex = Count - 1;
            SetCount(lastValueCellIndex);
            value = values[indexToValueToRemove];

            if (indexToValueToRemove != lastValueCellIndex)
            {
                ref var nodeToMove = ref valuesInfo[lastValueCellIndex];
                var movingBucketIndex = (int)Reduce(
                      (uint)nodeToMove._hashcode
                    , (uint)buckets.Length
                    , fastModBucketsMultiplier
                );
                var linkedListIterationIndex = buckets[movingBucketIndex] - 1;

                if (linkedListIterationIndex == lastValueCellIndex)
                {
                    buckets[movingBucketIndex] = indexToValueToRemove + 1;
                }

                while (valuesInfo[linkedListIterationIndex]._previous != -1
                    && valuesInfo[linkedListIterationIndex]._previous != lastValueCellIndex)
                {
                    linkedListIterationIndex = valuesInfo[linkedListIterationIndex]._previous;
                }

                if (valuesInfo[linkedListIterationIndex]._previous != -1)
                {
                    valuesInfo[linkedListIterationIndex]._previous = indexToValueToRemove;
                }

                valuesInfo[indexToValueToRemove] = nodeToMove;
                values[indexToValueToRemove] = values[lastValueCellIndex];
            }

            return true;
        }

        internal bool TryFindIndex(TKey key, out int findIndex)
        {
            var buckets = GetBucketsSpan();
            var valuesInfo = GetValuesInfoSpan();

            ThrowHelper.ThrowIfBucketsAreUninitialized(
                  buckets.Length > 0
                , ThrowHelper.CollectionType.SharedArrayMapUnsafe
            );

            var hash = key.GetHashCode();
            var bucketIndex = (int)Reduce(
                  (uint)hash
                , (uint)buckets.Length
                , GetFastModBucketsMultiplier()
            );
            var valueIndex = buckets[bucketIndex] - 1;

            while (valueIndex != -1)
            {
                ref readonly var node = ref valuesInfo[valueIndex];

                if (node._hashcode == hash && key.Equals(node.key))
                {
                    findIndex = valueIndex;
                    return true;
                }

                valueIndex = node._previous;
            }

            findIndex = 0;
            return false;
        }

        internal int GetIndex(TKey key)
        {
            var found = TryFindIndex(key, out var findIndex);
            ThrowHelper.ThrowIfKeyIsNotFound(found);
            return findIndex;
        }

        internal unsafe void Intersect<UValue>(SharedArrayMapUnsafe<TKey, UValue>* otherMapKeys)
            where UValue : unmanaged
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var key = GetValuesInfoSpan()[i].key;

                // SAFETY: The caller supplies a live borrowed header for the other map.
                unsafe
                {
                    if (otherMapKeys->ContainsKey(key) == false)
                    {
                        Remove(key);
                    }
                }
            }
        }

        internal unsafe void Exclude<UValue>(SharedArrayMapUnsafe<TKey, UValue>* otherMapKeys)
            where UValue : unmanaged
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var key = GetValuesInfoSpan()[i].key;

                // SAFETY: The caller supplies a live borrowed header for the other map.
                unsafe
                {
                    if (otherMapKeys->ContainsKey(key))
                    {
                        Remove(key);
                    }
                }
            }
        }

        internal unsafe void Union(SharedArrayMapUnsafe<TKey, TValue>* otherMap)
        {
            // SAFETY: The caller supplies a live borrowed header whose occupied range is contiguous.
            unsafe
            {
                var count = otherMap->Count;
                for (var i = 0; i < count; i++)
                {
                    this[otherMap->KeyAt(i)] = otherMap->ValueAt(i);
                }
            }
        }

        private bool AddValue(TKey key, out int indexSet)
        {
            var valuesInfo = GetValuesInfoSpan();
            var buckets = GetBucketsSpan();
            var count = Count;
            var collisions = GetCollisions();
            var hash = key.GetHashCode();
            var bucketIndex = (int)Reduce(
                  (uint)hash
                , (uint)buckets.Length
                , GetFastModBucketsMultiplier()
            );
            var valueIndex = buckets[bucketIndex] - 1;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                valuesInfo[count] = new ArrayMapNode<TKey>(key, hash);
            }
            else
            {
                var currentValueIndex = valueIndex;

                do
                {
                    ref var node = ref valuesInfo[currentValueIndex];

                    if (node._hashcode == hash && key.Equals(node.key))
                    {
                        indexSet = currentValueIndex;
                        return false;
                    }

                    currentValueIndex = node._previous;
                } while (currentValueIndex != -1);

                ResizeIfNeeded();
                collisions++;
                valuesInfo[count] = new ArrayMapNode<TKey>(key, hash, valueIndex);
            }

            IncrementVersion();
            buckets[bucketIndex] = count + 1;
            indexSet = count;
            SetCount(count + 1);
            SetCollisions(collisions);

            if (collisions > buckets.Length)
            {
                RecomputeBuckets();
            }

            return true;
        }

        private void RecomputeBuckets()
        {
            var valuesInfo = GetValuesInfoSpan();
            var buckets = GetBucketsSpan();
            buckets.Clear();

            var collisions = 0;
            var bucketsCapacity = (uint)buckets.Length;
            var fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier(bucketsCapacity);
            SetFastModBucketsMultiplier(fastModBucketsMultiplier);

            var count = Count;

            for (var newValueIndex = 0; newValueIndex < count; ++newValueIndex)
            {
                ref var valueInfoNode = ref valuesInfo[newValueIndex];
                var bucketIndex = (int)Reduce(
                      (uint)valueInfoNode._hashcode
                    , bucketsCapacity
                    , fastModBucketsMultiplier
                );
                var existingValueIndex = buckets[bucketIndex] - 1;
                buckets[bucketIndex] = newValueIndex + 1;

                if (existingValueIndex == -1)
                {
                    valueInfoNode._previous = -1;
                }
                else
                {
                    collisions++;
                    valueInfoNode._previous = existingValueIndex;
                }
            }

            SetCollisions(collisions);
        }

        private void ResizeIfNeeded()
        {
            ThrowHelper.ThrowIfCapacityIsImmutable(
                  Count != _valuesCapacity
                , ThrowHelper.CollectionType.SharedArrayMapUnsafe
            );
        }

        private Span<ArrayMapNode<TKey>> GetValuesInfoSpan()
        {
            // SAFETY: The owner pins a values-info buffer matching the recorded capacity.
            unsafe
            {
                return new Span<ArrayMapNode<TKey>>(_valuesInfo, _valuesInfoCapacity);
            }
        }

        private Span<TValue> GetValuesSpan()
        {
            // SAFETY: The owner pins a values buffer matching the recorded capacity.
            unsafe
            {
                return new Span<TValue>(_values, _valuesCapacity);
            }
        }

        private Span<int> GetBucketsSpan()
        {
            // SAFETY: The owner pins a bucket buffer matching the recorded capacity.
            unsafe
            {
                return new Span<int>(_buckets, _bucketsCapacity);
            }
        }

        private ulong GetFastModBucketsMultiplier()
        {
            // SAFETY: The owner keeps the multiplier storage live while the header is borrowed.
            unsafe
            {
                return *_fastModBucketsMultiplier;
            }
        }

        private void SetFastModBucketsMultiplier(ulong value)
        {
            // SAFETY: The owner keeps the multiplier storage live while the header is borrowed.
            unsafe
            {
                *_fastModBucketsMultiplier = value;
            }
        }

        private int GetCollisions()
        {
            // SAFETY: The owner keeps the collision storage live while the header is borrowed.
            unsafe
            {
                return (int)*_collisions;
            }
        }

        private void SetCollisions(int value)
        {
            // SAFETY: The owner keeps the collision storage live while the header is borrowed.
            unsafe
            {
                *_collisions = (uint)value;
            }
        }

        private void SetCount(int count)
        {
            // SAFETY: The owner keeps the count storage live while the header is borrowed.
            unsafe
            {
                *_freeValueCellIndex = count;
            }
        }

        private void IncrementVersion()
        {
            // SAFETY: The owner keeps the version storage live while the header is borrowed.
            unsafe
            {
                (*_version)++;
            }
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfNextNodeIsMissing([DoesNotReturnIf(false)] bool hasNextNode)
        {
            if (hasNextNode == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("This should never happen");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Reduce(uint hashcode, uint length, ulong fastModBucketsMultiplier)
        {
            if (hashcode >= length)
            {
                return Environment.Is64BitProcess
                    ? HashHelpers.FastMod(hashcode, length, fastModBucketsMultiplier)
                    : hashcode % length;
            }

            return hashcode;
        }
    }
}
