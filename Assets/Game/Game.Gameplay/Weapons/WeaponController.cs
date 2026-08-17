using System;
using ApexionGame.HFSM;
using EncosyTower.Collections;
using EncosyTower.Common;
using Game.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;

namespace Game.Gameplay.Weapons
{
    public sealed class WeaponController : IDisposable
    {
        private readonly ItemCatalog _catalog;
        private readonly Loadout _loadout;
        private readonly FasterList<WeaponEvent> _events;

        private readonly ArrayMap<EquipmentSlot, MeleeRuntime> _melee;
        private readonly ArrayMap<EquipmentSlot, RangedRuntime> _ranged;

        private MachineDefinition<MeleeWeaponContext, MeleeAttackState> _meleeDefinition;
        private MachineDefinition<RangedWeaponContext, RangedFireState> _rangedDefinition;

        private float _time;
        private bool _isDisposed;

        public WeaponController(ItemCatalog catalog, Loadout loadout)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            _events = new FasterList<WeaponEvent>(16);
            _melee = new ArrayMap<EquipmentSlot, MeleeRuntime>(4);
            _ranged = new ArrayMap<EquipmentSlot, RangedRuntime>(4);
        }

        public ReadOnlySpan<WeaponEvent> Events => _events.AsReadOnlySpan();

        public float Time => _time;

        public int DefinitionCount
            => (_meleeDefinition != null ? 1 : 0) + (_rangedDefinition != null ? 1 : 0);

        public void ClearEvents()
            => _events.Clear();

        public Success<WeaponError> OnEquipped(EquipmentSlot slot)
        {
            OnUnequipped(slot);

            if (_loadout.GetEquipped(slot).TryGetValue(out var equipped) == false)
            {
                return WeaponError.NoWeaponInSlot(slot);
            }

            if (_catalog.FindMeleeWeapon(equipped.Id).HasValue)
            {
                return AttachMelee(slot);
            }

            if (_catalog.FindRangedWeapon(equipped.Id).TryGetValue(out var ranged))
            {
                var valid = RangedFireMachine.Validate(equipped.Id, ranged);

                if (valid.TryGetFailure(out var invalid))
                {
                    return invalid;
                }

                return AttachRanged(slot);
            }

            return WeaponError.NotAWeapon(equipped.Id);
        }

        public void OnUnequipped(EquipmentSlot slot)
        {
            if (_melee.TryGetValue(slot, out var melee))
            {
                melee.Machine.Dispose();
                _melee.Remove(slot);
            }

            if (_ranged.TryGetValue(slot, out var ranged))
            {
                ranged.Machine.Dispose();
                _ranged.Remove(slot);
            }
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;

            foreach (var pair in _melee)
            {
                pair.Value.Context.CurrentTime = _time;
                pair.Value.Machine.Tick(deltaTime);
            }

            foreach (var pair in _ranged)
            {
                pair.Value.Context.CurrentTime = _time;
                pair.Value.Machine.Tick(deltaTime);
            }
        }

        public Success<WeaponError> RequestAttack(EquipmentSlot slot)
        {
            if (_melee.TryGetValue(slot, out var melee))
            {
                melee.Machine.Fire(WeaponTrigger.Attack);
                return Success.Yes;
            }

            if (_ranged.TryGetValue(slot, out var ranged))
            {
                ranged.Machine.Fire(WeaponTrigger.Attack);
                return Success.Yes;
            }

            return WeaponError.NoWeaponInSlot(slot);
        }

        public Success<WeaponError> RequestReload(EquipmentSlot slot)
        {
            if (_ranged.TryGetValue(slot, out var ranged) == false)
            {
                return WeaponError.NoWeaponInSlot(slot);
            }

            if (ranged.Context.CanReload == false)
            {
                return Success.Yes;
            }

            ranged.Machine.Fire(WeaponTrigger.Reload);
            return Success.Yes;
        }

        public void Cancel(EquipmentSlot slot)
        {
            if (_melee.TryGetValue(slot, out var melee))
            {
                melee.Machine.Fire(WeaponTrigger.Cancel);
            }

            if (_ranged.TryGetValue(slot, out var ranged))
            {
                ranged.Machine.Fire(WeaponTrigger.Cancel);
            }
        }

        public void SetAttackHeld(EquipmentSlot slot, bool held)
        {
            if (_ranged.TryGetValue(slot, out var ranged))
            {
                ranged.Context.AttackHeld = held;
            }
        }

        public Option<MeleeAttackState> MeleeStateOf(EquipmentSlot slot)
            => _melee.TryGetValue(slot, out var melee) ? melee.Machine.CurrentState : Option.None;

        public Option<RangedFireState> RangedStateOf(EquipmentSlot slot)
            => _ranged.TryGetValue(slot, out var ranged) ? ranged.Machine.CurrentState : Option.None;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (var pair in _melee)
            {
                pair.Value.Machine.Dispose();
            }

            foreach (var pair in _ranged)
            {
                pair.Value.Machine.Dispose();
            }

            _melee.Dispose();
            _ranged.Dispose();
        }

        private Success<WeaponError> AttachMelee(EquipmentSlot slot)
        {
            if (_meleeDefinition == null)
            {
                var built = MeleeAttackMachine.BuildOrError();

                if (built.TryGetError(out var error))
                {
                    return error;
                }

                built.TryGetValue(out _meleeDefinition);
            }

            var context = new MeleeWeaponContext(_catalog, _loadout, slot, _events);
            var machine = _meleeDefinition.CreateInstance(context, TickMode.Manual);

            _melee.TryAdd(slot, new MeleeRuntime(machine, context));

            return Success.Yes;
        }

        private Success<WeaponError> AttachRanged(EquipmentSlot slot)
        {
            if (_rangedDefinition == null)
            {
                var built = RangedFireMachine.BuildOrError();

                if (built.TryGetError(out var error))
                {
                    return error;
                }

                built.TryGetValue(out _rangedDefinition);
            }

            var context = new RangedWeaponContext(_catalog, _loadout, slot, _events);
            var machine = _rangedDefinition.CreateInstance(context, TickMode.Manual);

            _ranged.TryAdd(slot, new RangedRuntime(machine, context));

            return Success.Yes;
        }

        private readonly struct MeleeRuntime
        {
            public readonly HierarchicalStateMachine<MeleeWeaponContext, MeleeAttackState> Machine;
            public readonly MeleeWeaponContext Context;

            public MeleeRuntime(
                  HierarchicalStateMachine<MeleeWeaponContext, MeleeAttackState> machine
                , MeleeWeaponContext context
            )
            {
                Machine = machine;
                Context = context;
            }
        }

        private readonly struct RangedRuntime
        {
            public readonly HierarchicalStateMachine<RangedWeaponContext, RangedFireState> Machine;
            public readonly RangedWeaponContext Context;

            public RangedRuntime(
                  HierarchicalStateMachine<RangedWeaponContext, RangedFireState> machine
                , RangedWeaponContext context
            )
            {
                Machine = machine;
                Context = context;
            }
        }
    }
}
