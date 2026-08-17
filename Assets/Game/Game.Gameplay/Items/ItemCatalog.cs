using System;
using System.Runtime.CompilerServices;
using EncosyTower.Collections;
using EncosyTower.Common;
using Game.Common;
using Game.Data;

namespace Game.Gameplay.Items
{
    public sealed class ItemCatalog : IDisposable
    {
        private readonly ArrayMap<ItemId, ItemData> _items;
        private readonly ArrayMap<ItemId, EquipmentData> _equipment;
        private readonly ArrayMap<ItemId, MeleeWeaponData> _meleeWeapons;
        private readonly ArrayMap<ItemId, RangedWeaponData> _rangedWeapons;
        private readonly ArrayMap<ItemId, AmmoData> _ammo;

        private bool _isDisposed;

        public ItemCatalog(
              ReadOnlySpan<ItemData> items
            , ReadOnlySpan<EquipmentData> equipment
            , ReadOnlySpan<MeleeWeaponData> meleeWeapons
            , ReadOnlySpan<RangedWeaponData> rangedWeapons
            , ReadOnlySpan<AmmoData> ammo
        )
        {
            _items = new ArrayMap<ItemId, ItemData>(items.Length);
            _equipment = new ArrayMap<ItemId, EquipmentData>(equipment.Length);
            _meleeWeapons = new ArrayMap<ItemId, MeleeWeaponData>(meleeWeapons.Length);
            _rangedWeapons = new ArrayMap<ItemId, RangedWeaponData>(rangedWeapons.Length);
            _ammo = new ArrayMap<ItemId, AmmoData>(ammo.Length);

            for (var i = 0; i < items.Length; i++)
            {
                _items.TryAdd(items[i].Id, items[i]);
            }

            for (var i = 0; i < equipment.Length; i++)
            {
                _equipment.TryAdd(equipment[i].Id, equipment[i]);
            }

            for (var i = 0; i < meleeWeapons.Length; i++)
            {
                _meleeWeapons.TryAdd(meleeWeapons[i].Id, meleeWeapons[i]);
            }

            for (var i = 0; i < rangedWeapons.Length; i++)
            {
                _rangedWeapons.TryAdd(rangedWeapons[i].Id, rangedWeapons[i]);
            }

            for (var i = 0; i < ammo.Length; i++)
            {
                _ammo.TryAdd(ammo[i].Id, ammo[i]);
            }
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _items.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<ItemData> Find(ItemId id)
            => _items.TryGetValue(id, out var value) ? value : Option.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<EquipmentData> FindEquipment(ItemId id)
            => _equipment.TryGetValue(id, out var value) ? value : Option.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<MeleeWeaponData> FindMeleeWeapon(ItemId id)
            => _meleeWeapons.TryGetValue(id, out var value) ? value : Option.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<RangedWeaponData> FindRangedWeapon(ItemId id)
            => _rangedWeapons.TryGetValue(id, out var value) ? value : Option.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<AmmoData> FindAmmo(ItemId id)
            => _ammo.TryGetValue(id, out var value) ? value : Option.None;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _items.Dispose();
            _equipment.Dispose();
            _meleeWeapons.Dispose();
            _rangedWeapons.Dispose();
            _ammo.Dispose();
        }
    }
}
