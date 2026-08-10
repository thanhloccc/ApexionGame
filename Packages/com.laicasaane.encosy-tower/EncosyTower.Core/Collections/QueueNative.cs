using System;
using System.Collections;
using System.Collections.Generic;
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
    public partial struct QueueNative<T>
        : IDisposable
        , IReadOnlyCollection<T>
        , IHasCapacity, IIncreaseCapacity
        , IHasCount
        , IIsCreated
        , IClearable
        , IToArray<T>
        , ICopyToSpan<T>
        , ITryCopyToSpan<T>
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe QueueUnsafe<T>* m_Data;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId =
            Unity.Burst.SharedStatic<int>.GetOrCreate<QueueNative<T>>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        public QueueNative(int capacity, AllocatorStrategy allocator)
            : this()
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(capacity >= 0);
            ThrowHelper.ThrowIfAllocatorIsInvalid(allocator.IsValid);
            // SAFETY: Alloc returns a live header owned by this native value.
            unsafe
            {
                m_Data = QueueUnsafe<T>.Alloc(capacity, allocator);
            }
            CreateSafety(allocator.ToAllocator());
        }

        public QueueNative(ReadOnlySpan<T> source, AllocatorStrategy allocator)
            : this()
        {
            ThrowHelper.ThrowIfAllocatorIsInvalid(allocator.IsValid);
            // SAFETY: Alloc returns a live header owned by this native value.
            unsafe
            {
                m_Data = QueueUnsafe<T>.Alloc(source, allocator);
            }
            CreateSafety(allocator.ToAllocator());
        }

        private void CreateSafety(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = EncosyCollectionSafetyAPI.CreateSafetyHandle(allocator);
            EncosyCollectionSafetyAPI.SetStaticSafetyId<QueueNative<T>>(
                  ref m_Safety,
#if UNITY_BURST
                ref s_SafetyId.Data
#else
                ref s_SafetyId
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
                // SAFETY: The owning collection keeps the native header live while this property reads its state.
                unsafe
                {
                    return m_Data != null && m_Data->IsCreated;
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
                    return m_Data->_count;
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
                    return m_Data->_buffer.Capacity;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Enqueue(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(in T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Enqueue(in item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnqueueRange(ReadOnlySpan<T> items)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->EnqueueRange(items);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int capacity)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->EnsureCapacity(capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->IncreaseCapacityBy(amount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityTo(int capacity)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->IncreaseCapacityTo(capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trim()
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Trim();
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

        public void Dispose()
        {
            // SAFETY: The header is owned by this container and is freed exactly once.
            unsafe
            {
                if (m_Data == null)
                {
                    return;
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref m_Safety);
#endif
                QueueUnsafe<T>.Free(m_Data);
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

            // SAFETY: The dispose job receives the live header and owns its release.
            unsafe
            {
                if (m_Data == null)
                {
                    return inputDeps;
                }

                var result = new QueueNativeDisposeJob {
                    _data = new QueueNativeDispose {
                        m_Data = (QueueUnsafe<int>*)m_Data,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        m_Safety = m_Safety,
#endif
                    },
                }.Schedule(inputDeps);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                EncosyCollectionSafetyAPI.ReleaseSafetyHandleAfterSchedule(ref m_Safety);
#endif
                m_Data = null;
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckResizeWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
        }
    }

#if UNITY_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct QueueNativeDisposeJob : IJob
    {
        internal QueueNativeDispose _data;

        public readonly void Execute()
            => _data.Dispose();
    }

    [NativeContainer]
    internal struct QueueNativeDispose
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe QueueUnsafe<int>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        public readonly void Dispose()
        {
            // SAFETY: The scheduled job owns the live queue header and releases it once.
            unsafe
            {
                QueueUnsafe<int>.Free(m_Data);
            }
        }
    }
}
