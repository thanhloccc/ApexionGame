namespace ApexionGame.HFSM.Samples;

/// <summary>Waits on the <c>DistanceToPlayer</c> guard declared in <see cref="EnemyBrain"/> (button 3).</summary>
public sealed class ChaseBehaviour : StateBehaviour<EnemyContext>
{
    protected override void OnEnter(EnemyContext context, in StateInfo info)
        => context.Log.Add("Enter:Chase");

    protected override void OnExit(EnemyContext context, in StateInfo info)
        => context.Log.Add("Exit:Chase");
}
