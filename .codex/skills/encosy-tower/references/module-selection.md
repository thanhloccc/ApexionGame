# EncosyTower module selection

Use this as routing guidance, then inspect the actual package source and samples.

| Need | Module |
|---|---|
| Fire-and-forget events with zero or more listeners | PubSub |
| One-handler request/response | Processing |
| Service registry or cross-scene handoff | Vaults |
| GameObjects, scene templates, native bulk spawning, temp collections | Pooling |
| Screens, popups, navigation stacks and transitions | PageFlows.MonoPages |
| View-model binding and commands | MVVM and Variants |
| Spreadsheet-authored runtime tables | Data and Databases |
| Save/load, slots, versions and encryption | Persistences and Encryption |
| Addressables, Resources, sprites, scenes, typed PlayerPrefs | Keys and ConfigKeys |
| Localized text and locale switching | Localization |
| ScriptableObject-backed Project Settings | Settings |
| Runtime/dev/Burst/in-memory logging | Logging |
| Optional values or expected failures | Common `Option`/`Result` and PolyEnumStructs for `TError` |
| Strong IDs, interned strings, runtime type identity | Ids, StringIds and Types |
| Newtypes, union IDs, enum helpers, discriminated unions | TypeWraps, UnionIds, EnumExtensions and PolyEnumStructs |
| Managed/native/shared data structures and raw buffers | Collections and Buffers |
| Existing native container jobs | Jobs |
| In-game developer command console | VisualDebugging |
| Search, JSON, paths, naming conversions, guard clauses | Search, Serialization, IO, Naming and Debugging |
| Unified UniTask/Awaitable surface | Tasks |
| Editor windows, tables, menus and project feature setup | Editor tools and ProjectSetup |

Decision rules:

- Use PubSub for notifications; use Processing for a query/RPC with one result-producing owner.
- Prefer PubSub over a C# event across assemblies, scenes, or asynchronous boundaries.
- Use Vaults when lifecycle and wait-until-available semantics matter; use plain static state only for pure constants.
- Use Databases when designers or spreadsheets own structured data; a single hand-tuned asset may remain a plain ScriptableObject or Settings asset.
- Use `Option<T>` for absence and `Result<T,TError>` for an expected failure with a handled reason. Every gameplay/domain `TError` follows `structured-errors.md`; never use a flat enum. Reserve exceptions for programmer errors.
