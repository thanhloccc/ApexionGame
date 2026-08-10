using EncosyTower.EnumExtensions;
using EncosyTower.UnionIds;

namespace EncosyTower.Samples.Persistence.Shared;

[EnumExtensions]
[EnumMembersForTemplate(typeof(ItemType_EnumTemplate), 100)]
[KindForUnionId(typeof(ItemId), 100, nameof(Currency))]
public enum CurrencyType : byte
{
    Currency,
    Bronze,
    Silver,
    Gold,
    Crystal,
}
