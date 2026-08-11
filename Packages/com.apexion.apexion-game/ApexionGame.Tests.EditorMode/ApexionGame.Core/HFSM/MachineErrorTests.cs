using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

public sealed class MachineErrorTests
{
    [Test]
    public void Default_IsUndefined_AndToStringIsSafe()
    {
        var error = default(MachineError);

        Assert.That(error.ToString(), Is.Not.Null.And.Not.Empty);
        Assert.That(error.ToString(), Does.Contain("unknown state machine error"));
    }

    [Test]
    public void UnknownState_ExactMessage()
    {
        var error = MachineError.UnknownState(7);

        Assert.That(error.ToString(), Is.EqualTo("The state with ordinal '7' was never declared as a node."));
    }

    [Test]
    public void TriggerQueueFull_ExactMessage()
    {
        var trigger = TriggerId.Of(TestTrigger.Go);
        var error = MachineError.TriggerQueueFull(trigger, 16);

        var message = error.ToString();

        Assert.That(message, Does.StartWith("The trigger queue is full at 16 entries; dropped Trigger("));
        Assert.That(message, Does.EndWith(")."));
    }

    [Test]
    public void Prefix_WrapsMessageInBrackets()
    {
        var error = MachineError.EmptyMachine().Prefix("EnemyBrain");

        Assert.That(error.ToString(), Is.EqualTo("[EnemyBrain] The machine declares no states."));
    }

    [Test]
    public void EachCase_HasADistinctMessage()
    {
        var a = NodeIndex.Of(1);
        var b = NodeIndex.Of(2);

        var messages = new[] {
            default(MachineError).ToString(),
            MachineError.EmptyMachine().ToString(),
            MachineError.UnknownState(1).ToString(),
            MachineError.DuplicateState(1).ToString(),
            MachineError.InitialChildNotAChild(a, b).ToString(),
            MachineError.EmptyComposite(a).ToString(),
            MachineError.UnbalancedScope(a, 1).ToString(),
            MachineError.TransitionCrossesParallelRegion(a, b).ToString(),
            MachineError.NoPendingTransition().ToString(),
            MachineError.MachineNotRunning().ToString(),
            MachineError.TriggerQueueFull(TriggerId.None, 16).ToString(),
            MachineError.TransitionInFlight(a, b).ToString(),
        };

        Assert.That(messages, Is.Unique);
    }
}
