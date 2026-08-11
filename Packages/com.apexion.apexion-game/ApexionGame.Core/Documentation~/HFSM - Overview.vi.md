# HFSM — Overview

*[English](HFSM%20-%20Overview.md) · [Index](README.vi.md)*

## Status

| | |
|---|---|
| Phase | **Đang triển khai — phase 1–7 đã xong và có test, 8–10 còn treo** ([Roadmap](HFSM%20-%20Roadmap.vi.md#1-phase)) |
| Assembly đích | `ApexionGame.Core` (+ `.Editor`, `.Tests`, `.Samples`, `.Samples.Editor`, `.Authoring`) |
| Thư mục module | `Packages/com.apexion.apexion-game/ApexionGame.Core/HFSM/` |
| Namespace đích | `ApexionGame.HFSM` ([DEC-001](HFSM%20-%20Decisions.vi.md#dec-001)) |
| Đặt tên type | Không viết tắt: `HierarchicalStateMachine` · `Machine*` · tên trần ([DEC-019](HFSM%20-%20Decisions.vi.md#dec-019)) |
| Module EncosyTower | EnumExtensions, TypeWraps, PolyEnumStructs, Common (`Result`/`Option`), Collections, Pooling, Tasks, Types (`TypeId`), Logging, Debugging, UIElements, Editor.UIElements |
| Phụ thuộc Unity | `Unity.Mathematics`, `Unity.Collections` (cho `FixedString` trong error), UI Toolkit |
| DOTS | **không dùng** — project không cài DOTS và thiết kế này không cần |
| Quyết định còn mở | [DEC-013](HFSM%20-%20Decisions.vi.md#dec-013) |

---

## 1. Tóm tắt yêu cầu

Sau khi phần port `Entities.Stats` đã đóng (xong phase 5, 95/95 test xanh), dựng một **máy trạng
thái hữu hạn phân cấp** để studio tái dùng cho các game sau. Bốn yêu cầu, đúng lời người đặt:
*dễ dùng*, *code logic rõ ràng clean*, *high performance*, và *có Debugging*. Đặt tại
`Packages/com.apexion.apexion-game/ApexionGame.Core/HFSM`.

Hai vòng hỏi đã chốt hình dạng:

| Câu hỏi | Trả lời |
|---|---|
| Viết một state thế nào? | **Class kế thừa một base, nối dây bằng fluent builder.** Không source generator, không thuần lambda. |
| Tải thực tế? | **Hỗn hợp** — vài trăm AI agent tick mỗi frame cộng một ít máy flow sống lâu. Managed; không có tầng Burst/job. |
| Định danh một node bằng gì? | **`enum` riêng cho mỗi máy.** Tên node trong debugger lấy miễn phí từ `[EnumExtensions].ToStringFast()`. |
| Ai sở hữu dữ liệu của agent? | **Behaviour flyweight + slot state data theo instance.** Một definition dùng chung, nhiều instance rẻ; behaviour vẫn khai được `struct Data` riêng. |
| Ai gọi `Tick`? | **Cả hai** — mặc định qua runner tập trung, vẫn tick tay được. |
| Tính năng trong phạm vi | Phân cấp, timer theo state, **event trigger + polled guard**, **history (shallow/deep)**, **async enter/exit**, **parallel region**. Đủ cả bốn. |
| Mặt debug | **Đủ bốn** — cửa sổ editor (active path + log transition), graph cây state, guard inspector sống, overlay on-screen chạy được trong build. |
| Giao thêm | `ApexionGame.Core.Tests` (golden + benchmark), sample playground bấm nút, authoring bằng ScriptableObject, code refactor provider Roslyn. |

Hai thứ dứt khoát **không** phải: behaviour tree, và trình soạn node-graph trực quan.

---

## 2. Kết quả mong đợi

### 2.1 Trên đĩa sẽ có gì khi xong

```
Packages/com.apexion.apexion-game/
├── ApexionGame.Core/                          ← sửa asmdef: thêm khối versionDefines
│   ├── ApexionGame.Core.asmdef
│   ├── Documentation~/                        ← bộ doc này
│   └── HFSM/
│       ├── HierarchicalStateMachine`2.cs                          máy đang sống
│       ├── HierarchicalStateMachine`2+Transitions.cs              LCA, chuỗi exit/enter
│       ├── HierarchicalStateMachine`2+Async.cs                    bộ điều khiển transition async
│       ├── MachineDefinition`2.cs                đã bake, bất biến, dùng chung
│       ├── MachineBuilder`2.cs                   mặt trước fluent
│       ├── MachineBuilder`2+Validate.cs          mọi case MachineError sinh ra ở đây
│       ├── MachineRunner.cs                      tick theo batch
│       ├── MachineError.cs                       union PolyEnum
│       ├── StateBehaviour`1.cs                base, không có state data
│       ├── StateBehaviour`2.cs                base, có `ref TData`
│       ├── AsyncStateBehaviour`1.cs
│       ├── Common/                            StateNode, Transition, StateInfo, …
│       ├── Contracts/                         IMachineControl, IMachineTickable, IMachineDebug
│       ├── Internals/                         ActiveSet, StateDataBlob, ScopeStack
│       └── Debugging/                         registry, log transition, overlay, ValidationDefines
├── ApexionGame.Editor/                        assembly editor dùng chung — cửa sổ debugger nằm ở
│                                               đây (namespace ApexionGame.HFSM.Editor), không phải
│                                               một ApexionGame.Core.Editor riêng theo module
├── ApexionGame.Core.Authoring/                MachineGraphAsset + catalog
├── ApexionGame.Core.Tests/                    golden test + benchmark
├── ApexionGame.Core.Samples/                  playground + demo EnemyBrain + scene
└── ApexionGame.Core.Samples.Editor/           nút bấm trong Inspector của playground

Plugins/SourceGenerator.ApexionGame/
└── ApexionGame.SourceGen.CodeRefactors/       + 3 refactoring cho HFSM
```

Cây đầy đủ kèm từng file và namespace: [HFSM - Layout](HFSM%20-%20Layout.vi.md).

### 2.2 Lập trình viên viết gì

Khai báo một máy — đây là toàn bộ bề mặt cho một con enemy điển hình:

```csharp
public enum EnemyState { Idle, Patrol, Combat, Chase, Attack, Flee }
public enum EnemyTrigger { AttackFinished, Staggered }

public sealed class EnemyContext
{
    public float Health;
    public float DistanceToPlayer;
    public bool SeesPlayer;
}

// Build một lần — ví dụ một static field, hoặc cache trên một ScriptableObject.
public static readonly MachineDefinition<EnemyContext, EnemyState> Brain =
    HierarchicalStateMachine<EnemyContext, EnemyState>.Define(nameof(Brain))
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
        .BuildOrThrow();
```

Viết một state — có dữ liệu riêng cho từng agent, không cấp phát, không phình context:

```csharp
public sealed class AttackBehaviour : StateBehaviour<EnemyContext, AttackBehaviour.Data>
{
    public struct Data
    {
        public float Cooldown;
        public int SwingCount;
    }

    protected override void OnEnter(EnemyContext c, ref Data d, in StateInfo info)
    {
        d.Cooldown = 0f;
        d.SwingCount = 0;
    }

    protected override void OnUpdate(EnemyContext c, ref Data d, in StateInfo info, float deltaTime)
    {
        d.Cooldown -= deltaTime;

        if (d.Cooldown > 0f)
        {
            return;
        }

        d.Cooldown = 1.2f;

        if (++d.SwingCount >= 3)
        {
            info.Control.Fire(EnemyTrigger.AttackFinished);
        }
    }
}
```

Spawn và chạy một agent:

```csharp
var machine = Brain.CreateInstance(new EnemyContext());   // tự đăng ký vào runner
// …sau đó
machine.Fire(EnemyTrigger.Staggered);
machine.Dispose();                                        // gỡ đăng ký, trả về pool

// một nơi tick tất cả
MachineRunner.Default.Tick(Time.deltaTime);
```

Chữ ký đầy đủ: [HFSM - API Surface](HFSM%20-%20API%20Surface.vi.md).

### 2.3 Lập trình viên thấy gì khi debug

`ApexionGame > HFSM > Debugger` — một cửa sổ, bốn khung, sống lúc runtime:

```
┌─ HFSM Debugger ──────────────────────────────────────── [Auto ✓] [⏸] ─┐
│ Machine: [ EnemyBrain #0034            ▾ ]   17 live · 4 defs         │
├──────────────────────────┬────────────────────────────────────────────┤
│ ACTIVE PATH              │  STATE TREE                                │
│  Root                    │                 ┌────────┐                 │
│  └ Combat        4.82 s  │    ┌───────┐    │ Combat │◀── active       │
│    └ Attack      0.31 s  │    │ Idle  │───▶│════════│    (tô sáng)    │
│                          │    └───────┘    │ Chase  │                 │
│ history(Combat) = Chase  │        │        │ Attack │◀── leaf active  │
│                          │        ▼        └────────┘                 │
│ ── GUARDS (từ Attack)    │    ┌────────┐        │                     │
│  ● Attack→Chase          │    │ Patrol │        ▼                     │
│      on AttackFinished   │    └────────┘    ┌──────┐                  │
│  ○ *→Flee   false        │                  │ Flee │                  │
│      c.Health < 20f      │                  └──────┘                  │
│      Health = 74.0       │                                            │
├──────────────────────────┴────────────────────────────────────────────┤
│ TRANSITION LOG                      frame     Δt      cause           │
│  Chase   → Attack                    5142   0.42s   guard: c.Dist…<2f │
│  Combat  → Combat/Chase (history)    5106   —       enter             │
│  Patrol  → Combat                    5104   1.90s   guard: c.SeesPl…  │
└───────────────────────────────────────────────────────────────────────┘
```

- **Active path** — cấu hình đang sống kèm thời gian ở state, và child history đã ghi của mọi
  composite có history. Có parallel region thì mỗi region một đường.
- **State tree** — hình dạng vẽ bằng `Painter2D`, nhánh active tô sáng, cạnh transition có đầu mũi
  tên (chiều mới là toàn bộ ý nghĩa của đồ thị).
- **Guards** — mọi transition rời khỏi cấu hình đang active, kèm **nguyên văn mã nguồn** của guard
  và kết quả ở frame này. `c.Health < 20f → false (Health = 74.0)` trả lời thẳng câu "vì sao nó
  không chuyển". Nguyên văn lấy từ `[CallerArgumentExpression]` trên `.When(...)` — không cần đặt tên.
- **Transition log** — 32 transition gần nhất kèm frame, thời gian đã ở state nguồn, và nguyên nhân
  (guard nào, trigger nào, hay timer).

Cộng thêm **overlay on-screen** chạy được trong development build, không chỉ trong Editor:

```
┌ HFSM · EnemyBrain #0034 ─────────┐
│ Root/Combat/Attack        0.31 s │
│ last: Chase→Attack  (guard)      │
└──────────────────────────────────┘
```

Đặc tả đầy đủ, gồm cả luật poll-mà-không-dựng-lại mà debugger của Stats đã phải học bằng cách trả
giá: [HFSM - Debugging](HFSM%20-%20Debugging.vi.md).

### 2.4 Những phép kiểm chứng nó chạy đúng

| # | Kiểm chứng | Chạy bằng |
|---|---|---|
| C1 | `ApexionGame.Core` và cả năm assembly anh em compile, 0 lỗi 0 warning | `unity test . --mode EditMode` compile mọi assembly |
| C2 | Golden test thứ tự pass — chuỗi enter/exit là chuỗi ký tự chính xác, không phải "contains" | `ApexionGame.Core.Tests`, ~90 test |
| C3 | Mọi case `MachineError` đều được sinh ra từ một input builder sai thật | mỗi case một test |
| C4 | 500 máy × 20 node × 1000 tick cấp phát GC **bằng không** sau warm-up | `Is.Not.AllocatingGCMemory()` trong benchmark |
| C5 | 500 máy tick dưới 0.3 ms trên Editor/Mono | `MachineBenchmarkTests`, số ghi vào Roadmap |
| C6 | Nút 1–10 của playground, mỗi nút chạy một tình huống trên máy sạch | thủ công, trong scene sample |
| C7 | Debugger hiện một máy đang sống, và khung guard giải thích được một transition bị kẹt | thủ công, chụp màn hình vào Roadmap |
| C8 | Transition async bị huỷ giữa chừng để máy ở trạng thái xác định | golden test + nút 8 của playground |

---

## 3. Các bước

Xếp theo phụ thuộc. Mỗi dòng một hành động. Một phiên làm việc mới, không nhớ gì về cuộc hội thoại
này, phải thực thi được bảng này chỉ từ file — dòng nào cần chi tiết thiết kế thì nêu rõ mục tài
liệu chứa nó.

### Phase 1 — dựng khung assembly

| # | Việc | Xong khi | File |
|---|---|---|---|
| 1.1 | Chép nguyên khối `versionDefines` từ `EncosyTower.Core.asmdef` vào `ApexionGame.Core.asmdef`; giữ nguyên `references` sẵn có | `UNITASK`, `UNITY_COLLECTIONS`, `UNITY_MATHEMATICS`, `UNITY_BURST` đánh giá true trong assembly | `ApexionGame.Core/ApexionGame.Core.asmdef` |
| 1.2 | Tạo năm asmdef anh em, mỗi cái cùng khối `versionDefines` ([Layout §2](HFSM%20-%20Layout.vi.md#2-assembly)) | Unity liệt kê sáu assembly `ApexionGame.Core*`, tất cả compile rỗng | `ApexionGame.Core.Editor/`, `.Authoring/`, `.Tests/`, `.Samples/`, `.Samples.Editor/` + asmdef |
| 1.3 | `ValidationDefines.cs` — soi gương file của Stats; thêm `HFSM_DEBUG` = `APEXION_HFSM_DEBUG`, kèm cửa thoát `DISABLE_APEXION_CHECKS` | `[Conditional(ValidationDefines.HFSM_DEBUG)]` compile được | `HFSM/Debugging/ValidationDefines.cs` |
| 1.4 | `NodeIndex`, `TriggerId` — `[WrapType]` trên `int`, và `TypeId`+`int` tương ứng ([Data Model §2](HFSM%20-%20Data%20Model.vi.md#2-type-định-danh)) | cả hai là `readonly partial struct`; `default` là giá trị không hợp lệ | `HFSM/Common/NodeIndex.cs`, `HFSM/Common/TriggerId.cs` |
| 1.5 | `MachineError` — wrapper `[PolyEnumFactoryFor]` trên union `[PolyEnumStruct]`, 12 case kể cả `Undefined` ([Data Model §6](HFSM%20-%20Data%20Model.vi.md#6-hợp-đồng-lỗi)) | `MachineError.UnknownState(index)` compile **qua Unity**; `default(MachineError).ToString()` có nghĩa | `HFSM/MachineError.cs` |

**Exit:** cả sáu assembly compile qua Unity; `MachineErrorTests` phủ `Undefined`, thông điệp chính xác
của một case có payload, và `Prefix(...)`.

### Phase 2 — lõi: phân cấp, polled guard, timer

| # | Việc | Xong khi | File |
|---|---|---|---|
| 2.1 | Struct `StateNode`, `Transition`, `StateNodeKind`, `HistoryMode`, `StateInfo`, `GuardInfo` ([Data Model §3](HFSM%20-%20Data%20Model.vi.md#3-phía-definition)) | mỗi cái là `readonly struct` đúng như ghi; `StateNode` ≤ 40 byte | `HFSM/Common/*.cs` |
| 2.2 | Contract `IMachineControl`, `IMachineTickable` | `Fire`/`RequestTransition` gọi được từ behaviour qua `StateInfo.Control` | `HFSM/Contracts/*.cs` |
| 2.3 | `StateBehaviour<TContext>` + `StateBehaviour<TContext, TData>` kèm mối nối dispatch `*Core` internal ([API §3](HFSM%20-%20API%20Surface.vi.md#3-viết-một-state)) | assembly người dùng kế thừa được cả hai; `TData` truy cập qua `ref`, không box | `HFSM/StateBehaviour`1.cs`, `HFSM/StateBehaviour`2.cs` |
| 2.4 | `MachineBuilder<TContext, TState>` — `.State<T>()`, `.Composite()`, `.Child<T>()`, `.Initial()`, `.EndComposite()`, `.To().When()/.After()` | khai báo ở §2.2 compile và build được | `HFSM/MachineBuilder`2.cs` |
| 2.5 | `MachineBuilder+Validate` — bake index dày đặc, sắp transition theo nguồn, tính `Depth`, bố trí offset state data căn 8 byte, trả `Result<MachineDefinition, MachineError>` | mọi lỗi validate ở [Data Model §6](HFSM%20-%20Data%20Model.vi.md#6-hợp-đồng-lỗi) đều có input sinh ra nó | `HFSM/MachineBuilder`2+Validate.cs` |
| 2.6 | `MachineDefinition<TContext, TState>` — bất biến, `CreateInstance(ctx, tickMode)` | hai instance cùng definition dùng chung behaviour và không bao giờ dùng chung state data | `HFSM/MachineDefinition`2.cs` |
| 2.7 | `HierarchicalStateMachine<TContext, TState>` — tập active, timer, `Tick`, đánh giá polled guard sâu-nhất-trước, any-state trước tiên ([Flows §2–3](HFSM%20-%20Flows.vi.md#2-thứ-tự-đánh-giá)) | máy hai tầng chuyển state và tick được | `HFSM/HierarchicalStateMachine`2.cs` |
| 2.8 | `HierarchicalStateMachine+Transitions` — LCA, chuỗi exit (ngược độ sâu), chuỗi enter (xuống tới leaf khởi đầu) ([Flows §4](HFSM%20-%20Flows.vi.md#4-thực-thi-một-transition)) | golden test thứ tự pass trên cây 4 tầng | `HFSM/HierarchicalStateMachine`2+Transitions.cs` |

**Exit:** golden test thứ tự enter/exit xuyên các tầng; mọi case lỗi builder được phủ; máy 3 tầng
chạy 1000 tick không cấp phát.

### Phase 3 — trigger, any-state, history

| # | Việc | Xong khi | File |
|---|---|---|---|
| 3.1 | `TriggerId.Of<TEnum>()` dùng `TypeId<TEnum>` để hai enum trigger không đụng nhau ([DEC-006](HFSM%20-%20Decisions.vi.md#dec-006)) | `Of(A.X)` != `Of(B.X)` khi cả hai cùng ordinal 0 | `HFSM/Common/TriggerId.cs` |
| 3.2 | Transition `.On(trigger)`; `Fire()` xếp hàng và rút ở một điểm xác định trong tick ([Flows §5](HFSM%20-%20Flows.vi.md#5-trigger)) | trigger bắn trong `OnUpdate` được tiêu thụ ngay tick đó, đúng một lần | `HFSM/HierarchicalStateMachine`2.cs`, `HFSM/MachineBuilder`2.cs` |
| 3.3 | Nguồn `.AnyState()`, đánh giá trước mọi transition thuộc node | any-state thắng transition của leaf trong cùng một tick | `HFSM/MachineBuilder`2.cs` |
| 3.4 | `.Priority(n)`, `.After(seconds)`, `.MinDuration(seconds)`, `.Internal()` | test thứ tự khoá lại luật phá hoà ở [Flows §2](HFSM%20-%20Flows.vi.md#2-thứ-tự-đánh-giá) | `HFSM/MachineBuilder`2.cs` |
| 3.5 | History — `WithHistory(Shallow \| Deep)`, ghi lúc exit, khôi phục lúc vào lại ([Flows §6](HFSM%20-%20Flows.vi.md#6-history)) | shallow quay về đúng child, deep quay về nguyên đường tới leaf | `HFSM/HierarchicalStateMachine`2+Transitions.cs` |

**Exit:** trigger, any-state, priority, min-duration và cả hai chế độ history đều có golden test.

### Phase 4 — parallel region

| # | Việc | Xong khi | File |
|---|---|---|---|
| 4.1 | `.Parallel(state).Region(...)…EndParallel()` trong builder; `StateNodeKind.Parallel` | node parallel 3 region build được | `HFSM/MachineBuilder`2.cs` |
| 4.2 | Tập active thành bitset + active child theo node; thứ tự enter/exit/update theo [Flows §7](HFSM%20-%20Flows.vi.md#7-parallel-region) | vào node thì kích hoạt mọi region theo thứ tự khai báo; ra thì đảo ngược | `HFSM/Internals/ActiveSet.cs`, `HFSM/HierarchicalStateMachine`2+Transitions.cs` |
| 4.3 | Từ chối transition có nguồn và đích nằm ở **hai region khác nhau** của cùng một node parallel → `MachineError.TransitionCrossesParallelRegion` | test validate | `HFSM/MachineBuilder`2+Validate.cs` |

**Exit:** các region chuyển độc lập; transition ra ngoài node parallel thoát mọi region theo thứ tự
khai báo đảo ngược; history phối hợp đúng với region.

### Phase 5 — async enter/exit

| # | Việc | Xong khi | File |
|---|---|---|---|
| 5.1 | `AsyncStateBehaviour<TContext>` với `OnEnterAsync`/`OnExitAsync`, dùng mẫu alias `UnityTask` của EncosyTower dưới `#if UNITASK \|\| UNITY_6000_0_OR_NEWER` | compile khi có UniTask (project này có) | `HFSM/AsyncStateBehaviour`1.cs` |
| 5.2 | `MachinePhase` (`Idle`/`Exiting`/`Entering`) + `AsyncPolicy` (`Queue`/`CancelAndReplace`/`Ignore`) ([Flows §8](HFSM%20-%20Flows.vi.md#8-transition-async)) | transition thứ hai trong lúc đang dở hành xử đúng policy đã chọn | `HFSM/HierarchicalStateMachine`2+Async.cs` |
| 5.3 | Huỷ: `Dispose()` giữa chừng huỷ token và bỏ phần chuỗi còn lại, không chạy hook enter | không có `OnEnter` nào chạy sau `Dispose` | `HFSM/HierarchicalStateMachine`2+Async.cs` |

**Exit:** cancel-and-replace, queue, và dispose-giữa-chừng mỗi cái một golden test.

### Phase 6 — runner, pooling, benchmark

| # | Việc | Xong khi | File |
|---|---|---|---|
| 6.1 | `MachineRunner` — `FasterList<IMachineTickable>`, thêm/xoá hoãn lại để đăng ký trong lúc tick vẫn an toàn | đăng ký trong `Tick` không làm hỏng vòng lặp | `HFSM/MachineRunner.cs` |
| 6.2 | MonoBehaviour `MachineRunnerBehaviour` + `MachineRunner.InstallIntoPlayerLoop()` | cả hai đường đều tick được | `HFSM/MachineRunnerBehaviour.cs` |
| 6.3 | `HierarchicalStateMachine.Reset()` + pool instance trên definition; `Dispose()` trả về pool | 1000 vòng spawn/despawn chỉ cấp phát một lần | `HFSM/MachineDefinition`2.cs`, `HFSM/HierarchicalStateMachine`2.cs` |
| 6.4 | `MachineBenchmarkTests` — 500 máy × 20 node × 1000 tick, **không** `[Explicit]` ([Roadmap §4](HFSM%20-%20Roadmap.vi.md#4-benchmark)) | số ghi vào Roadmap; C4 và C5 đạt | `ApexionGame.Core.Tests/MachineBenchmarkTests.cs` |

**Exit:** C4 (0 GC) và C5 (< 0.3 ms) xác nhận bằng lần chạy thật, số dán vào Roadmap.

### Phase 7 — debugging

| # | Việc | Xong khi | File |
|---|---|---|---|
| 7.1 | `IMachineDebug`, `MachineDebugRegistry`, struct thông tin debug; máy tự đăng ký dưới `APEXION_HFSM_DEBUG` ([Debugging §2](HFSM%20-%20Debugging.vi.md#2-tìm-máy-đang-sống)) | cửa sổ tìm được máy đang chạy mà không cần code người dùng | `HFSM/Debugging/*.cs` |
| 7.2 | `TransitionLog` — ring buffer 32 mục, ghi nguyên nhân (`guard` / `trigger` / `timer` / `initial`), có gate | log chịu 10 000 transition mà không cấp phát | `HFSM/Debugging/TransitionLog.cs` |
| 7.3 | Nguyên văn guard qua `[CallerArgumentExpression]` trên `.When(...)`, lưu trong definition | khung guard hiện `c.Health < 20f` mà người dùng không phải đặt tên gì | `HFSM/MachineBuilder`2.cs` |
| 7.4 | `MachineDebuggerWindow` + `Views/` + `StyleSheets/` — active path, graph cây, khung guard, log; **refresh theo diff hình dạng, không bao giờ dựng lại cây** ([Debugging §4](HFSM%20-%20Debugging.vi.md#4-cửa-sổ-editor)) | cuộn và lựa chọn sống sót qua polling; C7 đạt | `ApexionGame.Core.Editor/**` |
| 7.5 | `MachineOverlayPanel` + `MachineOverlayBehaviour` UI runtime, có gate | overlay hiện trong development build | `HFSM/Debugging/MachineOverlayPanel.cs`, `…/MachineOverlayBehaviour.cs` |

**Exit:** C7 — ảnh chụp cửa sổ giải thích một transition bị kẹt, dán vào Roadmap.

### Phase 8 — sample playground

| # | Việc | Xong khi | File |
|---|---|---|---|
| 8.1 | `MachinePlaygroundWorld` — C# thuần, `IDisposable`, mỗi tình huống một hàm, dùng chung giữa scene và cửa sổ | không lặp logic giữa MonoBehaviour và code editor | `ApexionGame.Core.Samples/MachinePlaygroundWorld.cs` |
| 8.2 | MonoBehaviour `MachinePlayground` `[ExecuteAlways]` + `MachinePlaygroundEditor` với 10 nút, mỗi nút dựng lại máy sạch, dùng `System.Action` (không phải `Invoke(name)`) | nút chạy ngoài play mode; bấm thứ tự nào cũng được | `ApexionGame.Core.Samples/`, `ApexionGame.Core.Samples.Editor/` |
| 8.3 | Demo `EnemyBrain` — máy ở §2.2 với context giả, cộng scene `hfsm-playground.unity` viết tay `.meta` GUID cố định | scene mở được và nút chạy | `ApexionGame.Core.Samples/**`, `…/Scenes/hfsm-playground.unity` |
| 8.4 | Đăng ký một `IVisualCommand` bật/tắt overlay — **đặt trong assembly Samples**, để `ApexionGame.Core` không bao giờ tham chiếu `EncosyTower.Core.Extended` ([DEC-011](HFSM%20-%20Decisions.vi.md#dec-011)) | overlay bật/tắt từ console trong game | `ApexionGame.Core.Samples/MachineOverlayCommand.cs` |

**Exit:** C6 — cả mười nút cho ra đúng output đã ghi.

### Phase 9 — authoring bằng ScriptableObject

| # | Việc | Xong khi | File |
|---|---|---|---|
| 9.1 | `GuardCatalog<TContext>` / `BehaviourCatalog<TContext>` — `StringId → delegate`/`instance` ([DEC-013](HFSM%20-%20Decisions.vi.md#dec-013)) | tra được guard theo tên, và lúc đánh giá không box | `ApexionGame.Core.Authoring/GuardCatalog`1.cs` |
| 9.2 | `MachineGraphAsset : ScriptableObject` — danh sách node và transition đã serialize | asset round-trip qua inspector | `ApexionGame.Core.Authoring/MachineGraphAsset.cs` |
| 9.3 | `ToDefinition<TContext, TState>(catalogs)` → `Result<MachineDefinition, MachineError>` | máy dựng từ asset hành xử y hệt máy dựng từ code | `ApexionGame.Core.Authoring/MachineGraphAsset+ToDefinition.cs` |

**Exit:** một test dựng cùng một máy từ code và từ asset rồi khẳng định log transition trùng khớp
qua 100 tick.

### Phase 10 — code refactor provider

| # | Việc | Xong khi | File |
|---|---|---|---|
| 10.1 | *Generate HFSM state skeleton* — class rỗng → `sealed class X : StateBehaviour<TCtx>` kèm ba override, fully-qualified | chỉ chào khi class chưa có base type | `ApexionGame.SourceGen.CodeRefactors/**` |
| 10.2 | *Add per-state data struct* — viết lại `StateBehaviour<TCtx>` → `StateBehaviour<TCtx, X.Data>`, thêm struct lồng và tham số `ref Data` | giữ nguyên thân các override đã có | như trên |
| 10.3 | *Generate machine skeleton from enum* — trên một `enum`, sinh chuỗi builder với mỗi thành viên một `.State<>()` | code sinh ra compile được với assembly runtime | như trên |
| 10.4 | Deploy: build `-c Release`, `.meta` mang label `RunOnlyOnAssembliesWithReference` + `RoslynAnalyzer` (**không** có `SourceGenerator`), ghi trong cùng lệnh với bước copy DLL | Unity không auto-reference DLL | `Plugins/SourceGenerator.ApexionGame/**` |

**Exit:** 9 test refactor (có chào / không chào / nội dung sinh ra) pass qua `dotnet test`.

---

## 4. Mục tiêu và phi mục tiêu

### 4.1 Mục tiêu

| | |
|---|---|
| G1 | Một state là một class bình thường với ba override. Không attribute, không generator, không đăng ký. |
| G2 | Mỗi hình dạng máy một definition dùng chung; spawn một agent cấp phát một instance nhỏ, không phải cả cây state. |
| G3 | 0 cấp phát GC cho mỗi tick, mỗi transition, mỗi trigger — khẳng định bằng test, không bằng thiện chí. |
| G4 | Phân cấp, trigger, history, parallel region và transition async với ngữ nghĩa thứ tự **được ghi rõ** và bị golden test khoá lại. |
| G5 | Bốn mặt debug trả lời câu hỏi thật, trên hết là *"vì sao nó không chuyển?"* |
| G6 | Mọi thất bại là `Result<T, MachineError>` với payload có kiểu, không bao giờ là `false` trần hay enum phẳng. |

### 4.2 Phi mục tiêu

| | Vì sao |
|---|---|
| Behaviour tree, utility AI, GOAP | Công cụ khác cho bài toán khác. Một HFSM mọc thêm BT bên trong thì không còn là cái nào cả. |
| Trình soạn node-graph trực quan | Bề mặt cực lớn, mà debugger đã hiện đồ thị ở chế độ chỉ đọc. Authoring phase 9 là inspector dạng danh sách. |
| Tầng Burst/job, máy trạng thái unmanaged | Câu trả lời về tải là "vài trăm agent". Thiết kế managed thừa sức; thêm tầng native sẽ buộc state bỏ mọi reference. |
| Networking, rollback, bảo đảm tất định | Không có yêu cầu. Timer `float` làm rollback không vững; đừng giả vờ ngược lại. |
| Serialize một máy **đang chạy** vào save file | Bài toán khác (cần định danh node có version). Đường được hỗ trợ là `Reset()` + vào lại. |
| Tự chuyển state từ animation event / Timeline | Bắn một trigger từ callback; máy không nên biết tới hệ con của Unity. |

---

## 5. Ánh xạ module EncosyTower

| Phần của tính năng | Module | Dùng thế nào |
|---|---|---|
| Id state, `ToStringFast()`, `ToIndex()`, số thành viên | **EnumExtensions** | `TState : unmanaged, Enum`; người dùng gắn `[EnumExtensions]` lên enum của mình để có tên không cấp phát trong debugger |
| `NodeIndex` | **TypeWraps** | `[WrapType]` trên `int` |
| Định danh trigger không đụng nhau giữa các enum | **Types** | `TypeId<TEnum>` ghép với ordinal |
| `MachineError` | **PolyEnumStructs** | wrapper `[PolyEnumFactoryFor]` trên union `[PolyEnumStruct]`, 12 case |
| Kiểu trả về của `BuildOrError`, `TryFire` | **Common** | `Result<T, MachineError>`, `Option<T>` |
| Tích luỹ trong builder, danh sách runner, hàng đợi trigger | **Collections** | `FasterList<T>`; view `.ReadOnly` trên bề mặt debug công khai |
| Map `TState → node index` lúc Build | **Collections** + **Pooling** | `ArrayMap<int,int>` thuê từ `ArrayMapPool`, trả lại khi Build xong |
| Bộ đệm tạm cho chuỗi exit/enter | **Pooling** | `FasterListPool<int>` — thuê theo từng transition, không bao giờ là field của instance |
| `OnEnterAsync` / `OnExitAsync` | **Tasks** | `UnityTasks` và mẫu alias `using UnityTask = …`, gate `#if UNITASK \|\| UNITY_6000_0_OR_NEWER` |
| Log chỉ trong bản dev từ máy | **Logging** | `DevLogger` |
| Guard clause bị bóc khỏi bản release | **Debugging** | `Checks` + một `ThrowHelper` cục bộ, gate bằng `ValidationDefines` chép từ module Stats |
| Cửa sổ debugger, graph, khung guard | **Editor.UIElements** + **UIElements** | Dựng bằng C#, `Views/` + `StyleSheets/`, không UXML |
| Bật/tắt overlay trong game | **VisualDebugging.Commands** | `IVisualCommand` — chỉ khai trong assembly **Samples** |

### 5.1 Cái gì tự viết, và vì sao

**Chính bản thân runtime máy trạng thái.** EncosyTower không có module state machine — đã kiểm bằng
cách tìm `statemachine`/`hfsm` khắp package, 0 kết quả. Bảng quyết định có liệt `PolyEnumStructs`
dưới mục "discriminated union of structs (state machine, command)", nhưng đó là union *dữ liệu*: nó
mô hình hoá "giá trị này là một trong N hình dạng", chứ không phải phân cấp, transition, vòng đời
enter/exit, history hay region. Không có gì trong package vừa, nên runtime là mới — trong khi mọi
primitive đỡ bên dưới (id, error, collection, pool, task, logging, UI) đều lấy từ package chứ không
viết lại.

**`PageFlows` cố ý không tái dùng.** Nó là ngăn xếp điều hướng màn hình/popup với vòng đời và ngữ
nghĩa nút back riêng — đúng là một máy trạng thái, nhưng chuyên cho trang UI và không tổng quát trên
một context bất kỳ. Dùng nó cho AI enemy sẽ là chống lại nó. Máy flow *thật sự* nói về màn hình thì
vẫn nên tiếp tục dùng `PageFlows.MonoPages`.

---

## 6. Phần còn lại của thiết kế nằm đâu

| Câu hỏi | Tài liệu |
|---|---|
| Người dùng gõ chính xác cái gì? | [HFSM - API Surface](HFSM%20-%20API%20Surface.vi.md) |
| Mọi thứ chạy theo thứ tự nào? | [HFSM - Flows](HFSM%20-%20Flows.vi.md) |
| Trong bộ nhớ có gì, to bao nhiêu? | [HFSM - Data Model](HFSM%20-%20Data%20Model.vi.md) |
| File nào đặt ở đâu? | [HFSM - Layout](HFSM%20-%20Layout.vi.md) |
| Bốn mặt debug hoạt động ra sao? | [HFSM - Debugging](HFSM%20-%20Debugging.vi.md) |
| Vì sao dựng theo cách này? | [HFSM - Decisions](HFSM%20-%20Decisions.vi.md) |
| Làm theo thứ tự nào, và có thể hỏng ở đâu? | [HFSM - Roadmap](HFSM%20-%20Roadmap.vi.md) |
