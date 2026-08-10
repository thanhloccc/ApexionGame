// Adapted from Unity.Collections.Tests/NativeHashMapTests.cs.

using System;
using System.Collections.Generic;
using EncosyTower.Buffers;
using EncosyTower.Collections;
using EncosyTower.Common;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ArrayMapTests
    {
        [Test]
        public void Constructors_CreateEmptyMaps()
        {
            using var defaultMap = new ArrayMap<int, int>();
            using var zeroCapacityMap = new ArrayMap<int, int>(0);
            using var capacityMap = new ArrayMap<int, int>(8);

            Assert.AreEqual(0, defaultMap.Count);
            Assert.AreEqual(0, zeroCapacityMap.Count);
            Assert.AreEqual(0, capacityMap.Count);
            Assert.GreaterOrEqual(capacityMap.Capacity, 8);
        }

        [Test]
        public void CapacityZero_AddGrowsAndStoresValue()
        {
            using var map = new ArrayMap<int, int>(0);

            map.Add(1, 10);

            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(10, map[1]);
        }

        [Test]
        public void CopyConstructor_DeepCopiesContent()
        {
            using var source = new ArrayMap<int, int>(4);
            source.Add(1, 10);
            source.Add(2, 20);

            using var copy = new ArrayMap<int, int>(source);
            copy[1] = 100;
            copy.Add(3, 30);

            Assert.AreEqual(10, source[1]);
            Assert.IsFalse(source.ContainsKey(3));
            Assert.AreEqual(100, copy[1]);
            Assert.AreEqual(30, copy[3]);
        }

        [Test]
        public void Add_StoresValuesAndRejectsDuplicateKey()
        {
            using var map = new ArrayMap<int, int>(4);

            map.Add(1, 10);
            map.Add(2, 20);

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual(10, map[1]);
            Assert.AreEqual(20, map[2]);
            Assert.Throws<InvalidOperationException>(() => map.Add(1, 30));
        }

        [Test]
        public void TryAdd_OverloadsReportDuplicateAndIndex()
        {
            using var map = new ArrayMap<int, int>(4);

            Assert.IsTrue(map.TryAdd(1, 10));
            Assert.IsFalse(map.TryAdd(1, 20));
            Assert.IsTrue(map.TryAdd(2, 20, out var addedIndex));
            Assert.AreEqual(1, addedIndex);
            Assert.IsFalse(map.TryAdd(2, 30, out var existingIndex));
            Assert.AreEqual(1, existingIndex);
            Assert.AreEqual(20, map[2]);
        }

        [Test]
        public void Indexer_SetAddsOrOverwrites()
        {
            using var map = new ArrayMap<int, int>(4);

            map[1] = 10;
            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(10, map[1]);

            map[1] = 99;
            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(99, map[1]);
        }

        [Test]
        public void ContainsKeyAndTryGetValue_ReflectContent()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);

            Assert.IsTrue(map.ContainsKey(1));
            Assert.IsFalse(map.ContainsKey(2));
            Assert.IsTrue(map.TryGetValue(1, out var value));
            Assert.AreEqual(10, value);
            Assert.IsFalse(map.TryGetValue(2, out var missing));
            Assert.AreEqual(0, missing);
        }

        [Test]
        public void GetOrAdd_PlainAddsDefaultOrReturnsExistingReference()
        {
            using var map = new ArrayMap<int, int>(4);

            ref var added = ref map.GetOrAdd(1);
            Assert.AreEqual(0, added);
            added = 10;

            ref var existing = ref map.GetOrAdd(1);
            existing = 20;

            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(20, map[1]);
        }

        [Test]
        public void GetOrAdd_BuilderRunsOnlyWhenMissing()
        {
            using var map = new ArrayMap<int, int>(4);
            var buildCount = 0;

            ref var added = ref map.GetOrAdd(1, () =>
            {
                buildCount++;
                return 7;
            });
            ref var existing = ref map.GetOrAdd(1, () =>
            {
                buildCount++;
                return 99;
            });

            Assert.AreEqual(7, added);
            Assert.AreEqual(7, existing);
            Assert.AreEqual(1, buildCount);
        }

        [Test]
        public void GetOrAdd_OutIndexReportsAddedAndExistingIndex()
        {
            using var map = new ArrayMap<int, int>(4);

            map.GetOrAdd(1, out var firstIndex) = 10;
            map.GetOrAdd(2, out var secondIndex) = 20;
            map.GetOrAdd(1, out var existingIndex);

            Assert.AreEqual(0, firstIndex);
            Assert.AreEqual(1, secondIndex);
            Assert.AreEqual(0, existingIndex);
        }

        [Test]
        public void GetOrAdd_FuncRefPassesParameter()
        {
            using var map = new ArrayMap<int, int>(4);
            var parameter = 6;

            ref var value = ref map.GetOrAdd(
                  1
                , static (ref int state) => state * 10
                , ref parameter
            );

            Assert.AreEqual(60, value);
            Assert.AreEqual(60, map[1]);
        }

        [Test]
        public void GetValueByRef_ReturnsWritableReference()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);

            ref var value = ref map.GetValueByRef(1);
            value = 99;

            Assert.AreEqual(99, map[1]);
        }

        [Test]
        public void RecycleOrAdd_ReusesRemovedClassValue()
        {
            using var map = new ArrayMap<int, IReusableValue>(1);
            var original = new ReusableValue { Value = 10 };
            map.Add(1, original);
            map.Remove(1);
            var builderCalled = false;
            Func<ReusableValue> builder = () =>
            {
                builderCalled = true;
                return new ReusableValue { Value = -1 };
            };
            ActionRef<ReusableValue> recycler = static (ref ReusableValue value) =>
                value.Value = 20;

            ref var recycled = ref map.RecycleOrAdd(2, builder, recycler);

            Assert.IsFalse(builderCalled);
            Assert.AreSame(original, recycled);
            Assert.AreEqual(20, recycled.Value);
            Assert.AreSame(original, map[2]);
        }

        [Test]
        public void RecycleOrAdd_WithParameterReusesRemovedClassValue()
        {
            using var map = new ArrayMap<int, IReusableValue>(1);
            var original = new ReusableValue { Value = 10 };
            map.Add(1, original);
            map.Remove(1);
            var parameter = 30;
            FuncRef<int, IReusableValue> builder = static (ref int state) =>
                new ReusableValue { Value = state };
            ActionRef<ReusableValue, int> recycler = static (
                  ref ReusableValue value
                , ref int state
            ) => value.Value = state;

            ref var recycled = ref map.RecycleOrAdd<ReusableValue, int>(
                  2
                , builder
                , recycler
                , ref parameter
            );

            Assert.AreSame(original, recycled);
            Assert.AreEqual(30, recycled.Value);
        }

        [Test]
        public void Remove_OverloadsReportRemovedEntry()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);
            map.Add(3, 30);

            Assert.IsTrue(map.Remove(2, out var index, out var value));
            Assert.GreaterOrEqual(index, 0);
            Assert.AreEqual(20, value);
            Assert.IsFalse(map.ContainsKey(2));
            Assert.IsTrue(map.Remove(1));
            Assert.IsFalse(map.Remove(99));
            Assert.AreEqual(1, map.Count);
        }

        [Test]
        public void CapacityOperations_GrowAndTrimToCount()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);
            var initialCapacity = map.Capacity;

            map.EnsureCapacity(initialCapacity + 10);
            Assert.Greater(map.Capacity, initialCapacity);

            var ensuredCapacity = map.Capacity;
            map.IncreaseCapacityBy(10);
            Assert.GreaterOrEqual(map.Capacity, ensuredCapacity + 10);

            map.IncreaseCapacityTo(map.Capacity + 10);
            Assert.Greater(map.Capacity, ensuredCapacity);

            map.Trim();
            Assert.AreEqual(map.Count, map.Capacity);
            Assert.AreEqual(10, map[1]);
            Assert.AreEqual(20, map[2]);
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);

            map.Clear();

            Assert.AreEqual(0, map.Count);
            Assert.IsFalse(map.ContainsKey(1));
            map.Add(3, 30);
            Assert.AreEqual(30, map[3]);
        }

        [Test]
        public void TryFindIndexAndFindIndex_ReportFoundAndMissingKeys()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);

            Assert.IsTrue(map.TryFindIndex(2, out var index));
            Assert.AreEqual(1, index);
            Assert.IsFalse(map.TryFindIndex(99, out _));
            Assert.AreEqual(0, map.FindIndex(1));
            Assert.AreEqual(-1, map.FindIndex(99));
        }

        [Test]
        public void Intersect_KeepsOnlySharedKeys()
        {
            using var first = new ArrayMap<int, int>(4);
            using var second = new ArrayMap<int, string>(4);
            first.Add(1, 10);
            first.Add(2, 20);
            first.Add(3, 30);
            second.Add(2, "two");
            second.Add(3, "three");
            second.Add(4, "four");

            first.Intersect(second);

            Assert.AreEqual(2, first.Count);
            Assert.IsFalse(first.ContainsKey(1));
            Assert.IsTrue(first.ContainsKey(2));
            Assert.IsTrue(first.ContainsKey(3));
        }

        [Test]
        public void Exclude_RemovesSharedKeys()
        {
            using var first = new ArrayMap<int, int>(4);
            using var second = new ArrayMap<int, string>(4);
            first.Add(1, 10);
            first.Add(2, 20);
            first.Add(3, 30);
            second.Add(2, "two");
            second.Add(3, "three");

            first.Exclude(second);

            Assert.AreEqual(1, first.Count);
            Assert.IsTrue(first.ContainsKey(1));
            Assert.IsFalse(first.ContainsKey(2));
            Assert.IsFalse(first.ContainsKey(3));
        }

        [Test]
        public void Union_MergesAndOverwritesValues()
        {
            using var first = new ArrayMap<int, int>(4);
            using var second = new ArrayMap<int, int>(4);
            first.Add(1, 10);
            first.Add(2, 20);
            second.Add(2, 200);
            second.Add(3, 300);

            first.Union(second);

            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(10, first[1]);
            Assert.AreEqual(200, first[2]);
            Assert.AreEqual(300, first[3]);
        }

        [Test]
        public void Enumerator_DeconstructsAndVisitsAllPairs()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);
            map.Add(3, 30);
            var seen = new Dictionary<int, int>();

            foreach (var pair in map)
            {
                var (key, value) = pair;
                seen[key] = value;
            }

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, seen.Keys);
            Assert.AreEqual(10, seen[1]);
            Assert.AreEqual(20, seen[2]);
            Assert.AreEqual(30, seen[3]);
        }

        [Test]
        public void Enumerator_MutationDuringIteration_Throws()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var pair in map)
                {
                    map.Add(pair.Key + 10, pair.Value);
                }
            });
        }

        [Test]
        public void Keys_EnumeratesAllKeys()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);
            map.Add(3, 30);
            var keys = new HashSet<int>();

            foreach (var key in map.Keys)
            {
                keys.Add(key);
            }

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, keys);
        }

        [Test]
        public void Grow_ManyCollidingPathsKeepAllEntries()
        {
            const int N = 500;
            using var map = new ArrayMap<int, int>(2);

            for (var i = 0; i < N; i++)
            {
                map.Add(i, i * 3);
            }

            Assert.AreEqual(N, map.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(map.TryGetValue(i, out var value), $"missing key {i}");
                Assert.AreEqual(i * 3, value);
            }
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);

            Assert.DoesNotThrow(() => map.Dispose());
            Assert.DoesNotThrow(() => map.Dispose());
        }

        [Test]
        public void ReadOnly_AllPublicMembersReflectOwnerAndCopyConstructor()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);

            var direct = new ArrayMap<int, int>.ReadOnly(map);
            ArrayMap<int, int>.ReadOnly converted = map;
            var empty = ArrayMap<int, int>.ReadOnly.Empty;
            ArrayMap<int, int>.ReadOnly nullConverted = (ArrayMap<int, int>)null;

            Assert.IsTrue(direct.IsCreated);
            Assert.AreEqual(map.Capacity, direct.Capacity);
            Assert.AreEqual(2, direct.Count);
            Assert.AreEqual(2, map.Values.Length);
            Assert.IsTrue(direct.Keys.IsValid);
            Assert.AreEqual(2, direct.Values.Length);
            Assert.AreEqual(10, direct[1]);
            Assert.IsTrue(direct.ContainsKey(2));
            Assert.IsFalse(direct.ContainsKey(3));
            Assert.IsTrue(direct.TryGetValue(2, out var value));
            Assert.AreEqual(20, value);
            Assert.IsFalse(direct.TryGetValue(3, out _));
            Assert.AreEqual(10, direct.GetValueByRef(1));
            Assert.IsTrue(direct.TryFindIndex(2, out var index));
            Assert.AreEqual(1, index);
            Assert.IsFalse(direct.TryFindIndex(3, out _));
            Assert.AreEqual(0, direct.FindIndex(1));
            Assert.AreEqual(-1, direct.FindIndex(3));
            Assert.AreEqual(10, converted[1]);
            Assert.IsTrue(empty.IsCreated);
            Assert.AreEqual(0, empty.Count);
            Assert.IsTrue(nullConverted.IsCreated);
            Assert.AreEqual(0, nullConverted.Count);

            using var copy = new ArrayMap<int, int>(direct);
            Assert.AreEqual(10, copy[1]);
            Assert.AreEqual(20, copy[2]);
        }

        [Test]
        public void Enumerators_DirectConstructionMovementResetCurrentAndDispose()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(7, 70);

            var keys = new ArrayMap<int, int>.KeyEnumerable(map);
            Assert.IsTrue(keys.IsValid);

            var keyEnumerator = new ArrayMap<int, int>.KeyEnumerator(map);
            Assert.IsTrue(keyEnumerator.IsValid);
            Assert.IsTrue(keyEnumerator.MoveNext());
            Assert.AreEqual(7, keyEnumerator.Current);
            Assert.IsFalse(keyEnumerator.MoveNext());
            keyEnumerator.Reset();
            Assert.IsTrue(keyEnumerator.MoveNext());
            keyEnumerator.Dispose();

            var enumerableEnumerator = keys.GetEnumerator();
            Assert.IsTrue(enumerableEnumerator.MoveNext());
            Assert.AreEqual(7, enumerableEnumerator.Current);
            enumerableEnumerator.Dispose();

            var ownerEnumerator = new ArrayMapKeyValueEnumerator<int, int>(map);
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

            var readOnlyEnumerator = map.AsReadOnly().GetEnumerator();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();

            Assert.IsFalse(default(ArrayMap<int, int>.KeyEnumerable).IsValid);
            Assert.IsFalse(default(ArrayMap<int, int>.KeyEnumerator).IsValid);
            Assert.IsFalse(default(ArrayMapKeyValueEnumerator<int, int>).IsValid);
        }

        [Test]
        public void KeyValuePair_ConstructorPropertiesValidityAndDeconstructReflectBuffer()
        {
            var buffer = new BufferManaged<int>(new[] { 10, 20 });
            var key = 7;
            var pair = new ArrayMapKeyValuePair<int, int>(in key, in buffer, 1);

            AssertPair(pair, 7, 20);
            Assert.IsFalse(default(ArrayMapKeyValuePair<int, int>).IsValid);
        }

        [Test]
        public void ArrayMapNode_ConstructorsAndPublicKeyFieldStoreKey()
        {
            var firstKey = 7;
            var secondKey = 8;
            var first = new ArrayMapNode<int>(in firstKey, 70, 2);
            var second = new ArrayMapNode<int>(in secondKey, 80);

            Assert.AreEqual(7, first.key);
            Assert.AreEqual(8, second.key);

            first.key = 9;

            Assert.AreEqual(9, first.key);
        }

        private static void AssertPair(ArrayMapKeyValuePair<int, int> pair, int key, int value)
        {
            Assert.IsTrue(pair.IsValid);
            Assert.AreEqual(key, pair.Key);
            Assert.AreEqual(value, pair.Value);

            pair.Deconstruct(out var actualKey, out var actualValue);

            Assert.AreEqual(key, actualKey);
            Assert.AreEqual(value, actualValue);
        }

        private interface IReusableValue
        {
            int Value { get; set; }
        }

        private sealed class ReusableValue : IReusableValue
        {
            public int Value { get; set; }
        }
    }
}
