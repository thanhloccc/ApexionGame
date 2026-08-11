# API reference

*[Tiếng Việt](vi/08-API-REFERENCE.md) · [Guide index](README.md)*

A map of the surface, not a signature dump. Every type carries XML docs; this page tells you which
type to open.

| Namespace | |
|---|---|
| `ApexionGame.Entities.Stats` | everything runtime |
| `ApexionGame.Entities.Stats.Debugging` | `StatDebugRegistry`, `StatStoreDebug<4>`, `ValidationDefines` |
| `ApexionGame.Entities.Stats.Authoring` | `SerializableStatVariant`, `StatDefinitionAsset` |

## Identity

| Type | Shape | Notes |
|---|---|---|
| `StatOwnerHandle` | `readonly struct { int Index; int Version; }` | `Null`, `IsValid`, `==`, `Deconstruct`, `ToFixedString()`. Version invalidates recycled slots. Not stable across a save. |
| `StatIndex` | `struct { int value; }` | `IsValid => value > 0` — index 0 is the None stat. `IsValidInRange(length)`, `ToStatHandle(owner)`. |
| `StatIndex<TStatData>` | typed variant | implicit → `StatIndex` |
| `StatHandle` | `struct { StatOwnerHandle owner; StatIndex index; }` | `Null`, `IsValid` (follows `owner.IsValid`), `ToFixedString()` |
| `StatHandle<TStatData>` | typed variant | implicit → `StatHandle`, explicit ← `StatHandle` |
| `StatModifierHandle` | `struct { StatHandle affectedStatHandle; uint modifierId; }` | the id is local to the owner |
| `StatOwnerRemap` | wraps `NativeHashMap<StatOwnerHandle, StatOwnerHandle>` | `TryRemap`, `RemapOrNull` — see [Persistence](05-PERSISTENCE.md) |

## Values

| Type | |
|---|---|
| `StatVariant` | the value union — 18 concrete types plus `None`, 1–16 bytes, arithmetic and comparison operators, math helpers, a real `ToString()` |
| `StatVariantType` | enum of the 18 types + `None`; extension methods including `ZeroVariant()` / `OneVariant()` |
| `StatDataSize` | `Size2, Size4, Size6, Size8, Size12, Size16` — the `[StatSystem]` budget |
| `StatUserDataSize` | `Size1` (byte), `Size2` (ushort), `Size4` (uint) — width of `Stat.UserData` |
| `StatData<TStatData>` | `{ Data, UserData, ProduceChangeEvents }` — a stat plus its flags |
| `StatDataParams<TStatData>` | four `Option<T>`s: stat data, handle, produce-change-events, user data. What `Params.Create` returns. |
| `StatValueParams<TValuePair>` | same shape, untyped value — what the batch setters consume |
| `StatSingle<T>` | single-value marker, implicit from `T` |
| `None` | empty value type |
| `ModifierRange`, `ObserverRange` | `(startIndex, count)` blocks inside the shared buffers |
| `StatChangeEvent<TValuePair>` | `{ statHandle, prevValue, newValue }`, deconstructible |
| `ModifierTriggerEvent<4>` | `{ handle, modifier }`, deconstructible |
| `StatModifierRecord<4>` | modifier + its handle |

## Contracts

Implemented by generated code, except where noted.

| Interface | |
|---|---|
| `IStatValuePair` | `Type`, `IsPair`, get/set base and current |
| `IStatValuePairComposer<TValuePair>` | `Compose(isPair, base, current)` — the generated `ValuePair.Composer` has an `OnCompose` hook for clamping or rounding |
| `IStatData` | `IsValuePair`, `ValueType`, `BaseValue`, `CurrentValue` — generated from `[StatData]` |
| `IStat<TValuePair>` | ranges, `ProduceChangeEvents`, `ValuePair`, `UserData`, value accessors |
| `IStatModifierStack<TValuePair, TStat>` | `Reset(in TStat)`, `Apply(in StatVariant, ref StatVariant)` — **you write the hooks** |
| `IStatModifier<TValuePair, TStat, TStack>` | `Id`, `AddObservedStatsToList`, `Apply`, `RemapObservedStats` — **you write the hooks** |
| `IStatObserver` | `ObserverHandle` |

`RemapObservedStats` is new in this port. Modifiers written for upstream will not compile until the
hook is added.

## Storage

### `StatStore<TStat, TStatModifier, TStatObserver>`

Owns everything. Native memory, `IDisposable` + `INativeDisposable`.

```csharp
new StatStore<Stat, StatModifier, StatObserver>(initialOwnerCapacity, allocator)
```

| Member | |
|---|---|
| `IsCreated`, `Capacity`, `OwnerCount` | |
| `CreateOwner()` / `CreateOwner(statCapacity, modifierCapacity, observerCapacity)` | defaults are 4/4/4. Does **not** seed the None stat — go through the generated builder. |
| `DestroyOwner(owner)` | clears the buffers and pools the slot; does not free memory |
| `Exists(owner)`, `TryGetOwnerAt(slotIndex, out owner)` | |
| `TryGetStats` / `TryGetModifiers` / `TryGetObservers` / `TryGetBuffers` | `false` when the owner is dead or the version differs |
| `AsStatLookup()` / `AsModifierLookup()` / `AsObserverLookup()` | one-type-parameter views |
| `TryGetModifierIdCounter`, `TryIncrementModifierId` | |
| `TryCopyOwnerTo(...)`, `TryRestoreOwner(...)` | [Persistence](05-PERSISTENCE.md) |
| `Dispose()`, `Dispose(JobHandle)` | |

### `StatBuffer<T>` and `StatBufferLookup<T>`

`StatBuffer<T>` is the `DynamicBuffer<T>` replacement: `Length`, `Capacity`, `this[i]`,
`ElementAt(i) → ref T`, `Add`, `Insert`, `RemoveAt`, `Clear`, `SetCapacity`, `IncreaseCapacityTo`,
`AsArray`, `AsSpan`, `AsReadOnlySpan`, `IsCreated`. Passed **by value** — it wraps a stable pointer, so
copies stay valid for the owner's lifetime.

`StatBufferLookup<T>` is the `BufferLookup<T>` replacement: `TryGetBuffer(owner, out buffer)`,
`Exists(owner)`. No `Update(ref SystemState)` — there is no system scheduling to refresh against.

## Algorithm layer

You normally reach this through the generated non-generic wrappers. The generic types are what those
wrappers wrap.

### `StatAccessor<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>`

Generated as `RpgStatSystem.Accessor`. Constructed from a store, optionally with a composer. Split
across four files:

| File | Members |
|---|---|
| ``StatAccessor`6.cs`` | `TryCreateStatHandle` ×4, `TryGetStatData`, `TryGetStat`, `GetStats`, `TryGetStatValue`, `TrySetStatBaseValue` ×2, `TrySetStatCurrentValue` ×2, `TryUpdateStat`, `TryUpdateAllStats`, `TryGetModifiersOfStat`, `TryGetObserversOfStat`, `TryGetModifierCount`, `TryGetObserverCount`, `TryGetAllObservers`, `TryAddStatModifier`, `TryAddStatModifiersBatch`, `TryGetStatModifier`, `TryRemoveStatModifier`, `TryRemoveModifiersOfStat`, `DestroyOwnerAndUpdateObservers` |
| `+Batch.cs` | `TrySetStatData` ×4, `TrySetStatValues` ×4, `TrySetStatProduceChangeEvents` ×2, `TrySetStatUserData` ×2, `TrySetBaseValueToStats`, `TrySetCurrentValueToStats`, `TrySetDataToStats` |
| `+ReadOnly.cs` | `AsReadOnly()` and the `ReadOnly` view — getters only, parallel-safe |
| `+Persistence.cs` | `TryRemapOwner`, `TryRemapOwners` |

Mutating methods take `ref StatWorldData<…>`. `TryUpdateStat` and `TryUpdateAllStats` return `void` —
a missing stat is a no-op, not an error.

### `StatReader<TValuePair, TStat>`

Generated as `RpgStatSystem.Reader`, and what your modifier's `ApplyInternal` receives. Two modes: over
a `StatBufferLookup<TStat>` (any owner) or over a single `StatBuffer<TStat>` (one owner) — `UseLookup`
tells you which. Members: `Contains` ×2, `TryGetStatData`, `TryGetStat`, `TryGetStatValue`.

### `StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver>`

Generated as `RpgStatSystem.WorldData`. Two event lists, five scratch lists, one modifier-stack
reference.

```csharp
new RpgStatSystem.WorldData(initialCapacity, allocator)
```

| Member | |
|---|---|
| `GetStatChangeEvents(allocator)` / `(NativeList)` | |
| `GetModifierTriggerEvents(allocator)` / `(NativeList)` | |
| `AddStatChangeEvent`, `AddModifierTriggerEvent` | push your own |
| `ClearStatChangeEvents`, `ClearModifierTriggerEvents`, `Clear` | `Clear` empties both |
| `SetStatModifierStack(in TStack)` | seed the shared stack |
| `IsCreated`, `Dispose()`, `Dispose(JobHandle)` | |

**Not thread-safe, not reentrant.** One per update thread.

### `StatBuilder<6>`

Generated as `RpgStatSystem.Builder`, and wrapped again per collection as `RpgStats.Builder`. Replaces
upstream's `StatBaker` — no `IBaker`, no baking pipeline. `Owner`, `Clear(composer)`,
`CreateStatHandle` ×3, `Contains` ×2, `SetStatOrCreateHandle`, `SetStat` ×2, `TryAddStatModifier`.

### `StatAPI`

Static, generic, and the layer everything else calls. Generated as `RpgStatSystem.API` (non-generic).
Notable members:

| | |
|---|---|
| `CreateStatOwnerHandle<6>(ref store, out owner, out builder, capacities…, composer)` | **the only thing that seeds the None stat at index 0** |
| `CreateStatHandle` ×3, `GetStatData` / `TryGetStatData`, `GetStatValue` / `TryGetStatValue`, `Contains` ×4, `GetStat` / `TryGetStat` / `GetStats` | reads |
| `TryGetModifierCount`, `TryGetModifiersOfStat`, `TryGetObserverCount`, `TryGetObserversOfStat`, `TryGetAllObservers` | graph inspection |
| `OwnerHasAnyOtherDependantStats`, `GetOtherDependantStatsOfOwner`, `GetOtherDependantOwnersOfOwner`, `GetOwnersThatOwnerDependsOn` | cross-owner queries, ×2 overloads each |
| `MakeStatData<TValuePair, TStatData>` | |

Upstream's `AddStatComponents` (EntityManager / ECB / ParallelWriter overloads),
`BakeStatComponents(IBaker, …)` and `GetStatComponentTypeSet<>()` have no equivalent here.

### Jobs

`DeferredUpdateStatListJob<6>`, `DeferredUpdateStatQueueJob<6>`, `DeferredUpdateStatStreamJob<6>` in
`StatJobs.cs` — open generics, consuming `NativeList<StatHandle>`, `NativeQueue<StatHandle>` and
`NativeStream.Reader` respectively. Each has three public fields: `statAccessor`, `statWorldData`,
`statsToUpdate`.

**Use the generated versions** (`RpgStatSystem.DeferredUpdateStat*Job`). They are non-generic
`[BurstCompile]` wrappers around the closed generic. The open generics carry no `[BurstCompile]` on
purpose — the attribute has no effect there.

## Generated surface

### From `[StatSystem]`

| Type | |
|---|---|
| `Stat : IStat<ValuePair>`, `Stat<TStatData>` | the concrete stat struct |
| `ValuePair : IStatValuePair`, `ValuePair.Composer` | value pair + composer with an `OnCompose` hook |
| `StatDataStore` (internal) | the explicit-layout union, exactly `maxDataSize` bytes |
| `StatModifier` (partial), `StatModifier.Stack` (partial) | **you complete these** — [seven hooks](03-DECLARING-STATS.md#the-seven-hooks) |
| `StatObserver : IStatObserver` | |
| `API`, `Reader`, `Accessor`, `Accessor.ReadOnly`, `Builder`, `WorldData` | non-generic facades |
| `ModifierTriggerEvent`, `StatModifierRecord` | concrete aliases |
| `DeferredUpdateStat{List,Queue,Stream}Job` | `[BurstCompile]` |
| `IsCompatible(StatVariantType, bool isPair)` | the type table, at runtime |

### From `[StatCollection]` + `[StatData]`

| Member | |
|---|---|
| `enum Type` | one member per stat, named after your struct |
| `TypeId` | `EncodeToStatUserData`, `DecodeFromStatUserData` ×2, `ValidateStatUserData` ×2, `ValidateType`, `ValidateStat<T>`, `Types`, `OFFSET`, `LENGTH` |
| `Index`, `Index<TStatData>` | position in the collection; implicit conversions to `StatIndex` |
| `IndexRecord`, `StatIndexRecord`, `StatHandleRecord` | `(value, type, isValid)` triples, with `ToRecords` / `ToValidRecords` |
| `Indices`, `StatIndices`, `StatHandles` | fixed sets — span-enumerable, `GetIndexFor<T>()`, `GetStatHandleFor<T>()` |
| `Options.Data`, `Options.ProduceChangeEvents` | per-stat bundles for batch calls, all parameters optional |
| `<Stat>.Params.Create(...)` | up to 3 overloads: flags only, single value, base + current |
| `Builder` / `Builder<T>` | `Build(ref store, out owner, composer)`, `CreateAllStats`, `CreateStat`, `CreateStats`, `SetStat`, `SetStats`, `SetOrCreateStat`, `SetOrCreateStats`, `Reset`, `ToStats()`, `As<T>()` |
| `Accessor` / `Accessor<T>` | `Create(owner, statCollection, accessor, worldData)`, then chainable `TrySetStatBaseValue`, `TrySetStatCurrentValue`, `TrySetStatValues`, `TryCreateStat`, `TryCreateAllStats`, `TryCreateOrSetStat`, `TrySet*ToStats`, `TrySetProduceChangeEventsFor*`, `TryGetStat`, `TryGetStatData`, `FindValidStats` |
| `Reader` / `Reader<T>` | `Create(owner, statCollection, statBuffer)`, then `Contains`, `TryGetStat`, `TryGetStatData`, `GetStatDataOptions`, `GetProduceChangeEventsOptions`, `FindValidStats` |
| `<Collection>Extensions` | `GetStatHandlesFrom`, `GetStatIndicesFrom`, `GetIndicesFrom`, `ToComponent<T>`, `TryGetValuePair` |
| on the collection struct | `GetStatHandles(owner)`, `GetStatIndices()`, `ToStatHandles(owner)`, `ToStatIndices()`, `Indices` |

Collection accessor methods return the accessor so calls chain; success comes out through `out bool`.

## Debugging

| Type | |
|---|---|
| `IStatStoreDebug` | `Name`, `IsCreated`, `OwnerCount`, `GetOwners`, `GetStats`, `GetObserverEdges` |
| `StatStoreDebug<TValuePair, TStat, TStatModifier, TStatObserver>` | `(name, store, nameLookup = null)` — the concrete implementation |
| `StatDebugRegistry` | `Register`, `Unregister`, `Clear`, `Stores`, `Changed` |
| `StatDebugInfo`, `StatObserverEdge` | what the window renders |
| `ValidationDefines` | the `[Conditional]` symbol names |

Unregister before disposing the store. See [Tooling](07-TOOLING.md#stat-debugger).

## Authoring

| Type | |
|---|---|
| `SerializableStatVariant` | `Type`, `ToStatVariant()`, `From(in StatVariant)`, `Of(float/int/bool/float4)` |
| `StatDefinitionEntry` | `Id`, `BaseValue`, `ProduceChangeEvents`, `UserData` |
| `StatDefinitionAsset` | `Entries`, `TryGetEntry(id, out entry)`, `TryGetBaseValue(id, out variant)` |

## Diagnostics

Sixteen ids. Thirteen catch declaration mistakes; three report that a generator failed internally
rather than emitting broken code.

### `[StatSystem]`

| Id | Severity | |
|---|---|---|
| `AGS_STAT_SYSTEM_0001` | error | `[StatSystem]` cannot be applied to a generic type |
| `AGS_STAT_SYSTEM_0002` | error | assembly uses `[StatSystem]` but does not reference `EncosyTower.Core` |
| `AGS_STAT_SYSTEM_UNKNOWN_0001` | error | generator failure |

### `[StatCollection]`

| Id | Severity | |
|---|---|---|
| `AGS_STAT_COLLECTION_0001` | error | the `typeof` argument must resolve to a type with `[StatSystem]` |
| `AGS_STAT_COLLECTION_0002` | error | `typeIdOffset` + number of `[StatData]` members exceeds `uint.MaxValue` |
| `AGS_STAT_COLLECTION_0003` | error | `[StatCollection]` can only be applied to a struct |
| `AGS_STAT_COLLECTION_0004` | error | `[StatCollection]` cannot be applied to a generic struct |
| `AGS_STAT_COLLECTION_0005` | warning | a nested struct has no `[StatData]`, so no stat is generated for it |
| `AGS_STAT_COLLECTION_UNKNOWN_0001` | error | generator failure |

### `[StatData]`

| Id | Severity | |
|---|---|---|
| `AGS_STAT_DATA_0001` | error | `[StatData]` can only be applied to a struct |
| `AGS_STAT_DATA_0002` | error | `[StatData]` cannot be applied to a generic struct |
| `AGS_STAT_DATA_0003` | error | `StatVariantType.None` is not a valid argument |
| `AGS_STAT_DATA_0004` | error | the `typeof` argument must be an enum type |
| `AGS_STAT_DATA_0005` | error | value type does not fit `maxDataSize` — a pair needs twice the size; set `SingleValue = true` or raise the budget |
| `AGS_STAT_DATA_0006` | warning | the modifier stores a `StatHandle` but has no `RemapObservedStatsInternal`, so its handles will not survive save/load |
| `AGS_STAT_DATA_UNKNOWN_0001` | error | generator failure |

`AGS_STAT_COLLECTION_0005`, `AGS_STAT_DATA_0005` and `AGS_STAT_DATA_0006` are new in this port. Each
covers a case where upstream's generator silently dropped something.

## Preprocessor symbols

| Symbol | |
|---|---|
| `APEXION_RUNTIME_CHECKS` | enable runtime guards in a build |
| `APEXION_STATS_RUNTIME_CHECKS` | stats-specific guards |
| `DISABLE_APEXION_CHECKS` | disable all of the above |
| `APEXION_STAT_VALUE_TYPES_GENERATOR` | **temporary** — compiles the editor type-table generators; remove after regenerating |
| `ENABLE_UNITY_COLLECTIONS_CHECKS` | Unity's; drives the `AtomicSafetyHandle` guards on the store and buffers |
