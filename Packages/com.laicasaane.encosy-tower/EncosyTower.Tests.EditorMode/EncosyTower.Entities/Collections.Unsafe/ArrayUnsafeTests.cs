#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections.Unsafe
{
    public partial class ArrayUnsafeTests
    {
        [Test]
        public void WorldUpdateAllocator_InSystem_ConstructsWritesAndDisposes()
        {
            var world = new World(nameof(WorldUpdateAllocator_InSystem_ConstructsWritesAndDisposes));

            try
            {
                var systemHandle = world.CreateSystem<WorldUpdateAllocatorTestSystem>();
                systemHandle.Update(world.Unmanaged);
                ref var system = ref world.Unmanaged.GetUnsafeSystemRef<WorldUpdateAllocatorTestSystem>(
                    systemHandle
                );

                Assert.AreEqual(10, system.first);
                Assert.AreEqual(20, system.second);
                Assert.IsTrue(system.synchronousDisposed);
                Assert.IsTrue(system.scheduledDisposed);
            }
            finally
            {
                world.Dispose();
            }
        }

        private partial struct WorldUpdateAllocatorTestSystem : ISystem
        {
            public int first;
            public int second;
            public bool synchronousDisposed;
            public bool scheduledDisposed;

            public void OnUpdate(ref SystemState state)
            {
                ArrayUnsafe<int> synchronous = default;
                ArrayUnsafe<int> scheduled = default;

                try
                {
                    synchronous = new ArrayUnsafe<int>(2, state.WorldUpdateAllocator);
                    synchronous[0] = 10;
                    synchronous[1] = 20;
                    first = synchronous[0];
                    second = synchronous[1];

                    scheduled = new ArrayUnsafe<int>(1, state.WorldUpdateAllocator);
                    scheduled[0] = 30;

                    synchronous.Dispose();
                    synchronousDisposed = synchronous.IsCreated == false;

                    scheduled.Dispose(state.Dependency).Complete();
                    scheduledDisposed = scheduled.IsCreated == false;
                }
                finally
                {
                    if (synchronous.IsCreated)
                    {
                        synchronous.Dispose();
                    }

                    if (scheduled.IsCreated)
                    {
                        scheduled.Dispose();
                    }
                }
            }
        }
    }
}

#endif
