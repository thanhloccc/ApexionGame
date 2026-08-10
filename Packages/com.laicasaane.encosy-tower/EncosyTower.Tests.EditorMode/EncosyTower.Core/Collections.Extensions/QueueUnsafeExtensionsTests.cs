using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using QueueUnsafeAPI = EncosyTower.Collections.Extensions.QueueUnsafeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class QueueUnsafeExtensionsTests
    {
        [Test]
        public void Contains_AllValueInAndComparerOverloadsReturnExpectedResults()
        {
            using var queue = new QueueUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var comparer = new IntComparer();
            var three = 3;
            var four = 4;

            Assert.IsTrue(QueueUnsafeAPI.Contains(in queue, 3));
            Assert.IsTrue(QueueUnsafeAPI.Contains(in queue, in three));
            Assert.IsFalse(QueueUnsafeAPI.Contains(in queue, 4));
            Assert.IsTrue(QueueUnsafeAPI.Contains(in queue, 3, comparer));
            Assert.IsTrue(QueueUnsafeAPI.Contains(in queue, in three, comparer));
            Assert.IsFalse(QueueUnsafeAPI.Contains(in queue, in four, comparer));
        }
    }
}
