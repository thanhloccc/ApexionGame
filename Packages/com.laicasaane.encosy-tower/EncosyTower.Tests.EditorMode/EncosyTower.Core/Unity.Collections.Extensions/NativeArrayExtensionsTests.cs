#if UNITY_COLLECTIONS

using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using NativeArrayFactory = EncosyTower.Collections.NativeArray;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeArrayExtensionsTests
    {
        [Test]
        public void AllocatorFactories_CreateWritableCopiesWithoutAliasingSources()
        {
            var sourceValues = new[] { 1, 2, 3, 4 };
            var sourceArray = new NativeArray<int>(sourceValues, Allocator.Temp);
            NativeArray<int> fast = default;
            NativeArray<int> fromSlice = default;
            NativeArray<int> fromSpan = default;
            NativeArray<int> fromReadOnlySpan = default;

            try
            {
                var slice = new NativeSlice<int>(sourceArray, 1, 2);
                Span<int> span = sourceValues;
                ReadOnlySpan<int> readOnlySpan = sourceValues;

                fast = NativeArrayFactory.CreateFast<int>(3, Allocator.Temp);
                fromSlice = NativeArrayFactory.CreateFrom(in slice, Allocator.Temp);
                fromSpan = NativeArrayFactory.CreateFrom(span, Allocator.Temp);
                fromReadOnlySpan = NativeArrayFactory.CreateFrom(readOnlySpan, Allocator.Temp);

                fast[0] = 10;
                fast[2] = 30;
                sourceValues[0] = 99;
                sourceArray[1] = 88;

                Assert.AreEqual(10, fast[0]);
                Assert.AreEqual(30, fast[2]);
                CollectionAssert.AreEqual(new[] { 2, 3 }, fromSlice.ToArray());
                CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, fromSpan.ToArray());
                CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, fromReadOnlySpan.ToArray());
            }
            finally
            {
                EncosyNativeArrayExtensions.DisposeIfCreated(fast);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromSlice);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromSpan);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromReadOnlySpan);
                sourceArray.Dispose();
            }
        }

        [Test]
        public void AllocatorHandleFactories_CreateClearWritableAndCopiedArrays()
        {
            AllocatorManager.AllocatorHandle allocator = AllocatorManager.Persistent;
            var sourceValues = new[] { 4, 5, 6 };
            var sourceArray = new NativeArray<int>(sourceValues, Allocator.Temp);
            NativeArray<int> cleared = default;
            NativeArray<int> fast = default;
            NativeArray<int> fromSlice = default;
            NativeArray<int> fromSpan = default;
            NativeArray<int> fromReadOnlySpan = default;

            try
            {
                var slice = new NativeSlice<int>(sourceArray, 1, 2);
                Span<int> span = sourceValues;
                ReadOnlySpan<int> readOnlySpan = sourceValues;

                cleared = NativeArrayFactory.Create<int>(3, allocator);
                fast = NativeArrayFactory.CreateFast<int>(2, allocator);
                fromSlice = NativeArrayFactory.CreateFrom(in slice, allocator);
                fromSpan = NativeArrayFactory.CreateFrom(span, allocator);
                fromReadOnlySpan = NativeArrayFactory.CreateFrom(readOnlySpan, allocator);

                CollectionAssert.AreEqual(new[] { 0, 0, 0 }, cleared.ToArray());

                fast[0] = 7;
                fast[1] = 8;
                sourceValues[0] = 99;
                sourceArray[1] = 88;

                CollectionAssert.AreEqual(new[] { 7, 8 }, fast.ToArray());
                CollectionAssert.AreEqual(new[] { 5, 6 }, fromSlice.ToArray());
                CollectionAssert.AreEqual(new[] { 4, 5, 6 }, fromSpan.ToArray());
                CollectionAssert.AreEqual(new[] { 4, 5, 6 }, fromReadOnlySpan.ToArray());
            }
            finally
            {
                EncosyNativeArrayExtensions.DisposeIfCreated(cleared);
                EncosyNativeArrayExtensions.DisposeIfCreated(fast);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromSlice);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromSpan);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromReadOnlySpan);
                sourceArray.Dispose();
            }
        }

        [Test]
        public void RewindableAllocatorFactories_CreateClearWritableAndCopiedArrays()
        {
            var allocatorHelper = new AllocatorHelper<RewindableAllocator>(
                AllocatorManager.Persistent
            );
            allocatorHelper.Allocator.Initialize(64 * 1024);
            var sourceValues = new[] { 7, 8, 9 };
            var sourceArray = new NativeArray<int>(sourceValues, Allocator.Temp);
            NativeArray<int> cleared = default;
            NativeArray<int> fast = default;
            NativeArray<int> fromSlice = default;
            NativeArray<int> fromSpan = default;
            NativeArray<int> fromReadOnlySpan = default;

            try
            {
                ref var allocator = ref allocatorHelper.Allocator;
                var slice = new NativeSlice<int>(sourceArray, 1, 2);
                Span<int> span = sourceValues;
                ReadOnlySpan<int> readOnlySpan = sourceValues;

                cleared = NativeArrayFactory.Create<int>(3, allocator);
                fast = NativeArrayFactory.CreateFast<int>(2, allocator);
                fromSlice = NativeArrayFactory.CreateFrom(in slice, allocator);
                fromSpan = NativeArrayFactory.CreateFrom(span, allocator);
                fromReadOnlySpan = NativeArrayFactory.CreateFrom(readOnlySpan, allocator);

                CollectionAssert.AreEqual(new[] { 0, 0, 0 }, cleared.ToArray());

                fast[0] = 11;
                fast[1] = 12;
                sourceValues[0] = 99;
                sourceArray[1] = 88;

                CollectionAssert.AreEqual(new[] { 11, 12 }, fast.ToArray());
                CollectionAssert.AreEqual(new[] { 8, 9 }, fromSlice.ToArray());
                CollectionAssert.AreEqual(new[] { 7, 8, 9 }, fromSpan.ToArray());
                CollectionAssert.AreEqual(new[] { 7, 8, 9 }, fromReadOnlySpan.ToArray());
            }
            finally
            {
                EncosyNativeArrayExtensions.DisposeIfCreated(cleared);
                EncosyNativeArrayExtensions.DisposeIfCreated(fast);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromSlice);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromSpan);
                EncosyNativeArrayExtensions.DisposeIfCreated(fromReadOnlySpan);
                sourceArray.Dispose();
                allocatorHelper.Allocator.Dispose();
                allocatorHelper.Dispose();
            }
        }

        [Test]
        public void NewFillElementAndDisposeMembers_UseRequestedStorageAndState()
        {
            NativeArray<int> cleared = default;
            NativeArray<int> fastAllocator = default;
            NativeArray<int> fastHandle = default;
            NativeArray<int> synchronous = default;
            NativeArray<int> scheduled = default;

            try
            {
                ref var clearedResult = ref EncosyNativeArrayExtensions.New(
                      ref cleared
                    , 4
                    , AllocatorManager.Persistent
                );
                ref var fastAllocatorResult = ref EncosyNativeArrayExtensions.NewFast(
                      ref fastAllocator
                    , 2
                    , Allocator.Persistent
                );
                ref var fastHandleResult = ref EncosyNativeArrayExtensions.NewFast(
                      ref fastHandle
                    , 2
                    , AllocatorManager.Persistent
                );

                CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, clearedResult.ToArray());

                fastAllocatorResult[0] = 1;
                fastAllocatorResult[1] = 2;
                fastHandleResult[0] = 3;
                fastHandleResult[1] = 4;

                EncosyNativeArrayExtensions.Fill(cleared, 5);
                EncosyNativeArrayExtensions.Fill(cleared, 1..3, 9);

                CollectionAssert.AreEqual(new[] { 5, 9, 9, 5 }, cleared.ToArray());
                CollectionAssert.AreEqual(new[] { 1, 2 }, fastAllocator.ToArray());
                CollectionAssert.AreEqual(new[] { 3, 4 }, fastHandle.ToArray());
                Assert.AreEqual(5, EncosyNativeArrayExtensions.ElementAtOrDefault(cleared, 0));
                Assert.AreEqual(0, EncosyNativeArrayExtensions.ElementAtOrDefault(cleared, -1));
                Assert.AreEqual(0, EncosyNativeArrayExtensions.ElementAtOrDefault(cleared, 4));

                synchronous = new NativeArray<int>(1, Allocator.Persistent);
                EncosyNativeArrayExtensions.DisposeIfCreated(synchronous);
                synchronous = default;

                scheduled = new NativeArray<int>(1, Allocator.Persistent);
                JobHandle handle = EncosyNativeArrayExtensions.DisposeIfCreated(scheduled, default);
                handle.Complete();
                scheduled = default;

                var unchanged = EncosyNativeArrayExtensions.DisposeIfCreated(
                      default(NativeArray<int>)
                    , default
                );
                unchanged.Complete();

                EncosyNativeArrayExtensions.DisposeUnset(ref cleared);
                Assert.IsFalse(cleared.IsCreated);
            }
            finally
            {
                EncosyNativeArrayExtensions.DisposeIfCreated(cleared);
                EncosyNativeArrayExtensions.DisposeIfCreated(fastAllocator);
                EncosyNativeArrayExtensions.DisposeIfCreated(fastHandle);
                EncosyNativeArrayExtensions.DisposeIfCreated(synchronous);
                EncosyNativeArrayExtensions.DisposeIfCreated(scheduled);
            }
        }
    }
}

#endif
