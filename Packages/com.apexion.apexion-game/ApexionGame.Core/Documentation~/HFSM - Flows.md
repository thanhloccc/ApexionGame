# HFSM — Flows

*[Tiếng Việt](HFSM%20-%20Flows.vi.md) · [Index](README.md)*

Every ordering rule in this file is a **contract**, locked by a golden test that asserts an exact
sequence string. Where a rule is arbitrary but had to be picked, the reasoning is in
[HFSM - Decisions](HFSM%20-%20Decisions.md).

The running example:

```
Root
├── Idle                          leaf
├── Patrol                        leaf
├── Combat            composite, history = Shallow, initial = Chase
│   ├── Chase                     leaf
│   └── Attack                    leaf
└── Flee                          leaf
```

---

## 1. One tick, top to bottom

```
Tick(dt)
 │
 ├─ 1. if Phase != Idle  →  AdvanceAsync(dt); return          (§8)
 │
 ├─ 2. timers:  TimeInMachine += dt
 │              for each active node n:  TimeInNode[n] += dt
 │
 ├─ 3. drain the trigger queue                                (§5)
 │        for each queued trigger, resolve once; stop at the first that fires
 │
 ├─ 4. if no trigger fired  →  evaluate polled guards         (§2, §3)
 │
 ├─ 5. if a transition was selected  →  execute it            (§4)
 │        (an async chain leaves Phase != Idle; the tick ends here)
 │
 └─ 6. update the active configuration, outermost → innermost (§3.3)
          for each active node in entry order:  OnUpdate(ctx, ref data, info, dt)
          triggers fired here land in the queue for the NEXT tick's step 3
```

Two consequences worth stating plainly, because both are easy to get wrong and both are tested:

- **At most one transition per tick per region.** A chain of transitions cannot fire in one frame.
  This makes a frame bounded and makes the transition log readable. A machine needing an immediate
  chain uses `.Internal()` actions instead ([DEC-012](HFSM%20-%20Decisions.md#dec-012)).
- **Transitions are decided *before* `OnUpdate` runs.** The state that ticks is the one the machine
  settled on this frame — a state never runs `OnUpdate` in the same frame it exits.

---

## 2. Evaluation order

For one region, transitions are collected from the **active leaf upward to the root**, and the first
one whose guard passes wins.

```
priority 1:  AnyState transitions            — sorted by Priority desc, then declaration order
priority 2:  the active leaf's transitions   — sorted by Priority desc, then declaration order
priority 3:  its parent's transitions
     …       …up to Root
```

| Rule | Why it is this way |
|---|---|
| `AnyState` first | It exists precisely to interrupt — "stunned from anywhere", "dead from anywhere". If it lost to a leaf transition it would be useless. |
| Deepest node next | The most specific state knows the most about the situation. `Attack → Chase` should beat `Combat → Idle`. |
| Within a node: `Priority` desc, then declaration order | Declaration order alone is enough most of the time; `Priority` is the escape hatch that does not require reordering the builder. |
| First match wins, evaluation stops | Guards may be side-effect free but are not required to be cheap. Evaluating the rest after a winner is chosen wastes time and confuses the guard pane. |

A transition is **eligible** only if all of these hold:

1. its `MinDuration` has elapsed in the source state (0 by default);
2. its trigger matched, if it has one — otherwise it is polled;
3. its guard returned `true`, if it has one; a transition with neither a trigger nor a guard nor a
   `.After()` is unconditional and fires the first time it is evaluated.

### 2.1 Worked ordering example

Active configuration `Root/Combat/Attack`, and this frame `c.Health` drops to 15 while
`c.DistanceToPlayer` is 4:

```
AnyState  → Flee    guard c.Health < 20f        → true    ✔ selected, stop
Attack    → Chase   trigger AttackFinished      (not queued)
Attack    → Chase   guard c.DistanceToPlayer>3f → would be true, never evaluated
Combat    → Idle    guard !c.SeesPlayer         → never evaluated
```

The guard pane shows all four rows with the two unevaluated ones greyed, so the reason the
"obvious" transition did not happen is visible rather than inferred.

---

## 3. The active configuration

### 3.1 What "active" means

A machine's active configuration is a **set** of nodes, not a single node:

- entering a leaf activates the leaf and every ancestor up to Root;
- entering a composite activates exactly one of its children (initial, or history);
- entering a parallel node activates **all** of its regions (§7).

With no parallel nodes, the active set is a single root-to-leaf path.
`HierarchicalStateMachine.CurrentState` returns the deepest leaf of region 0.

### 3.2 Entry order

Ancestors before descendants — **outermost first**. Entering `Combat/Chase` from `Idle`:

```
OnExit  (Idle)
OnEnter (Combat)          ← parent enters before its child
OnEnter (Chase)
```

Written this way because a composite's `OnEnter` typically sets up something its children rely on
(an animation layer, a nav-mesh agent config). The reverse order would have the child configure
itself and then be overwritten by its parent.

### 3.3 Update order

Same as entry order — **outermost first**, ancestors then descendants:

```
OnUpdate(Combat)
OnUpdate(Chase)
```

A composite that ticks can veto or adjust what its children then do in the same frame. It also
matches the entry order, which means one mental model instead of two.

### 3.4 Exit order

Exactly reversed — **innermost first**:

```
OnExit(Chase)
OnExit(Combat)
```

A child releases what it took before its parent releases the thing the child was holding.

---

## 4. Executing one transition

Given a selected transition from source `S` to target `T`:

```
1. lca ← LowestCommonAncestor(S, T)
2. exit  every active node from the deepest active descendant up to (but excluding) lca,
         innermost first, recording history on every composite that has it   (§6)
3. run   the transition's .Do(action), if any
4. enter every node on the path from lca (exclusive) down to T, outermost first
5. descend from T to a leaf:
        composite → its history child if recorded, else its Initial child
        parallel  → all regions, each descended the same way                 (§7)
        leaf      → stop
6. reset TimeInNode for every newly entered node; PreviousNode ← the old leaf
7. append to the transition log with its TransitionCause                 (Debugging §3)
```

`LowestCommonAncestor` walks the deeper node up until depths match, then walks both up in lockstep.
`Depth` is baked into `StateNode` at build time, so this is O(depth) with no allocation and no search.

### 4.1 Self-transitions

`.To(x)` where `x` is already the source **is external**: it exits and re-enters, so `OnExit` and
`OnEnter` both run. That is usually what a self-transition is for (restart the attack swing).

`.Internal()` makes it internal: neither `OnExit` nor `OnEnter` runs, only `.Do(action)`. Use it for
"react without restarting".

### 4.2 Targeting a composite directly

`.To(Combat)` is legal. Step 5 descends into `Combat`'s history child or `Initial` child. Targeting
a composite with no `Initial` and no history is a build-time error — `MachineError.NoInitialChild`.

---

## 5. Triggers

`Fire(trigger)` appends to a per-instance queue; it never transitions synchronously. The queue is
drained at step 3 of the next tick — or of the *same* tick, if `Fire` was called from outside the
tick.

| Situation | What happens |
|---|---|
| `Fire` from gameplay code, outside `Tick` | queued; consumed on the next tick's step 3 |
| `Fire` from inside `OnUpdate` (step 6) | queued; consumed on the **next** tick, because step 3 has already passed |
| `Fire` from inside `OnEnter`/`OnExit` (during step 2/4) | queued; consumed on the next tick |
| Two triggers queued, both match a transition | the first queued one that matches a transition wins; the rest stay queued |
| A queued trigger matches nothing in the current configuration | **dropped** at drain time, and recorded in the debug log as `unmatched` |

Dropping an unmatched trigger is deliberate: keeping it would mean a `Hit` fired while dead resurfaces
minutes later when the machine happens to reach a state that handles it
([DEC-007](HFSM%20-%20Decisions.md#dec-007)). The debug log makes the drop visible, which is the
part that matters.

`TryFire` returns `Result<Unit, MachineError>` and reports `MachineError.MachineNotRunning` or
`MachineError.TriggerQueueFull` (the queue is bounded at 16 per instance). `Fire` is the fire-and-forget
form that logs instead of returning.

---

## 6. History

`WithHistory(...)` on a composite records where it was when it left, and restores that on re-entry.

| Mode | Recorded on exit | Restored on entry |
|---|---|---|
| `None` (default) | nothing | always `Initial` |
| `Shallow` | the composite's **direct** active child | that child, then that child's own `Initial`/history below it |
| `Deep` | the full active path below the composite, down to the leaf | the whole path, exactly |

Shallow vs deep only differ when the remembered child is itself a composite:

```
Combat (deep)
└── Ranged (composite, initial = Aim)
    ├── Aim
    └── Fire        ← active when Combat was exited

Shallow → re-enters Combat/Ranged/Aim      (remembers Ranged, then takes Ranged's Initial)
Deep    → re-enters Combat/Ranged/Fire     (remembers the whole path)
```

Rules that the tests lock:

- History is recorded during the exit chain (step 2), **before** the composite's own `OnExit` runs,
  so `OnExit` can still read the active child.
- `Reset()` clears every history slot. Re-entering after a `Reset` uses `Initial`.
- A machine that has never entered a composite has no history for it, and uses `Initial`.
- A transition that targets a specific descendant (`.To(Attack)`) **overrides** history — the
  explicit target wins. History only applies when the entry point is the composite itself.

---

## 7. Parallel regions

A `Parallel` node owns `Region` children. Entering it activates every region; each region then
behaves as an independent composite.

```
Fighter (parallel)
├── Movement (region)      Idle | Walk | Dash
├── Weapon   (region)      Holstered | Drawn | Firing
└── Buff     (region)      None | Hasted
```

| Operation | Order |
|---|---|
| Enter the parallel node | `OnEnter(Fighter)`, then region 0 fully (region root then its descendants), then region 1, then region 2 — **declaration order, depth-first** |
| Update | `OnUpdate(Fighter)`, then each region in declaration order, each outermost-first |
| Evaluate transitions | region 0 first, then region 1, … — each region resolves independently and may fire its own transition in the same tick |
| Exit the parallel node | region 2 fully, region 1, region 0 — **reverse declaration order** — then `OnExit(Fighter)` |

### 7.1 The rules that keep it comprehensible

- **A transition may not cross regions.** A source in `Movement` and a target in `Weapon` is
  `MachineError.TransitionCrossesParallelRegion` at build time. Crossing would mean one region
  yanking another's leaf out from under it, with no defined ordering.
- **A transition leaving the parallel node exits every region**, in reverse declaration order, then
  the parallel node itself. This is the only way regions end.
- **"One transition per tick" is per region.** Three regions may each fire one transition in the
  same tick. The limit exists to bound a frame, and three independent regions are three independent
  frames' worth of work by design.
- **History on a parallel node is per region.** Each region records its own; restoring restores all
  of them.
- `AnyState` transitions are evaluated once per region, against that region's active leaf. An
  `AnyState → Stunned` that targets a node outside the parallel node therefore exits all regions —
  which is the desired behaviour for a stun.

---

## 8. Async transitions

A behaviour that overrides `OnEnterAsync` or `OnExitAsync` makes any transition through it
asynchronous. The machine then runs the transition across several ticks.

```
Phase: Idle  ──select transition──▶  Exiting
                                       │  await each OnExitAsync in the exit chain,
                                       │  innermost first; sync OnExit calls run inline
                                       ▼
                                    Entering
                                       │  await each OnEnterAsync in the enter chain,
                                       │  outermost first
                                       ▼
                                     Idle
```

While `Phase != Idle`:

- `Tick` advances `TimeInMachine` and pumps the async driver; **no `OnUpdate` runs** for any node —
  the source states have already exited and the target states have not finished entering;
- polled guards are not evaluated;
- `Fire` still queues.

### 8.1 What a second transition does mid-flight

`AsyncPolicy`, chosen per instance via `MachineOptions`:

| Policy | Behaviour |
|---|---|
| `CancelAndReplace` (default) | Cancel the in-flight chain's token. Nodes already entered are exited (synchronously, innermost first — their `OnExitAsync` is **not** awaited during a cancellation). Then start the new transition. |
| `Queue` | Remember the new target; run it as soon as the current chain reaches `Idle`. Only one is remembered — a third replaces the second. |
| `Ignore` | Drop the new transition. Logged as `ignored` in the transition log. |

`CancelAndReplace` is the default because the common async case is a screen or ability transition
where the newest intent is the correct one, and because a queue that silently plays a stale
transition later is the harder bug to find.

### 8.2 Cancellation and disposal

- `Dispose()` during a transition cancels the token, exits every node entered so far synchronously,
  and never runs a pending `OnEnter`.
- A cancelled `OnEnterAsync` must observe its `CancellationToken`; a behaviour that ignores it
  simply finishes late, and the machine discards the result. Nothing corrupts, but the state's
  side effects still land — this is documented as a pitfall rather than defended against.
- The machine never throws `OperationCanceledException` out of `Tick`. It is caught at the driver
  boundary and logged through `DevLogger` under `APEXION_HFSM_DEBUG`.

---

## 9. Runner

```
MachineRunner.Tick(dt)
 │
 ├─ _ticking ← true
 ├─ for i in 0.._machines.Count:  _machines[i].Tick(dt)
 ├─ _ticking ← false
 └─ apply deferred: remove first (swap-back), then add
```

- Registering or disposing a machine inside `Tick` is safe — both are deferred to the end of the
  pass. A machine disposed mid-pass still receives that pass's tick; a machine created mid-pass does
  not. Both are stated so behaviour under spawn-on-death is not a surprise.
- Removal is a swap-with-last, so **tick order is not stable** across removals. Nothing may depend
  on it. Ordering between machines is not a supported concept — use `TickMode.Manual` if you
  need a specific sequence.
- A machine whose `Tick` throws is caught, logged with its debug name, and **unregistered**, so one
  broken agent cannot take down the frame every frame. This is the one place the runtime swallows an
  exception, and it is gated to log loudly.

---

## 10. Sequence walkthrough

`Idle → Combat/Chase → Combat/Attack → Flee`, with `Combat` having shallow history:

```
frame  event                          calls, in order
─────  ─────────────────────────────  ─────────────────────────────────────────────
  1    Start()                        OnEnter(Idle)
                                      OnUpdate(Idle)
  …
 45    guard c.SeesPlayer → true      OnExit(Idle)
                                      OnEnter(Combat)
                                      OnEnter(Chase)              ← Initial, no history yet
                                      OnUpdate(Combat), OnUpdate(Chase)
  …
 92    guard Distance < 2f → true     OnExit(Chase)
                                      OnEnter(Attack)             ← lca = Combat, not exited
                                      OnUpdate(Combat), OnUpdate(Attack)
  …
131    AnyState guard Health<20 → true  history(Combat) ← Attack   ← recorded before OnExit
                                        OnExit(Attack)
                                        OnExit(Combat)
                                        OnEnter(Flee)
                                        OnUpdate(Flee)
  …
180    trigger Healed → To(Combat)    OnExit(Flee)
                                      OnEnter(Combat)
                                      OnEnter(Attack)             ← shallow history restores Attack
                                      OnUpdate(Combat), OnUpdate(Attack)
```

Frame 92 is the point of the whole design: `Combat` is not exited and re-entered when moving between
its own children. A flat FSM would have to, and every `OnEnter(Combat)` side effect would replay.

This exact sequence is `S01_FullLifecycle_OrderIsExact` in `ApexionGame.Core.Tests`, asserted as one
string.
