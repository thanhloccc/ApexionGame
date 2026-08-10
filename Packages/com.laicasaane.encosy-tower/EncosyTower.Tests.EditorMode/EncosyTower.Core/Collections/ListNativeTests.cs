using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListNativeTests
    {
        [Test]
        public void Constructor_CreatesEmptyListAndSupportsZeroCapacity()
        {
            using var list = new ListNative<int>(4, Allocator.Temp);
            using var zeroCapacity = new ListNative<int>(0, Allocator.Temp);

            Assert.IsTrue(list.IsCreated);
            Assert.AreEqual(0, list.Count);
            Assert.GreaterOrEqual(list.Capacity, 4);

            zeroCapacity.Add(7);

            Assert.AreEqual(1, zeroCapacity.Count);
            Assert.AreEqual(7, zeroCapacity[0]);
        }

        [Test]
        public void SpanConstructor_CopiesSource()
        {
            var source = new[] { 1, 2, 3 };
            using var list = new ListNative<int>(source, Allocator.Temp);

            CollectionAssert.AreEqual(source, list.ToArray());
        }

        [Test]
        public void Add_GrowsPastInitialCapacity()
        {
            const int N = 200;
            using var list = new ListNative<int>(2, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                list.Add(i * 2);
            }

            Assert.AreEqual(N, list.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.AreEqual(i * 2, list[i]);
            }
        }

        [Test]
        public void ValueAndInOverloads_AddInsertAndPushExpectedValues()
        {
            using var list = new ListNative<int>(4, Allocator.Temp);
            var two = 2;
            var three = 3;
            var four = 4;

            list.Add(1);
            list.Add(in two);
            list.Insert(2, three);
            list.Insert(3, in four);

            var five = 5;
            Assert.AreEqual(4, list.Push(in five));

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list.ToArray());
        }

        [Test]
        public void InsertAndRemoveFamilies_PreserveExpectedOrder()
        {
            using var list = new ListNative<int>(8, Allocator.Temp);
            list.AddRange(new[] { 1, 3, 4, 5, 6 });

            list.Insert(1, 2);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, list.ToArray());

            list.RemoveAt(2);
            CollectionAssert.AreEqual(new[] { 1, 2, 4, 5, 6 }, list.ToArray());

            list.RemoveRange(1, 2);
            CollectionAssert.AreEqual(new[] { 1, 5, 6 }, list.ToArray());

            list.RemoveAtSwapBack(0);
            CollectionAssert.AreEqual(new[] { 6, 5 }, list.ToArray());
        }

        [Test]
        public void IndexerAndElementAt_MutateValues()
        {
            var list = new ListNative<int>(4, Allocator.Temp);

            try
            {
                list.AddRange(new[] { 1, 2 });
                list[0] = 10;
                ref var value = ref list.ElementAt(1);
                value = 20;

                CollectionAssert.AreEqual(new[] { 10, 20 }, list.ToArray());
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        public void AddRange_FamiliesAppendRequestedItems()
        {
            using var list = new ListNative<int>(2, Allocator.Temp);
            using var native = new NativeArray<int>(new[] { 5, 6, 7 }, Allocator.Temp);
            var slice = new NativeSlice<int>(native, 1, 2);

            list.AddRange(new[] { 1, 2 });
            list.AddRange(new[] { 3, 4, 99 }, 2);
            list.AddRange(native);
            list.AddRange(native, 2);
            list.AddRange(in slice);
            list.AddRange(in slice, 1);

            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4, 5, 6, 7, 5, 6, 6, 7, 6 },
                list.ToArray()
            );
        }

        [Test]
        public void AddReplicate_FamiliesReturnNewRanges()
        {
            using var list = new ListNative<int>(8, Allocator.Temp);

            var defaults = list.AddReplicate(2);
            var values = list.AddReplicate(7, 3);
            var noInit = list.AddReplicateNoInit(2);
            noInit[0] = 8;
            noInit[1] = 9;

            CollectionAssert.AreEqual(new[] { 0, 0 }, defaults.ToArray());
            CollectionAssert.AreEqual(new[] { 7, 7, 7 }, values.ToArray());
            CollectionAssert.AreEqual(new[] { 0, 0, 7, 7, 7, 8, 9 }, list.ToArray());
        }

        [Test]
        public void CopyFromAndTryCopyFrom_SpanFamiliesCopyOrReturnFalse()
        {
            using var list = ListNative<int>.Prefill(0, 6, Allocator.Temp);

            list.CopyFrom(new[] { 1, 2, 3 });
            list.CopyFrom(new[] { 4, 5, 99 }, 2);
            list.CopyFrom(2, new[] { 6, 7 });
            list.CopyFrom(4, new[] { 8, 9, 99 }, 2);

            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8, 9 }, list.ToArray());

            Assert.IsTrue(list.TryCopyFrom(new[] { 10, 11 }));
            Assert.IsTrue(list.TryCopyFrom(new[] { 12, 13, 99 }, 2));
            Assert.IsTrue(list.TryCopyFrom(2, new[] { 14, 15 }));
            Assert.IsTrue(list.TryCopyFrom(4, new[] { 16, 17, 99 }, 2));
            Assert.IsFalse(list.TryCopyFrom(5, new[] { 18, 19 }));

            CollectionAssert.AreEqual(new[] { 12, 13, 14, 15, 16, 17 }, list.ToArray());
        }

        [Test]
        public void CopyToAndTryCopyTo_SpanFamiliesCopyOrReturnFalse()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];
            var array = new int[6];

            list.CopyTo(full);
            list.CopyTo(partial.AsSpan(), 2);
            list.CopyTo(1, offset);
            list.CopyTo(1, explicitLength, 2);
            list.CopyTo(array, 1);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, array);

            Assert.IsTrue(list.TryCopyTo(full));
            Assert.IsTrue(list.TryCopyTo(partial, 2));
            Assert.IsTrue(list.TryCopyTo(1, offset));
            Assert.IsTrue(list.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(list.TryCopyTo(3, partial));
        }

        [Test]
        public void CopyTo_NativeFamiliesCopyRequestedRanges()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            using var native = new NativeArray<int>(3, Allocator.Temp);
            using var sliceOwner = new NativeArray<int>(4, Allocator.Temp);
            var slice = new NativeSlice<int>(sliceOwner, 1, 2);

            list.CopyTo(1, native);
            list.CopyTo(2, in slice, 2);

            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, native.ToArray());
            CollectionAssert.AreEqual(new[] { 0, 3, 4, 0 }, sliceOwner.ToArray());
        }

        [Test]
        public void CopyTo_RemainingNativeOverloadsCopyRequestedRanges()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            using var full = new NativeArray<int>(4, Allocator.Temp);
            using var partial = new NativeArray<int>(3, Allocator.Temp);
            using var sliceOwner = new NativeArray<int>(4, Allocator.Temp);
            var slice = new NativeSlice<int>(sliceOwner, 1, 3);

            list.CopyTo(full);
            list.CopyTo(1, partial, 2);
            list.CopyTo(1, in slice);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, partial.ToArray());
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 4 }, sliceOwner.ToArray());
        }

        [Test]
        public void PeekPopAndPush_UseListTail()
        {
            using var list = new ListNative<int>(2, Allocator.Temp);

            Assert.AreEqual(0, list.Push(10));
            Assert.AreEqual(1, list.Push(20));
            Assert.AreEqual(20, list.Peek());
            Assert.AreEqual(20, list.Pop());
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(10, list.Peek());
        }

        [Test]
        public void ClearAndFastClear_ResetCountAndAllowReuse()
        {
            using var list = new ListNative<int>(4, Allocator.Temp);
            list.AddRange(new[] { 1, 2, 3 });

            list.Clear();

            Assert.AreEqual(0, list.Count);
            list.Add(4);

            list.FastClear();

            Assert.AreEqual(0, list.Count);
            list.Add(5);
            Assert.AreEqual(5, list[0]);
        }

        [Test]
        public void CapacityOperations_GrowAndTrimToCount()
        {
            using var list = new ListNative<int>(4, Allocator.Temp);
            list.AddRange(new[] { 1, 2 });
            var initialCapacity = list.Capacity;

            list.IncreaseCapacityBy(5);
            Assert.GreaterOrEqual(list.Capacity, initialCapacity + 5);

            var grownCapacity = list.Capacity;
            list.IncreaseCapacityTo(grownCapacity + 5);
            Assert.GreaterOrEqual(list.Capacity, grownCapacity + 5);

            list.Trim();

            Assert.AreEqual(list.Count, list.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2 }, list.ToArray());
        }

        [Test]
        public void ArraysAndViews_ExposeEquivalentAliasedContent()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            var span = list.AsSpan();
            var readOnlySpan = list.AsReadOnlySpan();
            var slice = list.AsNativeSlice();

            span[0] = 10;
            slice[2] = 30;

            CollectionAssert.AreEqual(new[] { 10, 2, 30 }, list.ToArray());
            Assert.AreEqual(10, readOnlySpan[0]);
            Assert.AreEqual(30, readOnlySpan[2]);
        }

        [Test]
        public void Reinterpret_ExposesSameSizedValues()
        {
            using var list = new ListNative<int>(new[] { 1, 2 }, Allocator.Temp);
            var reinterpreted = list.Reinterpret<uint>();

            Assert.AreEqual(list.Count, reinterpreted.Count);
            Assert.AreEqual(1u, reinterpreted[0]);
            Assert.AreEqual(2u, reinterpreted[1]);
        }

        [Test]
        public void AsReadOnly_ReflectsOwnerContent()
        {
            using var list = new ListNative<int>(new[] { 1, 2 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();

            list.Add(3);

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(3, readOnly.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, readOnly.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, readOnly.AsReadOnlySpan().ToArray());
        }

        [Test]
        public void ReadOnly_PropertiesConversionAndCopyOverloadsExposeOwnerValues()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            ListNative<int>.ReadOnly readOnly = list;
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Count);
            Assert.GreaterOrEqual(readOnly.Capacity, 4);
            Assert.AreEqual(3, readOnly[2]);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, readOnly.AsReadOnlySpan().ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, readOnly.ToArray());

            readOnly.CopyTo(full);
            readOnly.CopyTo(partial.AsSpan(), 2);
            readOnly.CopyTo(1, offset);
            readOnly.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);

            Assert.IsTrue(readOnly.TryCopyTo(full));
            Assert.IsTrue(readOnly.TryCopyTo(partial, 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, offset));
            Assert.IsTrue(readOnly.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(readOnly.TryCopyTo(3, partial));
        }

        [Test]
        public void Enumerator_VisitsValuesInOrder()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var visited = new int[list.Count];
            var index = 0;

            foreach (var value in list)
            {
                visited[index++] = value;
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, visited);
        }

        [Test]
        public void OwnerAndReadOnlyEnumerators_MoveResetAndDisposeDirectly()
        {
            using var list = new ListNative<int>(new[] { 1, 2 }, Allocator.Temp);
            var ownerEnumerator = list.GetEnumerator();

            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(1, ownerEnumerator.Current);
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(1, ownerEnumerator.Current);
            ownerEnumerator.Dispose();

            var readOnlyEnumerator = list.AsReadOnly().GetEnumerator();

            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(1, readOnlyEnumerator.Current);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(2, readOnlyEnumerator.Current);
            Assert.IsFalse(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();
        }

        [Test]
        public void Enumerator_MutationDuringIteration_Throws()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var enumerator = list.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            list.Add(4);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var list = new ListNative<int>(4, Allocator.Temp);
            Assert.IsTrue(list.IsCreated);

            list.Dispose();

            Assert.IsFalse(list.IsCreated);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void UseAfterDispose_Throws()
        {
            var list = new ListNative<int>(4, Allocator.Temp);
            list.Add(1);
            list.Dispose();

            Assert.Catch(() => _ = list.Count);
        }

        [Test]
        public void DoubleDispose_DoesNotThrow()
        {
            var list = new ListNative<int>(4, Allocator.Temp);
            list.Dispose();

            Assert.DoesNotThrow(() => list.Dispose());
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var list = new ListNative<int>(4, Allocator.TempJob);
            list.Add(1);

            var handle = list.Dispose(default);

            Assert.IsFalse(list.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        public void Prefill_FactoriesCreateExpectedContent()
        {
            using var defaults = ListNative<int>.Prefill(3, Allocator.Temp);
            using var values = ListNative<int>.Prefill(7, 3, Allocator.Temp);

            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, defaults.ToArray());
            CollectionAssert.AreEqual(new[] { 7, 7, 7 }, values.ToArray());
        }

        [Test]
        public void RemoveRange_ZeroLengthBoundaries_PinContract()
        {
            // Pins the family-wide contract: RemoveRange validates startIndex < Count before
            // looking at length, so a zero-length remove at Count (including on an empty list)
            // throws, unlike BCL List<T>.RemoveRange. Zero-length removes at a valid index
            // are no-ops.
            using var empty = new ListNative<int>(0, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => empty.RemoveRange(0, 0));

            using var list = new ListNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => list.RemoveRange(3, 0));

            list.RemoveRange(0, 0);
            list.RemoveRange(2, 0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
        }
    }
}
