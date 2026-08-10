using EncosyTower.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// What every system needs, and nothing more: the store, the two teams, the journal, the battle feed and
    /// the dice.
    /// </summary>
    /// <remarks>
    /// Systems take this in their constructor instead of six arguments each. It is the seam that lets each
    /// system be read on its own — a system's file tells you exactly which parts of the world it touches.
    /// </remarks>
    public sealed class RtsMatchContext
    {
        private readonly ArrayMap<StatOwnerHandle, RtsUnit> _unitsByOwner = new(256);

        private Random _random;

        public RtsMatchContext(
              RtsStatWorld world
            , RtsMatchSettings settings
            , RtsJournal journal
            , RtsBattleFeed battle
        )
        {
            World = world;
            Settings = settings;
            Journal = journal;
            Battle = battle;

            _random = new Random(settings.randomSeed == 0u ? 1u : settings.randomSeed);
        }

        public RtsStatWorld World { get; }

        public RtsMatchSettings Settings { get; }

        public RtsJournal Journal { get; }

        public RtsBattleFeed Battle { get; }

        public RtsTeam[] Teams { get; private set; }

        public float Elapsed { get; set; }

        public RtsMatchOutcome Outcome { get; set; } = RtsMatchOutcome.InProgress;

        /// <summary>
        /// Destroy owners without removing their modifiers first. Off by default, and only
        /// <see cref="RtsGraphExperiments"/> turns it on — it exists to make an invisible leak visible.
        /// </summary>
        public bool SloppyDeaths { get; set; }

        public void SetTeams(RtsTeam[] teams) => Teams = teams;

        public RtsTeam TeamAt(int index) => Teams[index];

        public RtsTeam EnemyOf(int teamIndex) => Teams[1 - teamIndex];

        // ---- owner lookup ----------------------------------------------------------------------

        /// <summary>Who owns a stat, for the inspector's edge labels and for post-mortems.</summary>
        public bool TryGetUnit(in StatOwnerHandle owner, out RtsUnit unit)
            => _unitsByOwner.TryGetValue(owner, out unit);

        public void Register(RtsUnit unit) => _unitsByOwner[unit.Owner] = unit;

        public void Forget(RtsUnit unit) => _unitsByOwner.Remove(unit.Owner);

        public void ForgetAllUnits() => _unitsByOwner.Clear();

        // ---- dice ------------------------------------------------------------------------------

        /// <remarks>
        /// The generator is kept private and handed out through methods, so no system can copy the struct and
        /// silently fork the sequence — which is what makes a fixed seed worth having.
        /// </remarks>
        public float NextFloat(float min, float max) => _random.NextFloat(min, max);

        public void ResetDice(uint seed) => _random = new Random(seed == 0u ? 1u : seed);

        // ---- shared reads ----------------------------------------------------------------------

        public float Read(in StatHandle handle) => World.Read(handle);

        public float HpFraction(RtsUnit unit)
        {
            var max = World.Read(unit.Handles.maxHp);
            return max <= 0f ? 0f : math.saturate(World.Read(unit.Handles.hp) / max);
        }

        /// <summary>
        /// Attack of the first line unit on a team: the number that shows an army-wide change happened.
        /// </summary>
        public float SampleArmyAttack(RtsTeam team)
        {
            var units = team.Roster.AsReadOnlySpan();

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                if (unit.Alive && unit.IsHero == false && unit.IsStructure == false)
                {
                    return World.Read(unit.Handles.attack);
                }
            }

            return 0f;
        }
    }
}
