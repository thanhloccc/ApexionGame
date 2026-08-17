using EncosyTower.EnumExtensions;
using Game.Common;

namespace Game.Gameplay.Weapons
{
    [EnumExtensions]
    public enum WeaponEventKind : byte
    {
        Undefined = 0,
        AttackActivated,
        ShotFired,
        ReloadStarted,
        ReloadFinished,
        WentEmpty,
    }

    public readonly record struct WeaponEvent(
          WeaponEventKind Kind
        , EquipmentSlot Slot
        , ItemId Weapon
        , float AtTime
        , float Damage
    );
}
