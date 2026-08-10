using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using StackUnsafeAPI = EncosyTower.Collections.Extensions.StackUnsafeExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class StackUnsafeExtensionsTests
    {
        [Test]
        public void Contains_ValueAndComparerOverloadsReturnExpectedResults()
        {
            using var stack = new StackUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var comparer = new IntComparer();

            Assert.IsTrue(StackUnsafeAPI.Contains(in stack, 3));
            Assert.IsFalse(StackUnsafeAPI.Contains(in stack, 4));
            Assert.IsTrue(StackUnsafeAPI.Contains(in stack, 3, comparer));
            Assert.IsFalse(StackUnsafeAPI.Contains(in stack, 4, comparer));
        }
    }
}
