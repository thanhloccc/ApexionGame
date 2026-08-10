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
    public partial struct ListNative<T>
        : IDisposable
        , IReadOnlyList<T>
        , IIndexer<T>
        , IIsCreated
        , IHasCapacity
        , IHasCount
        , IAsSpan<T>
        , IAsReadOnlySpan<T>
        , IAsNativeSlice<T>
        , IToArray<T>
        , ICopyFromSpan<T>
        , ITryCopyFromSpan<T>
        , ICopyToSpan<T>
        , ITryCopyToSpan<T>
        , IAddRangeSpan<T>
        , IIncreaseCapacity
        , IClearable
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe ListUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId =
            Unity.Burst.SharedStatic<int>.GetOrCreate<ListNative<T>>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        public ListNative(int capacity, AllocatorStrategy allocator)
            : this()
        {
            ThrowHelper.ThrowIfCapacityIsInvalid(capacity >= 0);
            ThrowHelper.ThrowIfAllocatorIsInvalid(allocator.IsValid);
            // SAFETY: Alloc returns a live native header owned by this value.
            unsafe
            {
                m_Data = ListUnsafe<T>.Alloc(capacity, allocator);
            }
            CreateSafety(allocator.ToAllocator());
        }

        public ListNative(ReadOnlySpan<T> source, AllocatorStrategy allocator)
            : this()
        {
            ThrowHelper.ThrowIfAllocatorIsInvalid(allocator.IsValid);
            // SAFETY: Alloc returns a live native header owned by this value.
            unsafe
            {
                m_Data = ListUnsafe<T>.Alloc(source, allocator);
            }
            CreateSafety(allocator.ToAllocator());
        }

        private void CreateSafety(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = EncosyCollectionSafetyAPI.CreateSafetyHandle(allocator);

            EncosyCollectionSafetyAPI.SetStaticSafetyId<ListNative<T>>(
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
            get
            {
                // SAFETY: Reading the pointer and owner state only observes whether the allocation exists.
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
                // SAFETY: CheckRead validates the live header before reading its scalar state.
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
                // SAFETY: CheckRead validates the live header before reading its buffer capacity.
                unsafe
                {
                    return m_Data->_buffer.Capacity;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                CheckRead();
                // SAFETY: CheckRead validates the live header and ListUnsafe validates the logical index.
                unsafe
                {
                    return (*m_Data)[index];
                }
            }
            set
            {
                CheckWrite();
                // SAFETY: CheckWrite validates the live header and ListUnsafe validates the logical index.
                unsafe
                {
                    (*m_Data)[index] = value;
                }
            }
        }

        public ref T ElementAt(int index)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the owner and the returned ref is bounded by the live allocation.
            unsafe
            {
                return ref m_Data->ElementAt(index);
            }
        }

        public void Add(T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Add(item);
            }
        }

        public void Add(in T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Add(in item);
            }
        }

        public void Insert(int index, T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Insert(index, item);
            }
        }

        public void Insert(int index, in T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Insert(index, in item);
            }
        }

        public void AddRange(ReadOnlySpan<T> items)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->AddRange(items);
            }
        }

        public void AddRange(ReadOnlySpan<T> items, int count)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->AddRange(items, count);
            }
        }

        public void AddRange(NativeArray<T> items)
            => AddRange(items, items.Length);

        public void AddRange(NativeArray<T> items, int count)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->AddRange(items.AsSpan(), count);
            }
        }

        public void AddRange(in NativeSlice<T> items)
            => AddRange(in items, items.Length);

        public void AddRange(in NativeSlice<T> items, int count)
        {
            CheckResizeWrite();
            ThrowHelper.ThrowIfSourceCountIsInvalid((uint)count <= (uint)items.Length);
            if (count == 0)
            {
                return;
            }
            // SAFETY: AddReplicateNoInit grows the buffer and reserves the destination range before the copy.
            unsafe
            {
                var oldCount = m_Data->_count;
                m_Data->AddReplicateNoInit(count);
                var destination = CreateNativeArray(
                      m_Data->_buffer.GetUnsafePtr()
                    , m_Data->_count
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    , m_Safety
#endif
                );
                new NativeSlice<T>(destination)
                    .Slice(oldCount, count)
                    .CopyFrom(items.Slice(0, count));
            }
        }

        public void CopyFrom(ReadOnlySpan<T> source)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->CopyFrom(source);
            }
        }

        public void CopyFrom(ReadOnlySpan<T> source, int length)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->CopyFrom(source, length);
            }
        }

        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->CopyFrom(destinationStartIndex, source);
            }
        }

        public void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->CopyFrom(destinationStartIndex, source, length);
            }
        }

        public bool TryCopyFrom(ReadOnlySpan<T> source)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->TryCopyFrom(source);
            }
        }

        public bool TryCopyFrom(ReadOnlySpan<T> source, int length)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->TryCopyFrom(source, length);
            }
        }

        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->TryCopyFrom(destinationStartIndex, source);
            }
        }

        public bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->TryCopyFrom(destinationStartIndex, source, length);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
            => CopyTo(array.AsSpan().Slice(arrayIndex, Count));

        public void CopyTo(Span<T> destination)
            => CopyTo(0, destination);

        public void CopyTo(Span<T> destination, int length)
            => CopyTo(0, destination, length);

        public void CopyTo(int sourceStartIndex, Span<T> destination)
            => CopyTo(sourceStartIndex, destination, destination.Length);

        public void CopyTo(int sourceStartIndex, Span<T> destination, int length)
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                m_Data->CopyTo(sourceStartIndex, destination, length);
            }
        }

        public bool TryCopyTo(Span<T> destination)
            => TryCopyTo(0, destination);

        public bool TryCopyTo(Span<T> destination, int length)
            => TryCopyTo(0, destination, length);

        public bool TryCopyTo(int sourceStartIndex, Span<T> destination)
            => TryCopyTo(sourceStartIndex, destination, destination.Length);

        public bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return m_Data->TryCopyTo(sourceStartIndex, destination, length);
            }
        }

        public void CopyTo(NativeArray<T> array)
            => CopyTo(0, array);

        public void CopyTo(int index, NativeArray<T> array)
            => CopyTo(index, array, array.Length);

        public void CopyTo(int index, NativeArray<T> array, int length)
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                m_Data->AsReadOnlySpan().Slice(index, length).CopyTo(array.AsSpan()[..length]);
            }
        }

        public void CopyTo(int index, in NativeSlice<T> array)
            => CopyTo(index, in array, array.Length);

        public void CopyTo(int index, in NativeSlice<T> array, int length)
        {
            CheckRead();
            // SAFETY: The checked live list bounds the source range rebuilt over its buffer.
            unsafe
            {
                var source = CreateNativeArray(
                      m_Data->_buffer.GetUnsafePtr()
                    , m_Data->_count
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    , m_Safety
#endif
                );
                array.Slice(0, length).CopyFrom(new NativeSlice<T>(source).Slice(index, length));
            }
        }

        public void Clear()
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Clear();
            }
        }

        public void FastClear()
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->FastClear();
            }
        }

        public ref readonly T Peek()
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return ref m_Data->Peek();
            }
        }

        public ref readonly T Pop()
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return ref m_Data->Pop();
            }
        }

        public int Push(T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->Push(item);
            }
        }

        public int Push(in T item)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->Push(in item);
            }
        }

        public void RemoveAt(int index)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->RemoveAt(index);
            }
        }

        public void RemoveRange(int startIndex, int length)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->RemoveRange(startIndex, length);
            }
        }

        public void RemoveAtSwapBack(int index)
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                m_Data->RemoveAtSwapBack(index);
            }
        }

        public T[] ToArray()
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return m_Data->ToArray();
            }
        }

        public Span<T> AsSpan()
        {
            CheckWrite();
            // SAFETY: CheckWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->AsSpan();
            }
        }

        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before the read.
            unsafe
            {
                return m_Data->AsReadOnlySpan();
            }
        }

        public NativeSlice<T> AsNativeSlice()
        {
            CheckRead();
            // SAFETY: CheckRead validates the live native header before creating the borrowed slice.
            unsafe
            {
                return new NativeSlice<T>(CreateNativeArray(
                      m_Data->_buffer.GetUnsafePtr()
                    , m_Data->_count
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    , m_Safety
#endif
                ));
            }
        }

        public Span<T> AddReplicate(int amount)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->AddReplicate(amount);
            }
        }

        public Span<T> AddReplicate(T value, int amount)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->AddReplicate(value, amount);
            }
        }

        public Span<T> AddReplicateNoInit(int amount)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->AddReplicateNoInit(amount);
            }
        }

        public int IncreaseCapacityBy(int amount)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->IncreaseCapacityBy(amount);
            }
        }

        public int IncreaseCapacityTo(int capacity)
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                return m_Data->IncreaseCapacityTo(capacity);
            }
        }

        public void Trim()
        {
            CheckResizeWrite();
            // SAFETY: CheckResizeWrite validates the live native header before the write.
            unsafe
            {
                m_Data->Trim();
            }
        }

        public Enumerator GetEnumerator()
        {
            CheckRead();
            // SAFETY: The pointer remains owned by this native container until Dispose.
            unsafe
            {
                return new Enumerator(m_Data, AsReadOnly());
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public ReadOnly AsReadOnly()
            => new(this);

        public ListNative<U> Reinterpret<U>()
            where U : unmanaged
        {
            CheckRead();
            ThrowHelper.ThrowIfTypesHaveDifferentSize(AreTypesSameSize<U>());
            var result = default(ListNative<U>);
            // SAFETY: Equal-size validation preserves the owned header layout and the source handle guards
            // the allocation.
            unsafe
            {
                result.m_Data = (ListUnsafe<U>*)m_Data;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            result.m_Safety = m_Safety;

            EncosyCollectionSafetyAPI.SetStaticSafetyId<ListNative<U>>(
                  ref result.m_Safety
#if UNITY_BURST
                , ref ListNative<U>.s_SafetyId.Data
#else
                , ref ListNative<U>.s_SafetyId
#endif
            );
#endif

            return result;
        }

        public static ListNative<T> Prefill(int amount, AllocatorStrategy allocator)
        {
            var result = new ListNative<T>(amount, allocator);
            result.AddReplicate(amount);
            return result;
        }

        public static ListNative<T> Prefill(T value, int amount, AllocatorStrategy allocator)
        {
            var result = new ListNative<T>(amount, allocator);
            result.AddReplicate(value, amount);
            return result;
        }

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
                ListUnsafe<T>.Free(m_Data);
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

                var result = new ListNativeDisposeJob {
                    _data = new ListNativeDispose {
                        m_Data = (ListUnsafe<int>*)m_Data,
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
        private static bool AreTypesSameSize<U>()
            where U : unmanaged
            => UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>();

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

        private static unsafe NativeArray<T> CreateNativeArray(
              void* pointer
            , int length
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            , AtomicSafetyHandle safety
#endif
        )
        {
            // SAFETY: The pointer and logical length come from the checked live list allocation.
            unsafe
            {
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                      pointer
                    , length
                    , Allocator.None
                );
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);
#endif
                return array;
            }
        }
    }

#if UNITY_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct ListNativeDisposeJob : IJob
    {
        internal ListNativeDispose _data;

        public readonly void Execute()
            => _data.Dispose();
    }

    [NativeContainer]
    internal struct ListNativeDispose
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe ListUnsafe<int>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        public readonly void Dispose()
        {
            // SAFETY: The scheduled job owns the live list header and releases it once.
            unsafe
            {
                ListUnsafe<int>.Free(m_Data);
            }
        }
    }
}
