// No Unity.Collections.Tests analogue; covers the read-only hash-set view.

using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class HashSetReadOnlyTests
    {
        [Test]
        public void Constructor_ExposesSourceProperties()
        {
            var source = new HashSet<int> { 1, 2, 3 };
            var set = new HashSetReadOnly<int>(source);

            Assert.IsTrue(set.IsCreated);
            Assert.IsTrue(set.IsReadOnly);
            Assert.AreEqual(3, set.Count);
            Assert.GreaterOrEqual(set.Capacity, set.Count);
        }

        [Test]
        public void ContainsAndTryGetValue_FindExpectedValues()
        {
            var set = new HashSetReadOnly<int>(new HashSet<int> { 1, 2, 3 });

            Assert.IsTrue(set.Contains(2));
            Assert.IsFalse(set.Contains(4));
            Assert.IsTrue(set.TryGetValue(3, out var value));
            Assert.AreEqual(3, value);
            Assert.IsFalse(set.TryGetValue(4, out _));
        }

        [Test]
        public void CopyTo_ArrayAndSpanFamiliesCopyValues()
        {
            var set = new HashSetReadOnly<int>(new HashSet<int> { 1, 2, 3 });
            var array = new int[3];
            var arrayWithOffset = new int[5];
            var partialArray = new int[4];
            var span = new int[3];
            var spanWithOffset = new int[5];
            var partialSpan = new int[4];

            set.CopyTo(array);
            set.CopyTo(arrayWithOffset, 1);
            set.CopyTo(partialArray, 1, 2);
            set.CopyTo(new System.Span<int>(span));
            set.CopyTo(new System.Span<int>(spanWithOffset), 1);
            set.CopyTo(new System.Span<int>(partialSpan), 1, 2);

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, array);
            CollectionAssert.AreEquivalent(
                  new[] { 1, 2, 3 }
                , new System.Span<int>(arrayWithOffset, 1, 3).ToArray()
            );
            CollectionAssert.IsSubsetOf(
                  new System.Span<int>(partialArray, 1, 2).ToArray()
                , new[] { 1, 2, 3 }
            );
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, span);
            CollectionAssert.AreEquivalent(
                  new[] { 1, 2, 3 }
                , new System.Span<int>(spanWithOffset, 1, 3).ToArray()
            );
            CollectionAssert.IsSubsetOf(
                  new System.Span<int>(partialSpan, 1, 2).ToArray()
                , new[] { 1, 2, 3 }
            );
        }

        [Test]
        public void SetRelations_ReportExpectedResults()
        {
            var set = new HashSetReadOnly<int>(new HashSet<int> { 1, 2, 3 });
            var subset = new HashSetReadOnly<int>(new HashSet<int> { 1, 2 });
            var equal = new[] { 3, 2, 1 };

            Assert.IsTrue(subset.IsProperSubsetOf(set));
            Assert.IsTrue(set.IsProperSupersetOf(subset));
            Assert.IsTrue(subset.IsSubsetOf(set));
            Assert.IsTrue(set.IsSupersetOf(subset));
            Assert.IsTrue(set.Overlaps(subset));
            Assert.IsFalse(set.SetEquals(subset));
            Assert.IsTrue(set.Overlaps(new[] { 3, 4 }));
            Assert.IsFalse(set.Overlaps(new[] { 4, 5 }));
            Assert.IsTrue(set.SetEquals(equal));
            Assert.IsTrue(set.IsProperSupersetOf(new[] { 1, 2 }));
            Assert.IsTrue(subset.IsProperSubsetOf(equal));
            Assert.IsTrue(subset.IsSubsetOf(equal));
            Assert.IsTrue(set.IsSupersetOf(new[] { 1, 2 }));
        }

        [Test]
        public void Enumerator_VisitsEveryValue()
        {
            var set = new HashSetReadOnly<int>(new HashSet<int> { 1, 2, 3 });
            var values = new List<int>();

            foreach (var value in set)
            {
                values.Add(value);
            }

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, values);

            IEnumerable<int> genericEnumerable = set;
            var genericEnumerator = genericEnumerable.GetEnumerator();
            Assert.IsTrue(genericEnumerator.MoveNext());
            genericEnumerator.Dispose();

            IEnumerable enumerable = set;
            var enumerator = enumerable.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
        }

        [Test]
        public void Empty_IsCreatedAndContainsNoValues()
        {
            var set = HashSetReadOnly<int>.Empty;
            var constructed = new HashSetReadOnly<int>();

            Assert.IsTrue(set.IsCreated);
            Assert.IsTrue(set.IsReadOnly);
            Assert.AreEqual(0, set.Count);
            Assert.AreEqual(0, set.Capacity);
            Assert.IsTrue(constructed.IsCreated);
            Assert.AreEqual(0, constructed.Count);
        }

        [Test]
        public void ImplicitConversion_ReflectsLaterSourceMutations()
        {
            var source = new HashSet<int>();
            HashSetReadOnly<int> set = source;

            source.Add(42);

            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains(42));
        }

        [Test]
        public void EqualsAndGetHashCode_UseSourceIdentity()
        {
            var source = new HashSet<int> { 1, 2 };
            var same = new HashSetReadOnly<int>(source);
            var alias = new HashSetReadOnly<int>(source);
            var other = new HashSetReadOnly<int>(new HashSet<int> { 1, 2 });

            Assert.IsTrue(same.Equals(alias));
            Assert.IsTrue(same.Equals(source));
            Assert.AreEqual(same.GetHashCode(), alias.GetHashCode());
            Assert.IsFalse(same.Equals(other));
        }
    }
}
