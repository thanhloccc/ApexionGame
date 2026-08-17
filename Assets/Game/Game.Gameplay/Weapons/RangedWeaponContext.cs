using System;
using EncosyTower.Collections;
using Game.Common;
using Game.Data;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;

namespace Game.Gameplay.Weapons
{
    public sealed class RangedWeaponContext
    {
        private readonly ItemCatalog _catalog;
        private readonly Loadout _loadout;
        private readonly FasterList<WeaponEvent> _events;

        public RangedWeaponContext(
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

        public bool AttackHeld { get; set; }

        public float CurrentTime { get; set; }

        public int ShotsInBurst { get; private set; }

        public ItemId WeaponId
            => _loadout.GetEquipped(Slot).TryGetValue(out var equipped) ? equipped.Id : default;

        public int Rounds
            => _loadout.GetEquipped(Slot).TryGetValue(out var equipped)
                && equipped.Rounds.TryGetValue(out var rounds)
                ? rounds
                : 0;

        public int MagazineCapacity => Weapon(out var w) ? w.MagazineCapacity : 0;

        public float ReloadSeconds => Weapon(out var w) ? w.ReloadSeconds : 0f;

        public float BaseDamage => Weapon(out var w) ? w.BaseDamage : 0f;

        public WeaponFireMode FireMode => Weapon(out var w) ? w.FireMode : WeaponFireMode.Undefined;

        public float FireInterval
            => Weapon(out var w) && w.RoundsPerMinute > 0f ? 60f / w.RoundsPerMinute : 0f;

        public bool WantsAnotherShot
            => Rounds > 0 && FireMode switch {
                WeaponFireMode.Auto => AttackHeld,
                WeaponFireMode.Burst => ShotsInBurst < BurstCount,
                _ => false,
            };

        public bool CanReload => Rounds < MagazineCapacity;

        private int BurstCount => Weapon(out var w) ? w.BurstCount : 0;

        public void BeginBurst()
            => ShotsInBurst = 0;

        public void ConsumeRound()
        {
            var rounds = Rounds;

            if (rounds < 1)
            {
                return;
            }

            _loadout.SetRounds(Slot, rounds - 1);
            ShotsInBurst++;
            Emit(WeaponEventKind.ShotFired, BaseDamage);
        }

        public void Refill()
        {
            _loadout.SetRounds(Slot, MagazineCapacity);
            Emit(WeaponEventKind.ReloadFinished, 0f);
        }

        public void Emit(WeaponEventKind kind, float damage)
            => _events.Add(new WeaponEvent(kind, Slot, WeaponId, CurrentTime, damage));

        private bool Weapon(out RangedWeaponData data)
        {
            if (_loadout.GetEquipped(Slot).TryGetValue(out var equipped))
            {
                return _catalog.FindRangedWeapon(equipped.Id).TryGetValue(out data);
            }

            data = default;
            return false;
        }
    }
}
