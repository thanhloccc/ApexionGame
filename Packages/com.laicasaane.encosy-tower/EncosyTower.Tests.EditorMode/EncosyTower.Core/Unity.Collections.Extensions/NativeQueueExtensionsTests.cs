#if UNITY_COLLECTIONS

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeQueueExtensionsTests
    {
        [Test]
        public void NewOrClear_CreatesDefaultQueueAndClearsPopulatedQueue()
        {
            NativeQueue<int> queue = default;

            try
            {
                EncosyNativeQueueExtensions.NewOrClear(ref queue, AllocatorManager.Persistent);

                Assert.IsTrue(queue.IsCreated);

                queue.Enqueue(1);
                queue.Enqueue(2);
                EncosyNativeQueueExtensions.NewOrClear(ref queue, AllocatorManager.Persistent);

                Assert.AreEqual(0, queue.Count);
            }
            finally
            {
                EncosyNativeQueueExtensions.DisposeIfCreated(queue);
            }
        }

        [Test]
        public void DisposalMembers_HandleCreatedAndDefaultQueues()
        {
            var synchronous = new NativeQueue<int>(AllocatorManager.Persistent);
            var scheduled = new NativeQueue<int>(AllocatorManager.Persistent);
            var unset = new NativeQueue<int>(AllocatorManager.Persistent);

            try
            {
                EncosyNativeQueueExtensions.DisposeIfCreated(synchronous);
                synchronous = default;

                JobHandle handle = EncosyNativeQueueExtensions.DisposeIfCreated(scheduled, default);
                handle.Complete();
                scheduled = default;

                EncosyNativeQueueExtensions.DisposeIfCreated(default(NativeQueue<int>));
                var unchanged = EncosyNativeQueueExtensions.DisposeIfCreated(
                      default(NativeQueue<int>)
                    , default
                );
                unchanged.Complete();

                EncosyNativeQueueExtensions.DisposeUnset(ref unset);
                Assert.IsFalse(unset.IsCreated);
            }
            finally
            {
                EncosyNativeQueueExtensions.DisposeIfCreated(synchronous);
                EncosyNativeQueueExtensions.DisposeIfCreated(scheduled);
                EncosyNativeQueueExtensions.DisposeIfCreated(unset);
            }
        }
    }
}

#endif
