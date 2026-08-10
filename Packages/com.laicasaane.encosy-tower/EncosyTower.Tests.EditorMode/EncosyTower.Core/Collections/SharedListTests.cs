using System;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeListTests.cs (list subset) against the
    // managed SharedList owner.
    public partial class SharedListTests
    {
        [Test]
        public void Constructor_IsEmpty()
        {
            using var list = new SharedList<int>(8);

            Assert.AreEqual(0, list.Count);
            Assert.GreaterOrEqual(list.Capacity, 8);
        }

        [Test]
        public void ConvenienceConstructors_CopySourcesAndReserveExtraCapacity()
        {
            var array = new[] { 1, 2, 3 };
            var segment = new ArraySegment<int>(new[] { 0, 4, 5, 0 }, 1, 2);
            ReadOnlySpan<int> span = new[] { 6, 7 };
            ICollection<int> collection = new List<int> { 8, 9 };
            using var native = new NativeArray<int>(new[] { 10, 11 }, Allocator.Temp);
            using var sliceOwner = new NativeArray<int>(new[] { 0, 12, 13, 0 }, Allocator.Temp);
            var slice = new NativeSlice<int>(sliceOwner, 1, 2);
            using var empty = new SharedList<int>();
            using var fromArray = new SharedList<int>(array);
            using var fromSegment = new SharedList<int>(in segment);
            using var fromSpan = new SharedList<int>(in span);
            using var fromNative = new SharedList<int>(native);
            using var fromSlice = new SharedList<int>(in slice);
            using var fromCollection = new SharedList<int>(collection);
            using var withExtraCapacity = new SharedList<int>(collection, 5);
            using var fromShared = new SharedList<int>(fromArray);

            Assert.AreEqual(0, empty.Count);
            CollectionAssert.AreEqual(array, fromArray.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 5 }, fromSegment.ToArray());
            CollectionAssert.AreEqual(new[] { 6, 7 }, fromSpan.ToArray());
            CollectionAssert.AreEqual(new[] { 10, 11 }, fromNative.ToArray());
            CollectionAssert.AreEqual(new[] { 12, 13 }, fromSlice.ToArray());
            CollectionAssert.AreEqual(new[] { 8, 9 }, fromCollection.ToArray());
            CollectionAssert.AreEqual(array, fromShared.ToArray());
            Assert.GreaterOrEqual(withExtraCapacity.Capacity, collection.Count + 5);
        }

        [Test]
        public void GenericOwnerConstructors_CopyManagedAndNativeSources()
        {
            var array = new[] { 1, 2 };
            var segment = new ArraySegment<int>(new[] { 0, 3, 4 }, 1, 2);
            ReadOnlySpan<int> span = new[] { 5, 6 };
            ICollection<int> collection = new List<int> { 7, 8 };
            using var native = new NativeArray<uint>(new uint[] { 9, 10 }, Allocator.Temp);
            using var sliceOwner = new NativeArray<uint>(new uint[] { 0, 11, 12 }, Allocator.Temp);
            var slice = new NativeSlice<uint>(sliceOwner, 1, 2);
            using var empty = new SharedList<int, uint>();
            using var capacity = new SharedList<int, uint>(4);
            using var fromArray = new SharedList<int, uint>(array);
            using var fromSegment = new SharedList<int, uint>(in segment);
            using var fromSpan = new SharedList<int, uint>(in span);
            using var fromCollection = new SharedList<int, uint>(collection);
            using var withExtraCapacity = new SharedList<int, uint>(collection, 3);
            using var fromNative = new SharedList<int, uint>(native);
            using var fromSlice = new SharedList<int, uint>(in slice);

            Assert.AreEqual(0, empty.Count);
            Assert.GreaterOrEqual(capacity.Capacity, 4);
            CollectionAssert.AreEqual(array, fromArray.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4 }, fromSegment.ToArray());
            CollectionAssert.AreEqual(new[] { 5, 6 }, fromSpan.ToArray());
            CollectionAssert.AreEqual(new[] { 7, 8 }, fromCollection.ToArray());
            Assert.GreaterOrEqual(withExtraCapacity.Capacity, collection.Count + 3);
            CollectionAssert.AreEqual(new[] { 9, 10 }, fromNative.ToArray());
            CollectionAssert.AreEqual(new[] { 11, 12 }, fromSlice.ToArray());
        }

        [Test]
        public void Add_StoresValues()
        {
            using var list = new SharedList<int>(8);

            list.Add(10);
            list.Add(20);
            list.Add(30);

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(10, list[0]);
            Assert.AreEqual(20, list[1]);
            Assert.AreEqual(30, list[2]);
        }

        [Test]
        public void ValueAndInOverloads_AddInsertAndPushExpectedValues()
        {
            using var list = new SharedList<int, int>(4);
            var two = 2;
            var four = 4;
            var six = 6;

            list.Add(1);
            list.Add(in two);
            list.Insert(2, 3);
            list.Insert(3, in four);

            Assert.AreEqual(4, list.Push(5));
            Assert.AreEqual(5, list.Push(in six));
            Assert.IsFalse(list.IsReadOnly);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, list.ToArray());
        }

        [Test]
        public void AddRange_AllOverloadsAppendRequestedValues()
        {
            using var list = new SharedList<int, int>(2);
            ReadOnlySpan<int> fullSpan = new[] { 5, 6 };
            ReadOnlySpan<int> partialSpan = new[] { 7, 8, 99 };
            IEnumerable<int> collection = new List<int> { 9, 10 };
            var iterator = YieldValues();

            list.AddRange(new[] { 1, 2 });
            list.AddRange(new[] { 3, 4, 99 }, 2);
            list.AddRange(fullSpan);
            list.AddRange(partialSpan, 2);
            list.AddRange(collection);
            list.AddRange(iterator);

            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 },
                list.ToArray()
            );
        }

        [Test]
        public void Add_GrowsPastInitialCapacity()
        {
            const int N = 200;
            using var list = new SharedList<int>(4);

            for (var i = 0; i < N; i++)
            {
                list.Add(i * 2);
            }

            Assert.AreEqual(N, list.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.AreEqual(i * 2, list[i], $"index {i}");
            }
        }

        [Test]
        public void Indexer_Set_Overwrites()
        {
            using var list = new SharedList<int>(4);
            list.Add(1);
            list.Add(2);

            list[1] = 99;

            Assert.AreEqual(99, list[1]);
        }

        [Test]
        public void Insert_ShiftsRight()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);
            list.Add(3);

            list.Insert(1, 2);

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void RemoveAt_ShiftsLeft()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.RemoveAt(1);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(3, list[1]);
        }

        [Test]
        public void Remove_RemovesFirstMatch()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.IsTrue(list.Remove(2));
            Assert.IsFalse(list.Remove(99));

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(3, list[1]);
        }

        [Test]
        public void IndexOf_FindsValue()
        {
            using var list = new SharedList<int>(8);
            list.Add(10);
            list.Add(20);
            list.Add(30);

            Assert.AreEqual(1, list.IndexOf(20));
            Assert.AreEqual(-1, list.IndexOf(99));
        }

        [Test]
        public void SearchAndRemovalOverloads_FindAndRemoveExpectedValues()
        {
            using var list = new SharedList<int, int>(new[] { 1, 2, 3, 2, 4 });
            var two = 2;
            var missing = 9;

            Assert.IsTrue(list.Contains(2));
            Assert.IsFalse(list.Contains(missing));
            Assert.AreEqual(3, list.IndexOf(2, 2));
            Assert.AreEqual(3, list.IndexOf(2, 2, 2));
            Assert.AreEqual(1, list.IndexOf(in two));
            Assert.AreEqual(3, list.IndexOf(in two, 2));
            Assert.AreEqual(3, list.IndexOf(in two, 2, 2));
            Assert.AreEqual(-1, list.IndexOf(in missing));
            Assert.IsTrue(list.Remove(in two));

            list.RemoveRange(1, 2);
            list.AddRange(new[] { 5, 6 });
            list.RemoveAtSwapBack(1);

            CollectionAssert.AreEqual(new[] { 1, 6, 5 }, list.ToArray());
        }

        [Test]
        public void ElementAt_ReturnsRef()
        {
            using var list = new SharedList<int>(8);
            list.Add(5);

            ref var slot = ref list.ElementAt(0);
            slot = 55;

            Assert.AreEqual(55, list[0]);
        }

        [Test]
        public void CopyFromAndTryCopyFrom_AllOverloadsCopyOrReturnFalse()
        {
            using var list = SharedList<int, int>.Prefill(0, 6);

            list.CopyFrom(new[] { 1, 2 });
            list.CopyFrom(new[] { 3, 4, 99 }, 2);
            list.CopyFrom(2, new[] { 5, 6 });
            list.CopyFrom(4, new[] { 7, 8, 99 }, 2);

            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7, 8 }, list.ToArray());

            Assert.IsTrue(list.TryCopyFrom(new[] { 9, 10 }));
            Assert.IsTrue(list.TryCopyFrom(new[] { 11, 12, 99 }, 2));
            Assert.IsTrue(list.TryCopyFrom(2, new[] { 13, 14 }));
            Assert.IsTrue(list.TryCopyFrom(4, new[] { 15, 16, 99 }, 2));
            Assert.IsFalse(list.TryCopyFrom(5, new[] { 17, 18 }));

            CollectionAssert.AreEqual(new[] { 11, 12, 13, 14, 15, 16 }, list.ToArray());
        }

        [Test]
        public void CopyToAndTryCopyTo_AllOverloadsCopyOrReturnFalse()
        {
            using var list = new SharedList<int, int>(new[] { 1, 2, 3, 4 });
            var array = new int[6];
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            list.CopyTo(array, 1);
            list.CopyTo(full);
            list.CopyTo(partial.AsSpan(), 2);
            list.CopyTo(1, offset);
            list.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, array);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);

            Assert.IsTrue(list.TryCopyTo(full));
            Assert.IsTrue(list.TryCopyTo(partial, 2));
            Assert.IsTrue(list.TryCopyTo(1, offset));
            Assert.IsTrue(list.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(list.TryCopyTo(3, partial));
        }

        [Test]
        public void Clear_ResetsCount()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);
            list.Add(2);

            list.Clear();

            Assert.AreEqual(0, list.Count);

            list.Add(7);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(7, list[0]);
        }

        [Test]
        public void AsSpan_ReflectsContent()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);
            list.Add(2);
            list.Add(3);

            var span = list.AsSpan();

            Assert.AreEqual(3, span.Length);
            Assert.AreEqual(1, span[0]);
            Assert.AreEqual(3, span[2]);
        }

        [Test]
        public void CapacityStackReplicateViewsAndFactoriesExposeExpectedValues()
        {
            using var list = new SharedList<int, int>(4);
            list.AddRange(new[] { 1, 2 });
            var initialCapacity = list.Capacity;

            list.IncreaseCapacityBy(3);
            Assert.GreaterOrEqual(list.Capacity, initialCapacity + 3);

            var increasedCapacity = list.Capacity;
            list.IncreaseCapacityTo(increasedCapacity + 3);
            Assert.GreaterOrEqual(list.Capacity, increasedCapacity + 3);

            var defaults = list.AddReplicate(2);
            var values = list.AddReplicate(7, 2);
            var noInit = list.AddReplicateNoInit(1);
            noInit[0] = 8;

            CollectionAssert.AreEqual(new[] { 0, 0 }, defaults.ToArray());
            CollectionAssert.AreEqual(new[] { 7, 7 }, values.ToArray());
            Assert.AreEqual(8, list.Peek());
            Assert.AreEqual(8, list.Pop());
            Assert.AreEqual(6, list.Count);

            var readOnlySpan = list.AsReadOnlySpan();
            var writableSpan = list.AsSpan();
            writableSpan[0] = 10;

            Assert.AreEqual(10, readOnlySpan[0]);

            list.Trim();

            Assert.AreEqual(list.Count, list.Capacity);

            using var defaultsList = SharedList<int, int>.Prefill(2);
            using var valuesList = SharedList<int, int>.Prefill(9, 2);

            CollectionAssert.AreEqual(new[] { 0, 0 }, defaultsList.ToArray());
            CollectionAssert.AreEqual(new[] { 9, 9 }, valuesList.ToArray());
        }

        [Test]
        public void ReadOnly_ConstructorConversionsCopiesViewsAndEnumeratorsExposeOwnerValues()
        {
            using var list = new SharedList<int, int>(new[] { 1, 2, 3, 4 });
            var direct = new SharedList<int, int>.ReadOnly(list);
            SharedList<int, int>.ReadOnly implicitReadOnly = list;
            ReadOnlySpan<int> implicitSpan = implicitReadOnly;
            var empty = SharedList<int, int>.ReadOnly.Empty;
            var array = new int[6];
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            Assert.IsTrue(direct.IsCreated);
            Assert.AreEqual(4, direct.Count);
            Assert.GreaterOrEqual(direct.Capacity, 4);
            Assert.IsTrue(direct.IsReadOnly);
            Assert.AreEqual(3, direct[2]);
            Assert.AreEqual(0, empty.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, implicitSpan.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, direct.AsReadOnlySpan().ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, direct.ToArray());

            direct.CopyTo(array, 1);
            direct.CopyTo(full);
            direct.CopyTo(partial.AsSpan(), 2);
            direct.CopyTo(1, offset);
            direct.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, array);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);

            Assert.IsTrue(direct.TryCopyTo(full));
            Assert.IsTrue(direct.TryCopyTo(partial, 2));
            Assert.IsTrue(direct.TryCopyTo(1, offset));
            Assert.IsTrue(direct.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(direct.TryCopyTo(3, partial));

            var reinterpreted = direct.Reinterpret<uint>();
            var nativeReadOnly = direct.AsNative();

            Assert.AreEqual(1u, reinterpreted[0]);
            Assert.AreEqual(4, nativeReadOnly.Count);

            var ownerEnumerator = list.GetEnumerator();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(1, ownerEnumerator.Current);
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            ownerEnumerator.Dispose();

            var readOnlyEnumerator = direct.GetEnumerator();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(1, readOnlyEnumerator.Current);
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();

            var directEnumerator = new SharedList<int, int>.Enumerator(direct);
            Assert.IsTrue(directEnumerator.MoveNext());
            directEnumerator.Dispose();
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Dispose_InvalidatesExistingReadOnlyAndNativeViews()
        {
            var list = new SharedList<int, int>(new[] { 1, 2 });
            var readOnly = list.AsReadOnly();
            SharedListNative<int> native = list;

            list.Dispose();

            Assert.IsTrue(readOnly.IsCreated);
            Assert.Catch(() => _ = readOnly.Count);
            Assert.Catch(() => _ = native.Count);
            Assert.DoesNotThrow(() => list.Dispose());
        }

        [Test]
        public void RemoveRange_ZeroLengthBoundaries_PinContract()
        {
            // Pins the family-wide contract: RemoveRange validates startIndex < Count before
            // looking at length, so a zero-length remove at Count (including on an empty list)
            // throws, unlike BCL List<T>.RemoveRange. Zero-length removes at a valid index
            // are no-ops.
            using var empty = new SharedList<int>(4);

            Assert.Throws<InvalidOperationException>(() => empty.RemoveRange(0, 0));

            using var list = new SharedList<int>(new[] { 1, 2, 3 });

            Assert.Throws<InvalidOperationException>(() => list.RemoveRange(3, 0));

            list.RemoveRange(0, 0);
            list.RemoveRange(2, 0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
        }

        private static IEnumerable<int> YieldValues()
        {
            yield return 11;
            yield return 12;
        }
    }
}
