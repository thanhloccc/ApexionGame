using NUnit.Framework;
using Unity.Collections;

namespace ApexionGame.Entities.Stats.Tests
{
    internal struct FakeStat
    {
        public int id;
        public float value;
    }

    // The store never interprets modifier or observer elements, so these tests only ever push
    // default values through them. The fields are here to give the two types a distinct layout.
#pragma warning disable CS0649 // never assigned to -- deliberate, see above

    internal struct FakeModifier
    {
        public uint id;
        public float amount;
    }

    internal struct FakeObserver
    {
        public int ownerIndex;
        public int statIndex;
    }

#pragma warning restore CS0649

    public sealed class StatStoreTests
    {
        private StatStore<FakeStat, FakeModifier, FakeObserver> _store;

        [SetUp]
        public void SetUp()
        {
            _store = new StatStore<FakeStat, FakeModifier, FakeObserver>(8, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
        }

        [Test]
        public void CreateOwner_ReturnsValidHandleWithEmptyBuffers()
        {
            var owner = _store.CreateOwner();

            Assert.IsTrue(owner.IsValid);
            Assert.IsTrue(_store.Exists(owner));
            Assert.AreEqual(1, _store.OwnerCount);

            Assert.IsTrue(_store.TryGetBuffers(owner, out var stats, out var modifiers, out var observers));
            Assert.AreEqual(0, stats.Length);
            Assert.AreEqual(0, modifiers.Length);
            Assert.AreEqual(0, observers.Length);
        }

        [Test]
        public void NullHandle_NeverResolves()
        {
            Assert.IsFalse(StatOwnerHandle.Null.IsValid);
            Assert.IsFalse(_store.Exists(StatOwnerHandle.Null));
            Assert.IsFalse(_store.TryGetStats(StatOwnerHandle.Null, out _));
        }

        [Test]
        public void DestroyOwner_InvalidatesHandleAndLowersOwnerCount()
        {
            var owner = _store.CreateOwner();

            Assert.IsTrue(_store.DestroyOwner(owner));
            Assert.IsFalse(_store.Exists(owner));
            Assert.IsFalse(_store.TryGetStats(owner, out _));
            Assert.AreEqual(0, _store.OwnerCount);
        }

        [Test]
        public void DestroyOwner_Twice_ReturnsFalseOnSecondCall()
        {
            var owner = _store.CreateOwner();

            Assert.IsTrue(_store.DestroyOwner(owner));
            Assert.IsFalse(_store.DestroyOwner(owner));
        }

        [Test]
        public void CreateOwner_AfterDestroy_RecyclesSlotWithNewVersion()
        {
            var first = _store.CreateOwner();
            _store.DestroyOwner(first);

            var second = _store.CreateOwner();

            Assert.AreEqual(first.Index, second.Index);
            Assert.AreNotEqual(first.Version, second.Version);
            Assert.IsTrue(_store.Exists(second));
            Assert.IsFalse(_store.Exists(first));
            Assert.AreEqual(1, _store.Capacity, "A recycled slot must not grow the slot list.");
        }

        [Test]
        public void RecycledOwner_StartsWithClearedBuffers()
        {
            var first = _store.CreateOwner();
            _store.TryGetStats(first, out var firstStats);
            firstStats.Add(new FakeStat { id = 7 });

            _store.DestroyOwner(first);

            var second = _store.CreateOwner();

            Assert.IsTrue(_store.TryGetStats(second, out var secondStats));
            Assert.AreEqual(0, secondStats.Length);
        }

        [Test]
        public void RecycledOwner_ResetsModifierIdCounter()
        {
            var first = _store.CreateOwner();
            _store.TryIncrementModifierId(first, out _);
            _store.TryIncrementModifierId(first, out var lastId);
            Assert.AreEqual(2u, lastId);

            _store.DestroyOwner(first);

            var second = _store.CreateOwner();

            Assert.IsTrue(_store.TryGetModifierIdCounter(second, out var counter));
            Assert.AreEqual(0u, counter);
        }

        [Test]
        public void ModifierId_IsPerOwnerAndMonotonic()
        {
            var a = _store.CreateOwner();
            var b = _store.CreateOwner();

            _store.TryIncrementModifierId(a, out var a1);
            _store.TryIncrementModifierId(a, out var a2);
            _store.TryIncrementModifierId(b, out var b1);

            Assert.AreEqual(1u, a1);
            Assert.AreEqual(2u, a2);
            Assert.AreEqual(1u, b1);
        }

        [Test]
        public void Buffer_StaysValidWhileManyOwnersAreCreated()
        {
            var owner = _store.CreateOwner();

            Assert.IsTrue(_store.TryGetStats(owner, out var stats));
            stats.Add(new FakeStat { id = 1, value = 10f });
            stats.Add(new FakeStat { id = 2, value = 20f });

            for (var i = 0; i < 5000; i++)
            {
                _store.CreateOwner();
            }

            Assert.AreEqual(2, stats.Length, "The buffer header must keep a stable address.");
            Assert.AreEqual(1, stats[0].id);
            Assert.AreEqual(20f, stats[1].value);

            stats.Add(new FakeStat { id = 3, value = 30f });
            Assert.AreEqual(3, stats.Length);
        }

        [Test]
        public void Buffers_OfDifferentOwners_AreIndependent()
        {
            var a = _store.CreateOwner();
            var b = _store.CreateOwner();

            _store.TryGetStats(a, out var statsA);
            _store.TryGetStats(b, out var statsB);

            statsA.Add(new FakeStat { id = 1 });

            Assert.AreEqual(1, statsA.Length);
            Assert.AreEqual(0, statsB.Length);
        }

        [Test]
        public void CreateAndDestroyManyOwners_KeepsOwnerCountConsistent()
        {
            const int COUNT = 10_000;

            var owners = new NativeList<StatOwnerHandle>(COUNT, Allocator.Temp);

            for (var i = 0; i < COUNT; i++)
            {
                owners.Add(_store.CreateOwner());
            }

            Assert.AreEqual(COUNT, _store.OwnerCount);
            Assert.AreEqual(COUNT, _store.Capacity);

            for (var i = 0; i < COUNT; i += 2)
            {
                Assert.IsTrue(_store.DestroyOwner(owners[i]));
            }

            Assert.AreEqual(COUNT / 2, _store.OwnerCount);

            for (var i = 0; i < COUNT / 2; i++)
            {
                var recycled = _store.CreateOwner();
                Assert.IsTrue(recycled.IsValid);
            }

            Assert.AreEqual(COUNT, _store.OwnerCount);
            Assert.AreEqual(COUNT, _store.Capacity, "Recycling must not grow the slot list.");

            for (var i = 1; i < COUNT; i += 2)
            {
                Assert.IsTrue(_store.Exists(owners[i]), "Untouched owners must stay alive.");
            }

            owners.Dispose();
        }

        [Test]
        public void Dispose_Twice_IsSafe()
        {
            var store = new StatStore<FakeStat, FakeModifier, FakeObserver>(4, Allocator.Persistent);
            store.CreateOwner();

            store.Dispose();

            Assert.IsFalse(store.IsCreated);
            Assert.DoesNotThrow(() => store.Dispose());
        }
    }
}
