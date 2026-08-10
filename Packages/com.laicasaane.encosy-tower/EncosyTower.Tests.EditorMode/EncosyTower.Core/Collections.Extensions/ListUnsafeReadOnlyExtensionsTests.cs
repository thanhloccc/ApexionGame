using System;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using ListUnsafeReadOnlyAPI = EncosyTower.Collections.Extensions.ListUnsafeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListUnsafeReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_BothValueOverloadsReturnExpectedResults()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();
            var three = 3;

            Assert.IsTrue(ListUnsafeReadOnlyAPI.Contains(in readOnly, 3));
            Assert.IsTrue(ListUnsafeReadOnlyAPI.Contains(in readOnly, in three));
            Assert.IsFalse(ListUnsafeReadOnlyAPI.Contains(in readOnly, 4));
        }

        [Test]
        public void IndexOf_AllValueInComparerAndRangeOverloadsRespectBounds()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 3, 7 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();
            var comparer = new IntComparer();
            var three = 3;
            var seven = 7;

            Assert.AreEqual(1, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, 3));
            Assert.AreEqual(3, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, 3, 2));
            Assert.AreEqual(-1, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, 3, 2, 1));
            Assert.AreEqual(1, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, in three));
            Assert.AreEqual(3, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, in three, 2));
            Assert.AreEqual(-1, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, in three, 2, 1));
            Assert.AreEqual(4, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, 7, comparer));
            Assert.AreEqual(4, ListUnsafeReadOnlyAPI.IndexOf(in readOnly, in seven, comparer));
        }

        [Test]
        public void BinarySearch_AllValueInAndRangeOverloadsReturnExpectedResults()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, ListUnsafeReadOnlyAPI.BinarySearch(in readOnly, 5, comparer));
            Assert.AreEqual(2, ListUnsafeReadOnlyAPI.BinarySearch(in readOnly, in five, comparer));
            Assert.AreEqual(2, ListUnsafeReadOnlyAPI.BinarySearch(in readOnly, 1, 2, 5, comparer));
            Assert.Less(
                  ListUnsafeReadOnlyAPI.BinarySearch(in readOnly, 0, 2, in seven, comparer)
                , 0
            );
            Assert.Less(ListUnsafeReadOnlyAPI.BinarySearch(in readOnly, 4, comparer), 0);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void RangeOverloads_RejectInvalidIndexCountAndSection()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();
            var comparer = new IntComparer();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => ListUnsafeReadOnlyAPI.IndexOf(in readOnly, 3, -1, 1)
            );
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ListUnsafeReadOnlyAPI.IndexOf(in readOnly, 3, 0, -1)
            );
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ListUnsafeReadOnlyAPI.BinarySearch(in readOnly, 3, 2, 5, comparer)
            );
        }
    }
}
