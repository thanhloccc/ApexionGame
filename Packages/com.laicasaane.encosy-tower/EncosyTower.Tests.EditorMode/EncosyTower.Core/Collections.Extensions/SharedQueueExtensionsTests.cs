// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using System;
using EncosyTower.Collections;
using NUnit.Framework;

using SharedQueueAPI = EncosyTower.Collections.Extensions.SharedQueueExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedQueueExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 3, 5 }.AsSpan());
            var comparer = new IntComparer();

            Assert.IsTrue(SharedQueueAPI.Contains(queue, 3));
            Assert.IsTrue(SharedQueueAPI.Contains(queue, 3, comparer));
            Assert.IsFalse(SharedQueueAPI.Contains(queue, 4));
            Assert.IsFalse(SharedQueueAPI.Contains(queue, 4, comparer));
        }
    }
}
