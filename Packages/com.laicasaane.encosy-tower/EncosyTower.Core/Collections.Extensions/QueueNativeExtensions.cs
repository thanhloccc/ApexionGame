using System;
using System.Collections.Generic;

namespace EncosyTower.Collections.Extensions
{
    public static class QueueNativeExtensions
    {
        public static bool Contains<T>(this in QueueNative<T> self, T item)
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
              this in QueueNative<T> self
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
