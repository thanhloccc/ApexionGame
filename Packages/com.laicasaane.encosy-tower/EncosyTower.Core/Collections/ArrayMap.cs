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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Common;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    /// <summary>
    /// A dictionary that stores its values in a contiguous array, so the values
    /// can be iterated directly as an array, without an enumerator.
    /// Most operations perform on par with <see cref="Dictionary{TKey, TValue}"/>.
    /// Growing on add is slower, because two internal arrays must be resized.
    /// </summary>
    /// <remarks>
    /// Not thread-safe.
    /// </remarks>
    [DebuggerTypeProxy(typeof(ArrayMapDebugProxy<,>))]
    public partial class ArrayMap<TKey, TValue> : IDisposable
        , ICollection<ArrayMapKeyValuePair<TKey, TValue>>
        , IReadOnlyCollection<ArrayMapKeyValuePair<TKey, TValue>>
        , IClearable, IIncreaseCapacity, IHasCount, ITryGetValue<TKey, TValue>
    {
        internal BufferManaged<ArrayMapNode<TKey>> _valuesInfo;
        internal BufferManaged<TValue> _values;
        internal BufferManaged<int> _buckets;

        internal ulong _fastModBucketsMultiplier;
        internal uint _collisions;
        internal int _freeValueCellIndex;
        internal int _version;

        public ArrayMap() : this(0)
        {
        }

        public ArrayMap(int capacity)
        {
            _version = default;
            _valuesInfo = default;
            _valuesInfo.Alloc(capacity);
            _values = default;
            _values.Alloc(capacity);
            _buckets = default;
            _buckets.Alloc(HashHelpers.GetPrime(capacity));

            if (capacity > 0)
            {
                _fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint)_buckets.Capacity);
            }
        }

        public ArrayMap([NotNull] ArrayMap<TKey, TValue> source)
        {
            var capacity = source.Capacity;

            _version = default;
            _valuesInfo = default;
            _valuesInfo.Alloc(capacity);
            _values = default;
            _values.Alloc(capacity);
            _buckets = default;
            // Buckets may have grown past GetPrime(capacity) via RecomputeBuckets;
            // the copied bucket data and _fastModBucketsMultiplier are only valid
            // for the exact source bucket count.
            _buckets.Alloc(source._buckets.Capacity);

            source._valuesInfo.AsSpan().CopyTo(_valuesInfo.AsSpan());
            source._values.AsSpan().CopyTo(_values.AsSpan());
            source._buckets.AsSpan().CopyTo(_buckets.AsSpan());

            _freeValueCellIndex = source._freeValueCellIndex;
            _collisions = source._collisions;
            _fastModBucketsMultiplier = source._fastModBucketsMultiplier;
        }

        public ArrayMap(ReadOnly source) : this(source._map)
        {
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.Capacity;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _freeValueCellIndex;
        }

        public KeyEnumerable Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(this);
        }

        public ReadOnlyMemory<TValue> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.AsArraySegment().Slice(0, _freeValueCellIndex);
        }

        bool ICollection<ArrayMapKeyValuePair<TKey, TValue>>.IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[FindIndex(key)];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                AddValue(key, out var index);

                _values[index] = value;
            }
        }

        public void Dispose()
        {
            if (_valuesInfo.IsCreated == false)
            {
                return;
            }

            _version++;
            _valuesInfo.Dispose();
            _values.Dispose();
            _buckets.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrayMapKeyValueEnumerator<TKey, TValue> GetEnumerator()
            => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, in TValue value)
        {
            var itemAdded = AddValue(key, out var index);

            ThrowHelper.ThrowIfKeyIsPresent(itemAdded);

            if (itemAdded)
            {
                _values[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, in TValue value)
        {
            var itemAdded = AddValue(key, out var index);

            if (itemAdded)
            {
                _values[index] = value;
            }

            return itemAdded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, in TValue value, out int index)
        {
            var itemAdded = AddValue(key, out index);

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
        public bool ContainsKey(TKey key)
        {
            return TryFindIndex(key, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out TValue result)
        {
            if (TryFindIndex(key, out var index))
            {
                result = _values[index];
                return true;
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key)
        {
            if (TryFindIndex(key, out var index))
            {
                _version++;
                return ref _values[index];
            }

            AddValue(key, out index);

            _values[index] = default;

            return ref _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key, Func<TValue> builder)
        {
            if (TryFindIndex(key, out var index))
            {
                _version++;
                return ref _values[index];
            }

            AddValue(key, out index);

            _values[index] = builder();

            return ref _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key, out int index)
        {
            if (TryFindIndex(key, out index))
            {
                _version++;
                return ref _values[index];
            }

            AddValue(key, out index);

            return ref _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd<TParam>(TKey key, FuncRef<TParam, TValue> builder, ref TParam parameter)
        {
            if (TryFindIndex(key, out var index))
            {
                _version++;
                return ref _values[index];
            }

            AddValue(key, out index);

            _values[index] = builder(ref parameter);

            return ref _values[index];
        }

        /// <summary>
        /// Gets the value for <paramref name="key"/>, or adds an entry when the key is not present.
        /// Intended for maps that are fast-cleared and store class values: when the new
        /// slot still holds an object from before the clear, that object is recycled
        /// instead of creating a new one.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="builder">Creates a new value when there is no object to recycle.</param>
        /// <param name="recycler">Resets a leftover object before it is reused.</param>
        /// <typeparam name="TValueProxy">The concrete class type stored as the value.</typeparam>
        /// <returns>A reference to the value for <paramref name="key"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue RecycleOrAdd<TValueProxy>(
              TKey key
            , Func<TValueProxy> builder
            , ActionRef<TValueProxy> recycler
        )
            where TValueProxy : class, TValue
        {
            if (TryFindIndex(key, out var index))
            {
                _version++;
                return ref _values[index];
            }

            AddValue(key, out index);

            if (_values[index] == null)
            {
                _values[index] = builder();
            }
            else
            {
                recycler(ref UnsafeUtility.As<TValue, TValueProxy>(ref _values[index]));
            }

            return ref _values[index];
        }

        /// <summary>
        /// Gets the value for <paramref name="key"/>, or adds an entry when the key is not present.
        /// Intended for maps that are fast-cleared and store class values: when the new
        /// slot still holds an object from before the clear, that object is recycled
        /// instead of creating a new one.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="builder">Creates a new value when there is no object to recycle.</param>
        /// <param name="recycler">Resets a leftover object before it is reused.</param>
        /// <param name="parameter">State passed by reference to <paramref name="builder"/>
        /// and <paramref name="recycler"/>.</param>
        /// <typeparam name="TValueProxy">The concrete class type stored as the value.</typeparam>
        /// <typeparam name="TParam">The type of <paramref name="parameter"/>.</typeparam>
        /// <returns>A reference to the value for <paramref name="key"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue RecycleOrAdd<TValueProxy, TParam>(
              TKey key
            , FuncRef<TParam, TValue> builder
            , ActionRef<TValueProxy, TParam> recycler
            , ref TParam parameter
        )
            where TValueProxy : class, TValue
        {
            if (TryFindIndex(key, out var index))
            {
                _version++;
                return ref _values[index];
            }

            AddValue(key, out index);

            if (_values[index] == null)
            {
                _values[index] = builder(ref parameter);
            }
            else
            {
                recycler(ref UnsafeUtility.As<TValue, TValueProxy>(ref _values[index]), ref parameter);
            }

            return ref _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetValueByRef(TKey key)
        {
            var found = TryFindIndex(key, out var index);

            ThrowHelper.ThrowIfKeyIsNotFound(found);

            _version++;
            return ref _values[index];
        }

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
        public bool Remove(TKey key)
        {
            return Remove(key, out _, out _);
        }

        public bool Remove(TKey key, out int index, out TValue value)
        {
            var hash = key.GetHashCode();
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Capacity, _fastModBucketsMultiplier);

            //find the bucket
            var indexToValueToRemove = _buckets[bucketIndex] - 1;
            var itemAfterCurrentOne = -1;
            var comparer = EqualityComparer<TKey>.Default;

            // Part one: find the key in the bucket list and update the list so that it does not
            //point anymore to the cell to remove
            while (indexToValueToRemove != -1)
            {
                ref var node = ref _valuesInfo[indexToValueToRemove];
                if (node._hashcode == hash && comparer.Equals(node.key, key))
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
                index = default;
                value = default;
                return false; //not found!
            }

            _version++;
            index = indexToValueToRemove; // The out parameter exposes the index of the element to remove.

            _freeValueCellIndex--; //one less value to iterate
            value = _values[indexToValueToRemove]; // The out parameter exposes the value of the element to remove.

            //Part two:
            //At this point nodes pointers and buckets are updated, but the _values array
            //still contains the value to delete. The map must support
            //to iterate over the values like an array, so the values array must always be up to date

            // Removing the last cell requires fewer operations because no swap is needed.
            // Otherwise, the last value cell replaces the removed value.

            var lastValueCellIndex = _freeValueCellIndex;
            if (indexToValueToRemove != lastValueCellIndex)
            {
                // Transfer the last value of both arrays to the index of the value being removed.
                // The bucket pointer must be updated accordingly.
                // First, find the bucket-list index of the pointer to the cell.
                //to move
                ref var modeToMove = ref _valuesInfo[lastValueCellIndex];

                var movingBucketIndex = (int)Reduce(
                      (uint)modeToMove._hashcode
                    , (uint)_buckets.Capacity
                    , _fastModBucketsMultiplier
                );

                var linkedListIterationIndex = _buckets[movingBucketIndex] - 1;

                //if the key is found and the bucket points directly to the node to remove
                //it must now point to the cell where it's going to be moved (update bucket list
                //first linked list node to iterate from)
                if (linkedListIterationIndex == lastValueCellIndex)
                {
                    _buckets[movingBucketIndex] = indexToValueToRemove + 1;
                }

                //find the prev element of the last element in the valuesInfo array
                while (_valuesInfo[linkedListIterationIndex]._previous != -1
                    && _valuesInfo[linkedListIterationIndex]._previous != lastValueCellIndex)
                {
                    linkedListIterationIndex = _valuesInfo[linkedListIterationIndex]._previous;
                }

                // Any value whose previous node is the last value cell must point to the replacement index.
                if (_valuesInfo[linkedListIterationIndex]._previous != -1)
                {
                    _valuesInfo[linkedListIterationIndex]._previous = indexToValueToRemove;
                }

                //finally, actually move the values
                _valuesInfo[indexToValueToRemove] = modeToMove;
                _values[indexToValueToRemove] = _values[lastValueCellIndex];
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
        public bool TryFindIndex(TKey key, out int index)
        {
            ThrowHelper.ThrowIfBucketsAreUninitialized(
                _buckets.Capacity > 0,
                ThrowHelper.CollectionType.ArrayMap
            );

            var hash = key.GetHashCode();
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Capacity, _fastModBucketsMultiplier);
            var valueIndex = _buckets[bucketIndex] - 1;
            var comparer = EqualityComparer<TKey>.Default;

            // An existing value must still be checked against the requested key.
            while (valueIndex != -1)
            {
                ref var node = ref _valuesInfo[valueIndex];
                if (node._hashcode == hash && comparer.Equals(node.key, key))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindIndex(TKey key)
        {
            var found = TryFindIndex(key, out var index);
            return found ? index : -1;
        }

        public void Intersect<UValue>([NotNull] ArrayMap<TKey, UValue> otherMapKeys)
        {
            var keys = _valuesInfo.AsSpan();

            for (var i = Count - 1; i >= 0; i--)
            {
                var tKey = keys[i].key;

                if (otherMapKeys.ContainsKey(tKey) == false)
                {
                    Remove(tKey);
                }
            }
        }

        public void Exclude<UValue>([NotNull] ArrayMap<TKey, UValue> otherMapKeys)
        {
            var keys = _valuesInfo.AsSpan();

            for (var i = Count - 1; i >= 0; i--)
            {
                var tKey = keys[i].key;

                if (otherMapKeys.ContainsKey(tKey))
                {
                    Remove(tKey);
                }
            }
        }

        public void Union([NotNull] ArrayMap<TKey, TValue> otherMapKeys)
        {
            foreach (var other in otherMapKeys)
            {
                this[other.Key] = other.Value;
            }
        }

        private bool AddValue(TKey key, out int indexSet)
        {
            var hash = key.GetHashCode(); //IEquatable doesn't enforce the override of GetHashCode
            var bucketIndex = (int)Reduce((uint)hash, (uint)_buckets.Capacity, _fastModBucketsMultiplier);

            //buckets value -1 means it's empty
            var valueIndex = _buckets[bucketIndex] - 1;

            if (valueIndex == -1)
            {
                ResizeIfNeeded();
                //create the info node at the last position and fill it with the relevant information
                _valuesInfo[_freeValueCellIndex] = new ArrayMapNode<TKey>(key, hash);
            }
            else //collision or already exists
            {
                var currentValueIndex = valueIndex;
                var comparer = EqualityComparer<TKey>.Default;

                do
                {
                    //must check if the key already exists in the map
                    ref var node = ref _valuesInfo[currentValueIndex];
                    if (node._hashcode == hash && comparer.Equals(node.key, key))
                    {
                        //the key already exists, simply replace the value!
                        indexSet = currentValueIndex;
                        return false;
                    }

                    currentValueIndex = node._previous;
                } while (currentValueIndex != -1); //-1 means no more values with key with the same hash

                ResizeIfNeeded();

                //oops collision!
                _collisions++;
                //create a new node which previous index points to node currently pointed in the bucket (valueIndex)
                //_freeValueCellIndex = valueIndex + 1
                _valuesInfo[_freeValueCellIndex] = new ArrayMapNode<TKey>(key, hash, valueIndex);
                //Important: the new node is always the one that will be pointed by the bucket cell
                // Therefore, the bucket always points to the last value added.
            }

            _version++;

            //item with this bucketIndex will point to the last value created
            // TODO: If the original node is assumed to be the one in the bucket,
            // the bucket would not need to be updated here. This is a small but important optimization.
            _buckets[bucketIndex] = _freeValueCellIndex + 1;

            indexSet = _freeValueCellIndex;
            _freeValueCellIndex++;

            //too many collisions
            if (_collisions > _buckets.Capacity)
            {
                if (_buckets.Capacity < 100)
                {
                    RecomputeBuckets((int)_collisions << 1);
                }
                else
                {
                    RecomputeBuckets(HashHelpers.ExpandPrime((int)_collisions));
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
            for (var newValueIndex = 0; newValueIndex < freeValueCellIndex; ++newValueIndex)
            {
                //get the original hash code and find the new bucketIndex due to the new length
                ref var valueInfoNode = ref _valuesInfo[newValueIndex];
                var bucketIndex = (int)Reduce(
                      (uint)valueInfoNode._hashcode
                    , bucketsCapacity
                    , _fastModBucketsMultiplier
                );
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
                    _collisions++;
                    //the bucket will point to this value, so
                    //the previous index will be used as previous for the new value.
                    valueInfoNode._previous = existingValueIndex;
                }
            }
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
        private static uint Reduce(uint hashcode, uint N, ulong fastModBucketsMultiplier)
        {
            if (hashcode >= N) //is the condition return actually an optimization?
            {
                return Environment.Is64BitProcess
                ? HashHelpers.FastMod(hashcode, N, fastModBucketsMultiplier)
                : hashcode % N;
            }

            return hashcode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<ArrayMapKeyValuePair<TKey, TValue>> IEnumerable<ArrayMapKeyValuePair<TKey, TValue>>.GetEnumerator()
            => new ArrayMapKeyValueEnumerator<TKey, TValue>(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => new ArrayMapKeyValueEnumerator<TKey, TValue>(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<ArrayMapKeyValuePair<TKey, TValue>>.Add(ArrayMapKeyValuePair<TKey, TValue> item)
        {
            TryAdd(item.Key, item.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<ArrayMapKeyValuePair<TKey, TValue>>.Contains(ArrayMapKeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<ArrayMapKeyValuePair<TKey, TValue>>.CopyTo(
              ArrayMapKeyValuePair<TKey, TValue>[] array
            , int arrayIndex
        )
        {
            ThrowNotImplementedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack, StackTraceHidden, DoesNotReturn]
        private static void ThrowNotImplementedException()
            => throw new NotImplementedException("This method is not implemented by design.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICollection<ArrayMapKeyValuePair<TKey, TValue>>.Remove(ArrayMapKeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public readonly struct KeyEnumerable : IEnumerable<TKey>, IIsValid
        {
            private readonly ArrayMap<TKey, TValue> _map;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public KeyEnumerable([NotNull] ArrayMap<TKey, TValue> map)
            {
                _map = map;
            }

            public bool IsValid
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _map != null;
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
            private readonly ArrayMap<TKey, TValue> _map;
            private readonly int _version;

            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public KeyEnumerator([NotNull] ArrayMap<TKey, TValue> map) : this()
            {
                _map = map;
                _index = -1;
                _version = map._version;
            }

            public readonly bool IsValid
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _map != null;
            }

            public readonly TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _map._valuesInfo[_index].key;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                ThrowHelper.ThrowIfEnumeratorIsInvalid(IsValid);
                ThrowHelper.ThrowIfMapIsBeingIterated(_version == _map._version);

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
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Current;
            }
        }
    }

    public struct ArrayMapKeyValueEnumerator<TKey, TValue> : IEnumerator<ArrayMapKeyValuePair<TKey, TValue>>, IIsValid
    {
        private readonly ArrayMap<TKey, TValue> _map;
        private readonly int _version;

        private int _index;

        public ArrayMapKeyValueEnumerator([NotNull] ArrayMap<TKey, TValue> map) : this()
        {
            _map = map;
            _index = -1;
            _version = map._version;
        }

        public readonly bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _map != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ThrowHelper.ThrowIfEnumeratorIsInvalid(IsValid);
            ThrowHelper.ThrowIfMapIsBeingIterated(_version == _map._version);

            if (_index >= _map.Count - 1)
            {
                return false;
            }

            ++_index;
            return true;
        }

        public readonly ArrayMapKeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_map._valuesInfo[_index].key, _map._values, _index);
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

    [DebuggerDisplay("[{Key}] = {Value}")]
    [DebuggerTypeProxy(typeof(ArrayMapKeyValuePairDebugProxy<,>))]
    public readonly struct ArrayMapKeyValuePair<TKey, TValue> : IIsValid
    {
        private readonly BufferManaged<TValue> _mapValues;
        private readonly TKey _key;
        private readonly int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrayMapKeyValuePair(in TKey key, in BufferManaged<TValue> mapValues, int index)
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
            get => ref _mapValues[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }
    }

    internal sealed class ArrayMapKeyValuePairDebugProxy<TKey, TValue>
    {

        private readonly ArrayMapKeyValuePair<TKey, TValue> _keyValue;

        public ArrayMapKeyValuePairDebugProxy(in ArrayMapKeyValuePair<TKey, TValue> keyValue)
        {
            _keyValue = keyValue;
        }

        public TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _keyValue.Key;
        }

        public TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _keyValue.Value;
        }
    }

    internal sealed class ArrayMapDebugProxy<TKey, TValue>
    {

        private readonly ArrayMap<TKey, TValue> _map;

        public ArrayMapDebugProxy([NotNull] ArrayMap<TKey, TValue> map)
        {
            _map = map;
        }

        public uint Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)_map.Count;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ArrayMapKeyValuePair<TKey, TValue>[] KeyValues
        {
            get
            {
                var map = _map;
                var array = new ArrayMapKeyValuePair<TKey, TValue>[map.Count];
                var i = 0;

                foreach (var keyValue in map)
                {
                    array[i++] = keyValue;
                }

                return array;
            }
        }
    }
}
