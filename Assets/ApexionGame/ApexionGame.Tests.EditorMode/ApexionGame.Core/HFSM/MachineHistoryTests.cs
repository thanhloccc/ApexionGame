using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

public sealed class MachineHistoryTests
{
    private enum State { Outside, Combat, Ranged, Aim, Fire, Melee }

    /// <remarks>
    /// Driven entirely through <c>RequestTransitionOrError</c> rather than declared triggers, so
    /// each test moves the machine to an exact state without needing an edge declared for every
    /// step — history reacts the same way regardless of what caused the transition.
    /// </remarks>
    private static HierarchicalStateMachine<RecordingContext, State> Build(HistoryMode mode)
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("History")
            .State(State.Outside)
            .Composite(State.Combat).WithHistory(mode)
                .Initial(State.Ranged)
                .Composite(State.Ranged)
                    .Initial(State.Aim)
                    .Child(State.Aim)
                    .Child(State.Fire)
                .EndComposite()
                .Child(State.Melee)
            .EndComposite()
            .BuildOrThrow();

        return definition.CreateInstance(new RecordingContext(), TickMode.Manual);
    }

    private static void GoTo(HierarchicalStateMachine<RecordingContext, State> machine, State target)
    {
        var result = machine.RequestTransitionOrError(target);
        Assert.That(result.IsSuccess, Is.True, $"request to {target} should be accepted");
        machine.Tick(0.1f);
    }

    [Test]
    public void NoHistory_AlwaysReentersAtInitial()
    {
        var machine = Build(HistoryMode.None);

        GoTo(machine, State.Fire);
        GoTo(machine, State.Outside);
        GoTo(machine, State.Combat);

        Assert.That(machine.CurrentState, Is.EqualTo(State.Aim));

        machine.Dispose();
    }

    [Test]
    public void Shallow_RestoresTheDirectChild_ThenThatChildsOwnInitial()
    {
        var machine = Build(HistoryMode.Shallow);

        GoTo(machine, State.Fire); // Combat/Ranged/Fire
        GoTo(machine, State.Melee); // Combat's direct child becomes Melee; Ranged is exited with Fire remembered
        GoTo(machine, State.Outside); // Combat's direct child becomes Melee (recorded on this exit)
        GoTo(machine, State.Combat);

        // Shallow remembers Combat's direct child (Melee's sibling Ranged was NOT active when Combat
        // was last exited) -- wait, re-derive: the LAST time Combat was exited, its active child was
        // Melee, so Combat's history is Melee, not Ranged/Fire. Re-entering Combat must land on Melee.
        Assert.That(machine.CurrentState, Is.EqualTo(State.Melee));

        machine.Dispose();
    }

    [Test]
    public void Shallow_DoesNotReachBelowTheDirectChild()
    {
        var machine = Build(HistoryMode.Shallow);

        GoTo(machine, State.Fire); // Combat/Ranged/Fire
        GoTo(machine, State.Outside); // Combat's direct child recorded as Ranged -- but not Fire
        GoTo(machine, State.Combat);

        // Shallow restores Combat's direct child (Ranged), then Ranged uses its OWN Initial (Aim),
        // because Ranged's own history was never told to look back -- only Combat's was.
        Assert.That(machine.CurrentState, Is.EqualTo(State.Aim));

        machine.Dispose();
    }

    [Test]
    public void Deep_RestoresTheWholePath_DownToTheLeaf()
    {
        var machine = Build(HistoryMode.Deep);

        GoTo(machine, State.Fire); // Combat/Ranged/Fire
        GoTo(machine, State.Outside);
        GoTo(machine, State.Combat);

        // Deep restores the full remembered path: Combat/Ranged/Fire, not Ranged's own Initial.
        Assert.That(machine.CurrentState, Is.EqualTo(State.Fire));

        machine.Dispose();
    }

    [Test]
    public void ExplicitTarget_OverridesHistory()
    {
        var machine = Build(HistoryMode.Deep);

        GoTo(machine, State.Fire);
        GoTo(machine, State.Outside);

        // Targeting Melee directly must win over Combat's remembered Ranged/Fire path.
        GoTo(machine, State.Melee);

        Assert.That(machine.CurrentState, Is.EqualTo(State.Melee));

        machine.Dispose();
    }

    [Test]
    public void Reset_ClearsHistory()
    {
        var machine = Build(HistoryMode.Deep);

        GoTo(machine, State.Fire);

        machine.Reset();
        Assert.That(machine.CurrentState, Is.EqualTo(State.Outside));

        GoTo(machine, State.Combat);

        Assert.That(machine.CurrentState, Is.EqualTo(State.Aim), "history was cleared by Reset");

        machine.Dispose();
    }
}
