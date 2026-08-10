using EncosyTower.Buffers;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Tests.Core.Buffers
{
    public partial class AllocatorStrategyTests
    {
        [Test]
        public void Default_IsInvalid()
        {
            AllocatorStrategy strategy = default;

            Assert.IsFalse(strategy.IsValid);
            Assert.IsFalse(strategy.TryGetAllocator(out var allocator));
            Assert.AreEqual(default(Allocator), allocator);
            Assert.AreEqual(Allocator.Invalid, strategy.ToAllocator());

#if UNITY_COLLECTIONS
            Assert.IsFalse(strategy.TryGetAllocatorHandle(out var handle));
            Assert.AreEqual(default(AllocatorManager.AllocatorHandle), handle);
#endif
        }

        [Test]
        public void Allocator_ConstructorAndImplicitConversion_RoundTrip()
        {
            var constructed = new AllocatorStrategy(Allocator.Temp);
            AllocatorStrategy converted = Allocator.Temp;

            Assert.IsTrue(constructed.IsValid);
            Assert.IsTrue(constructed.TryGetAllocator(out var constructedAllocator));
            Assert.AreEqual(Allocator.Temp, constructedAllocator);
            Assert.AreEqual(Allocator.Temp, constructed.ToAllocator());

            Assert.IsTrue(converted.IsValid);
            Assert.IsTrue(converted.TryGetAllocator(out var convertedAllocator));
            Assert.AreEqual(Allocator.Temp, convertedAllocator);
        }

#if UNITY_COLLECTIONS
        [Test]
        public void AllocatorHandle_ConstructorAndImplicitConversion_RoundTrip()
        {
            AllocatorManager.AllocatorHandle source = AllocatorManager.Persistent;
            var constructed = new AllocatorStrategy(source);
            AllocatorStrategy converted = source;

            Assert.IsTrue(constructed.IsValid);
            Assert.IsTrue(constructed.TryGetAllocatorHandle(out var constructedHandle));
            Assert.AreEqual(source, constructedHandle);
            Assert.IsFalse(constructed.TryGetAllocator(out _));
            Assert.AreEqual(source.ToAllocator, constructed.ToAllocator());

            Assert.IsTrue(converted.TryGetAllocatorHandle(out var convertedHandle));
            Assert.AreEqual(source, convertedHandle);
        }
#endif

        [Test]
        public void RawAllocateAndFree_ReturnAlignedWritableStorage()
        {
            var strategy = new AllocatorStrategy(Allocator.Temp);

            unsafe
            {
                void* pointer = null;

                try
                {
                    pointer = strategy.Allocate(sizeof(int), UnsafeUtility.AlignOf<int>(), 4);

                    Assert.AreNotEqual(System.IntPtr.Zero, (System.IntPtr)pointer);
                    Assert.AreEqual(0, (long)pointer % UnsafeUtility.AlignOf<int>());

                    var values = (int*)pointer;
                    values[0] = 11;
                    values[3] = 44;
                    Assert.AreEqual(11, values[0]);
                    Assert.AreEqual(44, values[3]);
                }
                finally
                {
                    strategy.Free(pointer);
                    pointer = null;
                }
            }
        }

        [Test]
        public void GenericAllocateAndFree_ReturnWritableStorage()
        {
            var strategy = new AllocatorStrategy(Allocator.Temp);

            unsafe
            {
                int* pointer = null;

                try
                {
                    pointer = strategy.Allocate<int>();
                    Assert.AreNotEqual(System.IntPtr.Zero, (System.IntPtr)pointer);

                    *pointer = 73;
                    Assert.AreEqual(73, *pointer);
                }
                finally
                {
                    strategy.Free(pointer);
                    pointer = null;
                }
            }
        }

        [Test]
        public void ArrayAllocateAndFree_ReturnWritableStorage()
        {
            var strategy = new AllocatorStrategy(Allocator.Temp);

            unsafe
            {
                int* pointer = null;

                try
                {
                    pointer = strategy.AllocateArray<int>(4);
                    Assert.AreNotEqual(System.IntPtr.Zero, (System.IntPtr)pointer);

                    pointer[0] = 13;
                    pointer[3] = 46;
                    Assert.AreEqual(13, pointer[0]);
                    Assert.AreEqual(46, pointer[3]);
                }
                finally
                {
                    strategy.FreeArray(pointer, 4);
                    pointer = null;
                }
            }
        }

        [Test]
        public void ZeroLengthArrayAllocation_ReturnsNullAndCanBeFreed()
        {
            var strategy = new AllocatorStrategy(Allocator.Temp);

            unsafe
            {
                var pointer = strategy.AllocateArray<int>(0);

                Assert.AreEqual(System.IntPtr.Zero, (System.IntPtr)pointer);
                strategy.FreeArray(pointer, 0);
            }
        }
    }
}
