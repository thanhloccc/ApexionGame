#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections
{
    public partial class ListNativeTests
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
                ListNative<int> list = default;

                try
                {
                    list = new ListNative<int>(1, state.WorldUpdateAllocator);
                    list.Add(10);
                    list.Add(20);
                    list.Add(30);
                    count = list.Count;
                    capacity = list.Capacity;
                    first = list[0];
                    second = list[1];
                    third = list[2];
                }
                finally
                {
                    list.Dispose();
                }

                disposed = list.IsCreated == false;
            }
        }
    }
}

#endif
