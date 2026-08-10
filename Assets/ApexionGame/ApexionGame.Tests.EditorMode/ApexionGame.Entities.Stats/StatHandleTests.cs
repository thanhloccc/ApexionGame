using NUnit.Framework;
using Unity.Collections;

namespace ApexionGame.Entities.Stats.Tests
{
    public sealed class StatHandleTests
    {
        [Test]
        public void NullHandle_IsNotValid()
        {
            Assert.IsFalse(StatHandle.Null.IsValid);
            Assert.IsFalse(default(StatHandle).IsValid);
        }

        [Test]
        public void Handle_WithDeadOwner_IsNotValid()
        {
            var handle = new StatHandle(new StatOwnerHandle(5, 0), new StatIndex(1));

            Assert.IsFalse(handle.IsValid);
        }

        [Test]
        public void Handle_WithNoneStatIndex_IsNotValid()
        {
            var handle = new StatHandle(new StatOwnerHandle(0, 1), new StatIndex(0));

            Assert.IsFalse(handle.IsValid);
        }

        [Test]
        public void Handle_WithLiveOwnerAndPositiveIndex_IsValid()
        {
            var handle = new StatHandle(new StatOwnerHandle(0, 1), new StatIndex(1));

            Assert.IsTrue(handle.IsValid);
        }

        [Test]
        public void StatIndex_IsValidInRange()
        {
            Assert.IsFalse(new StatIndex(0).IsValidInRange(4));
            Assert.IsTrue(new StatIndex(1).IsValidInRange(4));
            Assert.IsTrue(new StatIndex(3).IsValidInRange(4));
            Assert.IsFalse(new StatIndex(4).IsValidInRange(4));
        }

        [Test]
        public void Equality_ComparesOwnerAndIndex()
        {
            var owner = new StatOwnerHandle(2, 1);
            var a = new StatHandle(owner, new StatIndex(3));
            var b = new StatHandle(owner, new StatIndex(3));
            var c = new StatHandle(owner, new StatIndex(4));
            var d = new StatHandle(new StatOwnerHandle(2, 2), new StatIndex(3));

            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != d);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void RecycledOwner_DoesNotEqualPreviousGeneration()
        {
            var before = new StatHandle(new StatOwnerHandle(7, 1), new StatIndex(1));
            var after = new StatHandle(new StatOwnerHandle(7, 2), new StatIndex(1));

            Assert.IsFalse(before == after);
        }

        [Test]
        public void ToFixedString_DoesNotOverflow()
        {
            var handle = new StatHandle(new StatOwnerHandle(int.MaxValue, int.MaxValue), new StatIndex(int.MaxValue));

            Assert.IsNotEmpty(handle.ToFixedString().ToString());
        }

        [Test]
        public void ModifierHandle_ComparesStatHandleAndId()
        {
            var stat = new StatHandle(new StatOwnerHandle(1, 1), new StatIndex(2));
            var a = new StatModifierHandle(stat, 4);
            var b = new StatModifierHandle(stat, 4);
            var c = new StatModifierHandle(stat, 5);

            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
        }

        [Test]
        public void OwnerRemap_MapsHandleAndKeepsStatIndex()
        {
            var map = new NativeHashMap<StatOwnerHandle, StatOwnerHandle>(4, Allocator.Temp);
            var oldOwner = new StatOwnerHandle(3, 1);
            var newOwner = new StatOwnerHandle(9, 4);
            map.Add(oldOwner, newOwner);

            var remap = new StatOwnerRemap(map);
            var oldHandle = new StatHandle(oldOwner, new StatIndex(6));

            Assert.IsTrue(remap.TryRemap(oldHandle, out var current));
            Assert.AreEqual(newOwner, current.owner);
            Assert.AreEqual(6, current.index.value);

            map.Dispose();
        }

        [Test]
        public void OwnerRemap_UnknownOwnerBecomesNull()
        {
            var map = new NativeHashMap<StatOwnerHandle, StatOwnerHandle>(4, Allocator.Temp);
            var remap = new StatOwnerRemap(map);
            var orphan = new StatHandle(new StatOwnerHandle(3, 1), new StatIndex(6));

            Assert.IsFalse(remap.TryRemap(orphan, out _));
            Assert.AreEqual(StatHandle.Null, remap.RemapOrNull(orphan));

            map.Dispose();
        }

        [Test]
        public void OwnerRemap_NotCreated_FailsSafely()
        {
            var remap = default(StatOwnerRemap);
            var handle = new StatHandle(new StatOwnerHandle(3, 1), new StatIndex(6));

            Assert.IsFalse(remap.IsCreated);
            Assert.IsFalse(remap.TryRemap(handle, out _));
        }
    }
}
