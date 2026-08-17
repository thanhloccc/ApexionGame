using System;
using EncosyTower.EnumExtensions;

namespace Game.Common
{
    [Flags]
    [EnumExtensions]
    public enum EquipmentSlotMask : ushort
    {
        None = 0,
        Head = 1 << 0,
        Body = 1 << 1,
        Back = 1 << 2,
        Hands = 1 << 3,
        Legs = 1 << 4,
        Feet = 1 << 5,
        Trinket = 1 << 6,
        PrimaryWeapon = 1 << 7,
        SecondaryWeapon = 1 << 8,
        MeleeWeapon = 1 << 9,

        AnyGunSlot = PrimaryWeapon | SecondaryWeapon,
        AnyWeaponSlot = AnyGunSlot | MeleeWeapon,
    }
}
