using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using ListUnsafeReadOnlyAPI = EncosyTower.Collections.Extensions.Unsafe.ListUnsafeReadOnlyExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListUnsafeReadOnlyExtensionsUnsafeTests
    {
        [Test]
        public void ReadOnlyAccessors_AliasListStorage()
        {
            using var list = new ListUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var readOnly = list.AsReadOnly();

            // SAFETY: The owner remains alive and index one is inside the borrowed read-only storage.
            unsafe
            {
                var buffer = ListUnsafeReadOnlyAPI.GetBufferUnsafe(in readOnly);
                ref readonly var second = ref ListUnsafeReadOnlyAPI.GetItemAtUnsafe(
                      in readOnly
                    , 1
                );

                Assert.AreEqual(1, buffer[0]);
                Assert.AreEqual(2, second);

                list[0] = 10;
                list[1] = 20;

                Assert.AreEqual(10, buffer[0]);
                Assert.AreEqual(20, second);
            }
        }
    }
}
