---
name: encosy-tower
description: REQUIRED for any feature work in this Unity project. Carries the doc-first gate — a feature request produces a design doc for review, and code starts only when the user explicitly asks to implement. Also the guide to EncosyTower, this project's standard library: consult it BEFORE writing any runtime, editor, or infrastructure C#. Use when implementing ANY feature — events/messaging, request-response, save/load, game data tables, UI screens & popups, MVVM data binding, object pooling, service locator, asset loading (Addressables/Resources), localization, settings, logging, IDs/enums/newtypes/unions, in-game cheat console, or editor tooling — to decide which EncosyTower module replaces hand-rolled code. ALSO required whenever code declares a collection or does math (performance defaults: FasterList/ArrayMap/ArraySet over List/Dictionary/HashSet, Shared*/Native for jobs, Unity.Mathematics over Mathf/Vector3), whenever a method can fail and returns Result/Option (error contracts are PolyEnumStruct unions, never flat enums), and whenever a file, folder, namespace, assembly, or type is created or renamed (structure & naming defaults copied from EncosyTower). ALSO covers this studio's own modules layered on top — the HFSM state machine in `ApexionGame.Core` (use it, never hand-roll an FSM) and the DOTS-free `ApexionGame.Entities.Stats` fork. Triggers on "implement", "optimize", "performance", "job", "Burst", "refactor", "rename", "error", "Result", "validation", "folder structure", "asmdef", "namespace", "state machine", "FSM", "HFSM", "stats", "modifier", and Vietnamese phrasing: "làm chức năng", "viết hệ thống", "thêm feature", "tạo màn hình", "lưu game", "bắn event", "tối ưu", "xử lý lỗi", "cấu trúc thư mục", "đặt tên", "tách code", "máy trạng thái", "chỉ số".
---

# EncosyTower — apply before hand-rolling

`Packages/com.laicasaane.encosy-tower` (v0.1.7-preview.3) is this project's standard library.
Nearly every generic system a game needs already exists there, source-generated and allocation-conscious.

**Default stance: do not hand-roll infrastructure. Find the module, use it, and say so.**

## Plan-first gate — check this before anything else

**A feature request produces a plan document, not code.** Write the plan, say it is ready for
review, and stop. Code starts only when the user explicitly asks to implement.

| The user says | You produce |
|---|---|
| "làm feature X", "thêm hệ thống X", "tạo màn hình X", "cần chức năng X" | **the plan doc, no source files** |
| "implement", "code đi", "viết code đi", "triển khai đi", "OK làm luôn" | **the code**, per the approved plan |

Four phases (full detail in `references/planning-workflow.md`):

0. **Load the right skills first, and say which.** **Every feature: `encosy-tower` +
   `system-design`** — the second carries the design gate and is not optional. Add
   `unity-cli-workflow` for anything touching Unity assets, scenes, prefabs, inspector wiring,
   editor windows, tests or builds; `coding-standards` once C# is being written. If a skill the task
   genuinely needs does not exist, say so before starting instead of improvising around the gap.
1. **Ask clarifying questions** (`AskUserQuestion`, ≤4 per round, one or two rounds, recommended
   option first). Push hardest on **what the expected output concretely looks like** — the more
   sharply the user can picture it while planning, the fewer wrong turns later. Never ask what the
   repo can answer or what has an obvious default.
2. **Write the plan.** `Documentation~/` of the owning assembly, `<Topic> - <Aspect>.md`, bilingual
   `X.md` + `X.vi.md` in the same turn. `<Topic> - Overview.md` opens with **Request summary →
   Expected output → Steps**, then status, goals/non-goals, **Encosy mapping**, **data model with
   the collection chosen per type**, **exact folder/namespace/file layout**, API surface, decisions.
   **Plus the `system-design` gate — decomposition, state ownership table, communication, rejected
   alternatives, and (when the feature has a performance dimension) the chosen performance tier.**
   Steps are a dependency-ordered table, each row one action with a verifiable done-condition and
   the exact files it touches. Completeness test: **could a fresh session with no memory of this
   conversation execute it from the file alone?**
3. **Stop for review.** List the file paths, end the turn. Do not start coding, do not offer to
   start while the user reads.

Then, on approval: execute the Steps in order, fix the plan in the same turn if reality diverges,
and report back against the Expected output section item by item.

- Code snippets *inside* the plan are expected — API sketches are part of the design. Creating the
  real source files is what waits.
- Gate does **not** apply to: bug fixes, edits to existing code, renames, formatting, questions,
  investigation, or an explicit "khỏi doc".
- Asked to implement with no plan yet? Write the plan first in that turn, then stop — and say why.

Doc naming, aspect vocabulary, required sections, bilingual rules, file template:
`references/feature-docs.md`.

## Workflow (once implementation is approved)

0. **Check the studio's own modules first** — `references/first-party-modules.md`. A state machine is
   `ApexionGame.Core/HFSM` (EncosyTower has no FSM module); stats are the DOTS-free
   `ApexionGame.Entities.Stats` fork. Reaching past these into EncosyTower, or hand-rolling, is the
   wrong move.
1. **Match** the request against the decision matrix below.
2. **Verify the gate** — the module may be compiled out. Check `references/setup.md` for the
   define symbol / package / asmdef reference it needs. If the gate is off, say so and give the
   one-line fix (add package, add asmdef reference) instead of silently writing a replacement.
3. **Read the real API before writing code.** Grep the module folder — this skill's tables are a
   map, not a signature reference. `references/recipes.md` has verified working patterns.
4. **Audit error contracts** — every new or touched `Result<T, TError>` goes through
   `references/structured-errors.md` before you write the call sites, not after.
5. **Verify through Unity, not the IDE.** Every EncosyTower attribute is a source generator; its
   members only exist after a Unity compile. Use `unity-cli-workflow` to compile and run tests.
6. **Report** in your answer: which EncosyTower modules you used, and for anything you hand-rolled,
   one sentence on why nothing in EncosyTower fit. Never silently reimplement.

If two modules overlap (e.g. PubSub vs Processing), pick using the "vs" notes in the matrix.

## Decision matrix

Rows marked 🏠 are **this studio's own modules**, not EncosyTower — check them before the rest.

| You are implementing… | Use | Namespace |
|---|---|---|
| 🏠 A state machine — AI brain, character controller, game/UI flow, anything with states | **HFSM** — `HierarchicalStateMachine<TContext,TState>.Define(name)`, `StateBehaviour<TContext>`, `MachineRunner`. EncosyTower has **no** FSM module → `references/first-party-modules.md` | `ApexionGame.HFSM` |
| 🏠 Character/unit stats, modifiers, stat observers | **Entities.Stats fork** (DOTS-free) — `StatBuffer<T>`, `StatBufferLookup<T>`, `StatOwnerHandle` | `ApexionGame.Entities.Stats` |
| Events, decoupled notifications, fire-and-forget | **PubSub** — `GlobalMessenger`, `Messenger` | `EncosyTower.PubSub` |
| Request → response, one handler, needs a return value | **Processing** — `GlobalProcessor`, `ProcessHub` | `EncosyTower.Processing` |
| Service locator / singleton registry / cross-scene handoff | **Vaults** — `GlobalObjectVault`, `GlobalValueVault<T>`, `SingletonVault<T>` | `EncosyTower.Vaults` |
| Spawning many GameObjects, bullets, VFX, enemies | **Pooling** — `GameObjectPool`, `SceneObjectPool`, `NativeGameObjectPool`, `SceneObjectPoolBehaviour<TKey>` | `EncosyTower.Pooling`, `.Pooling.Native` |
| Temporary `List`/`Dictionary`/`StringBuilder` in a hot path | **Collection pools** — `FasterListPool<T>`, `ArrayMapPool<K,V>`, `QueuePool`, `StackPool`, `StringBuilderPool` | `EncosyTower.Pooling` |
| UI screens, popups, navigation stack, back button, transitions | **PageFlows.MonoPages** — `MonoPageCodex`, `MonoPageFlow`, `MonoPageBase<TScopes>` | `EncosyTower.PageFlows`, `.PageFlows.MonoPages` |
| View ↔ data binding, `INotifyPropertyChanged`, button commands | **MVVM** — `[ObservableObject]`, `[ObservableProperty]`, `[RelayCommand]`, `MonoView`/`MonoBinder` | `EncosyTower.Mvvm.*` |
| A value that must flow through binding without boxing | **Variants** — `Variant`, `[Variant(typeof(T))]`, `IAdapter` | `EncosyTower.Variants` |
| Game config tables from Excel/CSV/Google Sheets → ScriptableObject | **Databases** — `[Database]`, `[Table]`, `[DataTableAsset]`, `DatabaseAsset` | `EncosyTower.Databases`, `.Databases.Authoring` |
| Immutable/mutable data structs with generated accessors | **Data** — `[Data]`, `[DataProperty]`, `[DataMutable]` | `EncosyTower.Data` |
| Save/load player progress, multi-slot, encrypted, versioned | **Persistences** — `[Persistence]`, `[Persist]`, `[PersistAccessor]`, `PersistStoreDefault<T>` | `EncosyTower.Persistences` |
| Encrypting a save blob or config string | **Encryption** — `AesEncryption`, `RijndaelEncryption`, `EncryptionBase` | `EncosyTower.Encryption` |
| Loading prefabs/sprites/scenes by string key | **Keys** — `AddressableKey<T>`, `ResourceKey<T>`, `AssetKey<T>`, `AtlasedSpriteKey`, `SceneBuildIndex` | `EncosyTower.AddressableKeys`, `.ResourceKeys`, `.AssetKeys`, `.AtlasedSprites`, `.Scenes` |
| PlayerPrefs-backed setting with a typed key | **ConfigKeys** — `ConfigKey<T>` + `GetPlayerPref()`/`SetPlayerPref()` | `EncosyTower.ConfigKeys` |
| Localized text / language switching | **Localization** — `L10n`, `L10nKey`, `L10nLanguage` | `EncosyTower.Localization` |
| Project Settings page backed by a ScriptableObject | **Settings** — `Settings<T>` + `[Settings(SettingsUsage…)]` | `EncosyTower.Settings` |
| Logging, dev-only logging, in-memory log for on-screen console | **Logging** — `DevLogger`, `StaticLogger`, `SlimLogger`, `StringBuilderLogger`, `IFixedLogger` (Burst) | `EncosyTower.Logging` |
| A method that may fail — instead of `null` / `bool` + `out` | **Common** — `Option<T>`, `Result<T>`, `Result<T,TError>`, `Error<T>` | `EncosyTower.Common` |
| The `TError` of a `Result<T,TError>` — a gameplay/domain failure | **PolyEnumStructs** — `[PolyEnumFactoryFor]` wrapper + `[PolyEnumStruct]` case union. **Never a flat enum** → `references/structured-errors.md` | `EncosyTower.PolyEnumStructs` |
| A strongly-typed wrapper over `int`/`string`/`Guid` (newtype) | **TypeWraps** — `[WrapType]`, `[WrapRecord]` | `EncosyTower.TypeWraps` |
| One ID that can be several kinds (Hero \| Enemy \| Item) | **UnionIds** — `[UnionId]`, `[UnionIdKind]` | `EncosyTower.UnionIds` |
| Fast/alloc-free enum `ToString`, `TryParse`, flag ops | **EnumExtensions** — `[EnumExtensions]`, `[EnumExtensionsFor]` | `EncosyTower.EnumExtensions` |
| Composing one enum from several enums (modular content) | **EnumTemplate** — `[EnumTemplate]`, `[EnumTemplateMembersFromEnum]` | `EncosyTower.EnumExtensions` |
| A discriminated union of structs (a command, an event payload, a case union). **Not a state machine** — that is HFSM, first row | **PolyEnumStructs** — `[PolyEnumStruct]` | `EncosyTower.PolyEnumStructs` |
| Interning strings to integer IDs (Burst-safe) | **StringIds** — `StringId`, `StringVault`, `GlobalStringVault`, `UnmanagedString` | `EncosyTower.StringIds` |
| Lightweight handles / instance IDs | **Ids** — `Id`, `Id2`, `Id3`, `Id<T>` | `EncosyTower.Ids` |
| Runtime type lookup, AOT-safe type cache, `typeof` → int | **Types** — `TypeId`, `TypeId<T>`, `TypeInfo`, `RuntimeTypeCache` | `EncosyTower.Types` |
| **Any** list / map / set — default, not just when optimizing | **Collections** — `FasterList<T>`, `ListFast<T>`, `ArrayMap<K,V>`, `ArraySet<T>` | `EncosyTower.Collections` |
| Data built in managed code then consumed by a job | **Shared\*** — `SharedArray<T>`, `SharedList<T>`, `SharedArrayMap<K,V>` (one allocation, both views) | `EncosyTower.Collections` |
| Unmanaged / Burst / job-side container | **Unity.Collections** (`NativeArray`, `NativeList`, `NativeHashMap`, `FixedString*`) or **`*Native`** (`ListNative`, `ArrayMapNative`, `QueueNative`, `StackNative`, `ReferenceNative`) | `Unity.Collections`, `EncosyTower.Collections` |
| **Any** vector / quaternion / scalar math in a hot path or job | **Unity.Mathematics** — `float2/3/4`, `int2/3/4`, `quaternion`, `half`, `math.*` (not `Mathf`/`Vector3`) | `Unity.Mathematics` |
| Raw memory / resizable native buffer | **Buffers** — `BufferManaged`, `BufferNative`, `BufferUnsafe` | `EncosyTower.Buffers` |
| Common job wrappers (clear, resize, copy native containers) | **Jobs** — `ListJobs`, `HashMapJobs`, `QueueJobs`, `BitArrayJobs`, `ArrayMapNativeJobs` | `EncosyTower.Jobs` |
| In-game cheat/debug console with typed commands | **VisualDebugging** — `IVisualCommand`, `VisualCommanderPage` (Core.Extended) | `EncosyTower.VisualDebugging.Commands` |
| Fuzzy string search (search box, command palette) | **Search** — `FuzzySearchAPI` | `EncosyTower.Search` |
| JSON serialize/deserialize | **Serialization** — `JsonHelper`, `ParsableStructConverter<T>` | `EncosyTower.Serialization.NewtonsoftJson` |
| File paths, persistent data folder | **IO** — `PathAPI`, `RootPath` | `EncosyTower.IO` |
| snake_case / kebab-case / PascalCase conversion | **Naming** — `NamingPolicy`, `NameCasing` | `EncosyTower.Naming` |
| Guard clauses / precondition checks stripped in release | **Debugging** — `Checks`, `ThrowHelper`, `ValidationDefines` | `EncosyTower.Debugging` |
| `await` that works on both UniTask and `Awaitable` | **Tasks** — `UnityTasks` | `EncosyTower.Tasks` |
| An editor window listing scenes / asmdefs / empty folders, popup menus, tables | **Editor tools** — `SceneListWindow`, `AssemblyDefinitionListWindow`, `GenericMenuPopup`, `SimpleTableView`, `EditorIcons`, `LinkXmlGenerator` | `EncosyTower.Editor.*` |
| **Any** UI Toolkit surface — editor window, inspector, settings page, runtime HUD or debug panel | **UIElements** — build the tree in C#, never UXML; `Views/` + `StyleSheets/`, View/ViewController split, `With*` builders → `references/ui-toolkit.md` | `EncosyTower.UIElements`, `EncosyTower.Editor.UIElements` |
| Declaring a package/define a feature needs, with an installer UI | **ProjectSetup** — `[Feature]`, `[RequiresPackage]` → `ProjectFeaturesWindow` | `EncosyTower.Editor.ProjectSetup` |

### Choosing between overlapping modules

- **PubSub vs Processing** — PubSub: 0..N listeners, no return value, may be async, scoped by type or
  instance. Processing: exactly one registered handler, returns `TResult`, used as an RPC/query.
  A "give me the current gold amount" call is Processing; a "gold changed" broadcast is PubSub.
- **PubSub vs C# `event`** — use PubSub whenever publisher and subscriber live in different
  assemblies or scenes, or when the handler must be async. A plain `event` is fine inside one class.
- **Vaults vs a static singleton** — Vaults when lifetime is scene- or session-bound and something
  may need `WaitUntilContains`/`TryGetAsync` before it exists. Plain `static` only for pure constants.
- **GameObjectPool vs SceneObjectPool vs NativeGameObjectPool** — prefab source / scene-template
  source / job-and-Burst bulk spawn respectively.
- **Databases vs plain ScriptableObject** — Databases as soon as designers own the data or it comes
  from a spreadsheet. A single hand-tuned config asset can stay a plain `ScriptableObject` (but
  consider `Settings<T>` so it gets a Project Settings page for free).
- **Option/Result vs exceptions** — `Option<T>` for "may be absent", `Result<T,TError>` for
  "may fail with a reason the caller handles". Exceptions stay for programmer errors.
- **Flat enum vs PolyEnumStruct for `TError`** — never a flat enum. States, modes, flags and
  categories stay plain enums; anything returned as an error is a PolyEnum union so the failure
  carries its id/state/amount payload. `references/structured-errors.md`.

## Performance defaults (applies to every file you write, not just new systems)

Prefer the high-performance types by default. The BCL/`Mathf` versions are the fallback, chosen
deliberately — not the starting point.

- **Lists** → `FasterList<T>` (or `ListFast<T>` to wrap a `List<T>` a foreign API demands).
- **Dictionaries / sets** → `ArrayMap<K,V>` / `ArraySet<T>` — values live in a contiguous array, so
  they iterate as an array with no enumerator.
- **Read-only params** → `<Type>.ReadOnly` views, `DictionaryReadOnly<K,V>`, `HashSetReadOnly<T>`
  instead of `IReadOnly*` interfaces (no boxing).
- **Short-lived collections in a hot path** → rent from `FasterListPool<T>`, `ArrayMapPool<K,V>`,
  `QueuePool<T>`, `StackPool<T>`, `StringBuilderPool`.
- **Data that crosses into a job** → the `Shared*` family (`SharedArray<T>`, `SharedList<T>`,
  `SharedArrayMap<K,V>`, `SharedStack/Queue/Reference`). One allocation, both a managed and a
  `NativeArray`/native view — no `CopyTo` round trip.
- **Unmanaged / Burst / jobs** → `Unity.Collections` when the consumer dictates the type
  (`NativeArray`, `NativeList`, `NativeHashMap`, `FixedString*`, `TransformAccessArray`);
  EncosyTower's `ListNative<T>`, `ArrayMapNative<K,V>`, `QueueNative<T>`, `StackNative<T>`,
  `ReferenceNative<T>` when you want the contiguous layout or `AllocatorStrategy`. Drop to
  `Collections.Unsafe` only with a measured reason, stated in a comment. Always dispose.
- **Math** → `Unity.Mathematics` (`float2/3/4`, `int2/3/4`, `quaternion`, `half`, `math.*`) in
  anything per-frame, per-entity, in a job, or `[BurstCompile]`d.
  `Mathf` / `Vector3` / `Quaternion` stay at the Unity API boundary (Transform, physics, UI,
  serialized fields). Use `math.lengthsq`/`distancesq` when comparing, `math.saturate` for
  `Clamp01`, `math.rsqrt` for `1/sqrt`, `math.select` instead of a branch in a Burst inner loop.
- Before writing a one-off `IJob` over a native container, check `EncosyTower.Jobs` —
  `ListJobs`, `HashMapJobs`, `HashSetJobs`, `QueueJobs`, `BitArrayJobs`, `ArrayMapNativeJobs`.

Plain `List<T>` / `Dictionary<K,V>` / `Mathf` remain correct for startup code, small fixed sets,
serialized fields, and public contracts where `IReadOnlyList<T>` is the honest type. Use them there
without wrapping — the rule is "hot paths use the fast types", not "never write `List<T>`".

Full selection tables, safety rules for the native/shared views, and the `Mathf` → `math` mapping:
`references/collections-and-math.md`.

## Structure & naming defaults (before creating any file or folder)

Project code mirrors EncosyTower's organization. Decide placement *before* writing, not after.

- **Assembly = feature boundary.** Assembly name, `rootNamespace`, and folder name are the same
  string. Editor/authoring/test surfaces are **sibling assemblies** with dotted suffixes
  (`X.Editor`, `X.Authoring`, `X.Tests`), never subfolders of the runtime assembly.
- **Top-level folder per module**, named after the concept. Dotted (`Pooling.Native`,
  `Collections.Extensions`) when the sub-area has its own namespace, gate, or audience.
- **Reuse the sub-folder vocabulary** — `Contracts/`, `Annotations/`, `Internals/`, `SourceGen/`,
  `Extensions/`, `APIs/`, `Common/`, `Converters/`, `Generators/`, `Components/`, `Storage/`,
  `Debugging/`, `Views/`, `StyleSheets/`. Never `Interfaces/`, `Utils/`, `Misc/`, `Scripts/`.
  A module under ~6 files stays flat.
- **Folders organize; namespaces serve the consumer — they are decoupled.** A sub-folder does *not*
  add a namespace segment. A module's public surface lives in **one flat namespace** so consumers
  write one `using`. Only `Internals` / `Unsafe` / `Extensions` / `SourceGen` / `Editor.*` /
  `Caches` / `Generators` / `Debugging` earn their own segment.
  (`PubSub/Contracts/` + `PubSub/Publishers/` → `EncosyTower.PubSub`; `PubSub/Internals/` →
  `EncosyTower.PubSub.Internals`.)
- **File naming**: `Type.cs`; `Type+Aspect.cs` for a partial split; `Type+Nested.cs` for a nested
  type; `` Type`N.cs `` for generics; `Type_Async.cs` / `Type_Backend.cs` for split halves;
  `*.gen.cs` for generated (never hand-edit). One primary type per file; **one extension class per
  file**. Moving files must carry their `.meta`.
- **Naming**: `_camelCase` private fields, `s_camelCase` private statics, `ALL_UPPER` consts,
  PascalCase public readonly/static fields. `Encosy<Type>Extensions` for foreign types,
  `<Type>Extensions` for your own. `Async` suffix, `Attribute` suffix, `API` suffix for static
  facades, `Global*` for global singletons. `Try*` → `bool`+`out` or `Option<T>`;
  `*OrError` → `Result<T,Error>`; `*OrThrow`; `*OrDefault`.
- **Usings outside the namespace block**, alphabetical in three groups with no blank line between
  them: `System*`, then `EncosyTower*`/own roots, then `Unity*`. Block-scoped namespaces in
  production code; file-scoped only in tests and samples. Conditional aliases go *inside* the block.
- **Member order**: fields → constructors → indexers → properties → operators → methods → nested
  types; within each, `const` → `static readonly` → `static` → instance, then `public` →
  `protected` → `private`. On `MonoBehaviour`/`ScriptableObject`, serialized fields come first.

Full rules, the folder→namespace evidence table, and a worked layout for `Game.Gameplay`:
`references/structure-and-naming.md`.

## Hard rules

- **Never use a flat `enum` as `TError` in `Result<T, TError>`.** Model gameplay/domain failures with
  a `[PolyEnumFactoryFor]` wrapper over a `[PolyEnumStruct]` case union: an `Undefined` case,
  `FixedString512Bytes` messages, `Prefix(...)`, `ToFixedString()`, typed immutable payloads. Call
  sites use generated factories — `MachineError.UnknownState(ordinal)`, with parentheses. Reference
  implementation: `ApexionGame.Core/HFSM/MachineError.cs`. Audit every touched `*Error` type before
  handoff. Full shape and checklist: `references/structured-errors.md`.
- Any type carrying an EncosyTower source-gen attribute **must be `partial`**. `[ObservableProperty]`
  properties must have `get => Get_X(); set => Set_X(value);` bodies — the generator supplies both,
  plus the `_x` backing field.
- PubSub messages **must implement `IMessage`** (strict mode is the default).
  Processing requests **must implement `IRequest` / `IRequest<TResult>` / `IAsyncRequest<…>`**.
- Never `using UniTask` / `using UnityEngine.Awaitable` directly in gameplay code. Follow the
  package's alias pattern (`references/recipes.md` → "Async").
- Anything under `PubSub`, `Processing`, `PageFlows`, `Tasks`, `Vaults` async APIs is gated behind
  `#if UNITASK || UNITY_6000_0_OR_NEWER`. Both hold here, but keep the guard when adding files
  that mirror package structure.
- Follow `CODING-CONVENTIONS.md` at the repo root — it is EncosyTower's own convention doc and the
  house style for this repo. Summary in `references/setup.md`.

## References (read on demand)

- `references/first-party-modules.md` — **read before the module map.** The studio's own layers:
  the HFSM in `ApexionGame.Core`, the DOTS-free stats fork, the assembly inventory, and the current
  (empty) state of `Assets/Game/`.
- `references/module-map.md` — every module, its assembly, gate, and what it actually contains.
- `references/recipes.md` — verified copy-ready patterns for PubSub, Processing, Vaults, Pooling,
  PageFlows, MVVM, Databases, Persistences, Keys, Settings, Logging, source-gen attributes.
- `references/structured-errors.md` — the `Result<T,TError>` error contract: PolyEnum shape, case &
  payload rules, generated factories, the pre-handoff audit. **Read before touching any `*Error`.**
- `references/collections-and-math.md` — which collection and which math API to reach for, the
  managed↔native `Shared*` bridge, native/unsafe tiers and their safety rules.
- `references/structure-and-naming.md` — assembly boundaries, module folders, the sub-folder
  vocabulary, the folder→namespace rule, file naming, member ordering.
- `references/ui-toolkit.md` — **read before any UI work.** Why UI is C# and not UXML, the
  `Views/`+`StyleSheets/` layout, View/ViewController split, USS class-name constants, stylesheet
  loading, runtime `UIDocument` panels, theme/widget classes, elements to reuse.
- `references/planning-workflow.md` — the four phases: which skills to load, how to run the
  clarifying-question round, what makes a plan autonomously executable, how to execute it.
- `references/feature-docs.md` — `<Topic> - <Aspect>.md` naming, aspect vocabulary, required
  sections, bilingual rules, the file template.
- `references/setup.md` — which modules are live in *this* project right now, define symbols,
  asmdef wiring, source-generator gotchas, coding conventions digest.

Samples are the best ground truth: `Packages/com.laicasaane.encosy-tower/Samples~/`
(Data, MonoPages, Mvvm, Persistence, Pooling, PubSub, Stats, VisualDebugging).

## See also — the portable `midcore-*` skills

This skill owns **which module, which type, which name, which file**. The `midcore-*` set owns the
scale concerns layered on top, and reads `.claude/project-profile.md` to find its way back here.

| Load instead / as well when the question is | Skill |
|---|---|
| Where an assembly goes, which way it may point, how to break a cycle | `midcore-assembly-architecture` |
| Content table and id design, import validation, cross-table integrity | `midcore-data-pipeline` |
| Changing a type that is persisted to a player's device | `midcore-save-migration` |
| What is worth testing, and what counts as verified | `midcore-testing` |
| Frame/memory budgets, profiling, and evidence for a performance claim | `midcore-perf-budget` |
| Release gates, build reproducibility, symbols | `midcore-release-pipeline` |
| Remote config, events, telemetry, kill switches | `midcore-live-ops` |

And the craft layer, all project-level:

| The question | Skill |
|---|---|
| **How this feature is shaped — decomposition, state ownership, communication, performance tier** | **`system-design` — load it on EVERY feature request, alongside this one** |
| How the C# is written — formatting, API design, attributes, comments | `coding-standards` (it owns `CODING-CONVENTIONS.md`) |
| Changing the structure of code that already works | `refactoring` |
| Working out why something is broken | `debugging` |

`system-design` overlaps this skill's triggers deliberately. Split: **this skill answers *which
module*; `system-design` answers *what shape*.** Picking the wrong module is fixable; splitting state
wrongly is a rewrite.

They deliberately do not name EncosyTower — they defer here through the profile's
`authority_skills.structure_naming`. So a `midcore-*` skill saying "use the project's typed-id
mechanism" means `[WrapType]`/`[WrapRecord]` from this file's matrix.
