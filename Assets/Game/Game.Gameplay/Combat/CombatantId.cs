using System.Runtime.CompilerServices;
using EncosyTower.Common;
using EncosyTower.TypeWraps;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Runtime handle for one participant in combat.
    /// </summary>
    /// <remarks>
    /// Deliberately not placed in <c>Game.Common</c>: unlike <c>ItemId</c> this never appears in
    /// authored data, only in a live <see cref="CombatWorld"/>.
    /// </remarks>
    [WrapRecord]
    public readonly partial record struct CombatantId(int Value) : IIsValid
    {
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value > 0;
        }
    }
}
