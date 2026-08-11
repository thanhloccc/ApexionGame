# ApexionGames — Land of Souls

Unity 6000.3 game project built on **EncosyTower** (`Packages/com.laicasaane.encosy-tower`, v0.1.7-preview.3).

## Plan-first gate — check before anything else

**A feature request produces a plan document, not code.** Write the plan, say it is ready for
review, and stop. Code starts only when the user explicitly asks to implement.

| The user says | You produce |
|---|---|
| "làm feature X", "thêm hệ thống X", "tạo màn hình X", "cần chức năng X" | **the plan doc, no source files** |
| "implement", "code đi", "viết code đi", "triển khai đi", "OK làm luôn" | **the code**, per the approved plan |

**Phase 0 — load the right skills, and say which.** Pure C# → `encosy-tower`. Anything touching
Unity assets, scenes, prefabs, inspector wiring, editor windows, tests or builds → `encosy-tower` +
`unity-cli-workflow`. If a skill the task genuinely needs does not exist, say so before starting.

**Phase 1 — ask clarifying questions** (`AskUserQuestion`, ≤4 per round, one or two rounds,
recommended option first). Push hardest on **what the expected output concretely looks like** — the
more sharply it can be pictured while planning, the fewer wrong turns later. Never ask what the repo
can answer or what has an obvious default.

**Phase 2 — write the plan.**
- Location: `Documentation~/` of the assembly that owns the feature.
- Naming: `<Topic> - <Aspect>.md`, topic prefix repeated on every file of the set —
  `Combat - Overview.md`, `Combat - Data Model.md`, `Combat - Encosy Mapping.md`.
- **Bilingual**: `X.md` (English) + `X.vi.md` (Vietnamese), written in the same turn, kept
  section-for-section in sync. Never translate code, identifiers, paths or `DEC-xxx` ids.
- `<Topic> - Overview.md` opens with **Request summary → Expected output → Steps**, then status,
  goals/non-goals, **Encosy module mapping**, **data model with the collection chosen per type**,
  **exact folder/namespace/file layout**, API surface, decisions.
- Steps are a dependency-ordered table: one action per row, a verifiable done-condition, the exact
  files touched. Completeness test: **could a fresh session with no memory of this conversation
  execute it from the file alone?**

**Phase 3 — stop for review.** List the paths, end the turn. Do not start coding or offer to.

On approval: execute the Steps in order, fix the plan in the same turn if reality diverges, and
report back against Expected output item by item.

- Code snippets inside the plan are expected; creating the real source files is what waits.
- Does **not** apply to bug fixes, edits to existing code, renames, formatting, questions, or
  investigation. Asked to implement with no plan? Write the plan first, then stop, and say why.

## Standing rule

**Before implementing anything, check whether EncosyTower already provides it.**
Invoke the `encosy-tower` skill — it holds the decision matrix (feature → module), the full module
map, verified recipes, and this project's define/gate status.

Then, in your reply: name the EncosyTower modules you used. If you hand-rolled part of it, give one
sentence on why nothing in the package fit. Never silently reimplement something the package has.

EncosyTower covers PubSub, Processing (request/response), Vaults (service locator), Pooling,
PageFlows.MonoPages (UI screens & popups), MVVM + ViewBinding, Databases (spreadsheet → SO),
Persistences (save/load), asset Keys, Localization, Settings, Logging, `Option`/`Result`,
Ids/StringIds/TypeIds, source-generated type tools, Collections & Buffers, an in-game cheat console,
and a large editor toolbox.

## Error contracts — non-negotiable

**A gameplay/domain type used as `TError` in `Result<T, TError>` is never a flat `enum`.** Model it
as a `[PolyEnumFactoryFor]` wrapper over a `[PolyEnumStruct]` case union.

- Required: an `Undefined` case (so `default(TError).ToString()` is safe), `FixedString512Bytes`
  messages, `Prefix(...)`, typed immutable payloads (ids, states, tokens, amounts) — never strings
  where a typed id exists, never mutable objects/exceptions/Unity objects/collections.
- Call sites use the **generated factories with parentheses**: `PlayerError.Dead()`,
  `PlayerError.UnknownDefinition(skillId)`.
- Plain enums remain right for states, modes, flags, categories that are not error contracts.
- Reference implementation: `Assets/Game/Game.Gameplay/Player/Common/PlayerError.cs`. Also
  conforming: `WeaponError`, `EquipmentError`. `Game.Data/Persistence/Player/PlayerPersistenceError`
  is the remaining **legacy flat enum** — do not copy it; migrate it when that surface is touched.
- Before handoff, audit every new/touched `*Error` and `Result<T,TError>` call site, and compile
  through Unity so PolyEnum generation actually runs. Full rules and the audit commands:
  `.claude/skills/encosy-tower/references/structured-errors.md`.

## Unity tooling

The official **Unity CLI** + Unity Pipeline package is this project's Unity integration.
**Unity MCP is deliberately not used** — do not require, install, or invoke it unless the user
explicitly reverses that policy. Editor-owned work (scenes, prefabs, imports, inspector state,
tests, builds) goes through `unity-cli-workflow`; direct file edits are preferred wherever they
fully accomplish the task. Source generators only run inside a Unity compile — IDE analysis is not
evidence that generated members exist.

## Performance defaults

Applies to every file, from the first line — not a later optimisation pass.

- `FasterList<T>` over `List<T>`; `ArrayMap<K,V>` / `ArraySet<T>` over `Dictionary` / `HashSet`.
- `<Type>.ReadOnly` / `DictionaryReadOnly` / `HashSetReadOnly` over `IReadOnly*` params.
- Rent short-lived collections: `FasterListPool<T>`, `ArrayMapPool`, `QueuePool`, `StackPool`,
  `StringBuilderPool`.
- Data crossing into a job → the `Shared*` family (`SharedArray<T>`, `SharedList<T>`,
  `SharedArrayMap<K,V>`): one allocation, managed + native views, no copy.
- Job/Burst side → `Unity.Collections` when the consumer dictates the type, or EncosyTower's
  `ListNative` / `ArrayMapNative` / `QueueNative` / `StackNative` / `ReferenceNative`. Always
  dispose. `Collections.Unsafe` only with a measured, commented reason.
- `Unity.Mathematics` (`float2/3/4`, `quaternion`, `half`, `math.*`) for anything per-frame,
  per-entity, in a job, or `[BurstCompile]`d. `Mathf` / `Vector3` / `Quaternion` stay at the Unity
  API boundary. `math.lengthsq`/`distancesq` when comparing, `math.saturate`, `math.rsqrt`,
  `math.select` instead of a Burst-inner-loop branch.

Plain `List<T>` / `Dictionary<K,V>` / `Mathf` remain right for startup code, small fixed sets,
serialized fields, and public contracts where `IReadOnlyList<T>` is honest — use them there without
wrapping.

## UI Toolkit

**All UI is built in C#, not UXML** — 27 `.uss` files vs 1 `.uxml` across EncosyTower, and the
project's own RTS HUD records why: a UXML asset's main-object id is content-derived, so a
hand-authored scene cannot reference one reliably.

- Layout: `Views/` (one `VisualElement` per file) + `StyleSheets/` (`X.uss`, `X_Dark.uss`,
  `X_Light.uss`, `X.tss`). Neither folder adds a namespace segment. Editor UI goes in the sibling
  `*.Editor` assembly.
- Split every non-trivial element into `XView : VisualElement` (tree only) and
  `XViewController : IDisposable` (data, refresh, callbacks), linked through `view.userData`.
- USS class names are `public static readonly string` constants, kebab-lower, BEM-ish
  (`block__element--modifier`), first line of the ctor is `AddToClassList(UssClassName)`.
- Load sheets by path constants built from a module root, via `WithEditorStyleSheet(...)`; at runtime
  serialize a `StyleSheet` and use `WithStyleSheet(...)`, or style inline with a `*Theme` +
  `*Widgets` pair.
- Reference implementations: `ApexionGame.Entities.Stats.Editor` (editor),
  `ApexionGame.Entities.Stats.Samples/Rts.Game/Hud` (runtime).
- Full rules: `.claude/skills/encosy-tower/references/ui-toolkit.md`.

## Structure & naming defaults

Copy EncosyTower's organization. Decide placement before creating files.

- Assembly name = `rootNamespace` = folder name. Editor/authoring/test surfaces are **sibling
  assemblies** with dotted suffixes (`X.Editor`, `X.Authoring`, `X.Tests`), never subfolders.
- One top-level folder per module, named after the concept; dotted (`Pooling.Native`,
  `Collections.Extensions`) when it has its own namespace, gate, or audience.
- Sub-folder vocabulary: `Contracts/`, `Annotations/`, `Internals/`, `SourceGen/`, `Extensions/`,
  `APIs/`, `Common/`, `Converters/`, `Generators/`, `Components/`, `Storage/`, `Debugging/`,
  `Views/`, `StyleSheets/`. Never `Interfaces/`, `Utils/`, `Misc/`, `Scripts/`. Small modules
  stay flat.
- **Folders organize; namespaces serve the consumer.** A sub-folder does *not* add a namespace
  segment — a module's public surface lives in one flat namespace. Only `Internals`, `Unsafe`,
  `Extensions`, `SourceGen`, `Editor.*`, `Caches`, `Generators`, `Debugging` earn a segment.
- Files: `Type.cs`, `Type+Aspect.cs`, `Type+Nested.cs`, `` Type`N.cs ``, `Type_Async.cs`,
  `*.gen.cs` (never hand-edit). One primary type per file, one extension class per file.
  Moving a file must carry its `.meta`.
- Naming: `_camelCase` private, `s_camelCase` private static, `ALL_UPPER` const, PascalCase public
  readonly/static. `Encosy<Type>Extensions` for foreign types, `<Type>Extensions` for own.
  Suffixes `Async` / `Attribute` / `API`; prefixes `Global*` / `Static*`.
  `Try*` → `bool`+`out` or `Option<T>`; `*OrError` → `Result<T,Error>`; `*OrThrow`; `*OrDefault`.
- Usings outside a **block-scoped** namespace, alphabetical, three groups, no blank line between:
  `System*` → `EncosyTower*`/own → `Unity*`. File-scoped namespaces only in tests and samples.
- Member order: fields → ctors → indexers → properties → operators → methods → nested types;
  then `const` → `static readonly` → `static` → instance; then `public` → `protected` → `private`.
  Serialized fields first on `MonoBehaviour` / `ScriptableObject`.

## Project facts

- **DOTS is not installed.** `com.unity.entities` is absent from `Packages/manifest.json`, so
  `EncosyTower.Entities.Stats` and `EncosyTower.Core/Entities` are compiled out. `ENTITY_STORE_V1`
  and `LATIOS_ENTITIES_1_4` in Player defines are leftovers, not a signal that DOTS is active.
- **`ApexionGame.*` (HFSM core + Entities.Stats fork) lives in the embedded package
  `Packages/com.apexion.apexion-game/`**, not under `Assets/`. `Assets/Game/*` is the game itself and
  consumes both `com.laicasaane.encosy-tower` and `com.apexion.apexion-game`.
- **`Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats` is a deliberate DOTS-free fork** of
  the package's stats system. Edit it there; never "fix" it by adding Unity.Entities or syncing it to
  the package.
- **Gameplay assemblies (verify current contents before relying on this list):**
  `Game.Common` (ids, shared types, stat facade) → `Game.Data` (+ `.Authoring`; Databases tables,
  Persistences) → `Game.Gameplay` (+ `.Editor`, `.Tests`; Player, Weapons, Equipment) plus
  `Game.Input` and `ApexionGame.Core`. Existing feature docs live in
  `Assets/Game/Game.Gameplay/Documentation~/` (`Equipment System - *`, `Player System - *`) —
  read the matching one before extending a system.
- New assemblies must reference `EncosyTower.Core` **and copy the `versionDefines` block** from
  `EncosyTower.Core.asmdef`, or `#if` guards evaluate false and code silently disappears.
- Every type carrying an EncosyTower attribute must be `partial`; generated members use the
  `get => Get_X(); set => Set_X(value);` forwarding-body convention.
- **Style: `CODING-CONVENTIONS.md` at the repo root** (EncosyTower's own guide) governs all C# here.

## Where things live

- Skill: `.claude/skills/encosy-tower/` — `SKILL.md`, plus `references/`:
  `planning-workflow`, `feature-docs`, `module-map`, `recipes`, `collections-and-math`,
  `structured-errors`, `structure-and-naming`, `setup`.
- Skill: `.claude/skills/unity-cli-workflow/` — Unity CLI / Pipeline / batch mode, validation rules.
- Skills available: `encosy-tower` and `unity-cli-workflow`, both project-level.
- Feature docs: `Documentation~/` of the owning assembly (e.g.
  `Assets/Game/Game.Gameplay/Documentation~/`). Existing example of the project's doc culture:
  `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/Documentation~/`.
- Memory: `.claude/memory/` — index in `MEMORY.md`, one fact per file.
- Package samples (best ground truth): `Packages/com.laicasaane.encosy-tower/Samples~/`.
