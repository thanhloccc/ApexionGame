using System;
using EncosyTower.Collections;
using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Spells, and the part the runtime does not do for you: <b>duration</b>.
    /// </summary>
    /// <remarks>
    /// A modifier lives until something removes it, and the handle its insertion returned is the only way
    /// back. Everything in this file is the bookkeeping that turns those two facts into a timed buff.
    /// <para>
    /// Recasting on a unit that already has the same spell <b>refreshes</b> it rather than stacking a second
    /// copy. Neither is more correct and the runtime has no opinion; refresh keeps the numbers readable and
    /// stops one button from becoming an unbounded buff. The <c>ClampMin</c> on <c>AttackInterval</c> stays
    /// either way — it is the thing that survives the mistake.
    /// </para>
    /// </remarks>
    public sealed class RtsEffectSystem
    {
        private readonly RtsMatchContext _context;
        private readonly RtsCombatSystem _combat;

        private readonly FasterList<RtsEffect> _effects = new(128);
        private readonly FasterList<RtsUnit> _targets = new(64);
        private readonly ArrayMap<RtsSpell, ExpiryTally> _expiries = new(8);

        public RtsEffectSystem(RtsMatchContext context, RtsCombatSystem combat)
        {
            _context = context;
            _combat = combat;
        }

        private struct ExpiryTally
        {
            public int units;
            public int returned;
            public int total;
        }

        public int ActiveCount => _effects.Count;

        public ReadOnlySpan<RtsEffect> Active => _effects.AsReadOnlySpan();

        public void Clear() => _effects.Clear();

        // ---- casting ---------------------------------------------------------------------------

        public RtsRejection Validate(RtsTeam team, int spellIndex)
        {
            if (_context.Outcome.IsDecided)
            {
                return RtsRejection.MatchOver;
            }

            if (spellIndex < 0 || spellIndex >= RtsContent.Spells.Count)
            {
                return RtsRejection.NoSuchThing;
            }

            if (team.IsReady(spellIndex) == false)
            {
                return RtsRejection.OnCooldown;
            }

            return team.CanAfford(RtsContent.Spells[spellIndex].SupplyCost)
                ? RtsRejection.None
                : RtsRejection.NotEnoughSupply;
        }

        /// <summary>Casts a spell centred on a point, on every living army unit of the target side in range.</summary>
        public bool TryCast(RtsTeam team, int spellIndex, float2 at, out RtsRejection rejection)
        {
            rejection = Validate(team, spellIndex);

            if (rejection != RtsRejection.None)
            {
                return false;
            }

            var spell = RtsContent.Spells[spellIndex];

            CollectTargets(spell, team, at);

            if (_targets.Count == 0)
            {
                rejection = RtsRejection.NothingInRange;
                return false;
            }

            team.TrySpend(spell.SupplyCost);
            team.StartCooldown(spellIndex, spell.Cooldown);

            _context.World.ClearChangeEvents();

            var added = 0;
            var refreshed = 0;
            var units = _targets.AsReadOnlySpan();

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                if (spell.InstantHeal > 0f)
                {
                    _combat.Heal(unit, spell.InstantHeal);
                }

                if (spell.IsInstant)
                {
                    continue;
                }

                var existing = Find(spell, unit);

                if (existing != null)
                {
                    existing.Refresh();
                    refreshed++;
                    continue;
                }

                added += Apply(spell, unit, team.Index);
            }

            _context.Journal.SpellCast(
                  team.Index
                , spell.Name
                , units.Length
                , added
                , refreshed
                , _context.World.DrainChangedStats());

            return true;
        }

        private void CollectTargets(RtsSpell spell, RtsTeam caster, float2 at)
        {
            var side = spell.Target == RtsSpellTarget.Allies ? caster : _context.EnemyOf(caster.Index);
            var units = side.Roster.AsReadOnlySpan();

            _targets.Clear();

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                // Army units only. A stronghold is a unit as far as the stat graph is concerned, but hasting
                // a building's attack speed is not a mechanic anyone wants to explain.
                if (unit.Alive == false || unit.IsStructure)
                {
                    continue;
                }

                if (RtsGeometry.Within(at, unit, spell.Radius))
                {
                    _targets.Add(unit);
                }
            }
        }

        /// <summary>One modifier per term, and the handles kept so they can be given back.</summary>
        private int Apply(RtsSpell spell, RtsUnit unit, int casterTeam)
        {
            var effect = new RtsEffect(spell, unit, casterTeam);
            var terms = spell.Terms;
            var added = 0;

            for (var t = 0; t < terms.Length; t++)
            {
                var term = terms[t];
                var affected = unit.Handles.Of(term.Stat);

                if (_context.World.TryAddModifier(affected, term.ToModifier(), out var handle))
                {
                    effect.AddModifier(handle);
                    added++;
                }
            }

            _effects.Add(effect);

            return added;
        }

        // ---- time ------------------------------------------------------------------------------

        public void Tick(float deltaTime)
        {
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                teams[t].AdvanceCooldowns(deltaTime);
            }

            _expiries.Clear();

            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];

                if (effect.Unit.Alive == false)
                {
                    // A death purges its own effects; this is the defensive path. Nothing to remove either
                    // way — see PurgeEffectsOf.
                    _effects.RemoveAt(i);
                    continue;
                }

                if (Advance(effect, deltaTime) == false)
                {
                    continue;
                }

                Expire(effect);
                _effects.RemoveAt(i);
            }

            ReportExpiries();
        }

        /// <summary>Runs an effect's damage ticks. Returns true when it has run out.</summary>
        private bool Advance(RtsEffect effect, float deltaTime)
        {
            var spell = effect.Spell;

            // Only the part of this step the effect is still alive for: every tick that fits inside the
            // remaining duration, and none past the end.
            var window = math.min(deltaTime, math.max(effect.Remaining, 0f));

            effect.Remaining -= deltaTime;

            if (spell.IsDamageOverTime)
            {
                effect.UntilNextTick -= window;

                while (effect.UntilNextTick <= 0f)
                {
                    _combat.Damage(effect.Unit, spell.DamagePerTick);
                    effect.UntilNextTick += spell.TickInterval;

                    if (_context.Read(effect.Unit.Handles.hp) <= 0f)
                    {
                        break;
                    }
                }
            }

            return effect.Remaining <= 0f;
        }

        private void Expire(RtsEffect effect)
        {
            var modifiers = effect.Modifiers;
            var returned = 0;

            for (var i = 0; i < modifiers.Length; i++)
            {
                if (_context.World.RemoveModifier(modifiers[i]))
                {
                    returned++;
                }
            }

            ref var tally = ref _expiries.GetOrAdd(effect.Spell);
            tally.units++;
            tally.returned += returned;
            tally.total += effect.ModifierCount;
        }

        private void ReportExpiries()
        {
            if (_expiries.Count == 0)
            {
                return;
            }

            foreach (var pair in _expiries)
            {
                var tally = pair.Value;

                _context.Journal.EffectExpired(pair.Key.Name, tally.units, tally.returned, tally.total);
            }
        }

        // ---- queries the rest of the game asks --------------------------------------------------

        public RtsEffect Find(RtsSpell spell, RtsUnit unit)
        {
            var effects = _effects.AsReadOnlySpan();

            for (var i = 0; i < effects.Length; i++)
            {
                if (effects[i].Spell == spell && effects[i].Unit == unit)
                {
                    return effects[i];
                }
            }

            return null;
        }

        public void CollectOn(RtsUnit unit, FasterList<RtsEffect> into)
        {
            into.Clear();

            var effects = _effects.AsReadOnlySpan();

            for (var i = 0; i < effects.Length; i++)
            {
                if (effects[i].Unit == unit)
                {
                    into.Add(effects[i]);
                }
            }
        }

        public bool HasBuff(RtsUnit unit) => Has(unit, debuff: false);

        public bool HasDebuff(RtsUnit unit) => Has(unit, debuff: true);

        private bool Has(RtsUnit unit, bool debuff)
        {
            var effects = _effects.AsReadOnlySpan();

            for (var i = 0; i < effects.Length; i++)
            {
                if (effects[i].Unit == unit && effects[i].Spell.IsDebuff == debuff)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Drops the effect records of a unit that is about to be destroyed, reporting how many modifiers went
        /// with it.
        /// </summary>
        /// <remarks>
        /// <b>No removal call here, and that is deliberate.</b> A spell's modifiers sit on the dying unit's own
        /// stats and observe nothing outside it, so <c>DestroyOwner</c> clearing that owner's buffers is all
        /// the cleanup they need.
        /// <para>
        /// Compare with the unit's links and a hero's aura terms, which <i>do</i> have to be removed by hand:
        /// the links observe the team node, and the aura terms live on it. The rule is not "remove
        /// everything" — it is "remove anything that touches an owner other than the one being destroyed".
        /// </para>
        /// </remarks>
        public int PurgeEffectsOf(RtsUnit unit)
        {
            var lost = 0;

            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].Unit != unit)
                {
                    continue;
                }

                lost += _effects[i].ModifierCount;
                _effects.RemoveAt(i);
            }

            return lost;
        }

        /// <summary>Forgets a handle that a prune has already removed from the store.</summary>
        public void Forget(in StatModifierHandle handle)
        {
            var effects = _effects.AsReadOnlySpan();

            for (var i = 0; i < effects.Length; i++)
            {
                effects[i].Forget(handle);
            }
        }
    }
}
