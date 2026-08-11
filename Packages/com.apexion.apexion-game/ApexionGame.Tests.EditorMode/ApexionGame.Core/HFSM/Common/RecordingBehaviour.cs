using System.Collections.Generic;

namespace ApexionGame.HFSM.Tests;

/// <summary>
/// The blackboard shared by golden-ordering tests: a transcript every behaviour appends to, plus a
/// couple of plain fields tests flip to drive guards.
/// </summary>
public sealed class RecordingContext
{
    public readonly List<string> Log = new();

    public bool Flag;
    public float Number;
}

/// <summary>
/// Appends <c>"Enter:Name"</c> / <c>"Update:Name"</c> / <c>"Exit:Name"</c> to the shared transcript.
/// </summary>
/// <remarks>
/// Takes its name in the constructor rather than via <c>new()</c>, so one class serves every node in
/// a test machine — attach with the instance-accepting builder overloads
/// (<c>State(state, behaviour)</c>, <c>Composite(state, behaviour)</c>, …).
/// </remarks>
public class RecordingBehaviour : StateBehaviour<RecordingContext>
{
    private readonly string _name;

    public RecordingBehaviour(string name)
    {
        _name = name;
    }

    protected override void OnEnter(RecordingContext context, in StateInfo info)
        => context.Log.Add($"Enter:{_name}");

    protected override void OnUpdate(RecordingContext context, in StateInfo info, float deltaTime)
        => context.Log.Add($"Update:{_name}");

    protected override void OnExit(RecordingContext context, in StateInfo info)
        => context.Log.Add($"Exit:{_name}");
}

/// <summary>
/// A <see cref="RecordingBehaviour"/> that also owns a per-instance data slot, to prove
/// <see cref="StateBehaviour{TContext, TData}"/> isolates one node's data from another's.
/// </summary>
public sealed class RecordingBehaviourWithData : StateBehaviour<RecordingContext, RecordingBehaviourWithData.Data>
{
    public struct Data
    {
        public int Counter;
        public double Sum;
    }

    private readonly string _name;

    public RecordingBehaviourWithData(string name)
    {
        _name = name;
    }

    protected override void OnEnter(RecordingContext context, ref Data data, in StateInfo info)
    {
        context.Log.Add($"Enter:{_name}:{data.Counter}");
    }

    protected override void OnUpdate(RecordingContext context, ref Data data, in StateInfo info, float deltaTime)
    {
        data.Counter++;
        data.Sum += 1.0;
        context.Log.Add($"Update:{_name}:{data.Counter}");
    }
}
