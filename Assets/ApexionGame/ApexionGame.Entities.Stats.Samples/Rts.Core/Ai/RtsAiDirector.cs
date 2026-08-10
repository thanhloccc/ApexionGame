using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Plays one side: fields units, buys research, and casts on whatever clump is biggest.
    /// </summary>
    /// <remarks>
    /// Deliberately without randomness, so the same match with the same inputs replays the same way — which is
    /// what makes the "run a match to completion" test worth anything. Team B has one enabled by default;
    /// enable Team A's from the HUD to watch the sample play itself.
    /// </remarks>
    public sealed class RtsAiDirector
    {
        /// <summary>Roughly the mix a human ends up building.</summary>
        private static readonly RtsUnitArchetype[] s_pattern = {
            RtsContent.Footman,
            RtsContent.Footman,
            RtsContent.Archer,
            RtsContent.Knight,
            RtsContent.Footman,
            RtsContent.Archer,
        };

        /// <summary>Fewer than this in the circle is not worth a spell.</summary>
        private const int WorthCasting = 4;

        /// <summary>Spare supply kept back so research never starves the army.</summary>
        private const float ResearchBuffer = 60f;

        private const float HeroDelay = 12f;

        private readonly int _teamIndex;
        private readonly RtsMatchContext _context;
        private readonly RtsSpawnSystem _spawn;
        private readonly RtsEconomySystem _economy;
        private readonly RtsEffectSystem _effects;

        private float _untilNextDecision;
        private int _spawnCursor;

        public RtsAiDirector(
              int teamIndex
            , RtsMatchContext context
            , RtsSpawnSystem spawn
            , RtsEconomySystem economy
            , RtsEffectSystem effects
        )
        {
            _teamIndex = teamIndex;
            _context = context;
            _spawn = spawn;
            _economy = economy;
            _effects = effects;
        }

        public bool Enabled { get; set; }

        public float DecisionInterval { get; set; } = 1.1f;

        public void Tick(float deltaTime)
        {
            if (Enabled == false || _context.Outcome.IsDecided)
            {
                return;
            }

            _untilNextDecision -= deltaTime;

            if (_untilNextDecision > 0f)
            {
                return;
            }

            _untilNextDecision = DecisionInterval;

            if (TryCast() == false)
            {
                Build();
            }
        }

        private bool TryCast()
        {
            var team = _context.TeamAt(_teamIndex);
            var spells = RtsContent.SpellSpan;

            for (var i = 0; i < spells.Length; i++)
            {
                var spell = spells[i];

                if (_effects.Validate(team, i) != RtsRejection.None)
                {
                    continue;
                }

                if (TryAim(spell, out var at) && _effects.TryCast(team, i, at, out _))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Aims at the unit with the most company. Candidate centres are the units themselves, which is crude
        /// and completely adequate.
        /// </summary>
        private bool TryAim(RtsSpell spell, out float2 at)
        {
            var side = spell.Target == RtsSpellTarget.Allies
                ? _context.TeamAt(_teamIndex)
                : _context.EnemyOf(_teamIndex);

            var units = side.Roster.AsReadOnlySpan();
            var bestCount = WorthCasting - 1;

            at = float2.zero;

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                if (unit.Alive == false || unit.IsStructure)
                {
                    continue;
                }

                var count = spell.InstantHeal > 0f
                    ? CountHurt(side, unit.Position, spell.Radius, 0.6f)
                    : Count(side, unit.Position, spell.Radius);

                if (count > bestCount)
                {
                    bestCount = count;
                    at = unit.Position;
                }
            }

            return bestCount >= WorthCasting;
        }

        private void Build()
        {
            var team = _context.TeamAt(_teamIndex);
            var hero = RtsContent.HeroOf(_teamIndex);

            // A hero as soon as one is affordable: the aura is worth more than three footmen.
            if (team.Hero == null
                && _context.Elapsed > HeroDelay
                && _spawn.TryBuy(team, hero, out _))
            {
                return;
            }

            // Research only out of spare supply. An army that stops spawning loses the field.
            for (var i = 0; i < RtsContent.Researches.Count; i++)
            {
                var cost = _economy.CostOf(team, i);

                if (cost > 0
                    && team.Supply >= cost + ResearchBuffer
                    && _economy.TryBuy(team, i, out _))
                {
                    return;
                }
            }

            if (_spawn.TryBuy(team, s_pattern[_spawnCursor % s_pattern.Length], out _))
            {
                _spawnCursor++;
            }
        }

        private static int Count(RtsTeam team, float2 at, float radius)
        {
            var units = team.Roster.AsReadOnlySpan();
            var count = 0;

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                if (unit.Alive && unit.IsStructure == false && RtsGeometry.Within(at, unit, radius))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountHurt(RtsTeam team, float2 at, float radius, float fraction)
        {
            var units = team.Roster.AsReadOnlySpan();
            var count = 0;

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                if (unit.Alive == false || unit.IsStructure || RtsGeometry.Within(at, unit, radius) == false)
                {
                    continue;
                }

                if (_context.HpFraction(unit) < fraction)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
