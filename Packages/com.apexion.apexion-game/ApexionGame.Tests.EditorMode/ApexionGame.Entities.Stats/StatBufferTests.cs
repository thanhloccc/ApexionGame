using System;
using NUnit.Framework;
using Unity.Collections;

namespace ApexionGame.Entities.Stats.Tests
{
    public sealed class StatBufferTests
    {
        private StatStore<FakeStat, FakeModifier, FakeObserver> _store;
        private StatOwnerHandle _owner;
        private StatBuffer<FakeStat> _buffer;

        [SetUp]
        public void SetUp()
        {
            _store = new StatStore<FakeStat, FakeModifier, FakeObserver>(4, Allocator.Persistent);
            _owner = _store.CreateOwner();
            _store.TryGetStats(_owner, out _buffer);
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
        }

        [Test]
        public void DefaultBuffer_IsNotCreated()
        {
            var buffer = default(StatBuffer<FakeStat>);

            Assert.IsFalse(buffer.IsCreated);
        }

        [Test]
        public void Add_AppendsInOrder()
        {
            _buffer.Add(new FakeStat { id = 1 });
            _buffer.Add(new FakeStat { id = 2 });
            _buffer.Add(new FakeStat { id = 3 });

            Assert.AreEqual(3, _buffer.Length);
            Assert.AreEqual(1, _buffer[0].id);
            Assert.AreEqual(2, _buffer[1].id);
            Assert.AreEqual(3, _buffer[2].id);
        }

        [Test]
        public void Insert_InTheMiddle_ShiftsTailAndKeepsOrder()
        {
            _buffer.Add(new FakeStat { id = 1 });
            _buffer.Add(new FakeStat { id = 3 });
            _buffer.Add(new FakeStat { id = 4 });

            _buffer.Insert(1, new FakeStat { id = 2 });

            Assert.AreEqual(4, _buffer.Length);
            Assert.AreEqual(1, _buffer[0].id);
            Assert.AreEqual(2, _buffer[1].id);
            Assert.AreEqual(3, _buffer[2].id);
            Assert.AreEqual(4, _buffer[3].id);
        }

        [Test]
        public void Insert_AtLength_AppendsAtTheEnd()
        {
            _buffer.Add(new FakeStat { id = 1 });

            _buffer.Insert(1, new FakeStat { id = 2 });

            Assert.AreEqual(2, _buffer.Length);
            Assert.AreEqual(2, _buffer[1].id);
        }

        [Test]
        public void Insert_AtZero_OnEmptyBuffer_Works()
        {
            _buffer.Insert(0, new FakeStat { id = 42 });

            Assert.AreEqual(1, _buffer.Length);
            Assert.AreEqual(42, _buffer[0].id);
        }

        [Test]
        public void RemoveAt_InTheMiddle_ShiftsTailAndKeepsOrder()
        {
            for (var i = 0; i < 5; i++)
            {
                _buffer.Add(new FakeStat { id = i });
            }

            _buffer.RemoveAt(2);

            Assert.AreEqual(4, _buffer.Length);
            Assert.AreEqual(0, _buffer[0].id);
            Assert.AreEqual(1, _buffer[1].id);
            Assert.AreEqual(3, _buffer[2].id);
            Assert.AreEqual(4, _buffer[3].id);
        }

        [Test]
        public void ElementAt_ReturnsWritableReference()
        {
            _buffer.Add(new FakeStat { id = 1, value = 1f });

            ref var element = ref _buffer.ElementAt(0);
            element.value = 99f;

            Assert.AreEqual(99f, _buffer[0].value);
        }

        [Test]
        public void Indexer_Setter_WritesThrough()
        {
            _buffer.Add(new FakeStat { id = 1 });

            _buffer[0] = new FakeStat { id = 5 };

            Assert.AreEqual(5, _buffer[0].id);
        }

        [Test]
        public void Clear_ResetsLengthButKeepsCapacity()
        {
            for (var i = 0; i < 10; i++)
            {
                _buffer.Add(new FakeStat { id = i });
            }

            var capacity = _buffer.Capacity;
            _buffer.Clear();

            Assert.AreEqual(0, _buffer.Length);
            Assert.AreEqual(capacity, _buffer.Capacity);
        }

        [Test]
        public void IncreaseCapacityTo_NeverShrinks()
        {
            _buffer.IncreaseCapacityTo(64);
            var capacity = _buffer.Capacity;

            Assert.GreaterOrEqual(capacity, 64);

            _buffer.IncreaseCapacityTo(2);

            Assert.AreEqual(capacity, _buffer.Capacity);
        }

        [Test]
        public void AsReadOnlySpan_SeesCurrentContent()
        {
            _buffer.Add(new FakeStat { id = 1 });
            _buffer.Add(new FakeStat { id = 2 });

            var span = _buffer.AsReadOnlySpan();

            Assert.AreEqual(2, span.Length);
            Assert.AreEqual(2, span[1].id);
        }

        [Test]
        public void AsSpan_WritesAliasTheBuffer()
        {
            _buffer.Add(new FakeStat { id = 1 });

            var span = _buffer.AsSpan();
            span[0] = new FakeStat { id = 7 };

            Assert.AreEqual(7, _buffer[0].id);
        }

        [Test]
        public void AsArray_AliasesTheBuffer()
        {
            _buffer.Add(new FakeStat { id = 1 });
            _buffer.Add(new FakeStat { id = 2 });

            var array = _buffer.AsArray();

            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(1, array[0].id);
        }

        [Test]
        public void Buffer_OfDestroyedOwner_IsRejected()
        {
            _buffer.Add(new FakeStat { id = 1 });
            _store.DestroyOwner(_owner);

            Assert.IsFalse(_store.TryGetStats(_owner, out _));
        }

        [Test]
        public void IndexOutOfRange_Throws()
        {
            _buffer.Add(new FakeStat { id = 1 });

            Assert.Throws<IndexOutOfRangeException>(() => _ = _buffer[1]);
            Assert.Throws<IndexOutOfRangeException>(() => _buffer.RemoveAt(1));
            Assert.Throws<IndexOutOfRangeException>(() => _buffer.Insert(2, new FakeStat()));
        }
    }
}
