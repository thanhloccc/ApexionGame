using EncosyTower.Common;
using Game.Common;
using Game.Data;
using Game.Gameplay.Items;

namespace Game.Gameplay.Equipment
{
    public static class EquipmentRules
    {
        public static Success<ItemError> CanEquip(
              in ItemData item
            , EquipmentSlot slot
            , float carriedWeightWithoutSlot
            , float weightCapacity
            , int equippedCopiesElsewhere
        )
        {
            if (item.SlotMask.Accepts(slot) == false)
            {
                return ItemError.SlotNotCompatible(item.Id, slot);
            }

            var max = item.MaxEquippedCopies;

            if (max > 0 && equippedCopiesElsewhere >= max)
            {
                return ItemError.TooManyCopies(item.Id, max);
            }

            var attempted = carriedWeightWithoutSlot + item.Weight;

            if (attempted > weightCapacity)
            {
                return ItemError.OverWeight(item.Id, attempted, weightCapacity);
            }

            return Success.Yes;
        }

        public static Success<ItemError> CanSetRounds(
              in ItemData item
            , in RangedWeaponData weapon
            , int rounds
        )
        {
            if (rounds < 0 || rounds > weapon.MagazineCapacity)
            {
                return ItemError.MagazineOverflow(item.Id, weapon.MagazineCapacity);
            }

            return Success.Yes;
        }
    }
}
