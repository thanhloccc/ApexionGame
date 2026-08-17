using EncosyTower.EnumExtensions;
using Game.Common;

namespace Game.Gameplay.Combat
{
    [EnumExtensions]
    public enum CombatEventKind : byte
    {
        Undefined = 0,
        DamageDealt,
        Died,
    }

    /// <summary>
    /// What combat produced during one tick, for anyone outside combat to react to.
    /// </summary>
    /// <remarks>
    /// Shaped like <c>WeaponEvent</c> on purpose — the two buffers are drained the same way.
    /// </remarks>
    public readonly record struct CombatEvent(
          CombatEventKind Kind
        , CombatantId Attacker
        , CombatantId Target
        , ItemId Weapon
        , float Amount
        , float AtTime
    );
}
