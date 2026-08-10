using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions
{
    public static class QueueUnsafeExtensions
    {
        public static bool Contains<T>(this in QueueUnsafe<T> self, T item)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var x in self)
            {
                if (x.Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this in QueueUnsafe<T> self, in T item)
            where T : unmanaged, IEquatable<T>
                => Contains(self, item);

        public static bool Contains<T, TComparer>(
              this in QueueUnsafe<T> self
            , T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
        {
            foreach (var x in self)
            {
                if (comparer.Equals(x, item))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TComparer>(
              this in QueueUnsafe<T> self
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
                => Contains(self, item, comparer);
    }
}
