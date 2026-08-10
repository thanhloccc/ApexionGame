using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public class ArraySortHelperTests
    {
        [Test]
        public void Sort_OrdersEmptySingleSmallAndLargeWindows()
        {
            var comparer = Comparer<int>.Default;
            var empty = new int[0];
            var single = new[] { 1 };
            var small = new[] { 4, 1, 3, 2 };
            var large = new int[40];

            for (var i = 0; i < large.Length; i++)
            {
                large[i] = large.Length - i;
            }

            ArraySortHelper<int, Comparer<int>>.Sort(empty, comparer);
            ArraySortHelper<int, Comparer<int>>.Sort(single, comparer);
            ArraySortHelper<int, Comparer<int>>.Sort(small, comparer);
            ArraySortHelper<int, Comparer<int>>.Sort(large, comparer);

            CollectionAssert.AreEqual(new int[0], empty);
            CollectionAssert.AreEqual(new[] { 1 }, single);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, small);

            for (var i = 0; i < large.Length; i++)
            {
                Assert.AreEqual(i + 1, large[i]);
            }
        }
    }
}
