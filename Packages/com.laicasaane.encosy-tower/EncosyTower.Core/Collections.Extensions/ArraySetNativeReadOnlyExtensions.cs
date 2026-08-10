using System;
using System.Runtime.CompilerServices;

namespace EncosyTower.Collections.Extensions
{
    public static class ArraySetNativeReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> GetItems<T>(this in ArraySetNative<T>.ReadOnly self)
            where T : unmanaged, IEquatable<T>
                => self.AsValuesReadOnlySpan();
    }
}
