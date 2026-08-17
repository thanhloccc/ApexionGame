# This project's own modules — check here before EncosyTower

Verified against the working tree on 2026-08-15.

EncosyTower is the standard library, but the studio has already built two first-party layers on top
of it. **Check this file before the EncosyTower decision matrix** — a state machine, a stat, or a
`Result` error contract has an in-house answer here, and reaching past it into EncosyTower (or
hand-rolling) is the wrong move.

Both live in the embedded UPM package `Packages/com.apexion.apexion-game/` — **not** under `Assets/`.

## Assembly inventory

| Assembly | Path (under `Packages/com.apexion.apexion-game/`) | What |
|---|---|---|
| `ApexionGame.Core` | `ApexionGame.Core/` | **HFSM** — hierarchical state machine. `ApexionGame.HFSM` namespace |
| `ApexionGame.Editor` | `ApexionGame.Editor/` | editor tooling for the above |
| `ApexionGame.Entities.Stats` | `ApexionGame.Entities.Stats/` | DOTS-free stats fork |
| `ApexionGame.Entities.Stats.Authoring` | `…​.Authoring/` | authoring surface |
| `ApexionGame.Entities.Stats.Editor` | `…​.Editor/` | stat debugger window — the UI Toolkit reference |
| `ApexionGame.Entities.Stats.Samples.Rts.Core` | `ApexionGame.Entities.Stats.Samples.Rts/Rts.Core/` | RTS sample core |
| `ApexionGame.Entities.Stats.Samples.Rts.Game` | `ApexionGame.Entities.Stats.Samples.Rts/Rts.Game/` | RTS sample + **runtime UI Toolkit reference** (`Hud/`) |
| `ApexionGame.Tests.EditorMode` | `ApexionGame.Tests.EditorMode/` | **the only first-party test assembly** — EditMode |
| `ApexionGame.Core.Samples` (+ `.Editor`) | `Samples~/ApexionGame.Core.Samples/` | HFSM playground |

Note the shapes that break the naming rules elsewhere in this skill, and are correct anyway:
the RTS sample is one folder (`ApexionGame.Entities.Stats.Samples.Rts/`) holding **two** assemblies
(`Rts.Core/`, `Rts.Game/`), and tests are centralised in `ApexionGame.Tests.EditorMode` rather than
one `*.Tests` sibling per assembly. Follow the existing shape; do not "fix" it.

---

## 1. HFSM — `ApexionGame.Core/HFSM/`, namespace `ApexionGame.HFSM`

**Use this for any state machine.** EncosyTower has no state machine module; `[PolyEnumStruct]` is a
discriminated union, not an FSM, and rolling a fresh one is never the answer here.

Status: phases 1–7 implemented and tested, 8–10 pending — read
`ApexionGame.Core/Documentation~/HFSM - Roadmap.md` before assuming a feature exists.

### Shape

```csharp
var machine = HierarchicalStateMachine<EnemyContext, EnemyState>
    .Define("EnemyBrain")
    .State<PatrolBehaviour>(EnemyState.Patrol)
    .Composite(EnemyState.Combat)
    .Child<ChaseBehaviour>(EnemyState.Chase)
    // … transitions, guards, triggers, timers …
    .Build();          // Result<…, MachineError>
```

| Parameter | Constraint | Meaning |
|---|---|---|
| `TContext` | `class` | per-agent blackboard, mutated inside `OnUpdate` — `class` deliberately, no `ref` plumbing |
| `TState` | `unmanaged, Enum` | node identity; one enum per machine shape. Debugger names come free from `[EnumExtensions].ToStringFast()` |
| `TData` | `unmanaged` | optional per-instance data owned by one behaviour |
| `TTrigger` | `unmanaged, Enum` | trigger identity |

Design commitments worth knowing before you propose an alternative — all recorded as `DEC-xxx` in
`HFSM - Decisions.md`:

- States are **classes deriving from `StateBehaviour<TContext>`**, wired by a fluent builder — not a
  source generator, not lambdas-only.
- Behaviours are **flyweights**: one shared definition (`MachineDefinition`) baked once, many cheap
  machine instances, per-instance data in a slot.
- Managed only — **no Burst/job tier**, by design, for a few hundred agents ticking per frame.
- Ticking works both ways: a central `MachineRunner` by default, manual ticking available.
- In scope: hierarchy, per-state timers, event triggers + polled guards, shallow/deep history, async
  enter/exit, parallel regions.
- Explicitly **not**: a behaviour tree, or a visual node-graph editor.

### Files

`HierarchicalStateMachine\`2.cs` + `+Transitions` (LCA, exit/enter chains) + `+Async` + `+Debug`;
`MachineDefinition\`2.cs`; `MachineBuilder\`2.cs` + `+Validate.cs` (**every `MachineError` case is
produced here**); `MachineRunner.cs` / `MachineRunnerBehaviour.cs`;
`StateBehaviour\`1.cs` / `\`2.cs` / `_Async.cs`; `AsyncStateBehaviour\`1.cs`;
`Common/` (`StateNode`, `Transition`, `TriggerId`, `GuardInfo`, `HistoryMode`, `TickMode`,
`AsyncPolicy`, `MachinePhase`, `NodeIndex`, `StateNodeKind`, `TransitionCause`, `MachineOptions`);
`Contracts/` (`IMachineControl`, `IMachineTickable`); `Internals/`;
`Debugging/` (`MachineDebugRegistry`, `TransitionLogEntry`, `MachineOverlayBehaviour/Panel/Theme`,
`GuardValueFormatter`, `ValidationDefines`).

### Docs — this is the project's doc-culture reference

`ApexionGame.Core/Documentation~/` is the **model doc set** for the plan-first gate: `HFSM - *.md`
with `.vi.md` mirrors, a `README.md` index with a reading order, `Overview` opening on
Request summary → Expected output → Steps. Read it before writing a plan of your own —
it is what `feature-docs.md` describes, done properly.

---

## 2. Entities.Stats — the DOTS-free fork

`ApexionGame.Entities.Stats` is a deliberate fork of `EncosyTower.Entities.Stats`, which is
**compiled out** in this project (`com.unity.entities` is not installed; the package asmdef has
`defineConstraints: ["UNITY_ENTITIES"]`).

- Its asmdef references only `EncosyTower.Core`, Burst, Collections, Mathematics — no
  `Unity.Entities`.
- Storage was rewritten around `StatBuffer<T>` / `StatBufferLookup<T>` / `StatOwnerHandle` /
  `StatOwnerSlot` instead of ECS chunks.
- It adds `Debugging/` (`StatDebugInfo`, `StatDebugRegistry`, `StatStoreDebug`) and
  `StatTypeTable` / `StatTypeTableGenerator`.
- Generator define: `APEXION_STAT_VALUE_TYPES_GENERATOR` (the package equivalent is
  `ENCOSY_STAT_VALUE_TYPES_GENERATOR`).

**Never "fix" it by adding `Unity.Entities` or by syncing it back to the package version.**

Docs: `ApexionGame.Entities.Stats/Documentation~/` — note it uses an **older `01-OVERVIEW.md`
numbering scheme**, not the `<Topic> - <Aspect>.md` convention. It is thorough and worth reading
(`guide/` has a 9-part walkthrough), but **do not copy its file naming** for new doc sets; copy
`ApexionGame.Core/Documentation~/HFSM - *.md` instead.

---

## 3. `Assets/Game/` — currently empty scaffolding

`Assets/Game/` holds `Game.Common/`, `Game.Data/`, `Game.Data.Authoring/`, `Game.Gameplay/` as
**empty folders with no `.asmdef` and no source files**. Only `Assets/Game/Input/`
(`Game.Input.asmdef`, `PlayerInputActions`) is real, plus `Addressables/` and `Scenes/`.

Earlier versions of this skill described a populated `Game.Common → Game.Data → Game.Gameplay` chain
with `PlayerError`, `WeaponError`, `EquipmentError` and feature docs under
`Game.Gameplay/Documentation~/`. **None of that is in the working tree, and none of it is in git
history.** Treat game-side gameplay code as greenfield: the first file in any of those folders also
creates the `.asmdef` (name = `rootNamespace` = folder name) with the `EncosyTower.Core` reference
and the copied `versionDefines` block — see `setup.md`.

Note the folder that breaks the pattern: `Game.Input` lives at `Assets/Game/Input/`, not
`Assets/Game/Game.Input/`. New game assemblies should use the folder-name-equals-assembly-name rule
rather than copying that.

**Verify before relying on this section** — it is the part most likely to have moved on.
