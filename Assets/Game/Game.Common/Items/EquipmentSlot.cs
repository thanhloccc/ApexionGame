using EncosyTower.EnumExtensions;

namespace Game.Common
{
    [EnumExtensions]
    public enum EquipmentSlot : byte
    {
        Undefined = 0,
        Head,
        Body,
        Back,
        Hands,
        Legs,
        Feet,
        Trinket,
        PrimaryWeapon,
        SecondaryWeapon,
        MeleeWeapon,
    }
}
