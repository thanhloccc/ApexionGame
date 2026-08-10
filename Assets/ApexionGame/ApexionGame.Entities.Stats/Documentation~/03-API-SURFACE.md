# 03 — API Surface

> Chữ ký dưới đây là *thiết kế*, chưa phải code cuối. Chúng bám sát bản gốc để diff dễ đọc.

## 3.1 Contracts (L1)

Giữ nguyên bản gốc, bỏ `IBufferElementData`.

```csharp
public interface IStatValuePair
{
    StatVariantType Type { get; }
    bool IsPair { get; }
    StatVariant GetBaseValueOrDefault(in StatVariant defaultValue = default);
    StatVariant GetCurrentValueOrDefault(in StatVariant defaultValue = default);
    bool TrySetBaseValue(in StatVariant value);
    bool TrySetCurrentValue(in StatVariant value);
    bool TrySetValues(in StatVariant baseValue, in StatVariant currentValue);
}

public interface IStatValuePairComposer<TValuePair>
    where TValuePair : IStatValuePair
{
    TValuePair Compose(bool isPair, in StatVariant baseValue, in StatVariant currentValue);
}

public interface IStatData
{
    bool IsValuePair { get; }
    StatVariantType ValueType { get; }
    StatVariant BaseValue { get; set; }
    StatVariant CurrentValue { get; set; }
}

public interface IStat<TValuePair>                       // bỏ : IBufferElementData
    where TValuePair : unmanaged, IStatValuePair
{
    ModifierRange ModifierRange { get; set; }
    ObserverRange ObserverRange { get; set; }
    bool ProduceChangeEvents { get; set; }
    TValuePair ValuePair { get; set; }
    uint UserData { get; set; }
    int UserDataSize { get; }
    StatVariant GetBaseValueOrDefault(in StatVariant defaultValue = default);
    StatVariant GetCurrentValueOrDefault(in StatVariant defaultValue = default);
    bool TrySetBaseValue(in StatVariant value);
    bool TrySetCurrentValue(in StatVariant value);
    bool TrySetValues(in StatVariant baseValue, in StatVariant currentValue);
}

public interface IStatModifierStack<TValuePair, TStat>
    where TValuePair : unmanaged, IStatValuePair
    where TStat : unmanaged, IStat<TValuePair>
{
    void Reset(in TStat stat);
    void Apply(in StatVariant baseValue, ref StatVariant currentValue);
}

public interface IStatModifier<TValuePair, TStat, TStatModifierStack>   // bỏ : IBufferElementData
    where TValuePair : unmanaged, IStatValuePair
    where TStat : unmanaged, IStat<TValuePair>
    where TStatModifierStack : unmanaged, IStatModifierStack<TValuePair, TStat>
{
    uint Id { get; set; }
    void AddObservedStatsToList(NativeList<StatHandle> observedStatHandles);
    void Apply(StatReader<TValuePair, TStat> reader, ref TStatModifierStack stack
        , out bool shouldProduceModifierTriggerEvent);

    // ★ THÊM MỚI so với bản gốc (DEC-004 = A) — cần cho save/load
    void RemapObservedStats(in StatOwnerRemap remap);
}
```

`StatOwnerRemap` (mới):

```csharp
public readonly struct StatOwnerRemap
{
    private readonly NativeHashMap<StatOwnerHandle, StatOwnerHandle> _map;

    public bool TryRemap(StatOwnerHandle old, out StatOwnerHandle current);
    public bool TryRemap(StatHandle old, out StatHandle current);      // giữ nguyên StatIndex
    public StatHandle RemapOrNull(StatHandle old);
}
```

Generator sinh implement mặc định: `StatCollectionGenerator` không biết field nào của
`StatModifier` chứa `StatHandle` (struct đó do user viết), nên **`StatSystemGenerator` chỉ
sinh `partial void OnRemapObservedStats(in StatOwnerRemap remap)`** cùng một implement
`RemapObservedStats` gọi vào nó — user override ở partial của mình, đối xứng với cách bản gốc
làm với `AddObservedStatsToList`. Analyzer `AGS_STAT_DATA_0006` cảnh báo nếu `StatModifier` có field kiểu
`StatHandle`/`StatHandle<T>` mà không thấy `OnRemapObservedStats` được implement.

```csharp

public interface IStatObserver                                          // bỏ : IBufferElementData
{
    StatHandle ObserverHandle { get; set; }
}
```

## 3.2 Storage seam (L2)

```csharp
public readonly struct StatOwnerHandle : IEquatable<StatOwnerHandle>, IIsValid { /* §2.3.1 */ }

public unsafe struct StatBuffer<T> : IIsCreated where T : unmanaged { /* §2.3.2 */ }

public unsafe struct StatStore<TStat, TStatModifier, TStatObserver>
    : IDisposable, INativeDisposable, IIsCreated
{
    public StatStore(int initialOwnerCapacity, AllocatorManager.AllocatorHandle allocator);

    public int OwnerCount { get; }
    public bool IsCreated { get; }

    public StatOwnerHandle CreateOwner(int statCapacity = 4, int modifierCapacity = 4, int observerCapacity = 4);
    public bool DestroyOwner(StatOwnerHandle owner);
    public bool Exists(StatOwnerHandle owner);

    public bool TryGetStats(StatOwnerHandle owner, out StatBuffer<TStat> buffer);
    public bool TryGetModifiers(StatOwnerHandle owner, out StatBuffer<TStatModifier> buffer);
    public bool TryGetObservers(StatOwnerHandle owner, out StatBuffer<TStatObserver> buffer);
    public bool TryGetBuffers(StatOwnerHandle owner, out StatBuffer<TStat> stats
        , out StatBuffer<TStatModifier> modifiers, out StatBuffer<TStatObserver> observers);

    public bool TryGetModifierIdCounter(StatOwnerHandle owner, out uint counter);

    public void Dispose();
    public JobHandle Dispose(JobHandle inputDeps);
}
```

## 3.3 Algorithm (L3)

### 3.3.1 `StatAPI` (static)

Bỏ 3 overload `AddStatComponents(EntityManager | ECB | ParallelWriter)` và
`BakeStatComponents(IBaker, …)` và `GetStatComponentTypeSet<>()`. Thêm:

```csharp
public static void CreateStatOwnerHandle<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>(
      ref StatStore<TStat, TStatModifier, TStatObserver> store
    , out StatOwnerHandle owner
    , out StatBuilder<...> builder
    , int statCapacity = 4, int modifierCapacity = 4, int observerCapacity = 4
    , TValuePairComposer valuePairComposer = default
);
```

Hàm này tự `stats.Add(new TStat())` để giữ **None stat ở index 0** — đúng như bản gốc làm
trong `BakeStatComponents` / `AddStatComponents`.

Giữ nguyên (đổi `ReadOnlySpan<TStat>`/`BufferLookup<TStat>` → `ReadOnlySpan<TStat>`/`StatStore`):

```
CreateStatHandle (3 overload)   GetStatData / TryGetStatData
GetStatValue / TryGetStatValue  Contains (4 overload)
GetStat / TryGetStat / GetStats SetStatData / TrySetStatData
SetStatValue / TrySetStatValue  SetStat / TrySetStat
GetStatRefUnsafe (3 overload)   TryGetModifierCount / TryGetModifiersOfStat
TryGetObserverCount / TryGetObserversOfStat / TryGetAllObservers
EntityHasAnyOtherDependantStatEntities  → OwnerHasAnyOtherDependantStats
GetOtherDependantStatsOfEntity          → GetOtherDependantStatsOfOwner
GetOtherDependantStatEntitiesOfEntity   → GetOtherDependantOwnersOfOwner
GetStatEntitiesThatEntityDependsOn      → GetOwnersThatOwnerDependsOn
UpdateSingleStatCommon          AddObserversOfStatToList
AddStatAsObserverOfOtherStat    MakeStatData
```

### 3.3.2 `StatAccessor<6>`

```csharp
public partial struct StatAccessor<TValuePair, TStat, TStatModifier, TStatModifierStack
    , TStatObserver, TValuePairComposer>
{
    internal StatStore<TStat, TStatModifier, TStatObserver> _store;   // trước: 4 lookup
    internal TValuePairComposer _valuePairComposer;
    internal TStat _nullStat;

    public StatAccessor(StatStore<TStat, TStatModifier, TStatObserver> store
        , TValuePairComposer valuePairComposer = default);
    // KHÔNG còn: StatAccessor(ref SystemState), Update(ref SystemState)
}
```

Toàn bộ method còn lại giữ tên và chữ ký (thay `Entity` → `StatOwnerHandle`,
`DynamicBuffer<T>` → `StatBuffer<T>`):

```
TryCreateStatHandle (4 overload)
TryGetStatData / TryGetStat / GetStats / TryGetStatValue
TrySetStatBaseValue / TrySetStatCurrentValue / TrySetStatValues / TrySetStatData
TrySetBaseValueToStats / TrySetCurrentValueToStats / TrySetDataToStats   (batch theo owner)
TrySetStatProduceChangeEvents / TrySetStatUserData
TryUpdateStat / TryUpdateAllStats
TryGetModifiersOfStat / TryGetObserversOfStat / TryGetModifierCount / TryGetObserverCount
TryGetAllObservers
TryAddStatModifier / TryAddStatModifiersBatch / TryGetStatModifier
TryRemoveStatModifier / TryRemoveModifiersOfStat
```

Ba hàm batch (`TrySetBaseValueToStats`, …) trong bản gốc dùng
`NativeParallelMultiHashMap<Entity, int>` + `NativeHashSet<Entity>` với `Allocator.Temp` để
gom theo entity. Port đổi key thành `StatOwnerHandle`. **D-02 ✅ (task 4.6):** ba hàm nhận thêm
`Allocator allocator = Allocator.Temp` ở cuối, để tránh `Allocator.Temp` trong job dài. Cả hai
container đều được dispose trước khi trả về nên truyền allocator nào cũng an toàn.

### 3.3.3 `StatAccessor.ReadOnly` / `StatReader<2>`

Bỏ `ComponentLookup<StatOwner>` (bản gốc giữ nó *chỉ để* đăng ký read-dependency với
system scheduling — không còn ý nghĩa ngoài ECS). `ReadOnly` giữ một `StatStore` và chỉ
expose method đọc. `StatReader<TValuePair, TStat>` giữ hai chế độ: một
**`StatBufferLookup<TStat>`** hoặc một `StatBuffer<TStat>` đơn.

> Ban đầu mục này ghi chế độ thứ nhất là "`useStore`" — sai, vì `StatStore` có 3 tham số kiểu
> còn `StatReader` chỉ được phép có 2. `StatBufferLookup<TStat>` (generic đúng 1 kiểu, lấy từ
> `store.AsStatLookup()`) là thứ thay thế đúng —
> [I-12](07-DECISIONS.md#i-12--statownerslot-tách-thành-mảng-song-song-thêm-statbufferlookupt).

### 3.3.4 `StatBuilder<6>` — thay `StatBaker<6>`

```csharp
public struct StatBuilder<TValuePair, TStat, TStatModifier, TStatModifierStack
    , TStatObserver, TValuePairComposer>
{
    public StatOwnerHandle Owner { get; }

    public void Clear(TValuePairComposer valuePairComposer = default);

    public StatHandle<TStatData> CreateStatHandle<TStatData>(TStatData statData
        , bool produceChangeEvents, uint userData) where TStatData : unmanaged, IStatData;
    public StatHandle<TStatData> CreateStatHandle<TStatData>(TValuePair valuePair
        , bool produceChangeEvents, uint userData) where TStatData : unmanaged, IStatData;
    public StatHandle CreateStatHandle(TValuePair valuePair, bool produceChangeEvents, uint userData);

    public bool Contains(StatHandle handle);
    public bool Contains(StatHandle handle, uint userData);

    public StatHandle<TStatData> SetStatOrCreateHandle<TStatData>(...);
    public void SetStat<TStatData>(...);
    public void SetStat(StatHandle handle, TValuePair valuePair, bool produceChangeEvents, uint userData);

    public bool TryAddStatModifier(StatHandle affectedStatHandle, TStatModifier modifier
        , out StatModifierHandle statModifierHandle);
}
```

Khác `StatBaker`: không có `IBaker`, không `SetComponent`; `modifierIdCounter` ghi trực tiếp
vào slot. Vẫn giữ tối ưu `isGuaranteedSingleEntity: true` (builder chỉ làm việc trên 1 owner).

### 3.3.5 Jobs

```csharp
[BurstCompile]
public struct DeferredUpdateStatListJob<...>   : IJob { StatAccessor<..> accessor; StatWorldData<..> worldData; NativeList<StatHandle> statsToUpdate; }
public struct DeferredUpdateStatQueueJob<...>  : IJob { ... NativeQueue<StatHandle> statsToUpdate; }
public struct DeferredUpdateStatStreamJob<...> : IJob { ... NativeStream.Reader statsToUpdate; }
```

Nhánh `#if LATIOS_FRAMEWORK` được bỏ (phase 1). Extension point: user tự viết `IJob`
gọi `accessor.TryUpdateStat(handle, ref worldData)`.

## 3.4 Generated surface (L4) — không đổi so với bản gốc

### 3.4.1 Từ `[StatSystem]`

```csharp
[StatSystem(StatDataSize.Size8)]
public static partial class StatSystem { }
```

sinh ra (trong `StatSystem`):

| Type | Vai trò |
|---|---|
| `Stat : IStat<ValuePair>` | struct stat cụ thể; `_valueData` là `StatDataStore`, `_userData` là `byte`/`ushort`/`uint` theo `StatUserDataSize` |
| `Stat<TStatData>` | phantom-typed |
| `ValuePair : IStatValuePair, IEquatable<ValuePair>` | `{ StatDataStore _data; StatVariantType _type; ByteBool _isPair; }` |
| `ValuePair.Composer : IStatValuePairComposer<ValuePair>` | có `partial void OnCompose(...)` để user chèn clamp/round |
| `StatDataStore` (internal) | `[StructLayout(Explicit)]`, mọi field `[FieldOffset(0)]` — union đúng `MaxDataSize` byte |
| `StatModifier` (partial, user hoàn thiện) | user tự thêm field + implement `Apply` |
| `StatModifier.Stack` (partial) | user implement `Reset`/`Apply` |
| `StatObserver : IStatObserver` | |
| `API` (static) | wrapper 0-generic quanh `StatAPI` |
| `Reader`, `Accessor`, `Accessor.ReadOnly`, `Builder`, `WorldData` | wrapper 0-generic |
| `ModifierTriggerEvent`, `StatModifierRecord` | alias cụ thể |
| `DeferredUpdateStat{List,Queue,Stream}Job` | job cụ thể, `[BurstCompile]` |
| `IsCompatible(StatVariantType, bool isPair)` | bảng kiểu hợp lệ theo `MaxDataSize` |

Quy tắc lọc kiểu (giữ nguyên `FilterTypes` ở `StatSystemSpec+WriteCode.cs:189`):
`size > MaxDataSize` → không dùng được; `size <= MaxDataSize/2` → dùng được ở chế độ
**pair** (base + current cùng nằm trong `MaxDataSize` byte).

### 3.4.2 Từ `[StatCollection]` + `[StatData]`

```csharp
[StatCollection(typeof(StatSystem), 1000)]
public partial struct Stats                          // KHÔNG cần : IComponentData nữa
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct MoveSpeed { }
    [StatData(typeof(DirectionType))] public partial struct Direction { }
    [StatData(StatVariantType.Half2)] public partial struct DirectionVector { }
    [StatData(typeof(MotionFlag))]    public partial struct Motion { }

    partial struct TypeId { }
    partial struct Indices { }
    partial struct StatIndices { }
    partial struct StatHandles { }
    static partial class Options { partial struct Data { } partial struct ProduceChangeEvents { } }
    static partial class Builder { }  partial struct Builder<T> { }
    static partial class Accessor { } partial struct Accessor<T> { }
    partial struct Reader<T> { }
}
static partial class StatsExtensions { }
```

sinh ra: `enum Type`, `TypeId` (+ `EncodeToStatUserData` / `TryDecode`, có `TypeIdOffset`),
`Index`/`Index<TStatData>`, `IndexRecord`, `StatIndexRecord`, `StatHandleRecord`,
`Indices`, `StatIndices`, `StatHandles`, `Options.Data`, `Options.ProduceChangeEvents`,
`<Stat>.Params.Create(...)`, `Builder`/`Builder<T>`, `Accessor`/`Accessor<T>`, `Reader<T>`,
extensions.

`TypeIdOffset` (tham số thứ 2 của `[StatCollection]`) dùng để nhiều collection không
trùng `Stat.UserData` — giữ nguyên.

## 3.5 Ví dụ end-to-end (dự kiến)

### 3.5.1 Khai báo

```csharp
using ApexionGame.Entities.Stats;
using Unity.Mathematics;

[StatSystem(StatDataSize.Size8)]
public static partial class StatSystem { }

public enum DirectionType : byte { Forward, Backward, Up, Down, Left }

[StatCollection(typeof(StatSystem), 1000)]
public partial struct Stats
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct MoveSpeed { }
    [StatData(typeof(DirectionType))] public partial struct Direction { }
}
```

Người dùng tự hoàn thiện `StatModifier` + `Stack` (giống bản gốc):

```csharp
public static partial class StatSystem
{
    public partial struct StatModifier
    {
        public enum ModifierType : byte { Add, AddFromStat, Multiply }

        public ModifierType type;
        public StatVariant value;
        public StatHandle observedStat;

        public void AddObservedStatsToList(NativeList<StatHandle> observed)
        {
            if (type == ModifierType.AddFromStat) { observed.Add(observedStat); }
        }

        public void Apply(StatReader reader, ref Stack stack, out bool trigger)
        {
            trigger = false;

            switch (type)
            {
                case ModifierType.Add:
                {
                    stack.add += value;
                    break;
                }
                case ModifierType.AddFromStat:
                {
                    if (reader.TryGetStatValue(observedStat, out var other))
                    {
                        stack.add += other.GetCurrentValueOrDefault();
                    }
                    break;
                }
                case ModifierType.Multiply:
                {
                    stack.mul *= value;
                    break;
                }
            }
        }

        public partial struct Stack
        {
            public StatVariant add;
            public StatVariant mul;

            public void Reset(in Stat stat)
            {
                add = new StatVariant(0f);
                mul = new StatVariant(1f);
            }

            public void Apply(in StatVariant baseValue, ref StatVariant currentValue)
            {
                currentValue = (baseValue + add) * mul;
            }
        }
    }
}
```

### 3.5.2 Khởi tạo world

```csharp
public sealed class StatService : IDisposable
{
    private StatSystem.Store _store;          // = StatStore<Stat, StatModifier, StatObserver>
    private StatSystem.Accessor _accessor;
    private StatSystem.WorldData _worldData;

    public StatService()
    {
        _store = new StatSystem.Store(initialOwnerCapacity: 256, Allocator.Persistent);
        _accessor = new StatSystem.Accessor(_store);
        _worldData = new StatSystem.WorldData(initialCapacity: 64, Allocator.Persistent);
    }

    public void Dispose()
    {
        _worldData.Dispose();
        _store.Dispose();
    }
}
```

### 3.5.3 Tạo một chủ thể (thay baking)

```csharp
public Stats SpawnPlayer(float hp, float moveSpeed, DirectionType dir)
{
    return Stats.Builder.Build(ref _store)          // thay Stats.Baker.Bake(this, entity)
        .CreateStat(Stats.Hp.Params.Create(hp))
        .CreateStat(Stats.MoveSpeed.Params.Create(moveSpeed))
        .CreateStat(Stats.Direction.Params.Create(dir))
        .ToStats();                                 // thay CreateComponentData<Stats>() + AddComponentToEntity()
}
```

Kết quả `Stats` là struct thuần chứa `StatHandles` → project tự lưu ở đâu cũng được
(field của `class Character`, `NativeList<Stats>`, `Dictionary<int, Stats>`, …).

### 3.5.4 Đọc / ghi / modifier

```csharp
// đọc
if (_accessor.ReadOnlyView.TryGetStatData(stats.Handles.Hp, out Stats.Hp hp))
{
    float current = hp.CurrentValue.Float;
}

// ghi base value → tự lan truyền tới observer
_accessor.TrySetStatBaseValue(stats.Handles.Hp, new StatSystem.ValuePair(120f), ref _worldData);

// thêm modifier
_accessor.TryAddStatModifier(
      stats.Handles.MoveSpeed
    , new StatSystem.StatModifier {
        type = StatSystem.StatModifier.ModifierType.Multiply,
        value = new StatVariant(1.5f),
    }
    , out var modifierHandle
    , ref _worldData
);

// tiêu thụ event
foreach (var (handle, prev, next) in _worldData.StatChangeEvents)
{
    // ...
}
_worldData.ClearStatChangeEvents();
```

### 3.5.5 Job hoá

```csharp
// nhiều job song song chỉ *thu thập* handle
var toUpdate = new NativeQueue<StatHandle>(Allocator.TempJob);

var damage = new ApplyDamageJob {
    accessor = _accessor.AsReadOnly(),
    statsToUpdate = toUpdate.AsParallelWriter(),
}.Schedule(count, 64);

// một job đơn luồng apply
var update = new StatSystem.DeferredUpdateStatQueueJob {
    statAccessor = _accessor,
    statWorldData = _worldData,
    statsToUpdate = toUpdate,
}.Schedule(damage);

update.Complete();
toUpdate.Dispose();
```
