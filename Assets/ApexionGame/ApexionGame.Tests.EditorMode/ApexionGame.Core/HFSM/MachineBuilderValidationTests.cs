using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

/// <summary>
/// One test per <see cref="MachineError"/> case that <c>BuildOrError</c> can produce, each fed a
/// real malformed declaration — a case nothing can raise is a lie in the API surface.
/// </summary>
public sealed class MachineBuilderValidationTests
{
    [Test]
    public void EmptyMachine_NoStatesDeclared()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Empty").BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("declares no states"));
    }

    [Test]
    public void UnknownState_TransitionTargetsAnUndeclaredState()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .State(TestState.Idle)
                .To(TestState.Combat)
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("was never declared as a node"));
    }

    [Test]
    public void DuplicateState_SameOrdinalDeclaredTwice()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .State(TestState.Idle)
            .State(TestState.Idle)
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("declared more than once"));
    }

    [Test]
    public void InitialChildNotAChild_NamesASiblingInsteadOfAChild()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .Composite(TestState.Combat)
                .Initial(TestState.Idle)
                .Child(TestState.Chase)
            .EndComposite()
            .State(TestState.Idle)
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("cannot be its Initial child"));
    }

    [Test]
    public void EmptyComposite_DeclaredWithNoChildren()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .Composite(TestState.Combat)
            .EndComposite()
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("has no children"));
    }

    [Test]
    public void EmptyComposite_AppliesToParallelToo()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .Parallel(TestState.Combat)
            .EndParallel()
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("has no children"));
    }

    [Test]
    public void UnbalancedScope_MissingEndComposite()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .Composite(TestState.Combat)
                .Child(TestState.Chase)
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("Unbalanced builder scope"));
    }

    [Test]
    public void UnbalancedScope_WrongEndCallForTheOpenScope()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .Composite(TestState.Combat)
                .Child(TestState.Chase)
            .EndParallel()
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("Unbalanced builder scope"));
    }

    [Test]
    public void TransitionCrossesParallelRegion_SourceAndTargetInSiblingRegions()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .Parallel(TestState.Combat)
                .Region(TestState.Chase)
                    .Child(TestState.Attack)
                        .To(TestState.Flee)
                .EndRegion()
                .Region(TestState.Flee)
                    .Child(TestState.Idle)
                .EndRegion()
            .EndParallel()
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("crosses two regions"));
    }

    [Test]
    public void NoPendingTransition_ModifierCalledBeforeTo()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Bad")
            .State(TestState.Idle)
                .Priority(1)
            .BuildOrError();

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetErrorOrDefault().ToString(), Does.Contain("no open"));
    }

    [Test]
    public void WellFormedMachine_BuildsSuccessfully()
    {
        var result = HierarchicalStateMachine<RecordingContext, TestState>.Define("Good")
            .State(TestState.Idle)
                .To(TestState.Combat).When(static c => c.Flag)
            .State(TestState.Combat)
            .BuildOrError();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValueOrDefault().NodeCount, Is.EqualTo(3)); // Root + Idle + Combat
    }
}
