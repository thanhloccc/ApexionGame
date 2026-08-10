// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using System;
using EncosyTower.Collections;
using NUnit.Framework;

using SharedQueueNativeAPI = EncosyTower.Collections.Extensions.SharedQueueNativeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedQueueNativeExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var queue = new SharedQueue<int>(new[] { 1, 3, 5 }.AsSpan());
            var view = queue.AsNative();
            var comparer = new IntComparer();

            Assert.IsTrue(SharedQueueNativeAPI.Contains(in view, 3));
            Assert.IsTrue(SharedQueueNativeAPI.Contains(in view, 3, comparer));
            Assert.IsFalse(SharedQueueNativeAPI.Contains(in view, 4));
            Assert.IsFalse(SharedQueueNativeAPI.Contains(in view, 4, comparer));
        }
    }
}
