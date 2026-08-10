#if UNITY_COLLECTIONS

using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeQueueTests.cs and UnsafeRingQueueTests.cs.
    public partial class QueueNativeTests
    {
        [Test]
        public void ConstructorsAndInEnqueue_CreateExpectedContent()
        {
            using var capacity = new QueueNative<int>(4, Allocator.Temp);
            using var source = new QueueNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var four = 4;

            source.Enqueue(in four);

            Assert.IsTrue(capacity.IsCreated);
            Assert.AreEqual(0, capacity.Count);
            Assert.GreaterOrEqual(capacity.Capacity, 4);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, source.ToArray());
        }

        [Test]
        public void EnqueueAndDequeue_PreserveFifoOrderWhileGrowing()
        {
            const int N = 200;
            using var queue = new QueueNative<int>(2, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                queue.Enqueue(i);
            }

            Assert.AreEqual(N, queue.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.AreEqual(i, queue.Dequeue());
            }

            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void EmptyQueue_DequeueAndPeekThrowWhileTryMethodsReturnFalse()
        {
            using var queue = new QueueNative<int>(0, Allocator.Temp);

            Assert.Catch(() => queue.Dequeue());
            Assert.Catch(() => queue.Peek());
            Assert.IsFalse(queue.TryDequeue(out var dequeued));
            Assert.IsFalse(queue.TryPeek(out var peeked));
            Assert.AreEqual(0, dequeued);
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void EnqueueRange_WrapsAndGrowsWithoutChangingOrder()
        {
            using var queue = new QueueNative<int>(8, Allocator.Temp);
            queue.EnqueueRange(new[] { 0, 1, 2, 3, 4, 5 });

            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, queue.Dequeue());
            }

            queue.EnqueueRange(new[] { 6, 7, 8 });
            queue.EnqueueRange(new[] { 9, 10, 11, 12, 13 });

            CollectionAssert.AreEqual(
                new[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 },
                queue.ToArray()
            );
        }

        [Test]
        public void PeekAndTryOperations_ObserveFrontWithoutRemovingIt()
        {
            using var queue = new QueueNative<int>(2, Allocator.Temp);
            queue.Enqueue(10);
            queue.Enqueue(20);

            Assert.AreEqual(10, queue.Peek());
            Assert.IsTrue(queue.TryPeek(out var peeked));
            Assert.AreEqual(10, peeked);
            Assert.AreEqual(2, queue.Count);

            Assert.IsTrue(queue.TryDequeue(out var dequeued));
            Assert.AreEqual(10, dequeued);
            Assert.AreEqual(20, queue.Peek());
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            using var queue = new QueueNative<int>(4, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3 });

            queue.Clear();

            Assert.AreEqual(0, queue.Count);
            Assert.IsFalse(queue.TryPeek(out _));

            queue.Enqueue(4);

            Assert.AreEqual(4, queue.Dequeue());
        }

        [Test]
        public void ToArrayAndCopyTo_LinearizeWrappedContent()
        {
            using var queue = new QueueNative<int>(8, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3, 4, 5, 6 });

            for (var i = 0; i < 4; i++)
            {
                queue.Dequeue();
            }

            queue.EnqueueRange(new[] { 7, 8, 9, 10 });

            var full = new int[6];
            var partial = new int[3];
            queue.CopyTo(full);
            queue.CopyTo(2, partial);

            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8, 9, 10 }, queue.ToArray());
            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8, 9, 10 }, full);
            CollectionAssert.AreEqual(new[] { 7, 8, 9 }, partial);
        }

        [Test]
        public void CopyToAndTryCopyTo_AllOverloadsCopyOrReturnFalse()
        {
            using var queue = new QueueNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            queue.CopyTo(full);
            queue.CopyTo(partial.AsSpan(), 2);
            queue.CopyTo(1, offset);
            queue.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, full);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partial);
            CollectionAssert.AreEqual(new[] { 2, 3 }, offset);
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, explicitLength);

            Assert.IsTrue(queue.TryCopyTo(full));
            Assert.IsTrue(queue.TryCopyTo(partial, 2));
            Assert.IsTrue(queue.TryCopyTo(1, offset));
            Assert.IsTrue(queue.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(queue.TryCopyTo(3, partial));
        }

        [Test]
        public void ReadOnly_ConversionPropertiesPeekCopiesAndEnumerationExposeOwnerValues()
        {
            using var queue = new QueueNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            QueueNative<int>.ReadOnly readOnly = queue;
            var direct = queue.AsReadOnly();
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(readOnly.Count, direct.Count);
            Assert.AreEqual(4, readOnly.Count);
            Assert.GreaterOrEqual(readOnly.Capacity, 4);
            Assert.AreEqual(1, readOnly.Peek());
            Assert.IsTrue(readOnly.TryPeek(out var peeked));
            Assert.AreEqual(1, peeked);
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

            var enumerator = readOnly.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            enumerator.Dispose();

            queue.Clear();
            Assert.IsFalse(readOnly.TryPeek(out peeked));
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void CapacityAndOwnerEnumeratorMembers_WorkDirectly()
        {
            using var queue = new QueueNative<int>(new[] { 1, 2 }, Allocator.Temp);
            var initialCapacity = queue.Capacity;

            queue.EnsureCapacity(initialCapacity + 2);
            Assert.GreaterOrEqual(queue.Capacity, initialCapacity + 2);

            var ensured = queue.Capacity;
            queue.IncreaseCapacityBy(2);
            Assert.GreaterOrEqual(queue.Capacity, ensured + 2);

            var increased = queue.Capacity;
            queue.IncreaseCapacityTo(increased + 2);
            Assert.GreaterOrEqual(queue.Capacity, increased + 2);

            queue.Trim();
            Assert.AreEqual(queue.Count, queue.Capacity);

            var enumerator = queue.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            enumerator.Dispose();
        }

        [Test]
        public void Enumerator_VisitsWrappedContentInFifoOrder()
        {
            using var queue = new QueueNative<int>(8, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3, 4, 5, 6 });
            queue.Dequeue();
            queue.Dequeue();
            queue.EnqueueRange(new[] { 7, 8, 9 });
            var visited = new int[queue.Count];
            var index = 0;

            foreach (var value in queue)
            {
                visited[index++] = value;
            }

            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7, 8, 9 }, visited);
        }

        [Test]
        public void Enumerator_MutationDuringIteration_Throws()
        {
            using var queue = new QueueNative<int>(8, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3 });
            var enumerator = queue.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            queue.Enqueue(4);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var queue = new QueueNative<int>(4, Allocator.Temp);
            Assert.IsTrue(queue.IsCreated);

            queue.Dispose();

            Assert.IsFalse(queue.IsCreated);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void UseAfterDispose_Throws()
        {
            var queue = new QueueNative<int>(4, Allocator.Temp);
            queue.Enqueue(1);
            queue.Dispose();

            Assert.Catch(() => _ = queue.Count);
        }

        [Test]
        public void DoubleDispose_DoesNotThrow()
        {
            var queue = new QueueNative<int>(4, Allocator.Temp);
            queue.Dispose();

            Assert.DoesNotThrow(() => queue.Dispose());
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var queue = new QueueNative<int>(4, Allocator.TempJob);
            queue.Enqueue(1);

            var handle = queue.Dispose(default);

            Assert.IsFalse(queue.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }
    }
}

#endif
