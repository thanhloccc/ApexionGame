using Game.Common;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// One incoming hit, gathered from weapon and ammo data, before the target's armour is applied.
    /// </summary>
    public readonly record struct DamageInfo(
          CombatantId Attacker
        , ItemId Weapon
        , float Raw
        , float AmmoMultiplier
        , float ArmorPenetration
    )
    {
        /// <summary>
        /// A hit from a weapon that uses no ammunition — melee.
        /// </summary>
        public static DamageInfo Unmodified(CombatantId attacker, ItemId weapon, float raw)
            => new(attacker, weapon, raw, 1f, 0f);
    }
}
