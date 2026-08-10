using System;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Everything the match needs that is not content. Serialisable, so the scene component can expose it.
    /// </summary>
    [Serializable]
    public struct RtsMatchSettings
    {
        /// <summary>Fixed simulation step. The view interpolates between two of these.</summary>
        public float tickSeconds;

        public int startingSupply;
        public float supplyPerSecond;
        public int supplyCap;

        public int startingFootmen;
        public int startingArchers;

        /// <summary>
        /// The floor under <c>AttackInterval</c>, applied as a <c>ClampMin</c> modifier on every unit.
        /// </summary>
        public float attackIntervalFloor;

        /// <summary>±this fraction of swing damage, so a battle log does not read like a spreadsheet.</summary>
        public float damageVariance;

        /// <summary>How far a unit looks for a target before it just walks at the enemy stronghold.</summary>
        public float acquireRange;

        /// <summary>Half the length of the battlefield along x. Strongholds sit at ±this.</summary>
        public float fieldHalfLength;

        /// <summary>Half the width of the battlefield along z.</summary>
        public float laneHalfWidth;

        public int maxUnitsPerTeam;

        /// <summary>Kills a hero needs per level.</summary>
        public int killsPerHeroLevel;

        /// <summary>Added to a hero's <c>AuraPower</c> base value per level.</summary>
        public float heroAuraPerLevel;

        /// <summary>Fixed, so replaying the same inputs lands on the same result.</summary>
        public uint randomSeed;

        public static RtsMatchSettings Default => new() {
            tickSeconds = 0.05f,
            startingSupply = 120,
            supplyPerSecond = 8f,
            supplyCap = 400,
            startingFootmen = 4,
            startingArchers = 2,
            attackIntervalFloor = 0.25f,
            damageVariance = 0.15f,
            acquireRange = 14f,
            fieldHalfLength = 18f,
            laneHalfWidth = 8f,
            maxUnitsPerTeam = 60,
            killsPerHeroLevel = 3,
            heroAuraPerLevel = 10f,
            randomSeed = 20260805u,
        };

        /// <summary>
        /// Fills in anything left at zero from <see cref="Default"/>.
        /// </summary>
        /// <remarks>
        /// A <c>[SerializeField]</c> struct on a freshly added component is all zeros, and a zero
        /// <see cref="tickSeconds"/> is an infinite loop rather than a slow game.
        /// </remarks>
        public RtsMatchSettings Normalized()
        {
            var d = Default;
            var s = this;

            if (s.tickSeconds <= 0f) s.tickSeconds = d.tickSeconds;
            if (s.startingSupply <= 0) s.startingSupply = d.startingSupply;
            if (s.supplyPerSecond <= 0f) s.supplyPerSecond = d.supplyPerSecond;
            if (s.supplyCap <= 0) s.supplyCap = d.supplyCap;
            if (s.startingFootmen < 0) s.startingFootmen = d.startingFootmen;
            if (s.startingArchers < 0) s.startingArchers = d.startingArchers;
            if (s.attackIntervalFloor <= 0f) s.attackIntervalFloor = d.attackIntervalFloor;
            if (s.damageVariance < 0f) s.damageVariance = d.damageVariance;
            if (s.acquireRange <= 0f) s.acquireRange = d.acquireRange;
            if (s.fieldHalfLength <= 0f) s.fieldHalfLength = d.fieldHalfLength;
            if (s.laneHalfWidth <= 0f) s.laneHalfWidth = d.laneHalfWidth;
            if (s.maxUnitsPerTeam <= 0) s.maxUnitsPerTeam = d.maxUnitsPerTeam;
            if (s.killsPerHeroLevel <= 0) s.killsPerHeroLevel = d.killsPerHeroLevel;
            if (s.heroAuraPerLevel <= 0f) s.heroAuraPerLevel = d.heroAuraPerLevel;
            if (s.randomSeed == 0u) s.randomSeed = d.randomSeed;

            return s;
        }
    }
}
