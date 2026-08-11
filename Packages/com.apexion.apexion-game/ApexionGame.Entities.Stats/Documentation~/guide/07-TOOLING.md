# Tooling

*[Tiếng Việt](vi/07-TOOLING.md) · [Guide index](README.md)*

## Stat Debugger

**ApexionGame ▸ Stats ▸ Stat Debugger**

Shows a live store: every owner, every stat as `base → current` with its modifier count, and the
observer graph drawn as a node diagram.

A `StatStore` is native memory that *your* code owns. Nothing can discover it, so you register it:

```csharp
_debug = new StatStoreDebug<
      RpgStatSystem.ValuePair, RpgStatSystem.Stat
    , RpgStatSystem.StatModifier, RpgStatSystem.StatObserver>("battle", _store, ResolveStatName);

StatDebugRegistry.Register(_debug);

// ... and Unregister BEFORE disposing the store.
StatDebugRegistry.Unregister(_debug);
```

Skipping registration costs nothing: the runtime never reads the registry.

**Unregister before `store.Dispose()`.** A registration that outlives its store hands the debugger a
dangling pointer.

### Stat names

The third argument is optional and turns `#3` into `attack`:

```csharp
private string ResolveStatName(StatOwnerHandle owner, int index)
{
    if (owner == _heroOwner) { return ((RpgStats.Type)index).ToString(); }
    if (owner == _auraOwner) { return ((AuraStats.Type)index).ToString(); }
    return string.Empty;
}
```

It has to be a callback rather than a lookup the library builds itself, because the mapping is
**per owner, not per store**. One store can hold owners built from different `[StatCollection]`s, and a
bare `StatHandle` carries no trace of which one built it — only the code that created the owner knows.
Leave it null and stats show as `#N`.

Give the debug view a **stable name**. Naming it after the current game state rewrites the debugger's
store dropdown on every change, which reads as the store vanishing and coming back.

### The graph view

Nodes are stats, edges are observer relationships. **A red node means a cycle was detected** — which
should be impossible, because `TryAddStatModifier` refuses any modifier that would close one. If it
ever lights up, that is a bug in the runtime, not a feature of your data.

## Stats Playground

**ApexionGame ▸ Stats ▸ Stats Playground**

Ten cases you can click, with live values beside them: cross-owner dependencies, an unequal-depth
diamond, cycle rejection, destroying an observed owner, save/load/remap, and an event dump. No Play
mode needed — the store is native memory the window owns, and closing it frees everything.

The same ten cases are also on a scene GameObject
([`stats-playground.unity`](../../../ApexionGame.Entities.Stats.Samples/), component marked
`[ExecuteAlways]`), if you prefer the Inspector. Each button rebuilds a clean world first, so order
does not matter.

Details: [the sample's README](../../../ApexionGame.Entities.Stats.Samples/README.md).

## Quick Actions

Put the cursor on a `partial class` or `partial struct` and press **Ctrl+.**:

| Action | Writes |
|---|---|
| *Make this a stat system* | the `[StatSystem]` attribute |
| *Generate stat modifier skeleton* | `StatModifier` + `Stack` with all seven `partial void ...Internal` hooks |
| *Generate stat collection skeleton* | `[StatCollection]` with a type-id seed that avoids the numbers already used in the project |

The modifier skeleton is the one worth reaching for. Which hooks exist, and which must be `readonly`,
is not guessable — and a misplaced `readonly` makes the compiler silently decline to match the partial
method, with no diagnostic anywhere.

## Authoring

`StatVariant` carries `[Serializable]` **and** nineteen fields at `[FieldOffset(0)]`. Unity ignores
explicit layout when serialising, so authoring one directly produces garbage. The `.Authoring`
assembly exists for that reason.

```csharp
using ApexionGame.Entities.Stats.Authoring;

[SerializeField] private SerializableStatVariant _hp;

// at runtime
StatVariant value = _hp.ToStatVariant();
```

`SerializableStatVariant.Of(...)` builds one from a `float`, `int`, `bool` or `float4`; `From(in
StatVariant)` converts an existing runtime value back.

`StatDefinitionAsset` is a `ScriptableObject` holding a named set of authored stats:

```csharp
[CreateAssetMenu] // ApexionGame/Stats/Stat Definition
```

| Field per entry | |
|---|---|
| `Id` | your string key — match it to a generated `TypeId` when applying |
| `BaseValue` | a `SerializableStatVariant` |
| `ProduceChangeEvents` | per-stat flag |
| `UserData` | raw type-id payload |

It deliberately references **no generated type**. A strongly typed asset would have to be generated per
`[StatCollection]`; instead the asset carries values only and your project decides how to map `Id` onto
generated handles — usually one small `switch` per collection.

Creating the owner still goes through `RpgStats.Builder.Build(ref store)`, since that is the only path
that seeds the None stat.

## Regenerating the value-type tables

`Common/StatVariant.gen.cs`, `StatVariantType.gen.cs`, `StatDataSize.gen.cs`,
`StatVariantTypeExtensions.gen.cs` and `StatSingleExtensions.gen.cs` are generated from
`Generators/StatTypeTable.cs` by an **editor** generator, and the output is committed. A normal
consumer never runs this. You only need it to add a value type — `float3x3` for a tensor stat, say.

1. Project Settings ▸ Player ▸ Scripting Define Symbols: add `APEXION_STAT_VALUE_TYPES_GENERATOR`.
2. Edit `Generators/StatTypeTable.cs` — add the type, or change the gameplay profile.
3. Menu **Tools ▸ UnityCodeGen ▸ Generate**.
4. **Remove the define from step 1.**
5. Rebuild the Roslyn generators, because the Roslyn-side copy of the table changed:
   `dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release`
6. Commit the `.gen.cs` files and the DLLs.

> Step 4 is not optional. Leave the define on and the seven files under `Generators/` compile into the
> assembly on every build.

Remember the size rules from [Declaring stats](03-DECLARING-STATS.md#value-types-and-the-size-budget): a
new type is usable at all only if `size <= maxDataSize`, and usable as a value pair only if
`size <= maxDataSize / 2`.

## Validation defines

Runtime checks live in `ThrowHelper` behind `[Conditional]` attributes, so they disappear from release
builds. `Debugging/ValidationDefines.cs` names the symbols:

| Symbol | Effect |
|---|---|
| `UNITY_EDITOR`, `DEBUG` | checks on (implicit) |
| `APEXION_RUNTIME_CHECKS` | turn checks on in a build |
| `APEXION_STATS_RUNTIME_CHECKS` | stats-specific checks |
| `ENABLE_UNITY_COLLECTIONS_CHECKS` | the `AtomicSafetyHandle` guards on `StatStore` / `StatBuffer<T>` |
| `DISABLE_APEXION_CHECKS` | escape hatch — neuters every symbol above |

Note the asymmetry: Burst's *Safety Checks Off* button does **not** remove the `APEXION_*` guards,
because they are not conditioned on `ENABLE_UNITY_COLLECTIONS_CHECKS`. If you need them gone, use
`DISABLE_APEXION_CHECKS`. Measure first — the current guards have not shown up in profiles.

## Testing outside Unity

Three things can be checked without opening the Editor, which is worth knowing when iterating on the
runtime or a generator:

```powershell
# generators and analyzers
dotnet test Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Tests

# the sample, compiled through the real generators — the regression gate for documented usage
dotnet build Plugins/SourceGenerator.ApexionGame/Samples/Samples.RpgStats/Samples.RpgStats.csproj

# Unity tests, batch mode, no Editor window
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Tests"
```

Two traps in that last pair:

- Unity's test runner **drops `[Explicit]` tests even when you name them in `--filter`**. That is why
  the benchmarks are not marked `[Explicit]`.
- `dotnet test` prints `Passed!` even when tests return `Inconclusive`. Read the skip count.

Set `UNITY_OS_INSTALL_ROOT` to your Unity install directory once, and `unity test` works from any
shell.
