# Pitfalls & FAQ

*[Tiếng Việt](vi/09-PITFALLS.md) · [Guide index](README.md)*

Ordered by how much time they cost, worst first. What they have in common: they fail **silently**.

## 1. A forgotten observed stat

```csharp
readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observedStatHandles)
{
    if (kind == Kind.AddFromStat)
    {
        observedStatHandles.Add(observedStat);
    }
    // ... and the second handle field you added last week?
}
```

**Symptom:** a stat is right when you set it up and wrong afterwards. It never recalculates when its
input changes. No exception, no warning, no log line.

**Cause:** the modifier reads a stat it never declared, so the runtime built no reverse edge, so
nothing tells the dependent stat to recompute.

**Fix:** every `StatHandle` a modifier reads must be added here. Treat the hook as part of the field
declaration — add a field, extend the hook in the same edit.

**How to catch it:** open the Stat Debugger's graph view. A modifier reading a stat with no edge
drawn to it is exactly this bug.

## 2. Skipping the remap pass after a load

**Symptom:** stats load slightly low. Cross-owner buffs are just absent.

**Cause:** `StatOwnerHandle` is a slot index. Restored owners land in different slots, and a handle
stored inside a modifier now points at whoever occupies the old slot — or nothing. An unresolvable
handle contributes zero, same as a legitimately destroyed source.

**Fix:** restore every owner, build the `StatOwnerRemap`, call `TryRemapOwners`, then recalculate. In
that order. [Persistence](05-PERSISTENCE.md).

**How to catch it:** the sample's case 9 prints the value before and after the remap side by side.

## 3. Reading `StatChangeEvent` count as "number of stats that changed"

**Symptom:** damage numbers appear twice; a "stat changed" sound plays three times for one hit.

**Cause:** one write can emit several events for the same stat. On a DAG where a short and a long
branch meet, the join is computed from a stale input first and repaired on a later pass — both passes
emit an event.

**Fix:** group by `statHandle` and use the last value, or drive gameplay off the value you read after
propagation finished rather than off events.

This is not a bug to work around; it is how convergence works. See
[Core concepts](02-CONCEPTS.md#how-a-write-propagates) and [DEC-005](../07-DECISIONS.md#dec-005).

## 4. `store.CreateOwner()` instead of the generated builder

**Symptom:** handles into a fresh owner behave strangely, or stat index 1 holds the value you expected
at 0.

**Cause:** stat index 0 must be the None stat. Only `StatAPI.CreateStatOwnerHandle` — which the
generated `Builder.Build` calls — seeds it. A bare `CreateOwner()` gives you an owner with empty
buffers.

**Fix:** always create owners through `RpgStats.Builder.Build(ref store, out var owner)`.
`store.CreateOwner()` exists for the store's own bookkeeping, not for you.

## 5. A misplaced `readonly` on a hook

**Symptom:** a hook simply never runs. `ApplyInternal` does not fire, or ids come back as zero.

**Cause:** partial method declarations must match exactly, including `readonly`. A mismatch means the
compiler treats yours as an unrelated method and leaves the generated declaration unimplemented — no
error, because an unimplemented `partial void` is legal.

**Fix:** generate the skeleton with **Ctrl+.** ▸ *Generate stat modifier skeleton* and fill it in.
Never type the signatures from memory. The correct `readonly` placement is in
[Declaring stats](03-DECLARING-STATS.md#the-seven-hooks).

## 6. Sharing one `WorldData`

**Symptom:** occasional wrong values or corruption under load, and never in a small repro.

**Cause:** `StatWorldData` holds five scratch lists and one modifier-stack reference that every
mutating call clears and reuses. Two threads in there at once interleave scratch state.

**Fix:** one `WorldData` per update thread. For parallelism: collect in parallel, apply
single-threaded. [Jobs & performance](06-JOBS-AND-PERFORMANCE.md).

**Note:** nothing enforces this. The `AtomicSafetyHandle` guards protect the store and its buffers,
not `WorldData`.

## 7. Disposing in the wrong order

```csharp
// wrong
store.Dispose();
worldData.Dispose();
```

`WorldData` owns the scratch lists the store's buffers are read into. Dispose `worldData` first, then
`store`.

## 8. Never calling `worldData.Clear()`

Event lists grow for the `WorldData`'s whole lifetime. Clear once per frame, after whatever consumes
the events has run.

---

## FAQ

**Do I need Unity ECS?**
No. That is the point of this port. `com.unity.entities` is not referenced anywhere. You do need
`Unity.Collections`, `Unity.Mathematics`, `Unity.Burst` and `EncosyTower.Core`.

**Can I use it alongside ECS in the same project?**
Yes — it is unaware of ECS entirely. But if your stats already live in ECS, use
[`EncosyTower.Entities.Stats`](https://github.com/laicasaane/EncosyTower) instead; it is the same
algorithms with ECS storage. There is no two-way adapter and none is planned.

**Why `EncosyTower.Core` as a hard dependency?**
`ByteBool`, `Option<T>`, `HashValue`, `IIsValid`, the collection extensions and the
`[WrapType]` / `[EnumExtensions]` generators — around 450 lines that would otherwise have been
rewritten. The *Roslyn generators*, by contrast, are fully independent: no `ProjectReference` to
`EncosyTower.SourceGen.*`, so you can update the EncosyTower package without touching this one.

**One store or many?**
One per `[StatSystem]`. Collections sharing a system share a store, and that is what lets a modifier
reach across collections. Splitting the store means splitting the `[StatSystem]` — and then no modifier
can cross the boundary.

**How many owners can it hold?**
Bounded by memory. Three allocations per owner when cold, zero after warm-up thanks to slot pooling.
The benchmark runs 10k owners × 8 stats × 4 modifiers comfortably; writes stay O(owner) regardless of
world size.

**Are cycles really impossible?**
Yes, and the check happens at insert time. `TryAddStatModifier` walks the observer chain and returns
`false` — leaving the store untouched — if the modifier would close a loop. This is what makes
propagation without a visited-set safe. If the debugger's graph ever shows a red node, that is a
runtime bug.

**Can a modifier read a stat on a destroyed owner?**
It can try. The lookup returns `false` and your `ApplyInternal` decides what to do — contribute
nothing is the right answer, and what the sample does. There is no exception and no cleanup pass;
dependent stats keep their last computed value until something recalculates them. Use
`DestroyOwnerAndUpdateObservers` if they must react immediately.

**Can modifiers mutate themselves?**
Yes. `ApplyInternal` receives the modifier by ref, so a modifier can hold a countdown or a stack
count and update it during recalculation. Raise the trigger flag to have a `ModifierTriggerEvent`
queued for it.

**Why isn't `StatVariant` inspector-friendly?**
It has nineteen fields at `[FieldOffset(0)]` and Unity's serialiser ignores explicit layout. Use
`SerializableStatVariant` from the `.Authoring` assembly. [Tooling ▸ Authoring](07-TOOLING.md#authoring).

**Can I add a value type?**
Yes — `float3x3`, `quaternion`, whatever. It is an editor codegen step, documented in
[Tooling ▸ Regenerating the value-type tables](07-TOOLING.md#regenerating-the-value-type-tables). Mind
the size rules: usable at all needs `size <= maxDataSize`; usable as a pair needs
`size <= maxDataSize / 2`.

**What is the performance cost of propagation?**
O(reachable dependents), and it stops at the first stat whose value does not actually change. Worst
case is O(k²) on graphs dense with unequal-depth diamonds — a deliberate trade for correctness, since
the linear version computes wrong values. Numbers in
[Jobs & performance](06-JOBS-AND-PERFORMANCE.md#measured-numbers).

**Is it deterministic?**
Yes, for a given graph and sequence of calls. Iteration order is fixed and there is no hashing on the
propagation path. Handle *values* are not stable across sessions, which is why cross-owner links need
the remap pass.

**Can I port modifiers written for upstream?**
Almost. Change the type names, then add `RemapObservedStatsInternal` — `IStatModifier` gained
`RemapObservedStats` in this port, so upstream modifiers will not compile until the hook is there.
Full divergence list in the [decision log](../07-DECISIONS.md).

**Something is wrong and I cannot see why.**
In this order: open the Stat Debugger and compare `base → current` against what you expect; check the
graph view for a missing edge (pitfall 1); check the console for `AGS_*` diagnostics; then run the
[sample](../../../ApexionGame.Entities.Stats.Samples/README.md) case closest to your shape and diff
your setup against it.
