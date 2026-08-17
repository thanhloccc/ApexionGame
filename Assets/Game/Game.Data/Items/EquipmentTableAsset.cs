using EncosyTower.Data;
using EncosyTower.Databases;
using Game.Common;

namespace Game.Data
{
    [DataTableAsset]
    public sealed partial class EquipmentTableAsset : DataTableAssetBase<ItemId, EquipmentData> { }

    [Data]
    public partial struct EquipmentData
    {
        public EquipmentData(ItemId id, EquipmentClass equipmentClass, float armorRating) : this()
        {
            _id = id;
            _class = equipmentClass;
            _armorRating = armorRating;
        }

        [DataProperty(typeof(ItemIdData))]
        public readonly ItemId Id => Get_Id();

        [DataProperty] public readonly EquipmentClass Class => Get_Class();

        [DataProperty] public readonly float ArmorRating => Get_ArmorRating();

        public readonly override string ToString()
            => $"{_id} :: {_class} :: armor {_armorRating}";
    }
}
