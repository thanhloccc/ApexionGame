// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using QueueNativeReadOnlyAPI = EncosyTower.Collections.Extensions.QueueNativeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class QueueNativeReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var queue = new QueueNative<int>(new[] { 1, 3, 5 }, Allocator.Temp);
            var view = queue.AsReadOnly();
            var comparer = new IntComparer();

            Assert.IsTrue(QueueNativeReadOnlyAPI.Contains(in view, 3));
            Assert.IsTrue(QueueNativeReadOnlyAPI.Contains(in view, 3, comparer));
            Assert.IsFalse(QueueNativeReadOnlyAPI.Contains(in view, 4));
            Assert.IsFalse(QueueNativeReadOnlyAPI.Contains(in view, 4, comparer));
        }
    }
}
