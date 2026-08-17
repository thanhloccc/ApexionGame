using System.Runtime.CompilerServices;

namespace Game.Common
{
    public static partial class EquipmentSlotExtensions
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EquipmentSlotMask ToMask(this EquipmentSlot self)
            => self switch {
                EquipmentSlot.Head => EquipmentSlotMask.Head,
                EquipmentSlot.Body => EquipmentSlotMask.Body,
                EquipmentSlot.Back => EquipmentSlotMask.Back,
                EquipmentSlot.Hands => EquipmentSlotMask.Hands,
                EquipmentSlot.Legs => EquipmentSlotMask.Legs,
                EquipmentSlot.Feet => EquipmentSlotMask.Feet,
                EquipmentSlot.Trinket => EquipmentSlotMask.Trinket,
                EquipmentSlot.PrimaryWeapon => EquipmentSlotMask.PrimaryWeapon,
                EquipmentSlot.SecondaryWeapon => EquipmentSlotMask.SecondaryWeapon,
                EquipmentSlot.MeleeWeapon => EquipmentSlotMask.MeleeWeapon,
                _ => EquipmentSlotMask.None,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Accepts(this EquipmentSlotMask self, EquipmentSlot slot)
        {
            var mask = slot.ToMask();
            return mask != EquipmentSlotMask.None && (self & mask) == mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGunSlot(this EquipmentSlot self)
            => self is EquipmentSlot.PrimaryWeapon or EquipmentSlot.SecondaryWeapon;
    }
}
