# HFSM — API Surface

*[Tiếng Việt](HFSM%20-%20API%20Surface.vi.md) · [Index](README.md)*

Signatures only. Behaviour and ordering are in [HFSM - Flows](HFSM%20-%20Flows.md); memory layout is
in [HFSM - Data Model](HFSM%20-%20Data%20Model.md).

Everything below lives in namespace `ApexionGame.HFSM` unless stated otherwise.

---

## 1. Type parameters, once

| Parameter | Constraint | Meaning |
|---|---|---|
| `TContext` | `class` | The per-agent blackboard. One instance per machine. Behaviours mutate it. |
| `TState` | `unmanaged, Enum` | Node identity. One enum per machine shape. |
| `TData` | `unmanaged` | Optional per-instance data owned by one behaviour. |
| `TTrigger` | `unmanaged, Enum` | Trigger identity. May be a different enum per machine, or shared. |

`TContext` is constrained to `class` deliberately — a blackboard is mutated from inside `OnUpdate`,
and a `class` makes that natural without `ref` plumbing through every signature
([DEC-003](HFSM%20-%20Decisions.md#dec-003)).

---

## 2. Declaring a machine

`Define` is a **static member of the machine class itself**; there is no separate factory type. The
instance members are in §4.

```csharp
public sealed class HierarchicalStateMachine<TContext, TState>
    where TContext : class
    where TState : unmanaged, Enum
{
    public static MachineBuilder<TContext, TState> Define(string name);
}
```

```csharp
var builder = HierarchicalStateMachine<EnemyContext, EnemyState>.Define("EnemyBrain");
```

### 2.1 Builder — structure

```csharp
public sealed class MachineBuilder<TContext, TState>
    where TContext : class
    where TState : unmanaged, Enum
{
    // ── nodes ──────────────────────────────────────────────────────────────
    public MachineBuilder<TContext, TState> State<TBehaviour>(TState state)
        where TBehaviour : StateBehaviour<TContext>, new();

    public MachineBuilder<TContext, TState> State(TState state);            // pure node
    public MachineBuilder<TContext, TState> State(TState state, StateBehaviour<TContext> shared);

    public MachineBuilder<TContext, TState> Composite(TState state);
    public MachineBuilder<TContext, TState> Child<TBehaviour>(TState state)
        where TBehaviour : StateBehaviour<TContext>, new();
    public MachineBuilder<TContext, TState> Child(TState state);
    public MachineBuilder<TContext, TState> Initial(TState state);
    public MachineBuilder<TContext, TState> WithHistory(HistoryMode history);
    public MachineBuilder<TContext, TState> EndComposite();

    public MachineBuilder<TContext, TState> Parallel(TState state);
    public MachineBuilder<TContext, TState> Region(TState state);           // opens a region
    public MachineBuilder<TContext, TState> EndRegion();
    public MachineBuilder<TContext, TState> EndParallel();

    // ── transitions ────────────────────────────────────────────────────────
    public TransitionBuilder<TContext, TState> AnyState();
    public TransitionBuilder<TContext, TState> To(TState target);

    // ── build ──────────────────────────────────────────────────────────────
    public Result<MachineDefinition<TContext, TState>, MachineError> BuildOrError();
    public MachineDefinition<TContext, TState> BuildOrThrow();
}
```

`Composite`/`Parallel` open a scope; the matching `End*` closes it. `Child`/`Region` and any `To`
attach to the innermost open scope. Mismatched scopes are `MachineError.UnbalancedScope`, reported by
`BuildOrError()` — not a silent misbuild ([DEC-005](HFSM%20-%20Decisions.md#dec-005)).

### 2.2 Builder — transitions

```csharp
public readonly struct TransitionBuilder<TContext, TState>
    where TContext : class
    where TState : unmanaged, Enum
{
    public MachineBuilder<TContext, TState> To(TState target);

    // Guarded. `expression` is filled by the compiler and shown in the debugger's guard pane.
    public MachineBuilder<TContext, TState> When(
          Guard<TContext> guard
        , [CallerArgumentExpression(nameof(guard))] string expression = null
    );

    public MachineBuilder<TContext, TState> When(
          GuardWithInfo<TContext> guard
        , [CallerArgumentExpression(nameof(guard))] string expression = null
    );

    public MachineBuilder<TContext, TState> On<TTrigger>(TTrigger trigger)
        where TTrigger : unmanaged, Enum;

    public MachineBuilder<TContext, TState> After(float seconds);           // elapsed-time transition
    public MachineBuilder<TContext, TState> MinDuration(float seconds);     // gate on any transition
    public MachineBuilder<TContext, TState> Priority(int priority);         // default 0, higher first
    public MachineBuilder<TContext, TState> Internal();                     // no exit/enter
    public MachineBuilder<TContext, TState> Do(TransitionAction<TContext> action);
}

public delegate bool Guard<in TContext>(TContext context);
public delegate bool GuardWithInfo<in TContext>(TContext context, GuardInfo info);
public delegate void TransitionAction<in TContext>(TContext context);
```

Both guard forms are offered so the common case stays terse:

```csharp
.To(EnemyState.Flee).When(static c => c.Health < 20f)
.To(EnemyState.Idle).When(static (c, i) => i.TimeInState > 5f && c.SeesPlayer == false)
```

`GuardInfo` is a 24-byte `readonly struct` passed by value, so type inference works and the
lambda needs no explicit parameter types ([DEC-004](HFSM%20-%20Decisions.md#dec-004)).

---

## 3. Writing a state

```csharp
public abstract class StateBehaviour<TContext>
    where TContext : class
{
    protected virtual void OnEnter(TContext context, in StateInfo info) { }
    protected virtual void OnUpdate(TContext context, in StateInfo info, float deltaTime) { }
    protected virtual void OnExit(TContext context, in StateInfo info) { }

    // Dispatch seam. `internal virtual`, not `abstract`, so a consumer assembly can derive
    // from this class directly without being able to break the blob contract.
    internal virtual void EnterCore(
        TContext context, byte[] blob, int offset, in StateInfo info);

    internal virtual void UpdateCore(
        TContext context, byte[] blob, int offset, in StateInfo info, float deltaTime);

    internal virtual void ExitCore(
        TContext context, byte[] blob, int offset, in StateInfo info);

    internal virtual int DataSize => 0;
    internal virtual int DataAlign => 1;
}

public abstract class StateBehaviour<TContext, TData> : StateBehaviour<TContext>
    where TContext : class
    where TData : unmanaged
{
    protected virtual void OnEnter(TContext context, ref TData data, in StateInfo info) { }
    protected virtual void OnUpdate(TContext context, ref TData data, in StateInfo info, float deltaTime) { }
    protected virtual void OnExit(TContext context, ref TData data, in StateInfo info) { }

    // sealed: slices the instance blob and forwards. Never overridden further.
    internal sealed override void EnterCore(TContext context, byte[] blob, int offset, in StateInfo info)
        => OnEnter(context, ref UnsafeUtility.As<byte, TData>(ref blob[offset]), info);

    internal sealed override int DataSize => UnsafeUtility.SizeOf<TData>();
    internal sealed override int DataAlign => UnsafeUtility.AlignOf<TData>();
}
```

The `ref TData` reaches straight into the instance's `byte[]` blob. `UnsafeUtility.As` produces a
GC-tracked managed reference, so the array is never pinned and the GC may still move it
([DEC-008](HFSM%20-%20Decisions.md#dec-008)).

### 3.1 Async states

```csharp
#if UNITASK || UNITY_6000_0_OR_NEWER

    using UnityTask = Cysharp.Threading.Tasks.UniTask;      // or UnityEngine.Awaitable

    public abstract class AsyncStateBehaviour<TContext> : StateBehaviour<TContext>
        where TContext : class
    {
        protected virtual UnityTask OnEnterAsync(TContext context, StateInfo info, CancellationToken token);
        protected virtual UnityTask OnExitAsync(TContext context, StateInfo info, CancellationToken token);
    }

#endif
```

A behaviour that overrides an `*Async` method makes its whole transition asynchronous — the machine
enters `MachinePhase.Exiting`/`Entering` and honours `AsyncPolicy`
([Flows §8](HFSM%20-%20Flows.md#8-async-transitions)).

### 3.2 What a state is handed

```csharp
public readonly struct StateInfo
{
    public NodeIndex Node { get; }
    public NodeIndex PreviousNode { get; }
    public float TimeInState { get; }
    public float TimeInMachine { get; }
    public IMachineControl Control { get; }
}

public readonly struct GuardInfo
{
    public NodeIndex Source { get; }
    public NodeIndex PreviousNode { get; }
    public float TimeInState { get; }
    public float TimeInMachine { get; }
}
```

`StateInfo` carries `NodeIndex`, not `TState`, because `StateBehaviour<TContext>` is shared
across machines with different state enums. Convert with `machine.StateOf(info.Node)` when a name is
needed ([DEC-009](HFSM%20-%20Decisions.md#dec-009)).

---

## 4. Definition and instances

```csharp
public sealed class MachineDefinition<TContext, TState>
    where TContext : class
    where TState : unmanaged, Enum
{
    public string Name { get; }
    public int NodeCount { get; }
    public int TransitionCount { get; }
    public int StateDataSize { get; }        // bytes allocated per instance for state data

    public HierarchicalStateMachine<TContext, TState> CreateInstance(
          TContext context
    );

    public HierarchicalStateMachine<TContext, TState> CreateInstance(
          TContext context
        , TickMode tickMode
    );

    public HierarchicalStateMachine<TContext, TState> CreateInstance(
          TContext context
        , in MachineOptions options
    );

    public NodeIndex IndexOf(TState state);
    public TState StateOf(NodeIndex node);
}

public readonly struct MachineOptions
{
    public TickMode TickMode { get; init; }        // Runner (default) | Manual
    public AsyncPolicy AsyncPolicy { get; init; }  // CancelAndReplace (default) | Queue | Ignore
    public MachineRunner Runner { get; init; }     // null → MachineRunner.Default
    public string DebugName { get; init; }         // debugger label; null → "{Name} #{id}"
    public bool StartOnCreate { get; init; }       // default true
}
```

The same class that carries the static `Define` from §2:

```csharp
public sealed class HierarchicalStateMachine<TContext, TState>
    : IMachineControl, IMachineTickable, IDisposable
    where TContext : class
    where TState : unmanaged, Enum
{
    public TContext Context { get; }
    public MachineDefinition<TContext, TState> Definition { get; }
    public MachinePhase Phase { get; }
    public bool IsRunning { get; }

    public TState CurrentState { get; }                 // deepest active leaf of region 0
    public NodeIndex CurrentNode { get; }
    public float TimeInState { get; }
    public float TimeInMachine { get; }

    public event Action<TState, TState> StateChanged;   // (from, to) — leaf changes only

    public void Start();
    public void Tick(float deltaTime);
    public void Reset();                                 // back to initial, clears history/timers/data
    public void Dispose();                               // unregisters, returns to the definition pool

    public void Fire<TTrigger>(TTrigger trigger)
        where TTrigger : unmanaged, Enum;

    public Result<Unit, MachineError> TryFire<TTrigger>(TTrigger trigger)
        where TTrigger : unmanaged, Enum;

    public bool IsActive(TState state);
    public int GetActiveStates(Span<TState> destination);   // fills leaves; returns count written

    public Result<Unit, MachineError> RequestTransition(TState target);
}
```

`CurrentState` returns region 0's leaf. With parallel regions there is more than one active leaf —
use `GetActiveStates` ([DEC-010](HFSM%20-%20Decisions.md#dec-010)).

### 4.1 Control handed to behaviours

```csharp
public interface IMachineControl
{
    void Fire<TTrigger>(TTrigger trigger) where TTrigger : unmanaged, Enum;
    Result<Unit, MachineError> RequestTransition(NodeIndex target);
    float TimeInMachine { get; }
}
```

A behaviour never holds the machine itself — only this interface, through `StateInfo.Control`.
That keeps `StateBehaviour<TContext>` free of `TState`.

---

## 5. Ticking

```csharp
public sealed class MachineRunner
{
    public static MachineRunner Default { get; }

    public int Count { get; }
    public string Name { get; }

    public MachineRunner(string name);

    public void Tick(float deltaTime);
    public void Clear();

    public static void InstallIntoPlayerLoop();     // inserts Default under Update
    public static void RemoveFromPlayerLoop();
}

[DisallowMultipleComponent]
public sealed class MachineRunnerBehaviour : MonoBehaviour   // drop into a scene as the alternative
{
    // serialized: which runner, which delta (scaled / unscaled / fixed)
}

public enum TickMode { Runner, Manual }
```

Registering or disposing a machine *during* `Tick` is safe — adds and removes are deferred to the
end of the pass ([Flows §9](HFSM%20-%20Flows.md#9-runner)).

---

## 6. Enums and small types

```csharp
public enum StateNodeKind : byte { Leaf, Composite, Parallel, Region }
public enum HistoryMode : byte { None, Shallow, Deep }
public enum MachinePhase : byte { Stopped, Idle, Exiting, Entering }
public enum AsyncPolicy : byte { CancelAndReplace, Queue, Ignore }
public enum TransitionCause : byte { Initial, Guard, Trigger, Timer, Request, History }

[WrapType(typeof(int))]
public readonly partial struct NodeIndex { }        // default == invalid

public readonly struct TriggerId : IEquatable<TriggerId>
{
    public TypeId Type { get; }
    public int Value { get; }

    public static TriggerId Of<TTrigger>(TTrigger trigger) where TTrigger : unmanaged, Enum;
}
```

`TriggerId` pairs `TypeId<TTrigger>` with the ordinal, so `EnemyTrigger.Staggered` and
`BossTrigger.Staggered` are different triggers even though both are ordinal 0
([DEC-006](HFSM%20-%20Decisions.md#dec-006)).

---

## 7. Errors

`MachineError` is a `[PolyEnumFactoryFor]` wrapper over a `[PolyEnumStruct]` union — never a flat enum.
Cases and payloads: [HFSM - Data Model §6](HFSM%20-%20Data%20Model.md#6-error-contract).

```csharp
var result = builder.BuildOrError();

if (result.TryGetValue(out var definition) == false)
{
    DevLogger.LogError(result.Error.Prefix(nameof(EnemyBrain)).ToString());
    return;
}
```

Call sites always use the generated factories with parentheses — `MachineError.UnknownState(node)`,
`MachineError.NoInitialChild(node)`.

---

## 8. Debug surface (public, gated)

```csharp
namespace ApexionGame.HFSM.Debugging
{
    public interface IMachineDebug
    {
        string Name { get; }
        string DefinitionName { get; }
        bool IsAlive { get; }
        MachinePhase Phase { get; }
        int NodeCount { get; }

        void GetNodes(List<StateNodeDebugInfo> result);
        void GetActiveNodes(List<NodeIndex> result);
        void GetTransitions(List<TransitionDebugInfo> result);
        void EvaluateOutgoingGuards(List<GuardDebugInfo> result);
        void GetLog(List<TransitionLogEntry> result);
    }

    public static class MachineDebugRegistry
    {
        public static event Action Changed;
        public static IReadOnlyList<IMachineDebug> Machines { get; }

        public static void Register(IMachineDebug machine);
        public static void Unregister(IMachineDebug machine);
        public static void Clear();
    }
}
```

Every method appends to the caller's list and never clears it — the same contract as
`IStatStoreDebug`, so the window can reuse rented lists across polls. Machines register themselves
when `APEXION_HFSM_DEBUG` is defined; nothing in the runtime reads the registry
([HFSM - Debugging §2](HFSM%20-%20Debugging.md#2-finding-live-machines)).

---

## 9. Full worked example

```csharp
using System;
using ApexionGame.HFSM;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    public enum SentryState { Idle, Patrol, Combat, Chase, Attack, Stunned }
    public enum SentryTrigger { Hit, StunEnded, AttackFinished }

    public sealed class SentryContext
    {
        public Transform Self;
        public Transform Player;
        public float Health = 100f;
        public bool SeesPlayer;
        public float DistanceToPlayer = float.MaxValue;
    }

    public static class SentryBrain
    {
        public static readonly MachineDefinition<SentryContext, SentryState> Definition =
            HierarchicalStateMachine<SentryContext, SentryState>.Define(nameof(SentryBrain))
                .AnyState()
                    .To(SentryState.Stunned).On(SentryTrigger.Hit).Priority(100)
                .State<IdleBehaviour>(SentryState.Idle)
                    .To(SentryState.Patrol).After(2f)
                    .To(SentryState.Combat).When(static c => c.SeesPlayer)
                .State<PatrolBehaviour>(SentryState.Patrol)
                    .To(SentryState.Combat).When(static c => c.SeesPlayer)
                .Composite(SentryState.Combat).WithHistory(HistoryMode.Shallow)
                    .Initial(SentryState.Chase)
                    .Child<ChaseBehaviour>(SentryState.Chase)
                        .To(SentryState.Attack).When(static c => c.DistanceToPlayer < 2f)
                    .Child<AttackBehaviour>(SentryState.Attack)
                        .To(SentryState.Chase).On(SentryTrigger.AttackFinished)
                        .To(SentryState.Chase).When(static c => c.DistanceToPlayer > 3f)
                    .EndComposite()
                    .To(SentryState.Idle).When(static c => c.SeesPlayer == false).MinDuration(3f)
                .State<StunnedBehaviour>(SentryState.Stunned)
                    .To(SentryState.Combat).On(SentryTrigger.StunEnded)
                .BuildOrThrow();
    }

    public sealed class SentryAgent : MonoBehaviour
    {
        [SerializeField] private Transform _player;

        private HierarchicalStateMachine<SentryContext, SentryState> _machine;

        private void OnEnable()
        {
            var context = new SentryContext { Self = transform, Player = _player };
            _machine = SentryBrain.Definition.CreateInstance(context);
            _machine.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            _machine.StateChanged -= OnStateChanged;
            _machine.Dispose();
            _machine = null;
        }

        private void Update()
        {
            var context = _machine.Context;
            context.DistanceToPlayer = Vector3.Distance(transform.position, _player.position);
            context.SeesPlayer = context.DistanceToPlayer < 12f;
            // no Tick here — MachineRunner.Default ticks it
        }

        public void TakeDamage(float amount)
        {
            _machine.Context.Health -= amount;
            _machine.Fire(SentryTrigger.Hit);
        }

        private void OnStateChanged(SentryState from, SentryState to)
            => DevLogger.Log($"{name}: {from} → {to}");
    }
}
```

`Update` only refreshes the blackboard. `MachineRunner.Default.Tick(Time.deltaTime)` — called once,
from `MachineRunnerBehaviour` or the player loop — drives every agent in one pass.
