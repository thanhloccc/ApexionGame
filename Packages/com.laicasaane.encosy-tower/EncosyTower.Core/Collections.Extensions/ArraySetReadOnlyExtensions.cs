using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EncosyTower.Collections
{
    public static class ArraySetReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> GetItems<T>([NotNull] this ArraySet<T>.ReadOnly self)
            => self._set._values.AsReadOnlySpan()[..self._set._freeValueCellIndex];
    }
}
