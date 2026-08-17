using EncosyTower.Data;
using EncosyTower.Databases;
using Game.Common;

namespace Game.Data
{
    [DataTableAsset]
    public sealed partial class AmmoTableAsset : DataTableAssetBase<ItemId, AmmoData> { }

    [Data]
    public partial struct AmmoData
    {
        public AmmoData(
              ItemId id
            , float damageMultiplier
            , float armorPenetration
            , int stackMax
        ) : this()
        {
            _id = id;
            _damageMultiplier = damageMultiplier;
            _armorPenetration = armorPenetration;
            _stackMax = stackMax;
        }

        [DataProperty(typeof(ItemIdData))]
        public readonly ItemId Id => Get_Id();

        [DataProperty] public readonly float DamageMultiplier => Get_DamageMultiplier();

        [DataProperty] public readonly float ArmorPenetration => Get_ArmorPenetration();

        [DataProperty] public readonly int StackMax => Get_StackMax();

        public readonly override string ToString()
            => $"{_id} :: x{_damageMultiplier} :: ap {_armorPenetration}";
    }
}
