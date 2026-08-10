using System;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeQueueTests.cs against SharedQueueNative<T>.
    public partial class SharedQueueNativeTests
    {
        [Test]
        public void OwnerEnqueue_AfterViewCreationIsVisible()
        {
            using var queue = new SharedQueue<int>(8);
            queue.Enqueue(1);
            var view = queue.AsNative();

            queue.Enqueue(2);

            Assert.AreEqual(2, view.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, view.ToArray());
        }

        [Test]
        public void DefaultImplicitConversionAndValueInOperations_ExposeSharedFifoState()
        {
            SharedQueueNative<int> empty = default;
            using var queue = new SharedQueue<int, int>(8);
            SharedQueueNative<int> view = queue;
            var two = 2;

            view.Enqueue(1);
            view.Enqueue(in two);

            Assert.IsFalse(empty.IsCreated);
            Assert.IsTrue(view.IsCreated);
            Assert.AreEqual(2, view.Count);
            Assert.GreaterOrEqual(view.Capacity, 8);
            Assert.AreEqual(1, view.Peek());
            Assert.IsTrue(view.TryPeek(out var peeked));
            Assert.AreEqual(1, peeked);
            Assert.IsTrue(view.TryDequeue(out var dequeued));
            Assert.AreEqual(1, dequeued);
            Assert.AreEqual(2, view.Dequeue());
            Assert.IsFalse(view.TryPeek(out peeked));
            Assert.IsFalse(view.TryDequeue(out dequeued));
            Assert.AreEqual(0, peeked);
            Assert.AreEqual(0, dequeued);
        }

        [Test]
        public void CopyToAndTryCopyTo_NativeAndReadOnlyOverloadsPreserveFifoOrder()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 2, 3, 4 }.AsSpan());
            var view = queue.AsNative();
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            view.CopyTo(full);
            view.CopyTo(partial.AsSpan(), 2);
            view.CopyTo(1, offset);
            view.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, view.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);
            Assert.IsTrue(view.TryCopyTo(full));
            Assert.IsTrue(view.TryCopyTo(partial, 2));
            Assert.IsTrue(view.TryCopyTo(1, offset));
            Assert.IsTrue(view.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(view.TryCopyTo(3, partial));

            var readOnly = view.AsReadOnly();
            var readOnlyFull = new int[4];
            var readOnlyPartial = new int[2];
            var readOnlyOffset = new int[2];
            var readOnlyLength = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Count);
            Assert.GreaterOrEqual(readOnly.Capacity, 4);
            Assert.AreEqual(1, readOnly.Peek());
            Assert.IsTrue(readOnly.TryPeek(out var peeked));
            Assert.AreEqual(1, peeked);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, readOnly.ToArray());

            readOnly.CopyTo(readOnlyFull);
            readOnly.CopyTo(readOnlyPartial.AsSpan(), 2);
            readOnly.CopyTo(1, readOnlyOffset);
            readOnly.CopyTo(1, readOnlyLength, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, readOnlyFull);
            CollectionAssert.AreEqual(new[] { 1, 2 }, readOnlyPartial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, readOnlyOffset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, readOnlyLength);
            Assert.IsTrue(readOnly.TryCopyTo(readOnlyFull));
            Assert.IsTrue(readOnly.TryCopyTo(readOnlyPartial, 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, readOnlyOffset));
            Assert.IsTrue(readOnly.TryCopyTo(1, readOnlyLength, 2));
            Assert.IsFalse(readOnly.TryCopyTo(3, readOnlyPartial));

            view.Clear();
            Assert.IsFalse(readOnly.TryPeek(out peeked));
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void NativeAndReadOnlyEnumerators_MoveResetAndDisposeDirectly()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 2 }.AsSpan());
            var view = queue.AsNative();
            var ownerEnumerator = view.GetEnumerator();

            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(1, ownerEnumerator.Current);
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            ownerEnumerator.Dispose();

            var readOnlyEnumerator = view.AsReadOnly().GetEnumerator();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(1, readOnlyEnumerator.Current);
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();
        }

        [Test]
        public void Clear_ResetsSharedCountAndAllowsReuse()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 2 }.AsSpan());
            var view = queue.AsNative();

            view.Clear();
            view.Enqueue(3);

            CollectionAssert.AreEqual(new[] { 3 }, queue.ToArray());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void ViewCreatedBeforeOwnerGrow_ThrowsOnNextAccess()
        {
            using var queue = new SharedQueue<int>(2);
            queue.Enqueue(1);
            queue.Enqueue(2);
            var staleView = queue.AsNative();

            queue.Enqueue(3);

            Assert.Catch(() => _ = staleView.Count);
        }

        [Test]
        public void ViewCreatedAfterOwnerGrow_CanMutateOwner()
        {
            using var queue = new SharedQueue<int>(2);
            queue.EnqueueRange(new[] { 1, 2, 3 });
            var view = queue.AsNative();

            view.Enqueue(4);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, queue.ToArray());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void FullView_EnqueueThrowsBecauseCapacityIsImmutable()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 2 }.AsSpan());
            var view = queue.AsNative();

            Assert.Throws<InvalidOperationException>(() => view.Enqueue(3));
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Dispose_ReleasesOwnerAndNativeHeader()
        {
            var queue = new SharedQueue<int>(4);
            queue.Enqueue(1);
            var view = queue.AsNative();

            queue.Dispose();

            Assert.IsFalse(queue.IsCreated);
            Assert.Catch(() => _ = view.Count);
            Assert.DoesNotThrow(() => queue.Dispose());
        }
    }
}
