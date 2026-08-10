// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Buffers;
using EncosyTower.Collections;
using NUnit.Framework;

using ListProxyAPI = EncosyTower.Collections.Extensions.ListProxyExtensions;
using ListProxyReadOnlyAPI = EncosyTower.Collections.Extensions.ListProxyReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListProxyExtensionsTests
    {
        [Test]
        public void ContainsAndRemove_OverloadsFindAndRemoveValues()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 3, 5, 7 });
            var comparer = new IntComparer();
            var three = 3;
            var seven = 7;

            Assert.IsTrue(ListProxyAPI.Contains(ref list, 3));
            Assert.IsTrue(ListProxyAPI.Contains(ref list, in three));
            Assert.IsTrue(ListProxyAPI.Contains(ref list, 3, comparer));
            Assert.IsTrue(ListProxyAPI.Contains(ref list, in three, comparer));
            Assert.IsFalse(ListProxyAPI.Contains(ref list, 4, comparer));
            Assert.IsTrue(ListProxyAPI.Remove(ref list, 3));
            Assert.IsTrue(ListProxyAPI.Remove(ref list, in seven, comparer));
            Assert.IsFalse(ListProxyAPI.Remove(ref list, 9));
            CollectionAssert.AreEqual(new[] { 1, 5 }, list.ToArray());

            var overloadList = NewList();
            overloadList.AddRange(new[] { 1, 3, 5, 7 });

            Assert.IsTrue(ListProxyAPI.Remove(ref overloadList, in three));
            Assert.IsTrue(ListProxyAPI.Remove(ref overloadList, 5, comparer));
            CollectionAssert.AreEqual(new[] { 1, 7 }, overloadList.ToArray());
        }

        [Test]
        public void BinarySearch_FullAndRangeReturnExpectedResults()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 3, 5, 7 });
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, ListProxyAPI.BinarySearch(ref list, 5, comparer));
            Assert.AreEqual(2, ListProxyAPI.BinarySearch(ref list, in five, comparer));
            Assert.Less(ListProxyAPI.BinarySearch(ref list, 4, comparer), 0);
            Assert.AreEqual(1, ListProxyAPI.BinarySearch(ref list, 1, 2, 3, comparer));
            Assert.Less(
                  ListProxyAPI.BinarySearch(ref list, 1, 2, in seven, comparer)
                , 0
            );
            Assert.Less(ListProxyAPI.BinarySearch(ref list, 1, 2, 7, comparer), 0);
        }

        [Test]
        public void IndexOf_OverloadsRespectStartAndCount()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 3, 5, 7 });
            var comparer = new IntComparer();
            var five = 5;

            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, 5));
            Assert.AreEqual(-1, ListProxyAPI.IndexOf(ref list, 4));
            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, 5, 2));
            Assert.AreEqual(-1, ListProxyAPI.IndexOf(ref list, 5, 3));
            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, 5, 1, 2));
            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, in five));
            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, in five, 2));
            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, in five, 1, 2));
            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, 5, comparer));
            Assert.AreEqual(2, ListProxyAPI.IndexOf(ref list, in five, comparer));
        }

        [Test]
        public void Sort_FullAndRangeSortAscending()
        {
            var comparer = new IntComparer();
            var full = NewList();
            var range = NewList();
            full.AddRange(new[] { 7, 5, 3, 1 });
            range.AddRange(new[] { 9, 7, 5, 3, 1 });

            ListProxyAPI.Sort(ref full, comparer);
            ListProxyAPI.Sort(ref range, 1, 3, comparer);

            CollectionAssert.AreEqual(new[] { 1, 3, 5, 7 }, full.ToArray());
            CollectionAssert.AreEqual(new[] { 9, 3, 5, 7, 1 }, range.ToArray());
        }

        private static ListProxy<BufferProvider<int>, BufferManaged<int>, int> NewList()
            => new(new BufferProvider<int>());
    }

    public partial class ListProxyReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_OverloadsFindExpectedValues()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 3, 5, 7 });
            var view = list.AsReadOnly();
            var comparer = new IntComparer();
            var three = 3;

            Assert.IsTrue(ListProxyReadOnlyAPI.Contains(ref view, 3));
            Assert.IsTrue(ListProxyReadOnlyAPI.Contains(ref view, in three));
            Assert.IsTrue(ListProxyReadOnlyAPI.Contains(ref view, 3, comparer));
            Assert.IsTrue(ListProxyReadOnlyAPI.Contains(ref view, in three, comparer));
            Assert.IsFalse(ListProxyReadOnlyAPI.Contains(ref view, 4));
            Assert.IsFalse(ListProxyReadOnlyAPI.Contains(ref view, 4, comparer));
        }

        [Test]
        public void BinarySearch_FullAndRangeReturnExpectedResults()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 3, 5, 7 });
            var view = list.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;
            var seven = 7;

            Assert.AreEqual(2, ListProxyReadOnlyAPI.BinarySearch(ref view, 5, comparer));
            Assert.AreEqual(
                  2
                , ListProxyReadOnlyAPI.BinarySearch(ref view, in five, comparer)
            );
            Assert.Less(ListProxyReadOnlyAPI.BinarySearch(ref view, 4, comparer), 0);
            Assert.AreEqual(
                  1
                , ListProxyReadOnlyAPI.BinarySearch(ref view, 1, 2, 3, comparer)
            );
            Assert.Less(
                  ListProxyReadOnlyAPI.BinarySearch(ref view, 1, 2, in seven, comparer)
                , 0
            );
            Assert.Less(
                  ListProxyReadOnlyAPI.BinarySearch(ref view, 1, 2, 7, comparer)
                , 0
            );
        }

        [Test]
        public void IndexOf_OverloadsRespectStartAndCount()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 3, 5, 7 });
            var view = list.AsReadOnly();
            var comparer = new IntComparer();
            var five = 5;

            Assert.AreEqual(2, ListProxyReadOnlyAPI.IndexOf(ref view, 5));
            Assert.AreEqual(-1, ListProxyReadOnlyAPI.IndexOf(ref view, 4));
            Assert.AreEqual(2, ListProxyReadOnlyAPI.IndexOf(ref view, 5, 2));
            Assert.AreEqual(-1, ListProxyReadOnlyAPI.IndexOf(ref view, 5, 3));
            Assert.AreEqual(2, ListProxyReadOnlyAPI.IndexOf(ref view, 5, 1, 2));
            Assert.AreEqual(2, ListProxyReadOnlyAPI.IndexOf(ref view, in five));
            Assert.AreEqual(2, ListProxyReadOnlyAPI.IndexOf(ref view, in five, 2));
            Assert.AreEqual(2, ListProxyReadOnlyAPI.IndexOf(ref view, in five, 1, 2));
            Assert.AreEqual(2, ListProxyReadOnlyAPI.IndexOf(ref view, 5, comparer));
            Assert.AreEqual(
                  2
                , ListProxyReadOnlyAPI.IndexOf(ref view, in five, comparer)
            );
        }

        private static ListProxy<BufferProvider<int>, BufferManaged<int>, int> NewList()
            => new(new BufferProvider<int>());
    }
}
