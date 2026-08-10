#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections.Unsafe
{
    public partial class QueueUnsafeTests
    {
        [Test]
        public void WorldUpdateAllocator_InSystem_GrowsAndPreservesContent()
        {
            var world = new World(nameof(WorldUpdateAllocator_InSystem_GrowsAndPreservesContent));

            try
            {
                var systemHandle = world.CreateSystem<WorldUpdateAllocatorTestSystem>();
                systemHandle.Update(world.Unmanaged);
                ref var system = ref world.Unmanaged.GetUnsafeSystemRef<WorldUpdateAllocatorTestSystem>(
                    systemHandle
                );

                Assert.AreEqual(3, system.count);
                Assert.AreEqual(4, system.capacity);
                Assert.AreEqual(10, system.first);
                Assert.AreEqual(20, system.second);
                Assert.AreEqual(30, system.third);
                Assert.IsTrue(system.disposed);
            }
            finally
            {
                world.Dispose();
            }
        }

        private partial struct WorldUpdateAllocatorTestSystem : ISystem
        {
            public int count;
            public int capacity;
            public int first;
            public int second;
            public int third;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                QueueUnsafe<int> queue = default;

                try
                {
                    queue = new QueueUnsafe<int>(1, state.WorldUpdateAllocator);
                    queue.Enqueue(10);
                    queue.Enqueue(20);
                    queue.Enqueue(30);
                    count = queue.Count;
                    capacity = queue.Capacity;
                    first = queue.Dequeue();
                    second = queue.Dequeue();
                    third = queue.Dequeue();
                }
                finally
                {
                    queue.Dispose();
                }

                disposed = queue.IsCreated == false;
            }
        }
    }
}

#endif
