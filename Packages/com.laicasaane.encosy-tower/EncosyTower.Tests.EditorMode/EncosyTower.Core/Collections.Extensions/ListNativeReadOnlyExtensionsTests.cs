// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using ListNativeReadOnlyAPI = EncosyTower.Collections.Extensions.ListNativeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListNativeReadOnlyExtensionsTests
    {
        [Test]
        public void ContainsIndexOfAndBinarySearch_ReturnExpectedResults()
        {
            using var list = new ListNative<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var view = list.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.IsTrue(ListNativeReadOnlyAPI.Contains(in view, 5));
            Assert.IsTrue(ListNativeReadOnlyAPI.Contains(in view, in five));
            Assert.IsFalse(ListNativeReadOnlyAPI.Contains(in view, 4));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, 5));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, 5, 2));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, 5, 1, 2));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, in five));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, in five, 2));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, in five, 1, 2));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, 5, comparer));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.IndexOf(in view, in five, comparer));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.BinarySearch(in view, 5, comparer));
            Assert.AreEqual(2, ListNativeReadOnlyAPI.BinarySearch(in view, in five, comparer));
            Assert.AreEqual(1, ListNativeReadOnlyAPI.BinarySearch(in view, 1, 2, 3, comparer));
            Assert.Less(
                  ListNativeReadOnlyAPI.BinarySearch(in view, 1, 2, in seven, comparer)
                , 0
            );
            Assert.Less(ListNativeReadOnlyAPI.BinarySearch(in view, 4, comparer), 0);
        }
    }
}
