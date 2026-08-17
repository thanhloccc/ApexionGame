using System;
using System.Runtime.CompilerServices;
using ApexionGame.Entities.Stats;
using EncosyTower.Common;
using Game.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using Game.Gameplay.Weapons;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Everything belonging to one character: what it carries, what that does to its stats,
    /// the weapon machines driving it, and its hit points.
    /// </summary>
    /// <remarks>
    /// This exists because equipping an item is a single transaction across three owners —
    /// loadout, stat modifiers, weapon machine — and before this type nobody owned that
    /// transaction, so it never happened outside tests.
    /// </remarks>
    public sealed class Combatant : IDisposable
    {
        private readonly ItemCatalog _catalog;
        private readonly Loadout _loadout;
        private readonly EquipmentStatBinding _binding;
        private readonly WeaponController _weapons;
        private readonly CharacterStatHandles _handles;
        private readonly StatOwnerHandle _owner;

        private Health _health;
        private bool _isDisposed;

        public Combatant(
              CombatantId id
            , ItemCatalog catalog
            , in CharacterStatHandles handles
            , StatOwnerHandle owner
            , float weightCapacity
            , float startingHealth
        )
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _handles = handles;
            _owner = owner;

            _loadout = new Loadout(_catalog, weightCapacity);
            _binding = new EquipmentStatBinding(_catalog);
            _weapons = new WeaponController(_catalog, _loadout);
            _health = new Health(startingHealth);

            Id = id;
        }

        public CombatantId Id { get; }

        public Loadout Loadout
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _loadout;
        }

        public WeaponController Weapons
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _weapons;
        }

        public CharacterStatHandles Handles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handles;
        }

        public StatOwnerHandle Owner
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _owner;
        }

        public float CurrentHealth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _health.Current;
        }

        public bool IsDead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _health.IsDead;
        }

        /// <summary>
        /// Puts an item into a slot, applies its stat modifiers, and attaches its weapon machine.
        /// </summary>
        /// <remarks>
        /// All three or none. If the weapon machine refuses, the loadout and the modifiers are
        /// rolled back — a slot holding a weapon with no machine behind it would accept attack
        /// requests and silently do nothing, which is the worst failure mode available here.
        /// </remarks>
        public Success<CombatError> Equip(
              EquippedItem item
            , EquipmentSlot slot
            , ref GameStatSystem.Accessor accessor
            , ref GameStatSystem.WorldData worldData
        )
        {
            var result = _loadout.Equip(item, slot);

            if (result.TryGetError(out var itemError))
            {
                return CombatError.EquipRejected(Id, slot, item.Id, itemError);
            }

            result.TryGetValue(out var change);

            _binding.Apply(slot, change.Equipped.Id, _handles, ref accessor, ref worldData);

            // Armour and trinkets have no machine; asking for one would report NotAWeapon.
            if (IsWeapon(change.Equipped.Id) == false)
            {
                return Success.Yes;
            }

            var attached = _weapons.OnEquipped(slot);

            if (attached.TryGetFailure(out var weaponError))
            {
                Rollback(change, slot, ref accessor, ref worldData);
                return CombatError.WeaponAttachFailed(Id, slot, change.Equipped.Id, weaponError);
            }

            return Success.Yes;
        }

        /// <summary>
        /// Empties a slot and returns what was in it, rounds included.
        /// </summary>
        /// <remarks>
        /// Returning the item rather than discarding it keeps a magazine's remaining rounds
        /// alive until an inventory exists to receive them.
        /// </remarks>
        public Result<EquippedItem, CombatError> Unequip(
              EquipmentSlot slot
            , ref GameStatSystem.Accessor accessor
            , ref GameStatSystem.WorldData worldData
        )
        {
            if (_loadout.GetEquipped(slot).HasValue == false)
            {
                return CombatError.UnequipRejected(Id, slot, ItemError.SlotEmpty(slot));
            }

            // Detach first so no machine ticks against a slot that is being emptied.
            _weapons.OnUnequipped(slot);
            _binding.Remove(slot, ref accessor, ref worldData);

            var removed = _loadout.Unequip(slot);

            if (removed.TryGetError(out var itemError))
            {
                return CombatError.UnequipRejected(Id, slot, itemError);
            }

            removed.TryGetValue(out var equipped);

            return equipped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HealthChange ApplyDamage(float amount)
            => _health.ApplyDamage(amount);

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _weapons.Dispose();
            _binding.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsWeapon(ItemId id)
            => _catalog.FindMeleeWeapon(id).HasValue || _catalog.FindRangedWeapon(id).HasValue;

        /// <summary>
        /// Undoes a partially applied equip and restores whatever the slot held before it.
        /// </summary>
        private void Rollback(
              in EquipChange change
            , EquipmentSlot slot
            , ref GameStatSystem.Accessor accessor
            , ref GameStatSystem.WorldData worldData
        )
        {
            _weapons.OnUnequipped(slot);
            _binding.Remove(slot, ref accessor, ref worldData);
            _loadout.Unequip(slot);

            if (change.Replaced.TryGetValue(out var previous) == false)
            {
                return;
            }

            // Re-applied directly rather than through Equip: this combination already succeeded
            // once, and recursing would risk a rollback of a rollback.
            if (_loadout.Equip(previous, slot).TryGetValue(out var restored) == false)
            {
                return;
            }

            _binding.Apply(slot, restored.Equipped.Id, _handles, ref accessor, ref worldData);

            if (IsWeapon(restored.Equipped.Id))
            {
                _weapons.OnEquipped(slot);
            }
        }
    }
}
