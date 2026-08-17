using System.Runtime.CompilerServices;
using EncosyTower.Common;
using EncosyTower.TypeWraps;

namespace Game.Common
{
    [WrapRecord]
    public readonly partial record struct EquipmentId(int Value) : IIsValid
    {
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value > 0;
        }
    }
}
