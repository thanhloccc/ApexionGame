using Game.Common;
using Game.Data;
using Game.Gameplay.Items;

namespace Game.Gameplay.Tests;

internal static class TestItems
{
    public static readonly ItemId Helmet = new(ItemId.IdKind.Equipment, 1);
    public static readonly ItemId Ring = new(ItemId.IdKind.Equipment, 2);
    public static readonly ItemId Rifle = new(ItemId.IdKind.RangedWeapon, 1);
    public static readonly ItemId Knife = new(ItemId.IdKind.MeleeWeapon, 1);
    public static readonly ItemId Revolver = new(ItemId.IdKind.RangedWeapon, 2);
    public static readonly ItemId Smg = new(ItemId.IdKind.RangedWeapon, 3);
    public static readonly ItemId BrokenGun = new(ItemId.IdKind.RangedWeapon, 4);
    public static readonly ItemId Bullet = new(ItemId.IdKind.Ammo, 1);
    public static readonly ItemId Missing = new(ItemId.IdKind.Equipment, 999);

    public const float HelmetWeight = 2f;
    public const float RingWeight = 0.5f;
    public const float RifleWeight = 3.5f;
    public const float KnifeWeight = 1f;

    public const int RifleMagazine = 30;
    public const float RifleRpm = 600f;
    public const float RifleInterval = 60f / RifleRpm;

    public const int RevolverMagazine = 6;
    public const int SmgMagazine = 20;
    public const int SmgBurstCount = 3;

    public const float KnifeWindUp = 0.1f;
    public const float KnifeActive = 0.15f;
    public const float KnifeRecovery = 0.25f;
    public const float ReloadSeconds = 2f;
    public const float HelmetArmorBonus = 5f;

    public const float KnifeDamage = 25f;
    public const float KnifeRange = 1.2f;
    public const float KnifeRadius = 0.4f;
    public const float KnifeArcDegrees = 60f;

    public const float RifleDamage = 18f;
    public const float RifleRange = 50f;
    public const float RevolverDamage = 40f;
    public const float RevolverRange = 25f;
    public const float SmgRange = 30f;
    public const float ProjectileRadius = 0.05f;

    public const float BulletDamageMultiplier = 1f;
    public const float BulletArmorPenetration = 0.2f;

    public static ItemCatalog CreateCatalog()
    {
        var items = new[] {
            new ItemData(
                  Helmet
                , null
                , HelmetWeight
                , EquipmentSlotMask.Head
                , 1
                , new[] {
                    new StatModifierData(CharacterStatKind.Armor, StatModifierOperation.Add, HelmetArmorBonus),
                }
            ),
            new ItemData(Ring, null, RingWeight, EquipmentSlotMask.Trinket, 2),
            new ItemData(Rifle, null, RifleWeight, EquipmentSlotMask.AnyGunSlot, 1),
            new ItemData(Knife, null, KnifeWeight, EquipmentSlotMask.MeleeWeapon, 1),
            new ItemData(Revolver, null, 1.2f, EquipmentSlotMask.AnyGunSlot, 1),
            new ItemData(Smg, null, 2.8f, EquipmentSlotMask.AnyGunSlot, 1),
            new ItemData(BrokenGun, null, 1f, EquipmentSlotMask.AnyGunSlot, 1),
            new ItemData(Bullet, null, 0.01f, EquipmentSlotMask.None, 0),
        };

        var equipment = new[] {
            new EquipmentData(Helmet, EquipmentClass.Medium, 12f),
            new EquipmentData(Ring, EquipmentClass.Light, 0f),
        };

        var melee = new[] {
            new MeleeWeaponData(
                  Knife, KnifeDamage, KnifeRange, KnifeRadius, KnifeArcDegrees
                , KnifeWindUp, KnifeActive, KnifeRecovery
            ),
        };

        var ranged = new[] {
            new RangedWeaponData(
                  Rifle, Bullet, WeaponFireMode.Auto
                , RifleDamage, RifleRpm, RifleMagazine, 3, ReloadSeconds
                , projectileRadius: ProjectileRadius, projectileRange: RifleRange
            ),
            new RangedWeaponData(
                  Revolver, Bullet, WeaponFireMode.Single
                , RevolverDamage, 300f, RevolverMagazine, 1, ReloadSeconds
                , projectileRadius: ProjectileRadius, projectileRange: RevolverRange
            ),
            new RangedWeaponData(
                  Smg, Bullet, WeaponFireMode.Burst
                , 12f, 900f, SmgMagazine, SmgBurstCount, ReloadSeconds
                , projectileRadius: ProjectileRadius, projectileRange: SmgRange
            ),
            new RangedWeaponData(
                  BrokenGun, Bullet, WeaponFireMode.Auto
                , 10f, 0f, 10, 1, ReloadSeconds
            ),
        };

        var ammo = new[] {
            new AmmoData(Bullet, BulletDamageMultiplier, BulletArmorPenetration, 60),
        };

        return new ItemCatalog(items, equipment, melee, ranged, ammo);
    }
}
