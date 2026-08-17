using System;
using System.Runtime.CompilerServices;
using EncosyTower.Common;
using Game.Common;
using Game.Data;
using Game.Gameplay.Items;

namespace Game.Gameplay.Equipment
{
    public sealed class Loadout
    {
        private static readonly int s_slotCount = (int)EquipmentSlot.MeleeWeapon + 1;

        private readonly ItemCatalog _catalog;
        private readonly Option<EquippedItem>[] _slots;
        private readonly float _weightCapacity;

        public Loadout(ItemCatalog catalog, float weightCapacity)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _weightCapacity = weightCapacity;
            _slots = new Option<EquippedItem>[s_slotCount];
        }

        public float WeightCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _weightCapacity;
        }

        public float CarriedWeight
        {
            get => WeightExcluding(EquipmentSlot.Undefined);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<EquippedItem> GetEquipped(EquipmentSlot slot)
            => IsSlotIndexValid(slot) ? _slots[(int)slot] : Option.None;

        public Result<EquipChange, ItemError> Equip(EquippedItem item, EquipmentSlot slot)
        {
            if (IsSlotIndexValid(slot) == false)
            {
                return ItemError.SlotNotCompatible(item.Id, slot);
            }

            if (_catalog.Find(item.Id).TryGetValue(out var data) == false)
            {
                return ItemError.UnknownItem(item.Id);
            }

            var check = EquipmentRules.CanEquip(
                  data
                , slot
                , WeightExcluding(slot)
                , _weightCapacity
                , CountCopiesExcluding(item.Id, slot)
            );

            if (check.TryGetFailure(out var failure))
            {
                return failure;
            }

            var roundsResult = ResolveRounds(item, data, slot);

            if (roundsResult.TryGetError(out var roundsError))
            {
                return roundsError;
            }

            roundsResult.TryGetValue(out var rounds);

            var previous = _slots[(int)slot];
            var equipped = new EquippedItem(item.Id, rounds);

            _slots[(int)slot] = equipped;

            return new EquipChange(equipped, slot, previous);
        }

        public Result<EquippedItem, ItemError> Unequip(EquipmentSlot slot)
        {
            if (IsSlotIndexValid(slot) == false || _slots[(int)slot].TryGetValue(out var equipped) == false)
            {
                return ItemError.SlotEmpty(slot);
            }

            _slots[(int)slot] = Option.None;

            return equipped;
        }

        public Result<int, ItemError> SetRounds(EquipmentSlot slot, int rounds)
        {
            if (IsSlotIndexValid(slot) == false || _slots[(int)slot].TryGetValue(out var equipped) == false)
            {
                return ItemError.SlotEmpty(slot);
            }

            if (_catalog.FindRangedWeapon(equipped.Id).TryGetValue(out var weapon) == false)
            {
                return ItemError.NotARangedWeapon(slot);
            }

            if (_catalog.Find(equipped.Id).TryGetValue(out var data) == false)
            {
                return ItemError.UnknownItem(equipped.Id);
            }

            var check = EquipmentRules.CanSetRounds(data, weapon, rounds);

            if (check.TryGetFailure(out var failure))
            {
                return failure;
            }

            _slots[(int)slot] = new EquippedItem(equipped.Id, rounds);

            return rounds;
        }

        private Result<Option<int>, ItemError> ResolveRounds(
              in EquippedItem item
            , in ItemData data
            , EquipmentSlot slot
        )
        {
            if (_catalog.FindRangedWeapon(item.Id).TryGetValue(out var weapon) == false)
            {
                return Option<int>.None;
            }

            if (item.Rounds.TryGetValue(out var requested) == false)
            {
                Option<int> full = weapon.MagazineCapacity;
                return full;
            }

            var check = EquipmentRules.CanSetRounds(data, weapon, requested);

            if (check.TryGetFailure(out var failure))
            {
                return failure;
            }

            Option<int> resolved = requested;
            return resolved;
        }

        private float WeightExcluding(EquipmentSlot slot)
        {
            var total = 0f;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (i == (int)slot)
                {
                    continue;
                }

                if (_slots[i].TryGetValue(out var equipped) == false)
                {
                    continue;
                }

                if (_catalog.Find(equipped.Id).TryGetValue(out var data))
                {
                    total += data.Weight;
                }
            }

            return total;
        }

        private int CountCopiesExcluding(ItemId id, EquipmentSlot slot)
        {
            var count = 0;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (i == (int)slot)
                {
                    continue;
                }

                if (_slots[i].TryGetValue(out var equipped) && equipped.Id == id)
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSlotIndexValid(EquipmentSlot slot)
            => slot != EquipmentSlot.Undefined && (int)slot < s_slotCount;
    }
}
