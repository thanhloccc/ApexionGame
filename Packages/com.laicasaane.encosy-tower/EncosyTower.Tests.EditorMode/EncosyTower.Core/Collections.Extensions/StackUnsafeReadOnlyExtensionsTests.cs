using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using StackUnsafeReadOnlyAPI = EncosyTower.Collections.Extensions.StackUnsafeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class StackUnsafeReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_ValueAndComparerOverloadsReturnExpectedResults()
        {
            using var stack = new StackUnsafe<int>(new[] { 1, 3, 5, 7 }, Allocator.Temp);
            var readOnly = stack.AsReadOnly();
            var comparer = new IntComparer();

            Assert.IsTrue(StackUnsafeReadOnlyAPI.Contains(in readOnly, 3));
            Assert.IsFalse(StackUnsafeReadOnlyAPI.Contains(in readOnly, 4));
            Assert.IsTrue(StackUnsafeReadOnlyAPI.Contains(in readOnly, 3, comparer));
            Assert.IsFalse(StackUnsafeReadOnlyAPI.Contains(in readOnly, 4, comparer));
        }
    }
}
