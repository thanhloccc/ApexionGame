#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections
{
    public partial class ArrayMapNativeTests
    {
        [Test]
        public void WorldUpdateAllocator_InSystem_ConstructsAndDisposes()
        {
            var world = new World(nameof(WorldUpdateAllocator_InSystem_ConstructsAndDisposes));

            try
            {
                var systemHandle = world.CreateSystem<WorldUpdateAllocatorConstructTestSystem>();
                systemHandle.Update(world.Unmanaged);
                ref var system = ref world.Unmanaged.GetUnsafeSystemRef<
                    WorldUpdateAllocatorConstructTestSystem
                >(systemHandle);

                Assert.AreEqual(1, system.count);
                Assert.AreEqual(4, system.capacity);
                Assert.AreEqual(20, system.value);
                Assert.IsTrue(system.disposed);
            }
            finally
            {
                world.Dispose();
            }
        }

        [Test]
        public void WorldUpdateAllocator_InSystem_GrowsAndPreservesContent()
        {
            var world = new World(nameof(WorldUpdateAllocator_InSystem_GrowsAndPreservesContent));

            try
            {
                var systemHandle = world.CreateSystem<WorldUpdateAllocatorGrowthTestSystem>();
                systemHandle.Update(world.Unmanaged);
                ref var system = ref world.Unmanaged.GetUnsafeSystemRef<
                    WorldUpdateAllocatorGrowthTestSystem
                >(systemHandle);

                Assert.AreEqual(32, system.count);
                Assert.AreEqual(37, system.capacity);
                Assert.IsTrue(system.valuesMatch);
                Assert.IsTrue(system.disposed);
            }
            finally
            {
                world.Dispose();
            }
        }

        private partial struct WorldUpdateAllocatorConstructTestSystem : ISystem
        {
            public int count;
            public int capacity;
            public int value;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                ArrayMapNative<int, int> map = default;

                try
                {
                    map = new ArrayMapNative<int, int>(4, state.WorldUpdateAllocator);
                    map.Add(2, 20);
                    count = map.Count;
                    capacity = map.Capacity;
                    value = map[2];
                }
                finally
                {
                    map.Dispose();
                }

                disposed = map.IsCreated == false;
            }
        }

        private partial struct WorldUpdateAllocatorGrowthTestSystem : ISystem
        {
            public int count;
            public int capacity;
            public bool valuesMatch;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                ArrayMapNative<int, int> map = default;

                try
                {
                    map = new ArrayMapNative<int, int>(1, state.WorldUpdateAllocator);

                    for (var i = 0; i < 32; i++)
                    {
                        map.Add(i, i * 3);
                    }

                    count = map.Count;
                    capacity = map.Capacity;
                    valuesMatch = true;

                    for (var i = 0; i < 32; i++)
                    {
                        if (map.TryGetValue(i, out var value) == false || value != i * 3)
                        {
                            valuesMatch = false;
                        }
                    }
                }
                finally
                {
                    map.Dispose();
                }

                disposed = map.IsCreated == false;
            }
        }
    }
}

#endif
