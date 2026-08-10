#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections
{
    public partial class StackNativeTests
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
                Assert.AreEqual(30, system.first);
                Assert.AreEqual(20, system.second);
                Assert.AreEqual(10, system.third);
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
                StackNative<int> stack = default;

                try
                {
                    stack = new StackNative<int>(1, state.WorldUpdateAllocator);
                    stack.Push(10);
                    stack.Push(20);
                    stack.Push(30);
                    count = stack.Count;
                    capacity = stack.Capacity;
                    first = stack.Pop();
                    second = stack.Pop();
                    third = stack.Pop();
                }
                finally
                {
                    stack.Dispose();
                }

                disposed = stack.IsCreated == false;
            }
        }
    }
}

#endif
