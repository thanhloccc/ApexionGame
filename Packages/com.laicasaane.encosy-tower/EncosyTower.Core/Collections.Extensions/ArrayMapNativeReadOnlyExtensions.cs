using System;
using System.Runtime.CompilerServices;

namespace EncosyTower.Collections.Extensions
{
    public static class ArrayMapNativeReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<TValue> GetValues<TKey, TValue>(this in ArrayMapNative<TKey, TValue>.ReadOnly self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
                => self.AsValuesReadOnlySpan();
    }
}
