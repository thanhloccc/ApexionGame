using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EncosyTower.Collections
{
    public static class ArrayMapReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<TValue> GetValues<TKey, TValue>([NotNull] this ArrayMap<TKey, TValue>.ReadOnly self)
            => self._map._values.AsReadOnlySpan()[..self._map._freeValueCellIndex];
    }
}
