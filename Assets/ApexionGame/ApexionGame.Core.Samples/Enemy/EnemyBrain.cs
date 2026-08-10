namespace ApexionGame.HFSM.Samples;

/// <summary>
/// The demo machine from HFSM - Overview.md §2.2, extended just enough to reach every button:
/// a <see cref="EnemyTrigger.Healed"/> way back out of <c>Flee</c> (so shallow history has something
/// to restore) and, when <c>UNITASK</c> is defined, an async <c>Stagger</c> leaf reached by a
/// trigger (so button 8 has an in-flight chain to cancel). Built once and shared by every instance
/// the playground spawns.
/// </summary>
public static class EnemyBrain
{
    public static readonly MachineDefinition<EnemyContext, EnemyState> Definition = Build();

    private static MachineDefinition<EnemyContext, EnemyState> Build()
    {
        var builder = HierarchicalStateMachine<EnemyContext, EnemyState>.Define("EnemyBrain")
            .AnyState()
                .To(EnemyState.Flee).When(static c => c.Health < 20f)
            .State<IdleBehaviour>(EnemyState.Idle)
                .To(EnemyState.Patrol).After(2f)
                .To(EnemyState.Combat).When(static c => c.SeesPlayer)
            .State<PatrolBehaviour>(EnemyState.Patrol)
                .To(EnemyState.Combat).When(static c => c.SeesPlayer)
            .Composite(EnemyState.Combat).WithHistory(HistoryMode.Shallow)
                .Initial(EnemyState.Chase)
                .Child<ChaseBehaviour>(EnemyState.Chase)
                    .To(EnemyState.Attack).When(static c => c.DistanceToPlayer < 2f)
                .Child<AttackBehaviour>(EnemyState.Attack)
                    .To(EnemyState.Chase).On(EnemyTrigger.AttackFinished)
                .EndComposite()
            .State<FleeBehaviour>(EnemyState.Flee)
                .To(EnemyState.Combat).On(EnemyTrigger.Healed);

#if UNITASK
        builder = builder
            .AnyState()
                .To(EnemyState.Stagger).On(EnemyTrigger.Staggered)
            .State<StaggerBehaviour>(EnemyState.Stagger)
                .To(EnemyState.Idle).After(1f);
#endif

        return builder.BuildOrThrow();
    }
}
