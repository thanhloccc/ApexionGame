using System.Collections.Generic;
using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

/// <summary>
/// The golden path from <c>HFSM - Flows.md §10</c>, asserted as one exact transcript per tick — not
/// a "contains" check. An ordering bug that produces the right calls in the wrong order is exactly
/// what "contains" would miss.
/// </summary>
public sealed class MachineOrderingTests
{
    private enum State { Idle, Patrol, Combat, Chase, Attack, Flee }

    private enum Trigger { Healed }

    private sealed class Context
    {
        public readonly List<string> Log = new();
        public bool SeesPlayer;
        public float DistanceToPlayer = float.MaxValue;
        public float Health = 100f;
    }

    private sealed class Recorder : StateBehaviour<Context>
    {
        private readonly string _name;

        public Recorder(string name)
        {
            _name = name;
        }

        protected override void OnEnter(Context c, in StateInfo info) => c.Log.Add($"Enter:{_name}");

        protected override void OnUpdate(Context c, in StateInfo info, float dt) => c.Log.Add($"Update:{_name}");

        protected override void OnExit(Context c, in StateInfo info) => c.Log.Add($"Exit:{_name}");
    }

    private static HierarchicalStateMachine<Context, State> Build(Context context)
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("Lifecycle")
            .AnyState()
                .To(State.Flee).When(static c => c.Health < 20f)
            .State(State.Idle, new Recorder("Idle"))
                .To(State.Combat).When(static c => c.SeesPlayer)
            .State(State.Patrol, new Recorder("Patrol"))
            .Composite(State.Combat, new Recorder("Combat")).WithHistory(HistoryMode.Shallow)
                .Initial(State.Chase)
                .Child(State.Chase, new Recorder("Chase"))
                    .To(State.Attack).When(static c => c.DistanceToPlayer < 2f)
                .Child(State.Attack, new Recorder("Attack"))
            .EndComposite()
            .State(State.Flee, new Recorder("Flee"))
                .To(State.Combat).On(Trigger.Healed)
            .BuildOrThrow();

        return definition.CreateInstance(context, TickMode.Manual);
    }

    [Test]
    public void FullLifecycle_OrderIsExact()
    {
        var context = new Context();
        var machine = Build(context);

        Assert.That(context.Log, Is.EqualTo(new[] { "Enter:Idle" }), "Start");
        context.Log.Clear();

        machine.Tick(0.1f);
        Assert.That(context.Log, Is.EqualTo(new[] { "Update:Idle" }), "steady state");
        context.Log.Clear();

        context.SeesPlayer = true;
        machine.Tick(0.1f);
        Assert.That(context.Log, Is.EqualTo(new[] {
            "Exit:Idle", "Enter:Combat", "Enter:Chase", "Update:Combat", "Update:Chase",
        }), "Idle -> Combat/Chase (Initial, no history yet)");
        context.Log.Clear();

        context.DistanceToPlayer = 1f;
        machine.Tick(0.1f);
        Assert.That(context.Log, Is.EqualTo(new[] {
            "Exit:Chase", "Enter:Attack", "Update:Combat", "Update:Attack",
        }), "Chase -> Attack: LCA is Combat, Combat is not exited");
        context.Log.Clear();

        context.Health = 10f;
        machine.Tick(0.1f);
        Assert.That(context.Log, Is.EqualTo(new[] {
            "Exit:Attack", "Exit:Combat", "Enter:Flee", "Update:Flee",
        }), "AnyState -> Flee: history(Combat) is recorded as Attack before OnExit");
        context.Log.Clear();

        machine.Fire(Trigger.Healed);
        machine.Tick(0.1f);
        Assert.That(context.Log, Is.EqualTo(new[] {
            "Exit:Flee", "Enter:Combat", "Enter:Attack", "Update:Combat", "Update:Attack",
        }), "Flee -> Combat: shallow history restores Attack, not Initial's Chase");

        machine.Dispose();
    }

    [Test]
    public void SiblingTransition_DoesNotReenterTheComposite()
    {
        var context = new Context { SeesPlayer = true };
        var machine = Build(context);
        machine.Tick(0.1f); // settle into Combat/Chase
        context.Log.Clear();

        context.DistanceToPlayer = 1f;
        machine.Tick(0.1f);

        Assert.That(context.Log, Has.No.Member("Enter:Combat"));
        Assert.That(context.Log, Has.No.Member("Exit:Combat"));

        machine.Dispose();
    }

    [Test]
    public void UpdateOrder_MatchesEntryOrder_OutermostFirst()
    {
        var context = new Context { SeesPlayer = true };
        var machine = Build(context);
        machine.Tick(0.1f);
        context.Log.Clear();

        machine.Tick(0.1f);

        Assert.That(context.Log, Is.EqualTo(new[] { "Update:Combat", "Update:Chase" }));

        machine.Dispose();
    }
}
