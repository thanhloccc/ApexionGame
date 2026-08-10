using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EncosyTower.Collections
{
    public static class ArraySetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> GetItems<T>([NotNull] this ArraySet<T> self)
            => self._values.AsSpan()[..self._freeValueCellIndex];
    }
}
