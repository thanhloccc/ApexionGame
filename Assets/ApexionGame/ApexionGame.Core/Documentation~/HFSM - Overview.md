# HFSM — Overview

*[Tiếng Việt](HFSM%20-%20Overview.vi.md) · [Index](README.md)*

## Status

| | |
|---|---|
| Phase | **Implementing — phases 1–7 done and tested, 8–10 pending** ([Roadmap](HFSM%20-%20Roadmap.md#1-phases)) |
| Target assembly | `ApexionGame.Core` (+ `.Editor`, `.Tests`, `.Samples`, `.Samples.Editor`, `.Authoring`) |
| Module folder | `Assets/ApexionGame/ApexionGame.Core/HFSM/` |
| Target namespace | `ApexionGame.HFSM` ([DEC-001](HFSM%20-%20Decisions.md#dec-001)) |
| Type naming | No abbreviation: `HierarchicalStateMachine` · `Machine*` · bare ([DEC-019](HFSM%20-%20Decisions.md#dec-019)) |
| EncosyTower modules | EnumExtensions, TypeWraps, PolyEnumStructs, Common (`Result`/`Option`), Collections, Pooling, Tasks, Types (`TypeId`), Logging, Debugging, UIElements, Editor.UIElements |
| Unity deps | `Unity.Mathematics`, `Unity.Collections` (for `FixedString` in errors), UI Toolkit |
| DOTS | **not used** — DOTS is not installed in this project and this design does not need it |
| Open decisions | [DEC-013](HFSM%20-%20Decisions.md#dec-013) |

---

## 1. Request summary

Now that the `Entities.Stats` port is closed (phase 5 done, 95/95 tests green), build a **hierarchical
finite state machine** that this studio can reuse across future titles. The four stated qualities,
in the user's words: *dễ dùng* (easy to use), *code logic rõ ràng clean* (clear, clean logic),
*high performance*, and *có Debugging* (real debugging). It lives in
`Assets/ApexionGame/ApexionGame.Core/HFSM`.

The question rounds settled the shape:

| Question | Answer |
|---|---|
| How is a state written? | **Class deriving from a base, wired by a fluent builder.** Not a source generator, not lambdas-only. |
| What workload? | **Mixed** — a few hundred AI agents ticking every frame plus a handful of long-lived flow machines. Managed; no Burst/job tier. |
| How is a node identified? | **A per-machine `enum`.** Node names in the debugger come free from `[EnumExtensions].ToStringFast()`. |
| Who owns per-agent data? | **Flyweight behaviours + a per-instance state-data slot.** One shared definition, many cheap instances; a behaviour may still declare its own `struct Data`. |
| Who calls `Tick`? | **Both** — a central runner by default, manual ticking available. |
| Features in scope | Hierarchy, per-state timers, **event triggers + polled guards**, **history (shallow/deep)**, **async enter/exit**, **parallel regions**. All four. |
| Debug surfaces | **All four** — editor window (active path + transition log), state-tree graph view, live guard inspector, on-screen overlay that works in a build. |
| Extra deliverables | `ApexionGame.Core.Tests` (golden + benchmark), button-driven sample playground, ScriptableObject authoring, Roslyn code-refactor provider. |

Two things this explicitly is **not**: a behaviour tree, and a visual node-graph editor.

---

## 2. Expected output

### 2.1 What exists on disk when this is done

```
Assets/ApexionGame/
├── ApexionGame.Core/                          ← asmdef fixed: versionDefines block added
│   ├── ApexionGame.Core.asmdef
│   ├── Documentation~/                        ← this doc set
│   └── HFSM/
│       ├── HierarchicalStateMachine`2.cs                          the live machine
│       ├── HierarchicalStateMachine`2+Transitions.cs              LCA, exit/enter chains
│       ├── HierarchicalStateMachine`2+Async.cs                    async transition driver
│       ├── MachineDefinition`2.cs                baked, immutable, shared
│       ├── MachineBuilder`2.cs                   fluent front end
│       ├── MachineBuilder`2+Validate.cs          every MachineError case is produced here
│       ├── MachineRunner.cs                      batch tick
│       ├── MachineError.cs                       PolyEnum union
│       ├── StateBehaviour`1.cs                base, no per-state data
│       ├── StateBehaviour`2.cs                base, with `ref TData`
│       ├── AsyncStateBehaviour`1.cs
│       ├── Common/                            StateNode, Transition, StateInfo, …
│       ├── Contracts/                         IMachineControl, IMachineTickable, IMachineDebug
│       ├── Internals/                         ActiveSet, StateDataBlob, ScopeStack
│       └── Debugging/                         registry, transition log, overlay, ValidationDefines
├── ApexionGame.Editor/                        shared editor assembly — debugger window lives here
│                                               (ApexionGame.HFSM.Editor namespace), not a
│                                               per-module ApexionGame.Core.Editor sibling
├── ApexionGame.Core.Authoring/                MachineGraphAsset + catalogs
├── ApexionGame.Core.Tests/                    golden tests + benchmark
├── ApexionGame.Core.Samples/                  playground + EnemyBrain demo + scene
└── ApexionGame.Core.Samples.Editor/           playground inspector buttons

Plugins/SourceGenerator.ApexionGame/
└── ApexionGame.SourceGen.CodeRefactors/       + 3 HFSM refactorings
```

Full tree with every file and namespace: [HFSM - Layout](HFSM%20-%20Layout.md).

### 2.2 What the developer writes

Declaring a machine — this is the whole surface for a typical enemy:

```csharp
public enum EnemyState { Idle, Patrol, Combat, Chase, Attack, Flee }
public enum EnemyTrigger { AttackFinished, Staggered }

public sealed class EnemyContext
{
    public float Health;
    public float DistanceToPlayer;
    public bool SeesPlayer;
}

// Built once — e.g. a static field, or cached on a ScriptableObject.
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

Writing a state — with per-agent data, no allocation, no context bloat:

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

Spawning and running an agent:

```csharp
var machine = Brain.CreateInstance(new EnemyContext());   // auto-registered with the runner
// …later
machine.Fire(EnemyTrigger.Staggered);
machine.Dispose();                                        // unregisters, returns to the pool

// one place ticks everything
MachineRunner.Default.Tick(Time.deltaTime);
```

Complete signatures: [HFSM - API Surface](HFSM%20-%20API%20Surface.md).

### 2.3 What the developer sees while debugging

`ApexionGame > HFSM > Debugger` — one window, four panes, live at runtime:

```
┌─ HFSM Debugger ──────────────────────────────────────── [Auto ✓] [⏸] ─┐
│ Machine: [ EnemyBrain #0034            ▾ ]   17 live · 4 defs         │
├──────────────────────────┬────────────────────────────────────────────┤
│ ACTIVE PATH              │  STATE TREE                                │
│  Root                    │                 ┌────────┐                 │
│  └ Combat        4.82 s  │    ┌───────┐    │ Combat │◀── active       │
│    └ Attack      0.31 s  │    │ Idle  │───▶│════════│    (highlight)  │
│                          │    └───────┘    │ Chase  │                 │
│ history(Combat) = Chase  │        │        │ Attack │◀── active leaf  │
│                          │        ▼        └────────┘                 │
│ ── GUARDS (from Attack)  │    ┌────────┐        │                     │
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

- **Active path** — the live configuration with time-in-state, and the recorded history child of
  every composite that has one. With parallel regions, one path per region.
- **State tree** — the shape drawn with `Painter2D`, active branch highlighted, transition edges
  with arrowheads (direction is the whole meaning of the graph).
- **Guards** — every transition leaving the active configuration, with its **source text** and this
  frame's result. `c.Health < 20f → false (Health = 74.0)` answers "why didn't it switch" directly.
  The source text comes from `[CallerArgumentExpression]` on `.When(...)` — no naming required.
- **Transition log** — the last 32 transitions with frame, time spent in the source state, and the
  cause (which guard, which trigger, or a timer).

Plus an **on-screen overlay** that works in a development build, not just the Editor:

```
┌ HFSM · EnemyBrain #0034 ─────────┐
│ Root/Combat/Attack        0.31 s │
│ last: Chase→Attack  (guard)      │
└──────────────────────────────────┘
```

Full spec, including the poll-without-rebuild rule that the Stats debugger had to learn the hard
way: [HFSM - Debugging](HFSM%20-%20Debugging.md).

### 2.4 The checks that prove it works

| # | Check | How it is run |
|---|---|---|
| C1 | `ApexionGame.Core` and all five siblings compile, 0 error 0 warning | `unity test . --mode EditMode` compiles every assembly |
| C2 | Golden ordering tests pass — enter/exit sequences are exact strings, not "contains" | `ApexionGame.Core.Tests`, ~90 tests |
| C3 | Every `MachineError` case is produced by a real malformed builder input | one test per case |
| C4 | 500 machines × 20 nodes × 1000 ticks allocates **zero** GC after warm-up | `Is.Not.AllocatingGCMemory()` in the benchmark |
| C5 | 500 machines tick under 0.3 ms in Editor/Mono | `MachineBenchmarkTests`, numbers recorded in the Roadmap |
| C6 | Playground buttons 1–10 each run one scenario on a clean machine | manual, in the sample scene |
| C7 | The debugger shows a live machine, and its guard pane explains a stuck transition | manual, recorded as a screenshot in the Roadmap |
| C8 | Async transition cancelled mid-flight leaves the machine in a defined state | golden test + playground button 8 |

---

## 3. Steps

Dependency-ordered. One action per row. A fresh session with no memory of this conversation must be
able to execute this table from the file alone — where a row needs design detail it names the doc
section that holds it.

### Phase 1 — assembly scaffolding

| # | Action | Done when | Files |
|---|---|---|---|
| 1.1 | Add the `versionDefines` block from `EncosyTower.Core.asmdef` to `ApexionGame.Core.asmdef` verbatim; keep the existing `references` | `UNITASK`, `UNITY_COLLECTIONS`, `UNITY_MATHEMATICS`, `UNITY_BURST` evaluate true inside the assembly | `ApexionGame.Core/ApexionGame.Core.asmdef` |
| 1.2 | Create the five sibling asmdefs, each with the same `versionDefines` block ([Layout §2](HFSM%20-%20Layout.md#2-assemblies)) | Unity lists six `ApexionGame.Core*` assemblies, all compiling empty | `ApexionGame.Core.Editor/`, `.Authoring/`, `.Tests/`, `.Samples/`, `.Samples.Editor/` + asmdefs |
| 1.3 | `ValidationDefines.cs` — mirror the Stats file; add `HFSM_DEBUG` = `APEXION_HFSM_DEBUG`, plus the `DISABLE_APEXION_CHECKS` escape hatch | `[Conditional(ValidationDefines.HFSM_DEBUG)]` compiles | `HFSM/Debugging/ValidationDefines.cs` |
| 1.4 | `NodeIndex`, `TriggerId` — `[WrapType]` over `int`, and `TypeId`+`int` respectively ([Data Model §2](HFSM%20-%20Data%20Model.md#2-identity-types)) | both are `readonly partial struct`; `default` is the invalid value | `HFSM/Common/NodeIndex.cs`, `HFSM/Common/TriggerId.cs` |
| 1.5 | `MachineError` — `[PolyEnumFactoryFor]` wrapper over a `[PolyEnumStruct]` union, 12 cases incl. `Undefined` ([Data Model §6](HFSM%20-%20Data%20Model.md#6-error-contract)) | `MachineError.UnknownState(index)` compiles **through Unity**; `default(MachineError).ToString()` is meaningful | `HFSM/MachineError.cs` |

**Exit:** all six assemblies compile through Unity; `MachineErrorTests` covers `Undefined`, one payload
case's exact message, and `Prefix(...)`.

### Phase 2 — core: hierarchy, polled guards, timers

| # | Action | Done when | Files |
|---|---|---|---|
| 2.1 | `StateNode`, `Transition`, `StateNodeKind`, `HistoryMode`, `StateInfo`, `GuardInfo` structs ([Data Model §3](HFSM%20-%20Data%20Model.md#3-definition-side)) | each is `readonly struct` where stated; `StateNode` is 40 bytes or less | `HFSM/Common/*.cs` |
| 2.2 | `IMachineControl`, `IMachineTickable` contracts | `Fire`/`RequestTransition` reachable from a behaviour via `StateInfo.Control` | `HFSM/Contracts/*.cs` |
| 2.3 | `StateBehaviour<TContext>` + `StateBehaviour<TContext, TData>` with the internal `*Core` dispatch seam ([API §3](HFSM%20-%20API%20Surface.md#3-writing-a-state)) | a user assembly can subclass both; `TData` is reached as `ref`, no boxing | `HFSM/StateBehaviour`1.cs`, `HFSM/StateBehaviour`2.cs` |
| 2.4 | `MachineBuilder<TContext, TState>` — `.State<T>()`, `.Composite()`, `.Child<T>()`, `.Initial()`, `.EndComposite()`, `.To().When()/.After()` | the §2.2 declaration compiles and builds | `HFSM/MachineBuilder`2.cs` |
| 2.5 | `MachineBuilder+Validate` — bakes dense indices, sorts transitions by source, computes `Depth`, lays out state-data offsets 8-byte aligned, returns `Result<MachineDefinition, MachineError>` | every validation error in [Data Model §6](HFSM%20-%20Data%20Model.md#6-error-contract) has a producing input | `HFSM/MachineBuilder`2+Validate.cs` |
| 2.6 | `MachineDefinition<TContext, TState>` — immutable, `CreateInstance(ctx, tickMode)` | two instances of the same definition share behaviours and never share state data | `HFSM/MachineDefinition`2.cs` |
| 2.7 | `HierarchicalStateMachine<TContext, TState>` — active set, timers, `Tick`, polled guard evaluation deepest-first, any-state first ([Flows §2–3](HFSM%20-%20Flows.md#2-evaluation-order)) | a two-level machine transitions and ticks | `HFSM/HierarchicalStateMachine`2.cs` |
| 2.8 | `HierarchicalStateMachine+Transitions` — LCA, exit chain (reverse depth), enter chain (down to initial leaf) ([Flows §4](HFSM%20-%20Flows.md#4-executing-one-transition)) | golden ordering test passes for a 4-level tree | `HFSM/HierarchicalStateMachine`2+Transitions.cs` |

**Exit:** golden tests for enter/exit ordering across depths; every builder error case covered; a
3-level machine runs 1000 ticks with no allocation.

### Phase 3 — triggers, any-state, history

| # | Action | Done when | Files |
|---|---|---|---|
| 3.1 | `TriggerId.Of<TEnum>()` using `TypeId<TEnum>` so two trigger enums cannot collide ([DEC-006](HFSM%20-%20Decisions.md#dec-006)) | `Of(A.X)` != `Of(B.X)` when both are ordinal 0 | `HFSM/Common/TriggerId.cs` |
| 3.2 | `.On(trigger)` transitions; `Fire()` queued and drained at a defined point in the tick ([Flows §5](HFSM%20-%20Flows.md#5-triggers)) | a trigger fired inside `OnUpdate` is consumed on the same tick, exactly once | `HFSM/HierarchicalStateMachine`2.cs`, `HFSM/MachineBuilder`2.cs` |
| 3.3 | `.AnyState()` source, evaluated before every node-owned transition | any-state beats a leaf transition in the same tick | `HFSM/MachineBuilder`2.cs` |
| 3.4 | `.Priority(n)`, `.After(seconds)`, `.MinDuration(seconds)`, `.Internal()` | ordering test locks the tie-break rule from [Flows §2](HFSM%20-%20Flows.md#2-evaluation-order) | `HFSM/MachineBuilder`2.cs` |
| 3.5 | History — `WithHistory(Shallow \| Deep)`, recorded on exit, restored on re-entry ([Flows §6](HFSM%20-%20Flows.md#6-history)) | shallow returns to the child, deep returns to the full leaf path | `HFSM/HierarchicalStateMachine`2+Transitions.cs` |

**Exit:** trigger, any-state, priority, min-duration and both history modes each have a golden test.

### Phase 4 — parallel regions

| # | Action | Done when | Files |
|---|---|---|---|
| 4.1 | `.Parallel(state).Region(...)…EndParallel()` in the builder; `StateNodeKind.Parallel` | a parallel node with 3 regions builds | `HFSM/MachineBuilder`2.cs` |
| 4.2 | Active set becomes a bitset + per-node active child; enter/exit/update ordering per [Flows §7](HFSM%20-%20Flows.md#7-parallel-regions) | entering activates every region in declaration order; exiting reverses it | `HFSM/Internals/ActiveSet.cs`, `HFSM/HierarchicalStateMachine`2+Transitions.cs` |
| 4.3 | Reject a transition whose source and target sit in **different** regions of the same parallel node → `MachineError.TransitionCrossesParallelRegion` | validation test | `HFSM/MachineBuilder`2+Validate.cs` |

**Exit:** regions transition independently; a transition targeting outside the parallel node exits
all regions in reverse declaration order; history interacts correctly with regions.

### Phase 5 — async enter/exit

| # | Action | Done when | Files |
|---|---|---|---|
| 5.1 | `AsyncStateBehaviour<TContext>` with `OnEnterAsync`/`OnExitAsync`, using the EncosyTower `UnityTask` alias pattern under `#if UNITASK \|\| UNITY_6000_0_OR_NEWER` | compiles with UniTask present (it is, in this project) | `HFSM/AsyncStateBehaviour`1.cs` |
| 5.2 | `MachinePhase` (`Idle`/`Exiting`/`Entering`) + `AsyncPolicy` (`Queue`/`CancelAndReplace`/`Ignore`) ([Flows §8](HFSM%20-%20Flows.md#8-async-transitions)) | a second transition during an in-flight one behaves per the chosen policy | `HFSM/HierarchicalStateMachine`2+Async.cs` |
| 5.3 | Cancellation: `Dispose()` mid-transition cancels the token and skips the remaining chain without running enter hooks | no `OnEnter` runs after `Dispose` | `HFSM/HierarchicalStateMachine`2+Async.cs` |

**Exit:** cancel-and-replace, queue, and dispose-mid-transition each have a golden test.

### Phase 6 — runner, pooling, benchmark

| # | Action | Done when | Files |
|---|---|---|---|
| 6.1 | `MachineRunner` — `FasterList<IMachineTickable>`, deferred add/remove so registering inside a tick is safe | registering during `Tick` does not corrupt the loop | `HFSM/MachineRunner.cs` |
| 6.2 | `MachineRunnerBehaviour` MonoBehaviour + `MachineRunner.InstallIntoPlayerLoop()` | either route ticks the machines | `HFSM/MachineRunnerBehaviour.cs` |
| 6.3 | `HierarchicalStateMachine.Reset()` + instance pooling on the definition; `Dispose()` returns to the pool | 1000 spawn/despawn cycles allocate once | `HFSM/MachineDefinition`2.cs`, `HFSM/HierarchicalStateMachine`2.cs` |
| 6.4 | `MachineBenchmarkTests` — 500 machines × 20 nodes × 1000 ticks, no `[Explicit]` ([Roadmap §4](HFSM%20-%20Roadmap.md#4-benchmark)) | numbers recorded in the Roadmap; C4 and C5 hold | `ApexionGame.Core.Tests/MachineBenchmarkTests.cs` |

**Exit:** C4 (zero GC) and C5 (< 0.3 ms) confirmed by a real run, numbers pasted into the Roadmap.

### Phase 7 — debugging

| # | Action | Done when | Files |
|---|---|---|---|
| 7.1 | `IMachineDebug`, `MachineDebugRegistry`, debug info structs; machines auto-register under `APEXION_HFSM_DEBUG` ([Debugging §2](HFSM%20-%20Debugging.md#2-finding-live-machines)) | window finds a running machine with no user code | `HFSM/Debugging/*.cs` |
| 7.2 | `TransitionLog` — 32-entry ring buffer, cause recorded (`guard` / `trigger` / `timer` / `initial`), gated | log survives 10 000 transitions with no allocation | `HFSM/Debugging/TransitionLog.cs` |
| 7.3 | Guard source text via `[CallerArgumentExpression]` on `.When(...)`, stored in the definition | the guard pane shows `c.Health < 20f` without the user naming anything | `HFSM/MachineBuilder`2.cs` |
| 7.4 | `MachineDebuggerWindow` + `Views/` + `StyleSheets/` — active path, tree graph, guard pane, log; **shape-diff refresh, never rebuild the tree** ([Debugging §4](HFSM%20-%20Debugging.md#4-the-editor-window)) | scrolling and selection survive polling; C7 holds | `ApexionGame.Core.Editor/**` |
| 7.5 | `MachineOverlayPanel` + `MachineOverlayBehaviour` runtime UI, gated | overlay renders in a development build | `HFSM/Debugging/MachineOverlayPanel.cs`, `…/MachineOverlayBehaviour.cs` |

**Exit:** C7 — a screenshot of the window explaining one stuck transition, pasted into the Roadmap.

### Phase 8 — sample playground

| # | Action | Done when | Files |
|---|---|---|---|
| 8.1 | `MachinePlaygroundWorld` — plain C#, `IDisposable`, one method per scenario, shared by scene and window | no logic duplicated between the MonoBehaviour and any editor code | `ApexionGame.Core.Samples/MachinePlaygroundWorld.cs` |
| 8.2 | `MachinePlayground` `[ExecuteAlways]` MonoBehaviour + `MachinePlaygroundEditor` with 10 buttons, each rebuilding a clean machine, using `System.Action` (not `Invoke(name)`) | buttons work outside play mode; any press order is valid | `ApexionGame.Core.Samples/`, `ApexionGame.Core.Samples.Editor/` |
| 8.3 | `EnemyBrain` demo — the §2.2 machine with a fake context, plus scene `hfsm-playground.unity` hand-written with a fixed `.meta` GUID | scene opens and the buttons run | `ApexionGame.Core.Samples/**`, `…/Scenes/hfsm-playground.unity` |
| 8.4 | Register an `IVisualCommand` that toggles the overlay — **in the Samples assembly**, so `ApexionGame.Core` never references `EncosyTower.Core.Extended` ([DEC-011](HFSM%20-%20Decisions.md#dec-011)) | overlay toggles from the in-game console | `ApexionGame.Core.Samples/MachineOverlayCommand.cs` |

**Exit:** C6 — all ten buttons produce the documented output.

### Phase 9 — ScriptableObject authoring

| # | Action | Done when | Files |
|---|---|---|---|
| 9.1 | `GuardCatalog<TContext>` / `BehaviourCatalog<TContext>` — `StringId → delegate`/`instance` ([DEC-013](HFSM%20-%20Decisions.md#dec-013)) | a guard is resolvable by name with no boxing at evaluation time | `ApexionGame.Core.Authoring/GuardCatalog`1.cs` |
| 9.2 | `MachineGraphAsset : ScriptableObject` — serialized node and transition lists | asset round-trips through the inspector | `ApexionGame.Core.Authoring/MachineGraphAsset.cs` |
| 9.3 | `ToDefinition<TContext, TState>(catalogs)` → `Result<MachineDefinition, MachineError>` | an asset-authored machine behaves identically to the code-authored one | `ApexionGame.Core.Authoring/MachineGraphAsset+ToDefinition.cs` |

**Exit:** one test builds the same machine from code and from an asset and asserts identical
transition logs over 100 ticks.

### Phase 10 — code refactor provider

| # | Action | Done when | Files |
|---|---|---|---|
| 10.1 | *Generate HFSM state skeleton* — empty class → `sealed class X : StateBehaviour<TCtx>` with the three overrides, fully qualified | offered only on a class with no base type | `ApexionGame.SourceGen.CodeRefactors/**` |
| 10.2 | *Add per-state data struct* — rewrites `StateBehaviour<TCtx>` → `StateBehaviour<TCtx, X.Data>`, adds the nested struct and the `ref Data` parameters | existing override bodies preserved | same |
| 10.3 | *Generate machine skeleton from enum* — on an `enum`, emits the builder chain with one `.State<>()` per member | generated code compiles against the runtime assembly | same |
| 10.4 | Deploy: build `-c Release`, `.meta` labels `RunOnlyOnAssembliesWithReference` + `RoslynAnalyzer` (**no** `SourceGenerator`), written in the same command as the DLL copy | Unity does not auto-reference the DLL | `Plugins/SourceGenerator.ApexionGame/**` |

**Exit:** 9 refactor tests (offered / not offered / generated content) pass via `dotnet test`.

---

## 4. Goals and non-goals

### 4.1 Goals

| | |
|---|---|
| G1 | A state is an ordinary class with three overrides. No attribute, no generator, no registration. |
| G2 | One shared definition per machine shape; spawning an agent allocates one small instance, not a state tree. |
| G3 | Zero GC allocation per tick, per transition, and per trigger — asserted by a test, not by intent. |
| G4 | Hierarchy, triggers, history, parallel regions and async transitions with **written-down** ordering semantics that golden tests lock. |
| G5 | Four debug surfaces that answer real questions, above all *"why didn't it switch?"* |
| G6 | Every failure is a `Result<T, MachineError>` with a typed payload, never a bare `false` or a flat enum. |

### 4.2 Non-goals

| | Why |
|---|---|
| Behaviour trees, utility AI, GOAP | A different tool for a different problem. An HFSM that grows a BT inside it is neither. |
| Visual node-graph editor | Enormous surface, and the debugger already shows the graph read-only. Phase 9 authoring is a list-based inspector. |
| Burst / job tier, unmanaged state machines | The workload answer was "a few hundred agents". A managed design hits that budget with room to spare; adding a native tier would force states to drop all references. |
| Networking, rollback, determinism guarantees | No requirement stated. `float` timers make rollback unsound; do not pretend otherwise. |
| Serializing a **running** machine to a save file | Different problem (needs versioned node identity). `Reset()` + re-enter is the supported path. |
| Automatic transitions from animation events / Timeline | Fire a trigger from the callback; the machine should not know about Unity subsystems. |

---

## 5. EncosyTower module mapping

| Part of the feature | Module | How it is used |
|---|---|---|
| State ids, `ToStringFast()`, `ToIndex()`, member count | **EnumExtensions** | `TState : unmanaged, Enum`; user applies `[EnumExtensions]` to their enum to get alloc-free names in the debugger |
| `NodeIndex` | **TypeWraps** | `[WrapType]` over `int` |
| Trigger identity that cannot collide across enums | **Types** | `TypeId<TEnum>` combined with the ordinal |
| `MachineError` | **PolyEnumStructs** | `[PolyEnumFactoryFor]` wrapper over a `[PolyEnumStruct]` union, 12 cases |
| `BuildOrError`, `TryFire` return types | **Common** | `Result<T, MachineError>`, `Option<T>` |
| Builder accumulation, runner list, trigger queue | **Collections** | `FasterList<T>`; `.ReadOnly` views on the public debug surface |
| `TState → node index` map during Build | **Collections** + **Pooling** | `ArrayMap<int,int>` rented from `ArrayMapPool`, returned when Build ends |
| Exit/enter chain scratch buffers | **Pooling** | `FasterListPool<int>` — rented per transition, never a per-instance field |
| `OnEnterAsync` / `OnExitAsync` | **Tasks** | `UnityTasks` and the `using UnityTask = …` alias pattern, gated `#if UNITASK \|\| UNITY_6000_0_OR_NEWER` |
| Dev-only logging from the machine | **Logging** | `DevLogger` |
| Guard clauses stripped from release | **Debugging** | `Checks` + a local `ThrowHelper`, gated by `ValidationDefines` copied from the Stats module |
| Debugger window, graph, guard pane | **Editor.UIElements** + **UIElements** | Built in C#, `Views/` + `StyleSheets/`, no UXML |
| In-game overlay toggle | **VisualDebugging.Commands** | `IVisualCommand` — declared in the **Samples** assembly only |

### 5.1 What is hand-rolled, and why

**The state machine runtime itself.** EncosyTower has no state-machine module — verified by
searching the whole package for `statemachine`/`hfsm`, zero hits. The decision matrix lists
`PolyEnumStructs` under "a discriminated union of structs (state machine, command)", but that is a
*data* union: it models "this value is one of N shapes", not hierarchy, transitions, entry/exit
lifecycle, history or regions. Nothing in the package fits, so the runtime is new — while every
supporting primitive underneath it (ids, errors, collections, pools, tasks, logging, UI) comes from
the package rather than being rewritten.

**`PageFlows` is deliberately not reused.** It is a screen/popup navigation stack with its own
lifecycle and back-button semantics — genuinely a state machine, but one specialised for UI pages
and not general over an arbitrary context. Using it for enemy AI would mean fighting it. Game-flow
machines that *are* about screens should keep using `PageFlows.MonoPages`.

---

## 6. Where the rest of the design lives

| Question | Document |
|---|---|
| What exactly does the user type? | [HFSM - API Surface](HFSM%20-%20API%20Surface.md) |
| In what order do things run? | [HFSM - Flows](HFSM%20-%20Flows.md) |
| What is in memory, and how big? | [HFSM - Data Model](HFSM%20-%20Data%20Model.md) |
| Which file goes where? | [HFSM - Layout](HFSM%20-%20Layout.md) |
| How do the four debug surfaces work? | [HFSM - Debugging](HFSM%20-%20Debugging.md) |
| Why is it built this way? | [HFSM - Decisions](HFSM%20-%20Decisions.md) |
| What order do we build it in, and what could go wrong? | [HFSM - Roadmap](HFSM%20-%20Roadmap.md) |
