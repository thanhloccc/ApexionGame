// Adapted from Unity.Collections.Tests/UnsafeRingQueueTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using QueueNativeUnsafeAPI = EncosyTower.Collections.Extensions.Unsafe.QueueNativeExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class QueueNativeExtensionsUnsafeTests
    {
        [Test]
        public void RawAccessors_ExposeWrappedRingStorage()
        {
            using var queue = new QueueNative<int>(8, Allocator.Temp);
            queue.EnqueueRange(new[] { 1, 2, 3, 4, 5, 6 });
            queue.Dequeue();
            queue.Dequeue();
            queue.EnqueueRange(new[] { 7, 8, 9 });
            var readOnly = queue.AsReadOnly();

            // SAFETY: The queue owns the ring buffer for the block and the raw head index is valid.
            unsafe
            {
                var buffer = QueueNativeUnsafeAPI.GetBufferUnsafe(in queue);
                var head = QueueNativeUnsafeAPI.GetHeadUnsafe(in queue);
                var tail = QueueNativeUnsafeAPI.GetTailUnsafe(in queue);
                var readOnlyBuffer = QueueNativeUnsafeAPI.GetBufferUnsafe(in readOnly);

                Assert.AreEqual(2, head);
                Assert.AreEqual(1, tail);
                Assert.AreEqual(head, QueueNativeUnsafeAPI.GetHeadUnsafe(in readOnly));
                Assert.AreEqual(tail, QueueNativeUnsafeAPI.GetTailUnsafe(in readOnly));
                Assert.AreEqual(3, readOnlyBuffer[head]);

                buffer[head] = 30;
            }

            Assert.AreEqual(30, queue.Peek());
        }
    }
}
