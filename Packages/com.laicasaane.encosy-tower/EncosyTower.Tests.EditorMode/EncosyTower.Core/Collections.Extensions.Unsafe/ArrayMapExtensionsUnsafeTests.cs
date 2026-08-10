using EncosyTower.Collections;
using NUnit.Framework;

using MapAPI = EncosyTower.Collections.Unsafe.ArrayMapExtensionsUnsafe;
using MapReadOnlyAPI = EncosyTower.Collections.Unsafe.ArrayMapReadOnlyExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections.Extensions.Unsafe
{
    public partial class ArrayMapExtensionsUnsafeTests
    {
        [Test]
        public void MutableAndReadOnlyAccessors_AliasMapStorage()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);

            var keys = MapAPI.GetKeysUnsafe(map);
            var values = MapAPI.GetValuesUnsafe(map);
            ref var secondValue = ref MapAPI.GetValueAtUnsafe(map, 1);
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
