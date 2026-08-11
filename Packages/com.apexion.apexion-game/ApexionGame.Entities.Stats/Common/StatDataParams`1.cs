using System.Runtime.CompilerServices;
using EncosyTower.Common;

namespace ApexionGame.Entities.Stats
{
    public readonly struct StatDataParams<TStatData> : IIsCreated
        where TStatData : unmanaged, IStatData
    {
        public readonly Option<TStatData> StatData;
        public readonly Option<StatHandle<TStatData>> Handle;
        public readonly Option<uint> UserData;
        public readonly Option<bool> ProduceChangeEvents;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatDataParams(
              Option<TStatData> statData = default
            , Option<StatHandle<TStatData>> handle = default
            , Option<bool> produceChangeEvents = default
            , Option<uint> userData = default
        )
        {
            StatData = statData;
            Handle = handle;
            UserData = userData;
            ProduceChangeEvents = produceChangeEvents;
        }

        public bool IsCreated
            => StatData.HasValue || Handle.HasValue || UserData.HasValue || ProduceChangeEvents.HasValue;
    }
}
