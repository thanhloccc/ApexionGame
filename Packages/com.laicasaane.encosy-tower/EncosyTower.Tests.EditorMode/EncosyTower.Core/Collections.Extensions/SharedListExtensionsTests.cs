// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

using SharedListAPI = EncosyTower.Collections.Extensions.SharedListExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedListExtensionsTests
    {
        [Test]
        public void ContainsAndRemove_ComparerFindsAndRemovesValues()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var comparer = new IntComparer();
            var three = 3;
            var seven = 7;

            Assert.IsTrue(SharedListAPI.Contains(list, 3, comparer));
            Assert.IsTrue(SharedListAPI.Contains(list, in three, comparer));
            Assert.IsFalse(SharedListAPI.Contains(list, 4, comparer));
            Assert.IsTrue(SharedListAPI.Remove(list, 3, comparer));
            Assert.IsTrue(SharedListAPI.Remove(list, in seven, comparer));
            Assert.IsFalse(SharedListAPI.Remove(list, 9, comparer));
            CollectionAssert.AreEqual(new[] { 1, 5 }, list.ToArray());
        }

        [Test]
        public void BinarySearch_FullAndRangeReturnExpectedResults()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, SharedListAPI.BinarySearch(list, 5, comparer));
            Assert.AreEqual(2, SharedListAPI.BinarySearch(list, in five, comparer));
            Assert.Less(SharedListAPI.BinarySearch(list, 4, comparer), 0);
            Assert.AreEqual(1, SharedListAPI.BinarySearch(list, 1, 2, 3, comparer));
            Assert.Less(SharedListAPI.BinarySearch(list, 1, 2, in seven, comparer), 0);
            Assert.Less(SharedListAPI.BinarySearch(list, 1, 2, 7, comparer), 0);
        }

        [Test]
        public void IndexOf_OverloadsRespectStartAndCount()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var comparer = new IntComparer();
            var seven = 7;

            Assert.AreEqual(2, SharedListAPI.IndexOf(list, 5));
            Assert.AreEqual(-1, SharedListAPI.IndexOf(list, 4));
            Assert.AreEqual(2, SharedListAPI.IndexOf(list, 5, 2));
            Assert.AreEqual(-1, SharedListAPI.IndexOf(list, 5, 3));
            Assert.AreEqual(2, SharedListAPI.IndexOf(list, 5, 1, 2));
            Assert.AreEqual(3, SharedListAPI.IndexOf(list, 7, comparer));
            Assert.AreEqual(3, SharedListAPI.IndexOf(list, in seven, comparer));
        }

        [Test]
        public void Sort_FullAndRangeSortAscending()
        {
            var comparer = new IntComparer();
            using var full = new SharedList<int>(7, 5, 3, 1);
            using var range = new SharedList<int>(9, 7, 5, 3, 1);

            SharedListAPI.Sort(full, comparer);
            SharedListAPI.Sort(range, 1, 3, comparer);

            CollectionAssert.AreEqual(new[] { 1, 3, 5, 7 }, full.ToArray());
            CollectionAssert.AreEqual(new[] { 9, 3, 5, 7, 1 }, range.ToArray());
        }
    }

    internal readonly struct IntComparer : IComparer<int>, IEqualityComparer<int>
    {
        public int Compare(int x, int y)
            => x.CompareTo(y);

        public bool Equals(int x, int y)
            => x == y;

        public int GetHashCode(int value)
            => value;
    }
}
