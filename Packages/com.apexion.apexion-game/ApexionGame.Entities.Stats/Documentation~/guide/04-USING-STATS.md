# Using stats

*[Tiếng Việt](vi/04-USING-STATS.md) · [Guide index](README.md)*

Two ways to reach the same runtime, and you will use both:

| | Reach | Use it for |
|---|---|---|
| **Collection accessor** — `RpgStats.Accessor.Create(owner, stats, accessor, worldData)` | one owner, one collection | typed, chainable calls; setup code |
| **System accessor** — `RpgStatSystem.Accessor` | any owner in the store | modifiers, cross-owner work, propagation |

The collection accessor is a thin typed wrapper *over* the system accessor. Everything it does, the
system accessor can do with an explicit `StatHandle`.

## Creating an owner

```csharp
var stats = RpgStats.Builder
    .Build(ref store, out var owner)
    .CreateAllStats(produceChangeEvents: true)
    .ToStats();

var handles = stats.GetStatHandles(owner);
```

`Build` allocates the owner slot **and seeds the None stat at index 0**, which is why you go through
it rather than `store.CreateOwner()`.

`ToStats()` hands back a small struct of `StatIndex` values — no pointers, no allocation. Keep it
wherever suits your project: a field on a character class, a `NativeList<RpgStats>`, a dictionary
keyed by your own entity id. Combine it with the owner handle to get `StatHandles` whenever you need
them.

Finer-grained construction, if you do not want every stat:

```csharp
var stats = RpgStats.Builder
    .Build(ref store, out var owner)
    .CreateStat(RpgStats.Hp.Params.Create(100f, produceChangeEvents: true))
    .CreateStat(RpgStats.Attack.Params.Create(10f))
    .ToStats();
```

The builder also has `CreateStats(...)` for several at once, `SetStat` / `SetOrCreateStat` for
re-authoring, and `Reset(composer)` to start over on the same owner.

**`produceChangeEvents` is per stat and defaults to off.** A stat with it off still propagates
normally — it just does not push a `StatChangeEvent`. Turn it on only for stats something actually
listens to; it is the cheapest optimisation available here.

## Reading

```csharp
// typed, through the collection accessor
var access = RpgStats.Accessor.Create(owner, stats, accessor, worldData);
access.TryGetStatData(out RpgStats.Hp hp, out _, out _);
float current = hp.currentValue;

// untyped, through the system accessor
if (accessor.TryGetStatValue(handles.hp, out var pair))
{
    float value = pair.GetCurrentValueOrDefault().Float;
}
```

Every `[StatData]` struct carries both halves — `baseValue` and `currentValue` — so one read gives you
what was authored and what the modifiers produced.

For read-only paths, take a view. It exposes only the getters, and several can be used concurrently
as long as nobody writes:

```csharp
var view = accessor.AsReadOnly();
view.TryGetStatValue(handles.attack, out var attack);
```

`accessor.GetStats(owner)` returns a `NativeArray<Stat>.ReadOnly` over the whole owner — remember
index 0 is the None stat.

## Writing

```csharp
accessor.TrySetStatBaseValue(handles.hp, new RpgStatSystem.ValuePair(new StatVariant(120f)), ref worldData);
```

The write sets the base value, recalculates the stat, and propagates to everything that depends on it
before returning. There is no "commit" step and no deferred flush.

Through the collection accessor the same write is typed and chainable:

```csharp
RpgStats.Accessor.Create(owner, stats, accessor, worldData)
    .TrySetStatBaseValue(new RpgStats.Hp(120f), out _)
    .TrySetStatBaseValue(new RpgStats.Attack(14f), out _);
```

Note the shape: the return value is the accessor so calls chain, and success comes out through
`out bool`. `out _` is the normal thing to write when a failure would mean a bug elsewhere.

Other writes on the system accessor:

| Method | |
|---|---|
| `TrySetStatCurrentValue` | overwrite the computed value — the next recalculation overwrites it back |
| `TrySetStatValues` / `TrySetStatData` | both halves at once |
| `TrySetStatProduceChangeEvents` | toggle the per-stat event flag |
| `TrySetStatUserData` | rewrite the type-id payload |
| `TryUpdateStat(handle, ref worldData)` | recalculate on demand and propagate |
| `TryUpdateAllStats(owner, ref worldData)` | recalculate every stat of one owner |

`TryUpdateStat` is what you call when an input changed outside the graph's knowledge — a modifier that
reads real time, or an observed owner that was destroyed.

### Batch writes

Three batch calls on the system accessor take many handles at once, group them by owner internally,
and propagate once:

```csharp
accessor.TrySetBaseValueToStats(paramsForStats, results, ref worldData, Allocator.TempJob);
```

`TrySetCurrentValueToStats` and `TrySetDataToStats` mirror it. The trailing `Allocator` defaults to
`Allocator.Temp`; pass something else inside a long-running job, where `Temp` is not appropriate. Both
internal containers are disposed before the call returns, so any allocator is safe.

The collection accessor exposes the same thing driven by an options bundle, which reads better in
setup code:

```csharp
RpgStats.Accessor.Create(owner, stats, accessor, worldData)
    .TrySetBaseValueToStats(new RpgStats.Options.Data(
          hp: new RpgStats.Hp(100f)
        , attack: new RpgStats.Attack(10f)
    ));
```

`Options.Data`'s parameters are `Option<TStatData>` — the stat structs themselves, not
`StatDataParams`. `Params.Create` belongs to `Builder.CreateStat` / `SetStat`, which take flags and a
handle as well as a value. An omitted parameter here is `Option.None`, so the stat keeps whatever it
had rather than being written to zero.

There is also a parameter-per-stat overload taking `Option<float>` (or whatever the stat's value type
is) if you would rather not name the types:

```csharp
RpgStats.Accessor.Create(owner, stats, accessor, worldData)
    .TrySetBaseValueToStats(hp: 100f, attack: 10f);
```

## Modifiers

```csharp
accessor.TryAddStatModifier(
      handles.attack                                          // the stat being affected
    , RpgStatSystem.StatModifier.AddFrom(aura.attackBonus)    // your modifier struct
    , out var modifierHandle
    , ref worldData
);
```

What happens inside, in order:

1. Your `AddObservedStatsToListInternal` is called to learn what the modifier reads.
2. Every observed stat is checked for existence — if one is missing, the call fails **before anything
   is mutated**. No half-added modifier.
3. The modifier gets an id, local to the owner.
4. The observer chain is walked; if the modifier would close a cycle, the call returns `false` and
   nothing changes.
5. The modifier is inserted into the owner's modifier block and the ranges behind it shift.
6. Reverse edges are written for each observed stat.
7. The affected stat is recalculated, and the change propagates.

So a `false` return means one of: the affected stat does not exist, an observed stat does not exist,
or the modifier would create a cycle. In all three cases the store is untouched.

Removing:

```csharp
accessor.TryRemoveStatModifier(modifierHandle, ref worldData);   // one
accessor.TryRemoveModifiersOfStat(handles.attack, ref worldData); // all on one stat
```

Removal propagates exactly like adding does.

### Batch adding

`TryAddStatModifiersBatch` adds many at once, deferring the range shifts and doing a single update at
the end. Meaningfully faster when building a character out of a dozen buffs. The constraint is
inherited from upstream and worth knowing: while the shift is deferred, the `startIndex` of stats
behind the insertion point is stale, so nothing inside the batch may read another stat's modifier
range. The batch API itself does not, which is why this is safe.

### Inspecting

```csharp
accessor.TryGetModifierCount(handles.attack, out var count);
accessor.TryGetModifiersOfStat(handles.attack, modifiers);   // NativeList<StatModifier>
accessor.TryGetObserversOfStat(handles.attack, observers);   // who depends on this stat
accessor.TryGetAllObservers(owner, observers);               // every reverse edge on this owner
accessor.TryGetStatModifier(modifierHandle, out var modifier);
```

## Events

Two event streams accumulate in `WorldData`:

```csharp
var events = worldData.GetStatChangeEvents(Allocator.Temp);

for (var i = 0; i < events.Length; i++)
{
    var (handle, prev, next) = events[i];
    // ...
}

events.Dispose();

// Clear once per frame, after whatever consumes them has run.
worldData.Clear();
```

| | |
|---|---|
| `StatChangeEvent<TValuePair>` | `{ statHandle, prevValue, newValue }` — for stats with `ProduceChangeEvents` |
| `ModifierTriggerEvent<…>` | `{ handle, modifier }` — pushed when your `ApplyInternal` raises the trigger flag |

There are overloads that fill a `NativeList` instead of allocating a `NativeArray`, plus
`ClearStatChangeEvents()` / `ClearModifierTriggerEvents()` for clearing one stream and
`AddStatChangeEvent` / `AddModifierTriggerEvent` for pushing your own.

**Group by `statHandle` before acting.** One write can produce several events for the same stat — on a
DAG with branches of unequal length, an intermediate value is emitted before the settled one. Driving
"took damage" logic off event *count* will misfire. See
[Core concepts](02-CONCEPTS.md#how-a-write-propagates).

**Nothing clears the lists for you.** Forget `Clear()` and they grow for the lifetime of the
`WorldData`.

## Destroying owners

```csharp
store.DestroyOwner(owner);                                    // fast path
accessor.DestroyOwnerAndUpdateObservers(owner, ref worldData); // also refreshes dependents
```

`DestroyOwner` alone does not touch observers — upstream has the same gap, and it is not a correctness
problem: stats pointing at a dead owner resolve to nothing and are skipped during propagation. It does
mean dependent stats keep their **last computed value** until something recalculates them.
`DestroyOwnerAndUpdateObservers` snapshots the dependents first, destroys, then updates each one.

Which to use: the plain one when the dependents are about to be destroyed too or will be recalculated
this frame anyway; the accessor one when a buff source disappears and other characters must react
immediately.

Either way, write your modifier's `ApplyInternal` so a missing observed stat contributes nothing
rather than failing:

```csharp
case Kind.AddFromStat:
    if (reader.TryGetStatValue(observedStat, out var other))
    {
        stack.add += other.GetCurrentValueOrDefault(new StatVariant(0f));
    }
    break;
```

Destroy is cheap — 0.166 µs per owner in the benchmark — because it clears the buffers and pools the
slot instead of freeing memory. Only `store.Dispose()` frees.

## Working with `StatVariant`

`StatVariant` is the value union. It has arithmetic and comparison operators, math helpers, and a
`ToString()` covering every type, so logging one prints a value rather than a type name.

```csharp
var v = new StatVariant(12.5f);
float f = v.Float;              // reinterpret — the caller is asserting the type
v = v + new StatVariant(2f);
```

Reading the wrong field is a type error the union cannot catch. Under
`APEXION_STATS_RUNTIME_CHECKS`, mismatched operators throw; in release the checks compile away.
Prefer going through the typed `[StatData]` structs and `GetCurrentValueOrDefault` — reach for raw
`StatVariant` only inside modifier code, where the type is already known.

**`StatVariant` cannot be authored in the Inspector** — it has 19 fields at `[FieldOffset(0)]`. Use
`SerializableStatVariant` from the `.Authoring` assembly. See [Tooling ▸ Authoring](07-TOOLING.md#authoring).
