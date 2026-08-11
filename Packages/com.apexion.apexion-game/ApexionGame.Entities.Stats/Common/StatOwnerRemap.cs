using System.Runtime.CompilerServices;
using EncosyTower.Common;
using Unity.Collections;

namespace ApexionGame.Entities.Stats
{
    /// <summary>
    /// Maps stat owner handles captured in a saved state onto the handles they were restored as.
    /// </summary>
    /// <remarks>
    /// <see cref="StatIndex"/> is never remapped: restoring an owner preserves the order of its stat buffer.
    /// </remarks>
    public readonly struct StatOwnerRemap : IIsCreated
    {
        private readonly NativeHashMap<StatOwnerHandle, StatOwnerHandle> _map;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatOwnerRemap(NativeHashMap<StatOwnerHandle, StatOwnerHandle> map)
        {
            _map = map;
        }

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _map.IsCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryRemap(StatOwnerHandle old, out StatOwnerHandle current)
        {
            if (_map.IsCreated == false)
            {
                current = StatOwnerHandle.Null;
                return false;
            }

            return _map.TryGetValue(old, out current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryRemap(StatHandle old, out StatHandle current)
        {
            if (TryRemap(old.owner, out var owner) == false)
            {
                current = StatHandle.Null;
                return false;
            }

            current = new StatHandle(owner, old.index);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StatHandle RemapOrNull(StatHandle old)
            => TryRemap(old, out var current) ? current : StatHandle.Null;
    }
}
