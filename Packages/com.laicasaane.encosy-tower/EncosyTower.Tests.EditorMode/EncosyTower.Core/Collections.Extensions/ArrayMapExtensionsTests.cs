using EncosyTower.Collections;
using NUnit.Framework;

using MapAPI = EncosyTower.Collections.ArrayMapExtensions;
using MapReadOnlyAPI = EncosyTower.Collections.ArrayMapReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections.Extensions
{
    public partial class ArrayMapExtensionsTests
    {
        [Test]
        public void GetValues_MutableAndReadOnlyViewsReflectMapStorage()
        {
            using var map = new ArrayMap<int, int>(4);
            map.Add(1, 10);
            map.Add(2, 20);

            var values = MapAPI.GetValues(map);
            var readOnlyValues = MapReadOnlyAPI.GetValues(map.AsReadOnly());

            Assert.AreEqual(map.Count, values.Length);
            Assert.AreEqual(map.Count, readOnlyValues.Length);
            Assert.AreEqual(10, readOnlyValues[0]);

            values[0] = 99;

            Assert.AreEqual(99, map[1]);
        }
    }
}
