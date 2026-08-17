using System;
using System.Runtime.CompilerServices;
using EncosyTower.Collections;
using Unity.Mathematics;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Geometry only: who a swing or a shot reaches. Knows nothing about health or damage.
    /// </summary>
    /// <remarks>
    /// Results are appended to a caller-supplied list so a caller can rent one from
    /// <c>FasterListPool</c> and keep the whole path allocation-free.
    /// </remarks>
    public static class HitQuery
    {
        /// <summary>
        /// Distance below which two combatants count as overlapping and the arc test is skipped.
        /// </summary>
        private const float OVERLAP_EPSILON_SQ = 1e-6f;

        /// <summary>
        /// Every living enemy inside the swing arc — a swing sweeps through several bodies.
        /// </summary>
        /// <returns>How many entries were appended to <paramref name="results"/>.</returns>
        public static int Melee(
              ReadOnlySpan<CombatantSpatial> spatials
            , in CombatantSpatial attacker
            , float range
            , float radius
            , float arcDegrees
            , FasterList<CombatantId> results
        )
        {
            if (range <= 0f && radius <= 0f)
            {
                return 0;
            }

            var cosHalfArc = math.cos(math.radians(math.clamp(arcDegrees, 0f, 360f) * 0.5f));
            var added = 0;

            for (var i = 0; i < spatials.Length; i++)
            {
                ref readonly var candidate = ref spatials[i];

                if (IsValidTarget(attacker, candidate) == false)
                {
                    continue;
                }

                var delta = candidate.Position - attacker.Position;
                var reach = range + radius + candidate.Radius;
                var distanceSq = math.lengthsq(delta);

                if (distanceSq > reach * reach)
                {
                    continue;
                }

                // Standing inside the attacker: no meaningful direction to test against.
                if (distanceSq > OVERLAP_EPSILON_SQ
                    && math.dot(delta * math.rsqrt(distanceSq), attacker.Forward) < cosHalfArc
                )
                {
                    continue;
                }

                results.Add(candidate.Id);
                added++;
            }

            return added;
        }

        /// <summary>
        /// The nearest living enemy the shot line reaches — a bullet stops at the first body.
        /// </summary>
        /// <returns>1 if something was hit, otherwise 0.</returns>
        public static int Ranged(
              ReadOnlySpan<CombatantSpatial> spatials
            , in CombatantSpatial attacker
            , float range
            , float projectileRadius
            , FasterList<CombatantId> results
        )
        {
            if (range <= 0f)
            {
                return 0;
            }

            var nearestDistance = float.MaxValue;
            var nearest = default(CombatantId);
            var found = false;

            for (var i = 0; i < spatials.Length; i++)
            {
                ref readonly var candidate = ref spatials[i];

                if (IsValidTarget(attacker, candidate) == false)
                {
                    continue;
                }

                var toTarget = candidate.Position - attacker.Position;
                var along = math.dot(toTarget, attacker.Forward);

                if (along < 0f || along > range)
                {
                    continue;
                }

                var hitRadius = candidate.Radius + math.max(0f, projectileRadius);
                var perpendicularSq = math.lengthsq(toTarget) - (along * along);

                if (perpendicularSq > hitRadius * hitRadius)
                {
                    continue;
                }

                if (along >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = along;
                nearest = candidate.Id;
                found = true;
            }

            if (found == false)
            {
                return 0;
            }

            results.Add(nearest);
            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidTarget(in CombatantSpatial attacker, in CombatantSpatial candidate)
            => candidate.IsAlive
            && candidate.Id != attacker.Id
            && candidate.Team != attacker.Team;
    }
}
