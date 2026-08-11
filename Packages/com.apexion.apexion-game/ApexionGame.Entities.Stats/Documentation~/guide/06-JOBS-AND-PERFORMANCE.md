# Jobs & performance

*[Tiếng Việt](vi/06-JOBS-AND-PERFORMANCE.md) · [Guide index](README.md)*

## The threading contract

| | |
|---|---|
| Parallel reads — `Accessor.ReadOnly`, `Reader` | ✅ any number of jobs, while nobody writes |
| Parallel writes | ❌ by design, as upstream |
| Writes from one single-threaded `IJob` | ✅ the main pattern |
| Collect in parallel → apply single-threaded | ✅ the three `DeferredUpdateStat*Job`s |
| Burst | ✅ all state is blittable; the generator emits non-generic job structs |

The binding constraint is `StatWorldData`. It holds one shared modifier-stack reference plus five
scratch lists, and every mutating accessor method clears and reuses them. Therefore:

- One `WorldData` per update thread. Never share one across concurrent jobs.
- Not reentrant. Do not call a mutating method while iterating an event list from the same
  `WorldData`.
- Nothing enforces this. `StatStore` and `StatBuffer<T>` carry `AtomicSafetyHandle` guards under
  `ENABLE_UNITY_COLLECTIONS_CHECKS`, so buffer misuse is caught; a shared `WorldData` is not.

Because there is no `SystemState` and no dependency graph, **your project owns every `JobHandle`.**
Nothing schedules on your behalf.

## Three scheduling patterns

### 1. Synchronous

The default, and correct for most games. A write propagates before the call returns:

```csharp
accessor.TrySetStatBaseValue(handles.hp, new ValuePair(newHp), ref worldData);
```

A write through an 8-deep chain measures around 2.8 µs. Thousands per frame are fine on the main
thread.

### 2. One update job per frame

Queue the handles that need recomputing during your frame, then drain them in one single-threaded
job:

```csharp
var toUpdate = new NativeList<StatHandle>(64, Allocator.TempJob);
// ... gameplay fills toUpdate ...

var handle = new RpgStatSystem.DeferredUpdateStatListJob {
    statAccessor  = accessor,
    statWorldData = worldData,
    statsToUpdate = toUpdate,
}.Schedule();

handle.Complete();
toUpdate.Dispose();
```

### 3. Parallel collect, single-threaded apply

The pattern for scale. Many jobs decide *what* changed in parallel; one job applies it:

```csharp
var toUpdate = new NativeQueue<StatHandle>(Allocator.TempJob);

var damage = new ApplyDamageJob {
    stats         = accessor.AsReadOnly(),          // read-only view — parallel-safe
    statsToUpdate = toUpdate.AsParallelWriter(),
}.Schedule(count, 64);

var update = new RpgStatSystem.DeferredUpdateStatQueueJob {
    statAccessor  = accessor,
    statWorldData = worldData,
    statsToUpdate = toUpdate,
}.Schedule(damage);

update.Complete();
toUpdate.Dispose();
```

Three collection types are supported, so you can pick the one your producer wants:

| Job | Consumes |
|---|---|
| `DeferredUpdateStatListJob` | `NativeList<StatHandle>` |
| `DeferredUpdateStatQueueJob` | `NativeQueue<StatHandle>` |
| `DeferredUpdateStatStreamJob` | `NativeStream.Reader` |

Anything more specific is a short `IJob` of your own that calls
`accessor.TryUpdateStat(handle, ref worldData)` in a loop. That is all the shipped jobs do.

## Burst

Use the **generated** jobs — `RpgStatSystem.DeferredUpdateStatListJob` — not the open generic ones in
`StatJobs.cs`. Burst does not compile open generics, so the generator emits a non-generic
`[BurstCompile]` wrapper around each closed job. This is also why no `[RegisterGenericJobType]` is
needed anywhere.

The generics in `StatJobs.cs` deliberately carry no `[BurstCompile]`; the attribute would do nothing
there.

Verified in Burst Inspector (2026-08-04): all three generated jobs appear under the stat system's
namespace, not greyed out, with real machine code in the Assembly panel. The three open generic
``DeferredUpdateStat*Job`6[…]`` entries are greyed out — Burst skipping them, exactly as designed.

`ThrowHelper`'s string interpolation does **not** block Burst, contrary to an earlier worry recorded in
the design docs. Its `[Conditional]` attributes hang off `UNITY_EDITOR` / `APEXION_*`, not
`ENABLE_UNITY_COLLECTIONS_CHECKS`, so they stay in the IL and Burst compiles them anyway.

## Measured numbers

`StatBenchmarkTests.cs`, five measurements over **10k owners × 8 stats × 4 modifiers**, where each
owner is an 8-deep chain (each stat observes the previous one). Unity 6000.3.20f1, Editor/Mono,
safety checks **on**, Burst **off**. All five run in 1.86 s.

| | Total | Per operation |
|---|---|---|
| B01 build the world (10k owners, 80k stats, 320k modifiers) | 291.4 ms | 0.911 µs/modifier |
| B02 `TrySetStatBaseValue` + propagation, once per owner | 28.2 ms | **2.824 µs/owner** |
| B03 `TryUpdateAllStats` per owner | 31.7 ms | 3.170 µs/owner |
| B04 read all 80k stats | 6.0 ms | 0.075 µs/stat |
| B05 destroy 10k owners | 1.7 ms | **0.166 µs/owner** |
| B05 rebuild (warm) | 266.1 ms | 0.832 µs/modifier |

> ⚠ **Relative numbers only.** Mono, safety checks on, Burst off. A player build is substantially
> faster. What this suite is *for* is catching an operation that accidentally became O(world) instead
> of O(owner) — not for quoting absolute throughput.

Two things the numbers confirm:

- **A write is O(owner), not O(world).** 2.824 µs to propagate through an 8-deep chain, regardless of
  the other 10k owners in the store.
- **Slot pooling works.** Destroy costs 0.166 µs/owner because it clears buffers and pushes the slot
  onto a free list rather than freeing memory. The rebuild afterwards is *cheaper* than the first
  build (266 ms vs 291 ms) because the allocations are reused.

A direct comparison against `DynamicBuffer` is not available: the project does not have
`com.unity.entities` installed, which is the whole point of the port.

## Complexity, and the one worst case

| Operation | Cost |
|---|---|
| Read a stat | O(1) |
| Write a stat | O(reachable dependents) — see below |
| Add a modifier | O(stats of that owner) for the range shift, plus a cycle walk over the observer chain |
| Remove a modifier | same |
| Create an owner | 3 allocations cold, 0 after warm-up |
| Destroy an owner | O(1) |

Propagation has **no visited-set**, which makes it worst-case **O(k²)** on graphs dense with
unequal-depth diamonds, where a topological-order propagation would be O(k). This is a deliberate
trade: the visited-set version is linear *and wrong* — it suppresses the second pass that repairs a
join computed from a stale input. Numbers at every depth are in
[DEC-005](../07-DECISIONS.md#dec-005).

In practice the brake is elsewhere. Recalculation stops the moment a stat's value pair does not
change, so propagation dies at the first stat whose result is unaffected. The quadratic case needs a
chain of diamonds all of whose values genuinely change — a shape worth avoiding for clarity, not just
for cost.

If you ever need both linear and correct, the design is known: give each stat a depth and make the
worklist a priority queue. It is recorded as future work, not implemented.

## Practical tuning

**`produceChangeEvents: false` for stats nothing listens to.** Per stat, cheap, and it removes both
the event push and the list growth.

**Size the store once.** `new StatStore<…>(initialOwnerCapacity, allocator)` — pick something close to
your peak owner count so the slot array does not grow repeatedly. Buffer capacity hints per owner go
in `store.CreateOwner(statCapacity, modifierCapacity, observerCapacity)`; the generated builder uses
the defaults (4 / 4 / 4).

**Batch when building characters.** `TryAddStatModifiersBatch` defers the range shifts and updates
once instead of once per modifier.

**Pass an explicit `Allocator` to the batch setters inside long jobs.** They default to
`Allocator.Temp`, which is not right for a job that outlives a frame. Both scratch containers are
disposed before returning, so any allocator works.

**Turn the runtime checks off in the shipping build if you need to.** `DISABLE_APEXION_CHECKS` neuters
every `[Conditional]` guard. The current guards are cheap enough that this has not been necessary —
measure before reaching for it.
