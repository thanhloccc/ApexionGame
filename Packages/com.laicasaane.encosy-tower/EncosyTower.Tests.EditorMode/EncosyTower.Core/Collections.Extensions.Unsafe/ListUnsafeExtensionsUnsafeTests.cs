using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using ListUnsafeAPI = EncosyTower.Collections.Extensions.Unsafe.ListUnsafeExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListUnsafeExtensionsUnsafeTests
    {
        [Test]
        public void MutableAccessors_AliasListStorage()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            // SAFETY: The list remains alive and both raw indices are inside its allocated storage.
            unsafe
            {
                var buffer = ListUnsafeAPI.GetBufferUnsafe(in list);
                ref var second = ref ListUnsafeAPI.GetItemAtUnsafe(in list, 1);
                buffer[0] = 10;
                second = 20;
            }

            CollectionAssert.AreEqual(new[] { 10, 20, 3 }, list.ToArray());
        }
    }
}
