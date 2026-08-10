// Adapted from Unity.Collections.Tests/NativeHashSetTests.cs.

using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ArraySetTests
    {
        [Test]
        public void Constructors_CreateEmptySets()
        {
            using var defaultSet = new ArraySet<int>();
            using var capacitySet = new ArraySet<int>(8);

            Assert.AreEqual(0, defaultSet.Count);
            Assert.AreEqual(0, capacitySet.Count);
            Assert.GreaterOrEqual(capacitySet.Capacity, 8);
        }

        [Test]
        public void CopyConstructors_DeepCopyMutableAndReadOnlySources()
        {
            using var source = new ArraySet<int>(4);
            source.Add(1);
            source.Add(2);
            using var mutableCopy = new ArraySet<int>(source);
            using var readOnlyCopy = new ArraySet<int>(source.AsReadOnly());

            mutableCopy.Add(3);
            readOnlyCopy.Add(4);

            Assert.IsFalse(source.Contains(3));
            Assert.IsFalse(source.Contains(4));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, mutableCopy.Items.ToArray());
            CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, readOnlyCopy.Items.ToArray());
        }

        [Test]
        public void Add_StoresUniqueValues()
        {
            using var set = new ArraySet<int>(4);
            var two = 2;

            Assert.IsTrue(set.Add(1));
            Assert.IsTrue(set.Add(in two));
            Assert.IsFalse(set.Add(1));
            Assert.AreEqual(2, set.Count);
        }

        [Test]
        public void Contains_OverloadsReflectContent()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            var one = 1;
            var missing = 2;

            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(in one));
            Assert.IsFalse(set.Contains(in missing));
        }

        [Test]
        public void Remove_OverloadsRemoveValues()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            var two = 2;

            Assert.IsTrue(set.Remove(in two));
            Assert.IsFalse(set.Remove(99));
            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.Contains(2));
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(3));
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            set.Add(2);

            set.Clear();

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.Contains(1));
            set.Add(3);
            Assert.IsTrue(set.Contains(3));
        }

        [Test]
        public void Grow_ManyValuesRemainRetrievable()
        {
            const int N = 500;
            using var set = new ArraySet<int>(2);

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(set.Add(i));
            }

            Assert.AreEqual(N, set.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(set.Contains(i), $"missing {i}");
            }
        }

        [Test]
        public void CopyToAndTryCopyTo_SpanFamiliesCopyOrReturnFalse()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Add(4);
            var expected = set.Items.ToArray();
            var destination = new int[set.Count];

            set.CopyTo(destination.AsSpan());
            CollectionAssert.AreEqual(expected, destination);

            Array.Clear(destination, 0, destination.Length);
            set.CopyTo(destination.AsSpan(), 2);
            set.CopyTo(2, destination.AsSpan(2));
            CollectionAssert.AreEqual(expected, destination);

            Array.Clear(destination, 0, destination.Length);
            set.CopyTo(1, destination.AsSpan(), 2);
            CollectionAssert.AreEqual(
                  expected.AsSpan(1, 2).ToArray()
                , destination.AsSpan(0, 2).ToArray()
            );

            Assert.IsTrue(set.TryCopyTo(destination.AsSpan()));
            Assert.IsTrue(set.TryCopyTo(destination.AsSpan(), 2));
            Assert.IsTrue(set.TryCopyTo(1, destination.AsSpan(0, 3)));
            Assert.IsTrue(set.TryCopyTo(1, destination.AsSpan(), 2));
            Assert.IsFalse(set.TryCopyTo(new int[3].AsSpan(), set.Count));
            Assert.IsFalse(set.TryCopyTo(3, new int[2].AsSpan()));

            var arrayDestination = new int[set.Count + 2];
            set.CopyTo(arrayDestination, 1);
            CollectionAssert.AreEqual(expected, arrayDestination.AsSpan(1, set.Count).ToArray());
        }

        [Test]
        public void CapacityOperations_GrowAndTrimToCount()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            set.Add(2);
            var initialCapacity = set.Capacity;

            set.EnsureCapacity(initialCapacity + 10);
            Assert.Greater(set.Capacity, initialCapacity);

            var ensuredCapacity = set.Capacity;
            set.IncreaseCapacityBy(10);
            Assert.GreaterOrEqual(set.Capacity, ensuredCapacity + 10);

            set.IncreaseCapacityTo(set.Capacity + 10);
            Assert.Greater(set.Capacity, ensuredCapacity);

            set.Trim();
            Assert.AreEqual(set.Count, set.Capacity);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
        }

        [Test]
        public void Intersect_KeepsOnlySharedValues()
        {
            using var first = new ArraySet<int>(4);
            using var second = new ArraySet<int>(4);
            first.Add(1);
            first.Add(2);
            first.Add(3);
            second.Add(2);
            second.Add(3);
            second.Add(4);

            first.Intersect(second);

            CollectionAssert.AreEquivalent(new[] { 2, 3 }, first.Items.ToArray());
        }

        [Test]
        public void Exclude_RemovesSharedValues()
        {
            using var first = new ArraySet<int>(4);
            using var second = new ArraySet<int>(4);
            first.Add(1);
            first.Add(2);
            first.Add(3);
            second.Add(2);
            second.Add(3);

            first.Exclude(second);

            CollectionAssert.AreEquivalent(new[] { 1 }, first.Items.ToArray());
        }

        [Test]
        public void Union_MergesUniqueValues()
        {
            using var first = new ArraySet<int>(4);
            using var second = new ArraySet<int>(4);
            first.Add(1);
            first.Add(2);
            second.Add(2);
            second.Add(3);

            first.Union(second);

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, first.Items.ToArray());
        }

        [Test]
        public void Enumerator_VisitsAllValues()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            var seen = new HashSet<int>();

            foreach (var value in set)
            {
                seen.Add(value);
            }

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, seen);
        }

        [Test]
        public void Enumerator_MutationDuringIteration_Throws()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            set.Add(2);

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var value in set)
                {
                    set.Add(value + 10);
                }
            });
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var set = new ArraySet<int>(4);
            set.Add(1);

            Assert.DoesNotThrow(() => set.Dispose());
            Assert.DoesNotThrow(() => set.Dispose());
        }

        [Test]
        public void ReadOnly_ConstructionConversionPropertiesContainsAndCopiesReflectOwner()
        {
            using var set = new ArraySet<int>(4);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Add(4);
            var direct = new ArraySet<int>.ReadOnly(set);
            var fromOwner = set.AsReadOnly();
            ArraySet<int>.ReadOnly converted = set;
            ArraySet<int>.ReadOnly nullConverted = (ArraySet<int>)null;
            var empty = ArraySet<int>.ReadOnly.Empty;
            var expected = set.Items.ToArray();
            var destination = new int[set.Count];

            Assert.IsTrue(direct.IsCreated);
            Assert.AreEqual(set.Capacity, direct.Capacity);
            Assert.AreEqual(set.Count, direct.Count);
            CollectionAssert.AreEqual(expected, direct.Items.ToArray());
            Assert.IsTrue(direct.Contains(1));
            var two = 2;
            var missing = 99;
            Assert.IsTrue(direct.Contains(in two));
            Assert.IsFalse(direct.Contains(in missing));
            Assert.AreEqual(1, fromOwner.Items.Span[0]);
            Assert.AreEqual(2, converted.Items.Span[1]);
            Assert.IsTrue(empty.IsCreated);
            Assert.AreEqual(0, empty.Count);
            Assert.IsTrue(nullConverted.IsCreated);
            Assert.AreEqual(0, nullConverted.Count);

            direct.CopyTo(destination.AsSpan());
            CollectionAssert.AreEqual(expected, destination);

            Array.Clear(destination, 0, destination.Length);
            direct.CopyTo(destination.AsSpan(), 2);
            direct.CopyTo(2, destination.AsSpan(2));
            CollectionAssert.AreEqual(expected, destination);

            Array.Clear(destination, 0, destination.Length);
            direct.CopyTo(1, destination.AsSpan(), 2);
            CollectionAssert.AreEqual(
                  expected.AsSpan(1, 2).ToArray()
                , destination.AsSpan(0, 2).ToArray()
            );

            Assert.IsTrue(direct.TryCopyTo(destination.AsSpan()));
            Assert.IsTrue(direct.TryCopyTo(destination.AsSpan(), 2));
            Assert.IsTrue(direct.TryCopyTo(1, destination.AsSpan(0, 3)));
            Assert.IsTrue(direct.TryCopyTo(1, destination.AsSpan(), 2));
            Assert.IsFalse(direct.TryCopyTo(new int[3].AsSpan(), set.Count));
            Assert.IsFalse(direct.TryCopyTo(3, new int[2].AsSpan()));
        }

        [Test]
        public void CollectionInterfaces_DispatchAddReadOnlyCopyAndEnumerationMembers()
        {
            using var set = new ArraySet<int>(4);
            ICollection<int> collection = set;

            Assert.IsFalse(collection.IsReadOnly);

            collection.Add(1);
            collection.Add(2);

            Assert.IsTrue(collection.Contains(1));

            var destination = new int[4];
            collection.CopyTo(destination, 1);
            CollectionAssert.AreEqual(new[] { 1, 2 }, destination.AsSpan(1, 2).ToArray());
            Assert.IsTrue(collection.Remove(1));
            Assert.AreEqual(1, collection.Count);

            IEnumerable<int> genericEnumerable = set;
            var genericEnumerator = genericEnumerable.GetEnumerator();
            Assert.IsTrue(genericEnumerator.MoveNext());
            Assert.AreEqual(2, genericEnumerator.Current);
            genericEnumerator.Dispose();

            IEnumerable enumerable = set;
            var enumerator = enumerable.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);

            collection.Clear();

            Assert.AreEqual(0, collection.Count);
        }

        [Test]
        public void Enumerators_DirectConstructionMovementCurrentResetDisposeAndValidity()
        {
            using var set = new ArraySet<int>(4);
            set.Add(7);
            var direct = new ArraySetEnumerator<int>(set);

            Assert.IsTrue(direct.IsValid);
            Assert.IsTrue(direct.MoveNext());
            Assert.AreEqual(7, direct.Current);
            Assert.IsFalse(direct.MoveNext());
            direct.Reset();
            Assert.IsTrue(direct.MoveNext());
            IEnumerator interfaceEnumerator = direct;
            Assert.AreEqual(7, interfaceEnumerator.Current);
            direct.Dispose();

            var ownerEnumerator = set.GetEnumerator();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            ownerEnumerator.Dispose();

            var readOnly = set.AsReadOnly();
            var readOnlyEnumerator = readOnly.GetEnumerator();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();

            IEnumerable<int> genericReadOnly = readOnly;
            var genericReadOnlyEnumerator = genericReadOnly.GetEnumerator();
            Assert.IsTrue(genericReadOnlyEnumerator.MoveNext());
            Assert.AreEqual(7, genericReadOnlyEnumerator.Current);
            genericReadOnlyEnumerator.Dispose();

            IEnumerable nonGenericReadOnly = readOnly;
            var nonGenericReadOnlyEnumerator = nonGenericReadOnly.GetEnumerator();
            Assert.IsTrue(nonGenericReadOnlyEnumerator.MoveNext());
            Assert.AreEqual(7, nonGenericReadOnlyEnumerator.Current);

            IReadOnlyCollection<int> readOnlyCollection = readOnly;
            Assert.AreEqual(1, readOnlyCollection.Count);
            Assert.IsFalse(default(ArraySetEnumerator<int>).IsValid);
        }

        [Test]
        public void HashCollisionsAndRemovalReuse_PreserveRemainingValues()
        {
            using var set = new ArraySet<CollisionValue>(2);
            var first = new CollisionValue(1);
            var second = new CollisionValue(2);
            var third = new CollisionValue(3);

            Assert.IsTrue(set.Add(first));
            Assert.IsTrue(set.Add(second));
            Assert.IsTrue(set.Add(third));
            Assert.IsFalse(set.Add(second));
            Assert.IsTrue(set.Remove(second));
            Assert.IsTrue(set.Add(new CollisionValue(4)));
            Assert.IsTrue(set.Contains(first));
            Assert.IsTrue(set.Contains(third));
            Assert.IsTrue(set.Contains(new CollisionValue(4)));
            Assert.IsFalse(set.Contains(second));
        }

        private readonly struct CollisionValue : IEquatable<CollisionValue>
        {
            private readonly int _value;

            public CollisionValue(int value)
            {
                _value = value;
            }

            public bool Equals(CollisionValue other)
                => _value == other._value;

            public override bool Equals(object obj)
                => obj is CollisionValue other && Equals(other);

            public override int GetHashCode()
                => 1;
        }
    }
}
