using System;
using EncosyTower.UnionIds;
using Unity.Collections;

namespace Game.Common
{
    [UnionId(Size = UnionIdSize.ULong, KindSettings = UnionIdKindSettings.PreserveOrder)]
    [UnionIdKind(typeof(EquipmentId), 0, "Equipment", signed: true)]
    [UnionIdKind(typeof(MeleeWeaponId), 1, "MeleeWeapon", signed: true)]
    [UnionIdKind(typeof(RangedWeaponId), 2, "RangedWeapon", signed: true)]
    [UnionIdKind(typeof(AmmoId), 3, "Ammo", signed: true)]
    public readonly partial struct ItemId
    {
        private static partial bool TryParse_Equipment(
              ReadOnlySpan<char> str
            , out EquipmentId value
            , bool ignoreCase
            , bool allowMatchingMetadataAttribute
        )
        {
            var result = int.TryParse(str, out var v);
            value = v;
            return result;
        }

        private static partial bool TryParse_MeleeWeapon(
              ReadOnlySpan<char> str
            , out MeleeWeaponId value
            , bool ignoreCase
            , bool allowMatchingMetadataAttribute
        )
        {
            var result = int.TryParse(str, out var v);
            value = v;
            return result;
        }

        private static partial bool TryParse_RangedWeapon(
              ReadOnlySpan<char> str
            , out RangedWeaponId value
            , bool ignoreCase
            , bool allowMatchingMetadataAttribute
        )
        {
            var result = int.TryParse(str, out var v);
            value = v;
            return result;
        }

        private static partial bool TryParse_Ammo(
              ReadOnlySpan<char> str
            , out AmmoId value
            , bool ignoreCase
            , bool allowMatchingMetadataAttribute
        )
        {
            var result = int.TryParse(str, out var v);
            value = v;
            return result;
        }

        private static partial void Append_Equipment(
              ref FixedString32Bytes result
            , EquipmentId value
            , bool displayString
        )
        {
            result.Append(value.Value);
        }

        private static partial void Append_MeleeWeapon(
              ref FixedString32Bytes result
            , MeleeWeaponId value
            , bool displayString
        )
        {
            result.Append(value.Value);
        }

        private static partial void Append_RangedWeapon(
              ref FixedString32Bytes result
            , RangedWeaponId value
            , bool displayString
        )
        {
            result.Append(value.Value);
        }

        private static partial void Append_Ammo(
              ref FixedString32Bytes result
            , AmmoId value
            , bool displayString
        )
        {
            result.Append(value.Value);
        }
    }
}
