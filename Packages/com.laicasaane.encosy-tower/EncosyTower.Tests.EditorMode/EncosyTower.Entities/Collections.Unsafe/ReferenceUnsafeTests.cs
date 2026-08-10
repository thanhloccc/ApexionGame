#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections.Unsafe
{
    public partial class ReferenceUnsafeTests
    {
        [Test]
        public void WorldUpdateAllocator_InSystem_ConstructsUpdatesAndDisposes()
        {
            var world = new World(nameof(WorldUpdateAllocator_InSystem_ConstructsUpdatesAndDisposes));

            try
            {
                var systemHandle = world.CreateSystem<WorldUpdateAllocatorTestSystem>();
                systemHandle.Update(world.Unmanaged);
                ref var system = ref world.Unmanaged.GetUnsafeSystemRef<WorldUpdateAllocatorTestSystem>(
                    systemHandle
                );

                Assert.AreEqual(20, system.value);
                Assert.IsTrue(system.disposed);
            }
            finally
            {
                world.Dispose();
            }
        }

        private partial struct WorldUpdateAllocatorTestSystem : ISystem
        {
            public int value;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                ReferenceUnsafe<int> reference = default;

                try
                {
                    reference = new ReferenceUnsafe<int>(10, state.WorldUpdateAllocator);
                    reference.Value = 20;
                    value = reference.Value;
                }
                finally
                {
                    reference.Dispose();
                }

                disposed = reference.IsCreated == false;
            }
        }
    }
}

#endif
