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
    partial class SharedList<T, TNative>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>The returned native view borrows the shared list allocation and must not outlive the list.</safety>
        public unsafe SharedListNative<TNative> AsNative()
        {
            // SAFETY: The returned native view borrows the shared list's live header and safety handle.
            unsafe
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                return new(_nativeData, _buffer.GetSafetyHandle());
#else
                return new(_nativeData);
#endif
            }
        }
    }

    /// <remarks>
    /// <para>SharedListNative is not thread safe.</para>
    /// <para>The capacity of SharedListNative is immutable, cannot change.</para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public readonly partial struct SharedListNative<T> : IReadOnlyList<T>, IIndexer<T>
        , IIsCreated, IToArray<T>
        , IAsSpan<T>, IAsReadOnlySpan<T>, IAsNativeSlice<T>
        , ICopyFromSpan<T>, ITryCopyFromSpan<T>
        , ICopyToSpan<T>, ITryCopyToSpan<T>
        , IAddRangeSpan<T>, IHasCapacity, IHasCount
        , IClearable
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal readonly unsafe SharedListUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        internal readonly AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        internal unsafe SharedListNative(SharedListUnsafe<T>* data, AtomicSafetyHandle safety)
        {
            // SAFETY: The constructor receives a borrowed live header from the owning shared list.
            unsafe
            {
                m_Data = data;
            }
            m_Safety = safety;
        }
#else
        internal unsafe SharedListNative(SharedListUnsafe<T>* data)
        {
            // SAFETY: The constructor receives a borrowed live header from the owning shared list.
            unsafe
            {
                m_Data = data;
            }
        }
#endif

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

                // SAFETY: The read check validates the live shared list header.
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

                // SAFETY: The read check validates the live shared list header.
                unsafe
                {
                    return m_Data->Count;
                }
            }
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                CheckRead();

                // SAFETY: The read check validates the live header before the indexed access.
                unsafe
                {
                    return (*m_Data)[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CheckWrite();

                // SAFETY: The write check validates the live header before the indexed mutation.
                unsafe
                {
                    (*m_Data)[index] = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before appending.
            unsafe
            {
                m_Data->Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before appending the referenced value.
            unsafe
            {
                m_Data->Add(in item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before insertion.
            unsafe
            {
                m_Data->Insert(index, item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, in T item)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before inserting the referenced value.
            unsafe
            {
                m_Data->Insert(index, in item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header for the returned mutable reference.
            unsafe
            {
                return ref m_Data->ElementAt(index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ReadOnlySpan<T> items)
            => AddRange(items, items.Length);

        public void AddRange(ReadOnlySpan<T> items, int count)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before copying source values into it.
            unsafe
            {
                m_Data->AddRange(items, count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(NativeArray<T> items)
            => AddRange(items, items.Length);

        public void AddRange(NativeArray<T> items, int count)
        {
            CheckWrite();

            // SAFETY: The write check validates the destination header; AsSpan validates the source array.
            unsafe
            {
                m_Data->AddRange(items.AsSpan(), count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(in NativeSlice<T> items)
            => AddRange(items, items.Length);

        public void AddRange(in NativeSlice<T> items, int count)
        {
            CheckWrite();

            // SAFETY: The write check validates the destination header; NativeSlice validates its source storage.
            unsafe
            {
                m_Data->AddRange(in items, count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ReadOnlySpan<T> source)
            => CopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ReadOnlySpan<T> source, int length)
            => CopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => CopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before copying into its buffer.
            unsafe
            {
                m_Data->CopyFrom(destinationStartIndex, source, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(ReadOnlySpan<T> source)
            => TryCopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(ReadOnlySpan<T> source, int length)
            => TryCopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => TryCopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before attempting the copy.
            unsafe
            {
                return m_Data->TryCopyFrom(destinationStartIndex, source, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex)
            => CopyTo(array.AsSpan().Slice(arrayIndex, Count));

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

            // SAFETY: The read check validates the live header before copying from its buffer.
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
        public void CopyTo(NativeArray<T> array)
            => CopyTo(0, array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int index, NativeArray<T> array)
            => CopyTo(index, array, array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int index, NativeArray<T> array, int length)
        {
            CheckRead();

            // SAFETY: The read check validates the source header; AsSpan validates the destination array.
            unsafe
            {
                m_Data->AsReadOnlySpan().Slice(index, length).CopyTo(array.AsSpan()[..length]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int index, in NativeSlice<T> array)
            => CopyTo(index, array, array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int index, in NativeSlice<T> array, int length)
        {
            CheckRead();

            // SAFETY: The read check validates the source header before constructing its temporary view.
            unsafe
            {
                var source = CreateNativeArray(m_Data->_buffer, m_Data->Count);
                array.Slice(0, length).CopyFrom(source.Slice(index, length));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(AsReadOnly());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Peek()
        {
            CheckRead();

            // SAFETY: The read check validates the live header for the returned reference.
            unsafe
            {
                return ref m_Data->Peek();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Pop()
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before removing its final value.
            unsafe
            {
                return ref m_Data->Pop();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Push(T item)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before appending through the stack API.
            unsafe
            {
                return m_Data->Push(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Push(in T item)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before appending the referenced value.
            unsafe
            {
                return m_Data->Push(in item);
            }
        }

        public void RemoveAt(int index)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before indexed removal.
            unsafe
            {
                m_Data->RemoveAt(index);
            }
        }

        public void RemoveRange(int startIndex, int length)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before range removal.
            unsafe
            {
                m_Data->RemoveRange(startIndex, length);
            }
        }

        public void RemoveAtSwapBack(int index)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before swap-back removal.
            unsafe
            {
                m_Data->RemoveAtSwapBack(index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            CheckRead();

            // SAFETY: The read check validates the live header before copying its contents.
            unsafe
            {
                return m_Data->ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            CheckWrite();

            // SAFETY: The write check validates the live header for the returned mutable span.
            unsafe
            {
                return m_Data->AsSpan();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            CheckRead();

            // SAFETY: The read check validates the live header for the returned read-only span.
            unsafe
            {
                return m_Data->AsReadOnlySpan();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> AsNativeSlice()
        {
            CheckWrite();

            // SAFETY: The write check validates the header and its pointer before attaching the safety handle.
            unsafe
            {
                return CreateNativeArray(m_Data->_buffer, m_Data->Count).Slice();
            }
        }

        public Span<T> AddReplicate(int amount)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before reserving initialized slots.
            unsafe
            {
                return m_Data->AddReplicate(amount);
            }
        }

        public Span<T> AddReplicate(T value, int amount)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before reserving filled slots.
            unsafe
            {
                return m_Data->AddReplicate(value, amount);
            }
        }

        public Span<T> AddReplicateNoInit(int amount)
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before reserving uninitialized slots.
            unsafe
            {
                return m_Data->AddReplicateNoInit(amount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedListNative<U> Reinterpret<U>()
            where U : unmanaged
        {
            CheckRead();
            ThrowHelper.ThrowIfTypesHaveDifferentSize(UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>());

            // SAFETY: Equal-size validation preserves the shared header layout and the result borrows this view.
            unsafe
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                return new SharedListNative<U>((SharedListUnsafe<U>*)m_Data, m_Safety);
#else
                return new SharedListNative<U>((SharedListUnsafe<U>*)m_Data);
#endif
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
        internal void MarkChanged()
        {
            CheckWrite();

            // SAFETY: The write check validates the live header before updating its version.
            unsafe
            {
                m_Data->IncrementVersion();
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
