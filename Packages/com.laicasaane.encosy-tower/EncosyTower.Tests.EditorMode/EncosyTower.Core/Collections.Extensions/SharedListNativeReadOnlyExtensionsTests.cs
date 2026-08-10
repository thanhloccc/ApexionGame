// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;

using NativeReadOnlyAPI = EncosyTower.Collections.Extensions.SharedListNativeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedListNativeReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_OverloadsFindExpectedValues()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var native = list.AsNative();
            var view = native.AsReadOnly();
            var three = 3;

            Assert.IsTrue(NativeReadOnlyAPI.Contains(in view, 3));
            Assert.IsTrue(NativeReadOnlyAPI.Contains(in view, in three));
            Assert.IsFalse(NativeReadOnlyAPI.Contains(in view, 4));
        }

        [Test]
        public void BinarySearch_FullAndRangeReturnExpectedResults()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var native = list.AsNative();
            var view = native.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, NativeReadOnlyAPI.BinarySearch(in view, 5, comparer));
            Assert.AreEqual(
                  2
                , NativeReadOnlyAPI.BinarySearch(in view, in five, comparer)
            );
            Assert.Less(NativeReadOnlyAPI.BinarySearch(in view, 4, comparer), 0);
            Assert.AreEqual(
                  1
                , NativeReadOnlyAPI.BinarySearch(in view, 1, 2, 3, comparer)
            );
            Assert.Less(
                  NativeReadOnlyAPI.BinarySearch(in view, 1, 2, in seven, comparer)
                , 0
            );
            Assert.Less(
                  NativeReadOnlyAPI.BinarySearch(in view, 1, 2, 7, comparer)
                , 0
            );
        }

        [Test]
        public void IndexOf_OverloadsRespectStartAndCount()
        {
            using var list = new SharedList<int>(1, 3, 5, 7);
            var native = list.AsNative();
            var view = native.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;

            Assert.AreEqual(2, NativeReadOnlyAPI.IndexOf(in view, 5));
            Assert.AreEqual(-1, NativeReadOnlyAPI.IndexOf(in view, 4));
            Assert.AreEqual(2, NativeReadOnlyAPI.IndexOf(in view, 5, 2));
            Assert.AreEqual(-1, NativeReadOnlyAPI.IndexOf(in view, 5, 3));
            Assert.AreEqual(2, NativeReadOnlyAPI.IndexOf(in view, 5, 1, 2));
            Assert.AreEqual(2, NativeReadOnlyAPI.IndexOf(in view, in five));
            Assert.AreEqual(2, NativeReadOnlyAPI.IndexOf(in view, in five, 2));
            Assert.AreEqual(2, NativeReadOnlyAPI.IndexOf(in view, in five, 1, 2));
            Assert.AreEqual(2, NativeReadOnlyAPI.IndexOf(in view, 5, comparer));
            Assert.AreEqual(
                  2
                , NativeReadOnlyAPI.IndexOf(in view, in five, comparer)
            );
        }
    }
}
