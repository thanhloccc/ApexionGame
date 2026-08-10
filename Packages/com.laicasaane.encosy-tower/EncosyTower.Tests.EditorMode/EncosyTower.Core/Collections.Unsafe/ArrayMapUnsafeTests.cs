using System;
using System.Collections.Generic;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using EncosyTower.TypeWraps;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    // Adapted from Unity.Collections.Tests/UnsafeHashMapTests.cs + NativeHashMapTests.cs,
    // mirroring the ArrayMapNativeTests compatibility gate against the raw Unsafe header.
    public partial class ArrayMapUnsafeTests
    {
        [WrapRecord]
        public readonly partial record struct UintId(uint Value);

        [Test]
        public void Constructor_CreatesValidMap()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);

            Assert.IsTrue(map.IsCreated);
            Assert.AreEqual(0, map.Count);
            Assert.GreaterOrEqual(map.Capacity, 4);
        }

        [Test]
        public void DefaultMap_IsNotCreated()
        {
            ArrayMapUnsafe<int, int> map = default;

            Assert.IsFalse(map.IsCreated);
        }

        [Test]
        public void Add_StoresValues()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);

            map.Add(1, 10);
            map.Add(2, 20);
            map.Add(3, 30);

            Assert.AreEqual(3, map.Count);
            Assert.AreEqual(10, map[1]);
            Assert.AreEqual(20, map[2]);
            Assert.AreEqual(30, map[3]);
        }

        [Test]
        public void Add_DuplicateKey_Throws()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);

            Assert.Throws<InvalidOperationException>(() => map.Add(1, 20));
        }

        [Test]
        public void Indexer_Set_AddsOrOverwrites()
        {
            var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);

            try
            {
                map[1] = 10;
                Assert.AreEqual(1, map.Count);
                Assert.AreEqual(10, map[1]);

                map[1] = 99;
                Assert.AreEqual(1, map.Count);
                Assert.AreEqual(99, map[1]);
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void TryAdd_ReturnsFalseOnDuplicate()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);

            Assert.IsTrue(map.TryAdd(1, 10, out var index0));
            Assert.AreEqual(0, index0);

            Assert.IsFalse(map.TryAdd(1, 20, out var index1));
            Assert.AreEqual(0, index1);
            Assert.AreEqual(10, map[1]);
        }

        [Test]
        public void ContainsKey_ReflectsContent()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);

            Assert.IsTrue(map.ContainsKey(1));
            Assert.IsFalse(map.ContainsKey(2));
        }

        [Test]
        public void TryGetValue_ReturnsExpected()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            Assert.IsTrue(map.TryGetValue(1, out var value1));
            Assert.AreEqual(10, value1);

            Assert.IsTrue(map.TryGetValue(2, out var value2));
            Assert.AreEqual(20, value2);

            Assert.IsFalse(map.TryGetValue(3, out var missing));
            Assert.AreEqual(0, missing);
        }

        [Test]
        public void FindIndex_ReturnsMinusOneWhenMissing()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);

            Assert.AreEqual(0, map.FindIndex(1));
            Assert.AreEqual(-1, map.FindIndex(2));
        }

        [Test]
        public void GetOrAdd_AddsWhenMissing()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);

            ref var value = ref map.GetOrAdd(1);
            Assert.AreEqual(0, value);
            value = 42;

            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(42, map[1]);
        }

        [Test]
        public void GetOrAdd_ReturnsExistingByRef()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);

            ref var value = ref map.GetOrAdd(1);
            Assert.AreEqual(10, value);
            value = 20;

            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(20, map[1]);
        }

        [Test]
        public void GetOrAdd_OutIndex_ReportsIndex()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);

            map.GetOrAdd(1, out var index0) = 5;
            Assert.AreEqual(0, index0);

            map.GetOrAdd(2, out var index1) = 6;
            Assert.AreEqual(1, index1);

            map.GetOrAdd(1, out var indexExisting);
            Assert.AreEqual(0, indexExisting);
        }

        [Test]
        public void GetValueByRef_ReturnsRef()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);

            ref var value = ref map.GetValueByRef(1);
            value = 99;

            Assert.AreEqual(99, map[1]);
        }

        [Test]
        public void Remove_RemovesKey()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);
            map.Add(3, 30);

            Assert.IsTrue(map.Remove(2));

            Assert.AreEqual(2, map.Count);
            Assert.IsFalse(map.ContainsKey(2));
            Assert.AreEqual(10, map[1]);
            Assert.AreEqual(30, map[3]);
        }

        [Test]
        public void Remove_MissingKey_ReturnsFalse()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);

            Assert.IsFalse(map.Remove(99));
            Assert.AreEqual(1, map.Count);
        }

        [Test]
        public void Remove_OutValue_ReportsRemoved()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            Assert.IsTrue(map.Remove(1, out var index, out var value));
            Assert.AreEqual(10, value);
            Assert.GreaterOrEqual(index, 0);
        }

        [Test]
        public void RemoveAndReadd_StaysConsistent()
        {
            using var map = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);

            for (var i = 0; i < 10; i++)
            {
                map.Add(i, i);
            }

            for (var i = 0; i < 10; i += 2)
            {
                Assert.IsTrue(map.Remove(i));
            }

            Assert.AreEqual(5, map.Count);

            for (var i = 0; i < 10; i += 2)
            {
                map.Add(i, i + 100);
            }

            Assert.AreEqual(10, map.Count);

            for (var i = 0; i < 10; i++)
            {
                var expected = (i % 2 == 0) ? i + 100 : i;
                Assert.AreEqual(expected, map[i]);
            }
        }

        [Test]
        public void Clear_ResetsCount()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            map.Clear();

            Assert.AreEqual(0, map.Count);
            Assert.IsFalse(map.ContainsKey(1));

            map.Add(1, 99);
            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(99, map[1]);
        }

        [Test]
        public void Recycle_ResetsCount()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            map.Recycle();

            Assert.AreEqual(0, map.Count);
            Assert.IsFalse(map.ContainsKey(1));
        }

        [Test]
        public void Grow_PastInitialCapacity_KeepsAllEntries()
        {
            const int N = 200;
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);

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
        public void EnsureCapacity_GrowsCapacity()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            var before = map.Capacity;

            map.EnsureCapacity(before + 100);

            Assert.Greater(map.Capacity, before);
        }

        [Test]
        public void IncreaseCapacityBy_GrowsCapacity()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            var before = map.Capacity;

            map.IncreaseCapacityBy(50);

            Assert.GreaterOrEqual(map.Capacity, before + 50);
        }

        [Test]
        public void Trim_ShrinksCapacityToCount()
        {
            using var map = new ArrayMapUnsafe<int, int>(64, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            map.Trim();

            Assert.AreEqual(2, map.Capacity);
            Assert.AreEqual(10, map[1]);
            Assert.AreEqual(20, map[2]);
        }

        [Test]
        public void Enumerator_VisitsAllPairs()
        {
            using var map = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);

            for (var i = 0; i < 5; i++)
            {
                map.Add(i, i * 10);
            }

            var seen = new Dictionary<int, int>();

            foreach (var pair in map)
            {
                seen[pair.Key] = pair.Value;
            }

            Assert.AreEqual(5, seen.Count);

            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual(i * 10, seen[i]);
            }
        }

        [Test]
        public void Keys_EnumeratesAllKeys()
        {
            using var map = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);

            for (var i = 0; i < 4; i++)
            {
                map.Add(i, i);
            }

            var keys = new HashSet<int>();

            foreach (var key in map.Keys)
            {
                keys.Add(key);
            }

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, keys);
        }

        [Test]
        public void Values_SliceMatchesCount()
        {
            using var map = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);
            map.Add(3, 30);

            var values = map.Values;

            Assert.AreEqual(3, values.Length);
        }

        [Test]
        public void CopyConstructor_DeepCopiesContent()
        {
            using var source = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);
            source.Add(1, 10);
            source.Add(2, 20);

            using var copy = new ArrayMapUnsafe<int, int>(source, Allocator.Temp);

            Assert.AreEqual(source.Count, copy.Count);
            Assert.AreEqual(10, copy[1]);
            Assert.AreEqual(20, copy[2]);

            copy.Add(3, 30);
            Assert.IsFalse(source.ContainsKey(3));
        }

        [Test]
        public void CopyConstructor_AfterBucketGrowth_AllKeysResolve()
        {
            // Regression for the copy-ctor bucket-capacity fix: collisions grow the source
            // buckets past GetPrime(capacity); the copy must allocate dest buckets from the
            // source bucket count, not GetPrime(capacity), or keys are lost after copy.
            const int N = 500;
            using var source = new ArrayMapUnsafe<int, int>(2, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                source.Add(i, i * 7);
            }

            using var copy = new ArrayMapUnsafe<int, int>(source, Allocator.Temp);

            Assert.AreEqual(N, copy.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(copy.TryGetValue(i, out var value), $"missing key {i} after copy");
                Assert.AreEqual(i * 7, value);
            }
        }

        [Test]
        public void Reinterpret_ReusesUnderlyingValues()
        {
            using var map = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);
            map.Add(1, 0);

            var reinterpreted = map.Reinterpret<uint>();

            Assert.AreEqual(map.Count, reinterpreted.Count);
            Assert.IsTrue(reinterpreted.ContainsKey(1));
        }

        [Test]
        public void Intersect_KeepsOnlySharedKeys()
        {
            using var a = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);
            using var b = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);

            a.Add(1, 1);
            a.Add(2, 2);
            a.Add(3, 3);

            b.Add(2, 0);
            b.Add(3, 0);
            b.Add(4, 0);

            a.Intersect(in b);

            Assert.AreEqual(2, a.Count);
            Assert.IsFalse(a.ContainsKey(1));
            Assert.IsTrue(a.ContainsKey(2));
            Assert.IsTrue(a.ContainsKey(3));
        }

        [Test]
        public void Exclude_RemovesSharedKeys()
        {
            using var a = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);
            using var b = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);

            a.Add(1, 1);
            a.Add(2, 2);
            a.Add(3, 3);

            b.Add(2, 0);
            b.Add(3, 0);

            a.Exclude(in b);

            Assert.AreEqual(1, a.Count);
            Assert.IsTrue(a.ContainsKey(1));
            Assert.IsFalse(a.ContainsKey(2));
            Assert.IsFalse(a.ContainsKey(3));
        }

        [Test]
        public void Union_MergesAndOverwrites()
        {
            using var a = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);
            using var b = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);

            a.Add(1, 1);
            a.Add(2, 2);

            b.Add(2, 200);
            b.Add(3, 300);

            a.Union(in b);

            Assert.AreEqual(3, a.Count);
            Assert.AreEqual(1, a[1]);
            Assert.AreEqual(200, a[2]);
            Assert.AreEqual(300, a[3]);
        }

        [Test]
        public void AsReadOnly_ReflectsContent()
        {
            using var map = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            var readOnly = map.AsReadOnly();

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(2, readOnly.Count);
            Assert.AreEqual(10, readOnly[1]);
            Assert.IsTrue(readOnly.ContainsKey(2));
            Assert.IsTrue(readOnly.TryGetValue(2, out var value));
            Assert.AreEqual(20, value);
            Assert.IsFalse(readOnly.ContainsKey(99));
        }

        [Test]
        public void AsReadOnly_TryGetValue_TypeWrap_ReturnsExpected()
        {
            using var map = new ArrayMapUnsafe<UintId, uint>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            Asserts(map.AsReadOnly());

            static void Asserts(ArrayMapUnsafe<UintId, uint>.ReadOnly readOnly)
            {
                Assert.IsTrue(readOnly.TryGetValue(1, out var value1));
                Assert.AreEqual(10, value1);

                Assert.IsTrue(readOnly.TryGetValue(2, out var value2));
                Assert.AreEqual(20, value2);

                Assert.IsFalse(readOnly.TryGetValue(3, out var missing));
                Assert.AreEqual(0, missing);
            }
        }

        [Test]
        public void AsReadOnly_Enumerator_VisitsAllPairs()
        {
            using var map = new ArrayMapUnsafe<int, int>(8, Allocator.Temp);

            for (var i = 0; i < 4; i++)
            {
                map.Add(i, i * 10);
            }

            var seen = new Dictionary<int, int>();

            foreach (var pair in map.AsReadOnly())
            {
                seen[pair.Key] = pair.Value;
            }

            Assert.AreEqual(4, seen.Count);

            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(i * 10, seen[i]);
            }
        }

        [Test]
        public void ReadOnly_ConstructorAndAllPublicMembersReflectOwner()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            var direct = new ArrayMapUnsafe<int, int>.ReadOnly(map);
            var fromOwner = map.AsReadOnly();

            Assert.IsTrue(direct.IsCreated);
            Assert.AreEqual(map.Capacity, direct.Capacity);
            Assert.AreEqual(2, direct.Count);
            Assert.IsTrue(direct.Keys.IsValid);
            Assert.AreEqual(2, direct.Values.Length);
            Assert.AreEqual(10, direct.Values[0]);
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
            Assert.AreEqual(20, fromOwner[2]);
        }

        [Test]
        public void Enumerators_DirectConstructionMovementResetCurrentAndDispose()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(7, 70);
            var readOnly = map.AsReadOnly();

            var keys = new ArrayMapUnsafe<int, int>.KeyEnumerable(in readOnly);
            Assert.IsTrue(keys.IsValid);

            var keyEnumerator = new ArrayMapUnsafe<int, int>.KeyEnumerator(in readOnly);
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

            var ownerEnumerator = new ArrayMapUnsafeKeyValueEnumerator<int, int>(in map);
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

            var readOnlyEnumerator = new ArrayMapUnsafeReadOnlyKeyValueEnumerator<int, int>(in readOnly);
            Assert.IsTrue(readOnlyEnumerator.IsValid);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            AssertPair(readOnlyEnumerator.Current, 7, 70);
            Assert.IsFalse(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();

            var viewEnumerator = readOnly.GetEnumerator();
            Assert.IsTrue(viewEnumerator.MoveNext());
            viewEnumerator.Dispose();

            Assert.IsFalse(default(ArrayMapUnsafe<int, int>.KeyEnumerable).IsValid);
            Assert.IsFalse(default(ArrayMapUnsafe<int, int>.KeyEnumerator).IsValid);
            Assert.IsFalse(default(ArrayMapUnsafeKeyValueEnumerator<int, int>).IsValid);
            Assert.IsFalse(default(ArrayMapUnsafeReadOnlyKeyValueEnumerator<int, int>).IsValid);
        }

        [Test]
        public void KeyValuePairs_PublicConstructorsPropertiesValidityAndDeconstructReflectBuffer()
        {
            using var buffer = new BufferUnsafe<int>(2, Allocator.Temp);
            buffer[0] = 10;
            buffer[1] = 20;
            var bufferReadOnly = buffer.AsReadOnly();
            var key = 7;
            var pair = new ArrayMapUnsafeKeyValuePair<int, int>(in key, in buffer, 1);
            var readOnlyPair = new ArrayMapUnsafeReadOnlyKeyValuePair<int, int>(
                  in key
                , in bufferReadOnly
                , 0
            );

            AssertPair(pair, 7, 20);
            AssertPair(readOnlyPair, 7, 10);
            Assert.IsFalse(default(ArrayMapUnsafeKeyValuePair<int, int>).IsValid);
            Assert.IsFalse(default(ArrayMapUnsafeReadOnlyKeyValuePair<int, int>).IsValid);
        }

        [Test]
        public void IncreaseCapacityToAndTryFindIndex_GrowAndReportBothPaths()
        {
            using var map = new ArrayMapUnsafe<int, int>(2, Allocator.Temp);
            map.Add(1, 10);
            var requestedCapacity = map.Capacity + 5;

            map.IncreaseCapacityTo(requestedCapacity);

            Assert.GreaterOrEqual(map.Capacity, requestedCapacity);
            Assert.IsTrue(map.TryFindIndex(1, out var index));
            Assert.AreEqual(0, index);
            Assert.IsFalse(map.TryFindIndex(2, out _));
        }

        [Test]
        public void AllocCapacityAndFree_OwnsLiveHeader()
        {
            // SAFETY: The pointer is freed exactly once in finally and is not used after free.
            unsafe
            {
                ArrayMapUnsafe<int, int>* data = null;

                try
                {
                    data = ArrayMapUnsafe<int, int>.Alloc(4, Allocator.Temp);

                    Assert.AreNotEqual(IntPtr.Zero, (IntPtr)data);
                    Assert.IsTrue(data->IsCreated);

                    data->Add(1, 10);

                    Assert.AreEqual(10, (*data)[1]);
                }
                finally
                {
                    if (data != null)
                    {
                        ArrayMapUnsafe<int, int>.Free(data);
                        data = null;
                    }
                }
            }
        }

        [Test]
        public void AllocSourceAndFree_DeepCopiesLiveHeader()
        {
            using var source = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            source.Add(1, 10);

            // SAFETY: The pointer is freed exactly once in finally and is not used after free.
            unsafe
            {
                ArrayMapUnsafe<int, int>* data = null;

                try
                {
                    data = ArrayMapUnsafe<int, int>.Alloc(in source, Allocator.Temp);

                    Assert.AreNotEqual(IntPtr.Zero, (IntPtr)data);
                    Assert.AreEqual(10, (*data)[1]);

                    data->Add(2, 20);

                    Assert.IsFalse(source.ContainsKey(2));
                }
                finally
                {
                    if (data != null)
                    {
                        ArrayMapUnsafe<int, int>.Free(data);
                        data = null;
                    }
                }
            }
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var map = new ArrayMapUnsafe<int, int>(4, Allocator.TempJob);
            map.Add(1, 10);

            var handle = map.Dispose(default);

            Assert.IsFalse(map.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            Assert.IsTrue(map.IsCreated);

            map.Dispose();

            Assert.IsFalse(map.IsCreated);
        }

        [Test]
        public void Collisions_ManyKeys_AllRetrievable()
        {
            const int N = 500;
            using var map = new ArrayMapUnsafe<int, int>(2, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                map.Add(i, i);
            }

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(map.TryGetValue(i, out var value), $"missing key {i}");
                Assert.AreEqual(i, value);
            }
        }

        private static void AssertPair(ArrayMapUnsafeKeyValuePair<int, int> pair, int key, int value)
        {
            Assert.IsTrue(pair.IsValid);
            Assert.AreEqual(key, pair.Key);
            Assert.AreEqual(value, pair.Value);

            pair.Deconstruct(out var actualKey, out var actualValue);

            Assert.AreEqual(key, actualKey);
            Assert.AreEqual(value, actualValue);
        }

        private static void AssertPair(ArrayMapUnsafeReadOnlyKeyValuePair<int, int> pair, int key, int value)
        {
            Assert.IsTrue(pair.IsValid);
            Assert.AreEqual(key, pair.Key);
            Assert.AreEqual(value, pair.Value);

            pair.Deconstruct(out var actualKey, out var actualValue);

            Assert.AreEqual(key, actualKey);
            Assert.AreEqual(value, actualValue);
        }
    }
}
