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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace EncosyTower.Collections
{
    /// <summary>
    /// A dictionary that stores its values in a contiguous array, so the values
    /// can be iterated directly as an array, without an enumerator.
    /// Most operations perform on par with <see cref="Dictionary{TKey, TValue}"/>.
    /// Growing on add is slower, because two internal arrays must be resized.
    /// <br/>
    /// This is a thin, safety-checked wrapper around a natively allocated
    /// <see cref="ArrayMapUnsafe{TKey, TValue}"/>. Every operation performs one
    /// <see cref="AtomicSafetyHandle"/> check, then forwards to the unsafe map.
    /// </summary>
    /// <remarks>
    /// Not thread-safe.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(ArrayMapNativeDebugProxy<,>))]
    public partial struct ArrayMapNative<TKey, TValue> : IDisposable, IClearable, IIsCreated
        , IIncreaseCapacity, IHasCount
        , ITryGetValue<TKey, TValue>
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe ArrayMapUnsafe<TKey, TValue>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
            = Unity.Burst.SharedStatic<int>.GetOrCreate<ArrayMapNative<TKey, TValue>>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        public ArrayMapNative(int capacity, AllocatorStrategy allocator) : this()
        {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data = ArrayMapUnsafe<TKey, TValue>.Alloc(capacity, allocator);
                CreateSafety(allocator.ToAllocator());
            }
        }

        public ArrayMapNative(ArrayMapNative<TKey, TValue> source, AllocatorStrategy allocator) : this()
        {
            ThrowHelper.ThrowIfNativeSourceCollectionIsNotCreated(
                source.IsCreated,
                ThrowHelper.CollectionType.ArrayMapNative
            );

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data = ArrayMapUnsafe<TKey, TValue>.Alloc(*source.m_Data, allocator);
                CreateSafety(allocator.ToAllocator());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateSafety(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = EncosyCollectionSafetyAPI.CreateSafetyHandle(allocator);

            EncosyCollectionSafetyAPI.SetStaticSafetyId<ArrayMapNative<TKey, TValue>>(
                  ref m_Safety
#if UNITY_BURST
                , ref s_SafetyId.Data
#else
                , ref s_SafetyId
#endif
            );

            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
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
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
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
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
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
                return new(AsReadOnly());
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
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
                unsafe
                {
                    return new NativeSlice<TValue>(ValuesNativeArray(), 0, m_Data->Count);
                }
            }
        }

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
                unsafe
                {
                    return (*m_Data)[key];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
                unsafe
                {
                    (*m_Data)[key] = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly NativeArray<TValue> ValuesNativeArray()
        {
            // SAFETY: The caller has validated the native container and the view is bounded by the map capacity.
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

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif

            if (IsCreated == false)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                ArrayMapNativeDispose.Dispose(m_Data);
                m_Data = null;
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif

            if (IsCreated == false)
            {
                return inputDeps;
            }

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                var jobHandle = new ArrayMapNativeDisposeJob {
                    _data = new ArrayMapNativeDispose {
                        m_Data = (ArrayMapUnsafe<int, int>*)m_Data,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        m_Safety = m_Safety,
#endif
                    }
                }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                EncosyCollectionSafetyAPI.ReleaseSafetyHandleAfterSchedule(ref m_Safety);
#endif

                m_Data = null;

                return jobHandle;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ArrayMapNativeKeyValueEnumerator<TKey, TValue> GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return new(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ArrayMapNative<TKey, UValue> Reinterpret<UValue>()
            where UValue : unmanaged
        {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                var result = default(ArrayMapNative<TKey, UValue>);
                result.m_Data = (ArrayMapUnsafe<TKey, UValue>*)m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                result.m_Safety = m_Safety;

                EncosyCollectionSafetyAPI.SetStaticSafetyId<ArrayMapNative<TKey, UValue>>(
                      ref result.m_Safety
#if UNITY_BURST
                    , ref ArrayMapNative<TKey, UValue>.s_SafetyId.Data
#else
                    , ref ArrayMapNative<TKey, UValue>.s_SafetyId
#endif
                );

#endif

                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, in TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data->Add(key, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, in TValue value, out int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return m_Data->TryAdd(key, value, out index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Recycle()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data->Recycle();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data->Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsKey(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
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

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return m_Data->TryGetValue(key, out result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return ref m_Data->GetOrAdd(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAdd(TKey key, out int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return ref m_Data->GetOrAdd(key, out index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetValueByRef(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return ref m_Data->GetValueByRef(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int size)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return m_Data->EnsureCapacity(size);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
            => EnsureCapacity(Capacity + amount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityTo(int size)
            => EnsureCapacity(size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
            => Remove(key, out _, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key, out int index, out TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return m_Data->Remove(key, out index, out value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trim()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data->Trim();
            }
        }

        public readonly bool TryFindIndex(TKey key, out int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return m_Data->TryFindIndex(key, out index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int FindIndex(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return m_Data->FindIndex(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Intersect<UValue>(in ArrayMapNative<TKey, UValue> otherMapKeys)
            where UValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(otherMapKeys.m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data->Intersect(in *otherMapKeys.m_Data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exclude<UValue>(in ArrayMapNative<TKey, UValue> otherMapKeys)
            where UValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(otherMapKeys.m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data->Exclude(in *otherMapKeys.m_Data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Union(in ArrayMapNative<TKey, TValue> otherMapKeys)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(otherMapKeys.m_Safety);
#endif

            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                m_Data->Union(in *otherMapKeys.m_Data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool AddValue(TKey key, out int indexSet)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: CheckWrite validated the owner before forwarding to the native map.
            unsafe
            {
                return m_Data->AddValue(key, out indexSet);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe readonly ref TValue GetValueRefAt(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: CheckWrite validated the owner and the ref is used within the native view lifetime.
            unsafe
            {
                return ref m_Data->_values[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void BumpVersion()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before mutating the native version counter.
            unsafe
            {
                m_Data->_version++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe readonly Span<TValue> AsValuesSpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            // SAFETY: CheckWrite validated the owner and the span is bounded by the native map count.
            unsafe
            {
                return m_Data->_values.AsSpan()[..m_Data->Count];
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
                _version = map.UncheckedVersion;
            }

            public readonly bool IsValid
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _map.IsCreated;
            }

            public readonly TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _map.KeyAt(_index);
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

    public struct ArrayMapNativeKeyValueEnumerator<TKey, TValue>
        : IEnumerator<ArrayMapNativeKeyValuePair<TKey, TValue>>, IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private ArrayMapNative<TKey, TValue> _map;
        private readonly int _version;

        private int _index;

        public ArrayMapNativeKeyValueEnumerator(in ArrayMapNative<TKey, TValue> map) : this()
        {
            _map = map;
            _index = -1;

            // SAFETY: The enumerator is constructed from a live, safety-checked native map.
            unsafe
            {
                _version = map.m_Data->_version;
            }
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

            // SAFETY: IsValid and the version check keep the native map header alive during enumeration.
            unsafe
            {
                ThrowHelper.ThrowIfMapIsBeingIterated(_version == _map.m_Data->_version);
            }

            // SAFETY: IsValid keeps the native map header alive during enumeration.
            unsafe
            {
                if (_index < _map.m_Data->Count - 1)
                {
                    ++_index;
                    return true;
                }

                return false;
            }
        }

        public readonly ArrayMapNativeKeyValuePair<TKey, TValue> Current
        {
            get
            {
                // SAFETY: Enumerator validation keeps the index inside the live map values-info buffer.
                unsafe
                {
                    return new(_map.m_Data->_valuesInfo[_index].key, _map.m_Data, _index);
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

    [DebuggerDisplay("[{Key}] = {Value}")]
    [DebuggerTypeProxy(typeof(ArrayMapNativeKeyValuePairDebugProxy<,>))]
    public readonly struct ArrayMapNativeKeyValuePair<TKey, TValue> : IIsValid
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly unsafe ArrayMapUnsafe<TKey, TValue>* _map;

        private readonly TKey _key;
        private readonly int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe ArrayMapNativeKeyValuePair(in TKey key, ArrayMapUnsafe<TKey, TValue>* map, int index)
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

        public ref readonly TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: Enumerator validation keeps the index inside the live map values buffer.
                unsafe
                {
                    return ref _map->_values[_index];
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

#if UNITY_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct ArrayMapNativeDisposeJob : IJob
    {
        internal ArrayMapNativeDispose _data;

        public readonly void Execute()
            => _data.Dispose();
    }

    [NativeContainer]
    internal struct ArrayMapNativeDispose
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe ArrayMapUnsafe<int, int>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        public readonly void Dispose()
        {
            // SAFETY: The scheduled dispose job owns the header and releases it exactly once.
            unsafe
            {
                Dispose(m_Data);
            }
        }

        /// <safety>data must be a live map header returned by the matching allocator and must not
        /// be used after this call.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Dispose<TKey, TValue>(ArrayMapUnsafe<TKey, TValue>* data)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // SAFETY: data is a live map header and ArrayMapUnsafe.Free releases it through its owning allocator.
            unsafe
            {
                if (data == null)
                {
                    return;
                }

                ArrayMapUnsafe<TKey, TValue>.Free(data);
            }
        }
    }

    internal sealed class ArrayMapNativeKeyValuePairDebugProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly ArrayMapNativeKeyValuePair<TKey, TValue> _keyValue;

        public ArrayMapNativeKeyValuePairDebugProxy(in ArrayMapNativeKeyValuePair<TKey, TValue> keyValue)
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

    internal sealed class ArrayMapNativeDebugProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly ArrayMapNative<TKey, TValue> _map;

        public ArrayMapNativeDebugProxy(in ArrayMapNative<TKey, TValue> map)
        {
            _map = map;
        }

        public uint Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)_map.Count;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ArrayMapNativeKeyValuePair<TKey, TValue>[] Items
        {
            get
            {
                var map = _map;
                var array = new ArrayMapNativeKeyValuePair<TKey, TValue>[map.Count];
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
