using System.Runtime.CompilerServices;
using EncosyTower.Common;

namespace ApexionGame.Entities.Stats
{
    public readonly struct StatData<TStatData> : IIsCreated
        where TStatData : unmanaged, IStatData
    {
        public readonly TStatData Data;
        public readonly uint UserData;
        public readonly bool ProduceChangeEvents;
        private readonly bool _isCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatData(TStatData data, bool produceChangeEvents = false, uint userData = default)
        {
            Data = data;
            UserData = userData;
            ProduceChangeEvents = produceChangeEvents;
            _isCreated = true;
        }

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isCreated;
        }
    }
}
