using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections
{
    public partial struct ListNative<T>
        where T : unmanaged
    {
        [StructLayout(LayoutKind.Sequential)]
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public readonly struct ReadOnly
            : IReadOnlyList<T>
            , IIsCreated
            , IHasCapacity
            , IHasCount
            , IToArray<T>
            , IAsReadOnlySpan<T>
            , ICopyToSpan<T>
            , ITryCopyToSpan<T>
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe ListUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

            internal ReadOnly(ListNative<T> source)
            {
                // SAFETY: The read-only view borrows the live header and owner handle.
                unsafe
                {
                    m_Data = source.m_Data;
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = source.m_Safety;
#endif
            }

            public readonly bool IsCreated
            {
                get
                {
                    // SAFETY: Reading the pointer and owner state only observes allocation state.
                    unsafe
                    {
                        return m_Data != null && m_Data->IsCreated;
                    }
                }
            }
            public readonly int Count
            {
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the live native header before the read.
                    unsafe
                    {
                        return m_Data->_count;
                    }
                }
            }
            public readonly int Capacity
            {
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the live native header before the read.
                    unsafe
                    {
                        return m_Data->_buffer.Capacity;
                    }
                }
            }
            public readonly T this[int index]
            {
                get
                {
                    CheckRead();
                    // SAFETY: CheckRead validates the live native header before the read.
                    unsafe
                    {
                        return (*m_Data)[index];
                    }
                }
            }

            public readonly ReadOnlySpan<T> AsReadOnlySpan()
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return m_Data->AsReadOnlySpan();
                }
            }

            public readonly T[] ToArray()
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return m_Data->ToArray();
                }
            }

            public readonly void CopyTo(Span<T> destination)
                => CopyTo(0, destination);

            public readonly void CopyTo(Span<T> destination, int length)
                => CopyTo(0, destination, length);

            public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
                => CopyTo(sourceStartIndex, destination, destination.Length);

            public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    m_Data->CopyTo(sourceStartIndex, destination, length);
                }
            }

            public readonly bool TryCopyTo(Span<T> destination)
                => TryCopyTo(0, destination);

            public readonly bool TryCopyTo(Span<T> destination, int length)
                => TryCopyTo(0, destination, length);

            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => TryCopyTo(sourceStartIndex, destination, destination.Length);

            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return m_Data->TryCopyTo(sourceStartIndex, destination, length);
                }
            }

            public readonly Enumerator GetEnumerator()
            {
                CheckRead();
                // SAFETY: CheckRead validates the live native header before the read.
                unsafe
                {
                    return new Enumerator(m_Data, this);
                }
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public static implicit operator ReadOnly(in ListNative<T> source)
                => source.AsReadOnly();

            private readonly void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            }
        }
    }
}
