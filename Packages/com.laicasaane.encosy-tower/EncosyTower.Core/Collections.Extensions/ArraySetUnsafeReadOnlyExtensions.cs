using System;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions
{
    public static class ArraySetUnsafeReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> GetItems<T>(this in ArraySetUnsafe<T>.ReadOnly self)
            where T : unmanaged, IEquatable<T>
                => self._values.AsReadOnlySpan()[..self._freeValueCellIndex];
    }
}
