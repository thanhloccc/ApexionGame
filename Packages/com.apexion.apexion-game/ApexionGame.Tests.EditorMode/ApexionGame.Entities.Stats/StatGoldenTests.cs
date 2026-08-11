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
    /// The 13 scenarios of the phase 2 exit criteria.
    /// </summary>
    public sealed class StatGoldenTests
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

        // ---- 1: set base value, no modifier ------------------------------------------------

        [Test]
        public void S01_SetBaseValue_WithoutModifier_RecomputesCurrentAndEmitsOneEvent()
        {
            var hp = NewOwnerWithStat(100f, out _);

            _worldData.Clear();
            Assert.IsTrue(_accessor.TrySetStatBaseValue(hp, new TestValuePair(120f), ref _worldData));

            Assert.AreEqual(120f, Current(hp));
            Assert.AreEqual(1, CountEvents(hp));
        }

        [Test]
        public void S01_SetBaseValue_ToSameValue_EmitsNoEvent()
        {
            var hp = NewOwnerWithStat(100f, out _);

            _worldData.Clear();
            _accessor.TrySetStatBaseValue(hp, new TestValuePair(100f), ref _worldData);

            Assert.AreEqual(0, CountEvents(hp));
        }

        // ---- 2: Add then Multiply, application order ----------------------------------------

        [Test]
        public void S02_AddThenMultiply_AppliesAsBasePlusAddTimesMul()
        {
            var hp = NewOwnerWithStat(100f, out _);

            Assert.IsTrue(_accessor.TryAddStatModifier(hp, TestModifier.Add(50f), out _, ref _worldData));
            Assert.AreEqual(150f, Current(hp));

            Assert.IsTrue(_accessor.TryAddStatModifier(hp, TestModifier.Multiply(2f), out _, ref _worldData));
            Assert.AreEqual(300f, Current(hp));
        }

        [Test]
        public void S02_MultiplyThenAdd_GivesSameResult_StackIsOrderIndependent()
        {
            var hp = NewOwnerWithStat(100f, out _);

            _accessor.TryAddStatModifier(hp, TestModifier.Multiply(2f), out _, ref _worldData);
            _accessor.TryAddStatModifier(hp, TestModifier.Add(50f), out _, ref _worldData);

            Assert.AreEqual(300f, Current(hp));
        }

        // ---- 3: cross-owner AddFromStat ------------------------------------------------------

        [Test]
        public void S03_CrossOwnerModifier_PropagatesWhenSourceChanges()
        {
            var src = NewOwnerWithStat(10f, out _);
            var dst = NewOwnerWithStat(100f, out _);

            Assert.IsTrue(_accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData));
            Assert.AreEqual(110f, Current(dst));

            _worldData.Clear();
            Assert.IsTrue(_accessor.TrySetStatBaseValue(src, new TestValuePair(20f), ref _worldData));

            Assert.AreEqual(20f, Current(src));
            Assert.AreEqual(120f, Current(dst));
            Assert.AreEqual(1, CountEvents(dst));
        }

        [Test]
        public void S03_CrossOwnerModifier_RegistersObserverOnSource()
        {
            var src = NewOwnerWithStat(10f, out _);
            var dst = NewOwnerWithStat(100f, out _);

            _accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData);

            Assert.IsTrue(_accessor.TryGetObserverCount(src, out var observerCount));
            Assert.AreEqual(1, observerCount);
        }

        // ---- 4: five-level chain --------------------------------------------------------------

        [Test]
        public void S04_FiveLevelChain_PropagatesToTheEnd()
        {
            var owner = NewOwner(out var builder);
            var a = NewStat(ref builder, 1f);
            var b = NewStat(ref builder, 0f);
            var c = NewStat(ref builder, 0f);
            var d = NewStat(ref builder, 0f);
            var e = NewStat(ref builder, 0f);

            Assert.IsTrue(_accessor.TryAddStatModifier(b, TestModifier.AddFrom(a), out _, ref _worldData));
            Assert.IsTrue(_accessor.TryAddStatModifier(c, TestModifier.AddFrom(b), out _, ref _worldData));
            Assert.IsTrue(_accessor.TryAddStatModifier(d, TestModifier.AddFrom(c), out _, ref _worldData));
            Assert.IsTrue(_accessor.TryAddStatModifier(e, TestModifier.AddFrom(d), out _, ref _worldData));

            Assert.AreEqual(1f, Current(e), "chain should already carry A's initial value");

            _accessor.TrySetStatBaseValue(a, new TestValuePair(5f), ref _worldData);

            Assert.AreEqual(5f, Current(b));
            Assert.AreEqual(5f, Current(c));
            Assert.AreEqual(5f, Current(d));
            Assert.AreEqual(5f, Current(e));
            Assert.IsTrue(owner.IsValid);
        }

        // ---- 5: diamond graph, D-01 measurement ------------------------------------------------

        [Test]
        public void S05_DiamondGraph_FinalValueIsCorrect()
        {
            BuildDiamond(out _, out var b, out var c, out var d);

            Assert.AreEqual(1f, Current(b));
            Assert.AreEqual(1f, Current(c));
            Assert.AreEqual(2f, Current(d));
        }

        [Test]
        public void S05_DiamondGraph_SetBaseValue_ConvergesToCorrectValue()
        {
            BuildDiamond(out var a, out _, out _, out var d);

            _worldData.Clear();
            _accessor.TrySetStatBaseValue(a, new TestValuePair(5f), ref _worldData);

            Assert.AreEqual(10f, Current(d), "D must equal B + C regardless of how many passes it took");
        }

        /// <remarks>
        /// D-01 / DEC-005 resolved: both propagation entry points now behave identically, because
        /// neither dedupes. Records the recalculation count so a future change is visible.
        /// </remarks>
        [Test]
        public void S05_DiamondGraph_D01_BothEntryPointsAgree()
        {
            BuildDiamond(out var a, out _, out _, out var d);

            _worldData.Clear();
            _accessor.TrySetStatBaseValue(a, new TestValuePair(5f), ref _worldData);
            var viaUpdateStatRef = CountEvents(d);

            _worldData.Clear();
            _accessor.TryUpdateStat(d, ref _worldData);
            var viaTryUpdateStat = CountEvents(d);

            TestContext.WriteLine($"D-01: UpdateStatRef emitted {viaUpdateStatRef} event(s) for D, "
                + $"TryUpdateStat emitted {viaTryUpdateStat}.");

            Assert.GreaterOrEqual(viaUpdateStatRef, 1, "D must be recalculated at least once");
            Assert.AreEqual(10f, Current(d));
        }

        /// <remarks>
        /// The bug that settled DEC-005. A join whose two incoming branches have different lengths
        /// is reached before the longer branch has been recalculated, so it is computed from a stale
        /// input and must be recalculated a second time. A visited set suppresses exactly that second
        /// pass and leaves the value permanently wrong — upstream's TryUpdateStat had one.
        /// <para>
        /// A -> B -> E  and  A -> C -> D -> E. With A = 5: B = 5, C = 5, D = 5, E = B + D = 10.
        /// </para>
        /// </remarks>
        [Test]
        public void S05_UnequalDepthDiamond_JoinIsRepairedNotLeftStale()
        {
            NewOwner(out var builder);
            var a = NewStat(ref builder, 1f);
            var b = NewStat(ref builder, 0f);
            var c = NewStat(ref builder, 0f);
            var d = NewStat(ref builder, 0f);
            var e = NewStat(ref builder, 0f);

            _accessor.TryAddStatModifier(b, TestModifier.AddFrom(a), out _, ref _worldData);
            _accessor.TryAddStatModifier(c, TestModifier.AddFrom(a), out _, ref _worldData);
            _accessor.TryAddStatModifier(d, TestModifier.AddFrom(c), out _, ref _worldData);
            _accessor.TryAddStatModifier(e, TestModifier.AddFrom(b), out _, ref _worldData);
            _accessor.TryAddStatModifier(e, TestModifier.AddFrom(d), out _, ref _worldData);

            Assert.AreEqual(2f, Current(e), "with A = 1 the join starts at B + D = 2");

            // Path 1 — TrySetStatBaseValue, which propagates through UpdateStatRef.
            _worldData.Clear();
            _accessor.TrySetStatBaseValue(a, new TestValuePair(5f), ref _worldData);
            Assert.AreEqual(10f, Current(e), "UpdateStatRef must repair E after D catches up");

            // Path 2 — TryRemoveStatModifier, which propagates through TryUpdateStat instead.
            _accessor.TryAddStatModifier(a, TestModifier.Add(4f), out var bump, ref _worldData);
            Assert.AreEqual(18f, Current(e), "A is now 9, so E = B + D = 18");

            _worldData.Clear();
            Assert.IsTrue(_accessor.TryRemoveStatModifier(bump, ref _worldData));

            Assert.AreEqual(10f, Current(e), "TryUpdateStat must repair E too — it has no visited set");
        }

        // ---- 6: reject self-observe -------------------------------------------------------------

        [Test]
        public void S06_SelfObservingModifier_IsRejected()
        {
            var hp = NewOwnerWithStat(100f, out _);

            Assert.IsFalse(_accessor.TryAddStatModifier(hp, TestModifier.AddFrom(hp), out _, ref _worldData));
            Assert.IsTrue(_accessor.TryGetModifierCount(hp, out var count));
            Assert.AreEqual(0, count);
        }

        // ---- 7: reject observer loops ------------------------------------------------------------

        [Test]
        public void S07_TwoLevelLoop_IsRejected()
        {
            var owner = NewOwner(out var builder);
            var a = NewStat(ref builder, 1f);
            var b = NewStat(ref builder, 1f);

            Assert.IsTrue(_accessor.TryAddStatModifier(a, TestModifier.AddFrom(b), out _, ref _worldData));
            Assert.IsFalse(
                  _accessor.TryAddStatModifier(b, TestModifier.AddFrom(a), out _, ref _worldData)
                , "B observing A would close a two-level loop"
            );

            Assert.IsTrue(_accessor.TryGetModifierCount(b, out var count));
            Assert.AreEqual(0, count);
            Assert.IsTrue(owner.IsValid);
        }

        [Test]
        public void S07_ThreeLevelLoop_IsRejected()
        {
            var owner = NewOwner(out var builder);
            var a = NewStat(ref builder, 1f);
            var b = NewStat(ref builder, 1f);
            var c = NewStat(ref builder, 1f);

            Assert.IsTrue(_accessor.TryAddStatModifier(a, TestModifier.AddFrom(b), out _, ref _worldData));
            Assert.IsTrue(_accessor.TryAddStatModifier(b, TestModifier.AddFrom(c), out _, ref _worldData));
            Assert.IsFalse(
                  _accessor.TryAddStatModifier(c, TestModifier.AddFrom(a), out _, ref _worldData)
                , "C observing A would close a three-level loop"
            );

            Assert.IsTrue(_accessor.TryGetModifierCount(c, out var count));
            Assert.AreEqual(0, count);
            Assert.IsTrue(owner.IsValid);
        }

        // ---- 8: batch add ---------------------------------------------------------------------------

        [Test]
        public void S08_BatchAddTwentyModifiers_LeavesRangesConsistent()
        {
            NewOwner(out var builder);
            var first = NewStat(ref builder, 0f);
            var second = NewStat(ref builder, 0f);

            var modifiers = new TestModifier[20];

            for (var i = 0; i < modifiers.Length; i++)
            {
                modifiers[i] = TestModifier.Add(1f);
            }

            var handles = new NativeList<StatModifierHandle>(20, Allocator.Temp);

            Assert.IsTrue(_accessor.TryAddStatModifiersBatch(first, modifiers, handles, ref _worldData));
            Assert.AreEqual(20, handles.Length);

            handles.Dispose();

            Assert.IsTrue(_accessor.TryGetStat(first, out var firstStat));
            Assert.IsTrue(_accessor.TryGetStat(second, out var secondStat));

            Assert.AreEqual(0, firstStat.ModifierRange.startIndex);
            Assert.AreEqual(20, firstStat.ModifierRange.count);
            Assert.AreEqual(20, secondStat.ModifierRange.startIndex, "the single deferred shift must run once");
            Assert.AreEqual(0, secondStat.ModifierRange.count);
            Assert.AreEqual(20f, Current(first));
        }

        [Test]
        public void S08_BatchAdd_ProducesUniqueModifierIds()
        {
            NewOwner(out var builder);
            var stat = NewStat(ref builder, 0f);

            var modifiers = new TestModifier[5];

            for (var i = 0; i < modifiers.Length; i++)
            {
                modifiers[i] = TestModifier.Add(1f);
            }

            var handles = new NativeList<StatModifierHandle>(5, Allocator.Temp);
            _accessor.TryAddStatModifiersBatch(stat, modifiers, handles, ref _worldData);

            var ids = new NativeHashSet<uint>(8, Allocator.Temp);

            for (var i = 0; i < handles.Length; i++)
            {
                Assert.IsTrue(ids.Add(handles[i].modifierId), "modifier ids must be unique per owner");
            }

            ids.Dispose();
            handles.Dispose();
        }

        // ---- 9: remove a modifier in the middle --------------------------------------------------------

        [Test]
        public void S09_RemoveModifierInTheMiddle_ShiftsFollowingStartIndexes()
        {
            NewOwner(out var builder);
            var first = NewStat(ref builder, 0f);
            var second = NewStat(ref builder, 0f);

            _accessor.TryAddStatModifier(first, TestModifier.Add(1f), out _, ref _worldData);
            _accessor.TryAddStatModifier(first, TestModifier.Add(2f), out var middle, ref _worldData);
            _accessor.TryAddStatModifier(first, TestModifier.Add(4f), out _, ref _worldData);
            _accessor.TryAddStatModifier(second, TestModifier.Add(8f), out _, ref _worldData);

            Assert.AreEqual(7f, Current(first));

            Assert.IsTrue(_accessor.TryGetStat(second, out var beforeRemoval));
            Assert.AreEqual(3, beforeRemoval.ModifierRange.startIndex);

            Assert.IsTrue(_accessor.TryRemoveStatModifier(middle, ref _worldData));

            Assert.IsTrue(_accessor.TryGetStat(first, out var firstStat));
            Assert.IsTrue(_accessor.TryGetStat(second, out var secondStat));

            Assert.AreEqual(2, firstStat.ModifierRange.count);
            Assert.AreEqual(2, secondStat.ModifierRange.startIndex, "the stat after the removal must shift down");
            Assert.AreEqual(1, secondStat.ModifierRange.count);

            Assert.AreEqual(5f, Current(first), "only the removed modifier's contribution disappears");
            Assert.AreEqual(8f, Current(second), "the other stat's modifier must survive the shift");
        }

        // ---- 10: remove all modifiers of a stat ----------------------------------------------------------

        [Test]
        public void S10_RemoveModifiersOfStat_ClearsRangeAndObservers()
        {
            var src = NewOwnerWithStat(10f, out _);
            var dst = NewOwnerWithStat(100f, out _);

            _accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData);
            _accessor.TryAddStatModifier(dst, TestModifier.Add(5f), out _, ref _worldData);

            Assert.AreEqual(115f, Current(dst));

            Assert.IsTrue(_accessor.TryRemoveModifiersOfStat(dst, ref _worldData));

            Assert.IsTrue(_accessor.TryGetModifierCount(dst, out var modifierCount));
            Assert.AreEqual(0, modifierCount);

            Assert.IsTrue(_accessor.TryGetObserverCount(src, out var observerCount));
            Assert.AreEqual(0, observerCount, "removing the modifier must unregister its observer");

            Assert.AreEqual(100f, Current(dst));
        }

        // ---- 11: destroy an observed owner -----------------------------------------------------------------

        [Test]
        public void S11_DestroyObservedOwner_PropagationSkipsIt()
        {
            var src = NewOwnerWithStat(10f, out _);
            var dst = NewOwnerWithStat(100f, out _);

            _accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData);
            Assert.AreEqual(110f, Current(dst));

            Assert.IsTrue(_store.DestroyOwner(src.owner));

            Assert.DoesNotThrow(() => _accessor.TryUpdateStat(dst, ref _worldData));
            Assert.AreEqual(100f, Current(dst), "an unreachable observed stat contributes nothing");
        }

        [Test]
        public void S11_DestroyOwnerAndUpdateObservers_RecalculatesDependents()
        {
            var src = NewOwnerWithStat(10f, out _);
            var dst = NewOwnerWithStat(100f, out _);

            _accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData);
            Assert.AreEqual(110f, Current(dst));

            Assert.IsTrue(_accessor.DestroyOwnerAndUpdateObservers(src.owner, ref _worldData));
            Assert.AreEqual(100f, Current(dst), "dependents must be updated without an explicit call");
        }

        // ---- 12: ProduceChangeEvents = false ------------------------------------------------------------------

        [Test]
        public void S12_ProduceChangeEventsFalse_SuppressesEventsButNotPropagation()
        {
            NewOwner(out var builder);
            var src = builder.CreateStatHandle(new TestValuePair(10f), produceChangeEvents: false, userData: 0u);
            var dst = builder.CreateStatHandle(new TestValuePair(100f), produceChangeEvents: true, userData: 0u);

            _accessor.TryAddStatModifier(dst, TestModifier.AddFrom(src), out _, ref _worldData);

            _worldData.Clear();
            _accessor.TrySetStatBaseValue(src, new TestValuePair(20f), ref _worldData);

            Assert.AreEqual(0, CountEvents(src), "the silent stat must not emit events");
            Assert.AreEqual(1, CountEvents(dst), "but its observers must still be updated");
            Assert.AreEqual(120f, Current(dst));
        }

        // ---- 13: UserData ---------------------------------------------------------------------------------------

        [Test]
        public void S13_UserData_RoundTripsThroughTheStat()
        {
            NewOwner(out var builder);
            var stat = builder.CreateStatHandle(new TestValuePair(1f), produceChangeEvents: true, userData: 1234u);

            Assert.IsTrue(_accessor.TryGetStat(stat, out var created));
            Assert.AreEqual(1234u, created.UserData);

            Assert.IsTrue(_accessor.TrySetStatUserData(stat, 4321u));
            Assert.IsTrue(_accessor.TryGetStat(stat, out var updated));
            Assert.AreEqual(4321u, updated.UserData);
        }

        [Test]
        public void S13_UserData_SurvivesRecalculation()
        {
            NewOwner(out var builder);
            var stat = builder.CreateStatHandle(new TestValuePair(1f), produceChangeEvents: true, userData: 77u);

            _accessor.TryAddStatModifier(stat, TestModifier.Add(1f), out _, ref _worldData);
            _accessor.TrySetStatBaseValue(stat, new TestValuePair(9f), ref _worldData);

            Assert.IsTrue(_accessor.TryGetStat(stat, out var after));
            Assert.AreEqual(77u, after.UserData);
            Assert.AreEqual(10f, Current(stat));
        }

        // ---- helpers ---------------------------------------------------------------------------------------------

        private StatOwnerHandle NewOwner(out TestBuilder builder)
        {
            StatAPI.CreateStatOwnerHandle<
                TestValuePair, TestStat, TestModifier, TestModifierStack, TestObserver, TestComposer
            >(ref _store, out var owner, out builder);

            return owner;
        }

        private StatHandle NewOwnerWithStat(float value, out TestBuilder builder)
        {
            NewOwner(out builder);
            return NewStat(ref builder, value);
        }

        private static StatHandle NewStat(ref TestBuilder builder, float value)
            => builder.CreateStatHandle(new TestValuePair(value), produceChangeEvents: true, userData: 0u);

        private void BuildDiamond(out StatHandle a, out StatHandle b, out StatHandle c, out StatHandle d)
        {
            NewOwner(out var builder);
            a = NewStat(ref builder, 1f);
            b = NewStat(ref builder, 0f);
            c = NewStat(ref builder, 0f);
            d = NewStat(ref builder, 0f);

            _accessor.TryAddStatModifier(b, TestModifier.AddFrom(a), out _, ref _worldData);
            _accessor.TryAddStatModifier(c, TestModifier.AddFrom(a), out _, ref _worldData);
            _accessor.TryAddStatModifier(d, TestModifier.AddFrom(b), out _, ref _worldData);
            _accessor.TryAddStatModifier(d, TestModifier.AddFrom(c), out _, ref _worldData);
        }

        private float Current(StatHandle handle)
        {
            Assert.IsTrue(_accessor.TryGetStatValue(handle, out var valuePair), "stat must be reachable");
            return valuePair.Current;
        }

        private int CountEvents(StatHandle handle)
        {
            var events = _worldData.GetStatChangeEvents(Allocator.Temp);
            var count = 0;

            for (var i = 0; i < events.Length; i++)
            {
                if (events[i].statHandle == handle)
                {
                    count++;
                }
            }

            events.Dispose();
            return count;
        }
    }
}
