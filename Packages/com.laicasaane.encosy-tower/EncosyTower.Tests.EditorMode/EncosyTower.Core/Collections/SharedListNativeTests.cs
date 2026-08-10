using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeListTests.cs against the SharedListNative view.
    // The view is a fixed-capacity window onto the owner's shared buffer/count/version.
    public partial class SharedListNativeTests
    {
        [Test]
        public void AsNative_ReflectsOwnerContent()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);
            list.Add(2);
            list.Add(3);

            var view = list.AsNative();

            Assert.IsTrue(view.IsCreated);
            Assert.AreEqual(3, view.Count);
            Assert.AreEqual(1, view[0]);
            Assert.AreEqual(2, view[1]);
            Assert.AreEqual(3, view[2]);
        }

        [Test]
        public void DefaultAndImplicitConversion_ReportCreationStateAndOwnerValues()
        {
            SharedListNative<int> empty = default;
            using var list = new SharedList<int, int>(new[] { 1, 2 });
            SharedListNative<int> view = list;

            Assert.IsFalse(empty.IsCreated);
            Assert.IsTrue(view.IsCreated);
            Assert.AreEqual(2, view.Count);
            Assert.GreaterOrEqual(view.Capacity, 2);
            CollectionAssert.AreEqual(new[] { 1, 2 }, view.ToArray());
        }

        [Test]
        public void GenericOwnerAsNative_UsesNativeElementTypeAndAliasesOwnerStorage()
        {
            using var list = new SharedList<int, uint>(new[] { 1, 2 });
            var view = list.AsNative();

            Assert.AreEqual(1u, view[0]);

            view[1] = 20u;

            Assert.AreEqual(20, list[1]);
        }

        [Test]
        public void ViewAdd_WithinCapacity_OwnerObservesChange()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);

            var view = list.AsNative();
            view.Add(2);
            view.Add(3);

            Assert.AreEqual(3, view.Count);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void ValueAndInOverloads_AddInsertAndPushMutateOwner()
        {
            using var list = new SharedList<int>(8);
            var view = list.AsNative();
            var two = 2;
            var four = 4;
            var six = 6;

            view.Add(1);
            view.Add(in two);
            view.Insert(2, 3);
            view.Insert(3, in four);

            Assert.AreEqual(4, view.Push(5));
            Assert.AreEqual(5, view.Push(in six));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, list.ToArray());
        }

        [Test]
        public void AddRange_AllOverloadsAppendRequestedValues()
        {
            using var list = new SharedList<int>(20);
            using var native = new NativeArray<int>(new[] { 5, 6, 7 }, Allocator.Temp);
            using var sliceOwner = new NativeArray<int>(new[] { 0, 8, 9, 10 }, Allocator.Temp);
            var slice = new NativeSlice<int>(sliceOwner, 1, 3);
            var view = list.AsNative();

            view.AddRange(new[] { 1, 2 });
            view.AddRange(new[] { 3, 4, 99 }, 2);
            view.AddRange(native);
            view.AddRange(native, 2);
            view.AddRange(in slice);
            view.AddRange(in slice, 2);

            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4, 5, 6, 7, 5, 6, 8, 9, 10, 8, 9 },
                list.ToArray()
            );
        }

        [Test]
        public void ViewIndexerSet_OwnerObservesChange()
        {
            using var list = new SharedList<int>(8);
            list.Add(1);
            list.Add(2);

            var view = list.AsNative();
            view[1] = 99;

            Assert.AreEqual(99, list[1]);
        }

        [Test]
        public void ElementAtCopyFromAndTryCopyFrom_MutateOwnerOrReturnFalse()
        {
            using var list = new SharedList<int>(new[] { 0, 0, 0, 0, 0, 0 });
            var view = list.AsNative();

            ref var first = ref view.ElementAt(0);
            first = 1;
            view.CopyFrom(new[] { 2, 3 });
            view.CopyFrom(new[] { 4, 5, 99 }, 2);
            view.CopyFrom(2, new[] { 6, 7 });
            view.CopyFrom(4, new[] { 8, 9, 99 }, 2);

            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8, 9 }, list.ToArray());

            Assert.IsTrue(view.TryCopyFrom(new[] { 10, 11 }));
            Assert.IsTrue(view.TryCopyFrom(new[] { 12, 13, 99 }, 2));
            Assert.IsTrue(view.TryCopyFrom(2, new[] { 14, 15 }));
            Assert.IsTrue(view.TryCopyFrom(4, new[] { 16, 17, 99 }, 2));
            Assert.IsFalse(view.TryCopyFrom(5, new[] { 18, 19 }));

            CollectionAssert.AreEqual(new[] { 12, 13, 14, 15, 16, 17 }, list.ToArray());
        }

        [Test]
        public void CopyToAndTryCopyTo_SpanAndArrayOverloadsCopyOrReturnFalse()
        {
            using var list = new SharedList<int>(new[] { 1, 2, 3, 4 });
            var view = list.AsNative();
            var array = new int[6];
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            view.CopyTo(array, 1);
            view.CopyTo(full);
            view.CopyTo(partial.AsSpan(), 2);
            view.CopyTo(1, offset);
            view.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, array);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);

            Assert.IsTrue(view.TryCopyTo(full));
            Assert.IsTrue(view.TryCopyTo(partial, 2));
            Assert.IsTrue(view.TryCopyTo(1, offset));
            Assert.IsTrue(view.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(view.TryCopyTo(3, partial));
        }

        [Test]
        public void CopyTo_NativeArrayAndSliceOverloadsCopyRequestedRanges()
        {
            using var list = new SharedList<int>(new[] { 1, 2, 3, 4 });
            using var full = new NativeArray<int>(4, Allocator.Temp);
            using var offset = new NativeArray<int>(3, Allocator.Temp);
            using var partial = new NativeArray<int>(3, Allocator.Temp);
            using var sliceOwner = new NativeArray<int>(5, Allocator.Temp);
            var fullSlice = new NativeSlice<int>(sliceOwner, 1, 3);
            var partialSlice = new NativeSlice<int>(sliceOwner, 2, 2);
            var view = list.AsNative();

            view.CopyTo(full);
            view.CopyTo(1, offset);
            view.CopyTo(1, partial, 2);
            view.CopyTo(1, in fullSlice);
            view.CopyTo(2, in partialSlice, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, offset.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, partial.ToArray());
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 4, 0 }, sliceOwner.ToArray());
        }

        [Test]
        public void RemoveClearStackReplicateAndViewsMutateSharedStorage()
        {
            using var list = new SharedList<int>(12);
            var view = list.AsNative();

            var defaults = view.AddReplicate(2);
            var values = view.AddReplicate(7, 3);
            var noInit = view.AddReplicateNoInit(2);
            noInit[0] = 8;
            noInit[1] = 9;

            CollectionAssert.AreEqual(new[] { 0, 0 }, defaults.ToArray());
            CollectionAssert.AreEqual(new[] { 7, 7, 7 }, values.ToArray());

            Assert.AreEqual(9, view.Peek());
            Assert.AreEqual(9, view.Pop());

            view.RemoveAt(0);
            view.RemoveRange(0, 2);
            view.RemoveAtSwapBack(0);

            var span = view.AsSpan();
            var readOnlySpan = view.AsReadOnlySpan();
            var slice = view.AsNativeSlice();
            span[0] = 10;
            slice[1] = 11;

            Assert.AreEqual(10, readOnlySpan[0]);
            CollectionAssert.AreEqual(new[] { 10, 11 }, list.ToArray());

            view.Clear();

            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void ReinterpretAndReadOnlySurface_AliasOwnerAndCopyAllRanges()
        {
            using var list = new SharedList<int>(new[] { 1, 2, 3, 4 });
            var view = list.AsNative();
            var reinterpreted = view.Reinterpret<uint>();
            SharedListNative<int>.ReadOnly readOnly = view;
            ReadOnlySpan<int> implicitSpan = readOnly;
            var array = new int[6];
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            reinterpreted[0] = 10u;

            Assert.AreEqual(10, list[0]);
            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Count);
            Assert.GreaterOrEqual(readOnly.Capacity, 4);
            Assert.IsTrue(readOnly.IsReadOnly);
            Assert.AreEqual(3, readOnly[2]);
            CollectionAssert.AreEqual(new[] { 10, 2, 3, 4 }, implicitSpan.ToArray());
            CollectionAssert.AreEqual(new[] { 10, 2, 3, 4 }, readOnly.AsReadOnlySpan().ToArray());
            CollectionAssert.AreEqual(new[] { 10, 2, 3, 4 }, readOnly.ToArray());

            readOnly.CopyTo(array, 1);
            readOnly.CopyTo(full);
            readOnly.CopyTo(partial.AsSpan(), 2);
            readOnly.CopyTo(1, offset);
            readOnly.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 0, 10, 2, 3, 4, 0 }, array);
            CollectionAssert.AreEqual(new[] { 10, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 10, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);

            Assert.IsTrue(readOnly.TryCopyTo(full));
            Assert.IsTrue(readOnly.TryCopyTo(partial, 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, offset));
            Assert.IsTrue(readOnly.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(readOnly.TryCopyTo(3, partial));

            var reinterpretedReadOnly = readOnly.Reinterpret<uint>();
            Assert.AreEqual(10u, reinterpretedReadOnly[0]);
        }

        [Test]
        public void OwnerAndReadOnlyEnumerators_ConstructMoveResetAndDisposeDirectly()
        {
            using var list = new SharedList<int>(new[] { 1, 2 });
            var view = list.AsNative();
            var ownerEnumerator = view.GetEnumerator();

            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(1, ownerEnumerator.Current);
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            ownerEnumerator.Dispose();

            var readOnly = view.AsReadOnly();
            var readOnlyEnumerator = readOnly.GetEnumerator();

            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(1, readOnlyEnumerator.Current);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(2, readOnlyEnumerator.Current);
            Assert.IsFalse(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();

            var directEnumerator = new SharedListNative<int>.Enumerator(readOnly);
            Assert.IsTrue(directEnumerator.MoveNext());
            directEnumerator.Dispose();
        }

        [Test]
        public void CurrentViewCreatedAfterOwnerGrowth_WritesOwnerStorage()
        {
            using var list = new SharedList<int>(1);
            list.Add(1);
            var stale = list.AsNative();
            var staleCapacity = stale.Capacity;

            list.IncreaseCapacityBy(4);

            var current = list.AsNative();
            current.Add(2);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(2, list[1]);
            Assert.Greater(current.Capacity, staleCapacity);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void NativeView_CannotGrowOwnerStorage()
        {
            using var list = new SharedList<int>(2);
            list.AddRange(new[] { 1, 2 });
            var view = list.AsNative();
            var capacity = list.Capacity;

            Assert.Throws<InvalidOperationException>(() => view.Add(3));
            Assert.AreEqual(capacity, list.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2 }, list.ToArray());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void ViewCreatedBeforeOwnerResize_ThrowsOnWrite()
        {
            // A view holds a copy of the owner buffer's safety handle at AsNative() time.
            // Growing the owner past capacity re-pins the buffer and releases that handle,
            // so a write through the stale view must throw.
            using var list = new SharedList<int>(2);
            list.Add(1);

            var staleView = list.AsNative();

            for (var i = 0; i < 64; i++)
            {
                list.Add(i); // forces AllocateMore -> re-pin -> old handle released
            }

            Assert.Catch(() => staleView.Add(123));
        }

        [Test]
        public void RemoveRange_ZeroLengthBoundaries_PinContract()
        {
            // Pins the family-wide contract: RemoveRange validates startIndex < Count before
            // looking at length, so a zero-length remove at Count (including on an empty list)
            // throws, unlike BCL List<T>.RemoveRange. Zero-length removes at a valid index
            // are no-ops.
            using var emptyOwner = new SharedList<int>(4);
            var emptyView = emptyOwner.AsNative();

            Assert.Throws<InvalidOperationException>(() => emptyView.RemoveRange(0, 0));

            using var owner = new SharedList<int>(new[] { 1, 2, 3 });
            var view = owner.AsNative();

            Assert.Throws<InvalidOperationException>(() => view.RemoveRange(3, 0));

            view.RemoveRange(0, 0);
            view.RemoveRange(2, 0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, view.ToArray());
            Assert.AreEqual(3, owner.Count);
        }
    }
}
