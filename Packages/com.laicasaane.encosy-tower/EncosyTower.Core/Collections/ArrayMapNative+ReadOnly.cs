using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections
{
    partial struct ArrayMapNative<TKey, TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
            => new(this);

        public readonly struct ReadOnly : IIsCreated, IHasCount, IHasCapacity, ITryGetValue<TKey, TValue>
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe ArrayMapUnsafe<TKey, TValue>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;

#if UNITY_BURST
            private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
                = Unity.Burst.SharedStatic<int>.GetOrCreate<ReadOnly>();
#else
            private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnly(ArrayMapNative<TKey, TValue> map)
            {
                // SAFETY: The read-only wrapper copies the validated source map's owned native header pointer.
                unsafe
                {
                    m_Data = map.m_Data;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = map.m_Safety;

                EncosyCollectionSafetyAPI.SetStaticSafetyId<ReadOnly>(
                      ref m_Safety
#if UNITY_BURST
                    , ref s_SafetyId.Data
#else
                    , ref s_SafetyId
#endif
                );
#endif
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: The native view carries the owner's safety handle and only reads its live header.
                    unsafe
                    {
                        return m_Data != null && m_Data->IsCreated;
                    }
                }
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    // SAFETY: CheckRead validated the owner before reading the native header.
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    // SAFETY: CheckRead validated the owner before reading the native header.
                    unsafe
                    {
                        return m_Data->Count;
                    }
                }
            }

            public readonly KeyEnumerable Keys
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return new(this);
                }
            }

            public readonly NativeSliceReadOnly<TValue> Values
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    // SAFETY: CheckRead validated the owner and the native values buffer is live for this view.
                    unsafe
                    {
                        return new NativeSlice<TValue>(ValuesNativeArray(), 0, m_Data->Count);
                    }
                }
            }

            public TValue this[TKey key]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    // SAFETY: CheckRead validated the owner; the map performs its own key lookup.
                    unsafe
                    {
                        return (*m_Data)[key];
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly NativeArray<TValue> ValuesNativeArray()
            {
                // SAFETY: CheckRead is performed by every public caller; the view is bounded by
                // the native map capacity.
                unsafe
                {
                    var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TValue>(
                          m_Data->_values.GetUnsafePtr()
                        , m_Data->Capacity
                        , Allocator.None
                    );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

                    return array;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ArrayMapNativeReadOnlyKeyValueEnumerator<TKey, TValue> GetEnumerator()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return new(this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool ContainsKey(TKey key)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner before forwarding to the native map.
                unsafe
                {
                    return m_Data->ContainsKey(key);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryGetValue(TKey key, out TValue result)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner before forwarding to the native map.
                unsafe
                {
                    return m_Data->TryGetValue(key, out result);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref readonly TValue GetValueByRef(TKey key)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner; the returned ref is bounded by the map's live values buffer.
                unsafe
                {
                    var found = m_Data->TryFindIndex(key, out var index);

                    // Burst is not able to vectorise code if throw is found, regardless if it's actually ever thrown
                    ThrowHelper.ThrowIfKeyIsNotFound(found);

                    return ref m_Data->_values[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly int FindIndex(TKey key)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner before forwarding to the native map.
                unsafe
                {
                    return m_Data->FindIndex(key);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryFindIndex(TKey key, out int index)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner before forwarding to the native map.
                unsafe
                {
                    return m_Data->TryFindIndex(key, out index);
                }
            }

            internal readonly int UncheckedVersion
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: Internal callers use the native view while its owner is alive.
                    unsafe
                    {
                        return m_Data->_version;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly TKey KeyAt(int index)
            {
                // SAFETY: Enumerator validation keeps the index within the live map.
                unsafe
                {
                    return m_Data->_valuesInfo[index].key;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly ReadOnlySpan<TValue> AsValuesReadOnlySpan()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner and the slice is bounded by the map count.
                unsafe
                {
                    return m_Data->_values.AsReadOnlySpan()[..m_Data->Count];
                }
            }
        }
    }

    public struct ArrayMapNativeReadOnlyKeyValueEnumerator<TKey, TValue>
        : IEnumerator<ArrayMapNativeReadOnlyKeyValuePair<TKey, TValue>>, IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly ArrayMapNative<TKey, TValue>.ReadOnly _map;
        private readonly int _version;

        private int _index;

        public ArrayMapNativeReadOnlyKeyValueEnumerator(in ArrayMapNative<TKey, TValue>.ReadOnly map) : this()
        {
            _map = map;
            _index = -1;
            _version = map.UncheckedVersion;
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
            ThrowHelper.ThrowIfMapIsBeingIterated(_version == _map.UncheckedVersion);

            if (_index < _map.Count - 1)
            {
                ++_index;
                return true;
            }

            return false;
        }

        public readonly ArrayMapNativeReadOnlyKeyValuePair<TKey, TValue> Current
        {
            get
            {
                // SAFETY: Enumerator validation keeps the pair attached to the live native map.
                unsafe
                {
                    return new(_map.KeyAt(_index), _map.m_Data, _index);
                }
            }
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

    public readonly struct ArrayMapNativeReadOnlyKeyValuePair<TKey, TValue> : IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
            private readonly unsafe ArrayMapUnsafe<TKey, TValue>* _map;

        private readonly TKey _key;
        private readonly int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe ArrayMapNativeReadOnlyKeyValuePair(in TKey key, ArrayMapUnsafe<TKey, TValue>* map, int index)
        {
            _map = map;
            _index = index;
            _key = key;
        }

        public bool IsValid
        {
            get
            {
                // SAFETY: The pair is created only from a live native map enumerator.
                unsafe
                {
                    return _map != null && _map->IsCreated;
                }
            }
        }

        public TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _key;
        }

        public TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: The enumerator supplies an index within the live map values buffer.
                unsafe
                {
                    return _map->_values[_index];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }
    }
}
