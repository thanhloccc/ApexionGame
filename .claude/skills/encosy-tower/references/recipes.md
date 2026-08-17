# EncosyTower recipes

Patterns below are distilled from `Packages/com.laicasaane.encosy-tower/Samples~/` and the package
source. Grep the module folder before relying on an exact overload — this file covers shape, not
every signature.

---

## Async — the `UnityTask` alias pattern

Never reference `UniTask` or `Awaitable` directly. Every EncosyTower file that is async opens with:

```csharp
#if UNITASK || UNITY_6000_0_OR_NEWER

namespace Game.Whatever
{
#if UNITASK
    using UnityTask = Cysharp.Threading.Tasks.UniTask;
    using UnityTaskBool = Cysharp.Threading.Tasks.UniTask<bool>;
#else
    using UnityTask = UnityEngine.Awaitable;
    using UnityTaskBool = UnityEngine.Awaitable<bool>;
#endif

    // ... async members return UnityTask / UnityTaskBool
}

#endif
```

Alias one name per **returned type**, not just one per task. `UnityTask` is the void-ish task;
declare a further alias for each generic result you return, named after what it carries:

```csharp
#if UNITASK
    using UnityTask      = Cysharp.Threading.Tasks.UniTask;
    using UnityTaskBool  = Cysharp.Threading.Tasks.UniTask<bool>;
    using UnityTaskVault = Cysharp.Threading.Tasks.UniTask<PlayerPersistence.ReadOnlyPersistence>;
#else
    using UnityTask      = UnityEngine.Awaitable;
    using UnityTaskBool  = UnityEngine.Awaitable<bool>;
    using UnityTaskVault = UnityEngine.Awaitable<PlayerPersistence.ReadOnlyPersistence>;
#endif
```

That is where the `UnityTaskVault` in the Persistences recipe below comes from — it is not a package
type, just this file's alias for `UniTask<ReadOnlyPersistence>`. `Awaitable` has no implicit
conversions, so the alias is what keeps both backends compiling from one signature.

Helpers: `UnityTasks.GetCompleted()`, `UnityTasks.GetCompleted<T>(value)`,
`UnityTasks.NextFrameAsync(token)`, `UnityTasks.WaitUntil(state, predicate, token)`,
`UnityTasks.WhenAll(tasks, count)`, `task.Forget()`, `await task.SuppressCancellationThrow()`.
On a MonoBehaviour, use `destroyCancellationToken`.

---

## PubSub

```csharp
using EncosyTower.PubSub;

// 1. Message = readonly record struct implementing IMessage
public readonly record struct GoldChangedMsg(int Current, int Delta) : IMessage;

// 2. Subscribe — UnityScope ties lifetime to this Unity object
private readonly List<ISubscription> _subscriptions = new();

private void Subscribe()
{
    var subscriber = GlobalMessenger.Subscriber.UnityScope(this)
        .WithState(this)                        // static handler => no closure alloc
        .WithSubscriptions(_subscriptions);     // collect for bulk unsubscribe

    subscriber.Subscribe<GoldChangedMsg>(Handle);
    subscriber.Subscribe<GoldSpentMsg>(HandleAsync);
}

private static void Handle(HudView state, GoldChangedMsg msg, PublishingContext ctx)
    => state.SetGold(msg.Current);

private static async UnityTask HandleAsync(HudView state, GoldSpentMsg msg, PublishingContext ctx)
{
    await UnityTasks.NextFrameAsync(ctx.Token);
    state.PlaySpendVfx();
}

private void OnDisable() => _subscriptions.Unsubscribe();   // SubscriptionCollectionExtensions

// 3. Publish
var ctx = PublishingContext.DropIfNoSubscriber(logger: DevLogger.Default, token: destroyCancellationToken);
GlobalMessenger.Publisher.UnityScope(this).Publish(new GoldChangedMsg(100, +10), ctx);

// await every handler, and warn if nobody listens:
await GlobalMessenger.Publisher.Global()
    .PublishAsync(new GoldSpentMsg(10), PublishingContext.WaitForSubscriber(token: token));
```

Scope choices: `.Global()` (`GlobalScope`), `.Scope<TScope>()` (type as scope),
`.Scope(instance)`, `.UnityScope(unityObject)` (lifetime-aware).

Hot path — cache the broker once:

```csharp
private CachedPublisher<GlobalScope, TickMsg> _tick;
private void Awake() => _tick = GlobalMessenger.Publisher.GlobalCache<TickMsg>();
private void Update() => _tick.Publish(new TickMsg(Time.deltaTime));
private void OnDestroy() => _tick.Dispose();
```

Middleware:

```csharp
public sealed class GoldLogInterceptor : IMessageInterceptor<GoldChangedMsg>
{
    public UnityTask InterceptAsync(GoldChangedMsg msg, PublishingContext ctx,
        PublishContinuation<GoldChangedMsg> next)
    {
        DevLogger.Default.LogInfo($"gold -> {msg.Current}");
        return next(msg, ctx);
    }
}

GlobalMessenger.Interceptors.AddInterceptor(_interceptor);   // RemoveInterceptor to detach
```

---

## Processing (request → response)

```csharp
using EncosyTower.Processing;

public readonly record struct GetGoldRequest() : IRequest<int>;
public readonly record struct SpendGoldRequest(int Amount) : IRequest<bool>;

// register (once, e.g. in the system that owns the wallet)
private readonly List<ProcessRegistry> _registries = new();

var hub = GlobalProcessor.Instance.Global().WithRegistries(_registries);
hub.Register<GetGoldRequest, int>(_ => _gold);
hub.Register<SpendGoldRequest, bool>(TrySpend);

// call
var gold = GlobalProcessor.Instance.Global().Process<GetGoldRequest, int>(default);

// or tolerate a missing handler
if (GlobalProcessor.Instance.Global()
        .TryProcess<GetGoldRequest, int>(default).TryGetValue(out var value))
{
    // ...
}

_registries.Unregister();   // RegistryCollectionExtensions
```

---

## Vaults

```csharp
using EncosyTower.Vaults;
using EncosyTower.Ids;
using EncosyTower.Types;

// keyed by a type-derived id
GlobalObjectVault.TryAdd(Type<IInventoryService>.Id, service);

if (GlobalObjectVault.TryGet(Type<IInventoryService>.Id, out Option<IInventoryService> opt)
    && opt.TryGetValue(out var svc))
{
    svc.Use();
}

// wait for something published later in the boot sequence
var opt = await GlobalObjectVault.TryGetAsync(Type<ISaveService>.Id, this, token);

// unmanaged values
GlobalValueVault<int>.TrySet(Type<PlayerLevel>.Id, 12);

// one instance per concrete type
var vault = new SingletonVault<ISystem>();
vault.TryGetOrAdd<CombatSystem>(out var combat);
```

---

## Pooling

```csharp
using EncosyTower.Pooling;

// prefab pool
var pool = new GameObjectPool { Prefab = new GameObjectPrefab { Source = _bulletPrefab, Parent = _root } };
pool.Prepool(64);

var go = pool.RentGameObject(RentingStrategy.Default);
pool.Return(go, ReturningStrategy.Default);

// bulk into a span, no per-item call overhead
Span<GameObject> buffer = stackalloc GameObject[32];
pool.Rent(buffer, RentingStrategy.Default);

// scoped temp collections instead of `new List<T>()`
var list = FasterListPool<Enemy>.Get();
try { /* ... */ } finally { FasterListPool<Enemy>.Release(list); }

var sb = StringBuilderPool.Get();
```

Job-friendly bulk spawn (needs `UNITY_COLLECTIONS`) — see
`Samples~/EncosyTower.Samples.Pooling/NativeGameObjectPooler.cs`:

```csharp
var prefab = new NativePrefabInfo(_template.GetInstanceID(), float3.zero, quaternion.identity,
    new float3(1f), rentScene, returnScene);
_pool = new NativeGameObjectPool(prefab, capacity);
_pool.Rent(count, _result, NativeRentingOptions.Everything);
_pool.Return(_result.AsArray().Slice(start, length), NativeReturningOptions.Everything);
```

For pooled instances that must feed a job, derive from `SceneObjectPoolBehaviour<TKey>` — it keeps
`TransformArray` / `Positions` / `Rotations` / `Scales` in sync for you.

---

## PageFlows (UI screens & popups)

Three pieces: a scope collection, a codex initializer, and page components.

```csharp
// 1. Named flows
[Preserve]
public struct GamePageFlowScopes : IPageFlowScopeCollection
{
    [Preserve] public PageFlowScope Screen { get; set; }
    [Preserve] public PageFlowScope Popup { get; set; }
}

// 2. Sits next to MonoPageCodex on the same GameObject
public class GamePageCodex : MonoBehaviour, IMonoPageCodexOnInitialize
{
    private readonly PageFlowScopeCollectionApplier<GamePageFlowScopes> _applier = new();
    private MonoPageCodex _codex;

    public IPageFlowScopeCollectionApplier PageFlowScopeCollectionApplier => _applier;

    public UnityTask OnInitializeAsync(MonoPageCodex codex)
    {
        _codex = codex;

        if (_applier.TryGet(out var scopes))
        {
            _codex.FlowContext.Publisher.Scope(scopes.Screen)
                .Publish(new ShowPageAsyncMessage("prefab-screen-main", new PageContext {
                    ShowOptions = PageTransitionOptions.NoTransition,
                }));
        }

        return UnityTasks.GetCompleted();
    }
}

// 3. Each screen/popup prefab
public class MainScreen : MonoPageBase<GamePageFlowScopes>
{
    private void OnOpenShopClick()
    {
        if (FlowScopeCollection.TryGetValue(out var scopes) == false)
        {
            return;
        }

        Publisher.Scope(scopes.Popup).Publish(new ShowPageAsyncMessage("prefab-popup-shop",
            new PageContext {
                ShowOptions = PageTransitionOptions.OnlyFirstPageHasDuration,
                HideOptions = PageTransitionOptions.NoTransition,
            }));
    }

    private void OnBackClick()
        => Publisher.Scope(scopes.Screen).Publish(new HideActivePageAsyncMessage(default));
}
```

Editor: add `MonoPageCodex`, declare one `FlowDefinition` per scope (name must match the property
name in `GamePageFlowScopes`), pick `MonoPageFlowKind`, set the loader strategy (Resources /
Addressables) on `MonoPageFlowContext`, and attach a `MonoPageTransition*` to each page prefab.

Warm the pool: `Publish(new PrepoolPageAsyncMessage("prefab-popup-shop", 2))`.
React to transitions by subscribing to `BeginTransitionMessage` / `EndTransitionMessage`.

---

## MVVM

```csharp
using EncosyTower.Mvvm.ComponentModel;
using EncosyTower.Mvvm.Input;

[ObservableObject]
public sealed partial class HudViewModel : MonoBehaviour
{
    [ObservableProperty]
    public int Gold { get => Get_Gold(); set => Set_Gold(value); }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBuy))]
    public bool Busy { get => Get_Busy(); set => Set_Busy(value); }

    public bool CanBuy => Busy == false && Gold > 0;

    [RelayCommand]
    private void OnBuy() { /* generates BuyCommand : IRelayCommand */ }
}
```

The generator emits the `_gold` / `_busy` backing fields — read those inside the class when you
must skip change notification.

View side: put `MonoView` on the prefab, add binders, point the binding context at the view model.
Declare binders for the Unity types you use:

```csharp
using EncosyTower.Mvvm.ViewBinding.Components;

[MonoBinder(typeof(TMP_Text), ExcludeObsolete = true)]
public partial class TMP_TextBinder { }

[MonoBinder(typeof(GameObject), ExcludeObsolete = true)]
[MonoBindingProperty(nameof(GameObject.SetActive))]
public partial class GameObjectBinder { }
```

Custom conversion between a source and a destination type:

```csharp
[Serializable]
[Label("Lerp Color", "Default")]
[Adapter(sourceType: typeof(float), destType: typeof(Color), order: 0)]
public sealed class LerpColorAdapter : IAdapter
{
    [SerializeField] private Color _from, _to;

    public Variant Convert(in Variant variant)
        => variant.TryGetValue(out float t) ? new ColorVariant(Color.Lerp(_from, _to, t)) : variant;
}

[Variant(typeof(Color))] public readonly partial struct ColorVariant { }
```

---

## Databases (spreadsheet → ScriptableObject)

```csharp
// Runtime data row
[Data, DataMutable(DataMutableOptions.WithReadOnlyView)]
public partial struct HeroData
{
    [DataProperty(typeof(EntityIdData))]
    public readonly EntityId Id => Get_Id();

    [DataProperty(typeof(uint), typeof(StringIdValueConverter))]
    [DataManualAuthoring(typeof(string))]
    public readonly StringId Name => Get_Name();

    [DataProperty] public readonly EntityStatData Stat => Get_Stat();
    [DataProperty] public readonly ReadOnlyMemory<StatMultiplierData> Multipliers => Get_Multipliers();
}

// Table asset
[DataTableAsset]
public sealed partial class HeroTableAsset : DataTableAssetBase<EntityId, HeroData> { }

// Database
[Database(NameCasing.SnakeLower, AssetName = "GameDatabaseAsset")]
public readonly partial struct GameDatabase
{
    [Table] public readonly HeroTableAsset Heroes => Get_Heroes();
    [Table] public readonly StringTableAsset Strings => Get_Strings();
}

// Runtime use
var db = new GameDatabase(_databaseAsset, InitializationBehaviour.Forced);
foreach (var hero in db.Heroes.Entries.Span) { /* ... */ }
```

Authoring side lives in an editor-only file guarded by `#if UNITY_EDITOR && BAKING_SHEET`, using
`[AuthorDatabase(typeof(GameDatabase), typeof(MyStringToEnumConverter))]` on a
`readonly partial struct`. Import runs from the Databases Settings window
(`EncosyTower.Databases.Settings`). Full worked example:
`Samples~/EncosyTower.Samples.Data/SampleDatabase.cs`.

---

## Persistences (save/load)

```csharp
[Persistence]
public static partial class PlayerPersistence
{
    private static Persistence s_vault;

    public static ReadOnlyAccessorCollection Accessors => s_vault.Accessors;

    public static async UnityTaskVault InitializeAsync(bool newGame, CancellationToken token)
    {
        var userId = API.GetOrGeneratePlayerId(forceGenerate: newGame);

        s_vault = new Persistence(StringVault.Default, new NoEncryption(Logger.Default),
            Logger.Default, ArrayPool<UnityTask>.Shared, userId);

        await s_vault.TryLoadAsync(Logger.Default, userId, SourcePriority.OnlyDevice,
            SaveDestination.Device, token).SuppressCancellationThrow();

        return s_vault.AsReadOnly();
    }

    partial class PersistDirectory
    {
        private static partial PersistStoreArgs GetStoreArgs<TData, TStore>(Func<TData> createFunc)
            where TData : IPersist where TStore : PersistStoreBase<TData>
            => new PersistStoreDefault<TData>.Args(createFunc, new PersistSourceDevice<TData>.Args(
                RootPath: SaveFolderPath,
                SerializeFunc: JsonHelper.TrySerialize,
                DeserializeFunc: JsonHelper.TryDeserialize,
                FileExtension: "json",
                MakeFilePathFunc: MakeFilePath));
    }
}

[Serializable, Persist]
internal partial class PlayerData
{
    public JsonArrayMap<ItemId, int> ItemAmounts { get; set; } = new();
}

[DisplayName("Player")]
[PersistAccessor(typeof(PlayerPersistence))]
public sealed class PlayerDataAccessor : IPersistAccessor
{
    private readonly PersistStoreDefault<PlayerData> _store;
    internal PlayerDataAccessor(PersistStoreDefault<PlayerData> store) => _store = store;

    public Result<Changed<int>, PlayerDataError> AddItem(ItemId id, int amount)
    {
        // mutate _store.Data, then:
        _store.MarkDirty();
        return new Changed<int>(newAmount, previousAmount);
    }
}
```

Every accessor method returns `Result<TValue, TError>` and calls `MarkDirty()` only on success.
Full example: `Samples~/EncosyTower.Samples.Persistence/`.

---

## Keys & asset loading

```csharp
using EncosyTower.AddressableKeys;
using EncosyTower.ResourceKeys;
using EncosyTower.ConfigKeys;

[SerializeField] private AddressableKey<GameObject> _enemyPrefab;

if (_enemyPrefab.TryLoad(out var prefab)) { /* ... */ }              // Option<T>
var result = _enemyPrefab.LoadOrError<GameObject>();                 // Result<T, Error>
var pair = _enemyPrefab.LoadGetHandle<GameObject>();                 // ValueHandlePair<T> — release later
var opt  = await _enemyPrefab.TryLoadAsync(token);

ResourceKey<Sprite> icon = "Icons/coin";
var sprite = icon.TryLoad();

// typed PlayerPrefs
private static readonly ConfigKey<string> s_playerId = "CURRENT_PLAYER_ID";
var id = s_playerId.GetPlayerPref().GetValueOrDefault(string.Empty);
s_playerId.SetPlayerPref(newId);
```

---

## Settings (Project Settings page for free)

```csharp
using EncosyTower.Settings;

[Settings(SettingsUsage.RuntimeProject, "Apexion/Gameplay")]
public sealed class GameplaySettings : Settings<GameplaySettings>
{
    [SerializeField] private float _tickRate = 30f;
    public float TickRate => _tickRate;
}

// anywhere
var rate = GameplaySettings.Instance.TickRate;
```

`SettingsUsage`: `RuntimeProject` (shipped asset), `EditorProject` (editor-only, committed),
`EditorUser` (per-developer, not committed).

---

## Option / Result / Error

```csharp
using EncosyTower.Common;

public Option<Item> FindItem(ItemId id)
    => _map.TryGetValue(id, out var item) ? item : Option.None;

public Result<Item, InventoryError> TakeItem(ItemId id, int amount)
{
    if (amount < 1)
    {
        return InventoryError.NonPositiveAmount(id, amount);   // implicit conversion
    }

    return item;                                               // implicit conversion
}

var (value, error) = TakeItem(id, 1);                          // deconstruct
if (result.TryGetValue(out var item)) { }
if (result.TryGetError(out var err)) { }
var safe = result.GetValueOrDefault(Item.Empty);
```

---

## Source-generated type tools

```csharp
// newtype
[WrapRecord] public readonly partial record struct HeroId(short Value);
[WrapType(typeof(string), "_value")] public readonly partial struct PlayerName { }

// one id, many kinds
[UnionId(Size = UnionIdSize.ULong, KindSettings = UnionIdKindSettings.PreserveOrder | UnionIdKindSettings.RemoveSuffix)]
[UnionIdKind(typeof(HeroId),  0, "Hero",  signed: true)]
[UnionIdKind(typeof(EnemyId), 1, "Enemy", signed: true)]
public readonly partial struct EntityId { }

// fast enum helpers -> ToStringFast(), TryParse(), flag ops
[EnumExtensionsFor(typeof(PageTransitionOptions))]
public static partial class PageTransitionOptionsExtensions { }

// enum composed from other enums (modular content)
[EnumTemplate]
[EnumTemplateMembersFromEnum(typeof(BaseItemType), 000)]
[EnumTemplateMembersFromEnum(typeof(DlcItemType),  100)]
public readonly partial struct ItemType_EnumTemplate { }   // generates enum ItemType

// discriminated union of structs
[PolyEnumStruct]
public partial struct PlayerAction
{
    public partial struct Move { public void Execute() { } }
    public partial struct Attack { public void Execute() { } }
}
```

---

## Logging

```csharp
using EncosyTower.Logging;

StaticDevLogger.LogInfo(entry);            // stripped outside dev builds
DevLogger.Default.LogWarning(message);
StaticLogger.LogErrorSlim(message);        // no stack trace
_unityLogger.LogInfo(this, message);       // click-through context

// on-screen console buffer
private readonly StringBuilderLogger _logger = new();
_logger.OnLogEntryWritten += () => _output.text = _logger.ToString();
```

Guards that vanish in release builds:

```csharp
using static EncosyTower.Debugging.ValidationDefines;

[HideInCallstack, StackTraceHidden]
[Conditional(UNITY_EDITOR), Conditional(DEBUG), Conditional(RUNTIME_CHECKS)]
private static void ErrorIfMissing(UnityEngine.Object context) { /* ... */ }
```

---

## In-game cheat console (VisualDebugging)

```csharp
using EncosyTower.VisualDebugging.Commands;

[ObservableObject]
[Label("Gold", "Resources"), VisualOrder(1)]
internal sealed partial class VisualCommand_Gold : IVisualCommand
{
    [ObservableProperty]
    private int Amount { get => Get_Amount(); set => Set_Amount(value); }

    [RelayCommand]
    private void SetAmount(int value) => _amount = value;

    public void Execute() => PlayerPersistence.Accessors.Player.AddItemAmount(ItemId.Gold, Amount);
}
```

Host it with `VisualCommanderPage` (a `MonoPage`, so it drops into an existing PageFlow) or the
editor window in `EncosyTower.Editor/VisualDebugging`.
