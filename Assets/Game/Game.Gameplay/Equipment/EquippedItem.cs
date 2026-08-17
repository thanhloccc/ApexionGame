using System.Runtime.CompilerServices;
using EncosyTower.Common;
using Game.Common;

namespace Game.Gameplay.Equipment
{
    public readonly record struct EquippedItem(ItemId Id, Option<int> Rounds)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EquippedItem Of(ItemId id)
            => new(id, Option.None);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EquippedItem Of(ItemId id, int rounds)
            => new(id, rounds);
    }

    public readonly record struct EquipChange(
          EquippedItem Equipped
        , EquipmentSlot Slot
        , Option<EquippedItem> Replaced
    );
}
