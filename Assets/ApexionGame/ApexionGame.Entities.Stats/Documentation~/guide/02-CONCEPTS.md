# Core concepts

*[Tiếng Việt](vi/02-CONCEPTS.md) · [Guide index](README.md)*

## A stat is a node, not a number

```
                 ┌──────────────────────────────────────────┐
                 │ Stat                                     │
   base value ──▶│  ValuePair { base, current }             │──▶ current value
                 │  ModifierRange (startIndex, count)       │
                 │  ObserverRange (startIndex, count)       │
                 │  UserData, ProduceChangeEvents           │
                 └──────────────────────────────────────────┘
                        │                        ▲
      its modifiers     │                        │ when this stat's value changes,
      read other stats ─┘                        │ every observer is recalculated
      (observed stats)                           │
```

Two values per stat:

- **base value** — what you author or write. `TrySetStatBaseValue` sets this.
- **current value** — what the modifier stack produced from the base value. This is what gameplay
  reads. With no modifiers the two are equal.

Two directed relationships, and they are the same edges seen from opposite ends:

- A **modifier** belongs to the stat it affects, and may *read* other stats — its **observed stats**.
- An **observer** entry is the reverse edge: "when I change, recalculate this stat." The runtime
  maintains these automatically whenever you add or remove a modifier. You never write them.

## Recalculating one stat

Whenever a stat needs recomputing, exactly this happens (`StatAPI.UpdateSingleStatCommon`):

1. Snapshot the stat, so it can be compared afterwards.
2. `stack.Reset(stat)` — clear the accumulator. Reset reads the stat's value *type*, which is why the
   identity elements come from `type.ZeroVariant()` / `type.OneVariant()` rather than literal `0f`
   and `1f`.
3. For each modifier in `ModifierRange`, call `modifier.Apply(reader, ref stack, out trigger)`. The
   modifier is passed **by ref**, so it may mutate its own state (a duration counter, say). If it
   raises `trigger`, a `ModifierTriggerEvent` is queued.
4. `stack.Apply(baseValue, ref currentValue)` produces the final current value, and the composer
   writes it back into the stat's `ValuePair`.
5. **If the value pair did not change, stop here.** This is the main brake on propagation — not a
   visited-set.
6. If the stat has `ProduceChangeEvents`, push a `StatChangeEvent { statHandle, prevValue, newValue }`.
7. Walk `ObserverRange` and enqueue each observer: same-owner observers into one worklist,
   other-owner observers into another.

Step 7's split exists so same-owner work can reuse the three buffers already open, without a second
lookup.

## How a write propagates

`TrySetStatBaseValue` writes the base value, then recalculates that stat, then drains the worklists —
breadth-first, until nothing changes. Cross-owner edges are followed exactly like same-owner ones;
there is no difference in behaviour, only in which buffers must be fetched.

Two properties make this safe:

**Cycles cannot exist.** `TryAddStatModifier` walks the observer chain before it commits, and returns
`false` if the new modifier would close a loop. The rejection happens *at insert time* — nothing is
mutated, no half-built modifier is left behind. So the graph is always a DAG and propagation always
terminates.

```csharp
// all three return false, and nothing changes in the store
accessor.TryAddStatModifier(hero.attack, Modifier.AddFrom(hero.attack), out _, ref worldData);
accessor.TryAddStatModifier(hero.hp,     Modifier.AddFrom(hero.attack), out _, ref worldData); // hp <-> attack
accessor.TryAddStatModifier(hero.hp,     Modifier.AddFrom(hero.defense), out _, ref worldData); // 3-hop loop
```

**A stat can be recalculated more than once per propagation, and that is correct.** This surprises
everyone, so it is worth being concrete. Take a diamond whose two branches have different lengths:

```
        ┌──────────────▶ moveSpeed        (1 hop)
   hp ──┤
        └──▶ attack ──▶ defense ──▶ moveSpeed   (3 hops)
```

The worklist reaches `moveSpeed` through the short branch first, while `defense` still holds its old
value. `moveSpeed` is computed — and it is wrong. Later `defense` updates, which pushes `moveSpeed`
back onto the worklist, and *that* pass repairs it.

Upstream had a visited-set that suppressed exactly this second pass, leaving the value permanently
wrong. This port removed it from both propagation paths. The trade is worst-case **O(k²)** instead of
O(k) on diamond-dense graphs, in exchange for always-correct values. Measurements, including the
error at every graph depth, are in [DEC-005](../07-DECISIONS.md#dec-005).

The practical consequence, if you consume events: **group `StatChangeEvent`s by `statHandle`.** One
write can emit several events for the same stat, and only the last one is the settled value.

## Storage

Three buffers per owner, and each owner is independent:

```
owner "hero"
  stats     : [ None ][ Hp ][ Attack ][ Defense ]        ← index 0 is always the None stat
  modifiers : [ Hp.m0 ][ Hp.m1 ][ Attack.m0 ]            ← grouped by affected stat
  observers : [ obs-of-Hp ][ obs-of-Attack ][ obs-of-Attack ]
                ▲                ▲
                │                └─ Attack.ObserverRange = (1, 2)
                └─ Hp.ObserverRange = (0, 1)
```

The `modifiers` and `observers` buffers are **block arenas**: stat *i*'s block must sit before stat
*i+1*'s. Every insert and removal shifts the `startIndex` of every stat behind it. This invariant is
the load-bearing wall of the whole system — if you modify the runtime, it is the thing to be careful
about.

Two consequences you can feel from the outside:

- Adding a modifier is O(stats of that one owner), not O(world). This is why storage is per-owner
  rather than one global arena.
- `DestroyOwner` does not free memory. It clears the three buffers, invalidates the version, and
  pushes the slot onto a free list; `CreateOwner` reuses it, capacity included. After warm-up,
  spawn/despawn churn does not allocate. Only `Dispose()` actually frees.

### Handles

| Type | Is | Valid when |
|---|---|---|
| `StatOwnerHandle` | `{ int Index; int Version; }` | the slot's version still matches |
| `StatIndex` | position in the owner's `stats` buffer | `value > 0` |
| `StatHandle` | `{ StatOwnerHandle owner; StatIndex index; }` | owner is valid |
| `StatHandle<TStatData>` | the same, typed | as above |
| `StatModifierHandle` | `{ StatHandle affectedStatHandle; uint modifierId; }` | — |

**Index 0 is the None stat.** Every real stat starts at 1. Only `StatAPI.CreateStatOwnerHandle` — and
the generated `Builder.Build` that calls it — seeds that slot. An owner created through a bare
`store.CreateOwner()` has no None stat, and handles into it will not behave.

**Handles for a destroyed owner fail silently.** The version no longer matches, so lookups return
`false` and propagation skips the edge. No exception. This is deliberate: a buff whose source died
should contribute nothing, not crash the frame. It also means a stale handle looks exactly like a
missing modifier contribution, which is worth remembering when a value is mysteriously low.

**`StatOwnerHandle` is not stable across a save.** It is a slot index. See
[Persistence](05-PERSISTENCE.md).

## What is thread-safe

| | |
|---|---|
| Parallel reads (`Accessor.ReadOnly`, `Reader`) | ✅ as many jobs as you like, while nobody writes |
| Parallel writes | ❌ by design, as upstream |
| Writes in a single-threaded `IJob` | ✅ the main pattern |
| Collect in parallel → apply single-threaded | ✅ via the three `DeferredUpdateStat*Job`s |
| Burst | ✅ everything is blittable; the generator emits non-generic job structs so Burst compiles them |

`StatWorldData` is the constraint: it holds one shared modifier-stack reference and five scratch
lists, and every mutating accessor method clears and reuses them. One per update thread, no sharing,
no reentrancy. Details and scheduling patterns in [Jobs & performance](06-JOBS-AND-PERFORMANCE.md).

## The four layers

Useful when reading the source or a stack trace:

```
L4  Generated per [StatSystem] / [StatCollection]
    RpgStatSystem.{Stat, ValuePair, StatModifier, Stack, StatObserver,
                   API, Reader, Accessor, Accessor.ReadOnly, Builder, WorldData,
                   DeferredUpdateStat{List,Queue,Stream}Job}
    RpgStats.{Type, TypeId, Index, Indices, StatIndices, StatHandles,
              Options, Params, Builder<T>, Accessor<T>, Reader<T>}
              ▲  thin, non-generic wrappers
L3  Algorithm — StatAPI, StatAccessor<6>, StatReader<2>, StatWorldData<5>, StatBuilder<6>
              ▲
L2  Storage seam — StatOwnerHandle, StatBuffer<T>, StatBufferLookup<T>, StatStore<3>
              ▲
L1  Contracts + values — IStat, IStatData, IStatModifier, IStatModifierStack, IStatObserver,
                         StatVariant, StatHandle, ranges, events
```

Everything you call in day-to-day code is L4. L3 is where the algorithms live and is what the design
docs describe. L2 is the layer that replaced ECS — and the only layer that had to be written from
scratch for this port.
