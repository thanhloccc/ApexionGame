// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using ListNativeAPI = EncosyTower.Collections.Extensions.ListNativeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListNativeExtensionsTests
    {
        [Test]
        public void ContainsAndRemove_FindAndRemoveValues()
        {
            using var list = new ListNative<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var three = 3;
            var seven = 7;

            Assert.IsTrue(ListNativeAPI.Contains(in list, 3));
            Assert.IsTrue(ListNativeAPI.Contains(in list, in three));
            Assert.IsFalse(ListNativeAPI.Contains(in list, 4));
            Assert.IsTrue(ListNativeAPI.Remove(in list, 3));
            Assert.IsTrue(ListNativeAPI.Remove(in list, in seven));
            Assert.IsFalse(ListNativeAPI.Remove(in list, 9));
            CollectionAssert.AreEqual(new[] { 1, 5 }, list.ToArray());
        }

        [Test]
        public void BinarySearchAndIndexOf_ReturnExpectedResults()
        {
            using var list = new ListNative<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, ListNativeAPI.BinarySearch(in list, 5, comparer));
            Assert.AreEqual(2, ListNativeAPI.BinarySearch(in list, in five, comparer));
            Assert.Less(ListNativeAPI.BinarySearch(in list, 4, comparer), 0);
            Assert.AreEqual(1, ListNativeAPI.BinarySearch(in list, 1, 2, 3, comparer));
            Assert.Less(ListNativeAPI.BinarySearch(in list, 1, 2, in seven, comparer), 0);
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, 5));
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, 5, 2));
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, 5, 1, 2));
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, in five));
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, in five, 2));
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, in five, 1, 2));
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, 5, comparer));
            Assert.AreEqual(2, ListNativeAPI.IndexOf(in list, in five, comparer));
        }

        [Test]
        public void Sort_FullAndRangeSortAscending()
        {
            var comparer = new IntComparer();
            using var full = new ListNative<int>(new[] { 7, 5, 3, 1 }, Allocator.Temp);
            using var range = new ListNative<int>(new[] { 9, 7, 5, 3, 1 }, Allocator.Temp);

            ListNativeAPI.Sort(in full, comparer);
            ListNativeAPI.Sort(in range, 1, 3, comparer);

            CollectionAssert.AreEqual(new[] { 1, 3, 5, 7 }, full.ToArray());
            CollectionAssert.AreEqual(new[] { 9, 3, 5, 7, 1 }, range.ToArray());
        }
    }
}
