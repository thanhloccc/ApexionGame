using System.Collections.Generic;
using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

public sealed class MachineParallelTests
{
    private enum State
    {
        Idle,
        Fighter,
        MoveRegion, MoveIdle, MoveWalk,
        WeaponRegion, WeaponHolstered, WeaponDrawn,
        BuffRegion, BuffNone, BuffHasted,
        Stunned,
    }

    private enum Trigger { Hit }

    private sealed class Context
    {
        public readonly List<string> Log = new();
        public bool Go;
        public bool GoWeapon;
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

    private static HierarchicalStateMachine<Context, State> Build()
    {
        var definition = HierarchicalStateMachine<Context, State>.Define("Fighter")
            .AnyState()
                .To(State.Stunned).On(Trigger.Hit).Priority(100)
            .State(State.Idle, new Recorder("Idle"))
                .To(State.Fighter).When(static c => c.Go)
            .Parallel(State.Fighter, new Recorder("Fighter"))
                .Region(State.MoveRegion)
                    .Initial(State.MoveIdle)
                    .Child(State.MoveIdle, new Recorder("MoveIdle"))
                        .To(State.MoveWalk).When(static c => c.Go)
                    .Child(State.MoveWalk, new Recorder("MoveWalk"))
                .EndRegion()
                .Region(State.WeaponRegion)
                    .Initial(State.WeaponHolstered)
                    .Child(State.WeaponHolstered, new Recorder("WeaponHolstered"))
                        .To(State.WeaponDrawn).When(static c => c.GoWeapon)
                    .Child(State.WeaponDrawn, new Recorder("WeaponDrawn"))
                .EndRegion()
                .Region(State.BuffRegion)
                    .Initial(State.BuffNone)
                    .Child(State.BuffNone, new Recorder("BuffNone"))
                    .Child(State.BuffHasted, new Recorder("BuffHasted"))
                .EndRegion()
            .EndParallel()
            .State(State.Stunned, new Recorder("Stunned"))
            .BuildOrThrow();

        return definition.CreateInstance(new Context(), TickMode.Manual);
    }

    [Test]
    public void EnteringParallel_ActivatesEveryRegion_InDeclarationOrder()
    {
        var machine = Build();
        var context = machine.Context;
        context.Log.Clear();

        context.Go = true;
        machine.Tick(0.1f);

        Assert.That(context.Log, Is.EqualTo(new[] {
            "Exit:Idle", "Enter:Fighter", "Enter:MoveIdle", "Enter:WeaponHolstered", "Enter:BuffNone",
        }));

        machine.Dispose();
    }

    [Test]
    public void TwoRegions_TransitionIndependently_InTheSameTick()
    {
        var machine = Build();
        var context = machine.Context;
        context.Go = true;
        machine.Tick(0.1f); // enter Fighter
        context.Log.Clear();

        context.Go = true; // still true: drives Move's own transition again
        context.GoWeapon = true;
        machine.Tick(0.1f);

        // Both regions may each fire one transition in the same tick.
        Assert.That(machine.IsActive(State.MoveWalk), Is.True);
        Assert.That(machine.IsActive(State.WeaponDrawn), Is.True);
        Assert.That(machine.IsActive(State.BuffNone), Is.True, "the third region is untouched");

        machine.Dispose();
    }

    [Test]
    public void LeavingTheParallelNode_ExitsEveryRegion_InReverseDeclarationOrder()
    {
        var machine = Build();
        var context = machine.Context;
        context.Go = true;
        machine.Tick(0.1f); // enter Fighter, all three regions
        context.Log.Clear();

        machine.Fire(Trigger.Hit);
        machine.Tick(0.1f);

        Assert.That(context.Log, Is.EqualTo(new[] {
            "Exit:BuffNone", "Exit:WeaponHolstered", "Exit:MoveIdle", "Exit:Fighter", "Enter:Stunned",
        }));
        Assert.That(machine.IsActive(State.Stunned), Is.True);

        machine.Dispose();
    }

    [Test]
    public void AnyStateTargetingOneSpecificRegion_DoesNotMatchFromASiblingRegion()
    {
        // Same shape, but the AnyState transition targets a leaf INSIDE the Weapon region
        // specifically, rather than a state outside the parallel node entirely.
        var definition = HierarchicalStateMachine<Context, State>.Define("CrossRegionAnyState")
            .AnyState()
                .To(State.WeaponDrawn).When(static c => c.GoWeapon)
            .State(State.Idle)
                .To(State.Fighter).When(static c => c.Go)
            .Parallel(State.Fighter)
                .Region(State.MoveRegion)
                    .Child(State.MoveIdle, new Recorder("MoveIdle"))
                .EndRegion()
                .Region(State.WeaponRegion)
                    .Child(State.WeaponHolstered, new Recorder("WeaponHolstered"))
                    .Child(State.WeaponDrawn, new Recorder("WeaponDrawn"))
                .EndRegion()
            .EndParallel()
            .BuildOrThrow();

        var context = new Context { Go = true };
        var machine = definition.CreateInstance(context, TickMode.Manual);
        machine.Tick(0.1f); // enter Fighter: MoveIdle region + WeaponHolstered region

        Assert.That(machine.IsActive(State.MoveIdle), Is.True);
        Assert.That(machine.IsActive(State.WeaponHolstered), Is.True);
        context.Log.Clear();

        context.GoWeapon = true;
        machine.Tick(0.1f);

        // Evaluated from the Move region's leaf, the AnyState transition into the Weapon region
        // would cross a sibling boundary and must be rejected there. Evaluated from the Weapon
        // region's own leaf, the SAME transition is legitimate (target is inside its own region)
        // and must fire. Net effect: Weapon region advances exactly once, Move region is untouched.
        Assert.That(machine.IsActive(State.WeaponDrawn), Is.True);
        Assert.That(machine.IsActive(State.WeaponHolstered), Is.False);
        Assert.That(machine.IsActive(State.MoveIdle), Is.True, "the Move region must be untouched");
        Assert.That(context.Log, Is.EqualTo(new[] { "Exit:WeaponHolstered", "Enter:WeaponDrawn" }));

        machine.Dispose();
    }
}
