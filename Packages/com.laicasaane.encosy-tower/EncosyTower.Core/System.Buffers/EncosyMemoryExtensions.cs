using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public static class EncosyMemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, TComparer>(this ReadOnlySpan<T> span, T value, TComparer comparer)
            where TComparer : IEqualityComparer<T>
        {
            var length = span.Length;

            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(span[i], value))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, TComparer>(this ReadOnlySpan<T> span, in T value, TComparer comparer)
            where TComparer : IEqualityComparer<T>
        {
            var length = span.Length;

            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(span[i], value))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
