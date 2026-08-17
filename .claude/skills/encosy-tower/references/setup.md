# EncosyTower in *this* project — what is live, what is not

Verified against `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset` and every
`.asmdef` on 2026-08-15. Re-check if the manifest changes.

For the studio's **own** modules — the HFSM in `ApexionGame.Core`, the stats fork, and the current
state of `Assets/Game/` — see `first-party-modules.md`.

## Module availability right now

| Module | Status | Why |
|---|---|---|
| Core (PubSub, Processing, Vaults, Pooling, Collections, Common, Ids, Types, Logging, Settings, Keys, IO, Naming, Conversion, Debugging, Tasks, Buffers, Jobs, Encryption, Serialization) | **live** | always compiled |
| PageFlows / MonoPages | **live** | UniTask installed → `UNITASK` |
| Mvvm + ViewBinding + Variants | **live** | uGUI + TMP installed |
| Addressables keys, AtlasedSprites | **live** | `com.unity.addressables` 2.9.1 |
| Localization (`L10n`, `L10nKey`) | **live** | `com.unity.localization` 1.5.12 |
| Databases (runtime) | **live** | Core |
| Databases.Authoring + Databases.Settings | **live** | `com.laicasaane.bakingsheet` 6.3.0-pre.1 → `BAKING_SHEET`; Newtonsoft present transitively |
| Search (`FuzzySearchAPI`) | **live** | `org.nuget.raffinert.fuzzysharp` 5.0.2 |
| Native pooling / native collections | **live** | `com.unity.collections` 2.6.8, `com.unity.burst` 1.8.29, `com.unity.mathematics` 1.3.3 |
| Core.Extended VisualDebugging | **live** | — |
| Editor tooling | **live** | — |
| **`EncosyTower.Entities.Stats`** | **compiled out** | `com.unity.entities` is **not** in the manifest, and the asmdef has `defineConstraints: ["UNITY_ENTITIES"]` |
| **`EncosyTower.Core/Entities`** (`[Lookup]`, `[TypeHandle]`, baker/blob extensions) | **compiled out** | same |

`ENTITY_STORE_V1` and `LATIOS_ENTITIES_1_4` appear in Player scripting defines but the packages
that make them meaningful are not installed — they are leftovers, not a signal that DOTS is active.

## This project's own layer

- `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats` — **a DOTS-free fork of
  `EncosyTower.Entities.Stats`.** Its asmdef references only `EncosyTower.Core`, Burst, Collections,
  Mathematics — no `Unity.Entities`. Storage was rewritten around `StatBuffer<T>` /
  `StatBufferLookup<T>` / `StatOwnerHandle` / `StatOwnerSlot` instead of ECS chunks, and it adds
  `Debugging/` (`StatDebugInfo`, `StatDebugRegistry`, `StatStoreDebug`) plus
  `StatTypeTable`/`StatTypeTableGenerator`.
  **Do not "fix" it by pulling in the package version or adding Unity.Entities.**
  Companion assemblies: `.Authoring`, `.Editor`, and the RTS sample folder
  `ApexionGame.Entities.Stats.Samples.Rts/` holding `Rts.Core/` + `Rts.Game/`. Tests live in the
  shared `ApexionGame.Tests.EditorMode`, not a `.Tests` sibling.
  Its generator define is `APEXION_STAT_VALUE_TYPES_GENERATOR` (Standalone), the package equivalent
  being `ENCOSY_STAT_VALUE_TYPES_GENERATOR`.
- `ApexionGame.Core` — the **HFSM** module (`ApexionGame.HFSM`), the studio's state machine. Use it
  instead of hand-rolling one; EncosyTower has no FSM module. Details in `first-party-modules.md`.
- `ApexionGame.Tests.EditorMode` — the only first-party test assembly, and EditMode only. There is
  no first-party PlayMode suite; say so rather than implying PlayMode coverage.

### `Assets/Game/` is empty scaffolding right now

`Game.Common/`, `Game.Data/`, `Game.Data.Authoring/`, `Game.Gameplay/` exist as **folders with no
`.asmdef` and no source files**. Only `Assets/Game/Input/` (`Game.Input.asmdef`,
`PlayerInputActions`), `Addressables/` and `Scenes/` hold anything.

Earlier revisions of this file described a populated `Game.Common → Game.Data → Game.Gameplay` chain
with Player/Weapons/Equipment systems and feature docs under `Game.Gameplay/Documentation~/`. That
code is **not in the working tree and not in git history** — treat game-side gameplay as greenfield.
The intended dependency direction still holds when those assemblies are created:
`Game.Common` (ids, shared types, stat facade) → `Game.Data` + `Game.Data.Authoring` (Databases
tables, Persistences) → `Game.Gameplay` — and `Game.Common` must never depend upward. The first file
in any of those folders also creates the `.asmdef`, wired as below.

## Wiring a new assembly

Add to your `.asmdef` `references`:

- `EncosyTower.Core` — always.
- `EncosyTower.Mvvm` — for `MonoView` / `MonoBinder` / adapters.
- `EncosyTower.Core.Extended` — for VisualDebugging commands.
- `UniTask`, `Unity.Addressables`, `Unity.ResourceManager`, `Unity.Localization`,
  `Unity.Burst`, `Unity.Collections`, `Unity.Mathematics`, `Unity.TextMeshPro` — as needed.
- `EncosyTower.Editor` — editor-only assemblies.

Copy the `versionDefines` block from `Packages/com.laicasaane.encosy-tower/EncosyTower.Core/EncosyTower.Core.asmdef`
so your code sees the same `UNITASK`, `UNITY_ADDRESSABLES`, `UNITY_COLLECTIONS`, `UNITY_BURST`,
`UNITY_ENTITIES`, `UNITY_LOCALIZATION`, `UNITY_NEWTONSOFT_JSON`, `UNITY_UGUI`, `UNITY_TEXTMESHPRO`,
`LATIOS_FRAMEWORK`, `ANNULUS_CODEGEN`, `NUGET_RAFFINERT_FUZZYSHARP` symbols. Without them your
`#if` guards silently evaluate false.

## Scripting define symbols

Currently set (Standalone): `DISABLE_DEBUG_ASSERT`, `REMOVE_DISPOSE_SENTINEL`, `ENTITY_STORE_V1`,
`LATIOS_ENTITIES_1_4`, `UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS`, `ENABLE_SDK_STYLE_PROJECTS`,
`SAINTSFIELD_SAINTS_EDITOR_APPLY`, `ENCOSY_PAGE_FLOW_PUBSUB_INCLUDE_CALLER_INFO_DEV`,
`ODIN_INSPECTOR*`, `APEXION_STAT_VALUE_TYPES_GENERATOR`.

EncosyTower-specific symbols you may want:

| Define | Effect |
|---|---|
| `ENCOSY_PUBSUB_RELAX_MODE` | messages no longer need `IMessage` (**not set — keep implementing `IMessage`**) |
| `ENCOSY_PROCESSING_RELAX_MODE` | requests no longer need `IRequest*` (not set) |
| `ENCOSY_PUBSUB_RUNTIME_CHECKS` / `ENCOSY_PROCESSING_RUNTIME_CHECKS` / `ENCOSY_COLLECTIONS_RUNTIME_CHECKS` / `ENCOSY_RUNTIME_CHECKS` | keep validation in non-editor builds |
| `ENCOSY_PAGE_FLOW_PUBSUB_INCLUDE_CALLER_INFO[_DEV]` | stamp caller file/line into page messages (**`_DEV` already on**) |
| `ENCOSY_DISABLE_LOG_INFO` | strip `LogInfo` calls |
| `ENCOSY_INCLUDE_AUTHORING` | compile `Databases.Authoring` into player builds |
| `ENCOSY_CLEAR_GLOBAL_STRING_VAULT_ON_ENTER_PLAY_MODE` | reset `GlobalStringVault` between play sessions |
| `ENCOSY_MVVM_ADAPTERS_GENERATOR` / `ENCOSY_VARIANTS_GENERATOR` / `ENCOSY_BYTE_BOOLS_GENERATOR` / `ENCOSY_STAT_VALUE_TYPES_GENERATOR` | re-run the checked-in `.gen.cs` writers (needs `com.annulusgames.unity-codegen`) |

`Tools ▸ Encosy ▸ Project Features` (`ProjectFeaturesWindow`) lists every `[Feature]` struct and
installs its `[RequiresPackage]` dependencies — use it before hand-editing `manifest.json`.

## Source-generator gotchas

- **`partial` is mandatory** for any type carrying an EncosyTower attribute. Missing `partial` is
  the #1 cause of "the generated method doesn't exist".
- `[ObservableProperty]` goes on a **property** whose body is `get => Get_X(); set => Set_X(value);`.
  The generator creates `Get_X`, `Set_X` and the `_x` field. Same shape for `[Data]` +
  `[DataProperty]` (`Get_X()`) and `[Table]` (`Get_X()`).
- Generated code only appears after a compile. If IntelliSense shows red but Unity compiles clean,
  it is the IDE, not the code.
- An assembly can opt out with `[assembly: SkipSourceGeneratorsForAssembly]` — several
  `SkipSourceGeneratorsForAssemblyAttribute.cs` files exist per feature namespace; do not copy them
  into gameplay assemblies.
- `.gen.cs` files are checked in and regenerated by `com.annulusgames.unity-codegen` behind the
  `*_GENERATOR` defines. Do not hand-edit them.
- Database authoring types are `[Conditional("UNITY_EDITOR")]` / `ENCOSY_INCLUDE_AUTHORING` — keep
  authoring code in an editor-only file or assembly.

## Coding conventions

**Owned by the `coding-standards` skill** — load it rather than reading anything here.

`CODING-CONVENTIONS.md` at the repo root is the authoritative document (1063 lines, 14 sections).
`coding-standards/SKILL.md` opens with a table mapping each section to whichever skill owns it, so a
convention question is answered without reading the whole thing.

A digest used to live here. It covered 2 of the document's 14 sections and was a third copy of rules
that already had owners, so it was removed rather than left to drift.
