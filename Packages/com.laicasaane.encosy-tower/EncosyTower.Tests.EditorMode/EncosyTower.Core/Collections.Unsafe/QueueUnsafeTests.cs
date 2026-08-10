using System;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public partial class QueueUnsafeTests
    {
        [Test]
        public void Constructors_CreateEmptyAndCopiedQueuesIncludingZeroCapacity()
        {
            using var queue = new QueueUnsafe<int>(4, new AllocatorStrategy(Allocator.Temp));
            using var zeroCapacity = new QueueUnsafe<int>(0, Allocator.Temp);
            using var copied = new QueueUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            Assert.IsTrue(queue.IsCreated);
            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(4, queue.Capacity);
            Assert.IsTrue(zeroCapacity.IsCreated);
            Assert.AreEqual(0, zeroCapacity.Count);
            Assert.AreEqual(0, zeroCapacity.Capacity);
            Assert.IsTrue(copied.IsCreated);
            Assert.AreEqual(3, copied.Count);
            Assert.AreEqual(3, copied.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, copied.ToArray());

            zeroCapacity.Enqueue(7);

            Assert.AreEqual(1, zeroCapacity.Count);
            Assert.AreEqual(7, zeroCapacity.Peek());
        }

        [Test]
        public void Enqueue_OverloadsGrowAndPreserveFifoOrder()
        {
            using var queue = new QueueUnsafe<int>(1, Allocator.Temp);
            var second = 2;

            queue.Enqueue(1);
            queue.Enqueue(in second);

            for (var i = 3; i <= 20; i++)
            {
                queue.Enqueue(i);
            }

            Assert.AreEqual(20, queue.Count);
            Assert.GreaterOrEqual(queue.Capacity, 20);

            for (var i = 1; i <= 20; i++)
            {
                Assert.AreEqual(i, queue.Dequeue());
            }

            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void EnqueueRange_WrappedGrowthPreservesFifoOrder()
        {
            using var queue = new QueueUnsafe<int>(new[] { 1, 2, 3, 4, 5, 6 }, Allocator.Temp);

            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
            queue.EnqueueRange(new[] { 7, 8, 9 });
            queue.EnqueueRange(ReadOnlySpan<int>.Empty);
            queue.EnqueueRange(new[] { 10, 11, 12 });

            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8, 9, 10, 11, 12 }, queue.ToArray());
            Assert.GreaterOrEqual(queue.Capacity, queue.Count);
        }

        [Test]
        public void DequeuePeekAndTryMethods_HandleSuccessAndFailure()
        {
            using var queue = new QueueUnsafe<int>(2, Allocator.Temp);

            Assert.IsFalse(queue.TryDequeue(out var emptyDequeue));
            Assert.IsFalse(queue.TryPeek(out var emptyPeek));
            Assert.AreEqual(0, emptyDequeue);
            Assert.AreEqual(0, emptyPeek);

            queue.EnqueueRange(new[] { 10, 20 });

            Assert.AreEqual(10, queue.Peek());
            Assert.IsTrue(queue.TryPeek(out var peeked));
            Assert.AreEqual(10, peeked);
            Assert.AreEqual(2, queue.Count);
            Assert.IsTrue(queue.TryDequeue(out var dequeued));
            Assert.AreEqual(10, dequeued);
            Assert.AreEqual(20, queue.Dequeue());
            Assert.IsFalse(queue.TryDequeue(out emptyDequeue));
            Assert.AreEqual(0, emptyDequeue);
        }

        [Test]
        public void Clear_ResetsRingAndAllowsReuse()
        {
            using var queue = new QueueUnsafe<int>(4, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3 });
            Assert.AreEqual(1, queue.Dequeue());

            queue.Clear();

            Assert.AreEqual(0, queue.Count);
            Assert.IsFalse(queue.TryPeek(out _));

            queue.EnqueueRange(new[] { 4, 5 });

            CollectionAssert.AreEqual(new[] { 4, 5 }, queue.ToArray());
        }

        [Test]
        public void CopyAndTryCopy_AllOverloadsLinearizeWrappedContent()
        {
            using var queue = new QueueUnsafe<int>(new[] { 1, 2, 3, 4, 5, 6 }, Allocator.Temp);
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            queue.Enqueue(7);
            queue.Enqueue(8);

            var full = new int[6];
            var partial = new int[3];
            var offset = new int[2];
            var range = new int[3];

            queue.CopyTo(full);
            queue.CopyTo(partial, 2);
            queue.CopyTo(2, offset);
            queue.CopyTo(1, range, 2);

            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7, 8 }, queue.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7, 8 }, full);
            CollectionAssert.AreEqual(new[] { 3, 4, 0 }, partial);
            CollectionAssert.AreEqual(new[] { 5, 6 }, offset);
            CollectionAssert.AreEqual(new[] { 4, 5, 0 }, range);
            Assert.IsTrue(queue.TryCopyTo(new int[6]));
            Assert.IsTrue(queue.TryCopyTo(new int[3], 2));
            Assert.IsTrue(queue.TryCopyTo(2, new int[3]));
            Assert.IsTrue(queue.TryCopyTo(1, new int[3], 2));

            var failedDestination = new[] { 9, 9 };
            Assert.IsFalse(queue.TryCopyTo(5, failedDestination));
            Assert.IsFalse(queue.TryCopyTo(5, failedDestination, 2));
            CollectionAssert.AreEqual(new[] { 9, 9 }, failedDestination);
        }

        [Test]
        public void CapacityMethodsGrowShrinkAndTrimWithoutChangingOrder()
        {
            using var queue = new QueueUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            queue.EnsureCapacity(10);
            Assert.GreaterOrEqual(queue.Capacity, 10);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToArray());

            var afterEnsure = queue.Capacity;
            queue.IncreaseCapacityBy(5);
            Assert.AreEqual(afterEnsure + 5, queue.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToArray());

            queue.IncreaseCapacityTo(queue.Count);
            Assert.AreEqual(queue.Count, queue.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToArray());

            queue.IncreaseCapacityTo(8);
            queue.Trim();

            Assert.AreEqual(queue.Count, queue.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToArray());
        }

        [Test]
        public void ReadOnly_AllPropertiesPeekCopyAndConversionMembersWork()
        {
            using var queue = new QueueUnsafe<int>(new[] { 1, 2, 3, 4, 5, 6 }, Allocator.Temp);
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            queue.Enqueue(7);
            queue.Enqueue(8);
            var readOnly = queue.AsReadOnly();
            QueueUnsafe<int>.ReadOnly converted = queue;
            var full = new int[6];
            var partial = new int[3];
            var offset = new int[2];
            var range = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(6, readOnly.Count);
            Assert.AreEqual(6, readOnly.Capacity);
            Assert.AreEqual(3, readOnly.Peek());
            Assert.IsTrue(readOnly.TryPeek(out var peeked));
            Assert.AreEqual(3, peeked);
            Assert.AreEqual(3, converted.Peek());

            readOnly.CopyTo(full);
            readOnly.CopyTo(partial, 2);
            readOnly.CopyTo(2, offset);
            readOnly.CopyTo(1, range, 2);

            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7, 8 }, readOnly.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7, 8 }, full);
            CollectionAssert.AreEqual(new[] { 3, 4, 0 }, partial);
            CollectionAssert.AreEqual(new[] { 5, 6 }, offset);
            CollectionAssert.AreEqual(new[] { 4, 5, 0 }, range);
            Assert.IsTrue(readOnly.TryCopyTo(new int[6]));
            Assert.IsTrue(readOnly.TryCopyTo(new int[3], 2));
            Assert.IsTrue(readOnly.TryCopyTo(2, new int[3]));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[3], 2));
            Assert.IsFalse(readOnly.TryCopyTo(5, new int[2]));
            Assert.IsFalse(readOnly.TryCopyTo(5, new int[2], 2));
        }

        [Test]
        public void ReadOnlyEmpty_TryPeekReturnsFalse()
        {
            using var queue = new QueueUnsafe<int>(0, Allocator.Temp);
            var readOnly = queue.AsReadOnly();

            Assert.IsFalse(readOnly.TryPeek(out var value));
            Assert.AreEqual(0, value);
        }

        [Test]
        public void OwnerAndReadOnlyEnumerators_DirectMembersVisitFifoOrderAndReset()
        {
            using var queue = new QueueUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var ownerEnumerator = queue.GetEnumerator();
            var readOnlyEnumerator = queue.AsReadOnly().GetEnumerator();

            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(1, ownerEnumerator.Current);
            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(2, ownerEnumerator.Current);
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(1, ownerEnumerator.Current);
            ownerEnumerator.Dispose();

            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(1, readOnlyEnumerator.Current);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(2, readOnlyEnumerator.Current);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(3, readOnlyEnumerator.Current);
            Assert.IsFalse(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(0, readOnlyEnumerator.Current);
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(1, readOnlyEnumerator.Current);
            readOnlyEnumerator.Dispose();
        }

        [Test]
        public void Enumerator_DoesNotInvalidateOnMutationWithinCapacity()
        {
            using var queue = new QueueUnsafe<int>(8, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3 });
            var enumerator = queue.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);

            queue.Enqueue(4);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(3, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();
        }

        [Test]
        public void Dispose_MarksNotCreatedAndDoubleDisposeIsNoOp()
        {
            var queue = new QueueUnsafe<int>(4, Allocator.Temp);
            queue.Enqueue(1);

            queue.Dispose();

            Assert.IsFalse(queue.IsCreated);
            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(0, queue.Capacity);
            Assert.DoesNotThrow(() => queue.Dispose());
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var queue = new QueueUnsafe<int>(4, Allocator.TempJob);
            queue.Enqueue(1);

            var handle = queue.Dispose(default);

            Assert.IsFalse(queue.IsCreated);
            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(0, queue.Capacity);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Validation_RejectsInvalidCapacityCopiesAndEmptyAccess()
        {
            Assert.Throws<InvalidOperationException>(
                static () => _ = new QueueUnsafe<int>(-1, Allocator.Temp)
            );

            using var queue = new QueueUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => queue.EnsureCapacity(-1));
            Assert.Throws<InvalidOperationException>(() => queue.IncreaseCapacityTo(2));
            Assert.Throws<InvalidOperationException>(() => queue.CopyTo(4, new int[1]));

            queue.Clear();
            var readOnly = queue.AsReadOnly();

            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
            Assert.Throws<InvalidOperationException>(() => queue.Peek());
            Assert.Throws<InvalidOperationException>(() => readOnly.Peek());
        }
    }
}
