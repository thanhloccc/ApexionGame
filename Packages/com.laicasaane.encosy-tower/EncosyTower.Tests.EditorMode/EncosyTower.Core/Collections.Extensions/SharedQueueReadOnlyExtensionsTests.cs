// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using System;
using EncosyTower.Collections;
using NUnit.Framework;

using SharedQueueReadOnlyAPI = EncosyTower.Collections.Extensions.SharedQueueReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedQueueReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 3, 5 }.AsSpan());
            var view = queue.AsReadOnly();
            var comparer = new IntComparer();

            Assert.IsTrue(SharedQueueReadOnlyAPI.Contains(in view, 3));
            Assert.IsTrue(SharedQueueReadOnlyAPI.Contains(in view, 3, comparer));
            Assert.IsFalse(SharedQueueReadOnlyAPI.Contains(in view, 4));
            Assert.IsFalse(SharedQueueReadOnlyAPI.Contains(in view, 4, comparer));
        }
    }
}
