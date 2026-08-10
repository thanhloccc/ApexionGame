#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Buffers;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Buffers
{
    public partial class BufferUnsafeTests
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

                Assert.AreEqual(4, system.capacity);
                Assert.AreEqual(11, system.first);
                Assert.AreEqual(22, system.second);
                Assert.IsTrue(system.disposed);
            }
            finally
            {
                world.Dispose();
            }
        }

        private partial struct WorldUpdateAllocatorTestSystem : ISystem
        {
            public int capacity;
            public int first;
            public int second;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                BufferUnsafe<int> buffer = default;

                try
                {
                    buffer = new BufferUnsafe<int>(2, state.WorldUpdateAllocator);
                    buffer[0] = 11;
                    buffer[1] = 22;
                    buffer.Resize(4, copyContent: true);
                    capacity = buffer.Capacity;
                    first = buffer[0];
                    second = buffer[1];
                }
                finally
                {
                    buffer.Dispose();
                }

                disposed = buffer.IsCreated == false;
            }
        }
    }
}

#endif
