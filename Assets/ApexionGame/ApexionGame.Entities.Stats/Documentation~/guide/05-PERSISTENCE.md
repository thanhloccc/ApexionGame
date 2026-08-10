# Persistence

*[Tiếng Việt](vi/05-PERSISTENCE.md) · [Guide index](README.md)*

The library gives you blittable state and a remap pass. It does **not** choose a file format — no
JSON, no binary layout, no Addressables integration. Where the bytes go is your project's decision.

An owner's complete state is three buffers plus one counter. All of it is `unmanaged`, so it can go
straight into whatever writer you already have.

## Save

```csharp
var stats     = new NativeList<RpgStatSystem.Stat>(8, Allocator.Temp);
var modifiers = new NativeList<RpgStatSystem.StatModifier>(8, Allocator.Temp);
var observers = new NativeList<RpgStatSystem.StatObserver>(8, Allocator.Temp);

store.TryCopyOwnerTo(owner, stats, modifiers, observers, out var modifierIdCounter);

// write the four things to disk, however you like
```

Save the counter too. Without it, modifier ids restart and can collide with ids in restored
modifiers.

## Load

```csharp
store.TryRestoreOwner(
      stats.AsArray().AsReadOnlySpan()
    , modifiers.AsArray().AsReadOnlySpan()
    , observers.AsArray().AsReadOnlySpan()
    , modifierIdCounter
    , out var restoredOwner
);
```

`TryRestoreOwner` takes a free slot and writes the buffers back verbatim. **`StatIndex` values survive
unchanged** — stat order inside the buffer is preserved — so a saved `RpgStats` struct is still valid
against the restored owner. `StatOwnerHandle` is not: the owner almost certainly lands in a different
slot with a different version.

## The remap pass — do not skip it

`StatHandle` contains the owner's index and version. Every cross-owner modifier in your save therefore
points at a slot number that now means something else, or nothing.

```csharp
// 1. Restore every owner first, collecting old handle -> new handle
var map = new NativeHashMap<StatOwnerHandle, StatOwnerHandle>(count, Allocator.Temp);
map.Add(oldAuraOwner, newAuraOwner);
map.Add(oldHeroOwner, newHeroOwner);

// 2. One remap pass over the restored owners
var remap = new StatOwnerRemap(map);
accessor.TryRemapOwners(restoredOwners, in remap);   // or TryRemapOwner(owner, in remap) one at a time

// 3. Recalculate, so the repaired links produce values again
accessor.TryUpdateAllStats(newHeroOwner, ref worldData);

map.Dispose();
```

The remap does two things: it fixes `StatObserver.ObserverHandle` (the store can do that itself, since
`IStatObserver` exposes the property), and it calls `RemapObservedStatsInternal` on every modifier so
*you* can fix the handles stored in your own fields:

```csharp
partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
{
    if (kind == Kind.AddFromStat)
    {
        observedStat = remap.RemapOrNull(observedStat);
    }
}
```

`StatOwnerRemap` offers `TryRemap(StatOwnerHandle, out …)`, `TryRemap(StatHandle, out …)` — which keeps
the `StatIndex` and rewrites only the owner — and `RemapOrNull(StatHandle)`, which returns
`StatHandle.Null` for an owner that was not in the save.

### What skipping it looks like

Nothing. That is the problem.

A cross-owner modifier whose handle no longer resolves **contributes zero** and raises nothing — same
path as a legitimately destroyed source. The hero loads with slightly lower attack and no error
anywhere. The test `Remap_WithoutRemapPass_LosesTheLink` pins that behaviour so it cannot be
"fixed" into an exception by accident.

The sample's case 9 prints the value both before and after the remap so the difference is visible:
[`StatsPlaygroundWorld.Case09_SaveLoadRemap`](../../../ApexionGame.Entities.Stats.Samples/StatsPlaygroundWorld.cs).

An analyzer helps a little: `AGS_STAT_DATA_0006` warns when a modifier has a `StatHandle` field and no
`RemapObservedStatsInternal`. It cannot tell whether the implementation is *complete*, so a modifier
with two handle fields that remaps one of them passes silently. Keep the hook next to the fields it
maintains.

## Ordering rules

1. **Restore every owner before remapping anything.** The map has to be complete, or handles pointing
   at owners restored later resolve to null and are lost.
2. **Then remap.**
3. **Then recalculate.** Restored values are whatever was saved; they are correct only if nothing about
   the world changed. `TryUpdateAllStats` per restored owner is the safe default.
4. **Do not reuse handles captured before the save.** They point at destroyed owners. Rebuild them:
   `stats.GetStatHandles(restoredOwner)`.

## Things worth checking in your own save code

- **Version numbers are per slot, not global.** Two saves loaded in a different order produce different
  handles. Never persist a `StatOwnerHandle` as a cross-reference in your own data — persist your own
  stable id and rebuild the map at load time.
- **`modifierIdCounter` is per owner.** Modifier ids are local to an owner, so a
  `StatModifierHandle` from a previous session is only meaningful after the remap.
- **`ProduceChangeEvents` and `UserData` round-trip inside `Stat`,** so nothing extra is needed to
  preserve them.
- **Verify the type-id seeds have not moved.** `UserData` encodes a type-id derived from the
  `[StatCollection]` seed and the declaration order of `[StatData]` members. Reordering the members or
  changing the seed invalidates every existing save. Add new stats at the end.
