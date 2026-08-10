#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections.Unsafe
{
    public partial class ArrayMapUnsafeTests
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

        private partial struct WorldUpdateAllocatorTestSystem : ISystem
        {
            public int count;
            public int capacity;
            public bool valuesMatch;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                ArrayMapUnsafe<int, int> map = default;

                try
                {
                    map = new ArrayMapUnsafe<int, int>(1, state.WorldUpdateAllocator);

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
