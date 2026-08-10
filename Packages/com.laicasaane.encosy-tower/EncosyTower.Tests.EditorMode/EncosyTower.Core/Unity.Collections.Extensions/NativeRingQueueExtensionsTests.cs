#if UNITY_COLLECTIONS

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public class NativeRingQueueExtensionsTests
    {
        [Test]
        public void DisposalMembers_HandleCreatedAndDefaultQueues()
        {
            var synchronous = new NativeRingQueue<int>(4, AllocatorManager.Persistent);
            var scheduled = new NativeRingQueue<int>(4, AllocatorManager.Persistent);
            var unset = new NativeRingQueue<int>(4, AllocatorManager.Persistent);

            try
            {
                synchronous.Enqueue(1);
                scheduled.Enqueue(2);
                unset.Enqueue(3);

                EncosyNativeRingQueueExtensions.DisposeIfCreated(synchronous);
                synchronous = default;

                JobHandle handle = EncosyNativeRingQueueExtensions.DisposeIfCreated(
                      scheduled
                    , default
                );
                handle.Complete();
                scheduled = default;

                EncosyNativeRingQueueExtensions.DisposeIfCreated(default(NativeRingQueue<int>));
                var unchanged = EncosyNativeRingQueueExtensions.DisposeIfCreated(
                      default(NativeRingQueue<int>)
                    , default
                );
                unchanged.Complete();

                EncosyNativeRingQueueExtensions.DisposeUnset(ref unset);
                Assert.IsFalse(unset.IsCreated);
            }
            finally
            {
                EncosyNativeRingQueueExtensions.DisposeIfCreated(synchronous);
                EncosyNativeRingQueueExtensions.DisposeIfCreated(scheduled);
                EncosyNativeRingQueueExtensions.DisposeIfCreated(unset);
            }
        }
    }
}

#endif
