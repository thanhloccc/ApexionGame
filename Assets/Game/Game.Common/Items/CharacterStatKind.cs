using EncosyTower.EnumExtensions;

namespace Game.Common
{
    [EnumExtensions]
    public enum CharacterStatKind : byte
    {
        Undefined = 0,
        MaxHealth,
        Armor,
        MoveSpeed,
        CarryCapacity,
    }
}
