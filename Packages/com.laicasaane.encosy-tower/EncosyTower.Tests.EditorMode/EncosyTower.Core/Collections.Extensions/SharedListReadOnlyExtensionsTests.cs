// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;

using SharedListReadOnlyAPI = EncosyTower.Collections.Extensions.SharedListReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedListReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_OverloadsFindExpectedValues()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var view = list.AsReadOnly();
            var comparer = new IntComparer();
            var three = 3;

            Assert.IsTrue(SharedListReadOnlyAPI.Contains(view, 3));
            Assert.IsTrue(SharedListReadOnlyAPI.Contains(view, in three));
            Assert.IsTrue(SharedListReadOnlyAPI.Contains(view, 3, comparer));
            Assert.IsTrue(SharedListReadOnlyAPI.Contains(view, in three, comparer));
            Assert.IsFalse(SharedListReadOnlyAPI.Contains(view, 4));
            Assert.IsFalse(SharedListReadOnlyAPI.Contains(view, 4, comparer));
        }

        [Test]
        public void BinarySearch_FullAndRangeReturnExpectedResults()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var view = list.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, SharedListReadOnlyAPI.BinarySearch(view, 5, comparer));
            Assert.AreEqual(2, SharedListReadOnlyAPI.BinarySearch(view, in five, comparer));
            Assert.Less(SharedListReadOnlyAPI.BinarySearch(view, 4, comparer), 0);
            Assert.AreEqual(1, SharedListReadOnlyAPI.BinarySearch(view, 1, 2, 3, comparer));
            Assert.Less(
                  SharedListReadOnlyAPI.BinarySearch(view, 1, 2, in seven, comparer)
                , 0
            );
            Assert.Less(SharedListReadOnlyAPI.BinarySearch(view, 1, 2, 7, comparer), 0);
        }

        [Test]
        public void IndexOf_OverloadsRespectStartAndCount()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var view = list.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;

            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, 5));
            Assert.AreEqual(-1, SharedListReadOnlyAPI.IndexOf(view, 4));
            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, 5, 2));
            Assert.AreEqual(-1, SharedListReadOnlyAPI.IndexOf(view, 5, 3));
            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, 5, 1, 2));
            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, in five));
            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, in five, 2));
            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, in five, 1, 2));
            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, 5, comparer));
            Assert.AreEqual(2, SharedListReadOnlyAPI.IndexOf(view, in five, comparer));
        }
    }
}
