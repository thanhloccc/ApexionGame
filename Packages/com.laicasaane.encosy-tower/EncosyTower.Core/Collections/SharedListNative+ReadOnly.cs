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
    partial class SharedList<T, TNative>
    {
        partial struct ReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            /// <safety>The returned native view borrows the shared list allocation and must not
            /// outlive the list.</safety>
            public unsafe SharedListNative<TNative>.ReadOnly AsNative()
            {
                // SAFETY: The returned native view borrows the shared list's live header and safety handle.
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

    partial struct SharedListNative<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnly AsReadOnly()
        {
            // SAFETY: The returned view borrows this shared list's live header and safety handle.
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
        public readonly struct ReadOnly : IReadOnlyList<T>, IReadOnlyIndexer<T>
            , IAsReadOnlySpan<T>, IToArray<T>
            , ICopyToSpan<T>, ITryCopyToSpan<T>
            , IHasCapacity, IHasCount, IIsCreated
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe SharedListUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            internal readonly AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe ReadOnly(
                  SharedListUnsafe<T>* data
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                , AtomicSafetyHandle safety
#endif
            )
            {
                // SAFETY: The constructor receives a borrowed live header from the owning shared list.
                unsafe
                {
                    m_Data = data;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                m_Safety = safety;
#endif
            }

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

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();

                    // SAFETY: The read check validates the live shared list header.
                    unsafe
                    {
                        return m_Data->Count;
                    }
                }
            }

            public int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();

                    // SAFETY: The read check validates the live shared list header.
                    unsafe
                    {
                        return m_Data->Capacity;
                    }
                }
            }

            public bool IsReadOnly
                => true;

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
            public Enumerator GetEnumerator()
                => new(this);

            public T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();

                    // SAFETY: The read check validates the live header before indexed access.
                    unsafe
                    {
                        return (*m_Data)[index];
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(in SharedListNative<T> list)
                => list.AsReadOnly();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnlySpan<T>(in ReadOnly list)
                => list.AsReadOnlySpan();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<T> AsReadOnlySpan()
            {
                CheckRead();

                // SAFETY: The read check validates the live header for the returned span.
                unsafe
                {
                    return m_Data->AsReadOnlySpan();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(T[] destination, int destinationIndex)
                => CopyTo(destination.AsSpan().Slice(destinationIndex, Count));

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
            {
                CheckRead();

                // SAFETY: The read check validates the live header before copying from it.
                unsafe
                {
                    m_Data->CopyTo(sourceStartIndex, destination, length);
                }
            }

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
            {
                CheckRead();

                // SAFETY: The read check validates the live header before attempting the copy.
                unsafe
                {
                    return m_Data->TryCopyTo(sourceStartIndex, destination, length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[] ToArray()
            {
                CheckRead();

                // SAFETY: The read check validates the live header before copying its values.
                unsafe
                {
                    return m_Data->ToArray();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SharedListNative<U>.ReadOnly Reinterpret<U>()
                where U : unmanaged
            {
                CheckRead();
                ThrowHelper.ThrowIfTypesHaveDifferentSize(
                    UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>()
                );

                // SAFETY: Equal-size validation preserves the shared header layout and the result borrows this view.
                unsafe
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                    return new SharedListNative<U>.ReadOnly((SharedListUnsafe<U>*)m_Data, m_Safety);
#else
                    return new SharedListNative<U>.ReadOnly((SharedListUnsafe<U>*)m_Data);
#endif
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}
