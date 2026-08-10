using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Collections.Extensions;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections
{
    partial class SharedArrayMap<TKey, TValue, TValueNative>
    {
        partial struct ReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            /// <safety>The returned native view borrows the shared map allocation and must not
            /// outlive the map.</safety>
            public unsafe SharedArrayMapNative<TKey, TValueNative>.ReadOnly AsNative()
            {
                // SAFETY: The returned native view borrows the shared map's live header and safety handle.
                unsafe
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                    return new(_nativeData, _nativeSafety);
#else
                    return new(_nativeData);
#endif
                }
            }

        }
    }

    partial struct SharedArrayMapNative<TKey, TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnly AsReadOnly()
        {
            // SAFETY: The returned view borrows this shared map's live header and safety handle.
            unsafe
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                return new(m_Data, m_Safety);
#else
                return new(m_Data);
#endif
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public readonly struct ReadOnly : IHasCapacity, IHasCount, ITryGetValue<TKey, TValue>, IIsCreated
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe SharedArrayMapUnsafe<TKey, TValue>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            internal readonly AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

            internal unsafe ReadOnly(
                  SharedArrayMapUnsafe<TKey, TValue>* data
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                , AtomicSafetyHandle safety
#endif
            )
            {
                // SAFETY: The constructor receives a borrowed live header from the owning shared map.
                unsafe
                {
                    m_Data = data;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                m_Safety = safety;
#endif
            }

#pragma warning disable IDE1006 // Naming Styles
            internal NativeArray<ArrayMapNode<TKey>>.ReadOnly _valuesInfo
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the shared map header and its live values-info storage.
                    unsafe
                    {
                        return CreateNativeArray(m_Data->_valuesInfo, m_Data->_valuesInfoCapacity).AsReadOnly();
                    }
                }
            }

            internal NativeArray<TValue>.ReadOnly _values
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the shared map header and its live values storage.
                    unsafe
                    {
                        return CreateNativeArray(m_Data->_values, m_Data->_valuesCapacity).AsReadOnly();
                    }
                }
            }

#pragma warning restore IDE1006 // Naming Styles

            public readonly bool IsCreated
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

            public readonly int Capacity
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

            public readonly int Count
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

            public readonly KeyEnumerable Keys
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(this);
            }

            public readonly NativeSliceReadOnly<TValue> Values
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _values.Slice(0, Count);
            }

            public readonly int Version
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
                // SAFETY: The caller has checked the shared safety handle and supplies the live bounded storage.
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
            private void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
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
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(SharedArrayMapNative<TKey, TValue> map)
                => map.AsReadOnly();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly SharedArrayMapNativeReadOnlyKeyValueEnumerator<TKey, TValue> GetEnumerator()
                => new(this);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool ContainsKey(TKey key)
            {
                CheckRead();

                // SAFETY: The read check validates the live header before lookup.
                unsafe
                {
                    return m_Data->ContainsKey(key);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryGetValue(TKey key, out TValue result)
            {
                CheckRead();

                // SAFETY: The read check validates the live header before value lookup.
                unsafe
                {
                    return m_Data->TryGetValue(key, out result);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly int GetIndex(TKey key)
            {
                CheckRead();

                // SAFETY: The read check validates the live header before index lookup.
                unsafe
                {
                    return m_Data->GetIndex(key);
                }
            }

            // Indices are stored with an offset of 1 so that 0 represents a missing entry in the bucket list.
            //When read the offset must be offset by -1 again to be the real one. In this way
            // This avoids initializing the array to -1.

            //WARNING this method must stay stateless (not relying on states that can change, it's ok to read
            //constant states) because it will be used in multithreaded parallel code
            public readonly bool TryFindIndex(TKey key, out int findIndex)
            {
                CheckRead();

                // SAFETY: The read check validates the live header before lookup.
                unsafe
                {
                    return m_Data->TryFindIndex(key, out findIndex);
                }
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

    public struct SharedArrayMapNativeReadOnlyKeyValueEnumerator<TKey, TValue>
        : IEnumerator<SharedArrayMapNativeReadOnlyKeyValuePair<TKey, TValue>>, IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly SharedArrayMapNative<TKey, TValue>.ReadOnly _map;
        private readonly int _version;

        private int _index;

        public SharedArrayMapNativeReadOnlyKeyValueEnumerator(
            in SharedArrayMapNative<TKey, TValue>.ReadOnly map
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

        public readonly SharedArrayMapNativeReadOnlyKeyValuePair<TKey, TValue> Current
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

    public readonly struct SharedArrayMapNativeReadOnlyKeyValuePair<TKey, TValue> : IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly NativeArray<TValue>.ReadOnly _mapValues;
        private readonly TKey _key;
        private readonly int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedArrayMapNativeReadOnlyKeyValuePair(in TKey key, NativeArray<TValue>.ReadOnly mapValues, int index)
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
