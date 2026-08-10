// No Unity.Collections.Tests analogue; covers the read-only dictionary view.

using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class DictionaryReadOnlyTests
    {
        [Test]
        public void Constructor_ExposesSourceProperties()
        {
            var source = new Dictionary<int, string>
            {
                [1] = "one",
                [2] = "two",
            };
            var dictionary = new DictionaryReadOnly<int, string>(source);

            Assert.IsTrue(dictionary.IsCreated);
            Assert.IsTrue(dictionary.IsReadOnly);
            Assert.AreEqual(2, dictionary.Count);
            Assert.GreaterOrEqual(dictionary.Capacity, dictionary.Count);
        }

        [Test]
        public void LookupMembers_FindExpectedValues()
        {
            var source = new Dictionary<int, string>
            {
                [1] = "one",
                [2] = "two",
            };
            var dictionary = new DictionaryReadOnly<int, string>(source);

            Assert.AreEqual("one", dictionary[1]);
            Assert.IsTrue(dictionary.ContainsKey(2));
            Assert.IsFalse(dictionary.ContainsKey(3));
            Assert.IsTrue(dictionary.ContainsValue("two"));
            Assert.IsFalse(dictionary.ContainsValue("three"));
            Assert.IsTrue(dictionary.TryGetValue(2, out var value));
            Assert.AreEqual("two", value);
            Assert.IsFalse(dictionary.TryGetValue(3, out _));
        }

        [Test]
        public void KeysValuesAndEnumerator_ExposeAllEntries()
        {
            var source = new Dictionary<int, string>
            {
                [1] = "one",
                [2] = "two",
            };
            var dictionary = new DictionaryReadOnly<int, string>(source);
            var visited = new Dictionary<int, string>();

            foreach (var pair in dictionary)
            {
                visited.Add(pair.Key, pair.Value);
            }

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, dictionary.Keys);
            CollectionAssert.AreEquivalent(new[] { "one", "two" }, dictionary.Values);
            CollectionAssert.AreEquivalent(source, visited);

            IReadOnlyDictionary<int, string> contract = dictionary;
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, contract.Keys);
            CollectionAssert.AreEquivalent(new[] { "one", "two" }, contract.Values);
        }

        [Test]
        public void Empty_IsCreatedAndContainsNoEntries()
        {
            var dictionary = DictionaryReadOnly<int, string>.Empty;
            var constructed = new DictionaryReadOnly<int, string>();

            Assert.IsTrue(dictionary.IsCreated);
            Assert.IsTrue(dictionary.IsReadOnly);
            Assert.AreEqual(0, dictionary.Count);
            Assert.AreEqual(0, dictionary.Capacity);
            Assert.IsTrue(constructed.IsCreated);
            Assert.AreEqual(0, constructed.Count);
        }

        [Test]
        public void ImplicitConversion_ReflectsLaterSourceMutations()
        {
            var source = new Dictionary<int, string>();
            DictionaryReadOnly<int, string> dictionary = source;

            source.Add(1, "one");

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("one", dictionary[1]);
        }

        [Test]
        public void EqualsAndGetHashCode_UseSourceIdentity()
        {
            var source = new Dictionary<int, string> { [1] = "one" };
            var same = new DictionaryReadOnly<int, string>(source);
            var alias = new DictionaryReadOnly<int, string>(source);
            var other = new DictionaryReadOnly<int, string>(
                new Dictionary<int, string> { [1] = "one" }
            );

            Assert.IsTrue(same.Equals(alias));
            Assert.IsTrue(same.Equals(source));
            Assert.AreEqual(same.GetHashCode(), alias.GetHashCode());
            Assert.IsFalse(same.Equals(other));
        }
    }
}
