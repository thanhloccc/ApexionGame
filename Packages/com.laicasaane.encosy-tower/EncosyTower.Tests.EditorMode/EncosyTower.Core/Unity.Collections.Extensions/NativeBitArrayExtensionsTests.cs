#if UNITY_COLLECTIONS

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeBitArrayExtensionsTests
    {
        [Test]
        public void CapacityAndNewOrClearMembers_UseRequestedLowerBoundsAndClearBits()
        {
            NativeBitArray array = default;

            try
            {
                EncosyNativeBitArrayExtensions.NewOrClear(
                      ref array
                    , 64
                    , AllocatorManager.Persistent
                );

                Assert.IsTrue(array.IsCreated);
                Assert.GreaterOrEqual(array.Capacity, 64);

                array.Set(5, true);
                var capacity = array.Capacity;

                EncosyNativeBitArrayExtensions.IncreaseCapacityBy(array, 64);
                Assert.GreaterOrEqual(array.Capacity, capacity + 64);

                var increasedCapacity = array.Capacity;
                EncosyNativeBitArrayExtensions.IncreaseCapacityTo(array, increasedCapacity + 64);
                Assert.GreaterOrEqual(array.Capacity, increasedCapacity + 64);

                var finalCapacity = array.Capacity;
                EncosyNativeBitArrayExtensions.IncreaseCapacityTo(array, finalCapacity - 1);
                Assert.AreEqual(finalCapacity, array.Capacity);

                EncosyNativeBitArrayExtensions.NewOrClear(
                      ref array
                    , 64
                    , AllocatorManager.Persistent
                );

                Assert.IsFalse(array.IsSet(5));
            }
            finally
            {
                EncosyNativeBitArrayExtensions.DisposeIfCreated(array);
            }
        }

        [Test]
        public void DisposalMembers_HandleCreatedAndDefaultArrays()
        {
            var synchronous = new NativeBitArray(64, AllocatorManager.Persistent);
            var scheduled = new NativeBitArray(64, AllocatorManager.Persistent);
            var unset = new NativeBitArray(64, AllocatorManager.Persistent);

            EncosyNativeBitArrayExtensions.DisposeIfCreated(synchronous);
            synchronous = default;

            JobHandle handle = EncosyNativeBitArrayExtensions.DisposeIfCreated(scheduled, default);
            handle.Complete();
            scheduled = default;

            var unchanged = EncosyNativeBitArrayExtensions.DisposeIfCreated(default, default);
            unchanged.Complete();

            EncosyNativeBitArrayExtensions.DisposeIfCreated(default);
            EncosyNativeBitArrayExtensions.DisposeUnset(ref unset);

            Assert.IsFalse(unset.IsCreated);
        }
    }
}

#endif
