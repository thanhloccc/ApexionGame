namespace ApexionGame.HFSM.Samples;

/// <summary>
/// Does nothing but log — the timer and the guard declared on it in <see cref="EnemyBrain"/> are
/// what move the machine along (buttons 1 and 2).
/// </summary>
public sealed class IdleBehaviour : StateBehaviour<EnemyContext>
{
    protected override void OnEnter(EnemyContext context, in StateInfo info)
        => context.Log.Add("Enter:Idle");

    protected override void OnExit(EnemyContext context, in StateInfo info)
        => context.Log.Add("Exit:Idle");
}
