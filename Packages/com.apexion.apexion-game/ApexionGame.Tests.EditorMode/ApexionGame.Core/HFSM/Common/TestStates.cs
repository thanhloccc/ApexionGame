namespace ApexionGame.HFSM.Tests;

/// <summary>
/// States shared by the tests that only need a small, generic vocabulary. Tests whose shape is
/// specific to one scenario (parallel regions, a five-node diamond, …) declare their own enum
/// instead of stretching this one to fit.
/// </summary>
public enum TestState
{
    Idle,
    Patrol,
    Combat,
    Chase,
    Attack,
    Flee,
    Stunned,
}

public enum TestTrigger
{
    Go,
    Hit,
    Done,
}

/// <summary>
/// A second trigger enum whose members share ordinals with <see cref="TestTrigger"/>, so a test can
/// prove <see cref="TriggerId"/> keeps them distinct.
/// </summary>
public enum OtherTestTrigger
{
    Go,
    Hit,
}
