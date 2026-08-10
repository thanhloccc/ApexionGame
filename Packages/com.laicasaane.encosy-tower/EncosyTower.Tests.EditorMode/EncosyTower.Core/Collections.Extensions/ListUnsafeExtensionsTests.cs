using System;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using ListUnsafeAPI = EncosyTower.Collections.Extensions.ListUnsafeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListUnsafeExtensionsTests
    {
        [Test]
        public void Contains_AllValueAndComparerOverloadsReturnExpectedResults()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var comparer = new IntComparer();
            var three = 3;
            var four = 4;

            Assert.IsTrue(ListUnsafeAPI.Contains(in list, 3));
            Assert.IsTrue(ListUnsafeAPI.Contains(in list, in three));
            Assert.IsFalse(ListUnsafeAPI.Contains(in list, 4));
            Assert.IsTrue(ListUnsafeAPI.Contains(in list, 3, comparer));
            Assert.IsTrue(ListUnsafeAPI.Contains(in list, in three, comparer));
            Assert.IsFalse(ListUnsafeAPI.Contains(in list, in four, comparer));
        }

        [Test]
        public void Remove_BothOverloadsRemoveFirstMatchAndReportMissing()
        {
            var list = new ListUnsafe<int>(new[] { 1, 3, 5, 3, 7 }, Allocator.Temp);
            var secondThree = 3;

            try
            {
                Assert.IsTrue(ListUnsafeAPI.Remove(ref list, 3));
                Assert.IsTrue(ListUnsafeAPI.Remove(ref list, in secondThree));
                Assert.IsFalse(ListUnsafeAPI.Remove(ref list, 9));
                CollectionAssert.AreEqual(new[] { 1, 5, 7 }, list.ToArray());
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        public void IndexOf_AllValueInComparerAndRangeOverloadsRespectBounds()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 3, 7 }, Allocator.Temp);
            var comparer = new IntComparer();
            var three = 3;
            var seven = 7;

            Assert.AreEqual(1, ListUnsafeAPI.IndexOf(in list, 3));
            Assert.AreEqual(3, ListUnsafeAPI.IndexOf(in list, 3, 2));
            Assert.AreEqual(-1, ListUnsafeAPI.IndexOf(in list, 3, 2, 1));
            Assert.AreEqual(1, ListUnsafeAPI.IndexOf(in list, in three));
            Assert.AreEqual(3, ListUnsafeAPI.IndexOf(in list, in three, 2));
            Assert.AreEqual(-1, ListUnsafeAPI.IndexOf(in list, in three, 2, 1));
            Assert.AreEqual(4, ListUnsafeAPI.IndexOf(in list, 7, comparer));
            Assert.AreEqual(4, ListUnsafeAPI.IndexOf(in list, in seven, comparer));
        }

        [Test]
        public void BinarySearch_AllValueInAndRangeOverloadsReturnExpectedResults()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, ListUnsafeAPI.BinarySearch(in list, 5, comparer));
            Assert.AreEqual(2, ListUnsafeAPI.BinarySearch(in list, in five, comparer));
            Assert.AreEqual(2, ListUnsafeAPI.BinarySearch(in list, 1, 2, 5, comparer));
            Assert.Less(ListUnsafeAPI.BinarySearch(in list, 0, 2, in seven, comparer), 0);
            Assert.Less(ListUnsafeAPI.BinarySearch(in list, 4, comparer), 0);
        }

        [Test]
        public void Sort_FullAndRangeOverloadsSortRequestedSections()
        {
            var comparer = new IntComparer();
            var full = new ListUnsafe<int>(new[] { 7, 5, 3, 1 }, Allocator.Temp);
            var range = new ListUnsafe<int>(new[] { 9, 7, 5, 3, 1 }, Allocator.Temp);

            try
            {
                ListUnsafeAPI.Sort(ref full, comparer);
                ListUnsafeAPI.Sort(ref range, 1, 3, comparer);

                CollectionAssert.AreEqual(new[] { 1, 3, 5, 7 }, full.ToArray());
                CollectionAssert.AreEqual(new[] { 9, 3, 5, 7, 1 }, range.ToArray());
            }
            finally
            {
                full.Dispose();
                range.Dispose();
            }
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void RangeOverloads_RejectInvalidIndexCountAndSection()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var comparer = new IntComparer();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => ListUnsafeAPI.IndexOf(in list, 3, -1, 1)
            );
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ListUnsafeAPI.IndexOf(in list, 3, 0, -1)
            );
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ListUnsafeAPI.BinarySearch(in list, 3, 2, 5, comparer)
            );
        }
    }
}
