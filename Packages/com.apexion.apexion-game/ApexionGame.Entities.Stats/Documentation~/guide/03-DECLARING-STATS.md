# Declaring stats

*[Tiếng Việt](vi/03-DECLARING-STATS.md) · [Guide index](README.md)*

Three attributes drive all code generation. Everything you call at runtime comes out of them.

| Attribute | Applied to | Generates |
|---|---|---|
| `[StatSystem]` | a `static partial class` (or `partial struct`) | the storage types, accessors, reader, builder, world data, jobs |
| `[StatCollection]` | a `partial struct` | the typed handle set, builder, accessor and reader for one group of stats |
| `[StatData]` | a `partial struct` nested in a collection | one strongly typed stat |

## `[StatSystem]`

```csharp
[StatSystem(StatDataSize.Size8)]
public static partial class RpgStatSystem { }

// or, with a wider type-id field:
[StatSystem(StatDataSize.Size8, StatUserDataSize.Size2)]
public static partial class RpgStatSystem { }
```

A stat system defines **one store's worth of types**. Every collection pointing at the same
`[StatSystem]` shares one `StatStore`, which is exactly what lets a modifier reach from one collection
into another. Two systems mean two stores and no edges between them.

| Parameter | Default | Meaning |
|---|---|---|
| `maxDataSize` | required | bytes available for one stat's value |
| `maxUserDataSize` | `Size1` | width of `Stat.UserData` — `byte`, `ushort` or `uint` |

`maxUserDataSize` bounds how many distinct stat types the whole system can identify. `Size1` gives 255
type-ids across all collections; if your `[StatCollection]` seeds are spread out (2100, 2200, …) you
want `Size2` or `Size4`.

### Value types and the size budget

`StatVariant` is a 19-member union — 18 concrete value types plus `None` — with explicit layout:

| Bytes | Types |
|---|---|
| 1 | `bool`, `sbyte`, `byte`, enums with a 1-byte underlying type |
| 2 | `short`, `ushort`, `half` |
| 4 | `int`, `uint`, `float`, `half2` |
| 6 | `half3` |
| 8 | `long`, `ulong`, `half4`, `float2`, `double` |
| 12 | `float3` |
| 16 | `float4` |

`StatDataSize` accepts `Size2`, `Size4`, `Size6`, `Size8`, `Size12`, `Size16`.

**A stat stores base *and* current in that budget.** So the rule is:

| Type size vs. `maxDataSize` | Usable? |
|---|---|
| `size <= maxDataSize / 2` | yes, as a value pair — the normal case |
| `maxDataSize / 2 < size <= maxDataSize` | only with `SingleValue = true` |
| `size > maxDataSize` | no |

With `Size8`, `float` and `int` are pairs; `double` and `float2` need `SingleValue = true`; `float4`
needs `Size16` at minimum, or `Size32`-equivalent budget for a pair (not offered — use
`SingleValue`). The analyzer reports `AGS_STAT_DATA_0005` with the exact numbers, so pick a size and
let it correct you.

Bigger is not free: the union's size is per stat, and it is what dominates a store's memory. Size8
covers most gameplay stats.

### What `[StatSystem]` generates

Inside your class:

| Type | Role |
|---|---|
| `Stat : IStat<ValuePair>` | the concrete stat struct — value pair, ranges, user data, flags |
| `Stat<TStatData>` | the same, phantom-typed |
| `ValuePair : IStatValuePair` | `{ data union; StatVariantType type; bool isPair; }` |
| `ValuePair.Composer` | builds a value pair; has a `partial void OnCompose(...)` hook for clamping or rounding |
| `StatModifier` (partial) | **you complete this** — see below |
| `StatModifier.Stack` (partial) | **you complete this** |
| `StatObserver : IStatObserver` | reverse edge record |
| `API` | non-generic facade over `StatAPI` |
| `Reader`, `Accessor`, `Accessor.ReadOnly`, `Builder`, `WorldData` | non-generic facades over the L3 generics |
| `ModifierTriggerEvent`, `StatModifierRecord` | concrete event aliases |
| `DeferredUpdateStat{List,Queue,Stream}Job` | `[BurstCompile]` non-generic job wrappers |
| `IsCompatible(StatVariantType, bool isPair)` | the compile-time type table, queryable at runtime |

Note what is *not* generated: the store. You instantiate
`StatStore<RpgStatSystem.Stat, RpgStatSystem.StatModifier, RpgStatSystem.StatObserver>` yourself.

## `[StatCollection]` and `[StatData]`

```csharp
[StatCollection(typeof(RpgStatSystem), 2100)]
public partial struct RpgStats
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct Attack { }
    [StatData(typeof(DirectionType))] public partial struct Direction { }
    [StatData(StatVariantType.Double, SingleValue = true)] public partial struct Score { }
}
```

`[StatData]` takes either a `StatVariantType` or `typeof(SomeEnum)`. `SingleValue = true` stores only
the current value, halving the space a stat needs.

The second argument to `[StatCollection]` is the **type-id seed**. It offsets this collection's ids
inside `Stat.UserData` so two collections never collide — which is what makes it possible to take a
bare `Stat` and ask which declaration produced it. Give each collection its own number and leave
room: the ids run from the seed upward, one per `[StatData]`.

A nested struct without `[StatData]` produces no stat, and warns (`AGS_STAT_COLLECTION_0005`) rather
than disappearing quietly.

### What a collection generates

| Member | Use |
|---|---|
| `enum Type` | one member per stat, named exactly like your struct |
| `TypeId` | `EncodeToStatUserData` / `DecodeFromStatUserData` / `ValidateStatUserData` |
| `Index`, `Index<TStatData>` | position inside this collection, convertible to `StatIndex` |
| `Indices`, `StatIndices`, `StatHandles` | fixed sets, span-enumerable, with `ToRecords(...)` |
| `<Stat>.Params.Create(...)` | authoring-friendly stat parameters, see below |
| `Options.Data`, `Options.ProduceChangeEvents` | per-stat option bundles for the batch calls |
| `Builder`, `Builder<T>` | owner creation |
| `Accessor`, `Accessor<T>` | read/write bound to one owner |
| `Reader`, `Reader<T>` | read-only, over a single stat buffer |
| `<Collection>Extensions` | `ToComponent<T>`, `TryGetValuePair`, and friends |

`Params.Create` has up to three overloads per stat:

```csharp
RpgStats.Hp.Params.Create(100f);                       // base = current = 100
RpgStats.Hp.Params.Create(100f, 80f);                  // base 100, current 80
RpgStats.Hp.Params.Create(produceChangeEvents: true);  // default value, flags only
```

Each carries its own encoded type-id, so `Builder.CreateStat(...)` needs nothing else.

`StatHandles` is what you keep. It is a small struct of handles with a field per stat, named after
your declarations in camelCase:

```csharp
var stats   = RpgStats.Builder.Build(ref store, out var owner).CreateAllStats().ToStats();
var handles = stats.GetStatHandles(owner);

handles.hp;        // StatHandle
handles.attack;    // StatHandle
```

## The seven hooks

The generator writes `StatModifier`'s interface plumbing and leaves one `partial void ...Internal` per
decision it cannot make for you. There are seven, and both their exact signatures and their
`readonly`-ness matter.

| Hook | On | `readonly` | What you write |
|---|---|---|---|
| `GetIdInternal(ref uint id)` | `StatModifier` | yes | hand back your stored id |
| `SetIdInternal(uint value)` | `StatModifier` | no | store the id the runtime assigns |
| `AddObservedStatsToListInternal(NativeList<StatHandle>)` | `StatModifier` | yes | **declare every stat this modifier reads** |
| `ApplyInternal(Reader, ref Stack, ref bool)` | `StatModifier` | no | contribute to the stack |
| `RemapObservedStatsInternal(in StatOwnerRemap)` | `StatModifier` | no | rewrite stored handles after a load |
| `ResetInternal(in Stat)` | `Stack` | no | set the identity elements |
| `ApplyInternal(in StatVariant, ref StatVariant)` | `Stack` | no | combine base value and stack into the current value |

> **Do not type these from memory.** Put the cursor on your `partial class`, press **Ctrl+.** and
> choose *Generate stat modifier skeleton*. A `readonly` in the wrong place makes the compiler decline
> to match the partial method — silently, with no diagnostic, and the hook simply never runs.

### `AddObservedStatsToListInternal` is the contract

This is the single most important method in your codebase's use of this library. It is how the
runtime learns which edges to build.

```csharp
readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observedStatHandles)
{
    if (kind == Kind.AddFromStat)
    {
        observedStatHandles.Add(observedStat);
    }
}
```

Omit a handle your modifier actually reads and the dependent stat is **never recalculated** when that
input changes. The old value just sits there. No exception, no warning, no log line. It is the hardest
class of bug this system produces, and the only defence is to add to this method every time you add a
`StatHandle` field.

### `RemapObservedStatsInternal` and save/load

`StatOwnerHandle` is a slot index, so handles stored inside a modifier stop being meaningful after a
save/load round trip. This hook is where you rewrite them:

```csharp
partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
{
    if (kind == Kind.AddFromStat)
    {
        observedStat = remap.RemapOrNull(observedStat);
    }
}
```

An analyzer warns (`AGS_STAT_DATA_0006`) if your modifier has a `StatHandle` field and no
implementation here. This method is **new in this port** — modifiers written against upstream will not
compile until you add it. Full flow in [Persistence](05-PERSISTENCE.md).

### `ResetInternal` and identity elements

```csharp
partial void ResetInternal(in Stat stat)
{
    var type = stat.ValuePair.Type;
    add = type.ZeroVariant();
    multiply = type.OneVariant();
}
```

Read the identity from the stat's type rather than writing `new StatVariant(0f)` / `(1f)`. A stack
hard-coded to `float` produces silent type mismatches the day someone declares an `int` stat.

### Stack order is your design decision

```csharp
partial void ApplyInternal(in StatVariant baseValue, ref StatVariant currentValue)
    => currentValue = (baseValue + add) * multiply;
```

`(base + add) * multiply` means flat bonuses land before percentages — the usual RPG convention, and
what the sample uses. Nothing in the runtime imposes it; a different order is one line away, and the
individual modifiers do not need to know.

## Diagnostics

Sixteen diagnostic ids ship with the generators: thirteen for declaration mistakes, three that report
an internal generator failure instead of emitting broken code. The full table is in the
[API reference](08-API-REFERENCE.md#diagnostics).

The two you will meet most:

| Id | Meaning |
|---|---|
| `AGS_STAT_DATA_0005` | value type does not fit — raise `maxDataSize` or set `SingleValue = true` |
| `AGS_STAT_SYSTEM_0002` | this assembly uses `[StatSystem]` but does not reference `EncosyTower.Core` |

## Quick Actions

Three refactorings, on the cursor in a `partial class` or `partial struct` (**Ctrl+.**):

| Action | Writes |
|---|---|
| *Make this a stat system* | the `[StatSystem]` attribute |
| *Generate stat modifier skeleton* | `StatModifier` + `Stack` with all seven hooks, correct signatures and `readonly` |
| *Generate stat collection skeleton* | `[StatCollection]` with a type-id seed that does not collide with the ones already in the project |

The middle one earns its keep. The other two save typing.
