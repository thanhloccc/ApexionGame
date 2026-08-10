#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections.Unsafe
{
    public partial class ArraySetUnsafeTests
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
                ArraySetUnsafe<int> set = default;

                try
                {
                    set = new ArraySetUnsafe<int>(1, state.WorldUpdateAllocator);

                    for (var i = 0; i < 32; i++)
                    {
                        set.Add(i);
                    }

                    count = set.Count;
                    capacity = set.Capacity;
                    valuesMatch = true;

                    for (var i = 0; i < 32; i++)
                    {
                        if (set.Contains(i) == false)
                        {
                            valuesMatch = false;
                        }
                    }
                }
                finally
                {
                    set.Dispose();
                }

                disposed = set.IsCreated == false;
            }
        }
    }
}

#endif
