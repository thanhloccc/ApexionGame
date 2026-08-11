using System.Runtime.CompilerServices;
using EncosyTower.Common;

namespace ApexionGame.Entities.Stats
{
    public readonly struct StatValueParams<TValuePair> : IIsCreated
        where TValuePair : unmanaged, IStatValuePair
    {
        public readonly Option<TValuePair> StatValues;
        public readonly Option<StatHandle> Handle;
        public readonly Option<uint> UserData;
        public readonly Option<bool> ProduceChangeEvents;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatValueParams(
              Option<TValuePair> statValues = default
            , Option<StatHandle> handle = default
            , Option<bool> produceChangeEvents = default
            , Option<uint> userData = default
        )
        {
            StatValues = statValues;
            Handle = handle;
            UserData = userData;
            ProduceChangeEvents = produceChangeEvents;
        }

        public bool IsCreated
            => StatValues.HasValue || Handle.HasValue || UserData.HasValue || ProduceChangeEvents.HasValue;
    }
}
