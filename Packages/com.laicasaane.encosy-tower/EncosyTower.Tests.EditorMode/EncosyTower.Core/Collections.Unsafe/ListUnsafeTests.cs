using System;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public partial class ListUnsafeTests
    {
        [Test]
        public void CapacityConstructor_CreatesEmptyListAndSupportsZeroCapacity()
        {
            using var list = new ListUnsafe<int>(4, new AllocatorStrategy(Allocator.Temp));
            using var zeroCapacity = new ListUnsafe<int>(0, Allocator.Temp);

            Assert.IsTrue(list.IsCreated);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(4, list.Capacity);
            Assert.IsTrue(zeroCapacity.IsCreated);
            Assert.AreEqual(0, zeroCapacity.Count);
            Assert.AreEqual(0, zeroCapacity.Capacity);

            zeroCapacity.Add(7);

            Assert.IsTrue(zeroCapacity.IsCreated);
            Assert.AreEqual(1, zeroCapacity.Count);
            Assert.AreEqual(7, zeroCapacity[0]);
        }

        [Test]
        public void SpanConstructor_CopiesSource()
        {
            var source = new[] { 1, 2, 3 };
            using var list = new ListUnsafe<int>(source, Allocator.Temp);

            Assert.IsTrue(list.IsCreated);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list.Capacity);
            CollectionAssert.AreEqual(source, list.ToArray());
        }

        [Test]
        public void Add_OverloadsGrowAndPreserveOrder()
        {
            using var list = new ListUnsafe<int>(1, Allocator.Temp);
            var second = 2;

            list.Add(1);
            list.Add(in second);

            for (var i = 3; i <= 20; i++)
            {
                list.Add(i);
            }

            Assert.AreEqual(20, list.Count);
            Assert.GreaterOrEqual(list.Capacity, 20);

            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(i + 1, list[i]);
            }
        }

        [Test]
        public void Insert_OverloadsSupportFirstMiddleAndLastPositions()
        {
            using var list = new ListUnsafe<int>(2, Allocator.Temp);
            var middle = 2;

            list.Insert(0, 1);
            list.Insert(1, 3);
            list.Insert(1, in middle);
            list.Insert(list.Count, 4);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list.ToArray());
        }

        [Test]
        public void AddRange_OverloadsAppendRequestedItems()
        {
            using var list = new ListUnsafe<int>(1, Allocator.Temp);

            list.AddRange(new[] { 1, 2 });
            list.AddRange(new[] { 3, 4, 99 }, 2);
            list.AddRange(ReadOnlySpan<int>.Empty);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list.ToArray());
        }

        [Test]
        public void IndexerAndElementAt_ReturnWritableReferences()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2 }, Allocator.Temp);

            ref var first = ref list[0];
            ref var second = ref list.ElementAt(1);
            first = 10;
            second = 20;

            CollectionAssert.AreEqual(new[] { 10, 20 }, list.ToArray());
        }

        [Test]
        public void CopyFromAndTryCopyFrom_AllOverloadsCopyOrReturnFalse()
        {
            using var list = new ListUnsafe<int>(new[] { 0, 0, 0, 0, 0, 0 }, Allocator.Temp);

            list.CopyFrom(new[] { 1, 2, 3 });
            list.CopyFrom(new[] { 4, 5, 99 }, 2);
            list.CopyFrom(2, new[] { 6, 7 });
            list.CopyFrom(4, new[] { 8, 9, 99 }, 2);

            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8, 9 }, list.ToArray());
            Assert.IsTrue(list.TryCopyFrom(new[] { 10, 11 }));
            Assert.IsTrue(list.TryCopyFrom(new[] { 12, 13, 99 }, 2));
            Assert.IsTrue(list.TryCopyFrom(2, new[] { 14, 15 }));
            Assert.IsTrue(list.TryCopyFrom(4, new[] { 16, 17, 99 }, 2));

            var expected = new[] { 12, 13, 14, 15, 16, 17 };
            CollectionAssert.AreEqual(expected, list.ToArray());
            Assert.IsFalse(list.TryCopyFrom(5, new[] { 18, 19 }));
            Assert.IsFalse(list.TryCopyFrom(5, new[] { 18, 19 }, 2));
            CollectionAssert.AreEqual(expected, list.ToArray());
        }

        [Test]
        public void CopyToAndTryCopyTo_AllOverloadsCopyOrReturnFalse()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var array = new int[6];
            var full = new int[4];
            var partial = new int[3];
            var offset = new int[2];
            var range = new int[3];

            list.CopyTo(array, 1);
            list.CopyTo(full);
            list.CopyTo(partial.AsSpan(), 2);
            list.CopyTo(1, offset);
            list.CopyTo(1, range, 2);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, array);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, range);
            Assert.IsTrue(list.TryCopyTo(new int[4]));
            Assert.IsTrue(list.TryCopyTo(new int[3], 2));
            Assert.IsTrue(list.TryCopyTo(1, new int[3]));
            Assert.IsTrue(list.TryCopyTo(1, new int[3], 2));

            var failedDestination = new[] { 9, 9 };
            Assert.IsFalse(list.TryCopyTo(3, failedDestination));
            Assert.IsFalse(list.TryCopyTo(3, failedDestination, 2));
            CollectionAssert.AreEqual(new[] { 9, 9 }, failedDestination);
        }

        [Test]
        public void ClearAndFastClear_ResetCountAndAllowReuse()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            list.Clear();

            Assert.AreEqual(0, list.Count);
            list.Add(4);
            Assert.AreEqual(4, list[0]);

            list.FastClear();

            Assert.AreEqual(0, list.Count);
            list.Add(5);
            Assert.AreEqual(5, list[0]);
        }

        [Test]
        public void RemoveFamilies_HandleBoundariesAndSwapBackOrder()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3, 4, 5, 6 }, Allocator.Temp);

            list.RemoveAt(0);
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6 }, list.ToArray());

            list.RemoveAt(list.Count - 1);
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, list.ToArray());

            list.RemoveRange(1, 2);
            CollectionAssert.AreEqual(new[] { 2, 5 }, list.ToArray());

            list.RemoveRange(1, 0);
            CollectionAssert.AreEqual(new[] { 2, 5 }, list.ToArray());

            list.RemoveAtSwapBack(0);
            CollectionAssert.AreEqual(new[] { 5 }, list.ToArray());
        }

        [Test]
        public void PeekPopAndPush_UseListTail()
        {
            using var list = new ListUnsafe<int>(1, Allocator.Temp);
            var second = 20;

            Assert.AreEqual(0, list.Push(10));
            Assert.AreEqual(1, list.Push(in second));
            Assert.AreEqual(20, list.Peek());
            Assert.AreEqual(20, list.Pop());
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(10, list.Peek());
        }

        [Test]
        public void AddReplicateFamilies_ReturnAliasedNewRanges()
        {
            using var list = new ListUnsafe<int>(8, Allocator.Temp);

            var defaults = list.AddReplicate(2);
            var values = list.AddReplicate(7, 3);
            var noInit = list.AddReplicateNoInit(2);
            noInit[0] = 8;
            noInit[1] = 9;

            CollectionAssert.AreEqual(new[] { 0, 0 }, defaults.ToArray());
            CollectionAssert.AreEqual(new[] { 7, 7, 7 }, values.ToArray());
            CollectionAssert.AreEqual(new[] { 8, 9 }, noInit.ToArray());
            CollectionAssert.AreEqual(new[] { 0, 0, 7, 7, 7, 8, 9 }, list.ToArray());

            defaults[0] = 10;
            Assert.AreEqual(10, list[0]);
        }

        [Test]
        public void CapacityOperations_GrowAndTrimWithoutChangingContent()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2 }, Allocator.Temp);
            var initialCapacity = list.Capacity;

            list.IncreaseCapacityBy(5);
            Assert.GreaterOrEqual(list.Capacity, initialCapacity + 5);
            CollectionAssert.AreEqual(new[] { 1, 2 }, list.ToArray());

            var requestedCapacity = list.Capacity + 5;
            list.IncreaseCapacityTo(requestedCapacity);
            Assert.GreaterOrEqual(list.Capacity, requestedCapacity);
            CollectionAssert.AreEqual(new[] { 1, 2 }, list.ToArray());

            list.Trim();

            Assert.AreEqual(list.Count, list.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2 }, list.ToArray());
        }

        [Test]
        public void SpansArraysAndReadOnlyViews_AliasOrCopyAsDocumented()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var span = list.AsSpan();
            var readOnlySpan = list.AsReadOnlySpan();
            var array = list.ToArray();
            var readOnly = list.AsReadOnly();
            ListUnsafe<int>.ReadOnly converted = list;
            ReadOnlySpan<int> convertedSpan = converted;

            span[0] = 10;
            list[1] = 20;
            array[2] = 30;

            Assert.AreEqual(10, list[0]);
            Assert.AreEqual(20, readOnlySpan[1]);
            Assert.AreEqual(10, readOnly[0]);
            Assert.AreEqual(20, converted[1]);
            Assert.AreEqual(3, convertedSpan[2]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void Reinterpret_AliasesSameSizedStorage()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2 }, Allocator.Temp);
            var reinterpreted = list.Reinterpret<uint>();

            Assert.IsTrue(reinterpreted.IsCreated);
            Assert.AreEqual(list.Count, reinterpreted.Count);
            Assert.AreEqual(list.Capacity, reinterpreted.Capacity);
            Assert.AreEqual(1u, reinterpreted[0]);

            reinterpreted[1] = 20u;

            Assert.AreEqual(20, list[1]);
        }

        [Test]
        public void ReadOnly_AllPropertiesCopyMethodsViewsAndConversionsWork()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();
            ListUnsafe<int>.ReadOnly converted = list;
            ReadOnlySpan<int> convertedSpan = readOnly;
            var full = new int[4];
            var partial = new int[3];
            var offset = new int[3];
            var range = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Count);
            Assert.AreEqual(4, readOnly.Capacity);
            Assert.AreEqual(2, readOnly[1]);
            Assert.AreEqual(1, converted[0]);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, convertedSpan.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, readOnly.AsReadOnlySpan().ToArray());

            readOnly.CopyTo(full);
            readOnly.CopyTo(partial, 2);
            readOnly.CopyTo(1, offset);
            readOnly.CopyTo(1, range, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, range);
            Assert.IsTrue(readOnly.TryCopyTo(new int[4]));
            Assert.IsTrue(readOnly.TryCopyTo(new int[3], 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[3]));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[3], 2));

            var failedDestination = new[] { 9, 9 };
            Assert.IsFalse(readOnly.TryCopyTo(3, failedDestination));
            Assert.IsFalse(readOnly.TryCopyTo(3, failedDestination, 2));
            CollectionAssert.AreEqual(new[] { 9, 9 }, failedDestination);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, readOnly.ToArray());
            Assert.AreEqual(4, readOnly.Peek());

            list[0] = 10;
            Assert.AreEqual(10, readOnly[0]);
        }

        [Test]
        public void OwnerEnumerator_DirectMembersVisitValuesAndReset()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var enumerator = list.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);

            enumerator.Reset();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            enumerator.Dispose();
        }

        [Test]
        public void ReadOnlyEnumerator_DirectMembersVisitValuesAndReset()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();
            var enumerator = readOnly.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(3, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
            Assert.AreEqual(0, enumerator.Current);

            enumerator.Reset();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            enumerator.Dispose();
        }

        [Test]
        public void Enumerator_DoesNotInvalidateOnOwnerMutation()
        {
            using var list = new ListUnsafe<int>(8, Allocator.Temp);
            list.AddRange(new[] { 1, 2, 3 });
            var enumerator = list.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);

            list[1] = 20;
            list.Add(4);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(20, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(3, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();
        }

        [Test]
        public void Dispose_MarksNotCreatedAndDoubleDisposeIsNoOp()
        {
            var list = new ListUnsafe<int>(4, Allocator.Temp);
            list.Add(1);

            list.Dispose();

            Assert.IsFalse(list.IsCreated);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, list.Capacity);
            Assert.DoesNotThrow(() => list.Dispose());
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var list = new ListUnsafe<int>(4, Allocator.TempJob);
            list.Add(1);

            var handle = list.Dispose(default);

            Assert.IsFalse(list.IsCreated);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, list.Capacity);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Validation_RejectsInvalidIndexesRangesAmountsAndCapacity()
        {
            Assert.Throws<InvalidOperationException>(
                () => _ = new ListUnsafe<int>(-1, Allocator.Temp)
            );

            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();

            Assert.Throws<InvalidOperationException>(() => _ = list[-1]);
            Assert.Throws<InvalidOperationException>(() => _ = list.ElementAt(3));
            Assert.Throws<InvalidOperationException>(() => list.Insert(4, 4));
            Assert.Throws<InvalidOperationException>(() => list.RemoveAt(3));
            Assert.Throws<InvalidOperationException>(() => list.RemoveRange(3, 1));
            Assert.Throws<InvalidOperationException>(() => list.RemoveAtSwapBack(3));
            Assert.Throws<InvalidOperationException>(() => list.AddReplicate(0));
            Assert.Throws<InvalidOperationException>(() => list.AddReplicate(1, 0));
            Assert.Throws<InvalidOperationException>(() => list.AddReplicateNoInit(0));
            Assert.Throws<InvalidOperationException>(() => list.IncreaseCapacityTo(2));
            Assert.Throws<InvalidOperationException>(() => _ = readOnly[3]);
            Assert.Throws<InvalidOperationException>(() => list.Reinterpret<byte>());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void EmptyOwnerAndReadOnly_PeekAndPopThrow()
        {
            using var list = new ListUnsafe<int>(1, Allocator.Temp);
            var readOnly = list.AsReadOnly();

            Assert.Throws<InvalidOperationException>(() => _ = list.Peek());
            Assert.Throws<InvalidOperationException>(() => _ = list.Pop());
            Assert.Throws<InvalidOperationException>(() => _ = readOnly.Peek());
        }

        [Test]
        public void RemoveRange_ZeroLengthBoundaries_PinContract()
        {
            // Pins the family-wide contract: RemoveRange validates startIndex < Count before
            // looking at length, so a zero-length remove at Count (including on an empty list)
            // throws, unlike BCL List<T>.RemoveRange. Zero-length removes at a valid index
            // are no-ops.
            using var empty = new ListUnsafe<int>(0, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => empty.RemoveRange(0, 0));

            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => list.RemoveRange(3, 0));

            list.RemoveRange(0, 0);
            list.RemoveRange(2, 0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
        }
    }
}
