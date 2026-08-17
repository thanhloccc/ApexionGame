using System;
using EncosyTower.Data;
using EncosyTower.Databases;
using Game.Common;

namespace Game.Data
{
    [DataTableAsset]
    public sealed partial class ItemTableAsset : DataTableAssetBase<ItemId, ItemData> { }

    [Data]
    public partial struct ItemData
    {
        public ItemData(
              ItemId id
            , string displayKey
            , float weight
            , EquipmentSlotMask slotMask
            , byte maxEquippedCopies
            , StatModifierData[] modifiers = null
        ) : this()
        {
            _id = id;
            _displayKey = displayKey;
            _weight = weight;
            _slotMask = slotMask;
            _maxEquippedCopies = maxEquippedCopies;
            _modifiers = modifiers ?? Array.Empty<StatModifierData>();
        }

        [DataProperty(typeof(ItemIdData))]
        public readonly ItemId Id => Get_Id();

        [DataProperty] public readonly string DisplayKey => Get_DisplayKey();

        [DataProperty] public readonly float Weight => Get_Weight();

        [DataProperty] public readonly EquipmentSlotMask SlotMask => Get_SlotMask();

        [DataProperty] public readonly byte MaxEquippedCopies => Get_MaxEquippedCopies();

        [DataProperty] public readonly ReadOnlyMemory<StatModifierData> Modifiers => Get_Modifiers();

        public readonly override string ToString()
            => $"{_id} :: {_displayKey} :: {_weight}kg";
    }
}
