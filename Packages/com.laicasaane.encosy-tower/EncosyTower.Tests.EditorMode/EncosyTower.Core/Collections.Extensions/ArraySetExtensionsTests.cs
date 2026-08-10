using EncosyTower.Collections;
using NUnit.Framework;

using SetAPI = EncosyTower.Collections.ArraySetExtensions;
using SetReadOnlyAPI = EncosyTower.Collections.ArraySetReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections.Extensions
{
    public partial class ArraySetExtensionsTests
    {
        [Test]
        public void GetItems_MutableAndReadOnlyViewsReflectSetStorage()
        {
            using var set = new ArraySet<int>(4);
            set.Add(10);
            set.Add(20);

            var items = SetAPI.GetItems(set);
            var readOnlyItems = SetReadOnlyAPI.GetItems(set.AsReadOnly());

            Assert.AreEqual(set.Count, items.Length);
            Assert.AreEqual(set.Count, readOnlyItems.Length);
            Assert.AreEqual(10, readOnlyItems[0]);

            items[0] = 99;

            Assert.AreEqual(99, set.Items.Span[0]);
        }
    }
}
