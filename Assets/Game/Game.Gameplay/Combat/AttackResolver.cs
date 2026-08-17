using System;
using ApexionGame.Entities.Stats;
using EncosyTower.Collections;
using EncosyTower.Pooling;
using Game.Common;
using Game.Gameplay.Items;
using Game.Gameplay.Weapons;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Turns what a combatant's weapons produced this tick into damage and combat events:
    /// drain → find who was reached → resolve the amount → apply it → report it.
    /// </summary>
    /// <remarks>
    /// Owns no state. Everything it reads or writes arrives as a parameter, which is what keeps it
    /// testable on its own and keeps <see cref="CombatWorld"/> to lifetime and ownership.
    /// Not to be confused with <see cref="DamageResolver"/>, which is only the arithmetic.
    /// </remarks>
    public sealed class AttackResolver
    {
        private readonly ItemCatalog _catalog;
        private readonly CombatantRegistry _registry;

        public AttackResolver(ItemCatalog catalog, CombatantRegistry registry)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Consumes <paramref name="attacker"/>'s pending weapon events and appends what happened
        /// to <paramref name="output"/>.
        /// </summary>
        public void Resolve(
              Combatant attacker
            , ArrayMap<CombatantId, Combatant> combatants
            , ref GameStatSystem.Accessor accessor
            , FasterList<CombatEvent> output
            , float time
        )
        {
            var weaponEvents = attacker.Weapons.Events;

            if (weaponEvents.Length < 1)
            {
                return;
            }

            if (_registry.Find(attacker.Id).TryGetValue(out var attackerSpatial) == false
                || attackerSpatial.IsAlive == false
            )
            {
                attacker.Weapons.ClearEvents();
                return;
            }

            using var pooled = FasterListPool<CombatantId>.Get(out var hits);

            for (var i = 0; i < weaponEvents.Length; i++)
            {
                ref readonly var weaponEvent = ref weaponEvents[i];

                if (LandsDamage(weaponEvent.Kind) == false)
                {
                    continue;
                }

                hits.Clear();

                if (FindHits(weaponEvent.Weapon, attackerSpatial, hits) < 1)
                {
                    continue;
                }

                ApplyHits(attacker.Id, weaponEvent, hits, combatants, ref accessor, output, time);
            }

            attacker.Weapons.ClearEvents();
        }

        private static bool LandsDamage(WeaponEventKind kind)
            => kind is WeaponEventKind.AttackActivated or WeaponEventKind.ShotFired;

        /// <summary>
        /// Picks the query that suits the weapon.
        /// </summary>
        /// <remarks>
        /// Two branches do not yet earn an abstraction. When a third weapon kind appears —
        /// travelling projectiles are the expected one — extract a hit profile instead of
        /// extending this chain.
        /// </remarks>
        private int FindHits(ItemId weapon, in CombatantSpatial attacker, FasterList<CombatantId> hits)
        {
            if (_catalog.FindMeleeWeapon(weapon).TryGetValue(out var melee))
            {
                return HitQuery.Melee(
                      _registry.Spatials
                    , attacker
                    , melee.Range
                    , melee.Radius
                    , melee.ArcDegrees
                    , hits
                );
            }

            if (_catalog.FindRangedWeapon(weapon).TryGetValue(out var ranged))
            {
                return HitQuery.Ranged(
                      _registry.Spatials
                    , attacker
                    , ranged.ProjectileRange
                    , ranged.ProjectileRadius
                    , hits
                );
            }

            return 0;
        }

        private void ApplyHits(
              CombatantId attackerId
            , in WeaponEvent weaponEvent
            , FasterList<CombatantId> hits
            , ArrayMap<CombatantId, Combatant> combatants
            , ref GameStatSystem.Accessor accessor
            , FasterList<CombatEvent> output
            , float time
        )
        {
            var info = BuildDamageInfo(attackerId, weaponEvent);

            for (var i = 0; i < hits.Count; i++)
            {
                var targetId = hits[i];

                if (combatants.TryGetValue(targetId, out var target) == false || target.IsDead)
                {
                    continue;
                }

                var armor = ReadArmor(target, ref accessor);
                var change = target.ApplyDamage(DamageResolver.Resolve(info, armor));

                if (change.Applied <= 0f)
                {
                    continue;
                }

                output.Add(new CombatEvent(
                      CombatEventKind.DamageDealt
                    , attackerId
                    , targetId
                    , weaponEvent.Weapon
                    , change.Applied
                    , time
                ));

                if (change.JustDied == false)
                {
                    continue;
                }

                // Mirror the death into the query array so nothing keeps shooting the corpse.
                _registry.MarkDead(targetId);

                output.Add(new CombatEvent(
                      CombatEventKind.Died
                    , attackerId
                    , targetId
                    , weaponEvent.Weapon
                    , 0f
                    , time
                ));
            }
        }

        /// <summary>
        /// Ranged weapons take their multiplier and penetration from the ammunition they are
        /// chambered for; melee weapons have neither.
        /// </summary>
        private DamageInfo BuildDamageInfo(CombatantId attackerId, in WeaponEvent weaponEvent)
        {
            if (_catalog.FindRangedWeapon(weaponEvent.Weapon).TryGetValue(out var ranged)
                && _catalog.FindAmmo(ranged.Ammo).TryGetValue(out var ammo)
            )
            {
                return new DamageInfo(
                      attackerId
                    , weaponEvent.Weapon
                    , weaponEvent.Damage
                    , ammo.DamageMultiplier
                    , ammo.ArmorPenetration
                );
            }

            return DamageInfo.Unmodified(attackerId, weaponEvent.Weapon, weaponEvent.Damage);
        }

        private static float ReadArmor(Combatant target, ref GameStatSystem.Accessor accessor)
            => accessor.TryGetStatValue(target.Handles.Armor, out var pair)
                ? pair.GetCurrentValueOrDefault(new StatVariant(0f)).Float
                : 0f;
    }
}
