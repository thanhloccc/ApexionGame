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
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(ArraySetNativeDebugProxy<>))]
    public partial struct ArraySetNative<T> : IDisposable, IClearable, IIsCreated
        , IIncreaseCapacity, IHasCount
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged, IEquatable<T>
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe ArraySetUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
            = Unity.Burst.SharedStatic<int>.GetOrCreate<ArraySetNative<T>>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        public ArraySetNative(int capacity, AllocatorStrategy allocator) : this()
        {
            // SAFETY: The allocation returns an owned native header stored for the lifetime of this container.
            unsafe
            {
                m_Data = ArraySetUnsafe<T>.Alloc(capacity, allocator);
                CreateSafety(allocator.ToAllocator());
            }
        }

        public ArraySetNative(ArraySetNative<T> source, AllocatorStrategy allocator) : this()
        {
            ThrowHelper.ThrowIfNativeSourceCollectionIsNotCreated(
                source.IsCreated,
                ThrowHelper.CollectionType.ArraySetNative
            );

            // SAFETY: source.IsCreated was checked and the allocation creates an owned deep copy.
            unsafe
            {
                m_Data = ArraySetUnsafe<T>.Alloc(*source.m_Data, allocator);
                CreateSafety(allocator.ToAllocator());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateSafety(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = EncosyCollectionSafetyAPI.CreateSafetyHandle(allocator);

            EncosyCollectionSafetyAPI.SetStaticSafetyId<ArraySetNative<T>>(
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
                // SAFETY: The pointer is an owned header and is only dereferenced after a null check.
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

        public readonly NativeSliceReadOnly<T> Items
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner and the slice is bounded by the live set count.
                unsafe
                {
                    return new NativeSlice<T>(ItemsNativeArray(), 0, m_Data->Count);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly NativeArray<T> ItemsNativeArray()
        {
            // SAFETY: Public callers validate the safety handle before exposing this bounded native view.
            unsafe
            {
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
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
            // SAFETY: The null check reads only the owned native header pointer.
            unsafe
            {
                if (m_Data == null)
                {
                    return;
                }
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }

            EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref m_Safety);
#endif

            // SAFETY: The header is owned by this container and the dispose path is idempotently guarded above.
            unsafe
            {
                ArraySetUnsafe<T>.Free(m_Data);
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

            // SAFETY: The checked container owns the header until the scheduled dispose job runs.
            unsafe
            {
                var jobHandle = new ArraySetNativeDisposeJob {
                    _data = new ArraySetNativeDispose {
                        m_Data = (ArraySetUnsafe<int>*)m_Data,
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
        public readonly ArraySetNativeEnumerator<T> GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return new(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ArraySetNative<U> Reinterpret<U>()
            where U : unmanaged, IEquatable<U>
        {
            // SAFETY: Reinterpret preserves the same live native header and only changes unmanaged element types.
            unsafe
            {
                var result = default(ArraySetNative<U>);
                result.m_Data = (ArraySetUnsafe<U>*)m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            result.m_Safety = m_Safety;

            EncosyCollectionSafetyAPI.SetStaticSafetyId<ArraySetNative<U>>(
                  ref result.m_Safety
#if UNITY_BURST
                , ref ArraySetNative<U>.s_SafetyId.Data
#else
                , ref ArraySetNative<U>.s_SafetyId
#endif
            );
#endif

                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before forwarding to the native set.
            unsafe
            {
                return m_Data->Add(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before forwarding to the native set.
            unsafe
            {
                return m_Data->Add(in value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before forwarding to the native set.
            unsafe
            {
                m_Data->Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            // SAFETY: CheckRead validated the owner before forwarding to the native set.
            unsafe
            {
                return m_Data->Contains(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            // SAFETY: CheckRead validated the owner before forwarding to the native set.
            unsafe
            {
                return m_Data->Contains(in value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int size)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before forwarding to the native set.
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
        public bool Remove(T value)
            => Remove(in value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before forwarding to the native set.
            unsafe
            {
                return m_Data->Remove(in value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trim()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before forwarding to the native set.
            unsafe
            {
                m_Data->Trim();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Intersect(in ArraySetNative<T> otherSet)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(otherSet.m_Safety);
#endif
            // SAFETY: Both safety handles were checked before accessing the two live native headers.
            unsafe
            {
                m_Data->Intersect(in *otherSet.m_Data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exclude(in ArraySetNative<T> otherSet)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(otherSet.m_Safety);
#endif
            // SAFETY: Both safety handles were checked before accessing the two live native headers.
            unsafe
            {
                m_Data->Exclude(in *otherSet.m_Data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Union(in ArraySetNative<T> otherSet)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(otherSet.m_Safety);
#endif
            // SAFETY: Both safety handles were checked before accessing the two live native headers.
            unsafe
            {
                m_Data->Union(in *otherSet.m_Data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool AddValue(in T value, out int indexSet)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner before forwarding to the native set.
            unsafe
            {
                return m_Data->AddValue(value, out indexSet);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ref T GetValueRefAt(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner and the caller uses the returned ref within
            // the native view lifetime.
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
        internal readonly Span<T> AsValuesSpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: CheckWrite validated the owner and the span is bounded by the native set count.
            unsafe
            {
                return m_Data->_values.AsSpan()[..m_Data->Count];
            }
        }

    }

    public struct ArraySetNativeEnumerator<T> : IEnumerator<T>, IIsValid
        where T : unmanaged, IEquatable<T>
    {
        private ArraySetNative<T> _set;
        private readonly int _version;

        private int _index;

        public ArraySetNativeEnumerator(in ArraySetNative<T> set) : this()
        {
            _set = set;
            _index = -1;

            // SAFETY: The enumerator is constructed from a live, safety-checked native set.
            unsafe
            {
                _version = set.m_Data->_version;
            }
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

            // SAFETY: IsValid and the version check keep the native set header alive during enumeration.
            unsafe
            {
                ThrowHelper.ThrowIfSetIsBeingIterated(_version == _set.m_Data->_version);
            }

            // SAFETY: IsValid keeps the native set header alive during enumeration.
            unsafe
            {
                if (_index < _set.m_Data->Count - 1)
                {
                    ++_index;
                    return true;
                }

                return false;
            }
        }

        public readonly T Current
        {
            get
            {
                // SAFETY: Enumerator validation keeps the index inside the live set values buffer.
                unsafe
                {
                    return _set.m_Data->_values[_index];
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

#if UNITY_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct ArraySetNativeDisposeJob : IJob
    {
        internal ArraySetNativeDispose _data;

        public readonly void Execute()
            => _data.Dispose();
    }

    [NativeContainer]
    internal struct ArraySetNativeDispose
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe ArraySetUnsafe<int>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        public readonly void Dispose()
        {
            // SAFETY: The scheduled dispose job owns the header and releases it exactly once.
            unsafe
            {
                ArraySetUnsafe<int>.Free(m_Data);
            }
        }
    }

    internal sealed class ArraySetNativeDebugProxy<T>
        where T : unmanaged, IEquatable<T>
    {

        private readonly ArraySetNative<T> _set;

        public ArraySetNativeDebugProxy(in ArraySetNative<T> set)
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
