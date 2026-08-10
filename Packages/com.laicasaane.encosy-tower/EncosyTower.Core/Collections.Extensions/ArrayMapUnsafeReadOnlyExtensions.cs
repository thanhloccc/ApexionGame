using System;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions
{
    public static class ArrayMapUnsafeReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<TValue> GetValues<TKey, TValue>(this in ArrayMapUnsafe<TKey, TValue>.ReadOnly self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
                => self.Values;
    }
}
