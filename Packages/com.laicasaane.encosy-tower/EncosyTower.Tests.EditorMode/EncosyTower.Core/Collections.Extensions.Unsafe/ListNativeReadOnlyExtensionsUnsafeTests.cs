// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using ListNativeReadOnlyUnsafeAPI = EncosyTower.Collections.Extensions.Unsafe.ListNativeReadOnlyExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListNativeReadOnlyExtensionsUnsafeTests
    {
        [Test]
        public void ReadOnlyAccessors_AliasListStorage()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var view = list.AsReadOnly();

            // SAFETY: The list remains alive for the block and the requested index is valid.
            unsafe
            {
                var buffer = ListNativeReadOnlyUnsafeAPI.GetBufferUnsafe(in view);
                ref readonly var second = ref ListNativeReadOnlyUnsafeAPI.GetItemAtUnsafe(
                      in view
                    , 1
                );

                Assert.AreEqual(1, buffer[0]);
                Assert.AreEqual(2, second);
            }
        }
    }
}
