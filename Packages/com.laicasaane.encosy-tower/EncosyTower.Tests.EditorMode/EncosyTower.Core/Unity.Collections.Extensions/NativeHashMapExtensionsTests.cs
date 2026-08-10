#if UNITY_COLLECTIONS

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeHashMapExtensionsTests
    {
        [Test]
        public void CapacityNewOrClearAndDeconstructMembers_CoverEveryMapVariant()
        {
            NativeHashMap<int, int> map = default;
            NativeParallelHashMap<int, int> parallelMap = default;
            NativeParallelMultiHashMap<int, int> multiMap = default;

            try
            {
                ref var mapResult = ref EncosyNativeHashMapExtensions.NewOrClear(
                      ref map
                    , 4
                    , AllocatorManager.Persistent
                );
                ref var parallelResult = ref EncosyNativeHashMapExtensions.NewOrClear(
                      ref parallelMap
                    , 4
                    , AllocatorManager.Persistent
                );
                ref var multiResult = ref EncosyNativeHashMapExtensions.NewOrClear(
                      ref multiMap
                    , 4
                    , AllocatorManager.Persistent
                );

                mapResult.Add(1, 10);
                parallelResult.Add(2, 20);
                multiResult.Add(3, 30);

                Assert.AreEqual(1, map.Count);
                Assert.AreEqual(1, parallelMap.Count());
                Assert.AreEqual(1, multiMap.Count());

                var mapCapacity = map.Capacity;
                EncosyNativeHashMapExtensions.IncreaseCapacityBy(map, 3);
                Assert.GreaterOrEqual(map.Capacity, mapCapacity + 3);

                var parallelCapacity = parallelMap.Capacity;
                EncosyNativeHashMapExtensions.IncreaseCapacityBy(parallelMap, 3);
                Assert.GreaterOrEqual(parallelMap.Capacity, parallelCapacity + 3);

                var multiCapacity = multiMap.Capacity;
                EncosyNativeHashMapExtensions.IncreaseCapacityBy(multiMap, 3);
                Assert.GreaterOrEqual(multiMap.Capacity, multiCapacity + 3);

                var mapTarget = map.Capacity + 2;
                var parallelTarget = parallelMap.Capacity + 2;
                var multiTarget = multiMap.Capacity + 2;

                EncosyNativeHashMapExtensions.IncreaseCapacityTo(map, mapTarget);
                EncosyNativeHashMapExtensions.IncreaseCapacityTo(parallelMap, parallelTarget);
                EncosyNativeHashMapExtensions.IncreaseCapacityTo(multiMap, multiTarget);

                Assert.GreaterOrEqual(map.Capacity, mapTarget);
                Assert.GreaterOrEqual(parallelMap.Capacity, parallelTarget);
                Assert.GreaterOrEqual(multiMap.Capacity, multiTarget);

                var fixedMapCapacity = map.Capacity;
                var fixedParallelCapacity = parallelMap.Capacity;
                var fixedMultiCapacity = multiMap.Capacity;

                EncosyNativeHashMapExtensions.IncreaseCapacityTo(map, fixedMapCapacity - 1);
                EncosyNativeHashMapExtensions.IncreaseCapacityTo(
                      parallelMap
                    , fixedParallelCapacity - 1
                );
                EncosyNativeHashMapExtensions.IncreaseCapacityTo(multiMap, fixedMultiCapacity - 1);

                Assert.AreEqual(fixedMapCapacity, map.Capacity);
                Assert.AreEqual(fixedParallelCapacity, parallelMap.Capacity);
                Assert.AreEqual(fixedMultiCapacity, multiMap.Capacity);

                var enumerator = map.GetEnumerator();
                Assert.IsTrue(enumerator.MoveNext());
                EncosyNativeHashMapExtensions.Deconstruct(
                      enumerator.Current
                    , out var key
                    , out var value
                );
                enumerator.Dispose();

                Assert.AreEqual(1, key);
                Assert.AreEqual(10, value);

                EncosyNativeHashMapExtensions.NewOrClear(
                      ref map
                    , 1
                    , AllocatorManager.Persistent
                );
                EncosyNativeHashMapExtensions.NewOrClear(
                      ref parallelMap
                    , 1
                    , AllocatorManager.Persistent
                );
                EncosyNativeHashMapExtensions.NewOrClear(
                      ref multiMap
                    , 1
                    , AllocatorManager.Persistent
                );

                Assert.AreEqual(0, map.Count);
                Assert.AreEqual(0, parallelMap.Count());
                Assert.AreEqual(0, multiMap.Count());
            }
            finally
            {
                EncosyNativeHashMapExtensions.DisposeIfCreated(map);
                EncosyNativeHashMapExtensions.DisposeIfCreated(parallelMap);
                EncosyNativeHashMapExtensions.DisposeIfCreated(multiMap);
            }
        }

        [Test]
        public void DisposalMembers_HandleAllMapVariants()
        {
            var mapSync = new NativeHashMap<int, int>(1, AllocatorManager.Persistent);
            var parallelSync = new NativeParallelHashMap<int, int>(1, AllocatorManager.Persistent);
            var multiSync = new NativeParallelMultiHashMap<int, int>(
                  1
                , AllocatorManager.Persistent
            );
            var mapScheduled = new NativeHashMap<int, int>(1, AllocatorManager.Persistent);
            var parallelScheduled = new NativeParallelHashMap<int, int>(
                  1
                , AllocatorManager.Persistent
            );
            var multiScheduled = new NativeParallelMultiHashMap<int, int>(
                  1
                , AllocatorManager.Persistent
            );
            var mapUnset = new NativeHashMap<int, int>(1, AllocatorManager.Persistent);
            var parallelUnset = new NativeParallelHashMap<int, int>(1, AllocatorManager.Persistent);
            var multiUnset = new NativeParallelMultiHashMap<int, int>(
                  1
                , AllocatorManager.Persistent
            );

            try
            {
                EncosyNativeHashMapExtensions.DisposeIfCreated(mapSync);
                mapSync = default;
                EncosyNativeHashMapExtensions.DisposeIfCreated(parallelSync);
                parallelSync = default;
                EncosyNativeHashMapExtensions.DisposeIfCreated(multiSync);
                multiSync = default;

                JobHandle mapHandle = EncosyNativeHashMapExtensions.DisposeIfCreated(
                      mapScheduled
                    , default
                );
                mapHandle.Complete();
                mapScheduled = default;

                JobHandle parallelHandle = EncosyNativeHashMapExtensions.DisposeIfCreated(
                      parallelScheduled
                    , default
                );
                parallelHandle.Complete();
                parallelScheduled = default;

                JobHandle multiHandle = EncosyNativeHashMapExtensions.DisposeIfCreated(
                      multiScheduled
                    , default
                );
                multiHandle.Complete();
                multiScheduled = default;

                EncosyNativeHashMapExtensions.DisposeUnset(ref mapUnset);
                EncosyNativeHashMapExtensions.DisposeUnset(ref parallelUnset);
                EncosyNativeHashMapExtensions.DisposeUnset(ref multiUnset);

                Assert.IsFalse(mapUnset.IsCreated);
                Assert.IsFalse(parallelUnset.IsCreated);
                Assert.IsFalse(multiUnset.IsCreated);
            }
            finally
            {
                EncosyNativeHashMapExtensions.DisposeIfCreated(mapSync);
                EncosyNativeHashMapExtensions.DisposeIfCreated(parallelSync);
                EncosyNativeHashMapExtensions.DisposeIfCreated(multiSync);
                EncosyNativeHashMapExtensions.DisposeIfCreated(mapScheduled);
                EncosyNativeHashMapExtensions.DisposeIfCreated(parallelScheduled);
                EncosyNativeHashMapExtensions.DisposeIfCreated(multiScheduled);
                EncosyNativeHashMapExtensions.DisposeIfCreated(mapUnset);
                EncosyNativeHashMapExtensions.DisposeIfCreated(parallelUnset);
                EncosyNativeHashMapExtensions.DisposeIfCreated(multiUnset);
            }
        }
    }
}

#endif
