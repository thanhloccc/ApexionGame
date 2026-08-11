# Changelog

*[Tiếng Việt](CHANGELOG.vi.md)*

Follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [SemVer](https://semver.org/).

## [0.1.0] — unreleased

First port of `EncosyTower.Entities.Stats` into an environment without `com.unity.entities`.

### Added

- **Runtime** — `StatOwnerHandle`, `StatBuffer<T>`, `StatBufferLookup<T>` and
  `StatStore<TStat, TStatModifier, TStatObserver>` replace the four ECS storage primitives.
- **Runtime** — `StatAccessor<6>`, `StatReader<2>`, `StatWorldData<5>`, `StatBuilder<6>`, `StatAPI`,
  and three deferred update jobs (`List` / `Queue` / `Stream`).
- **Runtime** — `StatVariant`: a union of the gameplay profile's 18 value types, with generated
  operators and math helpers.
- **Persistence** — `StatStore.TryCopyOwnerTo` / `TryRestoreOwner`, plus
  `StatAccessor.TryRemapOwner` / `TryRemapOwners` to repair cross-owner handles after a load.
- **Codegen** — three generators (`[StatSystem]`, `[StatCollection]`, `[StatData]`) and three
  analyzers, 16 diagnostic ids in total. Generated jobs are wrapped in non-generic
  `[BurstCompile]` structs.
- **Authoring** — the `ApexionGame.Entities.Stats.Authoring` assembly, with
  `SerializableStatVariant` and `StatDefinitionAsset`.
- **Sample** — the `ApexionGame.Entities.Stats.Samples` assembly: two collections on one system, a
  cross-owner modifier, and a `MonoBehaviour` that runs for real. Plus `StatsPlayground` and the
  `stats-playground.unity` scene: ten Inspector buttons, one behaviour each, runnable outside play
  mode. And a `Samples/Samples.RpgStats/` project that compiles those same files through the real
  generators outside Unity, as a regression gate for the usage shown in the docs.
- **Sample** — a second sample under `Samples/Battle/`: `BattleStatSystem` with five modifier kinds
  (`Add`, `Multiply`, `AddFromStat`, `AddFractionOfStat`, `ClampMaxFromStat`) and a stack that caps as
  well as scales, plus `BattleSimulation` — a cumulative fight where each worn item is an owner of its
  own, buffs and debuffs are timed modifier sets, damage over time ticks, and modifiers left dangling
  by broken gear are found through the `ModifierTriggerEvent` stream. Reachable from the
  `battle-sample` GameObject in the scene and from *ApexionGame ▸ Stats ▸ Battle Sample*.
- **Sample** — a third sample under `Samples/Rts/`: a two-faction match where every unit reads one
  shared `TeamStats` node — research writes its base value, commander auras are modifiers on it — so an
  army-wide change is a single write and a unit costs a constant five modifiers however many sources
  exist. Covers what the other samples do not: spawn and death at scale, the observer entries a
  `DestroyOwner` without prior modifier removal leaks on a shared node (with the count on screen), the
  cycle a naive commander aura closes and how to design around it, and the deferred
  `DeferredUpdateStatListJob`. Reachable from the `rts-sample` GameObject and from
  *ApexionGame ▸ Stats ▸ RTS Sample*.

### Fixed

- **Docs** — `Guide ▸ Using stats` showed `Options.Data` being constructed from `Params.Create(...)`.
  Its parameters are `Option<TStatData>`; `Params.Create` belongs to `Builder.CreateStat` / `SetStat`.
- **Tests** — 90 runtime tests (including 23 golden tests pinning the algorithm's invariants) and 17
  generator/analyzer tests. Plus 5 benchmarks at 10k owners × 8 stats × 4 modifiers — deliberately
  **not** marked `[Explicit]`, because Unity's test runner drops `[Explicit]` tests even when named
  directly in `--filter`.

### Changed, relative to upstream

The full list of 23 divergences is in
[`Documentation~/07-DECISIONS.md`](Documentation~/07-DECISIONS.md). The notable ones:

- **`StatStore` uses parallel arrays** (I-12) rather than one slot holding three type parameters.
  That is what makes a one-type-parameter `StatBufferLookup<T>` possible — matching ECS's
  `BufferLookup<T>` — and keeps `IStatModifier.Apply` at two type parameters.
- **`StatBuilder` replaces `StatBaker`** (I-20). No baking pipeline means no baker.
- **`ComponentLookup<StatOwner>` dropped from `Accessor.ReadOnly`** (D-101). Upstream only had it to
  register a read dependency with the ECS scheduler.
- **Batch APIs take an `Allocator`** (D-02), still defaulting to `Allocator.Temp`.
- **`IStatModifier` gained `RemapObservedStats`** (DEC-004). Modifiers written for upstream will not
  compile until the hook is added.

### Fixed

- **`StatVariant.ToString()` printed a type name instead of a value.** With no override it fell back
  to `object.ToString()`, so every log line and every inspector field showed
  `ApexionGame.Entities.Stats.StatVariant`. Added an override covering all 18 profile types.
- **The generated `StatSystem.Accessor` was missing `TryRemapOwner` / `TryRemapOwners`** (I-24),
  which put all of DEC-004 out of reach from the generated API. Surfaced while writing the sample; no
  test caught it, because the generator tests check that generated code *compiles cleanly*, not that
  it contains everything you need.

### Fixed — bugs present upstream

- **Propagation left wrong values on unequal-depth DAGs** (DEC-005 / D-01). Upstream's
  `TryUpdateStat` had a visited-set that suppressed exactly the recalculation whose job is to
  **repair** a value computed while the longer branch was still stale. The diamond `A→B→D` plus
  `A→C→E→D` with `A = 5` yields 6 instead of 10, and never corrects itself. This port removed the
  visited-set from both propagation paths. Trade-off: worst case O(k²) instead of O(k) on
  diamond-dense graphs. Measurements and full reasoning in
  [DEC-005](Documentation~/07-DECISIONS.md#dec-005).
- **14 generator integration tests had never run.** `UnityDllPaths.g.cs` was generated in
  `BeforeBuild`, outside the already-evaluated `Compile` glob, and deleted after each build. Every
  integration test reported `Inconclusive` while `dotnet test` still printed `Passed!`. Now 17 pass,
  0 skip. Details in
  [06-ROADMAP](Documentation~/06-ROADMAP.md#hai-lỗi-làm-14-test-tích-hợp-chưa-từng-chạy).
- **`Allocator.Temp` containers were not disposed** in the three batch methods. `Temp` is reclaimed
  per frame so nothing actually leaked, but leak detection inside a job would warn.

### Tooling

- **`StatDebugRegistry` + `StatStoreDebug<4>`** (runtime) — a store registers itself to become
  visible to tooling. Not registering costs nothing; the runtime never reads the registry.
- **`StatStore.TryGetOwnerAt`** — the only runtime API added for tooling: rebuild a handle from a
  slot index. Gameplay should not use it, because a slot index is not an identity.
- **`ApexionGame.Entities.Stats.Editor`** — the *ApexionGame ▸ Stats ▸ Stat Debugger* window: pick a
  store, pick an owner, read `base → current` plus modifier and observer counts, alongside an
  **observer graph** drawn with `Painter2D`. Nodes are laid out by longest-path depth so the
  unequal-depth shape from DEC-005 is visible; cross-owner endpoints are dimmed rather than dropped;
  a cycle turns red with a warning banner — cycles should be impossible, so that is a **live
  assertion**, not a feature.
- **`ApexionGame.SourceGen.CodeRefactors`** — three refactorings that write the `[StatSystem]` /
  `[StatCollection]` boilerplate, including the seven `partial void ...Internal` hooks that cannot be
  guessed without reading the generated code. 9 tests.

### Verified on a machine

- **Burst**: the three generated jobs (`DeferredUpdateStatListJob` / `QueueJob` / `StreamJob`)
  compile to machine code in *Burst Inspector*. The three open generic jobs in `StatJobs.cs` are
  skipped by Burst — as designed, and the reason the generator emits non-generic wrappers.
- **Tests**: 928/928 EditMode across the project; 95/95 in `ApexionGame.Entities.Stats.Tests`;
  26/26 in `ApexionGame.SourceGen.Tests` (17 generator/analyzer + 9 refactoring).
- **Benchmarks** at 10k owners × 8 stats × 4 modifiers: one stat write 2.824 µs/owner, owner destroy
  0.166 µs — exactly what D-100's free list promised.

### Not done

- Performance comparison against `DynamicBuffer`: the project has no `com.unity.entities` installed.
- Topological-order propagation — the only way to be both correct and linear.
