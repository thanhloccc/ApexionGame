using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeHashMapTests.cs against the
    // SharedArrayMapNative view (a fixed-capacity window onto the owner's shared buffers).
    public partial class SharedArrayMapNativeTests
    {
        [Test]
        public void AsNative_ReflectsOwnerContent()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(1, 10);
            map.Add(2, 20);

            var view = map.AsNative();

            Assert.IsTrue(view.IsCreated);
            Assert.AreEqual(2, view.Count);
            Assert.IsTrue(view.ContainsKey(1));
            Assert.IsTrue(view.TryGetValue(2, out var value));
            Assert.AreEqual(20, value);
        }

        [Test]
        public void ViewAdd_WithinCapacity_OwnerObservesChange()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(1, 10);

            var view = map.AsNative();
            view.Add(2, 20);

            Assert.AreEqual(2, view.Count);
            Assert.AreEqual(2, map.Count);
            Assert.AreEqual(20, map[2]);
        }

        [Test]
        public void MutableView_AllMembersMutateAndReportOwnerStorage()
        {
            using var map = new SharedArrayMap<int, int>(16);
            var view = map.AsNative();
            var value = 10;

            view.Add(1, in value);

            Assert.IsTrue(view.IsCreated);
            Assert.GreaterOrEqual(view.Capacity, 16);
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(1, view.Values.Length);
            Assert.AreEqual(10, view.Values[0]);
            Assert.AreEqual(10, view[1]);

            view[1] = 11;
            view[2] = 20;

            Assert.AreEqual(11, map[1]);
            Assert.AreEqual(20, map[2]);
            Assert.IsTrue(view.TryAdd(3, 30));
            Assert.IsFalse(view.TryAdd(3, 31));
            Assert.IsTrue(view.TryAdd(4, 40, out var fourthIndex));
            Assert.AreEqual(3, fourthIndex);
            Assert.IsFalse(view.TryAdd(4, 41, out var existingIndex));
            Assert.AreEqual(fourthIndex, existingIndex);

            view.GetOrAdd(5) = 50;
            view.GetOrAdd(6, out var sixthIndex) = 60;

            Assert.AreEqual(5, sixthIndex);
            Assert.AreEqual(50, map[5]);
            Assert.AreEqual(60, map[6]);

            ref var first = ref view.GetValueByRef(1);
            first = 12;

            Assert.AreEqual(12, map[1]);
            Assert.IsTrue(view.ContainsKey(2));
            Assert.IsFalse(view.ContainsKey(99));
            Assert.IsTrue(view.TryGetValue(3, out var foundValue));
            Assert.AreEqual(30, foundValue);
            Assert.IsFalse(view.TryGetValue(99, out var missingValue));
            Assert.AreEqual(0, missingValue);
            Assert.IsTrue(view.TryFindIndex(4, out var foundIndex));
            Assert.AreEqual(fourthIndex, foundIndex);
            Assert.IsFalse(view.TryFindIndex(99, out _));
            Assert.AreEqual(sixthIndex, view.GetIndex(6));

            Assert.IsTrue(view.Remove(2));
            Assert.IsFalse(view.Remove(99));
            Assert.IsTrue(view.Remove(3, out var removedIndex, out var removedValue));
            Assert.GreaterOrEqual(removedIndex, 0);
            Assert.AreEqual(30, removedValue);
            Assert.IsFalse(view.Remove(99, out _, out _));

            view.Clear();

            Assert.AreEqual(0, view.Count);
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void SetOperations_IntersectExcludeAndUnionMutateOwnerStorage()
        {
            using var intersectedOwner = CreateMap(1, 2, 3);
            using var intersectKeysOwner = new SharedArrayMap<int, long>(8);
            intersectKeysOwner.Add(2, 20);
            intersectKeysOwner.Add(3, 30);
            intersectKeysOwner.Add(4, 40);
            var intersected = intersectedOwner.AsNative();
            var intersectKeys = intersectKeysOwner.AsNative();

            intersected.Intersect(in intersectKeys);

            Assert.AreEqual(2, intersectedOwner.Count);
            Assert.IsTrue(intersectedOwner.ContainsKey(2));
            Assert.IsTrue(intersectedOwner.ContainsKey(3));

            using var excludedOwner = CreateMap(1, 2, 3);
            using var excludeKeysOwner = new SharedArrayMap<int, long>(8);
            excludeKeysOwner.Add(2, 20);
            excludeKeysOwner.Add(3, 30);
            var excluded = excludedOwner.AsNative();
            var excludeKeys = excludeKeysOwner.AsNative();

            excluded.Exclude(in excludeKeys);

            Assert.AreEqual(1, excludedOwner.Count);
            Assert.IsTrue(excludedOwner.ContainsKey(1));

            using var unionOwner = CreateMap(1, 2);
            using var unionSourceOwner = new SharedArrayMap<int, int>(8);
            unionSourceOwner.Add(2, 200);
            unionSourceOwner.Add(3, 300);
            var union = unionOwner.AsNative();
            var unionSource = unionSourceOwner.AsNative();

            union.Union(in unionSource);

            Assert.AreEqual(3, unionOwner.Count);
            Assert.AreEqual(10, unionOwner[1]);
            Assert.AreEqual(200, unionOwner[2]);
            Assert.AreEqual(300, unionOwner[3]);
        }

        [Test]
        public void ReadOnly_ViewConversionPropertiesAndLookupsReflectOwner()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(1, 10);
            map.Add(2, 20);
            var view = map.AsNative();
            var direct = view.AsReadOnly();
            SharedArrayMapNative<int, int>.ReadOnly converted = view;

            Assert.IsTrue(direct.IsCreated);
            Assert.AreEqual(view.Capacity, direct.Capacity);
            Assert.AreEqual(2, direct.Count);
            Assert.IsTrue(direct.Keys.IsValid);
            Assert.AreEqual(2, direct.Values.Length);
            Assert.AreEqual(10, direct.Values[0]);
            Assert.AreEqual(10, direct[1]);
            Assert.IsTrue(direct.ContainsKey(2));
            Assert.IsFalse(direct.ContainsKey(99));
            Assert.IsTrue(direct.TryGetValue(2, out var foundValue));
            Assert.AreEqual(20, foundValue);
            Assert.IsFalse(direct.TryGetValue(99, out var missingValue));
            Assert.AreEqual(0, missingValue);
            Assert.IsTrue(direct.TryFindIndex(2, out var foundIndex));
            Assert.AreEqual(1, foundIndex);
            Assert.IsFalse(direct.TryFindIndex(99, out _));
            Assert.AreEqual(0, direct.GetIndex(1));
            Assert.AreEqual(10, converted[1]);

            var version = direct.Version;
            view.GetValueByRef(1) = 11;

            Assert.Greater(direct.Version, version);
            Assert.AreEqual(11, direct[1]);
        }

        [Test]
        public void EnumeratorsAndPairs_DirectConstructionAndLifecycleExposeExpectedValues()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(7, 70);
            var view = map.AsNative();
            var keysFromView = view.Keys;
            var keys = new SharedArrayMapNative<int, int>.KeyEnumerable(in view);
            var keyEnumerator = new SharedArrayMapNative<int, int>.KeyEnumerator(in view);

            var keyFromViewEnumerator = keysFromView.GetEnumerator();
            Assert.IsTrue(keyFromViewEnumerator.MoveNext());
            Assert.AreEqual(7, keyFromViewEnumerator.Current);
            Assert.IsTrue(keyEnumerator.IsValid);
            Assert.IsTrue(keyEnumerator.MoveNext());
            Assert.AreEqual(7, keyEnumerator.Current);
            Assert.IsFalse(keyEnumerator.MoveNext());

            var keysEnumerator = keys.GetEnumerator();
            Assert.IsTrue(keysEnumerator.MoveNext());
            Assert.AreEqual(7, keysEnumerator.Current);

            var ownerEnumerator = new SharedArrayMapNativeKeyValueEnumerator<int, int>(view);
            Assert.IsTrue(ownerEnumerator.IsValid);
            Assert.IsTrue(ownerEnumerator.MoveNext());
            AssertPair(ownerEnumerator.Current, 7, 70);
            Assert.IsFalse(ownerEnumerator.MoveNext());
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            IEnumerator ownerInterface = ownerEnumerator;
            Assert.IsInstanceOf<SharedArrayMapNativeKeyValuePair<int, int>>(ownerInterface.Current);
            ownerEnumerator.Dispose();

            var viewEnumerator = view.GetEnumerator();
            Assert.IsTrue(viewEnumerator.MoveNext());
            viewEnumerator.Dispose();

            using var values = new NativeArray<int>(new[] { 80, 90 }, Allocator.Temp);
            var pairKey = 8;
            var pair = new SharedArrayMapNativeKeyValuePair<int, int>(in pairKey, values, 1);
            AssertPair(pair, 8, 90);

            var readOnly = view.AsReadOnly();
            var readOnlyKeys = new SharedArrayMapNative<int, int>.ReadOnly.KeyEnumerable(in readOnly);
            var readOnlyKeyEnumerator =
                new SharedArrayMapNative<int, int>.ReadOnly.KeyEnumerator(in readOnly);

            Assert.IsTrue(readOnlyKeys.IsValid);
            Assert.IsTrue(readOnlyKeyEnumerator.IsValid);
            Assert.IsTrue(readOnlyKeyEnumerator.MoveNext());
            Assert.AreEqual(7, readOnlyKeyEnumerator.Current);
            Assert.IsFalse(readOnlyKeyEnumerator.MoveNext());
            readOnlyKeyEnumerator.Reset();
            Assert.IsTrue(readOnlyKeyEnumerator.MoveNext());
            readOnlyKeyEnumerator.Dispose();

            var readOnlyKeysEnumerator = readOnlyKeys.GetEnumerator();
            Assert.IsTrue(readOnlyKeysEnumerator.MoveNext());
            readOnlyKeysEnumerator.Dispose();

            IEnumerable<int> genericKeys = readOnlyKeys;
            var genericKeysEnumerator = genericKeys.GetEnumerator();
            Assert.IsTrue(genericKeysEnumerator.MoveNext());
            Assert.AreEqual(7, genericKeysEnumerator.Current);
            genericKeysEnumerator.Dispose();

            IEnumerable nonGenericKeys = readOnlyKeys;
            var nonGenericKeysEnumerator = nonGenericKeys.GetEnumerator();
            Assert.IsTrue(nonGenericKeysEnumerator.MoveNext());
            Assert.AreEqual(7, nonGenericKeysEnumerator.Current);

            var readOnlyEnumerator =
                new SharedArrayMapNativeReadOnlyKeyValueEnumerator<int, int>(in readOnly);
            Assert.IsTrue(readOnlyEnumerator.IsValid);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            AssertPair(readOnlyEnumerator.Current, 7, 70);
            Assert.IsFalse(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            IEnumerator readOnlyInterface = readOnlyEnumerator;
            Assert.IsInstanceOf<SharedArrayMapNativeReadOnlyKeyValuePair<int, int>>(
                readOnlyInterface.Current
            );
            readOnlyEnumerator.Dispose();

            var readOnlyViewEnumerator = readOnly.GetEnumerator();
            Assert.IsTrue(readOnlyViewEnumerator.MoveNext());
            readOnlyViewEnumerator.Dispose();

            var readOnlyPair = new SharedArrayMapNativeReadOnlyKeyValuePair<int, int>(
                  in pairKey
                , values.AsReadOnly()
                , 0
            );
            AssertPair(readOnlyPair, 8, 80);

            Assert.IsFalse(default(SharedArrayMapNative<int, int>.KeyEnumerator).IsValid);
            Assert.IsFalse(default(SharedArrayMapNative<int, int>).IsCreated);
            Assert.IsFalse(default(SharedArrayMapNativeKeyValueEnumerator<int, int>).IsValid);
            Assert.IsFalse(default(SharedArrayMapNative<int, int>.ReadOnly).IsCreated);
            Assert.IsFalse(default(SharedArrayMapNative<int, int>.ReadOnly.KeyEnumerable).IsValid);
            Assert.IsFalse(default(SharedArrayMapNative<int, int>.ReadOnly.KeyEnumerator).IsValid);
            Assert.IsFalse(default(SharedArrayMapNativeReadOnlyKeyValueEnumerator<int, int>).IsValid);
            Assert.IsFalse(default(SharedArrayMapNativeReadOnlyKeyValuePair<int, int>).IsValid);
        }

        [Test]
        public void CurrentViewAliasesOwnerAndRefreshedPostGrowViewUsesNewStorage()
        {
            using var map = new SharedArrayMap<int, int>(2);
            map.Add(0, 0);
            var current = map.AsNative();

            current[0] = 10;

            Assert.AreEqual(10, map[0]);

            for (var i = 1; i < 64; i++)
            {
                map.Add(i, i);
            }

            var refreshed = map.AsNative();
            refreshed[63] = 630;

            Assert.AreEqual(map.Capacity, refreshed.Capacity);
            Assert.AreEqual(630, map[63]);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void FixedCapacityView_CannotGrowOwnerStorage()
        {
            using var map = new SharedArrayMap<int, int>(2);
            var view = map.AsNative();

            for (var i = 0; i < view.Capacity; i++)
            {
                view.Add(i, i * 10);
            }

            var capacity = map.Capacity;
            var count = map.Count;

            Assert.Throws<InvalidOperationException>(() => view.Add(count, count * 10));
            Assert.AreEqual(capacity, map.Capacity);
            Assert.AreEqual(count, map.Count);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void OwnerDispose_InvalidatesMutableAndReadOnlyViews()
        {
            var map = new SharedArrayMap<int, int>(4);
            map.Add(1, 10);
            var view = map.AsNative();
            var readOnly = view.AsReadOnly();

            map.Dispose();

            Assert.Catch(() => _ = view.Count);
            Assert.Catch(() => _ = readOnly.Count);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void ViewCreatedBeforeOwnerResize_ThrowsOnWrite()
        {
            // The view copies the owner buffer's safety handle at AsNative() time. Growing
            // the owner past capacity re-pins the buffers and releases that handle, so a
            // write through the stale view must throw.
            var map = new SharedArrayMap<int, int>(2);

            try
            {
                map.Add(0, 0);

                var staleView = map.AsNative();

                for (var i = 1; i < 64; i++)
                {
                    map.Add(i, i); // forces growth -> re-pin -> old handle released
                }

                Assert.Catch(() => staleView.Add(999, 999));
            }
            finally
            {
                map.Dispose();
            }
        }

        private static SharedArrayMap<int, int> CreateMap(params int[] keys)
        {
            var map = new SharedArrayMap<int, int>(8);

            for (var i = 0; i < keys.Length; i++)
            {
                map.Add(keys[i], keys[i] * 10);
            }

            return map;
        }

        private static void AssertPair(
              SharedArrayMapNativeKeyValuePair<int, int> pair
            , int key
            , int value
        )
        {
            Assert.AreEqual(key, pair.Key);
            Assert.AreEqual(value, pair.Value);

            pair.Deconstruct(out var actualKey, out var actualValue);

            Assert.AreEqual(key, actualKey);
            Assert.AreEqual(value, actualValue);
        }

        private static void AssertPair(
              SharedArrayMapNativeReadOnlyKeyValuePair<int, int> pair
            , int key
            , int value
        )
        {
            Assert.IsTrue(pair.IsValid);
            Assert.AreEqual(key, pair.Key);
            Assert.AreEqual(value, pair.Value);

            pair.Deconstruct(out var actualKey, out var actualValue);

            Assert.AreEqual(key, actualKey);
            Assert.AreEqual(value, actualValue);
        }

        [Test]
        public void ViewAdd_CollisionChurnPastBucketCount_DoesNotThrowAndPreservesContent()
        {
            using var map = new SharedArrayMap<CollidingKey, int>(8);
            var view = map.AsNative();

            view.Add(new CollidingKey(1), 10);
            view.Add(new CollidingKey(2), 20); // cumulative collision 1

            for (var i = 0; i < 11; i++) // collisions 2..12; the add at i == 10 crosses 11
            {
                view.Add(new CollidingKey(3), 30);
                Assert.IsTrue(view.Remove(new CollidingKey(3)));
            }

            Assert.AreEqual(2, view.Count);
            Assert.IsTrue(view.TryGetValue(new CollidingKey(1), out var value1));
            Assert.AreEqual(10, value1);
            Assert.IsTrue(view.TryGetValue(new CollidingKey(2), out var value2));
            Assert.AreEqual(20, value2);

            view.Add(new CollidingKey(4), 40);

            Assert.AreEqual(3, view.Count);
            Assert.AreEqual(40, view[new CollidingKey(4)]);
            Assert.AreEqual(3, map.Count);
        }
    }
}
