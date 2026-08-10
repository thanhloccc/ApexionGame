using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using SetAPI = EncosyTower.Collections.Extensions.Unsafe.ArraySetNativeExtensionsUnsafe;
using SetReadOnlyAPI = EncosyTower.Collections.Extensions.Unsafe.ArraySetNativeReadOnlyExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections.Extensions.Unsafe
{
    public partial class ArraySetNativeExtensionsUnsafeTests
    {
        [Test]
        public void MutableAndReadOnlyAccessors_AliasSetStorage()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);
            set.Add(10);
            set.Add(20);

            var nodes = SetAPI.GetNodesUnsafe(in set);
            var items = SetAPI.GetItemsUnsafe(in set);
            ref var secondItem = ref SetAPI.GetItemAtUnsafe(in set, 1);
            var readOnly = set.AsReadOnly();
            var readOnlyNodes = SetReadOnlyAPI.GetNodesUnsafe(in readOnly);
            var readOnlyItems = SetReadOnlyAPI.GetItemsUnsafe(in readOnly);
            ref readonly var readOnlySecondItem = ref SetReadOnlyAPI.GetItemAtUnsafe(
                  in readOnly
                , 1
            );

            Assert.AreEqual(10, nodes[0].key);
            Assert.AreEqual(10, readOnlyNodes[0].key);
            Assert.AreEqual(10, readOnlyItems[0]);

            items[0] = 11;
            secondItem = 22;
            nodes[0].key = 99;

            Assert.AreEqual(11, set.Items[0]);
            Assert.AreEqual(22, set.Items[1]);
            Assert.AreEqual(11, readOnlyItems[0]);
            Assert.AreEqual(22, readOnlySecondItem);
            Assert.AreEqual(99, readOnlyNodes[0].key);
        }
    }
}
