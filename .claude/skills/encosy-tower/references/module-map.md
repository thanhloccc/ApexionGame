# EncosyTower module map

Package root: `Packages/com.laicasaane.encosy-tower` — `com.laicasaane.encosy-tower` v0.1.7-preview.3, Unity 6000.3.

## Assemblies

| Assembly | Path | Gate |
|---|---|---|
| `EncosyTower.Core` | `EncosyTower.Core/` | always (auto-referenced) |
| `EncosyTower.Core.Extended` | `EncosyTower.Core.Extended/` | always — VisualDebugging lives here |
| `EncosyTower.Mvvm` | `EncosyTower.Mvvm/` | always — MonoView/MonoBinder/adapters |
| `EncosyTower.Editor` | `EncosyTower.Editor/` | editor only |
| `EncosyTower.Editor.Mvvm` | `EncosyTower.Editor.Mvvm/` | editor only |
| `EncosyTower.Databases.Authoring` | `EncosyTower.Databases.Authoring/` | `UNITY_EDITOR \|\| ENCOSY_INCLUDE_AUTHORING` **and** `BAKING_SHEET` **and** `UNITY_NEWTONSOFT_JSON` |
| `EncosyTower.Databases.Settings` | `EncosyTower.Databases.Settings/` | editor — the database import window |
| `EncosyTower.Entities.Stats` | `EncosyTower.Entities.Stats/` | `UNITY_ENTITIES` (**not installed here** — see setup.md) |
| `EncosyTower.Testing` | `EncosyTower.Testing/` | test utilities |

Source generators ship as DLLs in `EncosyTower.Core/SourceGenerators/` (Generators, Analyzers,
CodeRefactors, Formatters, Common, Helpers). They run on every assembly unless that assembly
declares `[SkipSourceGeneratorsForAssembly]`.

---

## EncosyTower.Core

### PubSub — `EncosyTower.PubSub`
Type-scoped and instance-scoped message bus, sync + async, with interceptors.

- Entry: `GlobalMessenger.Publisher` / `.Subscriber` / `.Interceptors`; or your own `Messenger`.
- Scopes: `.Global()`, `.Scope<TScope>()`, `.Scope(instance)`, `.UnityScope(unityObject)`
  (auto-unsubscribes with the object's lifetime).
- `Publisher<TScope>.Publish<T>(msg, PublishingContext)` / `PublishAsync`.
- `CachedPublisher<TScope,TMessage>` — resolve the broker once, publish in a hot loop.
- `Subscriber<TScope>.Subscribe<T>(handler)` → `ISubscription`; `.WithState(state)` for static
  handlers (no closure alloc); `.WithSubscriptions(List<ISubscription>)` to collect for bulk
  `Unsubscribe()` via `SubscriptionCollectionExtensions`.
- `PublishingContext.WaitForSubscriber(logger, token)` / `.DropIfNoSubscriber(...)` —
  see `PublishingStrategy`.
- `IMessageInterceptor<T>` / `IScopedMessageInterceptor` — middleware around publish.
- Contracts: `IMessage` (required in strict mode), `ISubscription`.
- Defines: `ENCOSY_PUBSUB_RELAX_MODE` (drop the `IMessage` requirement),
  `ENCOSY_PUBSUB_RUNTIME_CHECKS`.

### Processing — `EncosyTower.Processing`
Single-handler request/response ("mediator"), sync + async, with or without result.

- `GlobalProcessor.Instance` or your own `Processor`.
- `ProcessHub<TScope>`: `Register<TReq>(Action)`, `Register<TReq,TRes>(Func)`, `Unregister`,
  `Process`, `TryProcess` (returns `Option<TResult>`), `ContainsHandler`.
- Contracts: `IRequest`, `IRequest<TResult>`, `IAsyncRequest`, `IAsyncRequest<TResult>`.
- `ProcessRegistry` + `RegistryCollectionExtensions` for bulk unregister.
- Define: `ENCOSY_PROCESSING_RELAX_MODE`, `ENCOSY_PROCESSING_RUNTIME_CHECKS`.

### Vaults — `EncosyTower.Vaults`
- `ObjectVault<TId>` / `GlobalObjectVault` — store objects keyed by `Id<T>`, `Id2`, `StringId<T>`.
  Async: `WaitUntilContains(id, token)`, `TryGetAsync(id, context, token)`.
- `ValueVault<TId,TValue>` / `GlobalValueVault<TValue>` — same for unmanaged values.
- `SingletonVault<TBase>` / `GlobalSingletonVault` — one instance per concrete type
  (`TryAdd<T>()`, `TryGetOrAdd<T>(out T)`).

### Pooling — `EncosyTower.Pooling`, `.Pooling.Native`
- `GameObjectPrefab` + `GameObjectPool` — prefab-driven; `Prepool`, `Rent*`/`Return` over single
  values, `Span<>`, `List<>`; `RentingStrategy` / `ReturningStrategy` control activate/parent/scene.
- `SceneObjectPool` — pools a source GameObject inside a dedicated `Scene`.
- `SceneObjectPoolBehaviour<TKey>` / `<TKey,TValue>` — MonoBehaviour base that also maintains
  `TransformAccessArray`, `NativeList<float3>` positions/scales, `NativeList<quaternion>` rotations,
  so pooled instances feed straight into jobs.
- `NativeGameObjectPool` + `NativePrefabInfo` + `GameObjectInfo` — Burst/job bulk instantiate via
  `NativeRentingOptions` / `NativeReturningOptions` (needs `UNITY_COLLECTIONS`).
- Collection pools: `FasterListPool<T>`, `ArrayMapPool<K,V>`, `QueuePool<T>`, `StackPool<T>`,
  `StringBuilderPool`, `SimpleConcurrentPool<T>`.

### PageFlows — `EncosyTower.PageFlows`, `.PageFlows.MonoPages`
UI screen/popup orchestration driven by PubSub messages.

- Flow strategies: `SinglePageStack`, `MultiPageStack`, `SinglePageList`, `MultiPageList`
  (`MonoPageFlowKind`). Mono equivalents in `.MonoPages/Flows`.
- `MonoPageCodex` (MonoBehaviour) owns N `MonoPageFlow`s; wired by a component implementing
  `IMonoPageCodexOnInitialize` that returns a `PageFlowScopeCollectionApplier<TScopes>`.
- `IPageFlowScopeCollection` — a struct of named `PageFlowScope` properties (Screen, Popup, …).
- Page base: `MonoPageBase`, `MonoPageBase<TScopes>` (exposes `Publisher`, `Subscriber`,
  `FlowScopeCollection`).
- Opt-in page lifecycle: `IPageOnCreateAsync`, `IPageOnReturnToPool`, `IPageOnAttachToFlowAsync`,
  `IPageOnDetachFromFlowAsync`, `IPageHasOptions`, `IPageHasTransition`, `IPageNeedsFlowScope`,
  `IPageNeedsMessagePublisher`, `IPageNeedsMessageSubscriber`, `IPageNeedsFlowScopeCollection<T>`.
- Messages (`.MonoPages/PubSub`): `PrepoolPageAsyncMessage(assetKey, amount)`,
  `TrimPoolMessage(assetKey, amountToKeep)`, `AddPageAsyncMessage(assetKey, ctx)`,
  `ShowPageAsyncMessage(assetKey, ctx)`, `ShowPageAtIndexAsyncMessage(index, ctx)`,
  `HidePageAtIndexAsyncMessage(index, ctx)`, `HideActivePageAsyncMessage(ctx)`.
- Notifications (`PageFlows/PubSub`): `AttachPageMessage`, `DetachPageMessage`,
  `BeginTransitionMessage`, `EndTransitionMessage`.
- `PageContext` — `ShowOptions`/`HideOptions` (`PageTransitionOptions`: `NoTransition`,
  `ZeroDuration`, `OnlyFirstPageHasDuration`, `OnlyLastPageHasDuration`), `AsyncOperation`,
  `ReturnOperation`, `DefaultIndex`, `SequentialTransition`, `UserData`.
- Transitions: `MonoPageTransitionCanvasGroup`, `MonoPageTransitionGraphic`,
  `MonoPageTransitionPlayableDirector`, `MonoPageTransitionCollection`.
- Loading: `MonoPageLoaderStrategy` (Resources / Addressables), pooled by `MonoPagePool`.
- Project Settings page: `MonoPageFlowSettings` (`EncosyTower.Editor` provider).
- Define: `ENCOSY_PAGE_FLOW_PUBSUB_INCLUDE_CALLER_INFO[_DEV]` (already on for
  Standalone/Android/iOS here) — stamps caller file/line into page messages.

### Mvvm — `EncosyTower.Mvvm.*` (Core defines the contracts; `EncosyTower.Mvvm` the Unity components)
- ComponentModel: `[ObservableObject]`, `[ObservableProperty]`, `[NotifyPropertyChangedFor]`,
  `[NotifyCanExecuteChangedFor]`, `IObservableObject`, `PropertyChangeEventListener`.
- Input: `[RelayCommand]`, `IRelayCommand`, `RelayCommand`, `RelayCommand<T>`, `ICommandListener`.
- ViewBinding contracts: `IBinder`, `IAdapter`, `IBindingContext`, `BindingProperty`,
  `BindingCommand`, `Converter`, `[Adapter(sourceType, destType, order)]`, `[Binder]`.
- Components: `MonoView` (holds binders + context), `MonoBinder` / `MonoBinder<T>`, `MonoBinding`,
  `MonoViewSettings`, `[MonoBinder(typeof(Target))]`, `[MonoBindingProperty(name)]`,
  `[MonoBindingCommand(name, WrapperType=…)]`.
- Contexts: `UnityObjectBindingContext`.
- Built-in adapters: Any↔Bool/Int/Long/UInt/ULong/Float/Double/String, `SequentialAdapter`,
  `ScriptableAdapter`, `ResourcesAdapter`, Addressables adapters, `L10nKeyToString`,
  atlased-sprite adapters.
- Custom editor UI in `EncosyTower.Editor.Mvvm`.
- Define: `ENCOSY_MVVM_ADAPTERS_GENERATOR` (regenerate the `.gen.cs` adapter tables).

### Variants — `EncosyTower.Variants`
Fixed-size union value used by ViewBinding to move data without boxing.
`Variant`, `Variant<T>`, `VariantBase`, `[Variant(typeof(T))]` to register a custom type,
`IVariantConverter`, `VariantConverter<T>`, `VariantTypeKind`.
Editor: `VariantTypeSettings` Project Settings page. Define: `ENCOSY_VARIANTS_GENERATOR`.

### Data / Databases — `EncosyTower.Data`, `EncosyTower.Databases`, `.Databases.Authoring`
- `[Data]` on a `partial struct`/`class`; members declared as `[DataProperty] public readonly T X => Get_X();`
  → generator emits `_x` field, getter, comparer, `IData` / `IDataWithId<TId>`.
  `[DataMutable(DataMutableOptions.WithReadOnlyView)]` adds a mutable API + read-only view.
  `[DataWithoutId]`, `[DataComparer]`, `[DataFieldPolicy]`, `[PropertyType]`.
  `[DataProperty(typeof(TAuthoring))]` / `[DataProperty(typeof(TSerialized), typeof(TConverter))]`
  map an authoring/serialized representation onto a runtime type.
  `[DataManualAuthoring(typeof(T))]` adds a designer-facing column.
- `[DataTableAsset]` on `sealed partial class X : DataTableAssetBase<TId, TData>` →
  a ScriptableObject table; `.Entries` is a `ReadOnlyMemory<TData>`; `DataEntry` / `DataEntryRef`.
- `[Database(NameCasing.SnakeLower, AssetName = "…")]` on a `readonly partial struct`, with
  `[Table] public readonly XTableAsset X => Get_X();` per table. Construct at runtime with
  `new MyDatabase(databaseAsset, InitializationBehaviour.Forced)`.
- Authoring (editor): `[AuthorDatabase(typeof(MyDatabase), converterTypes…)]` on a
  `readonly partial struct` → generates BakingSheet `SheetContainer` + `*DataSheet` partials you
  can hook via `OnBeforePreprocess` / `OnPreprocess` / `OnProcess`.
  `[Horizontal]`, `[ConverterForDataProperty]`, `[ConverterForTable]`, `[DataAuthoringConverter]`.
- `EncosyTower.Databases.Settings` is the editor window that imports from
  Google Sheets / Excel / CSV / JSON into `DatabaseAsset`.

### Persistences — `EncosyTower.Persistences`
- `[Persistence]` on a `static partial class` → generates nested `Persistence`, `PersistDirectory`,
  `ReadOnlyPersistence`, `ReadOnlyAccessorCollection`, `StringIdCollection`.
  You implement `PersistDirectory.GetStoreArgs<TData,TStore>(createFunc)` and optionally
  `GetIgnoreEncryption(ref bool)`.
- `[Persist]` on the serializable data class (gets `Id`, `Version` plumbing).
- `[PersistAccessor(typeof(MyPersistence))]` on a class implementing `IPersistAccessor` — the
  gameplay-facing API over one data blob; call `_store.MarkDirty()` on every mutation.
- Storage: `PersistStoreDefault<TData>` over `PersistSourceDevice<TData>`
  (`RootPath`, `SerializeFunc`, `DeserializeFunc`, `FileExtension`, `MakeFilePathFunc`).
  Extend `PersistSourceBase<TData>` for a remote/cloud source.
- `SourcePriority` (`OnlyDevice`, …) and `SaveDestination` (`Device`, …) select which source wins.
- Encryption plugs in via `EncryptionBase` (`AesEncryption`, `RijndaelEncryption`, `EncryptionHash`,
  `RandomStringGenerator`) — pass a no-op implementation to disable.

### Keys & loading
- `AssetKey` / `AssetKey<T>` — a validated string key, `[Serializable]`, with a `TypeConverter`.
- `AddressableKey` / `AddressableKey<T>` (+ `AddressableKeyExtensions[_Async]`):
  `Load<T>()`, `TryLoad<T>()` → `Option<T>`, `LoadOrError<T>()` → `Result<T,Error>`,
  `LoadGetHandle<T>()` → `ValueHandlePair<T>`, `Instantiate*`/`TryInstantiate*` (GameObject or
  component), scene loading extensions. `AddressableKeyError` enumerates failures.
- `ResourceKey` / `ResourceKey<T>` — same shape over `Resources`; `InstancedAndPrefab`.
- `AtlasedSpriteKey` — `(atlas, sprite)` pair, Resources and Addressables variants.
- `ConfigKey` / `ConfigKey<T>` — typed PlayerPrefs key: `HasPlayerPref()`, `GetPlayerPref()` →
  `Option<T>`, `SetPlayerPref(value)`; also `StringIdPlayerPrefExtensions`.
- `SceneBuildIndex` (+ async extensions, editor API) — scene by build index, not by magic string.
- `Loaders` contracts: `ILoad<T>`, `ILoadAsync<T>`, `ITryLoad<T>`, `ITryLoadAsync<T>`,
  `ILoadOrError<T>`, `ILoadOrErrorAsync<T>` — implement these for your own loaders.

### Localization — `EncosyTower.Localization` (needs `UNITY_LOCALIZATION`)
`L10n.Initialize`, `IsReady()`, `SetLocale(code)`, `GetActiveLocales(collection)`,
`SaveSelectedLanguage(onSave)`; `L10nKey` / `L10nKey<T>` wrap `TableReference` +
`TableEntryReference` with sync/async lookup extensions; `L10nLanguage`,
`[L10nLanguageEnumTemplate]` to generate a language enum from the project's locales.

### Settings — `EncosyTower.Settings`
`Settings<T> : ScriptableObject` with `[Settings(SettingsUsage.RuntimeProject | EditorProject |
EditorUser, "Display/Path", filename)]`. `T.Instance` auto-creates and loads.
Nested `SubSettings` for grouped sections. Editor side auto-registers a Project Settings provider
(`ScriptableObjectSettingsProvider`, `SettingsEditor`).

### Logging — `EncosyTower.Logging`
`ILogger` / `IUnityLogger` / `IFixedLogger` (Burst-safe `FixedString`).
`Logger.Default`, `DevLogger.Default` (stripped outside dev builds), `SlimLogger` / `SlimDevLogger`
(no stack trace), `StaticLogger` / `StaticDevLogger` (static facade, has `*Slim` variants),
`UnityObjectLogger` (context-bound), `StringBuilderLogger` (in-memory, `OnLogEntryWritten` — use
for an on-screen console). `CallerInfo` / `BurstCallerInfo`, `LogEnvironment`.
Define: `ENCOSY_DISABLE_LOG_INFO`.

### Common — `EncosyTower.Common`
`Option<T>` (`Some`, `SomeIf`, `TryGetValue`, `GetValueOrDefault`, `GetValueOrThrow`),
`Result<T>` / `Result<T,TError>` (`IsSuccess`, `IsError`, `TryGetValue`, `GetErrorOrDefault`,
deconstruct, implicit conversions), `Error` / `Error<T>`, `Success`, `StringOrException`.
`Bool<T>`, `ByteBool`, `ByteBools`, `Scoped<TScope,TValue>`, `SerializableGuid`, `DateTimeId`,
`DelegateId`, `HashValue` / `HashValue64`, `StringEnum<TEnum>` / `StringEnum<TEnum,TConverter>`,
`T<T>` and `Type<T>` (compile-time type ids), `ValueRef<T>`, `AutoDisposeManager`, `GlobalScope`.
Marker interfaces: `IIsValid`, `IIsCreated`, `IIsDefined`, `IHasValue`, `IGetHashCode64`.
String/object/DateTime/StringBuilder extensions (`IsEmpty()`, `IsNotEmpty()`,
`IsEmptyOrWhiteSpace()`, `IsInvalid()` for Unity objects…).

### Ids / StringIds / Types
- `Id` (uint), `Id2`, `Id3`, `Id<T>` — value handles with format/parse support.
- `StringId`, `StringId<T>`, `StringHash`, `MetaStringId`; `StringVault` (+`ReadOnly`,
  `Enumerator`), `StringVaultNative`, `StringVaultUnsafe`, `GlobalStringVault`,
  `UnmanagedString` / `UnmanagedStringSpan` (Burst-safe strings), `IStringVault`, `StringIdAPI`.
  Define: `ENCOSY_CLEAR_GLOBAL_STRING_VAULT_ON_ENTER_PLAY_MODE`.
- `TypeId`, `TypeId<T>`, `TypeInfo`, `TypeInfo<T>`, `TypeHash`, `TypeIdVault`, `TypeRelation`,
  `RuntimeTypeCache` (`[Cache…]` attributes; editor builds a serialized cache asset so reflection
  works under IL2CPP), `LinkXmlTypeStore`.

### Collections & memory
- Managed: `FasterList<T>`, `ListFast<T>`, `ArrayMap<K,V>`, `ArraySet<T>`, `SharedArray<T>`,
  `SharedList<T>`, `SharedArrayMap<K,V>`, `SharedStack<T>`, `SharedQueue<T>`, `SharedReference<T>`,
  `ListProxy<T>`, `DictionaryReadOnly`, `HashSetReadOnly` — most have a `.ReadOnly` view struct.
- Native: `ListNative<T>`, `QueueNative<T>`, `StackNative<T>`, `ArrayMapNative<K,V>`,
  `ArraySetNative<T>`, `SharedListNative<T>`, `SharedArrayMapNative<K,V>`, `SharedStackNative<T>`,
  `SharedQueueNative<T>`, `ReferenceNative<T>`, `NativeSliceReadOnly<T>`.
- Contracts in `Collections.Contracts` (`IClearable`, `IAsSpan`, `ITryGetValue`, `IResizable`, …)
  — implement these so the generic extension methods apply to your own containers.
- Extensions: `Collections.Extensions[.Unsafe]`, `Unity.Collections.Extensions[.Unsafe]`,
  `Collections.Unsafe` (`EncosyMemoryAPI`, `EncosyCollectionSafetyAPI`).
- `Buffers`: `IBuffer`, `IAlloc`, `IBufferProvider`, `BufferManaged`, `BufferNative`,
  `BufferUnsafe`, `AllocatorStrategy`, `BufferProviderEnumerator`.
- Define: `ENCOSY_COLLECTIONS_RUNTIME_CHECKS`.

### Source-generated type tools
- `[WrapType(typeof(T), "_field")]` / `[WrapRecord]` — newtype over an existing type, with
  operators, equality, `TypeConverter`. `IWrap<T>`, `IWrapper`.
- `[UnionId(Size = UnionIdSize.ULong, KindSettings = …)]` + `[UnionIdKind(typeof(T), order, "Name",
  signed: bool)]` — one struct that is any of N id kinds, with `Kind`, `IdSigned`/`IdUnsigned`,
  `TryParse`, string round-trip. `UnionIdKindSettings.PreserveOrder | RemoveSuffix`.
- `[EnumExtensions]` on an enum, or `[EnumExtensionsFor(typeof(E))] static partial class` →
  `ToStringFast()`, `TryParse`, `IsDefined`, `ToIndex`, flag helpers, `IEnumBitField`.
- `[EnumTemplate]` on `X_EnumTemplate` + `[EnumTemplateMembersFromEnum(typeof(Other), offset)]` →
  generates enum `X` merged from several sources. Used to keep modular content enums in sync.
- `[PolyEnumStruct]` — discriminated union of nested structs with a generated `IEnumCase`
  interface, conversions, and optional `SortFieldsBySize` / `AutoEquatable` / `WithEnumExtensions`.
- `[SkipSourceGeneratorsForAssembly]` / `[AllowSourceGeneratorsForAssembly]` — per-assembly opt out.
- `CodeGen`: `Printer`, `CodeGenAPI`, `[ThisFilePath]` — for writing your own generators.
- Annotations: `[Label(name, directory)]`, `[Asset]`, `[ApiForAuthoring]`, `[ApiForEditor]`,
  `[RemoveFromDocs]`.

### Misc Core
- `Tasks`: `UnityTasks.GetCompleted()`, `GetCompleted<T>(value)`, `NextFrameAsync(token)`,
  `WaitUntil(...)`, `WaitWhile(...)`, `WhenAll(...)`, `.Forget()`, `.AsUnityTask()`,
  `.SuppressCancellationThrow()` — one API over UniTask and `Awaitable`.
- `Initialization`: `IInitializable`, `IDeinitializable`, `IIsInitialized`,
  `InitializationBehaviour` (`Forced`, …).
- `Debugging`: `Checks.IsTrue/IndexInRange/...` (conditional), `ThrowHelper`,
  `ValidationDefines` (`UNITY_EDITOR`, `DEBUG`, `RUNTIME_CHECKS` constants for
  `[Conditional]` guards).
- `Search`: `FuzzySearchAPI.Search(...)` over FuzzySharp (needs `org.nuget.raffinert.fuzzysharp`).
- `Serialization`: `ParsableStructConverter<T>` (TypeConverter base for parsable structs);
  `Serialization.NewtonsoftJson`: `JsonHelper.TrySerialize/TryDeserialize`,
  `[NewtonsoftJsonAotHelper]`.
- `IO`: `PathAPI`, `RootPath`, memory-stream extensions.
- `Naming`: `NameCasing`, `NamingPolicy` (camel / Pascal / snake_lower / SNAKE_UPPER /
  kebab-lower / KEBAB-UPPER / JSON separator).
- `Conversion` contracts: `ITryConvert`, `ITryParse<T>`, `IToStringFast`, `IToFixedString`,
  `IToDisplayString`, `IToIndex`, `ITransform`, `TransformFunc`.
- `Encryption`: `EncryptionBase` (`Encrypt`/`Decrypt`), `AesEncryption`, `RijndaelEncryption`,
  `EncryptionHash`, `RandomStringGenerator`.
- `UIElements` (runtime + editor): `SafeArea`, `SimpleTableView`, `EnableableFoldout`,
  `ButtonTextField`, `SerializableGuidField`, `SerializableSortingLayerField`, `VisualSeparator`,
  `ButtonAPI`, `AbstractGenericMenu`, extension methods.
- `Jobs`: `ListJobs`, `HashMapJobs`, `HashSetJobs`, `QueueJobs`, `BitArrayJobs`,
  `ArrayMapNativeJobs`, `EncosyIJobParallelForTransformExtensions`.
- `Entities` (`UNITY_ENTITIES` only): `[Lookup]`, `[TypeHandle]`, `[ISystem]`, `ISystemUpdates`,
  baker/blob/EntityManager extensions, Latios/Psyshock helpers.
- `PolyEnumStructs`, `UnionIds`, `TypeWraps`, `Variants`, `EnumExtensions` — see above.

---

## EncosyTower.Core.Extended — VisualDebugging
In-game command console. Write `[ObservableObject] [Label("Name","Category")] [VisualOrder(n)]
partial class X : IVisualCommand` with `[ObservableProperty]` fields and `[RelayCommand]` setters,
implement `Execute()`. Rendered by `VisualCommanderPage` / `VisualCommanderView` /
`VisualCommandView` / `VisualDirectoryView` / `VisualOptionsView` / `VisualPropertyView`,
bound through `VisualPropertyBindingAPI`. Editor twin: `EncosyTower.Editor/VisualDebugging`
(`VisualCommandWindow`).

---

## EncosyTower.Editor
`ProjectFeaturesWindow` (reads `[Feature]` + `[RequiresPackage]` structs and installs missing UPM
packages), `RequiredScriptingSymbols` + build preprocessor, `AssemblyDefinitionListWindow` /
`AssemblyReferenceWindow` / `AssemblyDefinitionAPI` / `AssemblyXmlDocumentationGenerator`,
`SceneListWindow`, `EmptyFolderListWindow`, `GenericMenuPopup` (+ window), `TreeViewPopup`,
`EditorIcons`, `EditorMenuItemPopulator`, `EncosyDebugLogLinkRouter`,
`EncosySerializedPropertyExtensions`, `LinkXmlGenerator`, `SerializableGuid` /
`SerializableSortingLayer` drawers & converters, `CSharpProjectGenerationPostprocessor`
(SDK-style `.csproj` — gated on `ENABLE_SDK_STYLE_PROJECTS`), `SpriteMultiModeDefaultPostprocessor`,
`UnityObjectTypes`, settings providers for MonoPageFlow / Variants / generic `Settings<T>`.

---

## EncosyTower.Entities.Stats (`UNITY_ENTITIES` — currently compiled out here)
DOTS stat system: `[StatCollection]`, `[StatData]`, `[StatSystem]`, `StatAccessor`, `StatReader`,
`StatBaker`, `StatJobs`, `StatWorldData`, `StatHandle`, `StatModifierHandle`, `StatModifierRecord`,
`StatVariant`, `IStat`, `IStatModifier`, `IStatModifierStack`, `IStatObserver`, `StatChangeEvent`,
`ModifierTriggerEvent`, `ObserverRange`.
**This project maintains its own DOTS-free fork** at
`Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats`
— see `setup.md`.
