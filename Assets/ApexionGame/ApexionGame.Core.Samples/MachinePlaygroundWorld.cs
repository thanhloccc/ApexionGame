using System;
using System.Collections.Generic;

namespace ApexionGame.HFSM.Samples;

/// <summary>
/// The ten scenarios HFSM - Overview.md §2.4 calls C6/C8, each run against a clean machine on
/// demand. Plain C#, <see cref="IDisposable"/>, one method per scenario — shared by
/// <see cref="MachinePlayground"/> and the inspector buttons so neither duplicates a scenario's
/// logic.
/// </summary>
public sealed class MachinePlaygroundWorld : IDisposable
{
    public const int ScenarioCount = 10;

    private static readonly string[] s_titles = {
        "1. Idle -> Patrol (After timer)",
        "2. Patrol -> Combat/Chase (guard)",
        "3. Chase -> Attack (guard; Combat itself is not re-entered)",
        "4. Attack -> Chase (trigger fired in OnUpdate, consumed next tick)",
        "5. AnyState -> Flee interrupts Attack in the same tick",
        "6. Flee -> Combat: shallow history restores Attack, not Chase",
        "7. Priority beats declaration order",
        "8. Async Stagger cancelled mid-flight (AsyncPolicy.CancelAndReplace)",
        "9. MinDuration blocks, then releases",
        "10. Parallel regions transition independently in one tick",
    };

    private HierarchicalStateMachine<EnemyContext, EnemyState> _enemy;

    public static string TitleOf(int scenario) => s_titles[scenario - 1];

    public void Dispose()
    {
        _enemy?.Dispose();
        _enemy = null;
    }

    /// <summary>Runs scenario <paramref name="scenario"/> (1-10) and returns its transcript.</summary>
    public string Run(int scenario) => scenario switch {
        1 => IdleToPatrol(),
        2 => PatrolToCombat(),
        3 => ChaseToAttack(),
        4 => AttackToChase(),
        5 => AnyStateInterruptsAttack(),
        6 => FleeToCombatRestoresHistory(),
        7 => PriorityBeatsDeclarationOrder(),
        8 => AsyncCancelAndReplace(),
        9 => MinDurationBlocksThenReleases(),
        10 => ParallelRegionsAreIndependent(),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Expected 1-10."),
    };

    // ── EnemyBrain scenarios (1, 2, 3, 4, 5, 6, 8) ──────────────────────────────────────────────

    private HierarchicalStateMachine<EnemyContext, EnemyState> FreshEnemy(out EnemyContext context)
    {
        _enemy?.Dispose();
        context = new EnemyContext();
        _enemy = EnemyBrain.Definition.CreateInstance(context, TickMode.Manual);
        return _enemy;
    }

    private string IdleToPatrol()
    {
        var machine = FreshEnemy(out var context);
        context.Log.Clear();

        machine.Tick(2f); // crosses the After(2f) threshold in one tick

        return Transcript(context.Log);
    }

    private string PatrolToCombat()
    {
        var machine = FreshEnemy(out var context);
        machine.Tick(2f); // Idle -> Patrol via the timer
        context.Log.Clear();

        context.SeesPlayer = true;
        machine.Tick(0.1f);

        return Transcript(context.Log);
    }

    private string ChaseToAttack()
    {
        var machine = FreshEnemy(out var context);
        context.SeesPlayer = true;
        machine.Tick(0.1f); // Idle -> Combat/Chase
        context.Log.Clear();

        context.DistanceToPlayer = 1f;
        machine.Tick(0.1f); // Chase -> Attack; LCA is Combat, Combat is not exited

        return Transcript(context.Log);
    }

    private string AttackToChase()
    {
        var machine = FreshEnemy(out var context);
        context.SeesPlayer = true;
        machine.Tick(0.1f); // -> Chase
        context.DistanceToPlayer = 1f;
        machine.Tick(0.1f); // -> Attack; entering also runs OnUpdate this same tick (swing 1)
        context.Log.Clear();

        machine.Tick(1.3f); // swing 2
        machine.Tick(1.3f); // swing 3, fires AttackFinished from OnUpdate
        context.DistanceToPlayer = 10f; // player backs off, so Chase won't immediately guard back into Attack
        machine.Tick(0.1f); // consumed here, exactly once: Attack -> Chase

        return Transcript(context.Log);
    }

    private string AnyStateInterruptsAttack()
    {
        var machine = FreshEnemy(out var context);
        context.SeesPlayer = true;
        machine.Tick(0.1f); // -> Chase
        context.DistanceToPlayer = 1f;
        machine.Tick(0.1f); // -> Attack
        context.Log.Clear();

        context.Health = 10f;
        machine.Tick(0.1f); // AnyState -> Flee wins over Attack's own transition this tick

        return Transcript(context.Log);
    }

    private string FleeToCombatRestoresHistory()
    {
        var machine = FreshEnemy(out var context);
        context.SeesPlayer = true;
        machine.Tick(0.1f); // -> Chase
        context.DistanceToPlayer = 1f;
        machine.Tick(0.1f); // -> Attack
        context.Health = 10f;
        machine.Tick(0.1f); // -> Flee; history(Combat) is recorded as Attack before OnExit
        context.Log.Clear();

        machine.Fire(EnemyTrigger.Healed);
        machine.Tick(0.1f); // Flee -> Combat; shallow history restores Attack, not Chase

        return Transcript(context.Log);
    }

    private string AsyncCancelAndReplace()
    {
#if UNITASK
        var machine = FreshEnemy(out var context);
        context.Log.Clear();

        machine.Fire(EnemyTrigger.Staggered);
        machine.Tick(0.1f); // Idle -> Stagger's EnterAsync suspends mid-flight
        context.Log.Add($"Phase:{machine.Phase}");

        context.Health = 10f;
        var accepted = machine.RequestTransitionOrError(EnemyState.Flee); // cancels Stagger synchronously
        context.Log.Add($"CancelAccepted:{accepted.IsSuccess}");

        machine.Tick(0.1f); // runs the replacement transition to Flee

        context.Log.Add($"Phase:{machine.Phase}");
        context.Log.Add($"CurrentState:{machine.CurrentState}");

        return Transcript(context.Log);
#else
        return "Button 8 needs UNITASK, which is not defined in this build.";
#endif
    }

    // ── dedicated mini demos (7, 9, 10) — the feature has no natural place on EnemyBrain ───────

    private string PriorityBeatsDeclarationOrder()
    {
        var context = new PriorityContext();

        var definition = HierarchicalStateMachine<PriorityContext, PriorityState>.Define("PriorityDemo")
            .State(PriorityState.A, new Recorder<PriorityContext>("A", static (c, s) => c.Log.Add(s)))
                .To(PriorityState.C).When(static _ => true)
                .To(PriorityState.D).When(static _ => true).Priority(10)
            .State(PriorityState.C, new Recorder<PriorityContext>("C", static (c, s) => c.Log.Add(s)))
            .State(PriorityState.D, new Recorder<PriorityContext>("D", static (c, s) => c.Log.Add(s)))
            .BuildOrThrow();

        using var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        machine.Tick(0.1f); // both guards are true; Priority(10) wins over C's earlier declaration

        return Transcript(context.Log);
    }

    private string MinDurationBlocksThenReleases()
    {
        var context = new DurationContext();

        var definition = HierarchicalStateMachine<DurationContext, DurationState>.Define("MinDurationDemo")
            .State(DurationState.A, new Recorder<DurationContext>("A", static (c, s) => c.Log.Add(s)))
                .To(DurationState.E).When(static c => c.Go).MinDuration(1f)
            .State(DurationState.E, new Recorder<DurationContext>("E", static (c, s) => c.Log.Add(s)))
            .BuildOrThrow();

        using var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Log.Clear();

        context.Go = true;
        machine.Tick(0.4f); // guard true, refused: only 0.4s in A
        machine.Tick(0.4f); // 0.8s, still refused
        machine.Tick(0.4f); // 1.2s, MinDuration satisfied -> fires

        return Transcript(context.Log);
    }

    private string ParallelRegionsAreIndependent()
    {
        var context = new ParallelContext();

        var definition = HierarchicalStateMachine<ParallelContext, ParallelState>.Define("ParallelDemo")
            .State(ParallelState.Idle, new Recorder<ParallelContext>("Idle", static (c, s) => c.Log.Add(s)))
                .To(ParallelState.Fighter).When(static c => c.Go)
            .Parallel(ParallelState.Fighter, new Recorder<ParallelContext>("Fighter", static (c, s) => c.Log.Add(s)))
                .Region(ParallelState.MoveRegion)
                    .Initial(ParallelState.MoveIdle)
                    .Child(ParallelState.MoveIdle, new Recorder<ParallelContext>("MoveIdle", static (c, s) => c.Log.Add(s)))
                        .To(ParallelState.MoveWalk).When(static c => c.Go)
                    .Child(ParallelState.MoveWalk, new Recorder<ParallelContext>("MoveWalk", static (c, s) => c.Log.Add(s)))
                .EndRegion()
                .Region(ParallelState.WeaponRegion)
                    .Initial(ParallelState.WeaponHolstered)
                    .Child(ParallelState.WeaponHolstered, new Recorder<ParallelContext>("WeaponHolstered", static (c, s) => c.Log.Add(s)))
                        .To(ParallelState.WeaponDrawn).When(static c => c.GoWeapon)
                    .Child(ParallelState.WeaponDrawn, new Recorder<ParallelContext>("WeaponDrawn", static (c, s) => c.Log.Add(s)))
                .EndRegion()
            .EndParallel()
            .BuildOrThrow();

        using var machine = definition.CreateInstance(context, TickMode.Manual);
        context.Go = true;
        machine.Tick(0.1f); // Idle -> Fighter: both regions enter their Initial child
        context.Log.Clear();

        context.GoWeapon = true; // Go is still true too
        machine.Tick(0.1f); // both regions transition independently, in this one tick

        return Transcript(context.Log);
    }

    private static string Transcript(List<string> log) => string.Join("\n", log);

    private enum PriorityState { A, C, D }

    private enum DurationState { A, E }

    private enum ParallelState { Idle, Fighter, MoveRegion, MoveIdle, MoveWalk, WeaponRegion, WeaponHolstered, WeaponDrawn }

    private sealed class PriorityContext
    {
        public readonly List<string> Log = new();
    }

    private sealed class DurationContext
    {
        public readonly List<string> Log = new();
        public bool Go;
    }

    private sealed class ParallelContext
    {
        public readonly List<string> Log = new();
        public bool Go;
        public bool GoWeapon;
    }

    /// <summary>Appends <c>"Enter:Name"</c> / <c>"Exit:Name"</c> to whichever demo context owns it.</summary>
    private sealed class Recorder<TContext> : StateBehaviour<TContext>
        where TContext : class
    {
        private readonly string _name;
        private readonly Action<TContext, string> _log;

        public Recorder(string name, Action<TContext, string> log)
        {
            _name = name;
            _log = log;
        }

        protected override void OnEnter(TContext context, in StateInfo info) => _log(context, $"Enter:{_name}");

        protected override void OnExit(TContext context, in StateInfo info) => _log(context, $"Exit:{_name}");
    }
}
