using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Turns a raw hit plus the target's armour into final damage. Pure, stateless.
    /// </summary>
    /// <remarks>
    /// Split out because the formula is the part that gets re-tuned most often, and tuning it
    /// should not require touching anything that owns state.
    /// </remarks>
    public static class DamageResolver
    {
        /// <summary>
        /// Armour value at which exactly half of incoming damage is absorbed.
        /// </summary>
        public const float ARMOR_CONSTANT = 100f;

        /// <summary>
        /// Floor applied to any hit that started with damage at all, so an extremely armoured
        /// target can still be killed rather than becoming a stalemate.
        /// </summary>
        public const float MIN_DAMAGE = 1f;

        /// <summary>
        /// Mitigation curve <c>armor / (armor + K)</c>.
        /// </summary>
        /// <remarks>
        /// Chosen over <c>raw - armor</c> because it can never go negative and has no threshold
        /// at which more armour stops mattering or makes the target immortal.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MitigationOf(float effectiveArmor)
        {
            var armor = math.max(0f, effectiveArmor);
            return armor / (armor + ARMOR_CONSTANT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EffectiveArmorOf(float targetArmor, float armorPenetration)
            => math.max(0f, targetArmor) * (1f - math.saturate(armorPenetration));

        public static float Resolve(in DamageInfo info, float targetArmor)
        {
            var raw = math.max(0f, info.Raw) * math.max(0f, info.AmmoMultiplier);

            if (raw <= 0f)
            {
                return 0f;
            }

            var effectiveArmor = EffectiveArmorOf(targetArmor, info.ArmorPenetration);
            var mitigated = raw * (1f - MitigationOf(effectiveArmor));

            return math.max(MIN_DAMAGE, mitigated);
        }
    }
}
