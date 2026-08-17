using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// The outcome of one damage application.
    /// </summary>
    /// <remarks>
    /// <see cref="JustDied"/> is true on exactly one call — the one that took the pool to zero.
    /// That is what lets a caller emit a death event without tracking whether it already did.
    /// </remarks>
    public readonly record struct HealthChange(float Applied, float Remaining, bool JustDied);

    /// <summary>
    /// Current hit points of one combatant.
    /// </summary>
    /// <remarks>
    /// Deliberately *not* a stat. Stats are values derived from modifiers; current health is
    /// mutated directly by damage. Keeping it here means damage never fights the modifier system
    /// over a base value. Maximum health stays a stat and is read from the store when needed.
    /// </remarks>
    public struct Health
    {
        private float _current;
        private bool _isDead;

        public Health(float current)
        {
            _current = math.max(0f, current);
            _isDead = _current <= 0f;
        }

        public readonly float Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        public readonly bool IsDead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isDead;
        }

        /// <summary>
        /// Removes <paramref name="amount"/> hit points, never below zero.
        /// </summary>
        /// <remarks>
        /// <c>Applied</c> reports what was actually removed, so an overkill hit reports the
        /// remaining health rather than the requested damage.
        /// </remarks>
        public HealthChange ApplyDamage(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return new HealthChange(0f, _current, false);
            }

            var applied = math.min(amount, _current);

            _current -= applied;

            if (_current > 0f)
            {
                return new HealthChange(applied, _current, false);
            }

            _current = 0f;
            _isDead = true;

            return new HealthChange(applied, 0f, true);
        }
    }
}
