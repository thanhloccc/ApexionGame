using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections.Extensions;
using NUnit.Framework;

using ArrayAPI = EncosyTower.Collections.Extensions.EncosyArrayExtensions;
using CollectionAPI = EncosyTower.Collections.Extensions.EncosyICollectionExtensions;
using DictionaryAPI = EncosyTower.Collections.Extensions.EncosyDictionaryExtensions;
using DisposableAPI = EncosyTower.Collections.Extensions.DisposableCollectionExtensions;
using EnumerableAPI = EncosyTower.Collections.Extensions.EncosyIEnumerableExtensions;
using ListAPI = EncosyTower.Collections.Extensions.EncosyListExtensions;
using UnmanagedAPI = EncosyTower.Collections.Extensions.EncosyICollectionExtensionsUnmanaged;

namespace EncosyTower.Tests.Core.Collections.Extensions
{
    public partial class EncosyArrayExtensionsTests
    {
        [Test]
        public void ConversionAndReadOnlySpan_ExposeSourceValues()
        {
            var source = new[] { 1, 2, 3 };
            var list = ArrayAPI.ToList(source);
            var fasterList = ArrayAPI.ToFasterList(source);
            var span = ArrayAPI.AsReadOnlySpan(source);

            CollectionAssert.AreEqual(source, list);
            CollectionAssert.AreEqual(source, fasterList.ToArray());
            CollectionAssert.AreEqual(source, span.ToArray());
        }
    }

    public partial class EncosyListExtensionsTests
    {
        [Test]
        public void IsNullOrEmpty_HandlesNullEmptyAndPopulatedLists()
        {
            IList<int> nullList = null;
            IList<int> emptyList = new List<int>();
            IList<int> populatedList = new List<int> { 1 };
            IReadOnlyList<int> nullReadOnlyList = null;
            IReadOnlyList<int> emptyReadOnlyList = new List<int>();
            IReadOnlyList<int> populatedReadOnlyList = new List<int> { 1 };

            Assert.IsTrue(ListAPI.IsNullOrEmpty(nullList));
            Assert.IsTrue(ListAPI.IsNullOrEmpty(emptyList));
            Assert.IsFalse(ListAPI.IsNullOrEmpty(populatedList));
            Assert.IsTrue(ListAPI.IsNullOrEmpty(nullReadOnlyList));
            Assert.IsTrue(ListAPI.IsNullOrEmpty(emptyReadOnlyList));
            Assert.IsFalse(ListAPI.IsNullOrEmpty(populatedReadOnlyList));
        }

        [Test]
        public void Views_AliasListStorage()
        {
            var list = new List<int> { 1, 2, 3 };
            var span = ListAPI.AsSpan(list);
            var readOnlySpan = ListAPI.AsReadOnlySpan(list);
            var fast = ListAPI.AsListFast(list);
            var readOnly = ListAPI.AsReadOnly(list);

            span[0] = 10;
            fast[1] = 20;

            Assert.AreEqual(10, list[0]);
            Assert.AreEqual(20, list[1]);
            Assert.AreEqual(10, readOnlySpan[0]);
            Assert.AreEqual(20, readOnly[1]);
            Assert.AreEqual(0, ListAPI.AsReadOnly<int>(null).Count);
        }
    }

    public partial class EncosyDictionaryExtensionsTests
    {
        [Test]
        public void IsNullOrEmpty_HandlesNullEmptyAndPopulatedDictionaries()
        {
            IDictionary<int, int> nullDictionary = null;
            IDictionary<int, int> emptyDictionary = new Dictionary<int, int>();
            IDictionary<int, int> populatedDictionary = new Dictionary<int, int> { [1] = 10 };

            Assert.IsTrue(DictionaryAPI.IsNullOrEmpty(nullDictionary));
            Assert.IsTrue(DictionaryAPI.IsNullOrEmpty(emptyDictionary));
            Assert.IsFalse(DictionaryAPI.IsNullOrEmpty(populatedDictionary));
        }

        [Test]
        public void AddRange_UsesPairsAndConvertedKeysWithoutReplacingExistingValues()
        {
            var dictionary = new Dictionary<int, string> { [1] = "original" };
            var pairs = new[]
            {
                new KeyValuePair<int, string>(1, "replacement"),
                new KeyValuePair<int, string>(2, "two"),
            };
            var convertedPairs = new[]
            {
                new KeyValuePair<string, string>("3", "three"),
            };

            DictionaryAPI.AddRange(dictionary, pairs);
            DictionaryAPI.AddRange(dictionary, convertedPairs, static key => int.Parse(key));

            Assert.AreEqual("original", dictionary[1]);
            Assert.AreEqual("two", dictionary[2]);
            Assert.AreEqual("three", dictionary[3]);
        }

        [Test]
        public void OverlapsAndAsReadOnly_ReportEquivalentContents()
        {
            var dictionary = new Dictionary<int, int> { [1] = 10, [2] = 20 };
            var equivalent = new Dictionary<int, int> { [2] = 20, [1] = 10 };
            var different = new Dictionary<int, int> { [1] = 10, [2] = 99 };
            var readOnly = DictionaryAPI.AsReadOnly(dictionary);

            Assert.IsTrue(DictionaryAPI.Overlaps(dictionary, equivalent));
            Assert.IsFalse(DictionaryAPI.Overlaps(dictionary, different));
            Assert.AreEqual(2, readOnly.Count);
            Assert.AreEqual(20, readOnly[2]);
        }
    }

    public partial class EncosyHashSetExtenionsTests
    {
        [Test]
        public void OverlapsAndAsReadOnly_ReportExpectedValues()
        {
            var set = new HashSet<int> { 1, 2, 3 };
            var overlapping = new HashSet<int> { 3, 4 };
            var separate = new HashSet<int> { 4, 5 };
            var readOnly = EncosyHashSetExtenions.AsReadOnly(set);

            Assert.IsTrue(EncosyHashSetExtenions.Overlaps(set, overlapping));
            Assert.IsFalse(EncosyHashSetExtenions.Overlaps(set, separate));
            Assert.IsFalse(EncosyHashSetExtenions.Overlaps<int>(null, overlapping));
            Assert.AreEqual(3, readOnly.Count);
            Assert.IsTrue(readOnly.Contains(2));
        }

        [Test]
        public void ReadOnlySetOperations_ProduceExpectedContents()
        {
            var other = EncosyHashSetExtenions.AsReadOnly(new HashSet<int> { 2, 3, 4 });
            var except = new HashSet<int> { 1, 2, 3 };
            var intersect = new HashSet<int> { 1, 2, 3 };
            var symmetric = new HashSet<int> { 1, 2, 3 };

            EncosyHashSetExtenions.ExceptWith(except, other);
            EncosyHashSetExtenions.IntersectWith(intersect, other);
            EncosyHashSetExtenions.SymmetricExceptWith(symmetric, other);

            CollectionAssert.AreEquivalent(new[] { 1 }, except);
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, intersect);
            CollectionAssert.AreEquivalent(new[] { 1, 4 }, symmetric);
        }
    }

    public partial class EncosyICollectionExtensionsTests
    {
        [Test]
        public void IsNullOrEmpty_HandlesCollectionInterfaces()
        {
            ICollection<int> nullCollection = null;
            ICollection<int> emptyCollection = new List<int>();
            ICollection<int> populatedCollection = new List<int> { 1 };
            IReadOnlyCollection<int> nullReadOnlyCollection = null;
            IReadOnlyCollection<int> emptyReadOnlyCollection = new List<int>();
            IReadOnlyCollection<int> populatedReadOnlyCollection = new List<int> { 1 };

            Assert.IsTrue(CollectionAPI.IsNullOrEmpty(nullCollection));
            Assert.IsTrue(CollectionAPI.IsNullOrEmpty(emptyCollection));
            Assert.IsFalse(CollectionAPI.IsNullOrEmpty(populatedCollection));
            Assert.IsTrue(CollectionAPI.IsNullOrEmpty(nullReadOnlyCollection));
            Assert.IsTrue(CollectionAPI.IsNullOrEmpty(emptyReadOnlyCollection));
            Assert.IsFalse(CollectionAPI.IsNullOrEmpty(populatedReadOnlyCollection));
        }

        [Test]
        public void CapacityAndAddRangeFast_UseListFastPaths()
        {
            var list = new List<int>(1) { 1 };
            ICollection<int> collection = list;

            Assert.IsTrue(CollectionAPI.TryIncreaseCapacityByFast(collection, 3));
            Assert.GreaterOrEqual(list.Capacity, 4);
            Assert.IsTrue(CollectionAPI.TryIncreaseCapacityToFast(collection, 8));
            Assert.GreaterOrEqual(list.Capacity, 8);

            CollectionAPI.AddRangeFast(collection, new ReadOnlySpan<int>(new[] { 2, 3 }));
            CollectionAPI.AddRangeFast(collection, (IEnumerable<int>)new[] { 4, 5 });

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list);
        }
    }

    public partial class EncosyICollectionExtensionsUnmanagedTests
    {
        [Test]
        public void AddRangeFastUnmanaged_UsesListFastPaths()
        {
            var list = new List<int> { 1 };
            ICollection<int> collection = list;

            UnmanagedAPI.AddRangeFastUnmanaged(
                  collection
                , new ReadOnlySpan<int>(new[] { 2, 3 })
            );
            UnmanagedAPI.AddRangeFastUnmanaged(
                  collection
                , (IEnumerable<int>)new[] { 4, 5 }
            );

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list);
        }
    }

    public partial class EncosyIEnumerableExtensionsTests
    {
        [Test]
        public void TryGetCountFast_DistinguishesCollectionsFromPureEnumerables()
        {
            IEnumerable<int> collection = new List<int> { 1, 2, 3 };
            IEnumerable<int> enumerable = new PureEnumerable<int>(new[] { 1, 2, 3 });

            Assert.IsTrue(EnumerableAPI.TryGetCountFast(collection, out var collectionCount));
            Assert.AreEqual(3, collectionCount);
            Assert.IsFalse(EnumerableAPI.TryGetCountFast(enumerable, out var enumerableCount));
            Assert.AreEqual(0, enumerableCount);
        }
    }

    public partial class DisposableCollectionExtensionsTests
    {
        [Test]
        public void Dispose_DisposesElementsAndClearsCollection()
        {
            var first = new DisposableSpy();
            var second = new DisposableSpy();
            var collection = new List<IDisposable> { first, null, second };

            DisposableAPI.Dispose(collection);

            Assert.IsTrue(first.IsDisposed);
            Assert.IsTrue(second.IsDisposed);
            Assert.AreEqual(0, collection.Count);
        }
    }

    internal sealed class PureEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;

        public PureEnumerable(IEnumerable<T> source)
        {
            _source = source;
        }

        public IEnumerator<T> GetEnumerator()
            => _source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    internal sealed class DisposableSpy : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
