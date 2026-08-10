// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;

using SharedListNativeAPI = EncosyTower.Collections.Extensions.SharedListNativeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedListNativeExtensionsTests
    {
        [Test]
        public void ContainsAndRemove_FindAndRemoveValues()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var view = list.AsNative();
            var three = 3;
            var seven = 7;

            Assert.IsTrue(SharedListNativeAPI.Contains(in view, 3));
            Assert.IsTrue(SharedListNativeAPI.Contains(in view, in three));
            Assert.IsFalse(SharedListNativeAPI.Contains(in view, 4));
            Assert.IsTrue(SharedListNativeAPI.Remove(in view, 3));
            Assert.IsTrue(SharedListNativeAPI.Remove(in view, in seven));
            Assert.IsFalse(SharedListNativeAPI.Remove(in view, 9));
            CollectionAssert.AreEqual(new[] { 1, 5 }, list.ToArray());
        }

        [Test]
        public void BinarySearch_FullAndRangeReturnExpectedResults()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var view = list.AsNative();
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, SharedListNativeAPI.BinarySearch(in view, 5, comparer));
            Assert.AreEqual(2, SharedListNativeAPI.BinarySearch(in view, in five, comparer));
            Assert.Less(SharedListNativeAPI.BinarySearch(in view, 4, comparer), 0);
            Assert.AreEqual(1, SharedListNativeAPI.BinarySearch(in view, 1, 2, 3, comparer));
            Assert.Less(
                  SharedListNativeAPI.BinarySearch(in view, 1, 2, in seven, comparer)
                , 0
            );
            Assert.Less(SharedListNativeAPI.BinarySearch(in view, 1, 2, 7, comparer), 0);
        }

        [Test]
        public void IndexOf_OverloadsRespectStartAndCount()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var view = list.AsNative();
            var comparer = new IntComparer();
            var five = 5;

            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, 5));
            Assert.AreEqual(-1, SharedListNativeAPI.IndexOf(in view, 4));
            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, 5, 2));
            Assert.AreEqual(-1, SharedListNativeAPI.IndexOf(in view, 5, 3));
            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, 5, 1, 2));
            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, in five));
            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, in five, 2));
            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, in five, 1, 2));
            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, 5, comparer));
            Assert.AreEqual(2, SharedListNativeAPI.IndexOf(in view, in five, comparer));
        }

        [Test]
        public void Sort_FullAndRangeSortAscending()
        {
            var comparer = new IntComparer();
            using var full = new SharedList<int>(7, 5, 3, 1);
            using var range = new SharedList<int>(9, 7, 5, 3, 1);
            var fullView = full.AsNative();
            var rangeView = range.AsNative();

            SharedListNativeAPI.Sort(in fullView, comparer);
            SharedListNativeAPI.Sort(in rangeView, 1, 3, comparer);

            CollectionAssert.AreEqual(new[] { 1, 3, 5, 7 }, full.ToArray());
            CollectionAssert.AreEqual(new[] { 9, 3, 5, 7, 1 }, range.ToArray());
        }
    }
}
