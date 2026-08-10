using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections
{
    public partial struct SharedQueueNative<T>
        where T : unmanaged
    {
        [StructLayout(LayoutKind.Sequential)]
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public readonly struct ReadOnly
            : IReadOnlyCollection<T>
            , IIsCreated
            , IHasCapacity
            , IHasCount
            , IToArray<T>
            , ICopyToSpan<T>
            , ITryCopyToSpan<T>
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe SharedQueueUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            internal readonly AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnly(SharedQueueNative<T> source)
            {
                // SAFETY: The read-only view borrows the live header and copied shared handle.
                unsafe
                {
                    m_Data = source.m_Data;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                m_Safety = source.m_Safety;
#endif
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: The owning collection keeps the native header live while this property reads its state.
                    unsafe
                    {
                        return m_Data != null;
                    }
                }
            }
            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the live native header before the read.
                    unsafe
                    {
                        return m_Data->Count;
                    }
                }
            }
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the live native header before the read.
                    unsafe
                    {
                        return m_Data->Capacity;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly T Peek()
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return m_Data->Peek();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryPeek(out T value)
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return m_Data->TryPeek(out value);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly T[] ToArray()
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return m_Data->ToArray();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination)
                => CopyTo(0, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination, int length)
                => CopyTo(0, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
                => CopyTo(sourceStartIndex, destination, destination.Length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    m_Data->CopyTo(sourceStartIndex, destination, length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination)
                => TryCopyTo(0, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination, int length)
                => TryCopyTo(0, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => TryCopyTo(sourceStartIndex, destination, destination.Length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return m_Data->TryCopyTo(sourceStartIndex, destination, length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Enumerator GetEnumerator()
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return new Enumerator(m_Data, this);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            }
        }
    }
}
