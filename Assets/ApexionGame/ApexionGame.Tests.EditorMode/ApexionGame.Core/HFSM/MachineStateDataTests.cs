using NUnit.Framework;

namespace ApexionGame.HFSM.Tests;

public sealed class MachineStateDataTests
{
    private enum State { A, B, C }

    [Test]
    public void EachInstance_GetsItsOwnIndependentSlot()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("Data")
            .State(State.A, new RecordingBehaviourWithData("A"))
            .BuildOrThrow();

        var m1 = definition.CreateInstance(new RecordingContext(), TickMode.Manual);
        var m2 = definition.CreateInstance(new RecordingContext(), TickMode.Manual);

        m1.Tick(0.1f);
        m1.Tick(0.1f);
        m1.Tick(0.1f); // three updates on m1

        m2.Tick(0.1f); // one update on m2

        Assert.That(m1.Context.Log, Is.EqualTo(new[] { "Enter:A:0", "Update:A:1", "Update:A:2", "Update:A:3" }));
        Assert.That(m2.Context.Log, Is.EqualTo(new[] { "Enter:A:0", "Update:A:1" }));

        m1.Dispose();
        m2.Dispose();
    }

    [Test]
    public void MixedAlignment_DoubleAndFloat3_LayOutWithoutCorruption()
    {
        // double needs 8-byte alignment, float3 needs 4; declaring the float3 behaviour first
        // forces the double behaviour's slot to be padded rather than landing on an odd offset.
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("Alignment")
            .State(State.A, new Vector3Behaviour())
            .State(State.B, new DoubleBehaviour())
            .BuildOrThrow();

        var context = new RecordingContext();
        var machine = definition.CreateInstance(context, TickMode.Manual);
        // Starts in A automatically (declared first); A's OnEnter already wrote its float3 slot.

        var result = machine.RequestTransitionOrError(State.B);
        Assert.That(result.IsSuccess, Is.True);
        machine.Tick(0.1f); // performs A -> B, then runs B's first OnUpdate (its double: 0 -> 1)
        machine.Tick(0.1f); // B's second OnUpdate (its double: 1 -> 2)

        Assert.That(context.Number, Is.EqualTo(2.0).Within(0.0001), "the double slot was read back exactly");

        machine.Dispose();
    }

    private sealed class Vector3Behaviour : StateBehaviour<RecordingContext, Unity.Mathematics.float3>
    {
        protected override void OnEnter(RecordingContext c, ref Unity.Mathematics.float3 data, in StateInfo info)
            => data = new Unity.Mathematics.float3(1f, 2f, 3f);
    }

    private sealed class DoubleBehaviour : StateBehaviour<RecordingContext, double>
    {
        protected override void OnEnter(RecordingContext c, ref double data, in StateInfo info)
            => data = 0.0;

        protected override void OnUpdate(RecordingContext c, ref double data, in StateInfo info, float deltaTime)
        {
            data += 1.0;
            c.Number = (float)data;
        }
    }

    [Test]
    public void Reset_ZeroesStateData()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("ResetData")
            .State(State.A, new RecordingBehaviourWithData("A"))
            .BuildOrThrow();

        var machine = definition.CreateInstance(new RecordingContext(), TickMode.Manual);
        machine.Tick(0.1f);
        machine.Tick(0.1f); // counter should be 2 by now

        machine.Reset();
        machine.Context.Log.Clear();
        machine.Tick(0.1f);

        Assert.That(machine.Context.Log, Is.EqualTo(new[] { "Update:A:1" }), "counter restarted from zero");

        machine.Dispose();
    }

    [Test]
    public void NoStateData_AllocatesAZeroLengthBlob_NotNull()
    {
        var definition = HierarchicalStateMachine<RecordingContext, State>.Define("NoData")
            .State(State.A)
            .BuildOrThrow();

        Assert.That(definition.StateDataSize, Is.EqualTo(0));

        var machine = definition.CreateInstance(new RecordingContext(), TickMode.Manual);
        Assert.DoesNotThrow(() => machine.Tick(0.1f));

        machine.Dispose();
    }
}
