// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using StackNativeAPI = EncosyTower.Collections.Extensions.StackNativeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class StackNativeExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var stack = new StackNative<int>(new[] { 1, 3, 5 }, Allocator.Temp);
            var comparer = new IntComparer();

            Assert.IsTrue(StackNativeAPI.Contains(in stack, 3));
            Assert.IsTrue(StackNativeAPI.Contains(in stack, 3, comparer));
            Assert.IsFalse(StackNativeAPI.Contains(in stack, 4));
            Assert.IsFalse(StackNativeAPI.Contains(in stack, 4, comparer));
        }
    }
}
