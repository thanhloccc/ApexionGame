// https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/Dictionaries/SveltoDictionary.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections
{
    partial class SharedArrayMap<TKey, TValue, TValueNative>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>The returned native view borrows the shared map allocation and must not outlive the map.</safety>
        public unsafe SharedArrayMapNative<TKey, TValueNative> AsNative()
        {
            // SAFETY: The returned native view borrows the shared map's live header and safety handle.
            unsafe
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                return new(_nativeData, _values.GetSafetyHandle());
#else
                return new(_nativeData);
#endif
            }
        }
    }

    /// <summary>
    /// A dictionary that stores its values in a contiguous array, so the values
    /// can be iterated directly as an array, without an enumerator.
    /// Most operations perform on par with <see cref="Dictionary{TKey, TValue}"/>.
    /// <br/>
    /// This is the native view over the storage of a
    /// <see cref="SharedArrayMap{TKey, TValue, TValueNative}"/>, usable in Burst and jobs.
    /// </summary>
    /// <remarks>
    /// <para>Not thread-safe.</para>
    /// <para>The capacity is fixed; only the owning
    /// <see cref="SharedArrayMap{TKey, TValue, TValueNative}"/> can grow the storage.</para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public readonly partial struct SharedArrayMapNative<TKey, TValue> : IClearable, IHasCapacity
        , IHasCount, ITryGetValue<TKey, TValue>, IIsCreated
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal readonly unsafe SharedArrayMapUnsafe<TKey, TValue>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        internal readonly AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        internal unsafe SharedArrayMapNative(
              SharedArrayMapUnsafe<TKey, TValue>* data
            , AtomicSafetyHandle safety
        )
        {
            // SAFETY: The constructor receives a borrowed live header from the owning shared map.
            unsafe
            {
                m_Data = data;
            }
            m_Safety = safety;
        }
#else
        internal unsafe SharedArrayMapNative(SharedArrayMapUnsafe<TKey, TValue>* data)
        {
            // SAFETY: The constructor receives a borrowed live header from the owning shared map.
            unsafe
            {
                m_Data = data;
            }
        }
#endif

#pragma warning disable IDE1006 // Naming Styles
        internal NativeArray<ArrayMapNode<TKey>> _valuesInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckExists();
                // SAFETY: CheckExists validates the shared map header and its live values-info storage.
                unsafe
                {
                    return CreateNativeArray(m_Data->_valuesInfo, m_Data->_valuesInfoCapacity);
                }
            }
        }

        internal NativeArray<TValue> _values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckExists();
                // SAFETY: CheckExists validates the shared map header and its live values storage.
                unsafe
                {
                    return CreateNativeArray(m_Data->_values, m_Data->_valuesCapacity);
                }
            }
        }

#pragma warning restore IDE1006 // Naming Styles

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: Reading the pointer field only observes whether the borrowed header exists.
                unsafe
                {
                    return m_Data != null;
                }
            }
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();

                // SAFETY: The read check validates the live map header.
                unsafe
                {
                    return m_Data->Capacity;
                }
            }
        }

        internal int BucketCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();

                // SAFETY: The read check validates the live map header.
                unsafe
                {
                    return m_Data->BucketCapacity;
                }
            }
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();

                // SAFETY: The read check validates the live map header.
                unsafe
                {
                    return m_Data->Count;
                }
            }
        }

        public KeyEnumerable Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(this);
        }

        public NativeSliceReadOnly<TValue> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();

                // SAFETY: The read check validates the header before attaching its safety handle to the view.
                unsafe
                {
                    return new NativeSliceReadOnly<TValue>(
                        CreateNativeArray(m_Data->_values, m_Data->Count).Slice()
                    );
                }
            }
        }

        internal int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();

                // SAFETY: The read check validates the live header before reading its version.
                unsafe
                {
                    return m_Data->Version;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe NativeArray<U> CreateNativeArray<U>(U* pointer, int length)
            where U : unmanaged
        {
            // SAFETY: The caller has checked the shared safety handle and supplies live bounded storage.
            unsafe
            {
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<U>(
                      pointer
                    , length
                    , Allocator.None
                );

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

                return array;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckExists()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
        }

        private readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        private readonly void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();

                // SAFETY: The read check validates the live header before keyed access.
                unsafe
                {
                    return (*m_Data)[key];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CheckWrite();

                // SAFETY: The write check validates the live header before keyed mutation.
                unsafe
                {
                    (*m_Data)[key] = value;
                }
            }
        }

        /// <remarks>
        /// The map must not be modified while it is being enumerated.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedArrayMapNativeKeyValueEnumerator<TKey, TValue> GetEnumerator()
            => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, in TValue value)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before insertion.
            unsafe
            {
                m_Data->Add(key, in value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, in TValue value)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before attempting insertion.
            unsafe
            {
                return m_Data->TryAdd(key, in value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, in TValue value, out int index)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before attempting indexed insertion.
            unsafe
            {
                return m_Data->TryAdd(key, in value, out index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before clearing it.
            unsafe
            {
                m_Data->Clear();
            }
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
            CheckRead();

            // SAFETY: The read check validates the live header before copying its complete storage state.
            unsafe
            {
                m_Data->CopyTo(
                      valuesInfo
                    , values
                    , buckets
                    , out count
                    , out collisions
                    , out fastModBucketsMultiplier
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            CheckRead();

            // SAFETY: The read check validates the live header before lookup.
            unsafe
            {
                return m_Data->ContainsKey(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out TValue result)
        {
            CheckRead();

            // SAFETY: The read check validates the live header before value lookup.
            unsafe
            {
                return m_Data->TryGetValue(key, out result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header for the returned mutable reference.
            unsafe
            {
                return ref m_Data->GetOrAdd(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key, out int index)
        {
            CheckWrite();

            // SAFETY: The write check validates the header for the returned indexed reference.
            unsafe
            {
                return ref m_Data->GetOrAdd(key, out index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetValueByRef(TKey key)
        {
            CheckWrite();

            // SAFETY: The write check validates the header for the returned mutable reference.
            unsafe
            {
                return ref m_Data->GetValueByRef(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before removal.
            unsafe
            {
                return m_Data->Remove(key);
            }
        }

        public bool Remove(TKey key, out int index, out TValue value)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before indexed removal.
            unsafe
            {
                return m_Data->Remove(key, out index, out value);
            }
        }

        // Indices are stored with an offset of 1 so that 0 represents a missing entry in the bucket list.
        //When read the offset must be offset by -1 again to be the real one. In this way
        // This avoids initializing the array to -1.

        //WARNING this method must stay stateless (not relying on states that can change, it's ok to read
        //constant states) because it will be used in multithreaded parallel code
        public bool TryFindIndex(TKey key, out int findIndex)
        {
            CheckRead();

            // SAFETY: The read check validates the live header before lookup.
            unsafe
            {
                return m_Data->TryFindIndex(key, out findIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(TKey key)
        {
            CheckRead();

            // SAFETY: The read check validates the live header before index lookup.
            unsafe
            {
                return m_Data->GetIndex(key);
            }
        }

        public void Intersect<UValue>(in SharedArrayMapNative<TKey, UValue> otherMapKeys)
            where UValue : unmanaged
        {
            CheckWrite();
            otherMapKeys.CheckRead();

            // SAFETY: Both safety checks validate the borrowed headers for the cross-map operation.
            unsafe
            {
                m_Data->Intersect(otherMapKeys.m_Data);
            }
        }

        public void Exclude<UValue>(in SharedArrayMapNative<TKey, UValue> otherMapKeys)
            where UValue : unmanaged
        {
            CheckWrite();
            otherMapKeys.CheckRead();

            // SAFETY: Both safety checks validate the borrowed headers for the cross-map operation.
            unsafe
            {
                m_Data->Exclude(otherMapKeys.m_Data);
            }
        }

        public void Union(in SharedArrayMapNative<TKey, TValue> otherMapKeys)
        {
            CheckWrite();
            otherMapKeys.CheckRead();

            // SAFETY: Both safety checks validate the borrowed headers before unioning their contents.
            unsafe
            {
                m_Data->Union(otherMapKeys.m_Data);
            }
        }

        public readonly struct KeyEnumerable
        {
            private readonly SharedArrayMapNative< TKey, TValue > _map;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public KeyEnumerable(in SharedArrayMapNative<TKey, TValue> map)
            {
                _map = map;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public KeyEnumerator GetEnumerator()
                => new(_map);
        }

        public struct KeyEnumerator : IIsValid
        {
            private readonly SharedArrayMapNative< TKey, TValue > _map;
            private readonly int _version;

            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public KeyEnumerator(in SharedArrayMapNative<TKey, TValue> map) : this()
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

                if (_index < _map.Count - 1)
                {
                    ++_index;
                    return true;
                }

                return false;
            }

            public readonly TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _map._valuesInfo.AsReadOnlySpan()[_index].key;
            }
        }
    }

    public struct SharedArrayMapNativeKeyValueEnumerator<TKey, TValue>
        : IEnumerator<SharedArrayMapNativeKeyValuePair<TKey, TValue>>, IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly SharedArrayMapNative<TKey, TValue> _map;
        private readonly int _version;

        private int _index;

        public SharedArrayMapNativeKeyValueEnumerator(SharedArrayMapNative<TKey, TValue> map) : this()
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

        public readonly SharedArrayMapNativeKeyValuePair<TKey, TValue> Current
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

    public readonly struct SharedArrayMapNativeKeyValuePair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly NativeArray<TValue> _mapValues;
        private readonly TKey _key;
        private readonly int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedArrayMapNativeKeyValuePair(in TKey key, NativeArray<TValue> mapValues, int index)
        {
            _mapValues = mapValues;
            _index = index;
            _key = key;
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
