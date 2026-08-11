using EncosyTower.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Turns an owner into readable rows: base value, current value, and every modifier in between.
    /// </summary>
    /// <remarks>
    /// This is the part worth copying into a real project. It reads the <b>store</b> rather than the game's own
    /// bookkeeping, so what it shows is what the graph holds — including modifiers the game forgot it added.
    /// <para>
    /// Note what the runtime does not store: a modifier has no name. Every description below is derived from
    /// the modifier's own data — its kind, its factor, and the stat it observes — which is why
    /// <see cref="LabelOf"/> exists to name the far end of an edge.
    /// </para>
    /// </remarks>
    public sealed class RtsStatInspector
    {
        private readonly RtsMatchContext _context;
        private readonly RtsEffectSystem _effects;
        private readonly RtsGraphProbe _probe;

        private readonly FasterList<RtsEffect> _effectScratch = new(8);

        public RtsStatInspector(RtsMatchContext context, RtsEffectSystem effects, RtsGraphProbe probe)
        {
            _context = context;
            _effects = effects;
            _probe = probe;
        }

        public void Describe(RtsUnit unit, FasterList<RtsInspectorRow> rows)
        {
            rows.Clear();

            if (unit == null || unit.Alive == false)
            {
                return;
            }

            var world = _context.World;
            var handles = unit.Handles;

            rows.Add(RtsInspectorRow.Section("stats"));

            rows.Add(new RtsInspectorRow(RtsRowKind.Stat, "Hp"
                , $"{world.Read(handles.hp):0.#} / {world.Read(handles.maxHp):0.#}"));

            AddModifiers(handles.hp, rows);

            AddStat(handles.attack, nameof(UnitStats.Attack), rows);
            AddStat(handles.armor, nameof(UnitStats.Armor), rows);
            AddStat(handles.moveSpeed, nameof(UnitStats.MoveSpeed), rows);
            AddStat(handles.attackInterval, nameof(UnitStats.AttackInterval), rows);
            AddStat(handles.attackRange, nameof(UnitStats.AttackRange), rows);

            if (unit.IsHero)
            {
                AddStat(handles.auraPower, nameof(UnitStats.AuraPower), rows);
            }

            _effects.CollectOn(unit, _effectScratch);

            if (_effectScratch.Count > 0)
            {
                rows.Add(RtsInspectorRow.Section("effects"));

                var effects = _effectScratch.AsReadOnlySpan();

                for (var i = 0; i < effects.Length; i++)
                {
                    rows.Add(new RtsInspectorRow(RtsRowKind.Effect
                        , effects[i].Spell.Name
                        , $"{effects[i].Remaining:0.#}s · {effects[i].ModifierCount} mod"));
                }
            }

            rows.Add(RtsInspectorRow.Section("graph"));
            rows.Add(RtsInspectorRow.Note("modifiers on this unit", _probe.ModifiersOn(unit).ToString()));

            var interval = world.Read(handles.attackInterval);

            rows.Add(RtsInspectorRow.Note("attack per second"
                , $"{(interval <= 0f ? 0f : world.Read(handles.attack) / interval):0.##}"));

            if (unit.IsHero)
            {
                rows.Add(RtsInspectorRow.Note("level / kills", $"{unit.Level} / {unit.Kills}"));
                rows.Add(RtsInspectorRow.Note("aura terms on the node", unit.AuraTerms.Length.ToString()));
            }
        }

        public void Describe(RtsTeam team, FasterList<RtsInspectorRow> rows)
        {
            rows.Clear();

            rows.Add(RtsInspectorRow.Section("shared node"));

            AddStat(team.Handles.attackBonus, nameof(TeamStats.AttackBonus), rows);
            AddStat(team.Handles.armorBonus, nameof(TeamStats.ArmorBonus), rows);
            AddStat(team.Handles.moveSpeedBonus, nameof(TeamStats.MoveSpeedBonus), rows);

            rows.Add(RtsInspectorRow.Section("graph"));

            rows.Add(RtsInspectorRow.Note("observers (units reading this node)"
                , _probe.ObserversOnNode(team.Index).ToString()));

            rows.Add(RtsInspectorRow.Note("modifiers (hero aura terms)"
                , _probe.ModifiersOnNode(team.Index).ToString()));

            rows.Add(RtsInspectorRow.Note("living units", team.Units.Count.ToString()));
            rows.Add(RtsInspectorRow.Note("spawned / lost", $"{team.Spawned} / {team.Lost}"));

            if (team.SloppyDeaths > 0)
            {
                rows.Add(RtsInspectorRow.Note("sloppy deaths (leaked observers)"
                    , team.SloppyDeaths.ToString()));
            }
        }

        private void AddStat(in StatHandle handle, string name, FasterList<RtsInspectorRow> rows)
        {
            var world = _context.World;
            var baseValue = world.ReadBase(handle);
            var current = world.Read(handle);

            rows.Add(new RtsInspectorRow(RtsRowKind.Stat, name
                , math.abs(baseValue - current) < 0.005f
                    ? $"{current:0.##}"
                    : $"{baseValue:0.##} → {current:0.##}"));

            AddModifiers(handle, rows);
        }

        private void AddModifiers(in StatHandle handle, FasterList<RtsInspectorRow> rows)
        {
            var records = new NativeList<RtsStatSystem.StatModifierRecord>(8, Allocator.Temp);

            if (_context.World.TryReadModifiers(handle, records))
            {
                for (var i = 0; i < records.Length; i++)
                {
                    rows.Add(new RtsInspectorRow(RtsRowKind.Modifier
                        , Describe(records[i].modifier)
                        , string.Empty));
                }
            }

            records.Dispose();
        }

        private string Describe(in RtsStatSystem.StatModifier modifier)
            => modifier.kind switch {
                RtsStatSystem.StatModifier.Kind.Add
                    => $"{(modifier.value.Float >= 0f ? "+" : "")}{modifier.value.Float:0.##}",

                RtsStatSystem.StatModifier.Kind.Multiply
                    => $"× {modifier.value.Float:0.##}",

                RtsStatSystem.StatModifier.Kind.AddFromStat
                    => $"+ {LabelOf(modifier.observedStat)}",

                RtsStatSystem.StatModifier.Kind.AddFractionOfStat
                    => $"+ {modifier.value.Float * 100f:0.#}% of {LabelOf(modifier.observedStat)}",

                RtsStatSystem.StatModifier.Kind.ClampMaxFromStat
                    => $"≤ {LabelOf(modifier.observedStat)}",

                RtsStatSystem.StatModifier.Kind.ClampMinConstant
                    => $"≥ {modifier.value.Float:0.##}",

                _ => modifier.kind.ToString(),
            };

        /// <summary>Names the far end of an edge: which owner, and which of its stats.</summary>
        public string LabelOf(in StatHandle handle)
            => $"{OwnerLabel(handle.owner)}.{StatName(handle)}";

        private string OwnerLabel(in StatOwnerHandle owner)
        {
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                if (teams[t].Owner == owner)
                {
                    return $"{teams[t].Name}/node";
                }
            }

            return _context.TryGetUnit(owner, out var unit) ? unit.Label : "(gone)";
        }

        /// <remarks>
        /// A bare <see cref="StatHandle"/> does not say which <c>[StatCollection]</c> built its owner, so this
        /// is the one place that can answer — which is also what the store debugger is handed at startup.
        /// </remarks>
        public string StatName(in StatHandle handle)
        {
            var index = handle.index.value;
            var teams = _context.Teams;

            for (var t = 0; t < teams.Length; t++)
            {
                if (teams[t].Owner == handle.owner)
                {
                    return ((TeamStats.Type)index).ToString();
                }
            }

            return _context.TryGetUnit(handle.owner, out _)
                ? ((UnitStats.Type)index).ToString()
                : string.Empty;
        }

        /// <summary>The name resolver the store debugger asks for.</summary>
        public string ResolveStatName(StatOwnerHandle owner, int index)
            => StatName(new StatHandle(owner, new StatIndex { value = index }));
    }
}
