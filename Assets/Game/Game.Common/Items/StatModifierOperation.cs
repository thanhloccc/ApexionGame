using EncosyTower.EnumExtensions;

namespace Game.Common
{
    [EnumExtensions]
    public enum StatModifierOperation : byte
    {
        Undefined = 0,
        Add,
        Multiply,
    }
}
