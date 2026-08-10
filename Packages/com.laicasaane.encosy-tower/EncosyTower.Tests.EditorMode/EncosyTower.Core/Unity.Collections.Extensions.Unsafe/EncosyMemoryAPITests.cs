#if !(UNITY_EDITOR || DEBUG || ENCOSY_RUNTIME_CHECKS || ENCOSY_COLLECTIONS_RUNTIME_CHECKS || ENABLE_UNITY_COLLECTIONS_CHECKS) || DISABLE_ENCOSY_CHECKS
#define __ENCOSY_NO_VALIDATION__
#else
#define __ENCOSY_VALIDATION__
#endif

using System;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public partial class EncosyMemoryAPITests
    {
        [Test]
        public void MaximumRamSizeAndByteCountChecks_UseDocumentedBoundaries()
        {
            Assert.AreEqual(1L << 40, EncosyMemoryAPI.MAXIMUM_RAM_SIZE_IN_BYTES);
            Assert.DoesNotThrow(() => EncosyMemoryAPI.CheckByteCountIsReasonable(0));
            Assert.DoesNotThrow(
                () => EncosyMemoryAPI.CheckByteCountIsReasonable(
                    EncosyMemoryAPI.MAXIMUM_RAM_SIZE_IN_BYTES
                )
            );

#if __ENCOSY_VALIDATION__
            Assert.Throws<InvalidOperationException>(
                () => EncosyMemoryAPI.CheckByteCountIsReasonable(-1)
            );
            Assert.Throws<InvalidOperationException>(
                () => EncosyMemoryAPI.CheckByteCountIsReasonable(
                    EncosyMemoryAPI.MAXIMUM_RAM_SIZE_IN_BYTES + 1
                )
            );
#endif
        }

        [Test]
        public void UnmanagedRawAllocateAndFree_ProvideWritableAlignedStorage()
        {
            var allocator = new AllocatorStrategy(Allocator.Persistent);

            // SAFETY: The two-integer pointer is freed with its matching allocator.
            unsafe
            {
                void* pointer = null;

                try
                {
                    pointer = EncosyMemoryAPI.Unmanaged.Allocate(
                          UnsafeUtility.SizeOf<int>()
                        , UnsafeUtility.AlignOf<int>()
                        , 2
                        , allocator
                    );

                    Assert.AreNotEqual(IntPtr.Zero, (IntPtr)pointer);

                    var values = (int*)pointer;
                    values[0] = 10;
                    values[1] = 20;

                    Assert.AreEqual(10, values[0]);
                    Assert.AreEqual(20, values[1]);
                }
                finally
                {
                    EncosyMemoryAPI.Unmanaged.Free(pointer, allocator);
                    pointer = null;
                }

                Assert.AreEqual(IntPtr.Zero, (IntPtr)pointer);
            }
        }

        [Test]
        public void UnmanagedGenericAllocateAndFree_ProvideWritableTypedStorage()
        {
            var allocator = new AllocatorStrategy(Allocator.Persistent);

            // SAFETY: The typed pointer owns one integer and is released by its matching allocator.
            unsafe
            {
                int* pointer = null;

                try
                {
                    pointer = EncosyMemoryAPI.Unmanaged.Allocate<int>(allocator);

                    Assert.AreNotEqual(IntPtr.Zero, (IntPtr)pointer);

                    *pointer = 30;

                    Assert.AreEqual(30, *pointer);
                }
                finally
                {
                    EncosyMemoryAPI.Unmanaged.Free<int>(pointer, allocator);
                    pointer = null;
                }

                Assert.AreEqual(IntPtr.Zero, (IntPtr)pointer);
            }
        }

        [Test]
        public void UnmanagedArrayAllocateResizeAndFree_PreserveRequestedPrefix()
        {
            var allocator = new AllocatorStrategy(Allocator.Persistent);

            // SAFETY: Each replacing resize updates the pointer count used by final release.
            unsafe
            {
                int* pointer = null;
                long count = 0;

                try
                {
                    pointer = EncosyMemoryAPI.Unmanaged.Array.Allocate<int>(2, allocator);
                    count = 2;
                    pointer[0] = 1;
                    pointer[1] = 2;

                    pointer = EncosyMemoryAPI.Unmanaged.Array.Resize(pointer, count, 4, allocator);
                    count = 4;

                    Assert.AreEqual(1, pointer[0]);
                    Assert.AreEqual(2, pointer[1]);

                    pointer[2] = 3;
                    pointer[3] = 4;
                    pointer = (int*)EncosyMemoryAPI.Unmanaged.Array.Resize(
                          pointer
                        , count
                        , 3
                        , allocator
                        , UnsafeUtility.SizeOf<int>()
                        , UnsafeUtility.AlignOf<int>()
                    );
                    count = 3;

                    Assert.AreEqual(1, pointer[0]);
                    Assert.AreEqual(2, pointer[1]);
                    Assert.AreEqual(3, pointer[2]);
                }
                finally
                {
                    EncosyMemoryAPI.Unmanaged.Array.Free(pointer, count, allocator);
                    pointer = null;
                    count = 0;
                }

                Assert.AreEqual(IntPtr.Zero, (IntPtr)pointer);
                Assert.AreEqual(0, count);
            }
        }

        [Test]
        public void ArraySetClearAndCopy_MutateOnlyRequestedRanges()
        {
            var allocator = new AllocatorStrategy(Allocator.Persistent);

            // SAFETY: One six-element allocation contains two disjoint three-element copy ranges.
            unsafe
            {
                int* pointer = null;

                try
                {
                    pointer = EncosyMemoryAPI.Unmanaged.Array.Allocate<int>(6, allocator);
                    EncosyMemoryAPI.Array.Set(pointer, 3, 7);
                    EncosyMemoryAPI.Array.Set(pointer + 3, 3);
                    EncosyMemoryAPI.Array.Copy(pointer + 3, pointer, 3);

                    Assert.AreEqual(7, pointer[0]);
                    Assert.AreEqual(7, pointer[3]);
                    Assert.AreEqual(7, pointer[5]);

                    EncosyMemoryAPI.Array.Clear(pointer + 3, 3);

                    Assert.AreEqual(7, pointer[0]);
                    Assert.AreEqual(0, pointer[3]);
                    Assert.AreEqual(0, pointer[5]);
                }
                finally
                {
                    EncosyMemoryAPI.Unmanaged.Array.Free(pointer, 6, allocator);
                    pointer = null;
                }

                Assert.AreEqual(IntPtr.Zero, (IntPtr)pointer);
            }
        }

        [Test]
        public void DisposeJob_ExposesFieldsAndFreesOwnedPointerOnExecute()
        {
            var allocator = new AllocatorStrategy(Allocator.Persistent);

            // SAFETY: The job owns the pointer, which is nulled immediately after Execute.
            unsafe
            {
                int* pointer = null;
                EncosyMemoryAPI.DisposeJob job = default;

                try
                {
                    pointer = EncosyMemoryAPI.Unmanaged.Allocate<int>(allocator);
                    *pointer = 40;
                    job.ptr = pointer;
                    job.allocator = allocator;

                    Assert.AreEqual((IntPtr)pointer, (IntPtr)job.ptr);
                    Assert.IsTrue(job.allocator.TryGetAllocator(out var jobAllocator));
                    Assert.AreEqual(Allocator.Persistent, jobAllocator);

                    job.Execute();
                    job.ptr = null;
                    pointer = null;
                }
                finally
                {
                    EncosyMemoryAPI.Unmanaged.Free(pointer, allocator);
                    pointer = null;
                    job.ptr = null;
                }

                Assert.AreEqual(IntPtr.Zero, (IntPtr)pointer);
                Assert.AreEqual(IntPtr.Zero, (IntPtr)job.ptr);
            }
        }
    }
}
