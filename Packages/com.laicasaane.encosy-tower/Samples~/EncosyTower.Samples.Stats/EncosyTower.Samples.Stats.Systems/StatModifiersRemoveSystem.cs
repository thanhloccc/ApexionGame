using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace EncosyTower.Samples.Stats
{
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial struct StatModifiersRemoveSystem : ISystem
    {
        private StatSystem.Accessor _accessor;
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _accessor = new(ref state);

            _query = SystemAPI.QueryBuilder()
                .WithDisabled<IsLivingTag>()
                .WithAllRW<ModifierHandle>()
                .Build();

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _accessor.Update(ref state);

            var handleQueue = new NativeQueue<ModifierHandle>(state.WorldUpdateAllocator);

            state.Dependency = new CollectOutOfTimeModifiersJob {
                handleWriter = handleQueue.AsParallelWriter(),
            }.ScheduleParallel(_query, state.Dependency);

            state.Dependency = new RemoveCollectedModifiersJob {
                accessor = _accessor,
                handleQueue = handleQueue,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct CollectOutOfTimeModifiersJob : IJobEntity
        {
            public NativeQueue<ModifierHandle>.ParallelWriter handleWriter;

            public void Execute(ref DynamicBuffer<ModifierHandle> modifierHandleBuffer)
            {
                var length = modifierHandleBuffer.Length;

                for (var i = 0; i < length; i++)
                {
                    handleWriter.Enqueue(modifierHandleBuffer[i]);
                }

                modifierHandleBuffer.Clear();
            }
        }

        [BurstCompile]
        private struct RemoveCollectedModifiersJob : IJob
        {
            public StatSystem.Accessor accessor;
            public NativeQueue<ModifierHandle> handleQueue;

            public void Execute()
            {
                if (handleQueue.Count < 1)
                {
                    return;
                }

                var worldData = new StatSystem.WorldData(handleQueue.Count, Allocator.Temp);

                while (handleQueue.TryDequeue(out var handle))
                {
                    accessor.TryRemoveStatModifier(handle.value, ref worldData);
                }
            }
        }
    }
}
