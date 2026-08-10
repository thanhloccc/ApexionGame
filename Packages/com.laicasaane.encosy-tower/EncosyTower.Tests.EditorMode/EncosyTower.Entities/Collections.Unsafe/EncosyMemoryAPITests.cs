#if UNITY_COLLECTIONS && UNITY_ENTITIES

using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Entities;

namespace EncosyTower.Tests.Entities.Collections.Unsafe
{
    public partial class EncosyMemoryAPITests
    {
        [Test]
        public void WorldUpdateAllocator_InSystem_ResizePreservesPrefix()
        {
            var world = new World(nameof(WorldUpdateAllocator_InSystem_ResizePreservesPrefix));

            try
            {
                var systemHandle = world.CreateSystem<WorldUpdateAllocatorTestSystem>();
                systemHandle.Update(world.Unmanaged);

                ref var system = ref world.Unmanaged.GetUnsafeSystemRef<WorldUpdateAllocatorTestSystem>(
                    systemHandle
                );

                Assert.AreEqual(11, system.first);
                Assert.AreEqual(22, system.second);
                Assert.AreEqual(33, system.third);
                Assert.AreEqual(3, system.count);
                Assert.IsTrue(system.disposed);
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
            public int third;
            public long count;
            public bool disposed;

            public void OnUpdate(ref SystemState state)
            {
                var allocator = new AllocatorStrategy(state.WorldUpdateAllocator);

                // SAFETY: Each resize replaces the pointer and the final allocation is freed with its final count.
                unsafe
                {
                    int* pointer = null;
                    long pointerCount = 0;

                    try
                    {
                        pointer = EncosyMemoryAPI.Unmanaged.Array.Allocate<int>(2, allocator);
                        pointerCount = 2;
                        pointer[0] = 11;
                        pointer[1] = 22;

                        pointer = EncosyMemoryAPI.Unmanaged.Array.Resize(
                              pointer
                            , pointerCount
                            , 4
                            , allocator
                        );
                        pointerCount = 4;
                        first = pointer[0];
                        second = pointer[1];
                        pointer[2] = 33;
                        pointer[3] = 44;

                        pointer = EncosyMemoryAPI.Unmanaged.Array.Resize(
                              pointer
                            , pointerCount
                            , 3
                            , allocator
                        );
                        pointerCount = 3;
                        first = pointer[0];
                        second = pointer[1];
                        third = pointer[2];
                        count = pointerCount;
                    }
                    finally
                    {
                        EncosyMemoryAPI.Unmanaged.Array.Free(pointer, pointerCount, allocator);
                        pointer = null;
                    }

                    disposed = pointer == null;
                }
            }
        }
    }
}

#endif
