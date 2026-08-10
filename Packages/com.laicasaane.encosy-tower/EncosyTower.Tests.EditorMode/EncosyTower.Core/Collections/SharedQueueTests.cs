using System;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeQueueTests.cs.
    public partial class SharedQueueTests
    {
        [Test]
        public void Constructors_CreateExpectedContent()
        {
            using var empty = new SharedQueue<int>();
            using var capacity = new SharedQueue<int>(8);
            using var span = new SharedQueue<int>(new[] { 1, 2, 3 }.AsSpan());
            using var collection = new SharedQueue<int>(new List<int> { 4, 5, 6 });

            Assert.AreEqual(0, empty.Count);
            Assert.AreEqual(0, capacity.Count);
            Assert.GreaterOrEqual(capacity.Capacity, 8);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, span.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, collection.ToArray());
        }

        [Test]
        public void GenericConstructors_CreateExpectedContent()
        {
            using var empty = new SharedQueue<int, uint>();
            using var capacity = new SharedQueue<int, uint>(8);
            using var span = new SharedQueue<int, uint>(new[] { 1, 2, 3 }.AsSpan());
            using var collection = new SharedQueue<int, uint>(new List<int> { 4, 5, 6 });

            Assert.IsTrue(empty.IsCreated);
            Assert.AreEqual(0, empty.Count);
            Assert.GreaterOrEqual(capacity.Capacity, 8);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, span.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, collection.ToArray());
        }

        [Test]
        public void EnqueueAndDequeue_GrowAndPreserveFifoOrder()
        {
            const int N = 200;
            using var queue = new SharedQueue<int>(2);

            for (var i = 0; i < N; i++)
            {
                queue.Enqueue(i);
            }

            Assert.AreEqual(N, queue.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.AreEqual(i, queue.Dequeue());
            }
        }

        [Test]
        public void ValueInAndTryOperations_ReturnExpectedValues()
        {
            using var queue = new SharedQueue<int, int>(4);
            var two = 2;

            queue.Enqueue(1);
            queue.Enqueue(in two);

            Assert.AreEqual(1, queue.Peek());
            Assert.IsTrue(queue.TryPeek(out var peeked));
            Assert.AreEqual(1, peeked);
            Assert.IsTrue(queue.TryDequeue(out var dequeued));
            Assert.AreEqual(1, dequeued);
            Assert.AreEqual(2, queue.Dequeue());
            Assert.IsFalse(queue.TryPeek(out peeked));
            Assert.IsFalse(queue.TryDequeue(out dequeued));
            Assert.AreEqual(0, peeked);
            Assert.AreEqual(0, dequeued);
        }

        [Test]
        public void EnqueueRange_WrappedQueueRetainsLinearOrder()
        {
            using var queue = new SharedQueue<int>(8);
            queue.EnqueueRange(new[] { 0, 1, 2, 3, 4, 5 });

            for (var i = 0; i < 4; i++)
            {
                queue.Dequeue();
            }

            queue.EnqueueRange(new[] { 6, 7, 8, 9 });

            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8, 9 }, queue.ToArray());
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 2, 3 }.AsSpan());

            queue.Clear();

            Assert.AreEqual(0, queue.Count);
            queue.Enqueue(4);
            Assert.AreEqual(4, queue.Dequeue());
        }

        [Test]
        public void Trim_ShrinksCapacityToCountWithoutChangingOrder()
        {
            using var queue = new SharedQueue<int>(16);
            queue.EnqueueRange(new[] { 1, 2, 3 });

            queue.Trim();

            Assert.AreEqual(queue.Count, queue.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToArray());
        }

        [Test]
        public void CopyToAndTryCopyTo_AllOwnerAndReadOnlyOverloadsPreserveFifoOrder()
        {
            using var queue = new SharedQueue<int, int>(new[] { 1, 2, 3, 4 }.AsSpan());
            var ownerFull = new int[4];
            var ownerPartial = new int[2];
            var ownerOffset = new int[2];
            var ownerLength = new int[3];

            queue.CopyTo(ownerFull);
            queue.CopyTo(ownerPartial.AsSpan(), 2);
            queue.CopyTo(1, ownerOffset);
            queue.CopyTo(1, ownerLength, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, ownerFull);
            CollectionAssert.AreEqual(new[] { 1, 2 }, ownerPartial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, ownerOffset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, ownerLength);
            Assert.IsTrue(queue.TryCopyTo(ownerFull));
            Assert.IsTrue(queue.TryCopyTo(ownerPartial, 2));
            Assert.IsTrue(queue.TryCopyTo(1, ownerOffset));
            Assert.IsTrue(queue.TryCopyTo(1, ownerLength, 2));
            Assert.IsFalse(queue.TryCopyTo(3, ownerPartial));

            SharedQueue<int, int>.ReadOnly readOnly = queue;
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

            var nativeReadOnly = readOnly.AsNative();
            Assert.AreEqual(4, nativeReadOnly.Count);

            queue.Clear();
            Assert.IsFalse(readOnly.TryPeek(out peeked));
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void CapacityConversionsEnumeratorsAndDispose_WorkDirectly()
        {
            var queue = new SharedQueue<int, int>(new[] { 1, 2 }.AsSpan());

            try
            {
                var initialCapacity = queue.Capacity;
                queue.IncreaseCapacityBy(2);
                Assert.GreaterOrEqual(queue.Capacity, initialCapacity + 2);

                var increased = queue.Capacity;
                queue.IncreaseCapacityTo(increased + 2);
                Assert.GreaterOrEqual(queue.Capacity, increased + 2);

                SharedQueueNative<int> native = queue;
                Assert.AreEqual(2, native.Count);

                var ownerEnumerator = queue.GetEnumerator();
                Assert.IsTrue(ownerEnumerator.MoveNext());
                Assert.AreEqual(1, ownerEnumerator.Current);
                ownerEnumerator.Reset();
                Assert.IsTrue(ownerEnumerator.MoveNext());
                ownerEnumerator.Dispose();

                var readOnlyEnumerator = queue.AsReadOnly().GetEnumerator();
                Assert.IsTrue(readOnlyEnumerator.MoveNext());
                Assert.AreEqual(1, readOnlyEnumerator.Current);
                readOnlyEnumerator.Reset();
                Assert.IsTrue(readOnlyEnumerator.MoveNext());
                readOnlyEnumerator.Dispose();
            }
            finally
            {
                queue.Dispose();
            }

            Assert.IsFalse(queue.IsCreated);
            Assert.DoesNotThrow(() => queue.Dispose());
        }
    }
}
