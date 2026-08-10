using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using QueueUnsafeAPI = EncosyTower.Collections.Extensions.Unsafe.QueueUnsafeExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class QueueUnsafeExtensionsUnsafeTests
    {
        [Test]
        public void OwnerAndReadOnlyAccessors_ExposeWrappedRingStorage()
        {
            using var queue = new QueueUnsafe<int>(8, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3, 4, 5, 6 });
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            queue.EnqueueRange(new[] { 7, 8, 9 });
            var readOnly = queue.AsReadOnly();

            // SAFETY: The queue owns the ring buffer and the reported head stays valid for the block.
            unsafe
            {
                var buffer = QueueUnsafeAPI.GetBufferUnsafe(in queue);
                var head = QueueUnsafeAPI.GetHeadUnsafe(in queue);
                var tail = QueueUnsafeAPI.GetTailUnsafe(in queue);
                var readOnlyBuffer = QueueUnsafeAPI.GetBufferUnsafe(in readOnly);
                var readOnlyHead = QueueUnsafeAPI.GetHeadUnsafe(in readOnly);
                var readOnlyTail = QueueUnsafeAPI.GetTailUnsafe(in readOnly);

                Assert.AreEqual(2, head);
                Assert.AreEqual(1, tail);
                Assert.AreEqual(head, readOnlyHead);
                Assert.AreEqual(tail, readOnlyTail);
                Assert.AreEqual(3, readOnlyBuffer[head]);

                buffer[head] = 30;

                Assert.AreEqual(30, readOnlyBuffer[head]);
            }

            Assert.AreEqual(30, queue.Peek());
        }
    }
}
