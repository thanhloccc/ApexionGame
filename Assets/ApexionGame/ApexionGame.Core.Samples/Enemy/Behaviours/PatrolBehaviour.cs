namespace ApexionGame.HFSM.Samples;

/// <summary>Waits on the <c>SeesPlayer</c> guard declared in <see cref="EnemyBrain"/> (button 2).</summary>
public sealed class PatrolBehaviour : StateBehaviour<EnemyContext>
{
    protected override void OnEnter(EnemyContext context, in StateInfo info)
        => context.Log.Add("Enter:Patrol");

    protected override void OnExit(EnemyContext context, in StateInfo info)
        => context.Log.Add("Exit:Patrol");
}
