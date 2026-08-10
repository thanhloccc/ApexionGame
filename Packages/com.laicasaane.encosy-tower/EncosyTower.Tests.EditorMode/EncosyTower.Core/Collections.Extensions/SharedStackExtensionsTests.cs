// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using System;
using EncosyTower.Collections;
using NUnit.Framework;

using SharedStackAPI = EncosyTower.Collections.Extensions.SharedStackExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedStackExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var stack = new SharedStack<int>(new[] { 1, 3, 5 }.AsSpan());
            var comparer = new IntComparer();

            Assert.IsTrue(SharedStackAPI.Contains(stack, 3));
            Assert.IsTrue(SharedStackAPI.Contains(stack, 3, comparer));
            Assert.IsFalse(SharedStackAPI.Contains(stack, 4));
            Assert.IsFalse(SharedStackAPI.Contains(stack, 4, comparer));
        }
    }
}
