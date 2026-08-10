// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using StackNativeReadOnlyAPI = EncosyTower.Collections.Extensions.StackNativeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class StackNativeReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var stack = new StackNative<int>(new[] { 1, 3, 5 }, Allocator.Temp);
            var view = stack.AsReadOnly();
            var comparer = new IntComparer();

            Assert.IsTrue(StackNativeReadOnlyAPI.Contains(in view, 3));
            Assert.IsTrue(StackNativeReadOnlyAPI.Contains(in view, 3, comparer));
            Assert.IsFalse(StackNativeReadOnlyAPI.Contains(in view, 4));
            Assert.IsFalse(StackNativeReadOnlyAPI.Contains(in view, 4, comparer));
        }
    }
}
