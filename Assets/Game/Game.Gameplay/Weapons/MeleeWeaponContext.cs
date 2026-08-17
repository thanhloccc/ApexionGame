using System;
using EncosyTower.Collections;
using Game.Common;
using Game.Data;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;

namespace Game.Gameplay.Weapons
{
    public sealed class MeleeWeaponContext
    {
        private readonly ItemCatalog _catalog;
        private readonly Loadout _loadout;
        private readonly FasterList<WeaponEvent> _events;

        public MeleeWeaponContext(
              ItemCatalog catalog
            , Loadout loadout
            , EquipmentSlot slot
            , FasterList<WeaponEvent> events
        )
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Slot = slot;
        }

        public EquipmentSlot Slot { get; }

        public float CurrentTime { get; set; }

        public ItemId WeaponId
            => _loadout.GetEquipped(Slot).TryGetValue(out var equipped) ? equipped.Id : default;

        public float WindUpSeconds => Weapon(out var w) ? w.WindUpSeconds : 0f;

        public float ActiveSeconds => Weapon(out var w) ? w.ActiveSeconds : 0f;

        public float RecoverySeconds => Weapon(out var w) ? w.RecoverySeconds : 0f;

        public float BaseDamage => Weapon(out var w) ? w.BaseDamage : 0f;

        public void Emit(WeaponEventKind kind, float damage)
            => _events.Add(new WeaponEvent(kind, Slot, WeaponId, CurrentTime, damage));

        private bool Weapon(out MeleeWeaponData data)
        {
            if (_loadout.GetEquipped(Slot).TryGetValue(out var equipped))
            {
                return _catalog.FindMeleeWeapon(equipped.Id).TryGetValue(out data);
            }

            data = default;
            return false;
        }
    }
}
