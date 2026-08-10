// https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/Dictionaries/SveltoDictionary.cs

// MIT License
//
// Copyright (c) 2015-2020 Sebastiano Mandalà
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if !(UNITY_EDITOR || DEBUG || ENCOSY_RUNTIME_CHECKS || ENCOSY_COLLECTIONS_RUNTIME_CHECKS || ENABLE_UNITY_COLLECTIONS_CHECKS) || DISABLE_ENCOSY_CHECKS
#define __ENCOSY_NO_VALIDATION__
#else
#define __ENCOSY_VALIDATION__
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Buffers;
using EncosyTower.Common;
using EncosyTower.Logging;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(ArraySetUnsafeDebugProxy<>))]
    public partial struct ArraySetUnsafe<T> : IDisposable, IClearable, IIsCreated
        , IIncreaseCapacity, IHasCount
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged, IEquatable<T>
    {
        static ArraySetUnsafe()
        {
            NoBurstCheck();
        }

#if UNITY_BURST
        [Unity.Burst.BurstDiscard]
#endif
        static void NoBurstCheck()
        {
#if __ENCOSY_VALIDATION__
            try
            {
                var type = typeof(T);
                var method = type.GetMethod(
                      "GetHashCode"
                    , BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                );

                if (method == null)
                {
                    StaticDevLogger.LogWarning(
                          type.Name
                        + " does not implement GetHashCode and will potentially cause unwanted allocations (boxing)"
                    );
                }
            }
            catch
            {
            }
#endif
        }

        internal BufferUnsafe<ArrayMapNode<T>> _valuesInfo;
        internal BufferUnsafe<T> _values;
        internal BufferUnsafe<int> _buckets;

        internal ulong _fastModBucketsMultiplier;
        internal uint _collisions;
        internal int _freeValueCellIndex;
        internal int _version;

        internal AllocatorStrategy _allocator;

        public ArraySetUnsafe(int capacity, AllocatorStrategy allocator)
        {
            _valuesInfo = default;
            _valuesInfo.Alloc(capacity, allocator);
            _values = default;
            _values.Alloc(capacity, allocator);
            _buckets = default;
            _buckets.Alloc(HashHelpers.GetPrime(capacity), allocator);

            _freeValueCellIndex = 0;
            _version = 0;
            _collisions = 0;
            _fastModBucketsMultiplier = 0;
            _allocator = allocator;

            if (capacity > 0)
            {
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)_buckets.Capacity);
            }
        }

        public ArraySetUnsafe(ArraySetUnsafe<T> source, AllocatorStrategy allocator)
        {
            ThrowHelper.ThrowIfSourceCollectionIsNotCreated(
                source.IsCreated,
                ThrowHelper.CollectionType.ArraySetUnsafe
            );

            var capacity = source.Capacity;

            _valuesInfo = default;
            _valuesInfo.Alloc(capacity, allocator);
            _values = default;
            _values.Alloc(capacity, allocator);
            _buckets = default;
            // Buckets may have grown past GetPrime(capacity) via RecomputeBuckets;
            // the copied bucket data and _fastModBucketsMultiplier are only valid
            // for the exact source bucket count.
            _buckets.Alloc(source._buckets.Capacity, allocator);

            _freeValueCellIndex = 0;
            _version = 0;
            _collisions = 0;
            _fastModBucketsMultiplier = 0;
            _allocator = allocator;

            source._valuesInfo.AsSpan().CopyTo(_valuesInfo.AsSpan());
            source._values.AsSpan().CopyTo(_values.AsSpan());
            source._buckets.AsSpan().CopyTo(_buckets.AsSpan());

            _collisions = source._collisions;
            _fastModBucketsMultiplier = source._fastModBucketsMultiplier;
            _freeValueCellIndex = source._freeValueCellIndex;
        }

        private ArraySetUnsafe(
              BufferUnsafe<ArrayMapNode<T>> valuesInfo
            , BufferUnsafe<T> values
            , BufferUnsafe<int> buckets
            , ulong fastModBucketsMultiplier
            , uint collisions
            , int freeValueCellIndex
            , int version
            , AllocatorStrategy allocator
        )
        {
            _valuesInfo = valuesInfo;
            _values = values;
            _buckets = buckets;
            _fastModBucketsMultiplier = fastModBucketsMultiplier;
            _collisions = collisions;
            _freeValueCellIndex = freeValueCellIndex;
            _version = version;
            _allocator = allocator;
        }

        /// <summary>
        /// Allocates a heap header (the <see cref="ArraySetUnsafe{T}"/> struct itself) plus its
        /// buffers, and returns a pointer to it. Free it with
        /// <see cref="Free(ArraySetUnsafe{T}*)"/>.
        /// </summary>
        /// <safety>The returned set header is owned by allocator and must be freed exactly once with Free.</safety>
        public static unsafe ArraySetUnsafe<T>* Alloc(int capacity, AllocatorStrategy allocator)
        {
            // SAFETY: The allocator returns writable storage for the complete unmanaged struct.
            unsafe
            {
                var data = allocator.Allocate<ArraySetUnsafe<T>>();

                *data = new ArraySetUnsafe<T>(capacity, allocator);
                return data;
            }
        }

        /// <summary>
        /// Allocates a heap header seeded from an existing set (deep copy of the buffers),
        /// and returns a pointer to it. Free it with
        /// <see cref="Free(ArraySetUnsafe{T}*)"/>.
        /// </summary>
        /// <safety>The returned set header is owned by allocator and must be freed exactly once with Free.</safety>
        public static unsafe ArraySetUnsafe<T>* Alloc(in ArraySetUnsafe<T> source, AllocatorStrategy allocator)
        {
            // SAFETY: The allocator returns writable storage for the complete unmanaged struct.
            unsafe
            {
                var data = allocator.Allocate<ArraySetUnsafe<T>>();

                *data = new ArraySetUnsafe<T>(source, allocator);
                return data;
            }
        }

        /// <summary>
        /// Disposes the buffers of a header allocated by <see cref="Alloc(int, AllocatorStrategy)"/>
        /// and frees the header itself.
        /// </summary>
        /// <safety>data must be a live set header returned by Alloc and must not be used after this call.</safety>
        public static unsafe void Free(ArraySetUnsafe<T>* data)
        {
            // SAFETY: The pointer was allocated by this type's allocator and is freed once by its owner.
            unsafe
            {
                if (data == null)
                {
                    return;
                }

                var allocator = data->_allocator;
                data->Dispose();
                allocator.Free(data);
            }
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

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _valuesInfo.IsCreated && _values.IsCreated && _buckets.IsCreated;
        }

        public readonly ReadOnlySpan<T> Items
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.AsReadOnlySpan()[.._freeValueCellIndex];
        }

        public void Dispose()
        {
            if (_valuesInfo.IsCreated == false)
            {
                return;
            }

            _valuesInfo.Dispose();
            _values.Dispose();
            _buckets.Dispose();

            _freeValueCellIndex = 0;
            _version = 0;
            _collisions = 0;
            _fastModBucketsMultiplier = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (_valuesInfo.IsCreated == false)
            {
                return inputDeps;
            }

            inputDeps = _valuesInfo.Dispose(inputDeps);
            inputDeps = _values.Dispose(inputDeps);
            inputDeps = _buckets.Dispose(inputDeps);

            _freeValueCellIndex = 0;
            _version = 0;
            _collisions = 0;
            _fastModBucketsMultiplier = 0;

            return inputDeps;
        }

        /// <remarks>
        /// This returns readonly because the enumerator cannot be, but at the same time, it cannot be modified
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ArraySetUnsafeEnumerator<T> GetEnumerator()
            => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ArraySetUnsafe<U> Reinterpret<U>()
            where U : unmanaged, IEquatable<U>
        {
            return new ArraySetUnsafe<U>(
                  _valuesInfo.Reinterpret<ArrayMapNode<U>>()
                , _values.Reinterpret<U>()
                , _buckets
                , _fastModBucketsMultiplier
                , _collisions
                , _freeValueCellIndex
                , _version
                , _allocator
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(T value)
        {
            var itemAdded = AddValue(value, out var index);

            if (itemAdded)
            {
                _values[index] = value;
            }

            return itemAdded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T value)
        {
            var itemAdded = AddValue(value, out var index);

            if (itemAdded)
            {
                _values[index] = value;
            }

            return itemAdded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_freeValueCellIndex == 0)
            {
                return;
            }

            _version++;
            _freeValueCellIndex = 0;

            // Buckets cannot be FastCleared because it's important that the values are reset to 0
            _buckets.Clear();

            _values.FastClear();
            _valuesInfo.FastClear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(T value)
            => TryFindIndex(value, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(in T value)
            => TryFindIndex(value, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int size)
        {
            if (_values.Capacity < size)
            {
                var expandPrime = HashHelpers.ExpandPrime(size);

                _version++;
                _values.Resize(expandPrime, true, false);
                _valuesInfo.Resize(expandPrime);
            }

            return _values.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
            => EnsureCapacity(_values.Capacity + amount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityTo(int size)
            => EnsureCapacity(size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T value)
            => Remove(in value);

        public bool Remove(in T value)
        {
            var hash = value.GetHashCode();
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Capacity, _fastModBucketsMultiplier);

            //find the bucket
            var indexToValueToRemove = _buckets[bucketIndex] - 1;
            var itemAfterCurrentOne = -1;

            // Part one: find the key in the bucket list and update the list so that it does not
            //point anymore to the cell to remove
            while (indexToValueToRemove != -1)
            {
                ref var node = ref _valuesInfo[indexToValueToRemove];
                if (node._hashcode == hash && node.key.Equals(value))
                {
                    //if the key is found and the bucket points directly to the node to remove
                    if (_buckets[bucketIndex] - 1 == indexToValueToRemove)
                    {
                        //the bucket will point to the previous cell. if a previous cell exists
                        //its next pointer must be updated!
                        //<--- iteration order
                        //                      Bucket points always to the last one
                        //   ------- ------- -------
                        //   |  1  | |  2  | |  3  | //bucket cannot have next, only previous
                        //   ------- ------- -------
                        //--> insert order
                        _buckets[bucketIndex] = node._previous + 1;
                    }
                    else // The previous pointer must be updated when the removed element is not the last one.
                    {
                        ThrowIfMissingLinkedListNode(itemAfterCurrentOne != -1);
                        //update the previous pointer of the item after the one to remove with the
                        //previous pointer of the item to remove
                        _valuesInfo[itemAfterCurrentOne]._previous = node._previous;
                    }

                    break; // Stop here without updating indexToValueToRemove.
                }

                // A bucket always points to the last element of the list, so a missing item
                // requires backward iteration.
                itemAfterCurrentOne = indexToValueToRemove;
                indexToValueToRemove = node._previous;
            }

            if (indexToValueToRemove == -1)
            {
                return false; //not found!
            }

            _version++;
            _freeValueCellIndex--; //one less value to iterate

            //Part two:
            //At this point nodes pointers and buckets are updated, but the _values array
            //still contains the value to delete. The map must support
            //to iterate over the values like an array, so the values array must always be up to date

            // Removing the last cell requires fewer operations because no swap is needed.
            // Otherwise, the last value cell replaces the removed value.

            var lasTCellIndex = _freeValueCellIndex;
            if (indexToValueToRemove != lasTCellIndex)
            {
                // Transfer the last value of both arrays to the index of the value being removed.
                // The bucket pointer must be updated accordingly.
                // First, find the bucket-list index of the pointer to the cell.
                //to move
                ref var nodeToMove = ref _valuesInfo[lasTCellIndex];

                var movingBucketIndex = (int)Reduce(
                      (uint)nodeToMove._hashcode
                    , (uint)_buckets.Capacity
                    , _fastModBucketsMultiplier
                );

                var linkedListIterationIndex = _buckets[movingBucketIndex] - 1;

                //if the key is found and the bucket points directly to the node to remove
                //it must now point to the cell where it's going to be moved (update bucket list
                //first linked list node to iterate from)
                if (linkedListIterationIndex == lasTCellIndex)
                {
                    _buckets[movingBucketIndex] = indexToValueToRemove + 1;
                }

                //find the prev element of the last element in the valuesInfo array
                while (_valuesInfo[linkedListIterationIndex]._previous != -1
                    && _valuesInfo[linkedListIterationIndex]._previous != lasTCellIndex)
                {
                    linkedListIterationIndex = _valuesInfo[linkedListIterationIndex]._previous;
                }

                // Any value whose previous node is the last value cell must point to the replacement index.
                if (_valuesInfo[linkedListIterationIndex]._previous != -1)
                {
                    _valuesInfo[linkedListIterationIndex]._previous = indexToValueToRemove;
                }

                //finally, actually move the values
                _valuesInfo[indexToValueToRemove] = nodeToMove;
                _values[indexToValueToRemove] = _values[lasTCellIndex];
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trim()
        {
            var size = _freeValueCellIndex;

            _version++;
            _values.Resize(size);
            _valuesInfo.Resize(size);
        }

        // Indices are stored with an offset of 1 so that 0 represents a missing entry in the bucket list.
        //When read the offset must be offset by -1 again to be the real one. In this way
        // This avoids initializing the array to -1.

        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read
        //constant states) because it will be used in multithreaded parallel code
        private readonly bool TryFindIndex(in T value, out int index)
        {
            ThrowHelper.ThrowIfBucketsAreUninitialized(
                _buckets.Capacity > 0,
                ThrowHelper.CollectionType.ArraySetUnsafe
            );

            var hash = value.GetHashCode();
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Capacity, _fastModBucketsMultiplier);
            var valueIndex = _buckets[bucketIndex] - 1;

            // An existing value must still be checked against the requested key.
            while (valueIndex != -1)
            {
                //Comparer<T>.default needs to create a new comparer, so it is much slower
                //than assuming that Equals is implemented through IEquatable
                ref var node = ref _valuesInfo[valueIndex];
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

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfMissingLinkedListNode([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("The linked-list successor is missing.");
        }

        public void Intersect(in ArraySetUnsafe<T> otherSet)
        {
            var items = _valuesInfo.AsSpan();

            for (var i = Count - 1; i >= 0; i--)
            {
                var item = items[i].key;

                if (otherSet.Contains(item) == false)
                {
                    Remove(item);
                }
            }
        }

        public void Exclude(in ArraySetUnsafe<T> otherSet)
        {
            var items = Items;

            for (var i = Count - 1; i >= 0; i--)
            {
                var item = items[i];

                if (otherSet.Contains(item))
                {
                    Remove(item);
                }
            }
        }

        public void Union(in ArraySetUnsafe<T> otherSet)
        {
            foreach (var other in otherSet)
            {
                Add(other);
            }
        }

        internal bool AddValue(in T value, out int indexSet)
        {
            var hash = value.GetHashCode(); //IEquatable doesn't enforce the override of GetHashCode
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Capacity, _fastModBucketsMultiplier);

            //buckets value -1 means it's empty
            var valueIndex = _buckets[bucketIndex] - 1;
            var freeValueCellIndex = _freeValueCellIndex;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                //create the info node at the last position and fill it with the relevant information
                _valuesInfo[freeValueCellIndex] = new ArrayMapNode<T>(value, hash);
            }
            else //collision or already exists
            {
                var currenTIndex = valueIndex;
                do
                {
                    //must check if the key already exists in the map
                    //Comparer<T>.default needs to create a new comparer, so it is much slower
                    //than assuming that Equals is implemented through IEquatable (but what if
                    //the comparer is statically cached?)
                    ref var mode = ref _valuesInfo[currenTIndex];
                    if (mode._hashcode == hash && mode.key.Equals(value))
                    {
                        //the key already exists, simply replace the value!
                        indexSet = currenTIndex;
                        return false;
                    }

                    currenTIndex = mode._previous;
                } while (currenTIndex != -1); //-1 means no more values with key with the same hash

                ResizeIfNeeded();

                //oops collision!
                _collisions++;
                //create a new node which previous index points to node currently pointed in the bucket (valueIndex)
                //_freeValueCellIndex = valueIndex + 1
                _valuesInfo[freeValueCellIndex] = new ArrayMapNode<T>(value, hash, valueIndex);
                //Important: the new node is always the one that will be pointed by the bucket cell
                // Therefore, the bucket always points to the last value added.
            }

            _version++;

            //item with this bucketIndex will point to the last value created
            // TODO: If the original node is assumed to be the one in the bucket,
            // the bucket would not need to be updated here. This is a small but important optimization.
            _buckets[bucketIndex] = freeValueCellIndex + 1;

            indexSet = freeValueCellIndex;
            _freeValueCellIndex++;

            //too many collisions
            var collisions = _collisions;
            if (collisions > _buckets.Capacity)
            {
                if (_buckets.Capacity < 100)
                {
                    RecomputeBuckets((int)collisions << 1);
                }
                else
                {
                    RecomputeBuckets(HashHelpers.ExpandPrime((int)collisions));
                }
            }

            return true;
        }

        private void RecomputeBuckets(int newSize)
        {
            // More space is needed to reduce collisions.
            _buckets.Resize(newSize, false);
            _collisions = 0;
            _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)_buckets.Capacity);
            var bucketsCapacity = (uint)_buckets.Capacity;

            // Redistribute the hash codes of all stored values across the new buckets.
            //length
            var freeValueCellIndex = _freeValueCellIndex;
            var fastModBucketsMultiplier = _fastModBucketsMultiplier;
            var collisions = _collisions;

            for (var newValueIndex = 0; newValueIndex < freeValueCellIndex; ++newValueIndex)
            {
                //get the original hash code and find the new bucketIndex due to the new length
                ref var valueInfoNode = ref _valuesInfo[newValueIndex];
                var bucketIndex = (int)Reduce((uint)valueInfoNode._hashcode, bucketsCapacity, fastModBucketsMultiplier);
                //bucketsIndex can be -1 or a next value. If it's -1 means no collisions. If there is collision,
                // Create a new node whose previous pointer targets the old node, then point the
                // old node to the new one.
                //the bucket will now points to the new one
                // This rebuilds the linked list.
                //get the current valueIndex, it's -1 if no collision happens
                var existingValueIndex = _buckets[bucketIndex] - 1;
                //update the bucket index to the index of the current item that share the bucketIndex
                //(last found is always the one in the bucket)
                _buckets[bucketIndex] = newValueIndex + 1;
                if (existingValueIndex == -1)
                {
                    // Nothing was indexed because the bucket was empty, so the previous pointer must be updated.
                    //values of next and previous
                    valueInfoNode._previous = -1;
                }
                else
                {
                    //oops a value was already being pointed by this cell in the new bucket list,
                    //it means there is a collision, problem
                    collisions++;
                    //the bucket will point to this value, so
                    //the previous index will be used as previous for the new value.
                    valueInfoNode._previous = existingValueIndex;
                }
            }

            _collisions = collisions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeIfNeeded()
        {
            if (_freeValueCellIndex != _values.Capacity)
            {
                return;
            }

            var expandPrime = HashHelpers.ExpandPrime(_freeValueCellIndex);

            _values.Resize(expandPrime, true, false);
            _valuesInfo.Resize(expandPrime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Reduce(uint hashcode, uint N, ulong fastModBucketsMultiplier)
        {
            if (hashcode >= N) //is the condition return actually an optimization?
            {
                return Environment.Is64BitProcess
                ? HashHelpers.FastMod(hashcode, N, fastModBucketsMultiplier)
                : hashcode % N;
            }

            return hashcode;
        }

    }

    public struct ArraySetUnsafeEnumerator<T> : IEnumerator<T>, IIsValid
        where T : unmanaged, IEquatable<T>
    {
        private ArraySetUnsafe<T> _set;
        private readonly int _version;

        private int _index;

        public ArraySetUnsafeEnumerator(in ArraySetUnsafe<T> set) : this()
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
            get => _set._values[_index];
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

    internal sealed class ArraySetUnsafeDebugProxy<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly ArraySetUnsafe<T> _set;

        public ArraySetUnsafeDebugProxy(in ArraySetUnsafe<T> set)
        {
            _set = set;
        }

        public uint Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)_set.Count;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var set = _set;
                var array = new T[set.Count];
                var i = 0;

                foreach (var value in set)
                {
                    array[i++] = value;
                }

                return array;
            }
        }
    }
}
