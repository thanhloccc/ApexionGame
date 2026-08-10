using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// A match: two factions, a shared bonus node each, units that spawn and die, heroes whose auras feed the
    /// whole army, research, spells, and combat resolved on a fixed tick.
    /// </summary>
    /// <remarks>
    /// This class owns two things and nothing else: <b>who exists</b> and <b>the order things happen in</b>.
    /// Every mechanic lives in its own system, and every one of those systems can be read on its own.
    /// <para>
    /// Deliberately not a <c>MonoBehaviour</c>: the store is native memory, so the same model drives the scene
    /// and the EditMode tests, with no scene to load. To start over, dispose one of these and build another.
    /// </para>
    /// </remarks>
    public sealed class RtsMatch : IDisposable
    {
        private readonly RtsStatWorld _world;
        private readonly RtsMatchContext _context;

        private readonly RtsSpawnSystem _spawn;
        private readonly RtsMovementSystem _movement;
        private readonly RtsCombatSystem _combat;
        private readonly RtsEffectSystem _effects;
        private readonly RtsEconomySystem _economy;
        private readonly RtsDeathSystem _deaths;
        private readonly RtsAiDirector[] _directors;

        public RtsMatch(RtsMatchSettings settings)
        {
            var normalized = settings.Normalized();

            _world = new RtsStatWorld(ownerCapacity: 256);

            Journal = new RtsJournal(160);
            Battle = new RtsBattleFeed();

            _context = new RtsMatchContext(_world, normalized, Journal, Battle);

            _spawn = new RtsSpawnSystem(_context);
            _movement = new RtsMovementSystem(_context);
            _combat = new RtsCombatSystem(_context, _movement);
            _effects = new RtsEffectSystem(_context, _combat);
            _economy = new RtsEconomySystem(_context);
            _deaths = new RtsDeathSystem(_context, _effects);

            Probe = new RtsGraphProbe(_context);
            Inspector = new RtsStatInspector(_context, _effects, Probe);
            Experiments = new RtsGraphExperiments(_context, _effects, _deaths);

            _directors = new[] {
                new RtsAiDirector(0, _context, _spawn, _economy, _effects),
                new RtsAiDirector(1, _context, _spawn, _economy, _effects) { Enabled = true },
            };

            _world.RegisterDebugView("RTS Battle", Inspector.ResolveStatName);

            Deploy();
        }

        // ---- what exists -----------------------------------------------------------------------

        public RtsMatchSettings Settings => _context.Settings;

        public RtsJournal Journal { get; }

        public RtsBattleFeed Battle { get; }

        public RtsGraphProbe Probe { get; }

        public RtsStatInspector Inspector { get; }

        public RtsGraphExperiments Experiments { get; }

        public RtsEffectSystem Effects => _effects;

        public RtsEconomySystem Economy => _economy;

        public IReadOnlyList<RtsTeam> Teams => _context.Teams;

        public RtsTeam TeamAt(int index) => _context.TeamAt(index);

        public RtsAiDirector DirectorOf(int teamIndex) => _directors[teamIndex];

        public float Elapsed => _context.Elapsed;

        public RtsMatchOutcome Outcome => _context.Outcome;

        public bool IsOver => _context.Outcome.IsDecided;

        public void Dispose() => _world.Dispose();

        /// <remarks>
        /// The team nodes are created <b>before</b> any unit, because every unit's modifiers point at them.
        /// <c>TryAddStatModifier</c> checks that every observed stat exists and fails the whole call if one does
        /// not, so the order is not optional.
        /// </remarks>
        private void Deploy()
        {
            var settings = _context.Settings;
            var half = settings.fieldHalfLength;

            _context.SetTeams(new[] {
                NewTeam(0, "Team A", new float2(-half, 0f), +1f),
                NewTeam(1, "Team B", new float2(+half, 0f), -1f),
            });

            foreach (var team in _context.Teams)
            {
                team.Supply = settings.startingSupply;
                team.Stronghold = _spawn.Spawn(team, RtsContent.Stronghold, team.BasePosition);

                for (var i = 0; i < settings.startingFootmen; i++)
                {
                    _spawn.Spawn(team, RtsContent.Footman, _spawn.SpawnPointOf(team));
                }

                for (var i = 0; i < settings.startingArchers; i++)
                {
                    _spawn.Spawn(team, RtsContent.Archer, _spawn.SpawnPointOf(team));
                }
            }

            Journal.MatchStarted(_world.OwnerCount, _world.OwnerCount - _context.Teams.Length);
            Journal.GraphNote("every unit costs 5 modifiers: 3 links to its team node, 1 Hp cap, 1 interval floor");

            _world.ClearChangeEvents();
        }

        private RtsTeam NewTeam(int index, string name, float2 basePosition, float advanceDirection)
        {
            var stats = _world.CreateTeamOwner(out var owner);

            return new RtsTeam(index, name, owner, stats, basePosition, advanceDirection);
        }

        // ---- the order things happen in --------------------------------------------------------

        /// <summary>
        /// Advances the match by exactly one <see cref="RtsMatchSettings.tickSeconds"/>.
        /// </summary>
        /// <remarks>
        /// The order is the design, so it lives in one readable list:
        /// <list type="number">
        /// <item>supply accrues, so this tick's decisions can spend it;</item>
        /// <item>the AI decides, before anything moves;</item>
        /// <item>effects tick and expire — a buff's last tick still swings;</item>
        /// <item>units pick targets and walk;</item>
        /// <item>whoever is in range and off cooldown swings;</item>
        /// <item>the dead leave the graph, after combat rather than during it;</item>
        /// <item>the tick's change events are drained, because nothing clears them for you.</item>
        /// </list>
        /// </remarks>
        public void Tick()
        {
            if (IsOver || _world.IsCreated == false)
            {
                return;
            }

            var deltaTime = _context.Settings.tickSeconds;

            _context.Elapsed += deltaTime;
            Journal.Now = _context.Elapsed;

            _economy.Tick(deltaTime);

            for (var i = 0; i < _directors.Length; i++)
            {
                _directors[i].Tick(deltaTime);
            }

            _effects.Tick(deltaTime);
            _movement.Tick(deltaTime);
            _combat.Tick(deltaTime);
            _deaths.Tick();

            _world.DrainChangeEvents(out var changedStats, out var rawEvents);
            _world.DrainModifierTriggers();

            Probe.RecordTick(changedStats, rawEvents);
        }

        // ---- verbs -----------------------------------------------------------------------------

        public bool TrySpawn(int teamIndex, RtsUnitArchetype archetype, out RtsRejection rejection)
            => _spawn.TryBuy(TeamAt(teamIndex), archetype, out rejection);

        public bool TryResearch(int teamIndex, int researchIndex, out RtsRejection rejection)
            => _economy.TryBuy(TeamAt(teamIndex), researchIndex, out rejection);

        public bool TryCast(int teamIndex, int spellIndex, float2 at, out RtsRejection rejection)
            => _effects.TryCast(TeamAt(teamIndex), spellIndex, at, out rejection);

        public bool CanCast(int teamIndex, int spellIndex)
            => _effects.Validate(TeamAt(teamIndex), spellIndex) == RtsRejection.None;

        public bool CanResearch(int teamIndex, int researchIndex)
            => _economy.Validate(TeamAt(teamIndex), researchIndex) == RtsRejection.None;

        public bool CanSpawn(int teamIndex, RtsUnitArchetype archetype)
            => _spawn.Validate(TeamAt(teamIndex), archetype) == RtsRejection.None;

        /// <summary>Levels a hero on demand — the HUD button that shows one write reaching a whole army.</summary>
        public bool TryLevelUpHero(int teamIndex, out RtsRejection rejection)
        {
            var hero = TeamAt(teamIndex).Hero;

            if (hero == null)
            {
                rejection = RtsRejection.NoHeroFielded;
                return false;
            }

            rejection = RtsRejection.None;
            _deaths.LevelUp(hero);
            return true;
        }

        // ---- reads the presentation layer needs ------------------------------------------------

        public float Read(in StatHandle handle) => _world.Read(handle);

        public float ReadBase(in StatHandle handle) => _world.ReadBase(handle);

        public float HpFraction(RtsUnit unit) => _context.HpFraction(unit);

        public float SpellCooldown(int teamIndex, int spellIndex) => TeamAt(teamIndex).CooldownOf(spellIndex);

        /// <summary>Nearest living unit to a point, or null when nothing is close enough.</summary>
        public RtsUnit PickUnitAt(float2 point, float maxDistance)
        {
            RtsUnit best = null;
            var bestDistance = maxDistance;
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    var distance = RtsGeometry.Gap(point, units[i]);

                    if (distance < bestDistance)
                    {
                        best = units[i];
                        bestDistance = distance;
                    }
                }
            }

            return best;
        }
    }
}
