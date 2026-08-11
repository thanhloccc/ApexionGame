using System.Runtime.CompilerServices;
using EncosyTower.Common;

namespace ApexionGame.Entities.Stats
{
    /// <summary>
    /// Read-only view over stats, handed to <see cref="IStatModifier{TValuePair, TStat, TStatModifierStack}"/>.
    /// </summary>
    /// <remarks>
    /// Two modes: a <see cref="StatBufferLookup{T}"/> that resolves any owner, or a single
    /// <see cref="StatBuffer{T}"/> when the caller already knows every stat lives on one owner.
    /// </remarks>
    public struct StatReader<TValuePair, TStat> : IIsCreated
        where TValuePair : unmanaged, IStatValuePair
        where TStat : unmanaged, IStat<TValuePair>
    {
        private StatBufferLookup<TStat> _lookupStats;
        private StatBuffer<TStat> _statBuffer;
        private readonly ByteBool _isCreated;
        private readonly ByteBool _useLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal StatReader(StatBufferLookup<TStat> lookupStats) : this()
        {
            _lookupStats = lookupStats;
            _isCreated = true;
            _useLookup = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal StatReader(StatBuffer<TStat> statBuffer) : this()
        {
            _statBuffer = statBuffer;
            _isCreated = true;
            _useLookup = false;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isCreated;
        }

        public readonly bool UseLookup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _useLookup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(StatHandle statHandle)
        {
            return UseLookup
                ? StatAPI.Contains<TValuePair, TStat>(statHandle, _lookupStats)
                : StatAPI.Contains<TValuePair, TStat>(statHandle, _statBuffer.AsReadOnlySpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(StatHandle statHandle, uint userData)
        {
            return UseLookup
                ? StatAPI.Contains<TValuePair, TStat>(statHandle, userData, _lookupStats)
                : StatAPI.Contains<TValuePair, TStat>(statHandle, userData, _statBuffer.AsReadOnlySpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStatData<TStatData>(StatHandle<TStatData> statHandle, out TStatData statData)
            where TStatData : unmanaged, IStatData
        {
            return UseLookup
                ? StatAPI.TryGetStatData<TValuePair, TStat, TStatData>(statHandle, _lookupStats, out statData)
                : StatAPI.TryGetStatData<TValuePair, TStat, TStatData>(
                      statHandle
                    , _statBuffer.AsReadOnlySpan()
                    , out statData
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStat(StatHandle statHandle, out TStat stat)
        {
            return UseLookup
                ? StatAPI.TryGetStat<TValuePair, TStat>(statHandle, _lookupStats, out stat)
                : StatAPI.TryGetStat<TValuePair, TStat>(statHandle, _statBuffer.AsReadOnlySpan(), out stat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStatValue(StatHandle statHandle, out TValuePair valuePair)
        {
            return UseLookup
                ? StatAPI.TryGetStatValue<TValuePair, TStat>(statHandle, _lookupStats, out valuePair)
                : StatAPI.TryGetStatValue<TValuePair, TStat>(
                      statHandle
                    , _statBuffer.AsReadOnlySpan()
                    , out valuePair
                );
        }
    }
}
