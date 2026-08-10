using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ApexionGame.HFSM.Debugging;
using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

/// <summary>
/// Debugging §7 — a debugger that lies is worse than none, so the debug layer gets its own tests
/// rather than trust by construction.
/// </summary>
public sealed class MachineDebugTests
{
    private enum State { Idle, Combat, Patrol, Flee }

    private enum Trigger { Go }

    [Test]
    public void Registry_MachineAppearsAndDisappears()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("DebugRegistry")
            .State(State.Idle)
            .BuildOrThrow();

        var before = MachineDebugRegistry.Machines.Count;
        var machine = definition.CreateInstance(new RecordingContext(), TickMode.Manual);

        Assert.That(MachineDebugRegistry.Machines.Contains(machine), Is.True);

        machine.Dispose();

        Assert.That(MachineDebugRegistry.Machines.Contains(machine), Is.False);
        Assert.That(MachineDebugRegistry.Machines.Count, Is.EqualTo(before));
    }

    [Test]
    public void Log_RingBufferWrapsWithoutAllocation()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("DebugLogWrap")
            .State(State.Idle)
                .To(State.Combat).When(static c => c.Flag)
            .State(State.Combat)
                .To(State.Idle).When(static c => c.Flag == false)
            .BuildOrThrow();

        var context = new RecordingContext();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        var debug = (IMachineDebug)machine;

        // Warm up so the ring buffer's backing array is already allocated before measuring.
        for (var i = 0; i < 4; i++)
        {
            context.Flag = context.Flag == false;
            machine.Tick(0.01f);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 10_000; i++)
        {
            context.Flag = context.Flag == false;
            machine.Tick(0.01f);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0), $"10 000 transitions allocated {allocated} bytes");

        var log = new List<TransitionLogEntry>();
        debug.GetLog(log);
        Assert.That(log.Count, Is.EqualTo(32));

        machine.Dispose();
    }

    [Test]
    public void Log_RecordsCauseAndSourceDuration()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("DebugLogCause")
            .State(State.Idle)
                .To(State.Combat).When(static c => c.Flag)
            .State(State.Combat)
            .BuildOrThrow();

        var context = new RecordingContext();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        var debug = (IMachineDebug)machine;

        machine.Tick(0.5f);
        machine.Tick(0.25f);

        context.Flag = true;
        machine.Tick(0.1f); // Idle -> Combat fires on this tick

        var log = new List<TransitionLogEntry>();
        debug.GetLog(log);

        var last = log[^1];
        Assert.That(last.Cause, Is.EqualTo(TransitionCause.Guard));
        Assert.That(last.TransitionIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(last.TimeInSource, Is.EqualTo(0.85f).Within(0.0001f));

        machine.Dispose();
    }

    [Test]
    public void Log_UnmatchedTriggerIsRecorded()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("DebugLogUnmatched")
            .State(State.Idle)
            .BuildOrThrow();

        var machine = definition.CreateInstance(new RecordingContext(), TickMode.Manual);
        var debug = (IMachineDebug)machine;

        machine.Fire(Trigger.Go); // Idle arms nothing on Go -- dropped as unmatched
        machine.Tick(0.1f);

        var log = new List<TransitionLogEntry>();
        debug.GetLog(log);

        var last = log[^1];
        Assert.That(last.IsUnmatchedTrigger, Is.True);
        Assert.That(last.Cause, Is.EqualTo(TransitionCause.UnmatchedTrigger));

        machine.Dispose();
    }

    [Test]
    public void Transitions_ReportsEveryBakedEdge()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("DebugTransitions")
            .AnyState()
                .To(State.Flee).When(static c => c.Number > 10f)
            .State(State.Idle)
                .To(State.Combat).When(static c => c.Flag)
            .State(State.Combat)
            .State(State.Flee)
            .BuildOrThrow();

        var machine = definition.CreateInstance(new RecordingContext(), TickMode.Manual);
        var debug = (IMachineDebug)machine;

        var transitions = new List<TransitionDebugInfo>();
        debug.GetTransitions(transitions);

        Assert.That(transitions.Count, Is.EqualTo(2));
        Assert.That(transitions.Count(t => t.IsAnyState), Is.EqualTo(1));
        Assert.That(
              transitions.Single(t => t.IsAnyState == false).Target
            , Is.EqualTo(machine.Definition.IndexOf(State.Combat))
        );

        machine.Dispose();
    }

    [Test]
    public void Guards_EvaluationDoesNotMutateMachine()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("DebugGuardsPure")
            .AnyState()
                .To(State.Flee).When(static c => c.Number > 10f)
            .State(State.Idle)
                .To(State.Combat).When(static c => c.Flag)
            .State(State.Combat)
            .State(State.Flee)
            .BuildOrThrow();

        var context = new RecordingContext();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        var debug = (IMachineDebug)machine;

        var stateBefore = machine.CurrentState;
        var timeBefore = machine.TimeInState;

        var results = new List<GuardDebugInfo>();

        for (var i = 0; i < 50; i++)
        {
            results.Clear();
            debug.GetOutgoingGuards(machine.CurrentNode, results);
        }

        Assert.That(machine.CurrentState, Is.EqualTo(stateBefore));
        Assert.That(machine.TimeInState, Is.EqualTo(timeBefore));
        Assert.That(machine.Phase, Is.EqualTo(MachinePhase.Idle));
        Assert.That(results.Count, Is.EqualTo(2), "AnyState->Flee, Idle->Combat");

        machine.Dispose();
    }

    [Test]
    public void Guards_OrderMatchesTickOrder()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("DebugGuardsOrder")
            .AnyState()
                .To(State.Flee).When(static c => c.Number > 10f)
            .State(State.Idle)
                .To(State.Combat).Priority(5).When(static c => c.Flag)
                .To(State.Patrol).Priority(1).When(static c => true)
            .State(State.Combat)
            .State(State.Patrol)
            .State(State.Flee)
            .BuildOrThrow();

        var context = new RecordingContext { Flag = true };
        var machine = definition.CreateInstance(context, TickMode.Manual);
        var debug = (IMachineDebug)machine;

        var guards = new List<GuardDebugInfo>();
        debug.GetOutgoingGuards(machine.CurrentNode, guards);

        // AnyState (false) evaluates first; then Idle's own transitions in priority order --
        // Combat wins, so Patrol is reported but never actually evaluated.
        Assert.That(guards.Count, Is.EqualTo(3));
        Assert.That(guards[0].IsAnyState, Is.True);
        Assert.That(guards[0].Marker, Is.EqualTo(GuardMarker.False));
        Assert.That(guards[1].Target, Is.EqualTo(machine.Definition.IndexOf(State.Combat)));
        Assert.That(guards[1].Marker, Is.EqualTo(GuardMarker.Eligible));
        Assert.That(guards[2].Target, Is.EqualTo(machine.Definition.IndexOf(State.Patrol)));
        Assert.That(guards[2].Marker, Is.EqualTo(GuardMarker.NotEvaluated));

        machine.Tick(0.1f);

        Assert.That(machine.CurrentState, Is.EqualTo(State.Combat));

        machine.Dispose();
    }

    /// <summary>
    /// Stands in for "compiled with <c>APEXION_HFSM_DEBUG</c> off, the registry stays empty and the
    /// log array is null" -- that scenario needs a player build with the define stripped, out of
    /// reach of an EditMode test where <c>UNITY_EDITOR</c> is always defined. This instead guards
    /// the mechanism the claim depends on: the registration and logging methods must still carry
    /// the <c>[Conditional]</c> gate, so a regression that quietly drops it is caught here.
    /// </summary>
    [Test]
    public void DebugGatedMethods_CarryConditionalAttributes()
    {
        var type = typeof(HierarchicalStateMachine<RecordingContext, State>);
        const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;

        foreach (var name in new[] {
            "RegisterForDebugging", "UnregisterFromDebugging", "ClearLog",
            "RecordTransition", "RecordUnmatchedTrigger",
        })
        {
            var method = type.GetMethod(name, FLAGS);
            Assert.That(method, Is.Not.Null, $"{name} not found");

            var gated = method.GetCustomAttributes<ConditionalAttribute>()
                .Any(a => a.ConditionString == ValidationDefines.HFSM_DEBUG);

            Assert.That(gated, Is.True, $"{name} must carry [Conditional(ValidationDefines.HFSM_DEBUG)]");
        }
    }
}
