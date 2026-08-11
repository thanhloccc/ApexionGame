using EncosyTower.Collections;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Takes the dead out of the graph — the most important file in the sample.
    /// </summary>
    /// <remarks>
    /// The clean path removes a unit's own modifiers first, then destroys its owner. That order is what takes
    /// the unit out of the team node's <i>observer</i> list: <c>TryRemoveStatModifier</c> walks the observed
    /// stat and deletes the reverse edge, and nothing else does. <c>DestroyOwner</c> clears the dying owner's
    /// buffers and pools its slot — it does not reach into other owners.
    /// <para>
    /// Skip the removal and the node keeps one observer entry per dead unit for the rest of the match. Not a
    /// correctness problem — propagation cannot resolve them and skips them — but every write to that node
    /// walks them, they never come back, and in an RTS where units die continuously the list grows without
    /// bound. A leak that looks like nothing until the fight gets big, which is why the HUD keeps the count on
    /// screen and <see cref="RtsGraphExperiments"/> can produce it on demand.
    /// </para>
    /// <para>
    /// For a hero there is a second reason. Its aura terms live on the <i>team node</i>, so a clean death
    /// removes them and the whole army loses the buff in that propagation. A sloppy death leaves them: the
    /// node was not recalculated, so <b>the army keeps a dead hero's aura</b> until something touches it.
    /// </para>
    /// </remarks>
    public sealed class RtsDeathSystem
    {
        private readonly RtsMatchContext _context;
        private readonly RtsEffectSystem _effects;

        private readonly FasterList<RtsUnit> _dying = new(64);

        public RtsDeathSystem(RtsMatchContext context, RtsEffectSystem effects)
        {
            _context = context;
            _effects = effects;
        }

        public void Tick()
        {
            CollectDying();

            var dying = _dying.AsReadOnlySpan();

            for (var i = 0; i < dying.Length; i++)
            {
                var unit = dying[i];

                if (unit.IsStructure)
                {
                    Decide(unit);
                    continue;
                }

                CreditKill(unit);
                Remove(unit, announce: true);
            }
        }

        private void CollectDying()
        {
            _dying.Clear();

            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    var unit = units[i];

                    if (unit.Alive && _context.Read(unit.Handles.hp) <= 0f)
                    {
                        _dying.Add(unit);
                    }
                }
            }
        }

        /// <remarks>
        /// A fallen stronghold ends the match but is not destroyed: the view keeps drawing a ruin, and a
        /// post-mortem inspection still has something to look at.
        /// </remarks>
        private void Decide(RtsUnit stronghold)
        {
            if (_context.Outcome.IsDecided)
            {
                return;
            }

            var winner = 1 - stronghold.TeamIndex;

            _context.Outcome = new RtsMatchOutcome(winner);
            _context.Journal.StrongholdFell(stronghold.TeamIndex, winner);
        }

        /// <summary>Removes a unit from the match, cleanly unless the sloppy experiment is on.</summary>
        public void Remove(RtsUnit unit, bool announce)
        {
            if (unit.Alive == false)
            {
                return;
            }

            var team = _context.TeamAt(unit.TeamIndex);
            var world = _context.World;

            unit.Alive = false;

            var lostModifiers = _effects.PurgeEffectsOf(unit);
            var attackBefore = unit.IsHero ? _context.SampleArmyAttack(team) : 0f;

            if (_context.SloppyDeaths == false)
            {
                var links = unit.Links;

                for (var i = 0; i < links.Length; i++)
                {
                    world.RemoveModifier(links[i]);
                }

                var auraTerms = unit.AuraTerms;

                for (var i = 0; i < auraTerms.Length; i++)
                {
                    world.RemoveModifier(auraTerms[i]);
                }

                unit.ForgetGraphLinks();
            }
            else
            {
                team.SloppyDeaths++;
            }

            // DestroyOwner, not DestroyOwnerAndUpdateObservers: the removals above already recalculated
            // everything that depended on this unit, so there is nothing left to refresh. The
            // observer-updating variant on the generic StatAccessor is for the opposite order — destroy
            // first, ask questions later — which the sloppy path above deliberately demonstrates.
            world.DestroyOwner(unit.Owner);

            _context.Forget(unit);
            team.Units.Remove(unit);
            team.Lost++;

            _context.Battle.Death(unit.Id, unit.LastAttacker?.Id ?? 0);

            if (announce == false)
            {
                return;
            }

            if (unit.IsHero)
            {
                _context.Journal.HeroFell(
                      team.Index
                    , unit.Label
                    , attackBefore
                    , _context.SampleArmyAttack(team));
            }
            else if (lostModifiers > 0)
            {
                _context.Journal.UnitDied(team.Index, unit.Label, lostModifiers);
            }
        }

        private void CreditKill(RtsUnit victim)
        {
            var killer = victim.LastAttacker;

            if (killer == null || killer.Alive == false || killer.IsHero == false)
            {
                return;
            }

            killer.Kills++;

            if (killer.Kills >= killer.Level * _context.Settings.killsPerHeroLevel)
            {
                LevelUp(killer);
            }
        }

        /// <summary>
        /// One write to a hero's <c>AuraPower</c> base value, and the whole army is already correct.
        /// </summary>
        public void LevelUp(RtsUnit hero)
        {
            if (hero.Alive == false || hero.IsHero == false)
            {
                return;
            }

            var team = _context.TeamAt(hero.TeamIndex);
            var world = _context.World;
            var before = world.ReadBase(hero.Handles.auraPower);
            var raised = before + _context.Settings.heroAuraPerLevel;

            world.ClearChangeEvents();
            world.Write(hero.Handles.auraPower, raised);

            hero.Level++;

            _context.Journal.HeroLevelled(
                  team.Index
                , hero.Label
                , hero.Level
                , world.DrainChangedStats()
                , before
                , raised);
        }
    }
}
