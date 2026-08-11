namespace ApexionGame.HFSM.Samples;

/// <summary>
/// Three swings, roughly a second apart, then fires <see cref="EnemyTrigger.AttackFinished"/> — the
/// exact shape from HFSM - Overview.md §2.2, and the trigger button 4 exercises: fired from
/// <see cref="OnUpdate"/>, consumed on the very next tick, exactly once.
/// </summary>
public sealed class AttackBehaviour : StateBehaviour<EnemyContext, AttackBehaviour.Data>
{
    public struct Data
    {
        public float Cooldown;
        public int SwingCount;
    }

    protected override void OnEnter(EnemyContext context, ref Data data, in StateInfo info)
    {
        data.Cooldown = 0f;
        data.SwingCount = 0;
        context.Log.Add("Enter:Attack");
    }

    protected override void OnUpdate(EnemyContext context, ref Data data, in StateInfo info, float deltaTime)
    {
        data.Cooldown -= deltaTime;

        if (data.Cooldown > 0f)
        {
            return;
        }

        data.Cooldown = 1.2f;
        context.Log.Add($"Update:Attack:Swing{data.SwingCount + 1}");

        if (++data.SwingCount >= 3)
        {
            info.Control.Fire(EnemyTrigger.AttackFinished);
        }
    }

    protected override void OnExit(EnemyContext context, ref Data data, in StateInfo info)
        => context.Log.Add("Exit:Attack");
}
