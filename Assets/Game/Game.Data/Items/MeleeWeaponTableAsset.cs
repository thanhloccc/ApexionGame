using EncosyTower.Data;
using EncosyTower.Databases;
using Game.Common;

namespace Game.Data
{
    [DataTableAsset]
    public sealed partial class MeleeWeaponTableAsset : DataTableAssetBase<ItemId, MeleeWeaponData> { }

    [Data]
    public partial struct MeleeWeaponData
    {
        public MeleeWeaponData(
              ItemId id
            , float baseDamage
            , float range
            , float radius
            , float arcDegrees
            , float windUpSeconds
            , float activeSeconds
            , float recoverySeconds
        ) : this()
        {
            _id = id;
            _baseDamage = baseDamage;
            _range = range;
            _radius = radius;
            _arcDegrees = arcDegrees;
            _windUpSeconds = windUpSeconds;
            _activeSeconds = activeSeconds;
            _recoverySeconds = recoverySeconds;
        }

        [DataProperty(typeof(ItemIdData))]
        public readonly ItemId Id => Get_Id();

        [DataProperty] public readonly float BaseDamage => Get_BaseDamage();

        [DataProperty] public readonly float Range => Get_Range();

        [DataProperty] public readonly float Radius => Get_Radius();

        [DataProperty] public readonly float ArcDegrees => Get_ArcDegrees();

        [DataProperty] public readonly float WindUpSeconds => Get_WindUpSeconds();

        [DataProperty] public readonly float ActiveSeconds => Get_ActiveSeconds();

        [DataProperty] public readonly float RecoverySeconds => Get_RecoverySeconds();

        public readonly override string ToString()
            => $"{_id} :: dmg {_baseDamage} :: {_windUpSeconds}/{_activeSeconds}/{_recoverySeconds}";
    }
}
