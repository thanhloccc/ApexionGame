#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Buffers;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Buffers
{
    public partial class AllocatorStrategyTests
    {
        [Test]
        public void WorldUpdateAllocator_InSystem_ResolvesCustomAllocator()
        {
            var world = new World(nameof(WorldUpdateAllocator_InSystem_ResolvesCustomAllocator));

            try
            {
                var systemHandle = world.CreateSystem<WorldUpdateAllocatorTestSystem>();
                systemHandle.Update(world.Unmanaged);

                ref var system = ref world.Unmanaged.GetUnsafeSystemRef<WorldUpdateAllocatorTestSystem>(
                    systemHandle
                );

                Assert.IsTrue(system.allocatorResolved);
                Assert.IsTrue(system.allocatorMatches);
                Assert.IsTrue(system.allocatorHandleHidden);
                Assert.IsTrue(system.customHandleResolved);
                Assert.IsTrue(system.customHandleIsCustom);
                Assert.AreEqual(73, system.value);
                Assert.IsTrue(system.disposed);
            }
            finally
            {
                world.Dispose();
            }
        }

        private partial struct WorldUpdateAllocatorTestSystem : ISystem
        {
            public bool allocatorResolved;
            public bool allocatorMatches;
            public bool allocatorHandleHidden;
            public bool customHandleResolved;
            public bool customHandleIsCustom;
            public int value;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                var allocator = state.WorldUpdateAllocator;
                var strategy = new AllocatorStrategy(allocator);
                allocatorResolved = strategy.TryGetAllocator(out var resolvedAllocator);
                allocatorMatches = resolvedAllocator == allocator;
                allocatorHandleHidden = strategy.TryGetAllocatorHandle(out _) == false;
                customHandleResolved = strategy.TryGetCustomAllocatorHandle(out var customHandle);
                customHandleIsCustom = customHandle.IsCustomAllocator;

                // SAFETY: The pointer is allocated, accessed, and freed with the same live world allocator.
                unsafe
                {
                    int* pointer = null;

                    try
                    {
                        pointer = strategy.Allocate<int>();
                        *pointer = 73;
                        value = *pointer;
                    }
                    finally
                    {
                        strategy.Free(pointer);
                        pointer = null;
                    }

                    disposed = pointer == null;
                }
            }
        }
    }
}

#endif
