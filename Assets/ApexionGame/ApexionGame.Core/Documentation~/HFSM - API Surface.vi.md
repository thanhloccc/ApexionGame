# HFSM — API Surface

*[English](HFSM%20-%20API%20Surface.md) · [Index](README.vi.md)*

Chỉ có chữ ký. Hành vi và thứ tự nằm ở [HFSM - Flows](HFSM%20-%20Flows.vi.md); bố cục bộ nhớ nằm ở
[HFSM - Data Model](HFSM%20-%20Data%20Model.vi.md).

Mọi thứ dưới đây thuộc namespace `ApexionGame.HFSM` trừ khi ghi khác.

---

## 1. Tham số kiểu, nói một lần

| Tham số | Ràng buộc | Ý nghĩa |
|---|---|---|
| `TContext` | `class` | Blackboard của từng agent. Mỗi máy một instance. Behaviour ghi vào nó. |
| `TState` | `unmanaged, Enum` | Định danh node. Mỗi hình dạng máy một enum. |
| `TData` | `unmanaged` | Dữ liệu riêng của một behaviour, theo từng instance, tuỳ chọn. |
| `TTrigger` | `unmanaged, Enum` | Định danh trigger. Có thể mỗi máy một enum, hoặc dùng chung. |

`TContext` bị ràng `class` một cách có chủ đích — blackboard bị ghi từ trong `OnUpdate`, và `class`
làm điều đó tự nhiên mà không phải kéo `ref` qua mọi chữ ký
([DEC-003](HFSM%20-%20Decisions.vi.md#dec-003)).

---

## 2. Khai báo một máy

`Define` là **thành viên static của chính class máy**; không có type factory riêng. Thành viên
instance nằm ở §4.

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

### 2.1 Builder — cấu trúc

```csharp
public sealed class MachineBuilder<TContext, TState>
    where TContext : class
    where TState : unmanaged, Enum
{
    // ── node ───────────────────────────────────────────────────────────────
    public MachineBuilder<TContext, TState> State<TBehaviour>(TState state)
        where TBehaviour : StateBehaviour<TContext>, new();

    public MachineBuilder<TContext, TState> State(TState state);            // node thuần
    public MachineBuilder<TContext, TState> State(TState state, StateBehaviour<TContext> shared);

    public MachineBuilder<TContext, TState> Composite(TState state);
    public MachineBuilder<TContext, TState> Child<TBehaviour>(TState state)
        where TBehaviour : StateBehaviour<TContext>, new();
    public MachineBuilder<TContext, TState> Child(TState state);
    public MachineBuilder<TContext, TState> Initial(TState state);
    public MachineBuilder<TContext, TState> WithHistory(HistoryMode history);
    public MachineBuilder<TContext, TState> EndComposite();

    public MachineBuilder<TContext, TState> Parallel(TState state);
    public MachineBuilder<TContext, TState> Region(TState state);           // mở một region
    public MachineBuilder<TContext, TState> EndRegion();
    public MachineBuilder<TContext, TState> EndParallel();

    // ── transition ─────────────────────────────────────────────────────────
    public TransitionBuilder<TContext, TState> AnyState();
    public TransitionBuilder<TContext, TState> To(TState target);

    // ── build ──────────────────────────────────────────────────────────────
    public Result<MachineDefinition<TContext, TState>, MachineError> BuildOrError();
    public MachineDefinition<TContext, TState> BuildOrThrow();
}
```

`Composite`/`Parallel` mở một scope; `End*` tương ứng đóng nó. `Child`/`Region` và mọi `To` gắn vào
scope trong cùng đang mở. Scope lệch nhau là `MachineError.UnbalancedScope`, do `BuildOrError()` báo —
không phải một cỗ máy dựng sai trong im lặng ([DEC-005](HFSM%20-%20Decisions.vi.md#dec-005)).

### 2.2 Builder — transition

```csharp
public readonly struct TransitionBuilder<TContext, TState>
    where TContext : class
    where TState : unmanaged, Enum
{
    public MachineBuilder<TContext, TState> To(TState target);

    // Có guard. `expression` do compiler điền, hiện trong khung guard của debugger.
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

    public MachineBuilder<TContext, TState> After(float seconds);           // theo thời gian
    public MachineBuilder<TContext, TState> MinDuration(float seconds);     // chặn mọi transition
    public MachineBuilder<TContext, TState> Priority(int priority);         // mặc định 0, cao trước
    public MachineBuilder<TContext, TState> Internal();                     // không exit/enter
    public MachineBuilder<TContext, TState> Do(TransitionAction<TContext> action);
}

public delegate bool Guard<in TContext>(TContext context);
public delegate bool GuardWithInfo<in TContext>(TContext context, GuardInfo info);
public delegate void TransitionAction<in TContext>(TContext context);
```

Hai dạng guard cùng tồn tại để trường hợp thường gặp vẫn ngắn:

```csharp
.To(EnemyState.Flee).When(static c => c.Health < 20f)
.To(EnemyState.Idle).When(static (c, i) => i.TimeInState > 5f && c.SeesPlayer == false)
```

`GuardInfo` là `readonly struct` 24 byte truyền theo giá trị, nên suy luận kiểu chạy được và
lambda không phải khai kiểu tham số ([DEC-004](HFSM%20-%20Decisions.vi.md#dec-004)).

---

## 3. Viết một state

```csharp
public abstract class StateBehaviour<TContext>
    where TContext : class
{
    protected virtual void OnEnter(TContext context, in StateInfo info) { }
    protected virtual void OnUpdate(TContext context, in StateInfo info, float deltaTime) { }
    protected virtual void OnExit(TContext context, in StateInfo info) { }

    // Mối nối dispatch. `internal virtual`, không phải `abstract`, để assembly tiêu dùng kế thừa
    // thẳng class này được mà không có cách nào phá hợp đồng blob.
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

    // sealed: cắt blob của instance rồi chuyển tiếp. Không override thêm nữa.
    internal sealed override void EnterCore(TContext context, byte[] blob, int offset, in StateInfo info)
        => OnEnter(context, ref UnsafeUtility.As<byte, TData>(ref blob[offset]), info);

    internal sealed override int DataSize => UnsafeUtility.SizeOf<TData>();
    internal sealed override int DataAlign => UnsafeUtility.AlignOf<TData>();
}
```

`ref TData` chạm thẳng vào blob `byte[]` của instance. `UnsafeUtility.As` tạo ra một managed
reference được GC theo dõi, nên mảng không bao giờ bị pin và GC vẫn dời được nó
([DEC-008](HFSM%20-%20Decisions.vi.md#dec-008)).

### 3.1 State async

```csharp
#if UNITASK || UNITY_6000_0_OR_NEWER

    using UnityTask = Cysharp.Threading.Tasks.UniTask;      // hoặc UnityEngine.Awaitable

    public abstract class AsyncStateBehaviour<TContext> : StateBehaviour<TContext>
        where TContext : class
    {
        protected virtual UnityTask OnEnterAsync(TContext context, StateInfo info, CancellationToken token);
        protected virtual UnityTask OnExitAsync(TContext context, StateInfo info, CancellationToken token);
    }

#endif
```

Một behaviour override phương thức `*Async` sẽ làm cả transition của nó thành bất đồng bộ — máy vào
`MachinePhase.Exiting`/`Entering` và tuân theo `AsyncPolicy`
([Flows §8](HFSM%20-%20Flows.vi.md#8-transition-async)).

### 3.2 State được đưa cho cái gì

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

`StateInfo` mang `NodeIndex` chứ không phải `TState`, vì `StateBehaviour<TContext>` dùng
chung cho nhiều máy có enum state khác nhau. Cần tên thì đổi bằng `machine.StateOf(info.Node)`
([DEC-009](HFSM%20-%20Decisions.vi.md#dec-009)).

---

## 4. Definition và instance

```csharp
public sealed class MachineDefinition<TContext, TState>
    where TContext : class
    where TState : unmanaged, Enum
{
    public string Name { get; }
    public int NodeCount { get; }
    public int TransitionCount { get; }
    public int StateDataSize { get; }        // byte cấp cho state data mỗi instance

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
    public TickMode TickMode { get; init; }        // Runner (mặc định) | Manual
    public AsyncPolicy AsyncPolicy { get; init; }  // CancelAndReplace (mặc định) | Queue | Ignore
    public MachineRunner Runner { get; init; }     // null → MachineRunner.Default
    public string DebugName { get; init; }         // nhãn trong debugger; null → "{Name} #{id}"
    public bool StartOnCreate { get; init; }       // mặc định true
}
```

Vẫn là class mang `Define` static ở §2:

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

    public TState CurrentState { get; }                 // leaf sâu nhất của region 0
    public NodeIndex CurrentNode { get; }
    public float TimeInState { get; }
    public float TimeInMachine { get; }

    public event Action<TState, TState> StateChanged;   // (from, to) — chỉ khi leaf đổi

    public void Start();
    public void Tick(float deltaTime);
    public void Reset();                                 // về ban đầu, xoá history/timer/data
    public void Dispose();                               // gỡ đăng ký, trả về pool của definition

    public void Fire<TTrigger>(TTrigger trigger)
        where TTrigger : unmanaged, Enum;

    public Result<Unit, MachineError> TryFire<TTrigger>(TTrigger trigger)
        where TTrigger : unmanaged, Enum;

    public bool IsActive(TState state);
    public int GetActiveStates(Span<TState> destination);   // điền các leaf; trả về số đã ghi

    public Result<Unit, MachineError> RequestTransition(TState target);
}
```

`CurrentState` trả về leaf của region 0. Có parallel region thì có nhiều hơn một leaf active — dùng
`GetActiveStates` ([DEC-010](HFSM%20-%20Decisions.vi.md#dec-010)).

### 4.1 Quyền điều khiển đưa cho behaviour

```csharp
public interface IMachineControl
{
    void Fire<TTrigger>(TTrigger trigger) where TTrigger : unmanaged, Enum;
    Result<Unit, MachineError> RequestTransition(NodeIndex target);
    float TimeInMachine { get; }
}
```

Behaviour không bao giờ giữ chính cỗ máy — chỉ giữ interface này, qua `StateInfo.Control`. Nhờ
vậy `StateBehaviour<TContext>` không dính `TState`.

---

## 5. Tick

```csharp
public sealed class MachineRunner
{
    public static MachineRunner Default { get; }

    public int Count { get; }
    public string Name { get; }

    public MachineRunner(string name);

    public void Tick(float deltaTime);
    public void Clear();

    public static void InstallIntoPlayerLoop();     // chèn Default vào dưới Update
    public static void RemoveFromPlayerLoop();
}

[DisallowMultipleComponent]
public sealed class MachineRunnerBehaviour : MonoBehaviour   // đường thay thế: thả vào scene
{
    // serialize: runner nào, delta nào (scaled / unscaled / fixed)
}

public enum TickMode { Runner, Manual }
```

Đăng ký hoặc dispose một máy *trong lúc* `Tick` là an toàn — thêm và xoá được hoãn tới cuối lượt
([Flows §9](HFSM%20-%20Flows.vi.md#9-runner)).

---

## 6. Enum và các type nhỏ

```csharp
public enum StateNodeKind : byte { Leaf, Composite, Parallel, Region }
public enum HistoryMode : byte { None, Shallow, Deep }
public enum MachinePhase : byte { Stopped, Idle, Exiting, Entering }
public enum AsyncPolicy : byte { CancelAndReplace, Queue, Ignore }
public enum TransitionCause : byte { Initial, Guard, Trigger, Timer, Request, History }

[WrapType(typeof(int))]
public readonly partial struct NodeIndex { }        // default == không hợp lệ

public readonly struct TriggerId : IEquatable<TriggerId>
{
    public TypeId Type { get; }
    public int Value { get; }

    public static TriggerId Of<TTrigger>(TTrigger trigger) where TTrigger : unmanaged, Enum;
}
```

`TriggerId` ghép `TypeId<TTrigger>` với ordinal, nên `EnemyTrigger.Staggered` và
`BossTrigger.Staggered` là hai trigger khác nhau dù cùng ordinal 0
([DEC-006](HFSM%20-%20Decisions.vi.md#dec-006)).

---

## 7. Lỗi

`MachineError` là wrapper `[PolyEnumFactoryFor]` trên union `[PolyEnumStruct]` — không bao giờ là enum
phẳng. Danh sách case và payload: [HFSM - Data Model §6](HFSM%20-%20Data%20Model.vi.md#6-hợp-đồng-lỗi).

```csharp
var result = builder.BuildOrError();

if (result.TryGetValue(out var definition) == false)
{
    DevLogger.LogError(result.Error.Prefix(nameof(EnemyBrain)).ToString());
    return;
}
```

Chỗ gọi luôn dùng factory sinh ra kèm dấu ngoặc — `MachineError.UnknownState(node)`,
`MachineError.NoInitialChild(node)`.

---

## 8. Bề mặt debug (public, có gate)

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

Mọi phương thức đều *nối thêm* vào danh sách của người gọi và không bao giờ xoá — cùng hợp đồng với
`IStatStoreDebug`, nên cửa sổ tái dùng được list đã thuê qua các lượt poll. Máy tự đăng ký khi có
define `APEXION_HFSM_DEBUG`; không có gì trong runtime đọc registry
([HFSM - Debugging §2](HFSM%20-%20Debugging.vi.md#2-tìm-máy-đang-sống)).

---

## 9. Ví dụ đầy đủ

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
            // không Tick ở đây — MachineRunner.Default tick nó
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

`Update` chỉ làm mới blackboard. `MachineRunner.Default.Tick(Time.deltaTime)` — gọi một lần, từ
`MachineRunnerBehaviour` hoặc từ player loop — chạy mọi agent trong một lượt.
