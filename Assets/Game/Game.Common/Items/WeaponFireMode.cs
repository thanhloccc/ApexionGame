using EncosyTower.EnumExtensions;

namespace Game.Common
{
    [EnumExtensions]
    public enum WeaponFireMode : byte
    {
        Undefined = 0,
        Single,
        Burst,
        Auto,
    }
}
