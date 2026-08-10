# Folder structure, code separation, and naming — follow EncosyTower

Mirror the package's organization in project code. Rules below are derived from the package tree
and from `CODING-CONVENTIONS.md` §1, §3, §6, §7 at the repo root.

---

## 1. Assembly = feature boundary

One `.asmdef` per bounded feature. **Assembly name, `rootNamespace`, and the folder that holds it
are all the same string.**

```
EncosyTower.Core/EncosyTower.Core.asmdef      name = rootNamespace = "EncosyTower.Core"
EncosyTower.Mvvm/EncosyTower.Mvvm.asmdef      name = rootNamespace = "EncosyTower.Mvvm"
```

A feature that needs editor, authoring, or test surfaces splits into sibling assemblies with
dotted suffixes — never into subfolders of the runtime assembly:

```
X            X.Authoring       X.Editor       X.Tests       X.Samples
```

The package does this (`EncosyTower.Databases.Authoring`, `EncosyTower.Editor.Mvvm`,
`EncosyTower.Tests.PlayMode`) and so does this project
(`ApexionGame.Entities.Stats{,.Authoring,.Editor,.Tests,.Samples}`).

Editor code that is *too small* to justify its own assembly stays in the runtime assembly but must:
live in a folder prefixed `Editor`, be wrapped whole in `#if UNITY_EDITOR`, use an
`<Root>.Editor.*` namespace, and mark public APIs `[ApiForEditor]`. See
`EncosyTower.Core/Editor.ProjectSetup/`.

---

## 2. Top-level folders inside an assembly = modules

One folder per module, named after the concept. **Dotted when the folder is a namespace extension
of a sibling module:**

```
PubSub/            Processing/        Vaults/            Pooling/
Pooling.Native/    Collections/       Collections.Extensions/    Collections.Unsafe/
PageFlows/         PageFlows.MonoPages/                  Editor.ProjectSetup/
```

`Pooling.Native` is a separate folder rather than `Pooling/Native` because it is a separate
namespace with its own dependency (`UNITY_COLLECTIONS`). Use a dotted top-level folder whenever the
sub-area has a different gate, a different audience, or a different namespace.

---

## 3. Sub-folder vocabulary

Reuse these names — do not invent synonyms (`Interfaces/`, `Utils/`, `Misc/`, `Scripts/`).

| Folder | Holds |
|---|---|
| `Contracts/` | the interfaces the module is programmed against (`IMessage`, `IPage`, `IStatData`) |
| `Annotations/` | attributes the *user* applies (`[Table]`, `[ObservableProperty]`) |
| `Internals/` | implementation types a consumer never names |
| `SourceGen/` | attributes emitted by or consumed by the generators |
| `Extensions/` | extension classes |
| `APIs/` | static facades and entry points (`StatAPI`, `PathAPI`, `VariantAPI`) |
| `Common/` | small shared value types of the module |
| `Converters/` | conversion implementations |
| `Generators/` | codegen writers run by `com.annulusgames.unity-codegen` |
| `Components/` | MonoBehaviour / ECS components |
| `Storage/` | the module's data layout types |
| `Debugging/` | debug-only registries and inspectors |
| `Helpers/` | last resort, for genuinely uncategorised support code |
| `Views/`, `StyleSheets/` | UI Toolkit views and their USS |
| `Editor/` | editor-only code kept inside a runtime assembly |

Plus domain nouns when the module is large: `Publishers/`, `Subscribers/`, `Interceptors/`,
`Flows/`, `Context/`, `Transition/`, `Codex/`, `MonoPage/`, `Caches/`, `Lookups/`, `TypeHandles/`,
`DataTableAssets/`.

Modules with fewer than ~6 files stay flat — no sub-folders. `Ids/`, `Tasks/`, `Naming/`,
`Initialization/` are flat for exactly this reason.

---

## 4. Folders organize; namespaces serve the consumer — they are decoupled

**A sub-folder does NOT automatically add a namespace segment.** The public surface of a module
lives in **one flat namespace** so a consumer writes a single `using`.

| Folder | Namespace |
|---|---|
| `PubSub/Contracts/`, `PubSub/Publishers/`, `PubSub/Subscribers/` | `EncosyTower.PubSub` |
| `PubSub/Internals/` | `EncosyTower.PubSub.Internals` |
| `Databases/Annotations/`, `Databases/DataTableAssets/` | `EncosyTower.Databases` |
| `Data/Data/` | `EncosyTower.Data` |
| `Data/SourceGen/` | `EncosyTower.Data.SourceGen` |
| `Types/Caches/`, `Types/Internals/` | `EncosyTower.Types.Caches`, `.Internals` |
| `PageFlows/Contracts/`, `PageFlows/Internals/` | `EncosyTower.PageFlows` |
| `Mvvm/ViewBinding/Annotations/` | `EncosyTower.Mvvm.ViewBinding` |
| `Collections.Contracts/` | `EncosyTower.Collections` |
| `Collections.Unsafe/` | `EncosyTower.Collections.Unsafe` |

A folder earns its own namespace segment **only when it is a separate opt-in surface**:

- `.Internals` — implementation detail; a consumer importing it is doing something wrong.
- `.Unsafe` — opt-in danger, so the `using` is a visible decision.
- `.Extensions` — opt-in extension surface, so it does not pollute completion by default.
- `.SourceGen` — the generator contract, not the user API.
- `.Editor.*` — editor-only.
- `.Caches`, `.Generators`, `.Debugging` — tooling adjacent to the module, not part of it.

The project's own `ApexionGame.Entities.Stats` already follows this: `APIs/`, `Common/`,
`Contracts/`, `Storage/`, `Annotations/` all sit in the flat `ApexionGame.Entities.Stats`, while
`Debugging/` and `Generators/` take their own segment.

**Folders that mirror a foreign namespace hold types declared in that foreign namespace** —
polyfills and attributes that must resolve without an extra `using`. The folder name is the foreign
namespace, exactly:

```
System.Buffers/                                   -> namespace System.Buffers
System.Runtime.CompilerServices/                  -> namespace System.Runtime.CompilerServices
Unity.IL2CPP.CompilerServices/                    -> namespace Unity.IL2CPP.CompilerServices
UnityEngine.ResourceManagement.AsyncOperations/   -> namespace UnityEngine.ResourceManagement.AsyncOperations
```

Contrast extension folders, which are *named after what they extend* but declare types in
Encosy's own namespace: `UnityEngine.Extensions/` → `EncosyTower.UnityExtensions`,
`Unity.Collections.Extensions/` → `EncosyTower.Collections`.

---

## 5. File naming

| File content | Convention | Example |
|---|---|---|
| Single primary type | file name = type name | `ArrayMap.cs` |
| Partial split by aspect | `Type+Aspect.cs` | `ArrayMap+ReadOnly.cs` |
| Nested type split out | `Type+NestedType.cs` | `SharedList+Enumerator.cs` |
| Generic type | ``Type`N.cs`` | ``Id`1.cs``, ``StringEnum`2.cs``, ``MessageBroker`2.cs`` |
| Async half of a type | `Type_Async.cs` | ``ProcessHub`1_Async.cs``, `ObjectVault_Async.cs` |
| Backend-specific partial | `Type_Backend.cs` | `UnityTasks_UniTask.cs`, `UnityTasks_Awaitable.cs` |
| Generated | `*.gen.cs` — never hand-edit | `ByteBools.gen.cs` |

- **One primary type per file.** Small helpers may sit beside the type they serve.
- **One extension class per file** — `ListProxyExtensions.cs` and `ListProxyReadOnlyExtensions.cs`
  are separate files, not one file with two classes.
- Mark a type `partial` when split across files.
- A module folder keeps its own `AssemblyInfo.cs` when it needs assembly-level attributes
  (e.g. `[assembly: EncosyTower.Mvvm.SkipSourceGeneratorsForAssembly]`).
- Ported files keep the original license header at the very top plus a source URL comment —
  see `Collections/FasterList.cs`.
- **Unity `.meta` files**: moving or renaming a folder/file must go through the Unity Editor, or
  move the `.meta` alongside it. Orphaned `.meta` files break asset references.

---

## 6. Type and member naming

| What | Style | Example |
|---|---|---|
| Classes, structs, enums, delegates | PascalCase | `MessageBroker` |
| Interfaces | `I` + PascalCase | `IMessage`, `IPageFlow` |
| Methods, properties | PascalCase | `TryGetValue`, `IsValid` |
| Public/protected readonly & static fields | PascalCase | `Default`, `None`, `Value` |
| Public/protected mutable fields | camelCase | `useProjectSettings` |
| Private/internal fields | `_camelCase` | `_buffer`, `_count` |
| Private/internal static fields | `s_camelCase` | `s_instance`, `s_pool` |
| `const` | `ALL_UPPER` | `MAX_SIZE`, `PREFIX` |
| Locals, parameters | camelCase | `hintName`, `result` |
| `goto` labels | `ALL_UPPER` | `FAILED`, `DONE` |
| Native container fields | Unity's `m_PascalCase`, inside an `IDE1006` pragma pair | `m_Data`, `m_Safety` |

- Extensions over types **you do not own** (BCL, Unity): `Encosy<Type>Extensions` —
  `EncosyStringExtensions`, `EncosyGameObjectExtensions`.
- Extensions over **your own** types: `<Type>Extensions` — `AssetKeyExtensions`,
  `SubscriptionCollectionExtensions`.
- Async methods end in `Async`. Attribute classes end in `Attribute`.
- Static facades end in `API` (`StatAPI`, `PathAPI`), global singletons are prefixed `Global`
  (`GlobalMessenger`, `GlobalObjectVault`, `GlobalStringVault`), static logger facades are prefixed
  `Static` (`StaticLogger`, `StaticDevLogger`).
- `Try*` returns `bool` + `out`, or `Option<T>`. `*OrError` returns `Result<T, Error>`.
  `*OrThrow` throws. `*OrDefault` takes a fallback.

---

## 7. Usings and namespace blocks

- `using` directives **outside** the namespace block.
- Sorted alphabetically in three groups with **no blank line between them**:
  `System*`, then `EncosyTower*` (then your own roots), then `Unity*`.
- **Block-scoped** namespaces in production code (`namespace X { … }`). File-scoped
  (`namespace X;`) only in tests and samples.
- Conditional type aliases (`using UnityTask = …` under `#if`) go **inside** the namespace block.
- One file may hold several namespace blocks when `#if`-guarded partials of the same type must live
  together.

---

## 8. Member ordering inside a type

1. Fields → 2. Constructors (static first) → 3. Indexers → 4. Properties → 5. Operators →
6. Methods → 7. Nested types (interfaces, then static classes, then the rest).

Within each kind, sort by modifier (`const` → `static readonly` → `static` → instance), then by
visibility (`public` → `protected` → `private`). One blank line between groups, none inside a group.

For `MonoBehaviour` / `ScriptableObject`, serialized fields come first:
`public` fields → `[SerializeField] protected` → `[SerializeField] private` → then the order above.

---

## 9. Applying this to Land of Souls

Gameplay assemblies are `Assets/Game/Game.Common` and `Assets/Game/Game.Gameplay`; shared
project code is `Assets/ApexionGame/ApexionGame.Core`. Assembly name = `rootNamespace` = folder name
in each.

A new feature inside `Game.Gameplay` should look like:

```
Assets/Game/Game.Gameplay/
├── Game.Gameplay.asmdef              name = rootNamespace = "Game.Gameplay"
├── AssemblyInfo.cs                   only if assembly-level attributes are needed
├── Combat/                           -> namespace Game.Gameplay.Combat
│   ├── Contracts/IDamageable.cs      -> namespace Game.Gameplay.Combat  (flat)
│   ├── Annotations/                  -> namespace Game.Gameplay.Combat  (flat)
│   ├── Common/DamageKind.cs          -> namespace Game.Gameplay.Combat  (flat)
│   ├── Components/HealthBehaviour.cs -> namespace Game.Gameplay.Combat  (flat)
│   ├── Internals/DamageResolver.cs   -> namespace Game.Gameplay.Combat.Internals
│   ├── CombatAPI.cs
│   ├── CombatMessages.cs
│   └── Combat.Extensions/            -> namespace Game.Gameplay.Combat.Extensions
└── Inventory/                        -> namespace Game.Gameplay.Inventory
```

Checklist before creating files:

1. Does this belong in an existing module folder, or is it a new module? New module = new top-level
   folder under the assembly, named after the concept.
2. Does it need editor/authoring/test code? Those go in **sibling assemblies** with dotted suffixes.
3. Is the type part of the module's public surface? Then it goes in the module's **flat** namespace,
   whatever sub-folder it sits in. Only `Internals` / `Unsafe` / `Extensions` / `SourceGen` /
   `Editor` earn a namespace segment.
4. Pick the sub-folder from the vocabulary in §3 — do not invent a new one for a single file.
5. File name follows §5; if the type is generic or split, use the backtick / `+` / `_Async` form.
