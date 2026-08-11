using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexionGame.HFSM.Tests;

public sealed class MachineRunnerTests
{
    private enum State { A, B }

    private static HierarchicalStateMachine<RecordingContext, State> BuildOn(MachineRunner runner)
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("Runner")
            .State(State.A)
            .State(State.B)
            .BuildOrThrow();

        return definition.CreateInstance(new RecordingContext(), new MachineOptions { Runner = runner });
    }

    [Test]
    public void Tick_RunsEveryRegisteredMachine()
    {
        var runner = new MachineRunner("Test");
        var m1 = BuildOn(runner);
        var m2 = BuildOn(runner);

        Assert.That(runner.Count, Is.EqualTo(2));

        runner.Tick(0.1f);

        Assert.That(m1.TimeInMachine, Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(m2.TimeInMachine, Is.EqualTo(0.1f).Within(0.0001f));

        m1.Dispose();
        m2.Dispose();
    }

    [Test]
    public void Dispose_UnregistersFromTheRunner()
    {
        var runner = new MachineRunner("Test");
        var machine = BuildOn(runner);

        Assert.That(runner.Count, Is.EqualTo(1));

        machine.Dispose();

        Assert.That(runner.Count, Is.EqualTo(0));
    }

    [Test]
    public void RegisteringDuringTick_IsDeferredToTheEndOfThePass()
    {
        var runner = new MachineRunner("Test");
        var m1 = BuildOn(runner);
        HierarchicalStateMachine<RecordingContext, State> spawned = null;

        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("Spawner")
            .State(State.A, new SpawnOnFirstTick(() => spawned = BuildOn(runner)))
            .BuildOrThrow();

        var spawner = definition.CreateInstance(new RecordingContext(), new MachineOptions { Runner = runner });

        Assert.That(runner.Count, Is.EqualTo(2), "m1 + spawner, not yet the machine spawned inside Tick");

        runner.Tick(0.1f); // spawner's OnUpdate registers a third machine mid-pass

        Assert.That(spawned, Is.Not.Null);
        Assert.That(runner.Count, Is.EqualTo(3), "the deferred registration applied at the end of this pass");
        Assert.That(spawned.TimeInMachine, Is.EqualTo(0f), "created after this pass, so it did not get ticked yet");

        m1.Dispose();
        spawner.Dispose();
        spawned.Dispose();
    }

    [Test]
    public void DisposingDuringTick_IsDeferredButStillAppliedThisPass()
    {
        var runner = new MachineRunner("Test");
        var doomed = BuildOn(runner);

        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("Killer")
            .State(State.A, new DisposeOtherOnFirstTick(doomed))
            .BuildOrThrow();

        var killer = definition.CreateInstance(new RecordingContext(), new MachineOptions { Runner = runner });

        Assert.That(runner.Count, Is.EqualTo(2));

        runner.Tick(0.1f);

        Assert.That(doomed.IsRunning, Is.False);
        Assert.That(runner.Count, Is.EqualTo(1));

        killer.Dispose();
    }

    [Test]
    public void AThrowingMachine_IsCaughtLoggedAndUnregistered_WithoutStoppingOthers()
    {
        var runner = new MachineRunner("Test");
        var thrower = BuildThrowing(runner);
        var healthy = BuildOn(runner);

        // The runner is documented to log loudly rather than swallow silently -- expect exactly
        // what it emits instead of letting the test framework flag it as an unhandled error.
        LogAssert.Expect(LogType.Error, new Regex("threw during Tick and has been unregistered\\."));
        LogAssert.Expect(LogType.Exception, new Regex("deliberate test failure"));

        Assert.DoesNotThrow(() => runner.Tick(0.1f));

        Assert.That(runner.Count, Is.EqualTo(1), "the throwing machine was unregistered");
        Assert.That(healthy.TimeInMachine, Is.EqualTo(0.1f).Within(0.0001f), "the healthy machine still ran");

        healthy.Dispose();
    }

    private static HierarchicalStateMachine<RecordingContext, State> BuildThrowing(MachineRunner runner)
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("Throws")
            .State(State.A, new ThrowsOnUpdate())
            .BuildOrThrow();

        return definition.CreateInstance(new RecordingContext(), new MachineOptions { Runner = runner });
    }

    private sealed class SpawnOnFirstTick : StateBehaviour<RecordingContext>
    {
        private readonly System.Action _spawn;
        private bool _done;

        public SpawnOnFirstTick(System.Action spawn)
        {
            _spawn = spawn;
        }

        protected override void OnUpdate(RecordingContext c, in StateInfo info, float dt)
        {
            if (_done)
            {
                return;
            }

            _done = true;
            _spawn();
        }
    }

    private sealed class DisposeOtherOnFirstTick : StateBehaviour<RecordingContext>
    {
        private readonly HierarchicalStateMachine<RecordingContext, State> _other;
        private bool _done;

        public DisposeOtherOnFirstTick(HierarchicalStateMachine<RecordingContext, State> other)
        {
            _other = other;
        }

        protected override void OnUpdate(RecordingContext c, in StateInfo info, float dt)
        {
            if (_done)
            {
                return;
            }

            _done = true;
            _other.Dispose();
        }
    }

    private sealed class ThrowsOnUpdate : StateBehaviour<RecordingContext>
    {
        protected override void OnUpdate(RecordingContext c, in StateInfo info, float dt)
            => throw new System.InvalidOperationException("deliberate test failure");
    }
}
