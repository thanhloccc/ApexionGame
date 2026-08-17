using EncosyTower.EnumExtensions;

namespace Game.Gameplay.Weapons
{
    [EnumExtensions]
    public enum MeleeAttackState : byte
    {
        Idle = 0,
        WindUp,
        Active,
        Recovery,
    }

    [EnumExtensions]
    public enum RangedFireState : byte
    {
        Idle = 0,
        Firing,
        Cycling,
        Empty,
        Reloading,
    }

    [EnumExtensions]
    public enum WeaponTrigger : byte
    {
        Attack = 0,
        Reload,
        Cancel,
    }
}
