using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Puts units on the field, and ties them into the stat graph.
    /// </summary>
    /// <remarks>
    /// <b>A unit costs five modifiers, always</b> — three links to its team's node, one <c>Hp ≤ MaxHp</c> cap
    /// and one <c>AttackInterval</c> floor. Not five per hero on the field, not five per research level
    /// bought: five. That constant is what the shared node buys, and <c>RtsGraphInvariantTests</c> pins it.
    /// </remarks>
    public sealed class RtsSpawnSystem
    {
        private readonly RtsMatchContext _context;

        private int _nextUnitId = 1;

        public RtsSpawnSystem(RtsMatchContext context)
        {
            _context = context;
        }

        /// <summary>The player's (or the AI's) verb: pay for a unit and field it.</summary>
        public bool TryBuy(RtsTeam team, RtsUnitArchetype archetype, out RtsRejection rejection)
        {
            rejection = Validate(team, archetype);

            if (rejection != RtsRejection.None)
            {
                return false;
            }

            team.TrySpend(archetype.SupplyCost);

            _context.World.ClearChangeEvents();

            var unit = Spawn(team, archetype, SpawnPointOf(team));

            if (archetype.HasAura)
            {
                _context.Journal.HeroArrived(
                      team.Index
                    , unit.Label
                    , unit.AuraTerms.Length
                    , _context.World.DrainChangedStats());
            }
            else
            {
                _context.Journal.UnitSpawned(team.Index, archetype.Name);
            }

            return true;
        }

        public RtsRejection Validate(RtsTeam team, RtsUnitArchetype archetype)
        {
            if (_context.Outcome.IsDecided)
            {
                return RtsRejection.MatchOver;
            }

            if (archetype.IsHero && team.Hero != null)
            {
                return RtsRejection.HeroAlreadyFielded;
            }

            if (team.ArmyCount >= _context.Settings.maxUnitsPerTeam)
            {
                return RtsRejection.ArmyAtCap;
            }

            return team.CanAfford(archetype.SupplyCost)
                ? RtsRejection.None
                : RtsRejection.NotEnoughSupply;
        }

        /// <summary>Fields a unit without charging for it — match setup, and the stronghold.</summary>
        public RtsUnit Spawn(RtsTeam team, RtsUnitArchetype archetype, float2 position)
        {
            var world = _context.World;
            var stats = world.CreateUnitOwner(out var owner);

            world.WriteUnitSheet(owner, stats, new UnitStats.Options.Data(
                  hp: new UnitStats.Hp(archetype.MaxHp)
                , maxHp: new UnitStats.MaxHp(archetype.MaxHp)
                , attack: new UnitStats.Attack(archetype.Attack)
                , armor: new UnitStats.Armor(archetype.Armor)
                , moveSpeed: new UnitStats.MoveSpeed(archetype.MoveSpeed)
                , attackInterval: new UnitStats.AttackInterval(archetype.AttackInterval)
                , attackRange: new UnitStats.AttackRange(archetype.AttackRange)
                , auraPower: new UnitStats.AuraPower(archetype.Aura.Power)
            ));

            var unit = new RtsUnit(_nextUnitId++, team.Index, archetype, owner, stats, position);

            AttachToGraph(team, unit);

            if (archetype.HasAura)
            {
                AttachAura(team, unit);
            }

            // Staggered, so an army does not swing in lockstep on the same tick boundary.
            unit.Cooldown = _context.NextFloat(0f, math.max(archetype.AttackInterval, 0.01f));

            team.Units.Add(unit);
            team.Spawned++;
            _context.Register(unit);

            return unit;
        }

        public float2 SpawnPointOf(RtsTeam team)
        {
            var lane = _context.Settings.laneHalfWidth - 1f;

            return new float2(
                  team.BasePosition.x + team.AdvanceDirection * 2.2f
                , _context.NextFloat(-lane, lane));
        }

        /// <summary>The five modifiers, and the whole cost of joining an army.</summary>
        private void AttachToGraph(RtsTeam team, RtsUnit unit)
        {
            var handles = unit.Handles;
            var node = team.Handles;

            Link(unit, handles.attack, RtsStatSystem.StatModifier.AddFrom(node.attackBonus));
            Link(unit, handles.armor, RtsStatSystem.StatModifier.AddFrom(node.armorBonus));
            Link(unit, handles.moveSpeed, RtsStatSystem.StatModifier.AddFrom(node.moveSpeedBonus));
            Link(unit, handles.hp, RtsStatSystem.StatModifier.ClampMaxFrom(handles.maxHp));

            Link(unit, handles.attackInterval
                , RtsStatSystem.StatModifier.ClampMin(_context.Settings.attackIntervalFloor));
        }

        /// <summary>
        /// Hangs a hero's aura on its team's node — one modifier, on the team, not on the units.
        /// </summary>
        /// <remarks>
        /// Every unit's Attack is already correct on the next line, including units that spawn afterwards.
        /// That last part is what direct unit-to-hero edges cannot give you.
        /// </remarks>
        private void AttachAura(RtsTeam team, RtsUnit hero)
        {
            var aura = hero.Archetype.Aura;
            var node = team.Handles;

            AddTerm(node.attackBonus, aura.AttackFraction);
            AddTerm(node.armorBonus, aura.ArmorFraction);
            AddTerm(node.moveSpeedBonus, aura.MoveSpeedFraction);

            void AddTerm(StatHandle teamStat, float fraction)
            {
                if (fraction == 0f)
                {
                    return;
                }

                var modifier = RtsStatSystem.StatModifier
                    .AddFractionOf(hero.Handles.auraPower, fraction);

                if (_context.World.TryAddModifier(teamStat, modifier, out var handle))
                {
                    hero.AddAuraTerm(handle);
                }
                else
                {
                    _context.Journal.ModifierRefused(team.Index, $"{hero.Label} aura");
                }
            }
        }

        private void Link(RtsUnit unit, in StatHandle affected, in RtsStatSystem.StatModifier modifier)
        {
            if (_context.World.TryAddModifier(affected, modifier, out var handle))
            {
                unit.AddLink(handle);
            }
            else
            {
                _context.Journal.ModifierRefused(unit.TeamIndex, unit.Label);
            }
        }
    }
}
