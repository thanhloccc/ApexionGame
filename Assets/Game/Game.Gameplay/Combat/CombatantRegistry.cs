using System;
using System.Runtime.CompilerServices;
using EncosyTower.Collections;
using EncosyTower.Common;
using Unity.Mathematics;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Who is where, on which team, and still alive. The only place in combat that knows about space.
    /// </summary>
    /// <remarks>
    /// Storage is a dense array plus an id → index map: queries scan the array, everything else is
    /// an O(1) lookup. Swapping to a spatial hash later is a change confined to this file.
    /// </remarks>
    public sealed class CombatantRegistry : IDisposable
    {
        private readonly FasterList<CombatantSpatial> _dense;
        private readonly ArrayMap<CombatantId, int> _indexOf;

        private bool _isDisposed;

        public CombatantRegistry(int capacity)
        {
            _dense = new FasterList<CombatantSpatial>(capacity);
            _indexOf = new ArrayMap<CombatantId, int>(capacity);
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dense.Count;
        }

        /// <summary>
        /// The contiguous block a hit query walks.
        /// </summary>
        public ReadOnlySpan<CombatantSpatial> Spatials
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dense.AsReadOnlySpan();
        }

        public bool Contains(CombatantId id)
            => _indexOf.ContainsKey(id);

        public Option<CombatantSpatial> Find(CombatantId id)
            => _indexOf.TryGetValue(id, out var index) ? _dense[index] : Option.None;

        public bool Add(CombatantId id, byte team, float radius)
        {
            if (_indexOf.ContainsKey(id))
            {
                return false;
            }

            _indexOf.TryAdd(id, _dense.Count);

            _dense.Add(new CombatantSpatial {
                Id = id,
                Position = float3.zero,
                Forward = math.forward(),
                Radius = math.max(0f, radius),
                Team = team,
                IsAlive = true,
            });

            return true;
        }

        /// <summary>
        /// Removes by swapping the last entry into the hole, then repairing that entry's index.
        /// </summary>
        public bool Remove(CombatantId id)
        {
            if (_indexOf.TryGetValue(id, out var index) == false)
            {
                return false;
            }

            var last = _dense.Count - 1;

            if (index != last)
            {
                var moved = _dense[last];
                _dense[index] = moved;
                _indexOf[moved.Id] = index;
            }

            _dense.RemoveAtSwapBack(last);
            _indexOf.Remove(id);

            return true;
        }

        public bool SetTransform(CombatantId id, float3 position, float3 forward)
        {
            if (_indexOf.TryGetValue(id, out var index) == false)
            {
                return false;
            }

            var spatial = _dense[index];
            spatial.Position = position;
            spatial.Forward = math.normalizesafe(forward, math.forward());
            _dense[index] = spatial;

            return true;
        }

        public bool SetRadius(CombatantId id, float radius)
        {
            if (_indexOf.TryGetValue(id, out var index) == false)
            {
                return false;
            }

            var spatial = _dense[index];
            spatial.Radius = math.max(0f, radius);
            _dense[index] = spatial;

            return true;
        }

        /// <summary>
        /// Mirrors a death into the query array so hit queries stop considering the target.
        /// </summary>
        /// <remarks>
        /// <c>Health</c> remains the source of truth; <see cref="CombatWorld"/> is the single
        /// writer that keeps this copy in step, so the two cannot drift.
        /// </remarks>
        public bool MarkDead(CombatantId id)
        {
            if (_indexOf.TryGetValue(id, out var index) == false)
            {
                return false;
            }

            var spatial = _dense[index];
            spatial.IsAlive = false;
            _dense[index] = spatial;

            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _dense.Clear();
            _indexOf.Dispose();
        }
    }
}
