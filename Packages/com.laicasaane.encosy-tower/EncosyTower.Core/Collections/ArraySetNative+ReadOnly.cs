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
    partial struct ArraySetNative<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
            => new(this);

        public readonly struct ReadOnly : IIsCreated, IHasCount, IHasCapacity
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe ArraySetUnsafe<T>* m_Data;

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
            internal ReadOnly(ArraySetNative<T> set)
            {
                // SAFETY: The read-only wrapper copies the validated source set's owned native header pointer.
                unsafe
                {
                    m_Data = set.m_Data;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = set.m_Safety;

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

            public readonly NativeSliceReadOnly<T> Items
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
                        return new NativeSlice<T>(ItemsNativeArray(), 0, m_Data->Count);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly NativeArray<T> ItemsNativeArray()
            {
                // SAFETY: CheckRead is performed by every public caller; the view is bounded by
                // the native set capacity.
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ArraySetNativeReadOnlyEnumerator<T> GetEnumerator()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return new(this);
            }

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
                => new CopyToSpan<T>(Items.AsSpan()).CopyTo(sourceStartIndex, destination, length);

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
                => new CopyToSpan<T>(Items.AsSpan()).TryCopyTo(sourceStartIndex, destination, length);

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
            internal readonly T ValueAt(int index)
            {
                // SAFETY: Enumerator validation keeps the index within the live set values buffer.
                unsafe
                {
                    return m_Data->_values[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly ReadOnlySpan<T> AsValuesReadOnlySpan()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: CheckRead validated the owner and the slice is bounded by the set count.
                unsafe
                {
                    return m_Data->_values.AsReadOnlySpan()[..m_Data->Count];
                }
            }
        }
    }

    public struct ArraySetNativeReadOnlyEnumerator<T> : IEnumerator<T>, IIsValid
        where T : unmanaged, IEquatable<T>
    {
        private readonly ArraySetNative<T>.ReadOnly _set;
        private readonly int _version;

        private int _index;

        public ArraySetNativeReadOnlyEnumerator(in ArraySetNative<T>.ReadOnly set) : this()
        {
            _set = set;
            _index = -1;
            _version = set.UncheckedVersion;
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
            ThrowHelper.ThrowIfSetIsBeingIterated(_version == _set.UncheckedVersion);

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
            get => _set.ValueAt(_index);
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
