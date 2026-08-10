# HFSM — Layout

*[Tiếng Việt](HFSM%20-%20Layout.vi.md) · [Index](README.md)*

Exact folders, namespaces, file names and asmdef wiring. Follows
`.claude/skills/encosy-tower/references/structure-and-naming.md`; anything that departs from it is
flagged and justified.

---

## 1. Naming the module

| | |
|---|---|
| Folder | `HFSM/` |
| Namespace | `ApexionGame.HFSM` ([DEC-001](HFSM%20-%20Decisions.md#dec-001)) |
| Type names | **No abbreviation in identifiers** ([DEC-019](HFSM%20-%20Decisions.md#dec-019)) |

### 1.1 The three tiers of type name

| Tier | Rule | Examples |
|---|---|---|
| The machine | Full name, spelled out | `HierarchicalStateMachine<TContext, TState>` |
| Things that belong to a *machine* | `Machine*` | `MachineBuilder`, `MachineDefinition`, `MachineRunner`, `MachineError`, `MachineOptions`, `MachinePhase`, `IMachineControl` |
| Everything else | Bare concept name, no prefix | `StateNode`, `Transition`, `StateInfo`, `GuardInfo`, `NodeIndex`, `TriggerId`, `TransitionLog`, `HistoryMode`, `TickMode`, `AsyncPolicy` |

"Hierarchical" appears once, on the machine — it is a property of the machine, not of a builder or a
node. The namespace supplies the rest of the context, which is what a namespace is for.

The abbreviation survives only where it is an acronym in ALL CAPS and reads as one: the folder
`HFSM/`, the namespace segment `ApexionGame.HFSM`, the define `APEXION_HFSM_DEBUG`, the menu path
`ApexionGame > HFSM > Debugger`. Mixed-case `Hfsm` appears nowhere.

### 1.2 The collision this creates, and the fix

`Transition` collides with `UnityEngine.UIElements.Transition`. That matters in exactly one place —
the editor assembly, which needs both `using`s:

```csharp
using UnityEngine.UIElements;
using Transition = ApexionGame.HFSM.Transition;   // required in ApexionGame.Core.Editor
```

Most editor code touches `TransitionDebugInfo` and `TransitionLogEntry` rather than `Transition`
itself, so the alias is needed in one or two files. Flagged here so it reads as a known cost rather
than a surprise. `NodeIndex`, `StateInfo` and `GuardInfo` were checked against the referenced Unity
and EncosyTower assemblies — no collisions.

### 1.3 Why the namespace keeps the acronym

`ApexionGame.HFSM`, not `ApexionGame.Core.HFSM`: EncosyTower's assembly is `EncosyTower.Core` while
its modules live in `EncosyTower.PubSub`, `EncosyTower.Collections` — the `.Core` segment is an
assembly name, not a namespace prefix. Mirroring that keeps consumer `using`s short.

---

## 2. Assemblies

Originally planned as six siblings. What actually exists, as of phases 1–7:

| Assembly | Folder | References | Purpose |
|---|---|---|---|
| `ApexionGame.Core` | `Assets/ApexionGame/ApexionGame.Core/` | `EncosyTower.Core`, `UniTask`, `Unity.Burst`, `Unity.Collections`, `Unity.Mathematics` (already present) | the runtime |
| `ApexionGame.Editor` | `Assets/ApexionGame/ApexionGame.Editor/` | `ApexionGame.Core`, `EncosyTower.Core`, `EncosyTower.Editor` | **shared** editor assembly for every `ApexionGame.*` module — `MachineDebuggerWindow` lives here under `ApexionGame.HFSM.Editor`, not in a dedicated `ApexionGame.Core.Editor` |
| `ApexionGame.Tests.EditorMode` | `Assets/ApexionGame/ApexionGame.Tests.EditorMode/ApexionGame.Core/HFSM/` | project-wide test assembly | golden tests + benchmark, alongside the Stats module's tests under the same assembly |

`ApexionGame.Core.Authoring`, `.Samples` and `.Samples.Editor` (phases 9 and 8 respectively) have
not been created yet — they remain sibling assemblies as originally planned once those phases
start, since nothing else in the project consolidates authoring or sample code the way
`ApexionGame.Editor` already consolidates editor tooling.

`EncosyTower.Core.Extended` will be referenced **only** by `Samples` once phase 8 exists, so the
runtime assembly stays dependency-light ([DEC-011](HFSM%20-%20Decisions.md#dec-011)).

### 2.1 The `versionDefines` trap

`ApexionGame.Core.asmdef` currently has `"versionDefines": []`. Every `#if UNITASK`,
`#if UNITY_COLLECTIONS`, `#if UNITY_MATHEMATICS` inside it evaluates **false**, and the guarded code
silently disappears — including the whole async surface. Step 1.1 copies the block from
`EncosyTower.Core.asmdef` verbatim into all six asmdefs.

The failure mode is worth naming because it is silent: the assembly still compiles, the code just
is not there. Verify by putting `#if UNITASK` around a deliberate syntax error once and confirming
the compiler complains.

### 2.2 `allowUnsafeCode`

`ApexionGame.Core.asmdef` already has `"allowUnsafeCode": true`. The design does not currently need
it — `UnsafeUtility.As` is an ordinary method call, not an `unsafe` block — but leaving it on costs
nothing and matches `ApexionGame.Entities.Stats`.

---

## 3. Runtime tree

```
Assets/ApexionGame/ApexionGame.Core/
├── ApexionGame.Core.asmdef
├── AssemblyInfo.cs                          InternalsVisibleTo → .Tests, .Editor, .Authoring
├── Documentation~/                          this doc set (no .meta — Unity ignores `~`)
└── HFSM/
    ├── HierarchicalStateMachine`2.cs                            live machine: fields, Tick, Fire, properties
    ├── HierarchicalStateMachine`2+Transitions.cs                LCA, exit/enter chains, history record/restore
    ├── HierarchicalStateMachine`2+Async.cs                      the async driver and AsyncPolicy handling
    ├── HierarchicalStateMachine`2+Debug.cs                      IMachineDebug implementation, gated
    ├── MachineDefinition`2.cs                  baked arrays, CreateInstance, instance pool
    ├── MachineBuilder`2.cs                     fluent front end, scope cursor
    ├── MachineBuilder`2+Validate.cs            baking + every MachineError case
    ├── TransitionBuilder`2.cs           the `.To(...).When(...)` struct
    ├── MachineRunner.cs
    ├── MachineRunnerBehaviour.cs
    ├── MachineError.cs
    ├── StateBehaviour`1.cs
    ├── StateBehaviour`2.cs
    ├── AsyncStateBehaviour`1.cs             #if UNITASK || UNITY_6000_0_OR_NEWER
    ├── ThrowHelper.cs
    ├── Common/
    │   ├── StateNode.cs
    │   ├── Transition.cs
    │   ├── NodeIndex.cs
    │   ├── TriggerId.cs
    │   ├── StateInfo.cs
    │   ├── GuardInfo.cs
    │   ├── MachineOptions.cs
    │   ├── MachineDelegates.cs                 the three delegate declarations, one file
    │   ├── StateNodeKind.cs
    │   ├── HistoryMode.cs
    │   ├── MachinePhase.cs
    │   ├── AsyncPolicy.cs
    │   ├── TickMode.cs
    │   └── TransitionCause.cs
    ├── Contracts/
    │   ├── IMachineControl.cs
    │   └── IMachineTickable.cs
    ├── Internals/
    │   ├── ActiveSet.cs                 the bitset + active-child bookkeeping
    │   ├── StateDataBlob.cs             offset layout + typed slot access
    │   └── ScopeStack.cs                builder-only, tracks open Composite/Parallel
    └── Debugging/
        ├── ValidationDefines.cs
        ├── IMachineDebug.cs
        ├── MachineDebugRegistry.cs
        ├── StateNodeDebugInfo.cs
        ├── TransitionDebugInfo.cs
        ├── GuardDebugInfo.cs
        ├── GuardMeta.cs
        ├── TransitionLog.cs
        ├── TransitionLogEntry.cs
        ├── MachineOverlayPanel.cs
        └── MachineOverlayBehaviour.cs
```

### 3.1 Folder → namespace

| Folder | Namespace | Reason |
|---|---|---|
| `HFSM/` | `ApexionGame.HFSM` | the module's public surface, one flat namespace, one `using` for a consumer |
| `HFSM/Common/` | `ApexionGame.HFSM` | a sub-folder organises; it does **not** add a segment |
| `HFSM/Contracts/` | `ApexionGame.HFSM` | same |
| `HFSM/Internals/` | `ApexionGame.HFSM.Internals` | `Internals` is one of the segments that *does* earn its own namespace |
| `HFSM/Debugging/` | `ApexionGame.HFSM.Debugging` | likewise — and it matches `ApexionGame.Entities.Stats.Debugging` |

So a consumer writes exactly one `using ApexionGame.HFSM;` and gets the builder, the machine, the
behaviours, the enums and the errors. The debug surface is a second, deliberate opt-in `using`.

### 3.2 File naming

- `` HierarchicalStateMachine`2.cs `` — the backtick-arity form, matching `` StatStore`3.cs `` in the Stats module.
- `HierarchicalStateMachine`2+Transitions.cs` — a `+Aspect` partial split, matching `` StatAccessor`6+Batch.cs ``.
- One primary type per file. `MachineDelegates.cs` holding three delegate declarations is the single
  exception, because a one-line delegate per file is noise; it is called out here so it does not
  read as an accident.
- No `*.gen.cs` — this module has no code generator of its own. The Roslyn work in phase 10 is a
  *refactoring* provider, which writes into the user's file and generates nothing at build time.

---

## 4. Editor tree

Lives in the shared `ApexionGame.Editor` assembly (§2), not a dedicated sibling:

```
Assets/ApexionGame/ApexionGame.Editor/
├── ApexionGame.Editor.asmdef                includePlatforms: [Editor]
├── MachineDebuggerWindow.cs                    menu: ApexionGame > HFSM > Debugger
├── Views/
│   ├── MachineDebuggerView.cs                  the whole window body
│   ├── MachineDebuggerViewController.cs        polling, shape diffing, selection
│   ├── ActivePathView.cs                left pane: active nodes + times + history
│   ├── StateGraphView.cs                     Painter2D tree + transition edges
│   ├── GuardListView.cs                 outgoing guards with live results
│   ├── TransitionLogView.cs             the ring buffer as rows
│   └── StateNodeElement.cs                   one node box in the graph
└── StyleSheets/
    ├── MachineDebuggerWindow.tss               @import only
    ├── MachineDebuggerWindow.uss               var(--…) only, no literal colours
    ├── MachineDebuggerWindow_Dark.uss          colour values
    └── MachineDebuggerWindow_Light.uss         colour values
```

Namespace: `ApexionGame.HFSM.Editor` for the window, `ApexionGame.HFSM.Editor` for `Views/` too —
`Views` and `StyleSheets` add no namespace segment, same rule as `Common/`. The namespace is
independent of the assembly name; `ApexionGame.Editor.asmdef`'s own `rootNamespace` is
`ApexionGame.Editor`, a default for files that don't need HFSM's namespace specifically.

The runtime overlay (`MachineOverlayPanel`, `MachineOverlayBehaviour`, `MachineOverlayTheme`) is
**not** here — it must work in a player build, so it lives in `ApexionGame.Core/HFSM/Debugging/`
alongside the registry and the log, gated by the same
`#if (UNITY_EDITOR || DEVELOPMENT_BUILD || APEXION_HFSM_DEBUG) && !DISABLE_APEXION_CHECKS` the
`[Conditional]` methods use, written as a literal `#if` rather than through `ValidationDefines`
because the preprocessor cannot see a C# `const string`.

The debug info structs the window consumes (`NodeDebugInfo`, `GuardDebugInfo`, `GuardMarker`,
`TransitionDebugInfo`) live inside `HFSM/Debugging/IMachineDebug.cs`, next to the interface that
returns them — not as separate `StateNodeDebugInfo.cs` / `GuardDebugInfo.cs` / `GuardMeta.cs`
files as first sketched. `TransitionLog.cs` was never split out either; the ring buffer is private
fields and methods directly on `HierarchicalStateMachine`2+Debug.cs`.

Stylesheet paths are `const` strings built from `nameof(MachineDebuggerWindow)`, loaded through
`WithEditorStyleSheet(...)`. UI is built in C#; there is no `.uxml`
([UI Toolkit rules](.claude/skills/encosy-tower/references/ui-toolkit.md)).

---

## 5. The other three assemblies

```
Assets/ApexionGame/ApexionGame.Core.Authoring/
├── ApexionGame.Core.Authoring.asmdef
├── MachineGraphAsset.cs                        ScriptableObject: serialized nodes + transitions
├── MachineGraphAsset+ToDefinition.cs           asset → MachineDefinition, returns Result
├── GuardCatalog`1.cs                    StringId → Guard<TContext>
├── BehaviourCatalog`1.cs                StringId → StateBehaviour<TContext>
└── Common/
    ├── StateNodeRecord.cs                    [Serializable] authoring row
    └── TransitionRecord.cs

Assets/ApexionGame/ApexionGame.Core.Tests/
├── ApexionGame.Core.Tests.asmdef
├── MachineErrorTests.cs
├── MachineBuilderValidationTests.cs            one test per MachineError case
├── MachineOrderingTests.cs                     golden enter/exit/update sequences
├── MachineTransitionTests.cs                   LCA, self, internal, targeting a composite
├── MachineTriggerTests.cs
├── MachineHistoryTests.cs
├── MachineParallelTests.cs
├── MachineAsyncTests.cs
├── MachineStateDataTests.cs                    per-instance isolation, alignment, Reset zeroing
├── MachineRunnerTests.cs                       register/dispose during Tick, throwing machine
├── MachineBenchmarkTests.cs                    500 × 20 × 1000, zero-alloc assertion
└── Common/
    ├── TestContext.cs
    ├── TestStates.cs                        the enums used across tests
    └── RecordingBehaviour.cs                appends "Enter:Chase" etc. to a shared StringBuilder

Assets/ApexionGame/ApexionGame.Core.Samples/
├── ApexionGame.Core.Samples.asmdef
├── MachinePlaygroundWorld.cs                   plain C#, IDisposable, one method per scenario
├── MachinePlayground.cs                        [ExecuteAlways] MonoBehaviour
├── MachineOverlayCommand.cs                    IVisualCommand toggling the overlay
├── Enemy/
│   ├── EnemyContext.cs
│   ├── EnemyStates.cs                       the two enums
│   ├── EnemyBrain.cs                        the definition
│   └── Behaviours/                          IdleBehaviour.cs, ChaseBehaviour.cs, …
└── Scenes/
    ├── hfsm-playground.unity                hand-written, fixed .meta GUID
    └── hfsm-playground.unity.meta

Assets/ApexionGame/ApexionGame.Core.Samples.Editor/
├── ApexionGame.Core.Samples.Editor.asmdef
└── MachinePlaygroundEditor.cs                  ten buttons, System.Action, not Invoke(name)
```

Namespaces: `ApexionGame.HFSM.Authoring`, `ApexionGame.HFSM.Tests`, `ApexionGame.HFSM.Samples`,
`ApexionGame.HFSM.Samples.Editor`. Tests and samples use file-scoped namespaces; production code
uses block-scoped.

`Enemy/Behaviours/` adds no namespace segment — the demo's types all sit in
`ApexionGame.HFSM.Samples`.

---

## 6. Source-generator project

```
Plugins/SourceGenerator.ApexionGame/
└── ApexionGame.SourceGen.CodeRefactors/     ← already exists, extended in phase 10
    ├── HierarchicalStateMachineStateSkeletonRefactoring.cs
    ├── HierarchicalStateMachineStateDataRefactoring.cs
    └── HierarchicalStateMachineMachineSkeletonRefactoring.cs
```

Deployed to `Assets/ApexionGame/ApexionGame.Entities.Stats/SourceGenerators/` alongside the existing
DLLs, via the established `CopyBuildArtifacts` MSBuild target. `.meta` labels:
`RunOnlyOnAssembliesWithReference` + `RoslynAnalyzer`, **no** `SourceGenerator` label — a code
refactoring provider does not emit code at compile time. `isExplicitlyReferenced: 1`, every platform
`enabled: 0`.

Write the `.meta` in the **same command** as the DLL copy. Unity, if open, will otherwise import the
bare DLL and generate a default `.meta` with `isExplicitlyReferenced: 0`, auto-referencing it into
every assembly — the exact trap phase 3.11 of the Stats port hit.

---

## 7. Checklist before creating any file

1. Does the namespace follow §3.1 — flat for the public surface, a segment only for `Internals` and
   `Debugging`?
2. Are usings **outside** a block-scoped namespace, alphabetical, three groups with no blank line
   between: `System*`, then `ApexionGame*`/`EncosyTower*`, then `Unity*`?
3. Is the member order fields → ctors → indexers → properties → operators → methods → nested types,
   and within each `const` → `static readonly` → `static` → instance, then `public` → `protected` →
   `private`?
4. Are private fields `_camelCase`, private statics `s_camelCase`, consts `ALL_UPPER`?
5. If it moved, did its `.meta` move with it?
6. Does every type carrying an EncosyTower attribute have `partial`?
