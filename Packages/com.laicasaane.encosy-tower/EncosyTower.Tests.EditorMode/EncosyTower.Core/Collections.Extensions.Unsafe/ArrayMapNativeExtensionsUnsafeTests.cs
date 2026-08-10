using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using MapAPI = EncosyTower.Collections.Extensions.Unsafe.ArrayMapNativeExtensionsUnsafe;
using MapReadOnlyAPI = EncosyTower.Collections.Extensions.Unsafe.ArrayMapNativeReadOnlyExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections.Extensions.Unsafe
{
    public partial class ArrayMapNativeExtensionsUnsafeTests
    {
        [Test]
        public void MutableAndReadOnlyAccessors_AliasMapStorage()
        {
            using var map = new ArrayMapNative<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            var keys = MapAPI.GetKeysUnsafe(in map);
            var values = MapAPI.GetValuesUnsafe(in map);
            ref var secondValue = ref MapAPI.GetValueAtUnsafe(in map, 1);
            var readOnly = map.AsReadOnly();
            var readOnlyKeys = MapReadOnlyAPI.GetKeysUnsafe(in readOnly);
            var readOnlyValues = MapReadOnlyAPI.GetValuesUnsafe(in readOnly);
            ref readonly var readOnlySecondValue = ref MapReadOnlyAPI.GetValueAtUnsafe(
                  in readOnly
                , 1
            );

            Assert.AreEqual(1, keys[0].key);
            Assert.AreEqual(1, readOnlyKeys[0].key);
            Assert.AreEqual(10, readOnlyValues[0]);

            values[0] = 11;
            secondValue = 22;

            Assert.AreEqual(11, map[1]);
            Assert.AreEqual(22, map[2]);
            Assert.AreEqual(11, readOnlyValues[0]);
            Assert.AreEqual(22, readOnlySecondValue);

            keys[0].key = 99;

            Assert.AreEqual(99, readOnlyKeys[0].key);
        }
    }
}
