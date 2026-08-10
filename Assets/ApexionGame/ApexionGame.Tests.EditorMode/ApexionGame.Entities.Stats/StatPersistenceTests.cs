using NUnit.Framework;
using Unity.Collections;

namespace ApexionGame.Entities.Stats.Tests
{
    using TestStore = StatStore<TestStat, TestModifier, TestObserver>;

    using TestAccessor = StatAccessor<
        TestValuePair, TestStat, TestModifier, TestModifierStack, TestObserver, TestComposer>;

    using TestWorldData = StatWorldData<
        TestValuePair, TestStat, TestModifier, TestModifierStack, TestObserver>;

    using TestBuilder = StatBuilder<
        TestValuePair, TestStat, TestModifier, TestModifierStack, TestObserver, TestComposer>;

    /// <summary>
    /// Task 4.1 — save/load round trip, including the handle remap of DEC-004.
    /// </summary>
    public sealed class StatPersistenceTests
    {
        private TestStore _store;
        private TestAccessor _accessor;
        private TestWorldData _worldData;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStore(16, Allocator.Persistent);
            _accessor = new TestAccessor(_store);
            _worldData = new TestWorldData(16, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            _worldData.Dispose();
            _store.Dispose();
        }

        [Test]
        public void CopyOwner_CapturesBuffersAndCounter()
        {
            var hp = NewOwnerWithStat(100f);
            _accessor.TryAddStatModifier(hp, TestModifier.Add(10f), out _, ref _worldData);

            using var stats = new NativeList<TestStat>(4, Allocator.Temp);
            using var modifiers = new NativeList<TestModifier>(4, Allocator.Temp);
            using var observers = new NativeList<TestObserver>(4, Allocator.Temp);

            Assert.IsTrue(_store.TryCopyOwnerTo(hp.owner, stats, modifiers, observers, out var counter));

            Assert.AreEqual(2, stats.Length, "the None stat plus one real stat");
            Assert.AreEqual(1, modifiers.Length);
            Assert.AreEqual(1u, counter, "one modifier was added, so the counter advanced once");
        }

        [Test]
        public void CopyOwner_OfDeadOwner_Fails()
        {
            var hp = NewOwnerWithStat(100f);
            _store.DestroyOwner(hp.owner);

            using var stats = new NativeList<TestStat>(4, Allocator.Temp);
            using var modifiers = new NativeList<TestModifier>(4, Allocator.Temp);
            using var observers = new NativeList<TestObserver>(4, Allocator.Temp);

            Assert.IsFalse(_store.TryCopyOwnerTo(hp.owner, stats, modifiers, observers, out _));
        }

        [Test]
        public void RestoreOwner_ReproducesValuesAndStatIndices()
        {
            var hp = NewOwnerWithStat(100f);
            _accessor.TryAddStatModifier(hp, TestModifier.Add(23f), out _, ref _worldData);
            Assert.AreEqual(123f, Current(hp));

            using var stats = new NativeList<TestStat>(4, Allocator.Temp);
            using var modifiers = new NativeList<TestModifier>(4, Allocator.Temp);
            using var observers = new NativeList<TestObserver>(4, Allocator.Temp);
            _store.TryCopyOwnerTo(hp.owner, stats, modifiers, observers, out var counter);

            Assert.IsTrue(_store.TryRestoreOwner(
                  stats.AsArray().AsReadOnlySpan()
                , modifiers.AsArray().AsReadOnlySpan()
                , observers.AsArray().AsReadOnlySpan()
                , counter
                , out var restoredOwner
            ));

            // StatIndex is preserved because the stat buffer is restored in order.
            var restoredHp = new StatHandle(restoredOwner, hp.index);

            Assert.AreEqual(123f, Current(restoredHp));
            Assert.IsTrue(_store.TryGetModifierIdCounter(restoredOwner, out var restoredCounter));
            Assert.AreEqual(counter, restoredCounter, "a later modifier must not reuse an existing id");
        }

        [Test]
        public void RestoreOwner_ThenAddModifier_DoesNotReuseId()
        {
            var hp = NewOwnerWithStat(100f);
            _accessor.TryAddStatModifier(hp, TestModifier.Add(1f), out var firstHandle, ref _worldData);

            using var stats = new NativeList<TestStat>(4, Allocator.Temp);
            using var modifiers = new NativeList<TestModifier>(4, Allocator.Temp);
            using var observers = new NativeList<TestObserver>(4, Allocator.Temp);
            _store.TryCopyOwnerTo(hp.owner, stats, modifiers, observers, out var counter);

            _store.TryRestoreOwner(
                  stats.AsArray().AsReadOnlySpan()
                , modifiers.AsArray().AsReadOnlySpan()
                , observers.AsArray().AsReadOnlySpan()
                , counter
                , out var restoredOwner
            );

            var restoredHp = new StatHandle(restoredOwner, hp.index);

            Assert.IsTrue(_accessor.TryAddStatModifier(
                  restoredHp, TestModifier.Add(2f), out var secondHandle, ref _worldData));

            Assert.AreNotEqual(firstHandle.modifierId, secondHandle.modifierId);
        }

        /// <remarks>
        /// The reason DEC-004 exists: a cross-owner modifier stores a StatHandle, and that handle
        /// embeds owner Index/Version. Restored into different slots, it must be rewritten.
        /// </remarks>
        [Test]
        public void RestoreTwoOwners_RemapsCrossOwnerModifier()
        {
            var src = NewOwnerWithStat(10f);
            var dst = NewOwnerWithStat(100f);
            _accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData);
            Assert.AreEqual(110f, Current(dst));

            var srcState = Capture(src.owner);
            var dstState = Capture(dst.owner);

            // Simulate a fresh session: the original owners are gone.
            _store.DestroyOwner(src.owner);
            _store.DestroyOwner(dst.owner);

            // Burn a slot so the restored owners cannot land on their old indices by luck.
            _store.CreateOwner();

            var restoredSrc = Restore(srcState);
            var restoredDst = Restore(dstState);

            var map = new NativeHashMap<StatOwnerHandle, StatOwnerHandle>(4, Allocator.Temp);
            map.Add(src.owner, restoredSrc);
            map.Add(dst.owner, restoredDst);
            var remap = new StatOwnerRemap(map);

            Assert.IsTrue(_accessor.TryRemapOwner(restoredSrc, in remap));
            Assert.IsTrue(_accessor.TryRemapOwner(restoredDst, in remap));

            var newSrc = new StatHandle(restoredSrc, src.index);
            var newDst = new StatHandle(restoredDst, dst.index);

            // The modifier must read the restored source, not a stale handle.
            _accessor.TrySetStatBaseValue(newSrc, new TestValuePair(30f), ref _worldData);

            Assert.AreEqual(130f, Current(newDst), "the remapped modifier must follow the restored source");

            map.Dispose();
            srcState.Dispose();
            dstState.Dispose();
        }

        [Test]
        public void Remap_WithoutRemapPass_LosesTheLink()
        {
            var src = NewOwnerWithStat(10f);
            var dst = NewOwnerWithStat(100f);
            _accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData);

            var dstState = Capture(dst.owner);
            _store.DestroyOwner(src.owner);
            _store.DestroyOwner(dst.owner);

            var restoredDst = Restore(dstState);
            var newDst = new StatHandle(restoredDst, dst.index);

            // No remap pass: the stored handle still points at a destroyed owner, which resolves to
            // nothing and contributes zero. This is the failure DEC-004 prevents.
            _accessor.TryUpdateStat(newDst, ref _worldData);

            Assert.AreEqual(100f, Current(newDst));

            dstState.Dispose();
        }

        // ---- helpers -------------------------------------------------------------------------

        private struct OwnerState
        {
            public NativeList<TestStat> stats;
            public NativeList<TestModifier> modifiers;
            public NativeList<TestObserver> observers;
            public uint counter;

            public void Dispose()
            {
                stats.Dispose();
                modifiers.Dispose();
                observers.Dispose();
            }
        }

        private OwnerState Capture(StatOwnerHandle owner)
        {
            var state = new OwnerState {
                stats = new NativeList<TestStat>(4, Allocator.Temp),
                modifiers = new NativeList<TestModifier>(4, Allocator.Temp),
                observers = new NativeList<TestObserver>(4, Allocator.Temp),
            };

            Assert.IsTrue(_store.TryCopyOwnerTo(
                  owner, state.stats, state.modifiers, state.observers, out state.counter));

            return state;
        }

        private StatOwnerHandle Restore(OwnerState state)
        {
            Assert.IsTrue(_store.TryRestoreOwner(
                  state.stats.AsArray().AsReadOnlySpan()
                , state.modifiers.AsArray().AsReadOnlySpan()
                , state.observers.AsArray().AsReadOnlySpan()
                , state.counter
                , out var owner
            ));

            return owner;
        }

        private StatHandle NewOwnerWithStat(float value)
        {
            StatAPI.CreateStatOwnerHandle<
                TestValuePair, TestStat, TestModifier, TestModifierStack, TestObserver, TestComposer
            >(ref _store, out _, out TestBuilder builder);

            return builder.CreateStatHandle(new TestValuePair(value), produceChangeEvents: true, userData: 0u);
        }

        private float Current(StatHandle handle)
        {
            Assert.IsTrue(_accessor.TryGetStatValue(handle, out var valuePair), "stat must be reachable");
            return valuePair.Current;
        }
    }
}
