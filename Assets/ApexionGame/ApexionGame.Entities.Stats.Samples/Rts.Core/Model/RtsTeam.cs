using EncosyTower.Collections;
using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>One faction: a shared bonus node, an army, a wallet and a row of cooldowns.</summary>
    /// <remarks>
    /// The node is the point. Research writes its base value, hero auras are modifiers on it, and every unit
    /// reads it — so a unit never learns how many sources exist. See <see cref="TeamStats"/>.
    /// </remarks>
    public sealed class RtsTeam
    {
        private readonly FasterList<RtsUnit> _units = new(64);
        private readonly int[] _researchLevels = new int[RtsContent.Researches.Count];
        private readonly float[] _spellCooldowns = new float[RtsContent.Spells.Count];

        public readonly int Index;
        public readonly string Name;
        public readonly StatOwnerHandle Owner;
        public readonly TeamStats Stats;
        public readonly TeamStats.StatHandles Handles;

        /// <summary>Where this team's units come from, and where the enemy has to reach.</summary>
        public readonly float2 BasePosition;

        /// <summary>+1 for the team on the left, -1 for the team on the right.</summary>
        public readonly float AdvanceDirection;

        public float Supply;
        public int Spawned;
        public int Lost;

        /// <summary>Units destroyed without removing their modifiers first. Should stay zero.</summary>
        public int SloppyDeaths;

        public RtsUnit Stronghold;

        public RtsTeam(
              int index
            , string name
            , in StatOwnerHandle owner
            , in TeamStats stats
            , float2 basePosition
            , float advanceDirection
        )
        {
            Index = index;
            Name = name;
            Owner = owner;
            Stats = stats;
            Handles = stats.GetStatHandles(owner);
            BasePosition = basePosition;
            AdvanceDirection = advanceDirection;
        }

        /// <summary>Living units, stronghold included.</summary>
        public FasterList<RtsUnit> Units => _units;

        public FasterList<RtsUnit>.ReadOnly Roster => _units.AsReadOnly();

        public RtsUnit Hero
        {
            get
            {
                var units = _units.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    if (units[i].IsHero)
                    {
                        return units[i];
                    }
                }

                return null;
            }
        }

        /// <summary>Living units that are not the stronghold.</summary>
        public int ArmyCount
        {
            get
            {
                var units = _units.AsReadOnlySpan();
                var count = 0;

                for (var i = 0; i < units.Length; i++)
                {
                    if (units[i].IsStructure == false)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // ---- wallet ----------------------------------------------------------------------------

        public void Earn(float amount, float cap) => Supply = math.min(Supply + amount, cap);

        public bool CanAfford(int cost) => Supply >= cost;

        /// <summary>Spends, or leaves the wallet alone and says no. The only way supply goes down.</summary>
        public bool TrySpend(int cost)
        {
            if (Supply < cost)
            {
                return false;
            }

            Supply -= cost;
            return true;
        }

        // ---- research --------------------------------------------------------------------------

        public int LevelOf(int researchIndex) => _researchLevels[researchIndex];

        public void RaiseLevel(int researchIndex) => _researchLevels[researchIndex]++;

        // ---- cooldowns -------------------------------------------------------------------------

        public float CooldownOf(int spellIndex) => _spellCooldowns[spellIndex];

        public bool IsReady(int spellIndex) => _spellCooldowns[spellIndex] <= 0f;

        public void StartCooldown(int spellIndex, float seconds) => _spellCooldowns[spellIndex] = seconds;

        public void AdvanceCooldowns(float deltaTime)
        {
            for (var i = 0; i < _spellCooldowns.Length; i++)
            {
                _spellCooldowns[i] = math.max(_spellCooldowns[i] - deltaTime, 0f);
            }
        }
    }
}
