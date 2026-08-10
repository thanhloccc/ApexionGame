# HFSM — Roadmap

*[Tiếng Việt](HFSM%20-%20Roadmap.vi.md) · [Index](README.md)*

Phases, effort, test plan, risks. The step-by-step actions are in
[HFSM - Overview §3](HFSM%20-%20Overview.md#3-steps); this file is the shape of the whole job and
where it can go wrong.

---

## 1. Phases

LOC estimates are product code, tests excluded.

| Phase | Deliverable | LOC | Depends on | Status |
|---|---|---|---|---|
| 0 | This doc set | — | — | ✅ awaiting review |
| 1 | Six asmdefs, `ValidationDefines`, `NodeIndex`/`TriggerId`, `MachineError` | ~450 | 0 | ✅ done — one asmdef, see §1.2 |
| 2 | Core: nodes, builder, validate, definition, machine, LCA transitions | ~1 800 | 1 | ✅ done, tests green |
| 3 | Triggers, any-state, priority, min-duration, internal, history | ~500 | 2 | ✅ done, tests green |
| 4 | Parallel regions | ~450 | 3 | ✅ done, tests green |
| 5 | Async enter/exit + cancellation policy | ~400 | 4 | ✅ done, tests green |
| 6 | Runner, pooling, `Reset`, benchmark | ~350 | 5 | ✅ done, tests green |
| 7 | Debugging: registry, log, guard capture, editor window, overlay | ~1 900 | 6 | ✅ done — window is a flat depth/column graph, not nested boxes; see §1.2 |
| 8 | Sample playground, `EnemyBrain`, scene | ~700 | 7 | ⬜ |
| 9 | ScriptableObject authoring + catalogs | ~600 | 6 | ⬜ |
| 10 | Three code refactorings | ~700 | 2 | ⬜ |
| | **Total** | **~7 850** | | |

### 1.2 Where phases 1–7 diverged from this plan

Recorded here rather than silently left for someone to rediscover:

- **One editor assembly, not six.** The project settled on a single shared `ApexionGame.Editor`
  assembly for all `ApexionGame.*` editor tooling rather than a dedicated `ApexionGame.Core.Editor`
  sibling — `MachineDebuggerWindow` and its `Views/`/`StyleSheets/` live there, under the
  `ApexionGame.HFSM.Editor` namespace as planned. `ApexionGame.Core.Authoring`, `.Samples`,
  `.Samples.Editor` and a standalone `.Tests` were not created; tests instead landed in the
  project-wide `ApexionGame.Tests.EditorMode` assembly (`ApexionGame.Core/HFSM/` sub-folder), which
  predates this feature and already hosts the Stats module's tests the same way.
- **Debug info structs are co-located, not one-file-each.** `NodeDebugInfo`, `GuardDebugInfo`,
  `GuardMarker` and `TransitionDebugInfo` live inside `Debugging/IMachineDebug.cs` next to the
  interface that returns them, rather than as `StateNodeDebugInfo.cs`/`GuardDebugInfo.cs`/
  `GuardMeta.cs` — there was no second consumer that needed them split out, and grouping a small
  interface with the plain data it hands back matches how `NodeDebugInfo` was already written before
  this pass touched the file. `TransitionLog.cs` was never split out either; the ring buffer is a
  handful of private fields and methods directly on `HierarchicalStateMachine`2+Debug.cs`.
- **`Internals/ActiveSet.cs`, `StateDataBlob.cs`, `ScopeStack.cs` don't exist as separate types.**
  The active-set bitset operations are private methods on the machine itself
  (`HierarchicalStateMachine`2+Transitions.cs`), state-data offset math lives on
  `MachineBuilder`2+Validate.cs`, and the builder's open-scope tracking is a plain `List<int>` field
  on `MachineBuilder`2.cs`. Each turned out to be a handful of methods over an existing type's
  fields rather than a type worth its own file — revisit only if one of them grows a second reason
  to exist independently.
- **The graph view draws flat depth/column boxes, not nested containment.** HFSM - Debugging.md
  §4.3 describes a composite as a container drawn around its children. `StateGraphView` instead
  places every node (including composites) at `(depth, declaration order)`, styled by
  active/active-leaf/parallel/dimmed — the same layout `StatGraphView` uses for the Stats module,
  reused here because a tree needs no cycle detection or path relaxation to make it legible. Nested
  containment is a real amount of additional layout work for information the indented
  `ActivePathView` and the node styling already carry; left for a later pass if it turns out to be
  missed.
- **The transition log's cause column doesn't inline the guard's source text.** A row shows
  `guard`, `trigger: AttackFinished`, `timer`, etc. — not `guard: c.Dist…<2f` as sketched in Overview
  §2.3's mock. The guard pane is where the exact source text and live value live; duplicating it
  into every log row would need the log to carry a second string per entry for a case the guard pane
  already covers.
- **The in-game overlay toggle (`IVisualCommand`) is not wired.** It is explicitly scoped to
  `ApexionGame.Core.Samples` (DEC-011), which phase 8 has not created yet. Until then,
  `MachineOverlayBehaviour.SetVisible(bool)` is the toggle.

Phases 9 and 10 hang off earlier phases, not off phase 8 — they can be deferred indefinitely without
blocking anything. Phases 1–8 are the spine.

### 1.1 The order is not the dependency order

Phase 4 (parallel regions) is the riskiest piece and phase 7 (debugging) is the largest. Both sit
late. That is deliberate:

- **Parallel comes after history**, because history-per-region only makes sense once single-region
  history works, and doing both at once would make a failing test ambiguous about which feature
  broke.
- **Debugging comes after the runtime is complete**, because a debugger written against a moving
  API gets rewritten. The transition log is the one exception — it is cheap and would have helped
  during phases 2–5, so build the log (7.2) early if phase 3 or 4 starts fighting back.

### 1.2 Where a first usable version lands

**End of phase 3** is the first point where the system is genuinely useful: hierarchy, guards,
triggers, timers, history. That covers most enemy AI. A team could start writing brains there while
phases 4–7 continue.

Worth stating so the plan does not read as all-or-nothing: if scope has to be cut, cut from the
back. Parallel regions and asset authoring are the two most droppable items.

---

## 2. Test plan

`ApexionGame.Core.Tests`, run through the Unity CLI with the Editor **closed**:

```powershell
unity test . --mode EditMode --output results.xml --timeout 2400 --non-interactive
unity test . --mode EditMode --filter "ApexionGame.HFSM.Tests.MachineBenchmarkTests" --output bench.xml
```

Three notes carried over from the Stats port, each of which cost time there:

- **Unity must be closed.** Batch mode cannot open a project an Editor has locked.
- **`[Explicit]` tests are skipped even when named in `--filter`.** The benchmark therefore carries
  no `[Explicit]`.
- **Read the XML, not stdout.** CLI output is full of unrelated telemetry errors.

### 2.1 Golden ordering tests — the quality gate

These are the tests that matter. Each asserts a **single exact string**, not a set of "contains"
checks, because an ordering bug that produces the right calls in the wrong order is exactly what
"contains" misses.

`RecordingBehaviour` appends `Enter:Chase`, `Update:Combat`, `Exit:Attack` to a shared
`StringBuilder`; the test compares the whole transcript.

| # | Scenario | Locks |
|---|---|---|
| S01 | Full lifecycle `Idle → Combat/Chase → Combat/Attack → Flee → Combat` | [Flows §10](HFSM%20-%20Flows.md#10-sequence-walkthrough) end to end |
| S02 | Entry order is outermost-first, exit order innermost-first | [Flows §3.2](HFSM%20-%20Flows.md#32-entry-order), §3.4 |
| S03 | Update order equals entry order | §3.3 |
| S04 | Sibling transition inside a composite does **not** re-enter the composite | the whole reason hierarchy exists |
| S05 | 4-level tree, transition between two deep leaves under a common grandparent | LCA correctness |
| S06 | `AnyState` beats a leaf transition in the same tick | §2 |
| S07 | Deeper node beats shallower; `Priority` beats declaration order within a node | §2 |
| S08 | `MinDuration` blocks, then releases | §2 |
| S09 | Trigger fired in `OnUpdate` is consumed next tick, exactly once | §5 |
| S10 | Unmatched trigger is dropped and logged | §5, DEC-007 |
| S11 | Shallow history returns to the child; deep history returns to the leaf | §6 |
| S12 | Explicit `.To(descendant)` overrides history | §6 |
| S13 | Parallel: all regions enter in declaration order, exit in reverse | §7 |
| S14 | Parallel: two regions transition independently in the same tick | §7 |
| S15 | Parallel: leaving the parallel node exits every region | §7 |
| S16 | Async enter awaited across ticks; no `OnUpdate` while `Phase != Idle` | §8 |
| S17 | `CancelAndReplace` exits already-entered nodes before starting the replacement | §8.1 |
| S18 | `Dispose` mid-transition runs no pending `OnEnter` | §8.2 |
| S19 | Self-transition re-enters; `.Internal()` does not | §4.1 |
| S20 | `Reset` clears history, timers and state data | §6, Data Model §4.2 |

### 2.2 Other suites

| Suite | Count | Covers |
|---|---|---|
| `MachineBuilderValidationTests` | 12 | one per `MachineError` case, each from a real malformed input |
| `MachineErrorTests` | 4 | `Undefined`, exact message of a payload case, `Prefix`, `default(MachineError)` |
| `MachineStateDataTests` | 6 | per-instance isolation across 3 machines, alignment for `double`/`float3`, zeroing on `Reset`, zero-length blob path |
| `MachineRunnerTests` | 6 | register during tick, dispose during tick, throwing machine is unregistered, swap-remove does not skip a machine |
| Debug suite | 7 | [Debugging §7](HFSM%20-%20Debugging.md#7-test-coverage-for-the-debug-layer) |
| `MachineBenchmarkTests` | 5 | §4 below |

Target: **~90 tests**, matching the density the Stats module ended at (95).

---

## 3. What "done" means per phase

Copied here so a fresh session does not have to infer it:

| Phase | Exit criteria |
|---|---|
| 1 | Six assemblies compile through Unity; `MachineError` factories exist **after a Unity compile**, not just in the IDE |
| 2 | S01–S05 pass; every `MachineError` case has a producing input; 1 000 ticks allocate nothing |
| 3 | S06–S12 pass |
| 4 | S13–S15 pass; `TransitionCrossesParallelRegion` fires on a real crossing transition |
| 5 | S16–S18 pass |
| 6 | C4 (zero GC) and C5 (< 0.3 ms) measured, numbers pasted into §4 |
| 7 | The window explains one stuck transition; screenshot in this file; the debug suite passes |
| 8 | All ten playground buttons produce their documented output |
| 9 | An asset-built machine and a code-built machine produce identical transition logs over 100 ticks |
| 10 | 9 refactor tests pass via `dotnet test` |

---

## 4. Benchmark

`MachineBenchmarkTests`, 500 machines × 20 nodes × 4 behaviours with 16 B of state data each, 1 000
ticks. Editor/Mono, safety checks on — **relative numbers only**, a player build is much faster. What
this catches is an operation accidentally becoming O(machines) instead of O(1).

| # | Measures | Target |
|---|---|---|
| B01 | Build the definition once | < 5 ms |
| B02 | Create 500 instances | < 3 ms, one allocation batch each |
| B03 | 1 000 ticks with no transitions (guards all false) | < 0.15 ms/tick for 500 machines |
| B04 | 1 000 ticks where 10 % of machines transition each tick | < 0.30 ms/tick |
| B05 | Dispose 500 and recreate from the pool | recreate cheaper than B02 |
| B06 | **Zero GC**: B03 and B04 under `Is.Not.AllocatingGCMemory()` | 0 bytes after warm-up |

> Numbers to be filled in after phase 6 runs. Leaving the table empty until then is deliberate — a
> plan that pre-writes its own results is a plan that will not notice when reality disagrees.

---

## 5. Risk register

| # | Risk | Level | Mitigation |
|---|---|---|---|
| R1 | **Parallel regions interact badly with history and `AnyState`.** Three features whose combinations are where statechart implementations usually break. | **High** | Phase 4 lands after history and is tested against it (S13–S15 plus a history-inside-parallel case). If it fights back, it is the designated drop candidate — the rest of the system does not depend on it. |
| R2 | **Async cancellation leaves the machine in a half-entered configuration.** Entered some nodes, cancelled, and the active set no longer matches reality. | **High** | The active set is only committed after a chain completes; a cancelled chain exits exactly what it entered, tracked in a rented list. S17/S18 assert the resulting configuration explicitly, not just "no exception". |
| R3 | **`UnsafeUtility.As` alignment.** An unaligned `ref` to a `double` or `float3` is undefined behaviour on some platforms and will not fail in the Editor. | Medium | Offsets aligned to `AlignOf<TData>()` at build; `MachineStateDataTests` uses `double` and `float3` specifically; verified on an IL2CPP build before phase 8 closes. |
| R4 | **The debugger rebuilds its tree on every poll**, repeating the Stats window's first version — unusable scrolling and selection. | Medium | [Debugging §4.1](HFSM%20-%20Debugging.md#41-the-rule-the-stats-debugger-paid-to-learn) is written as a requirement with the four concrete bugs named. Review 7.4 against that list specifically. |
| R5 | **`versionDefines` forgotten on one of the six asmdefs**, so `#if UNITASK` silently removes the async surface from that assembly. | Medium | Step 1.1/1.2 copies the block into all six. Verify once by wrapping a deliberate syntax error in `#if UNITASK` and confirming the compiler complains. |
| R6 | **Guard lambdas capture accidentally**, allocating per machine build and, worse, sharing state between machines built from the same code path. | Medium | Convention is `static` lambdas everywhere in the docs and samples. The zero-alloc benchmark catches the allocation; a code review catches the sharing. Consider an analyzer later. |
| R7 | **Scope-based builder produces a confusing error** when `EndComposite()` is missed — the machine builds "successfully" with a wrong shape. | Low | `UnbalancedScope` is raised whenever the scope stack is non-empty at `Build()`, and it names the still-open node. Tested. |
| R8 | **500 interface calls per frame through `IMachineTickable`** is slower than expected on IL2CPP. | Low | B03/B04 measure exactly this. If it matters, the runner can hold a typed list per definition — a contained change behind the same API. |
| R9 | **Scope creep into a behaviour tree.** Every HFSM eventually gets asked for utility scoring or a sub-tree. | Low | Named as a non-goal in [Overview §4.2](HFSM%20-%20Overview.md#42-non-goals). A BT is a separate module that can *host* an HFSM, not grow inside it. |
| R10 | **Two machines sharing one behaviour instance with per-state data.** A behaviour is a flyweight, so sharing is legal — but the data slot is per node, and one behaviour instance used at two nodes gets two slots, which is correct but surprising. | Low | Documented in Data Model §3.3 and covered by a test asserting the two slots stay independent. |

---

## 6. Things deliberately not planned

Listed so a future session does not treat their absence as an oversight:

| | Why not |
|---|---|
| Burst/job tick path | Workload is a few hundred agents; a native tier would force states to hold no references. Revisit only with a measurement showing the managed path is the bottleneck. |
| Serializing a running machine into a save | Needs versioned node identity that survives an enum reorder. `Reset()` and re-enter is the supported path. |
| Visual node-graph *editor* | The debugger's graph is read-only and covers the "what shape is this" need. An editor is a project of its own. |
| Transition animations / blend trees | Belongs to the animation system; the HFSM fires the trigger and gets out of the way. |
| Networked / rollback-safe machines | `float` timers make rollback unsound. A deterministic variant would be a different design, not a flag. |
| An analyzer for non-`static` guard lambdas | Would be genuinely useful, but it needs the Roslyn analyzer project extended; revisit after phase 10 proves the refactoring provider works. |
