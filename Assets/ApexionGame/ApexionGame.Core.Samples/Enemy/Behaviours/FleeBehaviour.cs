namespace ApexionGame.HFSM.Samples;

/// <summary>
/// A dead end in HFSM - Overview.md §2.2's illustration; here it also carries the
/// <see cref="EnemyTrigger.Healed"/> transition back to <c>Combat</c> so buttons 6 and 8 have a way
/// out, and so shallow history has something worth restoring.
/// </summary>
public sealed class FleeBehaviour : StateBehaviour<EnemyContext>
{
    protected override void OnEnter(EnemyContext context, in StateInfo info)
        => context.Log.Add("Enter:Flee");

    protected override void OnExit(EnemyContext context, in StateInfo info)
        => context.Log.Add("Exit:Flee");
}
