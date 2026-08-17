using System;
using System.Runtime.CompilerServices;
using ApexionGame.Entities.Stats;
using EncosyTower.Collections;
using EncosyTower.Common;
using Game.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using Unity.Collections;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Owns the stat store, every <see cref="Combatant"/>, and the registry they are tracked in,
    /// and drives the tick.
    /// </summary>
    /// <remarks>
    /// The stat store is world-scoped — one store holds every character as an owner — which is
    /// what makes this the natural place for combatant lifetime. Resolving attacks is delegated to
    /// <see cref="AttackResolver"/> so that lifetime and per-tick arithmetic change independently.
    /// </remarks>
    public sealed class CombatWorld : IDisposable
    {
        public const float DEFAULT_MAX_HEALTH = 100f;
        public const float DEFAULT_RADIUS = 0.5f;

        /// <summary>
        /// Modifier slots reserved per combatant. Four is one per stat in <see cref="CharacterStats"/>;
        /// the store grows past it if a character carries more modifiers than stats.
        /// </summary>
        private const int MODIFIER_SLOTS_PER_COMBATANT = 4;

        private readonly ItemCatalog _catalog;
        private readonly CombatantRegistry _registry;
        private readonly AttackResolver _resolver;
        private readonly ArrayMap<CombatantId, Combatant> _combatants;
        private readonly FasterList<CombatEvent> _events;
        private readonly int _capacity;

        private StatStore<GameStatSystem.Stat, GameStatSystem.StatModifier, GameStatSystem.StatObserver> _store;
        private GameStatSystem.Accessor _accessor;
        private GameStatSystem.WorldData _worldData;

        private int _lastId;
        private float _time;
        private bool _isDisposed;

        public CombatWorld(ItemCatalog catalog, int capacity = 64)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _capacity = capacity < 1 ? 1 : capacity;

            _registry = new CombatantRegistry(_capacity);
            _resolver = new AttackResolver(_catalog, _registry);
            _combatants = new ArrayMap<CombatantId, Combatant>(_capacity);
            _events = new FasterList<CombatEvent>(64);

            _store = new StatStore<GameStatSystem.Stat, GameStatSystem.StatModifier, GameStatSystem.StatObserver>(
                initialOwnerCapacity: _capacity, Allocator.Persistent);

            _accessor = new GameStatSystem.Accessor(_store);

            _worldData = new GameStatSystem.WorldData(
                _capacity * MODIFIER_SLOTS_PER_COMBATANT, Allocator.Persistent);
        }

        public CombatantRegistry Registry
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _registry;
        }

        /// <summary>
        /// What happened during the most recent <see cref="Tick"/>. Cleared at the start of the next.
        /// </summary>
        public ReadOnlySpan<CombatEvent> Events
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _events.AsReadOnlySpan();
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _combatants.Count;
        }

        public float Time
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _time;
        }

        /// <summary>
        /// The combatant with this id. Throws when there is none — use <see cref="Find"/> when the
        /// id may not be live.
        /// </summary>
        public Combatant this[CombatantId id]
            => _combatants[id];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<Combatant> Find(CombatantId id)
            => _combatants.TryGetValue(id, out var combatant) ? combatant : Option.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(CombatantId id, out Combatant combatant)
            => _combatants.TryGetValue(id, out combatant);

        public Result<CombatantId, CombatError> CreateCombatant(
              byte team
            , float weightCapacity = 40f
            , float maxHealth = DEFAULT_MAX_HEALTH
            , float armor = 0f
            , float moveSpeed = 5f
            , float radius = DEFAULT_RADIUS
        )
        {
            if (_combatants.Count >= _capacity)
            {
                return CombatError.CapacityReached(_capacity);
            }

            var stats = CharacterStats.Builder
                .Build(ref _store, out var owner)
                .CreateAllStats(produceChangeEvents: true)
                .ToStats();

            var generated = stats.GetStatHandles(owner);

            var handles = new CharacterStatHandles(
                  generated.maxHealth
                , generated.armor
                , generated.moveSpeed
                , generated.carryCapacity
            );

            SetBaseValue(handles.MaxHealth, maxHealth);
            SetBaseValue(handles.Armor, armor);
            SetBaseValue(handles.MoveSpeed, moveSpeed);
            SetBaseValue(handles.CarryCapacity, weightCapacity);

            var id = new CombatantId(++_lastId);
            var combatant = new Combatant(id, _catalog, handles, owner, weightCapacity, maxHealth);

            _combatants.TryAdd(id, combatant);
            _registry.Add(id, team, radius);

            return id;
        }

        public bool DestroyCombatant(CombatantId id)
        {
            if (_combatants.TryGetValue(id, out var combatant) == false)
            {
                return false;
            }

            combatant.Dispose();
            _combatants.Remove(id);
            _registry.Remove(id);

            return true;
        }

        public Success<CombatError> Equip(CombatantId id, EquippedItem item, EquipmentSlot slot)
            => _combatants.TryGetValue(id, out var combatant)
                ? combatant.Equip(item, slot, ref _accessor, ref _worldData)
                : CombatError.UnknownCombatant(id);

        public Result<EquippedItem, CombatError> Unequip(CombatantId id, EquipmentSlot slot)
            => _combatants.TryGetValue(id, out var combatant)
                ? combatant.Unequip(slot, ref _accessor, ref _worldData)
                : CombatError.UnknownCombatant(id);

        public float StatValueOf(CombatantId id, CharacterStatKind kind)
        {
            if (_combatants.TryGetValue(id, out var combatant) == false)
            {
                return 0f;
            }

            return combatant.Handles.Resolve(kind).TryGetValue(out var handle)
                ? ReadStat(handle)
                : 0f;
        }

        /// <summary>
        /// Advances every weapon machine, then resolves everything they produced.
        /// </summary>
        /// <remarks>
        /// The two passes are separate on purpose: every machine advances against start-of-tick
        /// state, so two combatants shooting each other in the same tick both land their shots
        /// regardless of which is visited first.
        /// </remarks>
        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            _events.Clear();

            foreach (var pair in _combatants)
            {
                pair.Value.Weapons.Tick(deltaTime);
            }

            foreach (var pair in _combatants)
            {
                _resolver.Resolve(pair.Value, _combatants, ref _accessor, _events, _time);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (var pair in _combatants)
            {
                pair.Value.Dispose();
            }

            _combatants.Dispose();
            _registry.Dispose();
            _events.Clear();

            if (_worldData.IsCreated)
            {
                _worldData.Dispose();
            }

            if (_store.IsCreated)
            {
                _store.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ReadStat(StatHandle handle)
            => _accessor.TryGetStatValue(handle, out var pair)
                ? pair.GetCurrentValueOrDefault(new StatVariant(0f)).Float
                : 0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBaseValue(StatHandle handle, float value)
            => _accessor.TrySetStatBaseValue(handle, new GameStatSystem.ValuePair(new StatVariant(value)), ref _worldData);
    }
}
