// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using QueueNativeAPI = EncosyTower.Collections.Extensions.QueueNativeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class QueueNativeExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var queue = new QueueNative<int>(new[] { 1, 3, 5 }, Allocator.Temp);
            var comparer = new IntComparer();

            Assert.IsTrue(QueueNativeAPI.Contains(in queue, 3));
            Assert.IsTrue(QueueNativeAPI.Contains(in queue, 3, comparer));
            Assert.IsFalse(QueueNativeAPI.Contains(in queue, 4));
            Assert.IsFalse(QueueNativeAPI.Contains(in queue, 4, comparer));
        }
    }
}
