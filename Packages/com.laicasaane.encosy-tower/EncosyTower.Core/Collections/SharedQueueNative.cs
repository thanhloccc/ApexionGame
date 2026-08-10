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
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public readonly partial struct SharedQueueNative<T>
        : IReadOnlyCollection<T>
        , IIsCreated
        , IHasCapacity
        , IHasCount
        , IClearable
        , IToArray<T>
        , ICopyToSpan<T>
        , ITryCopyToSpan<T>
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal readonly unsafe SharedQueueUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        internal readonly AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe SharedQueueNative(SharedQueueUnsafe<T>* data, AtomicSafetyHandle safety)
        {
            // SAFETY: The owner supplies a live borrowed header and its representative shared safety handle.
            unsafe
            {
                m_Data = data;
            }
            m_Safety = safety;
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe SharedQueueNative(SharedQueueUnsafe<T>* data)
        {
            // SAFETY: The owner supplies a live borrowed header.
            unsafe
            {
                m_Data = data;
            }
        }
#endif

        public bool IsCreated
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
        public int Count
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
        public int Capacity
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
        public void Enqueue(T item)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Enqueue(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(in T item)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Enqueue(in item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->Dequeue();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T value)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->TryDequeue(out value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return m_Data->Peek();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T value)
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return m_Data->TryPeek(out value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return m_Data->ToArray();
            }
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
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
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
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return m_Data->TryCopyTo(sourceStartIndex, destination, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnly AsReadOnly()
            => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return new Enumerator(m_Data, AsReadOnly());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }
    }
}
