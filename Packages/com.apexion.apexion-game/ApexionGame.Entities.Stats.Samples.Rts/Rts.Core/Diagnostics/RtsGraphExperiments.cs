using EncosyTower.Collections;
using Unity.Collections;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// The things this sample lets you <i>do</i> to the graph to see how it behaves.
    /// </summary>
    /// <remarks>
    /// Every method here is wired to a button in the HUD. They are not test hooks and not debug leftovers:
    /// they are the sample's teaching surface, and the reason it is worth opening rather than reading.
    /// </remarks>
    public sealed class RtsGraphExperiments
    {
        private readonly RtsMatchContext _context;
        private readonly RtsEffectSystem _effects;
        private readonly RtsDeathSystem _deaths;

        private readonly FasterList<StatModifierHandle> _pruned = new(32);
        private readonly FasterList<RtsUnit> _doomed = new(16);

        public RtsGraphExperiments(
              RtsMatchContext context
            , RtsEffectSystem effects
            , RtsDeathSystem deaths
        )
        {
            _context = context;
            _effects = effects;
            _deaths = deaths;
        }

        /// <summary>
        /// Destroy owners without removing their modifiers first. The leak, on a switch.
        /// </summary>
        /// <remarks>
        /// Turn it on, cull a few units, and watch the observer count on the team node stay where it is. Turn
        /// it off, cull a few more, and watch it drop. Same code path either way — see
        /// <see cref="RtsDeathSystem"/>.
        /// </remarks>
        public bool SloppyDeaths
        {
            get => _context.SloppyDeaths;
            set
            {
                if (_context.SloppyDeaths == value)
                {
                    return;
                }

                _context.SloppyDeaths = value;
                _context.Journal.SloppyDeathsToggled(value);
            }
        }

        /// <summary>Kills some of a team's own units, front of the roster last.</summary>
        /// <remarks>
        /// A real button rather than a test seam: producing a death on demand is the only way to compare the
        /// clean and sloppy paths in the same match, at the same army size.
        /// </remarks>
        public int Cull(int teamIndex, int count)
        {
            var team = _context.TeamAt(teamIndex);
            var units = team.Roster.AsReadOnlySpan();

            _doomed.Clear();

            for (var i = units.Length - 1; i >= 0 && _doomed.Count < count; i--)
            {
                if (units[i].Alive && units[i].IsStructure == false)
                {
                    _doomed.Add(units[i]);
                }
            }

            var doomed = _doomed.AsReadOnlySpan();

            for (var i = 0; i < doomed.Length; i++)
            {
                _deaths.Remove(doomed[i], announce: false);
            }

            _context.Journal.UnitsCulled(teamIndex, doomed.Length, _context.SloppyDeaths);

            return doomed.Length;
        }

        /// <summary>
        /// Attempts the tempting version of a hero aura, and reports that the runtime refuses it.
        /// </summary>
        /// <remarks>
        /// <c>team.attackBonus</c> reading <c>hero.attack</c> would close a loop, because <c>hero.attack</c>
        /// already reads <c>team.attackBonus</c> like every other unit's does. Cycles are rejected at insert
        /// time rather than detected during propagation, so the call returns false and the store is
        /// untouched — but nothing throws, and a codebase that ignores the return value ends up with an aura
        /// that silently does not exist.
        /// </remarks>
        public bool TryCyclicAura(int teamIndex)
        {
            var team = _context.TeamAt(teamIndex);
            var hero = team.Hero;

            if (hero == null)
            {
                _context.Journal.ModifierRefused(teamIndex, "no hero to try a cyclic aura with");
                return false;
            }

            var accepted = _context.World.TryAddModifier(
                  team.Handles.attackBonus
                , RtsStatSystem.StatModifier.AddFractionOf(hero.Handles.attack, 0.25f)
                , out var handle);

            _context.Journal.CycleRefused(teamIndex, hero.Label, accepted);

            if (accepted)
            {
                // Should be unreachable. Undo it rather than leave the graph in a shape the rest of the sample
                // does not expect.
                _context.World.RemoveModifier(handle);
            }

            return accepted == false;
        }

        /// <summary>Removes every modifier that reported a missing source, and forgets its handle.</summary>
        public int Prune()
        {
            var reported = _context.World.PruneDangling(_pruned);
            var removed = _pruned.AsReadOnlySpan();

            for (var i = 0; i < removed.Length; i++)
            {
                Forget(removed[i]);
            }

            _context.Journal.ModifiersPruned(removed.Length, reported);

            return removed.Length;
        }

        private void Forget(in StatModifierHandle handle)
        {
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    units[i].Forget(handle);
                }
            }

            _effects.Forget(handle);
        }

        /// <summary>
        /// Recalculates every stat in the match through the generated <c>[BurstCompile]</c> job.
        /// </summary>
        /// <remarks>
        /// Nothing in the match needs this — a write propagates on its own. It is here because a real project
        /// eventually does: after a load, after a batch of edits made with propagation suppressed, or as a big
        /// red button when a number looks stale.
        /// </remarks>
        public int RecalculateEverything()
        {
            var handles = new NativeList<StatHandle>(512, Allocator.TempJob);
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                var node = teams[t].Handles;

                handles.Add(node.attackBonus);
                handles.Add(node.armorBonus);
                handles.Add(node.moveSpeedBonus);

                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    if (units[i].Alive == false)
                    {
                        continue;
                    }

                    var unit = units[i].Handles;

                    handles.Add(unit.attack);
                    handles.Add(unit.armor);
                    handles.Add(unit.moveSpeed);
                    handles.Add(unit.attackInterval);
                    handles.Add(unit.hp);
                }
            }

            var scheduled = _context.World.Recalculate(handles);

            handles.Dispose();

            _context.Journal.Recalculated(scheduled);

            return scheduled;
        }
    }
}
