using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeHashMapTests.cs against the managed
    // SharedArrayMap owner.
    public partial class SharedArrayMapTests
    {
        [Test]
        public void Constructor_IsEmpty()
        {
            using var map = new SharedArrayMap<int, int>(8);

            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void Add_StoresValues()
        {
            using var map = new SharedArrayMap<int, int>(8);

            map.Add(1, 10);
            map.Add(2, 20);

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual(10, map[1]);
            Assert.AreEqual(20, map[2]);
        }

        [Test]
        public void TryAdd_ReturnsFalseOnDuplicate()
        {
            using var map = new SharedArrayMap<int, int>(8);

            Assert.IsTrue(map.TryAdd(1, 10));
            Assert.IsFalse(map.TryAdd(1, 20));
            Assert.AreEqual(10, map[1]);
        }

        [Test]
        public void ContainsKey_ReflectsContent()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(1, 10);

            Assert.IsTrue(map.ContainsKey(1));
            Assert.IsFalse(map.ContainsKey(2));
        }

        [Test]
        public void TryGetValue_ReturnsExpected()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(1, 10);

            Assert.IsTrue(map.TryGetValue(1, out var value));
            Assert.AreEqual(10, value);
            Assert.IsFalse(map.TryGetValue(99, out _));
        }

        [Test]
        public void GetOrAdd_AddsWhenMissing()
        {
            using var map = new SharedArrayMap<int, int>(8);

            ref var slot = ref map.GetOrAdd(1);
            slot = 42;

            Assert.AreEqual(42, map[1]);
        }

        [Test]
        public void Indexer_Set_AddsOrOverwrites()
        {
            using var map = new SharedArrayMap<int, int>(8);

            map[1] = 10;
            Assert.AreEqual(10, map[1]);

            map[1] = 99;
            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(99, map[1]);
        }

        [Test]
        public void Remove_RemovesKey()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(1, 10);
            map.Add(2, 20);

            Assert.IsTrue(map.Remove(1));
            Assert.IsFalse(map.Remove(99));

            Assert.AreEqual(1, map.Count);
            Assert.IsFalse(map.ContainsKey(1));
            Assert.AreEqual(20, map[2]);
        }

        [Test]
        public void Clear_ResetsCount()
        {
            using var map = new SharedArrayMap<int, int>(8);
            map.Add(1, 10);
            map.Add(2, 20);

            map.Clear();

            Assert.AreEqual(0, map.Count);
            Assert.IsFalse(map.ContainsKey(1));
        }

        [Test]
        public void Grow_PastInitialCapacity_KeepsAllEntries()
        {
            const int N = 200;
            using var map = new SharedArrayMap<int, int>(4);

            for (var i = 0; i < N; i++)
            {
                map.Add(i, i * 3);
            }

            Assert.AreEqual(N, map.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.AreEqual(i * 3, map[i], $"key {i}");
            }
        }

        [Test]
        public void ConstructorsAndConversions_AllOwnerFormsCopyExpectedStorage()
        {
            using var convenienceDefault = new SharedArrayMap<int, int>();
            using var convenienceCapacity = new SharedArrayMap<int, int>(4);
            using var genericDefault = new SharedArrayMap<int, int, uint>();
            using var genericCapacity = new SharedArrayMap<int, int, uint>(4);
            genericCapacity.Add(1, -1);

            using var ownerCopy = new SharedArrayMap<int, int, uint>(genericCapacity);
            var native = genericCapacity.AsNative();
            SharedArrayMapNative<int, uint> converted = genericCapacity;
            using var nativeCopy = new SharedArrayMap<int, int, uint>(in native);

            Assert.AreEqual(0, convenienceDefault.Count);
            Assert.GreaterOrEqual(convenienceCapacity.Capacity, 4);
            Assert.AreEqual(0, genericDefault.Count);
            Assert.GreaterOrEqual(genericCapacity.Capacity, 4);
            Assert.AreEqual(-1, ownerCopy[1]);
            Assert.AreEqual(uint.MaxValue, native[1]);
            Assert.AreEqual(uint.MaxValue, converted[1]);
            Assert.AreEqual(-1, nativeCopy[1]);

            ownerCopy[1] = 10;
            nativeCopy[1] = 20;

            Assert.AreEqual(-1, genericCapacity[1]);
        }

        [Test]
        public void OwnerMembers_OverloadsReferencesFindAndCapacityOperationsWork()
        {
            using var map = new SharedArrayMap<int, int>(4);

            Assert.IsTrue(map.TryAdd(1, 10, out var firstIndex));
            Assert.AreEqual(0, firstIndex);
            Assert.IsFalse(map.TryAdd(1, 20, out var existingIndex));
            Assert.AreEqual(0, existingIndex);

            map.GetOrAdd(2, out var secondIndex) = 20;
            Assert.AreEqual(1, secondIndex);
            map.GetOrAdd(2, out var repeatedIndex) = 21;
            Assert.AreEqual(secondIndex, repeatedIndex);

            ref var first = ref map.GetValueByRef(1);
            first = 11;

            Assert.IsTrue(map.TryFindIndex(1, out var foundIndex));
            Assert.AreEqual(firstIndex, foundIndex);
            Assert.IsFalse(map.TryFindIndex(99, out _));
            Assert.AreEqual(secondIndex, map.GetIndex(2));
            Assert.IsTrue(map.Keys.GetEnumerator().IsValid);
            Assert.AreEqual(2, map.Values.Length);

            var initialCapacity = map.Capacity;
            map.EnsureCapacity(initialCapacity + 4);
            Assert.GreaterOrEqual(map.Capacity, initialCapacity + 4);

            var ensuredCapacity = map.Capacity;
            map.IncreaseCapacityBy(4);
            Assert.GreaterOrEqual(map.Capacity, ensuredCapacity + 4);

            map.IncreaseCapacityTo(map.Capacity + 4);
            Assert.Greater(map.Capacity, ensuredCapacity);

            Assert.IsTrue(map.Remove(1, out var removedIndex, out var removedValue));
            Assert.AreEqual(firstIndex, removedIndex);
            Assert.AreEqual(11, removedValue);
            Assert.IsFalse(map.Remove(99, out _, out _));

            map.Trim();

            Assert.AreEqual(map.Count, map.Capacity);
            Assert.AreEqual(21, map[2]);
        }

        [Test]
        public void SetOperations_IntersectExcludeAndUnionMutateExpectedKeys()
        {
            using var intersected = CreateMap(1, 2, 3);
            using var intersectKeys = new SharedArrayMap<int, long>(4);
            intersectKeys.Add(2, 20);
            intersectKeys.Add(3, 30);
            intersectKeys.Add(4, 40);

            intersected.Intersect(intersectKeys);

            Assert.AreEqual(2, intersected.Count);
            Assert.IsTrue(intersected.ContainsKey(2));
            Assert.IsTrue(intersected.ContainsKey(3));

            using var excluded = CreateMap(1, 2, 3);
            using var excludeKeys = new SharedArrayMap<int, long>(4);
            excludeKeys.Add(2, 20);
            excludeKeys.Add(3, 30);

            excluded.Exclude(excludeKeys);

            Assert.AreEqual(1, excluded.Count);
            Assert.IsTrue(excluded.ContainsKey(1));

            using var union = CreateMap(1, 2);
            using var unionSource = new SharedArrayMap<int, int>(4);
            unionSource.Add(2, 200);
            unionSource.Add(3, 300);

            union.Union(unionSource);

            Assert.AreEqual(3, union.Count);
            Assert.AreEqual(10, union[1]);
            Assert.AreEqual(200, union[2]);
            Assert.AreEqual(300, union[3]);
        }

        [Test]
        public void ReadOnly_ConstructionConversionViewsAndMembersReflectOwner()
        {
            using var map = new SharedArrayMap<int, int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);

            var direct = new SharedArrayMap<int, int, int>.ReadOnly(map);
            var fromOwner = map.AsReadOnly();
            SharedArrayMap<int, int, int>.ReadOnly converted = map;
            SharedArrayMap<int, int, int>.ReadOnly nullConverted = null;
            var empty = SharedArrayMap<int, int, int>.ReadOnly.Empty;
            var native = direct.AsNative();

            Assert.IsTrue(direct.IsCreated);
            Assert.AreEqual(map.Capacity, direct.Capacity);
            Assert.AreEqual(2, direct.Count);
            Assert.IsTrue(direct.Keys.IsValid);
            Assert.AreEqual(2, direct.Values.Length);
            Assert.AreEqual(10, direct[1]);
            Assert.IsTrue(direct.ContainsKey(2));
            Assert.IsFalse(direct.ContainsKey(3));
            Assert.IsTrue(direct.TryGetValue(2, out var value));
            Assert.AreEqual(20, value);
            Assert.IsFalse(direct.TryGetValue(3, out _));
            Assert.IsTrue(direct.TryFindIndex(2, out var index));
            Assert.AreEqual(1, index);
            Assert.IsFalse(direct.TryFindIndex(3, out _));
            Assert.AreEqual(0, direct.GetIndex(1));
            Assert.AreEqual(20, fromOwner[2]);
            Assert.AreEqual(10, converted[1]);
            Assert.IsFalse(empty.IsCreated);
            Assert.AreEqual(0, empty.Count);
            Assert.IsFalse(nullConverted.IsCreated);
            Assert.AreEqual(0, nullConverted.Count);
            Assert.IsTrue(native.IsCreated);
            Assert.AreEqual(20, native[2]);
        }

        [Test]
        public void CollectionInterfaces_DispatchEveryExplicitMember()
        {
            using var source = new SharedArrayMap<int, int>(4);
            source.Add(1, 10);
            var sourceEnumerator = source.GetEnumerator();
            Assert.IsTrue(sourceEnumerator.MoveNext());
            var pair = sourceEnumerator.Current;

            using var target = new SharedArrayMap<int, int>(4);
            ICollection<SharedArrayMapKeyValuePair<int, int, int>> collection = target;

            Assert.IsFalse(collection.IsReadOnly);

            collection.Add(pair);

            Assert.IsTrue(collection.Contains(pair));
            Assert.AreEqual(1, collection.Count);
            Assert.Throws<NotImplementedException>(() =>
                collection.CopyTo(new SharedArrayMapKeyValuePair<int, int, int>[1], 0)
            );
            Assert.IsTrue(collection.Remove(pair));
            Assert.IsFalse(collection.Remove(pair));

            collection.Add(pair);
            collection.Clear();

            Assert.AreEqual(0, collection.Count);

            IEnumerable<SharedArrayMapKeyValuePair<int, int, int>> genericEnumerable = source;
            var genericEnumerator = genericEnumerable.GetEnumerator();
            Assert.IsTrue(genericEnumerator.MoveNext());
            Assert.AreEqual(1, genericEnumerator.Current.Key);
            genericEnumerator.Dispose();

            IEnumerable enumerable = source;
            var enumerator = enumerable.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsInstanceOf<SharedArrayMapKeyValuePair<int, int, int>>(enumerator.Current);

            IReadOnlyCollection<SharedArrayMapKeyValuePair<int, int, int>> readOnlyCollection = source;
            Assert.AreEqual(1, readOnlyCollection.Count);

            sourceEnumerator.Dispose();
        }

        [Test]
        public void Enumerators_DirectConstructionMovementCurrentResetAndDispose()
        {
            using var map = new SharedArrayMap<int, int, int>(4);
            map.Add(7, 70);

            var keys = new SharedArrayMap<int, int, int>.KeyEnumerable(map);
            Assert.IsTrue(keys.IsValid);

            var keyEnumerator = new SharedArrayMap<int, int, int>.KeyEnumerator(map);
            Assert.IsTrue(keyEnumerator.IsValid);
            Assert.IsTrue(keyEnumerator.MoveNext());
            Assert.AreEqual(7, keyEnumerator.Current);
            Assert.IsFalse(keyEnumerator.MoveNext());
            keyEnumerator.Reset();
            Assert.IsTrue(keyEnumerator.MoveNext());
            keyEnumerator.Dispose();

            var keysEnumerator = keys.GetEnumerator();
            Assert.IsTrue(keysEnumerator.MoveNext());
            keysEnumerator.Dispose();

            IEnumerable<int> genericKeys = keys;
            var genericKeysEnumerator = genericKeys.GetEnumerator();
            Assert.IsTrue(genericKeysEnumerator.MoveNext());
            Assert.AreEqual(7, genericKeysEnumerator.Current);
            genericKeysEnumerator.Dispose();

            IEnumerable nonGenericKeys = keys;
            var nonGenericKeysEnumerator = nonGenericKeys.GetEnumerator();
            Assert.IsTrue(nonGenericKeysEnumerator.MoveNext());
            Assert.AreEqual(7, nonGenericKeysEnumerator.Current);

            var ownerEnumerator = new SharedArrayMapKeyValueEnumerator<int, int, int>(map);
            Assert.IsTrue(ownerEnumerator.IsValid);
            Assert.IsTrue(ownerEnumerator.MoveNext());
            AssertPair(ownerEnumerator.Current, 7, 70);
            Assert.IsFalse(ownerEnumerator.MoveNext());
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            ownerEnumerator.Dispose();

            var mapEnumerator = map.GetEnumerator();
            Assert.IsTrue(mapEnumerator.MoveNext());
            mapEnumerator.Dispose();

            var readOnly = map.AsReadOnly();
            var readOnlyKeys = new SharedArrayMap<int, int, int>.ReadOnly.KeyEnumerable(in readOnly);
            Assert.IsTrue(readOnlyKeys.IsValid);

            var readOnlyKeyEnumerator =
                new SharedArrayMap<int, int, int>.ReadOnly.KeyEnumerator(in readOnly);
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

            IEnumerable<int> genericReadOnlyKeys = readOnlyKeys;
            var genericReadOnlyKeysEnumerator = genericReadOnlyKeys.GetEnumerator();
            Assert.IsTrue(genericReadOnlyKeysEnumerator.MoveNext());
            Assert.AreEqual(7, genericReadOnlyKeysEnumerator.Current);
            genericReadOnlyKeysEnumerator.Dispose();

            IEnumerable nonGenericReadOnlyKeys = readOnlyKeys;
            var nonGenericReadOnlyKeysEnumerator = nonGenericReadOnlyKeys.GetEnumerator();
            Assert.IsTrue(nonGenericReadOnlyKeysEnumerator.MoveNext());
            Assert.AreEqual(7, nonGenericReadOnlyKeysEnumerator.Current);

            var readOnlyEnumerator =
                new SharedArrayMapReadOnlyKeyValueEnumerator<int, int, int>(in readOnly);
            Assert.IsTrue(readOnlyEnumerator.IsValid);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            AssertPair(readOnlyEnumerator.Current, 7, 70);
            Assert.IsFalse(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            IEnumerator readOnlyInterface = readOnlyEnumerator;
            Assert.IsInstanceOf<SharedArrayMapReadOnlyKeyValuePair<int, int, int>>(
                readOnlyInterface.Current
            );
            readOnlyEnumerator.Dispose();

            var viewEnumerator = readOnly.GetEnumerator();
            Assert.IsTrue(viewEnumerator.MoveNext());
            viewEnumerator.Dispose();

            Assert.IsFalse(default(SharedArrayMap<int, int, int>.KeyEnumerable).IsValid);
            Assert.IsFalse(default(SharedArrayMap<int, int, int>.KeyEnumerator).IsValid);
            Assert.IsFalse(default(SharedArrayMapKeyValueEnumerator<int, int, int>).IsValid);
            Assert.IsFalse(default(SharedArrayMap<int, int, int>.ReadOnly.KeyEnumerable).IsValid);
            Assert.IsFalse(default(SharedArrayMap<int, int, int>.ReadOnly.KeyEnumerator).IsValid);
            Assert.IsFalse(default(SharedArrayMapReadOnlyKeyValueEnumerator<int, int, int>).IsValid);
        }

        [Test]
        public void KeyValuePairs_PublicConstructorsPropertiesValidityAndDeconstructReflectStorage()
        {
            using var sharedValues = new SharedArray<int>(new[] { 10, 20 });
            using var nativeValues = new NativeArray<int>(new[] { 30, 40 }, Allocator.Temp);
            var key = 7;
            var pair = new SharedArrayMapKeyValuePair<int, int, int>(in key, sharedValues, 1);
            var readOnlyPair = new SharedArrayMapReadOnlyKeyValuePair<int, int, int>(
                  in key
                , nativeValues.AsReadOnly()
                , 0
            );

            AssertPair(pair, 7, 20);
            AssertPair(readOnlyPair, 7, 30);
            Assert.IsFalse(default(SharedArrayMapKeyValuePair<int, int, int>).IsValid);
            Assert.IsFalse(default(SharedArrayMapReadOnlyKeyValuePair<int, int, int>).IsValid);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var map = new SharedArrayMap<int, int>(4);
            map.Add(1, 10);

            Assert.DoesNotThrow(() => map.Dispose());
            Assert.DoesNotThrow(() => map.Dispose());
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

        private static void AssertPair(SharedArrayMapKeyValuePair<int, int, int> pair, int key, int value)
        {
            Assert.IsTrue(pair.IsValid);
            Assert.AreEqual(key, pair.Key);
            Assert.AreEqual(value, pair.Value);

            pair.Deconstruct(out var actualKey, out var actualValue);

            Assert.AreEqual(key, actualKey);
            Assert.AreEqual(value, actualValue);
        }

        private static void AssertPair(
              SharedArrayMapReadOnlyKeyValuePair<int, int, int> pair
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
        public void Add_CollisionChurnPastBucketCount_RecomputeRecountsCollisions()
        {
            using var map = new SharedArrayMap<CollidingKey, int>(8);

            map.Add(new CollidingKey(1), 10);
            map.Add(new CollidingKey(2), 20); // cumulative collision 1

            for (var i = 0; i < 11; i++) // collisions 2..12; the add at i == 10 crosses 11
            {
                map.Add(new CollidingKey(3), 30);
                Assert.IsTrue(map.Remove(new CollidingKey(3)));
            }

            Assert.AreEqual(2, map.Count);
            Assert.IsTrue(map.TryGetValue(new CollidingKey(1), out var value1));
            Assert.AreEqual(10, value1);
            Assert.IsTrue(map.TryGetValue(new CollidingKey(2), out var value2));
            Assert.AreEqual(20, value2);

            // A recounted counter reflects live chains only: three colliding keys were in
            // the map at recompute time, which is two chain links. The stale-accumulation
            // bug reports 14 here (12 carried over + 2 recounted).
            Assert.LessOrEqual(map._collisions.ValueRO, (uint)map.Count);
        }
    }
}
