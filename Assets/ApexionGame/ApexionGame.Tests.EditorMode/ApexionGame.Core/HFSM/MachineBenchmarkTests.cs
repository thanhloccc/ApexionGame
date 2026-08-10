using System;
using System.Diagnostics;
using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

/// <summary>
/// A measurement, not a pass/fail check on timing — same stance as the Stats module's own
/// benchmark. Deliberately <b>not</b> <c>[Explicit]</c>: Unity's test runner skips explicit tests
/// even when named directly in a <c>--filter</c>, which would put these numbers out of reach of
/// <c>unity test</c> entirely, and the whole class runs in well under two seconds.
/// </summary>
/// <remarks>
/// The one HARD assertion is zero GC allocation on a warmed-up, steady-state tick — one of the
/// four qualities this module was built for, checked rather than assumed.
/// </remarks>
[Category("Benchmark")]
public sealed class MachineBenchmarkTests
{
    private const int MACHINE_COUNT = 500;
    private const int TICKS = 1000;

    private enum State { Idle, Patrol, Combat, Chase, Attack, Flee }

    private sealed class Context
    {
        public bool SeesPlayer;
        public float DistanceToPlayer = float.MaxValue;
        public float Health = 100f;
    }

    private struct SmallData
    {
        public float Timer;
        public int Ticks;
        public long Flags;
    }

    private sealed class Behaviour : StateBehaviour<Context, SmallData>
    {
        protected override void OnUpdate(Context c, ref SmallData data, in StateInfo info, float dt)
        {
            data.Timer += dt;
            data.Ticks++;
        }
    }

    private static MachineDefinition<Context, State> BuildDefinition()
    {
        return HierarchicalStateMachine<Context, State>.Define("Benchmark")
            .AnyState()
                .To(State.Flee).When(static c => c.Health < 20f)
            .State(State.Idle, new Behaviour())
                .To(State.Patrol).After(2f)
            .State(State.Patrol, new Behaviour())
                .To(State.Combat).When(static c => c.SeesPlayer)
            .Composite(State.Combat, new Behaviour())
                .Initial(State.Chase)
                .Child(State.Chase, new Behaviour())
                    .To(State.Attack).When(static c => c.DistanceToPlayer < 2f)
                .Child(State.Attack, new Behaviour())
            .EndComposite()
            .State(State.Flee, new Behaviour())
            .BuildOrThrow();
    }

    [Test]
    public void B01_BuildDefinition()
    {
        var sw = Stopwatch.StartNew();
        var definition = BuildDefinition();
        sw.Stop();

        Report("build definition", sw, 1);
        TestContext.WriteLine($"  {definition.NodeCount} nodes, {definition.StateDataSize} bytes state data/instance");
    }

    [Test]
    public void B02_CreateInstances()
    {
        var definition = BuildDefinition();
        var machines = new HierarchicalStateMachine<Context, State>[MACHINE_COUNT];

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i] = definition.CreateInstance(new Context(), TickMode.Manual);
        }

        sw.Stop();
        Report("create instance", sw, MACHINE_COUNT);

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Dispose();
        }
    }

    [Test]
    public void B03_SteadyStateTick_NoTransitions()
    {
        var definition = BuildDefinition();
        var machines = new HierarchicalStateMachine<Context, State>[MACHINE_COUNT];

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i] = definition.CreateInstance(new Context(), TickMode.Manual);
        }

        var sw = Stopwatch.StartNew();

        for (var t = 0; t < TICKS; t++)
        {
            for (var i = 0; i < MACHINE_COUNT; i++)
            {
                machines[i].Tick(0.016f);
            }
        }

        sw.Stop();
        Report("steady-state tick", sw, MACHINE_COUNT * TICKS);

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Dispose();
        }
    }

    [Test]
    public void B04_TickWithSomeTransitionsEachPass()
    {
        var definition = BuildDefinition();
        var machines = new HierarchicalStateMachine<Context, State>[MACHINE_COUNT];

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i] = definition.CreateInstance(new Context(), TickMode.Manual);
        }

        var sw = Stopwatch.StartNew();

        for (var t = 0; t < TICKS; t++)
        {
            for (var i = 0; i < MACHINE_COUNT; i++)
            {
                var context = machines[i].Context;

                // Every tenth machine toggles SeesPlayer this pass, giving roughly 10% of the
                // population a transition to resolve instead of a pure guard-poll.
                if ((t + i) % 10 == 0)
                {
                    context.SeesPlayer = !context.SeesPlayer;
                }

                machines[i].Tick(0.016f);
            }
        }

        sw.Stop();
        Report("tick, ~10% transitioning", sw, MACHINE_COUNT * TICKS);

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Dispose();
        }
    }

    [Test]
    public void B05_DisposeThenRecreate_ReusesThePool()
    {
        var definition = BuildDefinition();
        var machines = new HierarchicalStateMachine<Context, State>[MACHINE_COUNT];

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i] = definition.CreateInstance(new Context(), TickMode.Manual);
        }

        var disposeSw = Stopwatch.StartNew();

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Dispose();
        }

        disposeSw.Stop();
        Report("dispose", disposeSw, MACHINE_COUNT);
        Assert.That(definition.PooledCount, Is.EqualTo(MACHINE_COUNT));

        var recreateSw = Stopwatch.StartNew();

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i] = definition.CreateInstance(new Context(), TickMode.Manual);
        }

        recreateSw.Stop();
        Report("recreate (from pool)", recreateSw, MACHINE_COUNT);

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Dispose();
        }
    }

    [Test]
    public void B06_SteadyStateTick_AllocatesNoGCMemory()
    {
        var definition = BuildDefinition();
        var machines = new HierarchicalStateMachine<Context, State>[MACHINE_COUNT];

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i] = definition.CreateInstance(new Context(), TickMode.Manual);
        }

        // Warm up: first calls into a code path can allocate for reasons that have nothing to do
        // with steady-state behaviour (JIT, static initialisers). Measure only what runs after.
        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Tick(0.016f);
        }

        // GC.GetAllocatedBytesForCurrentThread rather than the Unity Test Framework's
        // Is.Not.AllocatingGCMemory(): a plain BCL API, so this assertion does not depend on
        // whichever assembly happens to carry UnityEngine.TestTools.Constraints in a given
        // package version — the Stats module's own benchmark hit the same friction and settled
        // for Stopwatch-only reporting; this gets the hard zero-alloc guarantee back without the
        // dependency.
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Tick(0.016f);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0), $"steady-state tick allocated {allocated} bytes");

        for (var i = 0; i < MACHINE_COUNT; i++)
        {
            machines[i].Dispose();
        }
    }

    private static void Report(string what, Stopwatch sw, int operations)
    {
        var ms = sw.Elapsed.TotalMilliseconds;
        var perOp = ms * 1000d / operations;

        TestContext.WriteLine($"{what,-28} {ms,10:F1} ms   {perOp,8:F3} us/op   ({operations} ops)");
    }
}
