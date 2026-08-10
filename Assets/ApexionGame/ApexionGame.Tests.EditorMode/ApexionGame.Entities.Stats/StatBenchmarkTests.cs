using System.Diagnostics;
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
    /// Task 4.4 — 10k owners x 8 stats x 4 modifiers.
    /// </summary>
    /// <remarks>
    /// A measurement, not a pass/fail check — the assertions only guard against the world failing to
    /// build. Deliberately <b>not</b> <c>[Explicit]</c>: the whole class runs in under two seconds,
    /// and Unity's test runner skips explicit tests even when you name them in a filter, which put
    /// the numbers out of reach of `unity test` entirely.
    /// <para>
    /// Read the numbers as <b>relative</b> only. Editor tests run under Mono with safety checks on
    /// and Burst off, so absolute timings are several times worse than a Burst-compiled player.
    /// What they are good for is comparing one revision against the next, and catching an operation
    /// that is accidentally O(world) instead of O(owner).
    /// </para>
    /// </remarks>
    [Category("Benchmark")]
    public sealed class StatBenchmarkTests
    {
        private const int OWNERS = 10_000;
        private const int STATS_PER_OWNER = 8;
        private const int MODIFIERS_PER_STAT = 4;

        private TestStore _store;
        private TestAccessor _accessor;
        private TestWorldData _worldData;
        private NativeList<StatOwnerHandle> _owners;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStore(OWNERS, Allocator.Persistent);
            _accessor = new TestAccessor(_store);
            _worldData = new TestWorldData(1024, Allocator.Persistent);
            _owners = new NativeList<StatOwnerHandle>(OWNERS, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            _owners.Dispose();
            _worldData.Dispose();
            _store.Dispose();
        }

        [Test]
        public void B01_Build()
        {
            var sw = Stopwatch.StartNew();
            BuildWorld();
            sw.Stop();

            var stats = OWNERS * STATS_PER_OWNER;
            var modifiers = stats * MODIFIERS_PER_STAT;

            Report("build world", sw, modifiers);
            TestContext.WriteLine($"  {OWNERS} owners, {stats} stats, {modifiers} modifiers");
        }

        /// <remarks>
        /// The common gameplay write. Each owner is a self-contained chain, so this must scale with
        /// the number of owners and not with the size of the world.
        /// </remarks>
        [Test]
        public void B02_SetBaseValue_OnEveryOwner()
        {
            BuildWorld();
            _worldData.Clear();

            var sw = Stopwatch.StartNew();

            for (var i = 0; i < _owners.Length; i++)
            {
                _accessor.TrySetStatBaseValue(
                    new StatHandle(_owners[i], 1), new TestValuePair(50f + (i & 15)), ref _worldData);
            }

            sw.Stop();
            Report("set base value + propagate", sw, OWNERS);
        }

        [Test]
        public void B03_UpdateAllStats_OnEveryOwner()
        {
            BuildWorld();
            _worldData.Clear();

            var sw = Stopwatch.StartNew();

            for (var i = 0; i < _owners.Length; i++)
            {
                _accessor.TryUpdateAllStats(_owners[i], ref _worldData);
            }

            sw.Stop();
            Report("update all stats", sw, OWNERS);
        }

        [Test]
        public void B04_ReadEveryStat()
        {
            BuildWorld();

            var sw = Stopwatch.StartNew();
            var sum = 0f;

            for (var i = 0; i < _owners.Length; i++)
            {
                for (var s = 1; s <= STATS_PER_OWNER; s++)
                {
                    if (_accessor.TryGetStatValue(new StatHandle(_owners[i], s), out var valuePair))
                    {
                        sum += valuePair.Current;
                    }
                }
            }

            sw.Stop();
            Report("read stat value", sw, OWNERS * STATS_PER_OWNER);
            TestContext.WriteLine($"  checksum {sum}");
        }

        /// <remarks>
        /// D-100 claims destroy only clears the buffers and pushes the slot onto a free list, so a
        /// second wave of owners reuses the allocations. If that holds, the recreate pass is
        /// noticeably cheaper than the first build measured by B01.
        /// </remarks>
        [Test]
        public void B05_DestroyThenRecreate_ReusesSlots()
        {
            BuildWorld();

            var destroy = Stopwatch.StartNew();

            for (var i = 0; i < _owners.Length; i++)
            {
                _store.DestroyOwner(_owners[i]);
            }

            destroy.Stop();
            _owners.Clear();

            var recreate = Stopwatch.StartNew();
            BuildWorld();
            recreate.Stop();

            Report("destroy owner", destroy, OWNERS);
            Report("recreate owner (warm)", recreate, OWNERS * STATS_PER_OWNER * MODIFIERS_PER_STAT);
        }

        // ---- helpers -------------------------------------------------------------------------

        /// <summary>
        /// Each owner gets a chain: stat 1 is the source, stats 2..8 each observe the one before it,
        /// and every stat carries <see cref="MODIFIERS_PER_STAT"/> modifiers.
        /// </summary>
        private void BuildWorld()
        {
            for (var i = 0; i < OWNERS; i++)
            {
                StatAPI.CreateStatOwnerHandle<
                    TestValuePair, TestStat, TestModifier, TestModifierStack, TestObserver, TestComposer
                >(ref _store, out var owner, out TestBuilder builder, STATS_PER_OWNER + 1
                    , STATS_PER_OWNER * MODIFIERS_PER_STAT, STATS_PER_OWNER);

                _owners.Add(owner);

                var previous = default(StatHandle);

                for (var s = 0; s < STATS_PER_OWNER; s++)
                {
                    var handle = builder.CreateStatHandle(
                        new TestValuePair(10f + s), produceChangeEvents: true, userData: 0u);

                    // One modifier links this stat to the previous one; the rest are flat.
                    var linked = s > 0;

                    for (var m = 0; m < MODIFIERS_PER_STAT; m++)
                    {
                        var modifier = linked && m == 0
                            ? TestModifier.AddFrom(previous)
                            : TestModifier.Add(1f + m);

                        _accessor.TryAddStatModifier(handle, modifier, out _, ref _worldData);
                    }

                    previous = handle;
                }
            }
        }

        private static void Report(string what, Stopwatch sw, int operations)
        {
            var ms = sw.Elapsed.TotalMilliseconds;
            var perOp = sw.Elapsed.TotalMilliseconds * 1000d / operations;

            TestContext.WriteLine($"{what,-28} {ms,10:F1} ms   {perOp,8:F3} us/op   ({operations} ops)");
        }
    }
}
