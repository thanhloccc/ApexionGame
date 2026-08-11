using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using static ApexionGame.Entities.Stats.Debugging.ValidationDefines;

namespace ApexionGame.Entities.Stats
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct StatBuffer<T> : IIsCreated
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<T>* m_List;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal StatBuffer(UnsafeList<T>* list) : this()
        {
            m_List = list;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_List != null && m_List->IsCreated;
        }

        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                ThrowIfNotCreated(IsCreated);

                // SAFETY: The checks above validate the live list header before reading its length.
                return m_List->Length;
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
                ThrowIfNotCreated(IsCreated);

                // SAFETY: The checks above validate the live list header before reading its capacity.
                return m_List->Capacity;
            }
        }

        public readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                ThrowIfNotCreated(IsCreated);
                ThrowIfIndexOutOfRange((uint)index < (uint)m_List->Length, index, m_List->Length);

                // SAFETY: The checks above validate the live header and the index before the read.
                return m_List->Ptr[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                ThrowIfNotCreated(IsCreated);
                ThrowIfIndexOutOfRange((uint)index < (uint)m_List->Length, index, m_List->Length);

                // SAFETY: The checks above validate the live header and the index before the write.
                m_List->Ptr[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T ElementAt(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);
            ThrowIfIndexOutOfRange((uint)index < (uint)m_List->Length, index, m_List->Length);

            // SAFETY: The checks above validate the live header and the index. The returned
            // reference stays valid only until the next structural change of this buffer.
            return ref m_List->ElementAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Add(T item)
        {
            Add(in item);
        }

        public readonly void Add(in T item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            // SAFETY: The checks above validate the live header; Add grows the buffer itself.
            m_List->Add(in item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Insert(int index, T item)
        {
            Insert(index, in item);
        }

        public readonly void Insert(int index, in T item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);
            ThrowIfIndexOutOfRange((uint)index <= (uint)m_List->Length, index, m_List->Length);

            // SAFETY: The checks above validate the live header and the insertion point.
            // InsertRange shifts the tail and grows the buffer, leaving the slot uninitialized.
            m_List->InsertRange(index, 1);
            m_List->Ptr[index] = item;
        }

        public readonly void RemoveAt(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);
            ThrowIfIndexOutOfRange((uint)index < (uint)m_List->Length, index, m_List->Length);

            // SAFETY: The checks above validate the live header and the index before the shift.
            m_List->RemoveAt(index);
        }

        public readonly void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            // SAFETY: The check above validates the live header before resetting its length.
            m_List->Clear();
        }

        public readonly void SetCapacity(int capacity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            // SAFETY: The check above validates the live header before reallocating.
            m_List->SetCapacity(capacity);
        }

        public readonly void IncreaseCapacityTo(int capacity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            // SAFETY: The check above validates the live header before reallocating.
            if (capacity > m_List->Capacity)
            {
                m_List->SetCapacity(capacity);
            }
        }

        public readonly NativeArray<T> AsArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            // SAFETY: The checks above validate the live header. The array aliases the buffer
            // memory with Allocator.None, so it never frees it, and it borrows this buffer's
            // safety handle so a structural change invalidates it.
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                  m_List->Ptr
                , m_List->Length
                , Allocator.None
            );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref safety);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);
#endif
            return array;
        }

        public readonly Span<T> AsSpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            // SAFETY: The checks above validate the live header. The span aliases the buffer
            // memory and stays valid only until the next structural change.
            return new Span<T>(m_List->Ptr, m_List->Length);
        }

        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            // SAFETY: The checks above validate the live header. The span aliases the buffer
            // memory and stays valid only until the next structural change.
            return new ReadOnlySpan<T>(m_List->Ptr, m_List->Length);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfNotCreated([DoesNotReturnIf(false)] bool isCreated)
        {
            if (isCreated == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ObjectDisposedException CreateException()
                => new(nameof(StatBuffer<T>), "The stat buffer is not created or its owner was destroyed.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfIndexOutOfRange(
              [DoesNotReturnIf(false)] bool inRange
            , int index
            , int length
        )
        {
            if (inRange == false)
            {
                throw CreateException(index, length);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static IndexOutOfRangeException CreateException(int index, int length)
                => new($"Index {index} is out of range of a stat buffer of '{length}' elements.");
        }
    }
}
