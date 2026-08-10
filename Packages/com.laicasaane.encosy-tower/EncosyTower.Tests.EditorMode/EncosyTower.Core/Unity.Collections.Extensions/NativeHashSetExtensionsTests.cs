#if UNITY_COLLECTIONS

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeHashSetExtensionsTests
    {
        [Test]
        public void CapacityAndNewOrClearMembers_CoverBothSetVariants()
        {
            NativeHashSet<int> set = default;
            NativeParallelHashSet<int> parallelSet = default;

            try
            {
                ref var setResult = ref EncosyNativeHashSetExtensions.NewOrClear(
                      ref set
                    , 4
                    , AllocatorManager.Persistent
                );
                ref var parallelResult = ref EncosyNativeHashSetExtensions.NewOrClear(
                      ref parallelSet
                    , 4
                    , AllocatorManager.Persistent
                );

                setResult.Add(1);
                parallelResult.Add(2);

                Assert.AreEqual(1, set.Count);
                Assert.AreEqual(1, parallelSet.Count());

                var setCapacity = set.Capacity;
                var parallelCapacity = parallelSet.Capacity;

                EncosyNativeHashSetExtensions.IncreaseCapacityBy(set, 3);
                EncosyNativeHashSetExtensions.IncreaseCapacityBy(parallelSet, 3);

                Assert.GreaterOrEqual(set.Capacity, setCapacity + 3);
                Assert.GreaterOrEqual(parallelSet.Capacity, parallelCapacity + 3);

                var setTarget = set.Capacity + 2;
                var parallelTarget = parallelSet.Capacity + 2;

                EncosyNativeHashSetExtensions.IncreaseCapacityTo(set, setTarget);
                EncosyNativeHashSetExtensions.IncreaseCapacityTo(parallelSet, parallelTarget);

                Assert.GreaterOrEqual(set.Capacity, setTarget);
                Assert.GreaterOrEqual(parallelSet.Capacity, parallelTarget);

                var fixedSetCapacity = set.Capacity;
                var fixedParallelCapacity = parallelSet.Capacity;

                EncosyNativeHashSetExtensions.IncreaseCapacityTo(set, fixedSetCapacity - 1);
                EncosyNativeHashSetExtensions.IncreaseCapacityTo(
                      parallelSet
                    , fixedParallelCapacity - 1
                );

                Assert.AreEqual(fixedSetCapacity, set.Capacity);
                Assert.AreEqual(fixedParallelCapacity, parallelSet.Capacity);

                EncosyNativeHashSetExtensions.NewOrClear(
                      ref set
                    , 1
                    , AllocatorManager.Persistent
                );
                EncosyNativeHashSetExtensions.NewOrClear(
                      ref parallelSet
                    , 1
                    , AllocatorManager.Persistent
                );

                Assert.AreEqual(0, set.Count);
                Assert.AreEqual(0, parallelSet.Count());
            }
            finally
            {
                EncosyNativeHashSetExtensions.DisposeIfCreated(set);
                EncosyNativeHashSetExtensions.DisposeIfCreated(parallelSet);
            }
        }

        [Test]
        public void DisposalMembers_HandleBothSetVariants()
        {
            var setSync = new NativeHashSet<int>(1, AllocatorManager.Persistent);
            var parallelSync = new NativeParallelHashSet<int>(1, AllocatorManager.Persistent);
            var setScheduled = new NativeHashSet<int>(1, AllocatorManager.Persistent);
            var parallelScheduled = new NativeParallelHashSet<int>(1, AllocatorManager.Persistent);
            var setUnset = new NativeHashSet<int>(1, AllocatorManager.Persistent);
            var parallelUnset = new NativeParallelHashSet<int>(1, AllocatorManager.Persistent);

            try
            {
                EncosyNativeHashSetExtensions.DisposeIfCreated(setSync);
                setSync = default;
                EncosyNativeHashSetExtensions.DisposeIfCreated(parallelSync);
                parallelSync = default;

                JobHandle setHandle = EncosyNativeHashSetExtensions.DisposeIfCreated(
                      setScheduled
                    , default
                );
                setHandle.Complete();
                setScheduled = default;

                JobHandle parallelHandle = EncosyNativeHashSetExtensions.DisposeIfCreated(
                      parallelScheduled
                    , default
                );
                parallelHandle.Complete();
                parallelScheduled = default;

                EncosyNativeHashSetExtensions.DisposeUnset(ref setUnset);
                EncosyNativeHashSetExtensions.DisposeUnset(ref parallelUnset);

                Assert.IsFalse(setUnset.IsCreated);
                Assert.IsFalse(parallelUnset.IsCreated);
            }
            finally
            {
                EncosyNativeHashSetExtensions.DisposeIfCreated(setSync);
                EncosyNativeHashSetExtensions.DisposeIfCreated(parallelSync);
                EncosyNativeHashSetExtensions.DisposeIfCreated(setScheduled);
                EncosyNativeHashSetExtensions.DisposeIfCreated(parallelScheduled);
                EncosyNativeHashSetExtensions.DisposeIfCreated(setUnset);
                EncosyNativeHashSetExtensions.DisposeIfCreated(parallelUnset);
            }
        }
    }
}

#endif
