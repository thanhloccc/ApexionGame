# HFSM — Data Model

*[Tiếng Việt](HFSM%20-%20Data%20Model.vi.md) · [Index](README.md)*

Every type, the collection chosen for it, and why. The rule this document exists to enforce: the
**definition** is built once and shared, the **instance** is small and allocated per agent, and
nothing on the tick path allocates.

---

## 1. The two halves

```
        ┌───────────────────── shared, immutable, one per machine shape ─────────────────────┐
        │  MachineDefinition<TContext, TState>                                                  │
        │  ├─ StateNode[]              nodes, dense index, Depth/Parent/Kind baked            │
        │  ├─ Transition[]        sorted by source; each node holds (start, count)       │
        │  ├─ StateBehaviour<TContext>[]   one flyweight instance per node with a behaviour  │
        │  ├─ Guard<TContext>[]   guard delegates                                        │
        │  └─ GuardMeta[]         source text + priority   (only under APEXION_HFSM_DEBUG)│
        └────────────────────────────────────────────────────────────────────────────────────┘
                                             ▲  referenced, never copied
        ┌────────────────────────────────────┴──────── one per agent, ~250 B + blob ─────────┐
        │  HierarchicalStateMachine<TContext, TState>                                                            │
        │  ├─ TContext                the agent's blackboard                                 │
        │  ├─ int[]   _activeChild    per composite: which child is active, −1 = none        │
        │  ├─ int[]   _historyChild   per composite: the remembered child                    │
        │  ├─ float[] _timeInNode     per node                                               │
        │  ├─ ulong[] _activeMask     bitset over nodes                                      │
        │  ├─ byte[]  _stateData      the blob every StateBehaviour<_, TData> writes into    │
        │  └─ FasterList<TriggerId> _pendingTriggers  (bounded at 16)                    │
        └────────────────────────────────────────────────────────────────────────────────────┘
```

500 agents on a 20-node machine: **one** definition (~2 KB) plus 500 × (~250 B + blob). With four
behaviours declaring 16 bytes of data each, that is 500 × 314 B ≈ **153 KB total**. The alternative
— a state object tree per agent — would be 500 × 20 objects ≈ 10 000 allocations.

---

## 2. Identity types

| Type | Shape | Collection / storage | Why |
|---|---|---|---|
| `TState` | user's `enum`, `unmanaged` | — | Converted to a dense `int` at build time via `EnumExtensions.ToIndex()`. Never stored as an enum in a hot array. |
| `NodeIndex` | `[WrapType(typeof(int))] readonly partial struct` | value | A typed index so a node index cannot be passed where a transition index is expected. `default` is the invalid value, matching `StatIndex` in the Stats module. |
| `TriggerId` | `readonly struct { TypeId Type; int Value; }` — 8 bytes | value | `TypeId<TTrigger>` from `EncosyTower.Types` plus the ordinal, so two different trigger enums cannot collide on ordinal 0. |

`TState → NodeIndex` is resolved once, at build time. At runtime the machine works entirely in
dense `int` node indices; the enum reappears only at the API boundary (`CurrentState`,
`GetActiveStates`) and in the debugger.

---

## 3. Definition side

### 3.1 `StateNode` — 32 bytes

```csharp
public readonly struct StateNode
{
    public readonly int Parent;             // −1 for Root
    public readonly int FirstChild;         // −1 for a leaf
    public readonly int ChildCount;
    public readonly int InitialChild;       // −1 when Kind == Leaf
    public readonly int TransitionStart;    // index into Transition[]
    public readonly int TransitionCount;
    public readonly int StateDataOffset;    // byte offset into the instance blob, −1 if none
    public readonly ushort Depth;           // Root = 0; makes LCA O(depth) with no search
    public readonly byte KindAndHistory;    // StateNodeKind low nibble, HistoryMode high nibble
    public readonly byte BehaviourIndex;    // 0xFF = no behaviour
}
```

Stored as `StateNode[]`, not `FasterList<T>` — the count is final after `Build()`, and a plain array
is the cheapest thing to index into on the tick path. `FasterList<T>` is used while *building* it.

`Depth` being baked is what makes `LowestCommonAncestor` a loop over `Depth` steps instead of a
search ([Flows §4](HFSM%20-%20Flows.md#4-executing-one-transition)).

### 3.2 `Transition` — 32 bytes

```csharp
public readonly struct Transition
{
    public readonly int Source;             // −1 for an AnyState transition
    public readonly int Target;
    public readonly int GuardIndex;         // −1 = no guard
    public readonly int ActionIndex;        // −1 = no action
    public readonly TriggerId Trigger;  // default = polled, not triggered
    public readonly float MinDuration;
    public readonly short Priority;
    public readonly byte Flags;             // Internal | HasAfter | …
}
```

Stored as `Transition[]`, **sorted by source node, then priority descending, then declaration
order**. Because it is sorted, a node's outgoing transitions are a contiguous slice named by
`(TransitionStart, TransitionCount)` — evaluating a node is one linear walk over cache-adjacent
memory, no indirection, no per-node list object.

`AnyState` transitions occupy the slice at index 0, so they are also contiguous and are read first.

### 3.3 Behaviours and guards

| Array | Type | Note |
|---|---|---|
| `_behaviours` | `StateBehaviour<TContext>[]` | One instance per node that declared one. **Shared across every agent** — this is the flyweight. A behaviour with no `TData` may be shared across nodes too. |
| `_guards` | `object[]` holding `Guard<TContext>` or `GuardWithInfo<TContext>` | Two delegate shapes, discriminated by a flag on the transition, so the terse one-arg lambda costs nothing extra at call time. |
| `_actions` | `TransitionAction<TContext>[]` | `.Do(...)` bodies. |
| `_guardMeta` | `GuardMeta[]` | Source text from `[CallerArgumentExpression]`, node names. **Only allocated under `APEXION_HFSM_DEBUG`.** |

A behaviour instance holds **no per-agent field**. That is a hard invariant, not a convention: it is
what makes one definition serve 500 agents. Per-agent data goes in `TData` (§4.2) or in `TContext`.

---

## 4. Instance side

### 4.1 Arrays, sized by node count

| Field | Type | Size for 20 nodes | Why this type |
|---|---|---|---|
| `_activeChild` | `int[]` | 80 B | Indexed by node, written on every transition. A plain array beats any map at this size. |
| `_historyChild` | `int[]` | 80 B | Same shape; only touched for composites with history, but a parallel array is cheaper than a sparse map. |
| `_timeInNode` | `float[]` | 80 B | Advanced for active nodes only, but indexed by node so no lookup. |
| `_activeMask` | `ulong[]` | 8 B | Bitset. `IsActive(node)` is a shift and a test — needed because with parallel regions "is X active" is no longer "is X on the current path". |
| `_pendingTriggers` | `FasterList<TriggerId>` | 128 B cap | Bounded at 16; `Fire` beyond that returns `MachineError.TriggerQueueFull` rather than growing without limit. |

Total fixed overhead ≈ **250 B** for a 20-node machine, one allocation batch at construction, reused
across `Reset()` and pooling.

`ArrayMap<K,V>` was considered for `_activeChild`/`_historyChild` since only composites need entries.
Rejected: at 20–40 nodes a dense `int[]` is smaller than the map's node array *and* removes the hash
step from the transition path ([DEC-014](HFSM%20-%20Decisions.md#dec-014)).

### 4.2 The state-data blob

```
_stateData : byte[]        one allocation, laid out at build time

 offset 0        16        24                    40
 ┌──────────────┬─────────┬────────────────────┬─────────────┐
 │ ChaseData    │ (pad)   │ AttackData         │ FleeData    │
 │ 12 B, align 4│         │ 16 B, align 8      │ 8 B, align 8│
 └──────────────┴─────────┴────────────────────┴─────────────┘
     node 4         —          node 5              node 6
```

- Offsets are assigned in `Build()`, each aligned to `UnsafeUtility.AlignOf<TData>()`, and stored in
  `StateNode.StateDataOffset`. The total is `MachineDefinition.StateDataSize`.
- `StateBehaviour<TContext, TData>` reaches its slot with
  `UnsafeUtility.As<byte, TData>(ref blob[offset])`, which yields a **GC-tracked managed `ref`** — no
  pinning, no `GCHandle`, the collector may still move the array ([DEC-008](HFSM%20-%20Decisions.md#dec-008)).
- A machine whose behaviours declare no data allocates a zero-length array, not `null`, so the
  dispatch path has no branch.
- `Reset()` clears the blob with `Array.Clear`. Struct data therefore always starts zeroed — this is
  a documented guarantee, so `OnEnter` does not have to defensively initialise every field.

`NativeArray<byte>` was the alternative. Rejected: it would add a `Dispose` obligation and one native
allocation per agent, to buy a stable pointer this design never needs.

---

## 5. Transient allocations, and where they are avoided

| Path | What it needs | How it stays allocation-free |
|---|---|---|
| Executing a transition | the exit chain and the enter chain, as ordered node lists | Rented from `FasterListPool<int>` and returned in the same call. Not instance fields — a per-instance buffer would be 500 idle buffers to serve one transition at a time. |
| Building | node list, transition list, `TState → index` map, scope stack | `FasterList<T>` for the lists, `ArrayMap<int,int>` rented from `ArrayMapPool` for the map, both released at the end of `Build()`. Build is a cold path; clarity wins over micro-tuning, but nothing leaks. |
| Guard evaluation | nothing | Guards are `static` lambdas by convention, so no closure is allocated. A capturing lambda still works but allocates once at build time, never per tick. |
| Trigger queue | up to 16 ids | `FasterList` with capacity reserved at construction; `Clear()` on drain, never re-allocated. |
| Debug polling | node/transition/guard info lists | The `IMachineDebug` methods *append* to a caller-owned list, so the debugger window reuses the same lists every poll. Same contract as `IStatStoreDebug`. |
| `StateChanged` event | — | `Action<TState, TState>` with `TState` an enum would box under some invocation paths; it does not here because the delegate is generic over the value type. Verified by the zero-alloc benchmark rather than assumed. |

Per tick, per transition, per trigger: **zero GC allocation**, asserted by
`Is.Not.AllocatingGCMemory()` in `MachineBenchmarkTests`.

---

## 6. Error contract

`MachineError` is a `[PolyEnumFactoryFor]` wrapper over a `[PolyEnumStruct]` union — never a flat enum,
per the project's standing rule. Payloads are typed indices, never strings where an index exists.

```csharp
[PolyEnumFactoryFor(typeof(Error))]
public readonly partial struct MachineError
{
    private readonly FixedString64Bytes _prefix;
    private readonly Error _error;

    public MachineError Prefix(in FixedString64Bytes prefix) => new(_error, prefix);
    public override string ToString() => _error.ToMessage(_prefix).ToString();

    [PolyEnumStruct]
    readonly partial struct Error
    {
        partial interface IEnumCase
        {
            FixedString512Bytes ToMessage(in FixedString64Bytes prefix);
        }

        public readonly partial struct Undefined { … }
        public readonly partial record struct UnknownState(NodeIndex Node) { … }
        // …
    }
}
```

| Case | Payload | Raised when |
|---|---|---|
| `Undefined` | — | `default(MachineError)`; always present so `default.ToString()` is safe |
| `UnknownState` | `NodeIndex` | a transition targets an enum member that was never declared as a node |
| `DuplicateState` | `NodeIndex` | the same enum member declared twice |
| `NoInitialChild` | `NodeIndex` | a composite with no `Initial()` and no history is entered |
| `InitialChildNotAChild` | `NodeIndex parent, NodeIndex child` | `Initial()` names a node that is not a child of this composite |
| `LeafHasChildren` | `NodeIndex` | `Child()` called inside a node declared as a leaf |
| `UnbalancedScope` | `NodeIndex, int depth` | `EndComposite`/`EndParallel` missing or extra |
| `TransitionCrossesParallelRegion` | `NodeIndex source, NodeIndex target` | source and target sit in different regions of one parallel node |
| `CycleInInitialChain` | `NodeIndex` | `Initial` chain loops instead of reaching a leaf |
| `MachineNotRunning` | — | `Fire`/`RequestTransition` before `Start()` or after `Dispose()` |
| `TriggerQueueFull` | `TriggerId, int capacity` | more than 16 triggers queued for one instance |
| `TransitionInFlight` | `NodeIndex source, NodeIndex target` | `RequestTransition` during an async chain under `AsyncPolicy.Ignore` |

Call sites use the generated factories with parentheses: `MachineError.NoInitialChild(node)`. Every
case has a test that produces it from a real malformed input — a case nothing can raise is a lie in
the API surface.

Full rules for this shape: `.claude/skills/encosy-tower/references/structured-errors.md`.

---

## 7. Math

There is almost none, and that is worth stating rather than reaching for `Unity.Mathematics` out of
habit. The tick path does `float` addition on timers and `float` comparison on `MinDuration`.
`math.max` is used where a clamp is needed. No vectors, no quaternions, no jobs.

`Unity.Mathematics` is still referenced by the asmdef, because `GuardInfo` and user contexts sit
next to gameplay code that uses `float3`, and forcing a conversion at that boundary would be worse
than an unused reference.

---

## 8. Memory budget, measured targets

| Scenario | Definition | Per instance | 500 agents |
|---|---|---|---|
| 8 nodes, no state data | ~0.9 KB | 112 B | 55 KB |
| 20 nodes, 4 × 16 B state data | ~2.1 KB | 314 B | 153 KB |
| 40 nodes, parallel ×3, 8 × 16 B | ~4.4 KB | 620 B | 303 KB |

These are the numbers the benchmark in [Roadmap §4](HFSM%20-%20Roadmap.md#4-benchmark) checks against.
If a real measurement disagrees with this table, the table is wrong and gets corrected — not the
other way round.
