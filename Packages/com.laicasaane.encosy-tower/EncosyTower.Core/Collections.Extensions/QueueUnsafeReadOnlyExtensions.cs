using System;
using System.Collections.Generic;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions
{
    public static class QueueUnsafeReadOnlyExtensions
    {
        public static bool Contains<T>(this in QueueUnsafe<T>.ReadOnly self, T item)
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

        public static bool Contains<T, TComparer>(
              this in QueueUnsafe<T>.ReadOnly self
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
    }
}
