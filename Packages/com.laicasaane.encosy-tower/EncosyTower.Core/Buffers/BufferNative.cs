// https://github.com/sebas77/Svelto.Common/blob/master/DataStructures/DualMemorySupport/NativeStrategy.cs

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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Collections;
using EncosyTower.Collections.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace EncosyTower.Buffers
{
    /// <summary>
    /// A native buffer that wraps a heap-allocated <see cref="BufferUnsafe{T}"/> header with a
    /// single <see cref="AtomicSafetyHandle"/>.
    /// <br/>
    /// <see cref="BufferNative{T}"/> abstracts the handling of native memory so that
    /// data structures can use it interchangeably with <see cref="BufferManaged{T}"/>
    /// and <see cref="BufferUnsafe{T}"/> through the <see cref="IBuffer{T}"/> contract.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public struct BufferNative<T> : IBuffer<T>, IRefIndexer<T>
        , IAsNativeSlice<T>, IAsNativeSliceReadOnly<T>
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe BufferUnsafe<T>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
            = Unity.Burst.SharedStatic<int>.GetOrCreate<BufferNative<T>>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferNative(int size, AllocatorStrategy allocatorStrategy, bool clear = true) : this()
        {
            ThrowHelper.ThrowIfInvalidAllocatorStrategy(allocatorStrategy.IsValid);

            Alloc(size, allocatorStrategy, clear);
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
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
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return m_Data->Capacity;
                }
            }
        }

        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowIfIndexOutOfRangeException((uint)index < (uint)Capacity);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return ref UnsafeUtility.ArrayElementAsRef<T>(m_Data->GetUnsafePtr(), index);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Alloc(int newCapacity, AllocatorStrategy allocatorStrategy, bool memClear = true)
        {
            ThrowHelper.ThrowIfBufferAlreadyAllocated(IsCreated == false);
            ThrowHelper.ThrowIfInvalidAllocatorStrategy(allocatorStrategy.IsValid);

            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                m_Data = allocatorStrategy.Allocate<BufferUnsafe<T>>();

                *m_Data = new BufferUnsafe<T>(newCapacity, allocatorStrategy, memClear);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = EncosyCollectionSafetyAPI.CreateSafetyHandle(allocatorStrategy.ToAllocator());

            EncosyCollectionSafetyAPI.SetStaticSafetyId<BufferNative<T>>(
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newSize)
            => Resize(newSize, true, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newSize, bool copyContent)
            => Resize(newSize, copyContent, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newSize, bool copyContent, bool memClear)
        {
            ThrowHelper.ThrowIfResizeUninitializedBuffer(IsCreated);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // A resize reallocates, invalidating any NativeArray/slice views handed out earlier.
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif

            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                m_Data->Resize(newSize, copyContent, memClear);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void FastClear() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                m_Data->Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReadOnlySpan<T> source)
            => CopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReadOnlySpan<T> source, int length)
            => CopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => CopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).CopyFrom(destinationStartIndex, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(ReadOnlySpan<T> source)
            => TryCopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(ReadOnlySpan<T> source, int length)
            => TryCopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => TryCopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).TryCopyFrom(destinationStartIndex, source, length);

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
            => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

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
            => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly NativeArray<T> AsNativeArray()
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                      m_Data->GetUnsafePtr()
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
        public readonly NativeSlice<T> AsNativeSlice()
            => new(AsNativeArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly NativeSliceReadOnly<T> AsNativeSliceReadOnly()
            => new(AsNativeArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                return m_Data->AsSpan();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                return m_Data->AsReadOnlySpan();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
            => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BufferNative<U> Reinterpret<U>()
            where U : unmanaged
        {
            ThrowHelper.ThrowIfTypesNotEqualSize<T, U>(
                UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>()
            );

            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                var result = default(BufferNative<U>);
                result.m_Data = (BufferUnsafe<U>*)m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                result.m_Safety = m_Safety;

                EncosyCollectionSafetyAPI.SetStaticSafetyId<BufferNative<U>>(
                      ref result.m_Safety
#if UNITY_BURST
                    , ref BufferNative<U>.s_SafetyId.Data
#else
                    , ref BufferNative<U>.s_SafetyId
#endif
                );
#endif

                return result;
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (AtomicSafetyHandle.IsDefaultValue(m_Safety) == false)
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif

            if (IsCreated == false)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            EncosyCollectionSafetyAPI.DisposeSafetyHandle(ref m_Safety);
#endif

            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                BufferNativeDispose.Dispose(m_Data);
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

            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                var jobHandle = new BufferNativeDisposeJob {
                    _data = new BufferNativeDispose {
                        m_Data = (BufferUnsafe<int>*)m_Data,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        m_Safety = m_Safety,
#endif
                    },
                }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                EncosyCollectionSafetyAPI.ReleaseSafetyHandleAfterSchedule(ref m_Safety);
#endif

                m_Data = null;

                return jobHandle;
            }
        }

        public readonly struct ReadOnly : IReadOnlyBuffer<T>, IRefReadOnlyIndexer<T>
            , IAsNativeSliceReadOnly<T>
        {
#pragma warning disable IDE1006 // Naming Styles
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe BufferUnsafe<T>* m_Data;

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
            private ReadOnly(BufferNative<T> buffer)
            {
                // SAFETY: The read-only view borrows the live buffer header from its owner.
                unsafe
                {
                    m_Data = buffer.m_Data;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = buffer.m_Safety;

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

            public int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                    unsafe
                    {
                        return m_Data != null ? m_Data->Capacity : 0;
                    }
                }
            }

            public bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                    unsafe
                    {
                        return m_Data != null && m_Data->IsCreated;
                    }
                }
            }

            public ref readonly T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ThrowHelper.ThrowIfIndexOutOfRangeException((uint)index < (uint)Capacity);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                    unsafe
                    {
                        return ref UnsafeUtility.ArrayElementAsRef<T>(m_Data->GetUnsafePtr(), index);
                    }
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
                => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

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
                => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal NativeArray<T>.ReadOnly AsNativeArray()
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                          m_Data->GetUnsafePtr()
                        , m_Data->Capacity
                        , Allocator.None
                    );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

                    return array.AsReadOnly();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public NativeSliceReadOnly<T> AsNativeSliceReadOnly()
                => new(AsNativeArray());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<T> AsReadOnlySpan()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return m_Data->AsReadOnlySpan();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BufferNative<U>.ReadOnly Reinterpret<U>()
                where U : unmanaged
            {
                ThrowHelper.ThrowIfTypesNotEqualSize<T, U>(
                    UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>()
                );

                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    var buffer = default(BufferNative<U>);
                    buffer.m_Data = (BufferUnsafe<U>*)m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    buffer.m_Safety = m_Safety;
#endif

                    return buffer.AsReadOnly();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(BufferNative<T> buffer)
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return new ReadOnly(buffer);
                }
            }
        }

    }

#if UNITY_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct BufferNativeDisposeJob : IJob
    {
        internal BufferNativeDispose _data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Execute()
            => _data.Dispose();
    }

    [NativeContainer]
    internal struct BufferNativeDispose
    {
#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal unsafe BufferUnsafe<int>* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore IDE1006 // Naming Styles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            // SAFETY: The dispose job owns the live buffer header and releases it exactly once.
            unsafe
            {
                Dispose(m_Data);
            }
        }

        /// <safety>data must be a live buffer header returned by the matching allocator and must not be used after this call.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Dispose<T>(BufferUnsafe<T>* data)
            where T : unmanaged
        {
            // SAFETY: The caller supplies the owned header and its allocator is used for the matching release.
            unsafe
            {
                if (data == null)
                {
                    return;
                }

                var allocator = data->_allocator;
                data->Dispose();
                allocator.Free(data);
            }
        }
    }
}
