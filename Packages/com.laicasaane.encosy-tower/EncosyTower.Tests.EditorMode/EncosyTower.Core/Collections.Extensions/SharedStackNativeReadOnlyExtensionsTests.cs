// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using System;
using EncosyTower.Collections;
using NUnit.Framework;

using SharedStackNativeReadOnlyAPI = EncosyTower.Collections.Extensions.SharedStackNativeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class SharedStackNativeReadOnlyExtensionsTests
    {
        [Test]
        public void Contains_DefaultAndComparerFindExpectedValues()
        {
            using var stack = new SharedStack<int>(new[] { 1, 3, 5 }.AsSpan());
            var view = stack.AsNative().AsReadOnly();
            var comparer = new IntComparer();

            Assert.IsTrue(SharedStackNativeReadOnlyAPI.Contains(in view, 3));
            Assert.IsTrue(SharedStackNativeReadOnlyAPI.Contains(in view, 3, comparer));
            Assert.IsFalse(SharedStackNativeReadOnlyAPI.Contains(in view, 4));
            Assert.IsFalse(SharedStackNativeReadOnlyAPI.Contains(in view, 4, comparer));
        }
    }
}
