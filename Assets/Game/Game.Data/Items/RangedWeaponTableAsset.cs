using EncosyTower.Data;
using EncosyTower.Databases;
using Game.Common;

namespace Game.Data
{
    [DataTableAsset]
    public sealed partial class RangedWeaponTableAsset : DataTableAssetBase<ItemId, RangedWeaponData> { }

    [Data]
    public partial struct RangedWeaponData
    {
        public RangedWeaponData(
              ItemId id
            , ItemId ammo
            , WeaponFireMode fireMode
            , float baseDamage
            , float roundsPerMinute
            , int magazineCapacity
            , int burstCount
            , float reloadSeconds
            , float projectileSpeed = 0f
            , float projectileRadius = 0f
            , float projectileRange = 0f
            , float spreadDegrees = 0f
        ) : this()
        {
            _id = id;
            _ammo = ammo;
            _fireMode = fireMode;
            _baseDamage = baseDamage;
            _roundsPerMinute = roundsPerMinute;
            _magazineCapacity = magazineCapacity;
            _burstCount = burstCount;
            _reloadSeconds = reloadSeconds;
            _projectileSpeed = projectileSpeed;
            _projectileRadius = projectileRadius;
            _projectileRange = projectileRange;
            _spreadDegrees = spreadDegrees;
        }

        [DataProperty(typeof(ItemIdData))]
        public readonly ItemId Id => Get_Id();

        [DataProperty(typeof(ItemIdData))]
        public readonly ItemId Ammo => Get_Ammo();

        [DataProperty] public readonly WeaponFireMode FireMode => Get_FireMode();

        [DataProperty] public readonly float BaseDamage => Get_BaseDamage();

        [DataProperty] public readonly float RoundsPerMinute => Get_RoundsPerMinute();

        [DataProperty] public readonly int MagazineCapacity => Get_MagazineCapacity();

        [DataProperty] public readonly int BurstCount => Get_BurstCount();

        [DataProperty] public readonly float ReloadSeconds => Get_ReloadSeconds();

        [DataProperty] public readonly float ProjectileSpeed => Get_ProjectileSpeed();

        [DataProperty] public readonly float ProjectileRadius => Get_ProjectileRadius();

        [DataProperty] public readonly float ProjectileRange => Get_ProjectileRange();

        [DataProperty] public readonly float SpreadDegrees => Get_SpreadDegrees();

        public readonly override string ToString()
            => $"{_id} :: {_fireMode} :: {_roundsPerMinute}rpm x{_magazineCapacity}";
    }
}
