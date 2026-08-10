# Getting started

*[Tiếng Việt](vi/01-GETTING-STARTED.md) · [Guide index](README.md)*

## Requirements

| | |
|---|---|
| Unity | **6000.3** or newer (developed on 6000.3.20f1) |
| C# | 10 — `csc.rsp` next to the runtime asmdef pins `-langversion:10` |
| Runtime packages | `com.unity.collections` 2.6+, `com.unity.mathematics` 1.3+, `com.unity.burst` 1.8+ |
| Runtime library | [`com.laicasaane.encosy-tower`](https://github.com/laicasaane/EncosyTower) 0.1.7-preview.3+ |
| Editor-only | `com.annulusgames.unity-codegen` 1.0.0 |
| Not required | `com.unity.entities`, `Unity.Entities.Hybrid`, Latios Framework |

**About the two non-Unity dependencies.**

`EncosyTower.Core` is a hard runtime reference. It supplies `ByteBool`, `Option<T>`, `HashValue`,
`IIsValid`, the collection extensions, and the `[WrapType]` / `[EnumExtensions]` generators — roughly
450 lines that were not worth rewriting. An analyzer (`AGS_STAT_SYSTEM_0002`) errors if an assembly
uses `[StatSystem]` without referencing it, so you will find out immediately rather than through a
wall of missing-type errors.

`AnnulusGames.UnityCodeGen.Editor` is only needed to **regenerate** the value-type tables
(`Common/*.gen.cs`). Those files are committed, so a normal consumer never runs that codegen. See
[Tooling ▸ Regenerating the type tables](07-TOOLING.md#regenerating-the-value-type-tables).

## Install

Copy the folders you need into your project's `Assets/`:

| Folder | Required? | Contains |
|---|---|---|
| `ApexionGame.Entities.Stats` | **yes** | runtime + prebuilt Roslyn generators |
| `ApexionGame.Entities.Stats.Authoring` | optional | `SerializableStatVariant`, `StatDefinitionAsset` |
| `ApexionGame.Entities.Stats.Editor` | optional | *ApexionGame ▸ Stats ▸ Stat Debugger* |
| `ApexionGame.Entities.Stats.Samples` | optional | runnable sample + playground window |
| `ApexionGame.Entities.Stats.Tests` | optional | 95 tests, Editor only |

Then add `ApexionGame.Entities.Stats` to your assembly definition's references. That is the whole
setup: the generators ship prebuilt in `SourceGenerators/` with the asset labels Unity needs, so they
start running on the next compile. There is no build step and no menu item to click.

**Check that codegen is alive** before writing anything real. Declare a `[StatSystem]`, save, and see
whether `RpgStatSystem.Accessor` resolves in your IDE. If it does not:

- Is `ApexionGame.Entities.Stats` in your asmdef's `references`?
- Is `EncosyTower.Core` there too? (Look for `AGS_STAT_SYSTEM_0002` in the console.)
- Do the DLLs in `SourceGenerators/` still carry the `RoslynAnalyzer` label? Re-importing them by
  hand can drop it.

## The smallest thing that works

Four declarations, one `MonoBehaviour`. This is a trimmed version of
[`RpgStatsSample.cs`](../../../ApexionGame.Entities.Stats.Samples/RpgStatsSample.cs).

### 1. A stat system

```csharp
using ApexionGame.Entities.Stats;

[StatSystem(StatDataSize.Size8)]
public static partial class RpgStatSystem { }
```

`StatDataSize.Size8` is the byte budget for one stat's value. A stat normally stores a **pair** —
base value and current value side by side — so eight bytes covers any type up to four bytes: `float`,
`int`, `half2`, every enum. Larger types need a larger budget or `SingleValue = true`. An analyzer
rejects a `[StatData]` that does not fit, so you cannot get this wrong quietly; the rules are in
[Declaring stats](03-DECLARING-STATS.md#value-types-and-the-size-budget).

Every generated type hangs off this declaration: `Stat`, `ValuePair`, `StatModifier`, `Stack`,
`StatObserver`, `Accessor`, `Reader`, `Builder`, `WorldData`, and three `[BurstCompile]` jobs.

### 2. A collection of stats

```csharp
[StatCollection(typeof(RpgStatSystem), 2100)]
public partial struct RpgStats
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct Attack { }
}
```

`2100` is this collection's type-id seed. Give each collection its own number so their `UserData`
values never overlap — that is what lets a bare `Stat` be traced back to the declaration it came
from.

### 3. What the modifier does

The generator wires up the interfaces but cannot know your game's math. You fill in `partial void`
hooks:

```csharp
public static partial class RpgStatSystem
{
    public partial struct StatModifier
    {
        public enum Kind : byte { Add, Multiply, AddFromStat }

        public Kind kind;
        public StatVariant value;
        public StatHandle observedStat;

        private uint _id;

        public static StatModifier Add(float amount)
            => new() { kind = Kind.Add, value = new StatVariant(amount) };

        public static StatModifier AddFrom(StatHandle observed)
            => new() { kind = Kind.AddFromStat, observedStat = observed };

        readonly partial void GetIdInternal(ref uint id) => id = _id;

        partial void SetIdInternal(uint value) => _id = value;

        // Declare every stat this modifier reads. Miss one and the dependent stat
        // silently goes stale when that input changes.
        readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observed)
        {
            if (kind == Kind.AddFromStat)
            {
                observed.Add(observedStat);
            }
        }

        partial void ApplyInternal(Reader reader, ref Stack stack, ref bool shouldProduceTriggerEvent)
        {
            switch (kind)
            {
                case Kind.Add:
                    stack.add += value;
                    break;

                case Kind.Multiply:
                    stack.multiply *= value;
                    break;

                case Kind.AddFromStat:
                    // The observed owner may be gone. Contribute nothing rather than fail.
                    if (reader.TryGetStatValue(observedStat, out var other))
                    {
                        stack.add += other.GetCurrentValueOrDefault(new StatVariant(0f));
                    }
                    break;
            }
        }

        partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
        {
            if (kind == Kind.AddFromStat)
            {
                observedStat = remap.RemapOrNull(observedStat);
            }
        }

        public partial struct Stack
        {
            public StatVariant add;
            public StatVariant multiply;

            partial void ResetInternal(in Stat stat)
            {
                var type = stat.ValuePair.Type;
                add = type.ZeroVariant();
                multiply = type.OneVariant();
            }

            partial void ApplyInternal(in StatVariant baseValue, ref StatVariant currentValue)
                => currentValue = (baseValue + add) * multiply;
        }
    }
}
```

Seven hooks in total, and which ones exist — and which must be `readonly` — is not guessable. Do not
type them from memory: put the cursor on your `partial class`, press **Ctrl+.** and pick *Generate
stat modifier skeleton*. A misplaced `readonly` makes the compiler silently decline to match the
partial, with no diagnostic at all. Details in [Declaring stats](03-DECLARING-STATS.md#the-seven-hooks).

### 4. Run it

```csharp
public sealed class Hero : MonoBehaviour
{
    private StatStore<RpgStatSystem.Stat, RpgStatSystem.StatModifier, RpgStatSystem.StatObserver> _store;
    private RpgStatSystem.Accessor _accessor;
    private RpgStatSystem.WorldData _worldData;

    private void Start()
    {
        _store = new(initialOwnerCapacity: 8, Allocator.Persistent);
        _accessor = new(_store);
        _worldData = new(32, Allocator.Persistent);

        var stats = RpgStats.Builder
            .Build(ref _store, out var owner)
            .CreateAllStats(produceChangeEvents: true)
            .ToStats();

        var handles = stats.GetStatHandles(owner);

        RpgStats.Accessor
            .Create(owner, stats, _accessor, _worldData)
            .TrySetStatBaseValue(new RpgStats.Hp(100f), out _)
            .TrySetStatBaseValue(new RpgStats.Attack(10f), out _);

        _accessor.TryAddStatModifier(
            handles.attack, RpgStatSystem.StatModifier.Add(5f), out _, ref _worldData);

        _accessor.TryGetStatValue(handles.attack, out var attack);
        Debug.Log($"attack = {attack.GetCurrentValueOrDefault().Float}");   // 15
    }

    private void OnDestroy()
    {
        // WorldData first: it owns the scratch lists the store's buffers are read into.
        if (_worldData.IsCreated) { _worldData.Dispose(); }
        if (_store.IsCreated) { _store.Dispose(); }
    }
}
```

## Lifetime rules

**Three objects, and you own all of them.** Nothing in this library is a singleton, a
`MonoBehaviour`, or a Unity system. Nobody creates or disposes anything on your behalf.

| Object | Lives as long as | Notes |
|---|---|---|
| `StatStore<…>` | your world | Native memory. Holds every owner's stats, modifiers and observers. |
| `Accessor` | the store | A value copy of the store handle. Cheap to copy, safe to keep in a field. |
| `WorldData` | one update thread | Event lists plus five scratch buffers. **Not** thread-safe, **not** reentrant. |

**Dispose order is `worldData` then `store`.** WorldData holds the scratch lists that the store's
buffers are read into; disposing the store first leaves the next WorldData operation reading freed
memory.

**One `WorldData` per update thread.** Never share one between two jobs running concurrently, never
call a mutating accessor method while iterating its event list. If you need parallelism, the pattern
is *collect in parallel → apply single-threaded* — see [Jobs & performance](06-JOBS-AND-PERFORMANCE.md).

**`WorldData` accumulates events until you clear it.** Call `worldData.Clear()` once per frame after
whatever consumes the events has run, or the lists grow without bound.

## Where to go next

- [Core concepts](02-CONCEPTS.md) — what actually happens when you write a value.
- [The sample](../../../ApexionGame.Entities.Stats.Samples/README.md) — ten cases you can click,
  including cross-owner dependencies, cycle rejection, and save/load.
- [Pitfalls & FAQ](09-PITFALLS.md) — read before your first bug, not after.
