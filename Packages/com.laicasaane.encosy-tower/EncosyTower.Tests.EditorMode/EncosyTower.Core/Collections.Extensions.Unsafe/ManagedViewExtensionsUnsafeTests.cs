using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

using DictionaryAPI = EncosyTower.Collections.Extensions.Unsafe.DictionaryReadOnlyExtensionsUnsafe;
using FasterListAPI = EncosyTower.Collections.Extensions.Unsafe.FasterListExtensionsUnsafe;
using HashSetAPI = EncosyTower.Collections.Extensions.Unsafe.HashSetReadOnlyExtensionsUnsafe;
using ListFastAPI = EncosyTower.Collections.Extensions.Unsafe.ListFastExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections.Extensions.Unsafe
{
    public partial class ManagedViewExtensionsUnsafeTests
    {
        [Test]
        public void ReadOnlyViews_ReturnOriginalManagedCollections()
        {
            var dictionary = new Dictionary<int, string> { [1] = "one" };
            var set = new HashSet<int> { 1 };
            var list = new List<int> { 1 };
            var dictionaryView = new DictionaryReadOnly<int, string>(dictionary);
            var setView = new HashSetReadOnly<int>(set);
            var listView = new ListFast<int>(list).AsReadOnly();

            var returnedDictionary = DictionaryAPI.GetDictionaryUnsafe(dictionaryView);
            var returnedSet = HashSetAPI.GetHashSetUnsafe(setView);
            var returnedList = ListFastAPI.GetListUnsafe(listView);

            Assert.AreSame(dictionary, returnedDictionary);
            Assert.AreSame(set, returnedSet);
            Assert.AreSame(list, returnedList);
        }

        [Test]
        public void FasterListBuffer_AliasesListStorageAndReportsCount()
        {
            var list = new FasterList<int>(1, 2, 3);

            FasterListAPI.GetBufferUnsafe(list, out var buffer, out var count);
            buffer[0] = 99;

            Assert.AreEqual(list.Count, count);
            Assert.AreEqual(99, list[0]);
        }
    }
}
