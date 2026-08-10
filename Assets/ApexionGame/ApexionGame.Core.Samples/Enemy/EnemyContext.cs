using System.Collections.Generic;

namespace ApexionGame.HFSM.Samples;

/// <summary>
/// The playground's blackboard — a transcript every behaviour appends to, plus the handful of
/// fields the ten buttons flip to drive <see cref="EnemyBrain"/>'s guards.
/// </summary>
public sealed class EnemyContext
{
    public readonly List<string> Log = new();

    public float Health = 100f;
    public float DistanceToPlayer = float.MaxValue;
    public bool SeesPlayer;
}
