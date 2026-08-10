// com.unity.collections copyright © 2024 Unity Technologies
//
// Licensed under the Unity Companion License for Unity-dependent projects
// (see https://unity3d.com/legal/licenses/unity_companion_license).
//
// Unless expressly provided otherwise, the Software under this license
// is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Unsafe
{
    public struct EncosyMemoryAPI
    {
        public const long MAXIMUM_RAM_SIZE_IN_BYTES = 1L << 40; // a terabyte

#if UNITY_BURST
        [Unity.Burst.BurstCompile]
#endif
        public struct DisposeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            /// <safety>ptr is owned by the scheduled dispose job and must not be accessed after Execute.</safety>
            public unsafe void* ptr;
            public AllocatorStrategy allocator;

            public readonly void Execute()
            {
                // SAFETY: The job owns ptr and uses the allocator strategy paired with its allocation.
                unsafe
                {
                    allocator.Free(ptr);
                }
            }
        }

        public struct Unmanaged
        {
            /// <safety>The returned pointer is valid until freed with the matching allocator.</safety>
            public static unsafe void* Allocate(
                  long size
                , int align
                , long count
                , AllocatorStrategy allocator
            )
            {
                // SAFETY: The allocator and unmanaged byte count are supplied by the caller's allocation contract.
                unsafe
                {
                    return Array.Resize(null, 0, count, allocator, size, align);
                }
            }

            /// <safety>pointer must have been allocated by allocator and must not be used after this call.</safety>
            public static unsafe void Free(void* pointer, AllocatorStrategy allocator)
            {
                // SAFETY: The pointer is released only through its matching allocator strategy.
                unsafe
                {
                    if (pointer == null)
                    {
                        return;
                    }

#if UNITY_COLLECTIONS
                    if (allocator.TryGetCustomAllocatorHandle(out var handle))
                    {
                        AllocatorManager.Free(handle, pointer);
                        return;
                    }
#endif

                    UnsafeUtility.FreeTracked(pointer, allocator.ToAllocator());
                }
            }

            /// <safety>The returned pointer is aligned storage for one unmanaged T and must be
            /// freed with allocator.</safety>
            public static unsafe T* Allocate<T>(AllocatorStrategy allocator)
                where T : unmanaged
            {
                // SAFETY: The allocator returns storage sized and aligned for one unmanaged T.
                unsafe
                {
                    return Array.Resize<T>(null, 0, 1, allocator);
                }
            }

            /// <safety>pointer must have been allocated by allocator and must not be used after this call.</safety>
            public static unsafe void Free<T>(T* pointer, AllocatorStrategy allocator)
                where T : unmanaged
            {
                // SAFETY: The pointer is released only through its matching allocator strategy.
                unsafe
                {
                    if (pointer == null)
                    {
                        return;
                    }

                    Array.Resize(pointer, 1, 0, allocator);
                }
            }

            public struct Array
            {
#if UNITY_COLLECTIONS
                private static unsafe void* CustomResize(
                      void* oldPointer
                    , long oldCount
                    , long newCount
                    , AllocatorManager.AllocatorHandle allocator
                    , long size
                    , int align
                )
                {
                    // SAFETY: Unity's allocator contract validates the block range and returns
                    // its managed allocation pointer.
                    unsafe
                    {
                        void* newPointer = null;

                        if (newCount > 0)
                        {
                            newPointer = AllocatorManager.Allocate(
                                  allocator
                                , (int)size
                                , align
                                , (int)newCount
                            );

                            ThrowIfFailedToAllocate(newPointer != null);

                            if (oldCount > 0)
                            {
                                long count = System.Math.Min(oldCount, newCount);
                                long bytesToCopy = count * size;
                                CheckByteCountIsReasonable(bytesToCopy);
                                UnsafeUtility.MemCpy(newPointer, oldPointer, bytesToCopy);
                            }
                        }

                        if (oldCount > 0)
                        {
                            AllocatorManager.Free(
                                  allocator
                                , oldPointer
                                , (int)size
                                , align
                                , (int)oldCount
                            );
                        }

                        return newPointer;
                    }
                }
#endif

                /// <safety>oldPointer must be a live allocation owned by allocator; the returned
                /// allocation replaces it.</safety>
                public static unsafe void* Resize(
                      void* oldPointer
                    , long oldCount
                    , long newCount
                    , AllocatorStrategy allocator
                    , long size
                    , int align
                )
                {
                    // SAFETY: Counts, sizes, and allocator strategy are validated before any copy or free.
                    unsafe
                    {
                        var alignment = System.Math.Max(JobsUtility.CacheLineSize, align);

#if UNITY_COLLECTIONS
                        if (allocator.TryGetCustomAllocatorHandle(out var handle))
                        {
                            return CustomResize(oldPointer, oldCount, newCount, handle, size, alignment);
                        }
#endif

                        void* newPointer = default;

                        if (newCount > 0)
                        {
                            long bytesToAllocate = newCount * size;
                            CheckByteCountIsReasonable(bytesToAllocate);
                            newPointer = UnsafeUtility.MallocTracked(
                                  bytesToAllocate
                                , alignment
                                , allocator.ToAllocator()
                                , 0
                            );

                            if (oldCount > 0)
                            {
                                long count = System.Math.Min(oldCount, newCount);

                                long bytesToCopy = count * size;
                                CheckByteCountIsReasonable(bytesToCopy);
                                UnsafeUtility.MemCpy(newPointer, oldPointer, bytesToCopy);
                            }
                        }

                        if (oldCount > 0)
                        {
                            UnsafeUtility.FreeTracked(oldPointer, allocator.ToAllocator());
                        }

                        return newPointer;
                    }
                }

                /// <safety>oldPointer must be a live allocation of unmanaged T owned by allocator;
                /// the returned allocation replaces it.</safety>
                public static unsafe T* Resize<T>(
                      T* oldPointer
                    , long oldCount
                    , long newCount
                    , AllocatorStrategy allocator
                )
                    where T : unmanaged
                {
                    // SAFETY: The typed pointer is converted to bytes only for the validated resize operation.
                    unsafe
                    {
                        return (T*)Resize(
                              (byte*)oldPointer
                            , oldCount
                            , newCount
                            , allocator
                            , UnsafeUtility.SizeOf<T>()
                            , UnsafeUtility.AlignOf<T>()
                        );
                    }
                }

                /// <safety>The returned pointer is storage for count unmanaged T elements and
                /// must be freed with allocator.</safety>
                public static unsafe T* Allocate<T>(long count, AllocatorStrategy allocator)
                    where T : unmanaged
                {
                    // SAFETY: The resize helper validates the count and returns storage for unmanaged T elements.
                    unsafe
                    {
                        return Resize<T>(null, 0, count, allocator);
                    }
                }

                /// <safety>pointer must be the live allocation for count unmanaged T elements
                /// owned by allocator.</safety>
                public static unsafe void Free<T>(T* pointer, long count, AllocatorStrategy allocator)
                    where T : unmanaged
                {
                    // SAFETY: A null check and matching allocator strategy establish ownership for the release.
                    unsafe
                    {
                        if (pointer == null)
                        {
                            return;
                        }

                        Resize(pointer, count, 0, allocator);
                    }
                }
            }
        }

        public struct Array
        {
            /// <safety>pointer must reference count writable unmanaged T elements.</safety>
            public static unsafe void Set<T>(T* pointer, long count, T t = default)
                where T : unmanaged
            {
                // SAFETY: The validated count bounds every element write in the caller-owned buffer.
                unsafe
                {
                    long bytesToSet = count * UnsafeUtility.SizeOf<T>();
                    CheckByteCountIsReasonable(bytesToSet);

                    for (var i = 0; i < count; ++i)
                    {
                        pointer[i] = t;
                    }
                }
            }

            /// <safety>pointer must reference count writable unmanaged T elements.</safety>
            public static unsafe void Clear<T>(T* pointer, long count)
                where T : unmanaged
            {
                // SAFETY: The validated count bounds the clear operation in the caller-owned buffer.
                unsafe
                {
                    long bytesToClear = count * UnsafeUtility.SizeOf<T>();
                    CheckByteCountIsReasonable(bytesToClear);
                    UnsafeUtility.MemClear(pointer, bytesToClear);
                }
            }

            /// <safety>src and dest must reference live, non-overlapping ranges of count unmanaged T elements.</safety>
            public static unsafe void Copy<T>(T* dest, T* src, long count)
                where T : unmanaged
            {
                // SAFETY: The validated count bounds the copy between caller-owned buffers.
                unsafe
                {
                    long bytesToCopy = count * UnsafeUtility.SizeOf<T>();
                    CheckByteCountIsReasonable(bytesToCopy);
                    UnsafeUtility.MemCpy(dest, src, bytesToCopy);
                }
            }
        }

        public static void CheckByteCountIsReasonable(long size)
        {
            ThrowIfByteCountIsNegative(size < 0, size);
            ThrowIfByteCountExceedsMaximum(size > MAXIMUM_RAM_SIZE_IN_BYTES, size);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfFailedToAllocate([DoesNotReturnIf(false)] bool success)
        {
            if (success == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Failed to allocate.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfByteCountIsNegative(
            [DoesNotReturnIf(true)] bool isNegative
          , long size
        )
        {
            if (isNegative)
            {
                throw CreateException(size);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(long size)
            {
                return new($"Attempted to operate on {size} bytes of memory: negative size.");
            }
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfByteCountExceedsMaximum(
            [DoesNotReturnIf(true)] bool exceedsMaximum
          , long size
        )
        {
            if (exceedsMaximum)
            {
                throw CreateException(size);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(long size)
            {
                return new($"Attempted to operate on {size} bytes of memory: size too big.");
            }
        }
    }
}
