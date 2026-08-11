# ApexionGame.Entities.Stats

A **dependency-graph stat system** for Unity. A stat can be computed from other stats — including
stats owned by a *different* entity — and a write anywhere propagates through the graph on its own.
No ECS required.

*[Tiếng Việt](README.vi.md) · [Guide](Documentation~/guide/README.md) · [Design docs](Documentation~/README.md)*

```csharp
// hero.attack reads aura.attackBonus, which lives on another owner
accessor.TryAddStatModifier(hero.attack, StatModifier.AddFrom(aura.attackBonus), out _, ref worldData);

accessor.TrySetStatBaseValue(aura.attackBonus, new ValuePair(20f), ref worldData);
// hero.attack is already correct. Nobody called a recalculation.
```

That last line is the whole reason this library exists. Take it away and a
`Dictionary<string, float>` does the job.

---

## What it is

This is a port of [`EncosyTower.Entities.Stats`][encosy] — itself a port of
[**Trove Stats**][trove] (MIT, © 2023 Philippe St-Amand) — into a project with **no
`com.unity.entities`**. The algorithms are unchanged. Exactly four ECS storage primitives were
replaced:

| Unity.Entities | This package |
|---|---|
| `Entity` | `StatOwnerHandle` (index + version) |
| `DynamicBuffer<T>` | `StatBuffer<T>` |
| `BufferLookup<T>` | `StatBufferLookup<T>` |
| `ComponentLookup<StatOwner>` | `StatStore<TStat, TStatModifier, TStatObserver>` |

Everything else — observer graph, modifier stacks, cycle rejection, change propagation, deferred
update jobs, the code generators, the authoring-friendly generated API — behaves as upstream does.

**One deliberate divergence:** the propagation visited-set was removed, because upstream's version
leaves permanently wrong values on unequal-depth DAGs. Measurements and reasoning in
[DEC-005](Documentation~/07-DECISIONS.md#dec-005). If you plan to touch the runtime, read that first.

## Feature summary

- **Cross-owner dependencies.** A modifier declares which stats it reads; the runtime maintains the
  reverse edges and recalculates dependents when an input changes.
- **Cycles are rejected at insert time,** not detected at runtime — the graph is always a DAG, so
  propagation cannot hang.
- **Blittable and Burst-friendly.** Native memory throughout, no managed state in the core, no
  allocation on the hot path.
- **Codegen-driven API.** `[StatSystem]` / `[StatCollection]` / `[StatData]` generate strongly typed
  handles, builders, accessors, readers and non-generic `[BurstCompile]` jobs.
- **18 value types** in one 1–16 byte union (`bool` … `float4`, `half`…`half4`, `double`, enums).
- **Save/load support** — blittable owner state plus a handle remap pass for cross-owner links.
- **Editor tooling** — a live store inspector with an observer-graph view, and three Quick Action
  refactorings that write the boilerplate you cannot guess.

## Requirements

| | |
|---|---|
| Unity | **6000.3** or newer (developed on 6000.3.20f1) |
| C# | 10 (`csc.rsp` pins `-langversion:10`) |
| Runtime packages | `com.unity.collections` 2.6+, `com.unity.mathematics` 1.3+, `com.unity.burst` 1.8+ |
| Runtime library | [`com.laicasaane.encosy-tower`][encosy] 0.1.7-preview.3+ — `EncosyTower.Core` only |
| Editor-only | `com.annulusgames.unity-codegen` 1.0.0 — needed **only** to regenerate the value-type tables |
| Not required | `com.unity.entities`, `Unity.Entities.Hybrid`, Latios Framework |

## Install

Copy the following folders into your project's `Assets/`:

| Folder | Required? |
|---|---|
| `ApexionGame.Entities.Stats` | yes — runtime + prebuilt generator DLLs |
| `ApexionGame.Entities.Stats.Authoring` | optional — `ScriptableObject` authoring of stat values |
| `ApexionGame.Entities.Stats.Editor` | optional — *ApexionGame ▸ Stats ▸ Stat Debugger* |
| `ApexionGame.Entities.Stats.Samples` | optional — runnable sample, read this first |
| `ApexionGame.Entities.Stats.Tests` | optional — 95 tests (Editor only) |

Then reference `ApexionGame.Entities.Stats` from your `.asmdef`. The Roslyn generators ship prebuilt
in `SourceGenerators/` with the correct asset labels, so they start running as soon as the reference
exists — nothing to install, no build step.

Full walkthrough: [Getting started](Documentation~/guide/01-GETTING-STARTED.md).

## Three steps

**1 — Declare**

```csharp
using ApexionGame.Entities.Stats;

[StatSystem(StatDataSize.Size8)]
public static partial class RpgStatSystem { }

[StatCollection(typeof(RpgStatSystem), 2100)]
public partial struct RpgStats
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct Attack { }
}
```

`StatDataSize.Size8` is the byte budget for one stat's value. An analyzer errors on any `[StatData]`
that does not fit.

**2 — Build a store and an owner**

```csharp
var store = new StatStore<RpgStatSystem.Stat, RpgStatSystem.StatModifier, RpgStatSystem.StatObserver>(
    initialOwnerCapacity: 256, Allocator.Persistent);

var accessor  = new RpgStatSystem.Accessor(store);
var worldData = new RpgStatSystem.WorldData(64, Allocator.Persistent);

var stats = RpgStats.Builder
    .Build(ref store, out var owner)
    .CreateAllStats(produceChangeEvents: true)
    .ToStats();
```

`stats` is a small struct of `StatIndex` values. Keep it wherever you like — a field on your
character class, a `NativeList`, a dictionary keyed by your own id.

**3 — Read and write**

```csharp
var access = RpgStats.Accessor.Create(owner, stats, accessor, worldData);

access.TrySetStatBaseValue(new RpgStats.Hp(100f), out _)
      .TrySetStatBaseValue(new RpgStats.Attack(10f), out _);

access.TryGetStatData(out RpgStats.Hp hp, out _, out _);
```

Generated accessor methods return the accessor so calls chain; success comes out through an
`out bool`, not the return value.

Dispose in this order: `worldData.Dispose()` then `store.Dispose()`.

## Documentation

| | |
|---|---|
| [**Guide**](Documentation~/guide/README.md) | Install, concepts, codegen, modifiers, persistence, jobs, tooling, API reference, pitfalls |
| [Samples](../ApexionGame.Entities.Stats.Samples/README.md) | Ten runnable cases; a character with gear, buffs and debuffs; and a two-faction RTS match with shared bonus nodes, spawning and death |
| [Design docs](Documentation~/README.md) | Architecture, decision log, every divergence from upstream |
| [Changelog](CHANGELOG.md) | |

New here? [Getting started](Documentation~/guide/01-GETTING-STARTED.md) →
[Core concepts](Documentation~/guide/02-CONCEPTS.md) →
[the sample](../ApexionGame.Entities.Stats.Samples/README.md).

## Three traps worth knowing before you start

**`AddObservedStatsToListInternal` is a contract, not a convenience.** It is where a modifier
declares which stats it reads. Miss one and the dependent stat is simply never recalculated when
that input changes — stale value, no exception, no warning. This is the hardest bug in the system to
find.

**Stat index 0 is *None*.** `StatIndex.IsValid => value > 0`. Only `StatAPI.CreateStatOwnerHandle`
(and the generated `Builder.Build` that calls into it) seeds that stat. Creating an owner with a
bare `store.CreateOwner()` and expecting handles to work will not go well.

**One stat can be recalculated several times in a single propagation, and that is correct.** On a
DAG where a short and a long branch meet, the second pass is the one that repairs the value computed
while the long branch was still stale. If you count `StatChangeEvent`s to drive gameplay logic, group
them by `statHandle` — do not assume one event per stat.

More in [Pitfalls & FAQ](Documentation~/guide/09-PITFALLS.md).

## Performance

10k owners × 8 stats × 4 modifiers, Unity 6000.3.20f1, Editor/Mono, safety checks **on**, Burst
**off** — relative numbers only, a player build is considerably faster:

| | Total | Per operation |
|---|---|---|
| Build the world (10k owners, 80k stats, 320k modifiers) | 291.4 ms | 0.911 µs/modifier |
| `TrySetStatBaseValue` + propagation, once per owner | 28.2 ms | **2.824 µs/owner** |
| `TryUpdateAllStats` per owner | 31.7 ms | 3.170 µs/owner |
| Read all 80k stats | 6.0 ms | 0.075 µs/stat |
| Destroy 10k owners | 1.7 ms | **0.166 µs/owner** |

The number that matters: a write propagating through an 8-deep chain costs 2.8 µs and does **not**
depend on the other 10k owners in the world. Details and caveats in
[Jobs & performance](Documentation~/guide/06-JOBS-AND-PERFORMANCE.md).

## Testing

95 tests in `ApexionGame.Entities.Stats.Tests` (90 functional — including 23 golden tests that pin
the algorithm's invariants — plus 5 benchmarks), and 17 generator/analyzer tests that run outside
Unity. Last full run: **928 pass / 0 fail / 0 skip** across the project, Unity 6000.3.20f1,
2026-08-04.

```powershell
# Unity tests, no Editor window needed
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Tests"

# Generators and analyzers
dotnet test Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Tests
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Issues and pull requests in English or Vietnamese are both
fine.

## License

[MIT](LICENSE.md).

The algorithms are © 2023 Philippe St-Amand ([Trove Stats][trove], MIT), by way of
[EncosyTower][encosy] (MIT, © Laicasaane). Both licenses are reproduced in
[LICENSE.md](LICENSE.md).

[trove]: https://github.com/PhilSA/Trove
[encosy]: https://github.com/laicasaane/EncosyTower
