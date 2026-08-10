#if UNITY_COLLECTIONS

using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeListExtensionsTests
    {
        [Test]
        public void CollectionMembers_ManageCapacityRangesViewsAndDefaultAccess()
        {
            NativeList<int> list = default;

            try
            {
                EncosyNativeListExtensions.NewOrClear(
                      ref list
                    , 4
                    , AllocatorManager.Persistent
                );

                Assert.IsTrue(list.IsCreated);
                Assert.GreaterOrEqual(list.Capacity, 4);

                var capacity = list.Capacity;
                EncosyNativeListExtensions.IncreaseCapacityBy(list, 3);
                Assert.GreaterOrEqual(list.Capacity, capacity + 3);

                var target = list.Capacity + 2;
                EncosyNativeListExtensions.IncreaseCapacityTo(list, target);
                Assert.GreaterOrEqual(list.Capacity, target);

                var fixedCapacity = list.Capacity;
                EncosyNativeListExtensions.IncreaseCapacityTo(list, fixedCapacity - 1);
                Assert.AreEqual(fixedCapacity, list.Capacity);

                list.Add(1);
                list.Add(4);
                ReadOnlySpan<int> tail = new[] { 5, 6 };
                EncosyNativeListExtensions.AddRange(list, tail);

                CollectionAssert.AreEqual(
                      new[] { 1, 4, 5, 6 }
                    , EncosyNativeListExtensions.AsReadOnlySpan(list).ToArray()
                );

                list.Clear();
                list.Add(1);
                list.Add(4);

                Span<int> inserted = EncosyNativeListExtensions.InsertRangeSpan(list, 1, 2);
                inserted[0] = 2;
                inserted[1] = 3;

                CollectionAssert.AreEqual(
                      new[] { 1, 2, 3, 4 }
                    , EncosyNativeListExtensions.AsReadOnlySpan(list).ToArray()
                );

                Span<int> insertedWithEnd = EncosyNativeListExtensions.InsertRangeWithBeginEndSpan(
                      list
                    , 2
                    , 4
                );
                insertedWithEnd[0] = 20;
                insertedWithEnd[1] = 30;

                Assert.AreEqual(2, insertedWithEnd.Length);
                CollectionAssert.AreEqual(
                      new[] { 1, 2, 20, 30, 3, 4 }
                    , EncosyNativeListExtensions.AsReadOnlySpan(list).ToArray()
                );

                Span<int> emptyInsert = EncosyNativeListExtensions.InsertRangeWithBeginEndSpan(
                      list
                    , 1
                    , 1
                );
                Assert.IsTrue(emptyInsert.IsEmpty);

                Span<int> writable = EncosyNativeListExtensions.AsSpan(list);
                writable[0] = 10;
                ReadOnlySpan<int> readOnly = EncosyNativeListExtensions.AsReadOnlySpan(list);

                Assert.AreEqual(10, readOnly[0]);
                Assert.AreEqual(10, EncosyNativeListExtensions.ElementAtOrDefault(list, 0));
                Assert.AreEqual(0, EncosyNativeListExtensions.ElementAtOrDefault(list, -1));
                Assert.AreEqual(
                      0
                    , EncosyNativeListExtensions.ElementAtOrDefault(list, list.Length)
                );

                EncosyNativeListExtensions.NewOrClear(
                      ref list
                    , 1
                    , AllocatorManager.Persistent
                );

                Assert.AreEqual(0, list.Length);
            }
            finally
            {
                EncosyNativeListExtensions.DisposeIfCreated(list);
            }
        }

        [Test]
        public void DisposalMembers_HandleCreatedAndDefaultLists()
        {
            var synchronous = new NativeList<int>(1, AllocatorManager.Persistent);
            var scheduled = new NativeList<int>(1, AllocatorManager.Persistent);
            var unset = new NativeList<int>(1, AllocatorManager.Persistent);

            try
            {
                EncosyNativeListExtensions.DisposeIfCreated(synchronous);
                synchronous = default;

                JobHandle handle = EncosyNativeListExtensions.DisposeIfCreated(scheduled, default);
                handle.Complete();
                scheduled = default;

                EncosyNativeListExtensions.DisposeIfCreated(default(NativeList<int>));
                var unchanged = EncosyNativeListExtensions.DisposeIfCreated(
                      default(NativeList<int>)
                    , default
                );
                unchanged.Complete();

                EncosyNativeListExtensions.DisposeUnset(ref unset);
                Assert.IsFalse(unset.IsCreated);
            }
            finally
            {
                EncosyNativeListExtensions.DisposeIfCreated(synchronous);
                EncosyNativeListExtensions.DisposeIfCreated(scheduled);
                EncosyNativeListExtensions.DisposeIfCreated(unset);
            }
        }
    }
}

#endif
