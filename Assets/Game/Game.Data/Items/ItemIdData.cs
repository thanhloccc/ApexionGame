using System.Runtime.CompilerServices;
using EncosyTower.Data;
using Game.Common;

namespace Game.Data
{
    [Data, DataWithoutId]
    public partial struct ItemIdData
    {
        [DataProperty] public readonly string Kind => Get_Kind();

        [DataProperty] public readonly int SubId => Get_SubId();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString()
            => $"{_kind}:{_subId}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ItemId(ItemIdData data)
        {
            var parsed = ItemId_IdKindExtensions.TryParse(data._kind, out var kind, true, true);
            return parsed ? new ItemId(kind, data._subId) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ItemIdData(ItemId value)
            => new() { _kind = value.Kind.ToString(), _subId = value.IdSigned };
    }
}
