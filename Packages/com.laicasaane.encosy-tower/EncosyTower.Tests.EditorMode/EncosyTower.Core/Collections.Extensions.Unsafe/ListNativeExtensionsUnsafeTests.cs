// Adapted from Unity.Collections.Tests/ListExtensionsTests.cs.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using ListNativeUnsafeAPI = EncosyTower.Collections.Extensions.Unsafe.ListNativeExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListNativeExtensionsUnsafeTests
    {
        [Test]
        public void MutableAccessors_AliasListStorage()
        {
            using var list = new ListNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            // SAFETY: The list owns the aliased buffer for the block and both indices are valid.
            unsafe
            {
                var buffer = ListNativeUnsafeAPI.GetBufferUnsafe(in list);
                ref var second = ref ListNativeUnsafeAPI.GetItemAtUnsafe(in list, 1);
                buffer[0] = 10;
                second = 20;
            }

            CollectionAssert.AreEqual(new[] { 10, 20, 3 }, list.ToArray());
        }
    }
}
