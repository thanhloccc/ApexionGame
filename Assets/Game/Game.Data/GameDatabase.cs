using EncosyTower.Databases;
using EncosyTower.Naming;

namespace Game.Data
{
    [Database(NameCasing.SnakeLower, AssetName = "GameDatabaseAsset")]
    public readonly partial struct GameDatabase
    {
        [Table] public readonly ItemTableAsset Items => Get_Items();

        [Table] public readonly EquipmentTableAsset Equipment => Get_Equipment();

        [Table] public readonly MeleeWeaponTableAsset MeleeWeapons => Get_MeleeWeapons();

        [Table] public readonly RangedWeaponTableAsset RangedWeapons => Get_RangedWeapons();

        [Table] public readonly AmmoTableAsset Ammo => Get_Ammo();
    }
}
