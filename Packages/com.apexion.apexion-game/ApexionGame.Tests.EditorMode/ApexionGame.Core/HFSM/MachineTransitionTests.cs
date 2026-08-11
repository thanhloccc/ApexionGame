using System.Collections.Generic;
using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

public sealed class MachineTransitionTests
{
    private enum State { A, B, C, D, E }

    private sealed class Context
    {
        public readonly List<string> Log = new();
        public bool Go;
        public int DoCount;
    }

    private sealed class Recorder : StateBehaviour<Context>
    {
        private readonly string _name;

        public Recorder(string name)
        {
            _name = name;
        }

        protected override void OnEnter(Context c, in StateInfo info) => c.Log.Add($"Enter:{_name}");

        protected override void OnExit(Context c, in StateInfo info) => c.Log.Add($"Exit:{_name}");
    }

    [Test]
    public void FourLevelTree_TransitionBetweenDeepLeavesUsesCorrectLca()
    {
        // Root -> A (composite) -> B (composite) -> {C, D}; Root -> E (leaf)
        var definition = HierarchicalStateMachine<Context, State>.Define("Deep")
            .Composite(State.A, new Recorder("A"))
                .Composite(State.B, new Recorder("B"))
                    .Child(State.C, new Recorder("C"))
                        .To(State.D).When(static c => c.Go)
                    .Child(State.D, new Recorder("D"))
                .EndComposite()
            .EndComposite()
            .State(State.E, new Recorder("E"))
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        context.Go = true;
        machine.Tick(0.1f);

        // LCA(C, D) = B. Only C exits and D enters; A and B never leave the active configuration.
        Assert.That(context.Log, Is.EqualTo(new[] { "Exit:C", "Enter:D" }));

        machine.Dispose();
    }

    [Test]
    public void SelfTransition_External_ExitsAndReenters()
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("Self")
            .State(State.A, new Recorder("A"))
                .To(State.A).When(static c => c.Go)
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        context.Go = true;
        machine.Tick(0.1f);

        Assert.That(context.Log, Is.EqualTo(new[] { "Exit:A", "Enter:A" }));

        machine.Dispose();
    }

    [Test]
    public void SelfTransition_Internal_RunsOnlyTheAction_NoExitOrEnter()
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("Internal")
            .State(State.A, new Recorder("A"))
                .To(State.A).When(static c => c.Go).Internal().Do(static c => c.DoCount++)
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        context.Go = true;
        machine.Tick(0.1f);

        Assert.That(context.Log, Is.Empty);
        Assert.That(context.DoCount, Is.EqualTo(1));

        machine.Dispose();
    }

    [Test]
    public void TargetingACompositeDirectly_DescendsToItsInitialChild()
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("TargetComposite")
            .State(State.E, new Recorder("E"))
                .To(State.A).When(static c => c.Go)
            .Composite(State.A, new Recorder("A"))
                .Initial(State.C)
                .Child(State.C, new Recorder("C"))
                .Child(State.D, new Recorder("D"))
            .EndComposite()
            .BuildOrThrow();

        var context = new Context();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        context.Go = true;
        machine.Tick(0.1f);

        Assert.That(context.Log, Is.EqualTo(new[] { "Exit:E", "Enter:A", "Enter:C" }));
        Assert.That(machine.CurrentState, Is.EqualTo(State.C));

        machine.Dispose();
    }

    [Test]
    public void MinDuration_BlocksThenReleases()
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("MinDuration")
            .State(State.A, new Recorder("A"))
                .To(State.E).When(static c => c.Go).MinDuration(1f)
            .State(State.E, new Recorder("E"))
            .BuildOrThrow();

        var context = new Context { Go = true };
        var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        machine.Tick(0.5f);
        Assert.That(machine.CurrentState, Is.EqualTo(State.A), "blocked before 1s elapsed");

        machine.Tick(0.6f);
        Assert.That(machine.CurrentState, Is.EqualTo(State.E), "released once 1s elapsed");

        machine.Dispose();
    }

    [Test]
    public void Priority_BeatsDeclarationOrder_WithinTheSameNode()
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("Priority")
            .State(State.A, new Recorder("A"))
                .To(State.C).When(static c => c.Go)
                .To(State.D).When(static c => c.Go).Priority(10)
            .State(State.C, new Recorder("C"))
            .State(State.D, new Recorder("D"))
            .BuildOrThrow();

        var context = new Context { Go = true };
        var machine = definition.CreateInstance(context, TickMode.Manual);

        machine.Tick(0.1f);

        Assert.That(machine.CurrentState, Is.EqualTo(State.D));

        machine.Dispose();
    }

    [Test]
    public void DeeperNode_BeatsShallowerNode_InTheSameTick()
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("DepthPriority")
            .Composite(State.A)
                .To(State.E).When(static c => c.Go) // shallow: Root-owned via A's own transition
                .Initial(State.C)
                .Child(State.C, new Recorder("C"))
                    .To(State.D).When(static c => c.Go) // deep: C's own transition
                .Child(State.D, new Recorder("D"))
            .EndComposite()
            .State(State.E, new Recorder("E"))
            .BuildOrThrow();

        var context = new Context { Go = true };
        var machine = definition.CreateInstance(context, TickMode.Manual);

        machine.Tick(0.1f);

        Assert.That(machine.CurrentState, Is.EqualTo(State.D), "C's own transition is deeper than A's");

        machine.Dispose();
    }
}
