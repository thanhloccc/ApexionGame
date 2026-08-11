# HFSM — Debugging

*[Tiếng Việt](HFSM%20-%20Debugging.vi.md) · [Index](README.md)*

Four surfaces, all four requested. They exist to answer four different questions:

| Surface | Question it answers |
|---|---|
| Active path pane | *Where is it right now, and for how long?* |
| Guard inspector | ***Why didn't it switch?*** — the one that earns the whole feature |
| Transition log | *What just happened, and what caused it?* |
| Graph view | *What is this machine's shape, and which branch is live?* |
| On-screen overlay | all of the above, on a device, in a build |

---

## 1. What it costs when it is off

Everything in this document is gated behind `APEXION_HFSM_DEBUG`, defined automatically in
`UNITY_EDITOR` and `DEVELOPMENT_BUILD`, and removable with `DISABLE_APEXION_CHECKS`.

```csharp
namespace ApexionGame.HFSM.Debugging
{
    public static class ValidationDefines
    {
#if DISABLE_APEXION_CHECKS
        public const string UNITY_EDITOR = "__DISABLE_APEXION_CHECKS__";
        public const string HFSM_DEBUG = "__DISABLE_APEXION_CHECKS__";
        public const string RUNTIME_CHECKS = "__DISABLE_APEXION_CHECKS__";
#else
        public const string UNITY_EDITOR = "UNITY_EDITOR";
        public const string HFSM_DEBUG = "APEXION_HFSM_DEBUG";
        public const string RUNTIME_CHECKS = "APEXION_RUNTIME_CHECKS";
#endif
    }
}
```

With the define off, a release build pays:

| | |
|---|---|
| Registry registration | nothing — the call is `[Conditional]` |
| Transition log | nothing — the ring buffer is never allocated |
| Guard metadata (source text, names) | nothing at runtime; the `[CallerArgumentExpression]` string literals remain in the assembly, a few hundred bytes total ([DEC-015](HFSM%20-%20Decisions.md#dec-015)) |
| Guard evaluation recording | nothing — the recording branch is inside a `[Conditional]` method |

Same structure as `ApexionGame.Entities.Stats/Debugging/ValidationDefines.cs`, so one mental model
covers both modules.

---

## 2. Finding live machines

A machine is a plain managed object owned by whoever created it. There is no world and no singleton
to walk — the same problem the Stats debugger had to solve for native store memory.

The Stats module solved it with **voluntary registration**: the user constructs a
`StatStoreDebug<4>` and calls `StatDebugRegistry.Register`. That was right there, because a store is
raw native memory and registering it has a real cost.

Here, **registration is automatic**:

```csharp
// inside HierarchicalStateMachine<TContext, TState>'s constructor
RegisterForDebugging();

[Conditional(ValidationDefines.HFSM_DEBUG)]
private void RegisterForDebugging() => MachineDebugRegistry.Register(this);
```

`HierarchicalStateMachine<TContext, TState>` implements `IMachineDebug` directly in
`HierarchicalStateMachine`2+Debug.cs`, so there is no adapter object. Under the define, a machine appears in the debugger the moment it exists; with the
define off, `RegisterForDebugging` is not even a call.

This is a deliberate departure from the Stats pattern, justified by the difference in cost: a
managed object in a `List<IMachineDebug>` costs one reference, while `StatStoreDebug` wraps native memory whose
lifetime the registry must not extend ([DEC-016](HFSM%20-%20Decisions.md#dec-016)).

**`Dispose()` must unregister.** A registration outliving its machine is a leak that keeps the
context alive. `Dispose` does it; the registry also drops entries whose `IsAlive` is false when the
window polls, so a machine dropped without `Dispose` disappears rather than lingering as a ghost.

---

## 3. The transition log

A per-instance ring buffer, 32 entries, allocated only under the define.

```csharp
public readonly struct TransitionLogEntry
{
    public readonly NodeIndex From;
    public readonly NodeIndex To;
    public readonly TransitionCause Cause;   // Initial | Guard | Trigger | Timer | Request | History
    public readonly int CauseIndex;              // transition index — resolves back to guard text
    public readonly float TimeInSource;          // how long it sat in the source state
    public readonly int Frame;
    public readonly float RealtimeSinceStartup;
}
```

Also recorded, because they are the events people actually hunt for:

| Event | Recorded as |
|---|---|
| A trigger fired but matched nothing in the current configuration | `Cause = Trigger`, `To = default`, flagged `unmatched` |
| A transition was refused by `MinDuration` | not recorded — it would flood the log; the guard pane shows it live instead |
| An async transition was cancelled and replaced | two entries: the cancelled one flagged `cancelled`, then the replacement |
| An async transition was ignored under `AsyncPolicy.Ignore` | one entry flagged `ignored` |
| `Reset()` | one entry, `Cause = Initial` |

The buffer is a fixed array with a write cursor. 10 000 transitions allocate nothing — asserted in
`MachineRunnerTests`.

---

## 4. The editor window

`ApexionGame > HFSM > Debugger`, `ApexionGame.Core.Editor`, UI Toolkit built in C#.

```
┌─ HFSM Debugger ──────────────────────────────────────── [Auto ✓] [⏸] ─┐
│ Machine: [ EnemyBrain #0034            ▾ ]   17 live · 4 defs         │
├──────────────────────────┬────────────────────────────────────────────┤
│ ACTIVE PATH              │  STATE TREE                                │
│  Root                    │                 ┌────────┐                 │
│  └ Combat        4.82 s  │    ┌───────┐    │ Combat │◀── active       │
│    └ Attack      0.31 s  │    │ Idle  │───▶│════════│                 │
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

### 4.1 The rule the Stats debugger paid to learn

**Poll does not mean rebuild.**

The first Stats debugger polled every 0.25 s and rebuilt the entire UI tree each time. Scrolling
reset, selection was overwritten mid-interaction, the graph flickered, and dictionaries were
allocated continuously. The fix was to compare the *shape* of the data against what is displayed and
touch the tree only when the shape changed; numbers get written straight into existing labels.

The same tiering applies here, and it is a requirement, not advice:

| What changed | What the controller does | How often it actually happens |
|---|---|---|
| Set of live machines | `SetMachines` — rebuild the dropdown | on spawn/despawn |
| Selected machine's definition | `SetTopology` — rebuild graph nodes and edges | on selection change |
| Active configuration | `SetActive` — toggle USS classes on existing node elements | on transition |
| Times, guard results, log rows | `SetValues` — write into existing labels | **every poll** |

Four concrete bugs the Stats window hit, all avoidable here by construction:

1. The dropdown label embedded a live count (`"battle (17 owners)"`), so the selection was lost
   whenever the count changed. **Machine labels must be identity-only** — name and instance id, no
   live numbers.
2. `SetSelectionWithoutNotify` ran on every poll and overwrote what the user had just clicked. Only
   set selection when the selected item genuinely disappeared.
3. The sample renamed its store per scenario, so the store appeared to vanish on every button press.
   Playground machines keep a stable `DebugName`.
4. Three "nothing to show" messages rendered at once. One empty state, chosen by precedence.

Also carried over: an **Auto** toggle to freeze the view, and `TwoPaneSplitView` for the
table/graph divider.

### 4.2 The guard inspector

The pane that justifies the feature. For the active configuration it lists **every** outgoing
transition, in evaluation order ([Flows §2](HFSM%20-%20Flows.md#2-evaluation-order)), with:

| Column | Content |
|---|---|
| Marker | `●` eligible and would fire · `○` evaluated, false · `◌` not evaluated this frame (a higher-priority one already won) · `⏳` blocked by `MinDuration`, with the remaining time |
| Route | `Attack → Chase`, or `* → Flee` for an `AnyState` transition |
| Condition | the guard's **source text** from `[CallerArgumentExpression]`, or `on AttackFinished` for a trigger, or `after 2.0s` for a timer |
| Value | for a guard, the live result; where the guard is a single comparison against a context field, the field's current value is shown next to it |

`Health = 74.0` next to `c.Health < 20f` is the difference between "the guard is false" and knowing
*why*. It is best-effort: the value line appears only when the source text matches the simple
`c.<Field> <op> <literal>` pattern, parsed once at build time. When it does not match, the row shows
the expression and the boolean result and nothing more — a heuristic that degrades to something
still useful ([DEC-017](HFSM%20-%20Decisions.md#dec-017)).

**Evaluating guards for display must not affect the machine.** `EvaluateOutgoingGuards` calls the
same delegates the tick path calls, which is safe only because guards are expected to be pure. That
expectation is stated in the API docs and re-stated here; a guard with side effects will
double-apply them while the debugger is open, and that is a bug in the guard.

### 4.3 The graph view

Drawn with `Painter2D` in `generateVisualContent`, reading resolved `layout`, so it repaints after
`GeometryChangedEvent`. Directly reusing the approach of the Stats observer graph.

| Visual | Meaning |
|---|---|
| Node box, bright border | on the active path |
| Node box, filled | the active **leaf** |
| Node box, dimmed | not active |
| Nested box | a composite drawn as a container around its children — the hierarchy is the point |
| Dashed container | a parallel node; each region gets its own compartment |
| Edge with arrowhead | a transition; direction is the whole meaning |
| Edge, thick | fired within the last second |
| Edge, dotted | trigger-driven rather than polled |
| Small clock badge | the transition has a `MinDuration` or is an `.After()` |

Layout: nodes are placed by tree depth (columns) and sibling order (rows). Unlike the Stats graph,
there is no longest-path subtlety here — the structure is a tree, not a DAG, so depth is unambiguous.

**Not doing:** free node dragging, saved layouts, editing the machine from the graph. It is a
read-only view of a code-defined structure.

### 4.4 Clicking through

Selecting a row in the guard pane highlights the corresponding edge in the graph, and selecting a
node in the graph filters the guard pane to that node's transitions. The Stats debugger explicitly
listed this as *"worth having, not done"* — here it is in scope, because with hierarchy plus
parallel regions the number of rows is larger and the graph is the only way to keep them located.

---

## 5. The on-screen overlay

Runtime UI Toolkit, so it works in a development build where no Editor window exists.

```
┌ HFSM · EnemyBrain #0034 ─────────┐
│ Root/Combat/Attack        0.31 s │
│ last: Chase→Attack  (guard)      │
└──────────────────────────────────┘
```

| | |
|---|---|
| Element | `MachineOverlayPanel : VisualElement` in `HFSM/Debugging/`, built in C#, styled inline through a small `*Theme`/`*Widgets` pair — no serialized `StyleSheet`, so it works with no asset wiring |
| Host | `MachineOverlayBehaviour : MonoBehaviour` with a `UIDocument`; add it to a scene, or spawn it from code |
| Selection | cycles through registered machines, or filters by `DebugName` prefix |
| Content | active path, time in state, last transition and its cause; optionally the top 3 guard rows |
| Cost | polls at 10 Hz, writes into existing labels; whole panel is inside `#if APEXION_HFSM_DEBUG` |
| Toggle | an `IVisualCommand` registered from **`ApexionGame.Core.Samples`**, so `ApexionGame.Core` never references `EncosyTower.Core.Extended` ([DEC-011](HFSM%20-%20Decisions.md#dec-011)) |

The overlay deliberately shows less than the window. A device screen has no room for a graph, and
the questions asked on a device are "which state is it stuck in" and "what was the last thing that
happened".

---

## 6. Logging

`DevLogger` from `EncosyTower.Logging`, never `UnityEngine.Debug` directly.

| Event | Level | Gated by |
|---|---|---|
| A machine's `Tick` threw and it was unregistered | Error | always — this one must be loud in a release build too |
| A trigger was dropped unmatched | Warning | `APEXION_HFSM_DEBUG` |
| An async transition was cancelled | Info | `APEXION_HFSM_DEBUG` |
| `BuildOrThrow` failed | Exception, with `MachineError.ToString()` | always |
| Every transition | — | **never logged by default.** Per-transition logging at 500 agents is a wall of text; the transition log exists precisely so this is not needed. An opt-in `machine.VerboseLogging = true` covers the case where someone genuinely wants it for one agent. |

---

## 7. Test coverage for the debug layer

The debug layer needs tests too — a debugger that lies is worse than none.

| Test | Asserts |
|---|---|
| `Registry_MachineAppearsAndDisappears` | create → present; `Dispose` → absent |
| `Log_RingBufferWrapsWithoutAllocation` | 10 000 transitions, 32 retained, `Is.Not.AllocatingGCMemory()` |
| `Log_RecordsCauseAndSourceDuration` | a guard transition records `Cause = Guard`, the right transition index, and the real time in source |
| `Log_UnmatchedTriggerIsRecorded` | a trigger with no matching transition produces a flagged entry |
| `Guards_EvaluationDoesNotMutateMachine` | machine state before and after `EvaluateOutgoingGuards` is identical |
| `Guards_OrderMatchesTickOrder` | the guard pane's order equals the evaluation order from [Flows §2](HFSM%20-%20Flows.md#2-evaluation-order) |
| `DebugDisabled_NoRegistrationNoLog` | compiled with the define off, the registry stays empty and the log array is null |
