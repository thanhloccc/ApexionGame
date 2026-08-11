using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

public sealed class MachineTriggerTests
{
    private static HierarchicalStateMachine<RecordingContext, TestState> Build()
    {
        var definition = HierarchicalStateMachine<RecordingContext, TestState>.Define("Triggers")
            .State(TestState.Idle)
                .To(TestState.Combat).On(TestTrigger.Go)
            .State(TestState.Combat)
                .To(TestState.Flee).On(TestTrigger.Hit)
            .State(TestState.Flee)
            .BuildOrThrow();

        return definition.CreateInstance(new RecordingContext(), TickMode.Manual);
    }

    [Test]
    public void FiredOutsideTick_IsConsumedOnTheNextTick()
    {
        var machine = Build();

        machine.Fire(TestTrigger.Go);
        Assert.That(machine.CurrentState, Is.EqualTo(TestState.Idle), "not applied synchronously");

        machine.Tick(0.1f);
        Assert.That(machine.CurrentState, Is.EqualTo(TestState.Combat));

        machine.Dispose();
    }

    [Test]
    public void UnmatchedTrigger_IsDroppedRatherThanRetained()
    {
        var machine = Build();

        // Idle has no transition armed by Hit.
        machine.Fire(TestTrigger.Hit);
        machine.Tick(0.1f);
        Assert.That(machine.CurrentState, Is.EqualTo(TestState.Idle), "dropped, not queued");

        // Advancing to a state that DOES handle Hit must not replay the dropped trigger.
        machine.Fire(TestTrigger.Go);
        machine.Tick(0.1f);
        Assert.That(machine.CurrentState, Is.EqualTo(TestState.Combat));

        machine.Tick(0.1f); // if the old Hit had lingered, this would fire Combat -> Flee
        Assert.That(machine.CurrentState, Is.EqualTo(TestState.Combat));

        machine.Dispose();
    }

    [Test]
    public void TwoDifferentEnums_SharingAnOrdinal_DoNotCollide()
    {
        var idA = TriggerId.Of(TestTrigger.Go);
        var idB = TriggerId.Of(OtherTestTrigger.Go);

        Assert.That(idA, Is.Not.EqualTo(idB));
    }

    [Test]
    public void QueueFull_ReturnsErrorInsteadOfGrowingUnbounded()
    {
        var machine = Build();

        // Idle never handles Hit, so none of these drain on their own — the queue genuinely fills.
        for (var i = 0; i < 16; i++)
        {
            var result = machine.FireOrError(TestTrigger.Hit);
            Assert.That(result.IsSuccess, Is.True, $"slot {i} should still fit");
        }

        var overflow = machine.FireOrError(TestTrigger.Hit);
        Assert.That(overflow.IsError, Is.True);
        Assert.That(overflow.GetErrorOrDefault().ToString(), Does.Contain("full"));

        machine.Dispose();
    }
}
