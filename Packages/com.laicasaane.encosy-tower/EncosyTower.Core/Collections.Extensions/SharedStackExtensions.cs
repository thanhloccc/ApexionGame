using System;
using System.Collections.Generic;

namespace EncosyTower.Collections.Extensions
{
    public static class SharedStackExtensions
    {
        public static bool Contains<T, TNative>(this SharedStack<T, TNative> self, T item)
            where T : unmanaged, IEquatable<T>
            where TNative : unmanaged
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

        public static bool Contains<T, TNative, TComparer>(
              this SharedStack<T, TNative> self
            , T item
            , TComparer comparer
        )
            where T : unmanaged
            where TNative : unmanaged
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
