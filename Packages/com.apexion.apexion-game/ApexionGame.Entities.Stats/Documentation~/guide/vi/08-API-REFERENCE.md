# API reference

*[English](../08-API-REFERENCE.md) · [Mục lục hướng dẫn](README.md)*

Bản đồ bề mặt API, không phải bảng chữ ký đầy đủ. Mọi type đều có XML doc; trang này nói cho bạn biết
nên mở type nào.

| Namespace | |
|---|---|
| `ApexionGame.Entities.Stats` | mọi thứ runtime |
| `ApexionGame.Entities.Stats.Debugging` | `StatDebugRegistry`, `StatStoreDebug<4>`, `ValidationDefines` |
| `ApexionGame.Entities.Stats.Authoring` | `SerializableStatVariant`, `StatDefinitionAsset` |

## Định danh

| Type | Hình dạng | Ghi chú |
|---|---|---|
| `StatOwnerHandle` | `readonly struct { int Index; int Version; }` | `Null`, `IsValid`, `==`, `Deconstruct`, `ToFixedString()`. Version vô hiệu hoá slot tái dùng. Không ổn định qua các phiên. |
| `StatIndex` | `struct { int value; }` | `IsValid => value > 0` — index 0 là None stat. `IsValidInRange(length)`, `ToStatHandle(owner)`. |
| `StatIndex<TStatData>` | bản có kiểu | implicit → `StatIndex` |
| `StatHandle` | `struct { StatOwnerHandle owner; StatIndex index; }` | `Null`, `IsValid` (theo `owner.IsValid`), `ToFixedString()` |
| `StatHandle<TStatData>` | bản có kiểu | implicit → `StatHandle`, explicit ← `StatHandle` |
| `StatModifierHandle` | `struct { StatHandle affectedStatHandle; uint modifierId; }` | id cục bộ theo owner |
| `StatOwnerRemap` | bọc `NativeHashMap<StatOwnerHandle, StatOwnerHandle>` | `TryRemap`, `RemapOrNull` — xem [Lưu / tải](05-PERSISTENCE.md) |

## Giá trị

| Type | |
|---|---|
| `StatVariant` | union giá trị — 18 kiểu cụ thể cộng `None`, 1–16 byte, toán tử số học và so sánh, hàm toán học, `ToString()` thật |
| `StatVariantType` | enum 18 kiểu + `None`; extension gồm `ZeroVariant()` / `OneVariant()` |
| `StatDataSize` | `Size2, Size4, Size6, Size8, Size12, Size16` — ngân sách của `[StatSystem]` |
| `StatUserDataSize` | `Size1` (byte), `Size2` (ushort), `Size4` (uint) — độ rộng `Stat.UserData` |
| `StatData<TStatData>` | `{ Data, UserData, ProduceChangeEvents }` — một stat cộng cờ của nó |
| `StatDataParams<TStatData>` | bốn `Option<T>`: stat data, handle, produce-change-events, user data. Là thứ `Params.Create` trả về. |
| `StatValueParams<TValuePair>` | cùng hình dạng, giá trị không kiểu — thứ các hàm set batch tiêu thụ |
| `StatSingle<T>` | marker single-value, implicit từ `T` |
| `None` | value type rỗng |
| `ModifierRange`, `ObserverRange` | khối `(startIndex, count)` trong buffer dùng chung |
| `StatChangeEvent<TValuePair>` | `{ statHandle, prevValue, newValue }`, deconstruct được |
| `ModifierTriggerEvent<4>` | `{ handle, modifier }`, deconstruct được |
| `StatModifierRecord<4>` | modifier + handle của nó |

## Contract

Được implement bởi code sinh ra, trừ chỗ ghi chú khác.

| Interface | |
|---|---|
| `IStatValuePair` | `Type`, `IsPair`, get/set base và current |
| `IStatValuePairComposer<TValuePair>` | `Compose(isPair, base, current)` — `ValuePair.Composer` sinh ra có hook `OnCompose` để clamp hoặc round |
| `IStatData` | `IsValuePair`, `ValueType`, `BaseValue`, `CurrentValue` — sinh từ `[StatData]` |
| `IStat<TValuePair>` | range, `ProduceChangeEvents`, `ValuePair`, `UserData`, các hàm truy cập giá trị |
| `IStatModifierStack<TValuePair, TStat>` | `Reset(in TStat)`, `Apply(in StatVariant, ref StatVariant)` — **bạn viết hook** |
| `IStatModifier<TValuePair, TStat, TStack>` | `Id`, `AddObservedStatsToList`, `Apply`, `RemapObservedStats` — **bạn viết hook** |
| `IStatObserver` | `ObserverHandle` |

`RemapObservedStats` mới có ở bản port này. Modifier viết cho bản gốc không compile cho tới khi thêm hook.

## Lưu trữ

### `StatStore<TStat, TStatModifier, TStatObserver>`

Sở hữu tất cả. Native memory, `IDisposable` + `INativeDisposable`.

```csharp
new StatStore<Stat, StatModifier, StatObserver>(initialOwnerCapacity, allocator)
```

| Thành viên | |
|---|---|
| `IsCreated`, `Capacity`, `OwnerCount` | |
| `CreateOwner()` / `CreateOwner(statCapacity, modifierCapacity, observerCapacity)` | mặc định 4/4/4. **Không** gieo None stat — hãy đi qua builder sinh ra. |
| `DestroyOwner(owner)` | xoá buffer và pool slot; không giải phóng bộ nhớ |
| `Exists(owner)`, `TryGetOwnerAt(slotIndex, out owner)` | |
| `TryGetStats` / `TryGetModifiers` / `TryGetObservers` / `TryGetBuffers` | `false` khi owner đã chết hoặc version lệch |
| `AsStatLookup()` / `AsModifierLookup()` / `AsObserverLookup()` | view một tham số kiểu |
| `TryGetModifierIdCounter`, `TryIncrementModifierId` | |
| `TryCopyOwnerTo(...)`, `TryRestoreOwner(...)` | [Lưu / tải](05-PERSISTENCE.md) |
| `Dispose()`, `Dispose(JobHandle)` | |

### `StatBuffer<T>` và `StatBufferLookup<T>`

`StatBuffer<T>` là thứ thay `DynamicBuffer<T>`: `Length`, `Capacity`, `this[i]`,
`ElementAt(i) → ref T`, `Add`, `Insert`, `RemoveAt`, `Clear`, `SetCapacity`, `IncreaseCapacityTo`,
`AsArray`, `AsSpan`, `AsReadOnlySpan`, `IsCreated`. Truyền **theo giá trị** — nó bọc một pointer ổn
định, nên bản copy vẫn hợp lệ suốt đời owner.

`StatBufferLookup<T>` là thứ thay `BufferLookup<T>`: `TryGetBuffer(owner, out buffer)`,
`Exists(owner)`. Không có `Update(ref SystemState)` — không có system scheduling để refresh theo.

## Tầng thuật toán

Bình thường bạn với tới tầng này qua các wrapper không generic sinh ra. Các type generic là thứ mà
wrapper đó bọc.

### `StatAccessor<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>`

Sinh ra thành `RpgStatSystem.Accessor`. Dựng từ một store, tuỳ chọn thêm composer. Chia làm bốn file:

| File | Thành viên |
|---|---|
| ``StatAccessor`6.cs`` | `TryCreateStatHandle` ×4, `TryGetStatData`, `TryGetStat`, `GetStats`, `TryGetStatValue`, `TrySetStatBaseValue` ×2, `TrySetStatCurrentValue` ×2, `TryUpdateStat`, `TryUpdateAllStats`, `TryGetModifiersOfStat`, `TryGetObserversOfStat`, `TryGetModifierCount`, `TryGetObserverCount`, `TryGetAllObservers`, `TryAddStatModifier`, `TryAddStatModifiersBatch`, `TryGetStatModifier`, `TryRemoveStatModifier`, `TryRemoveModifiersOfStat`, `DestroyOwnerAndUpdateObservers` |
| `+Batch.cs` | `TrySetStatData` ×4, `TrySetStatValues` ×4, `TrySetStatProduceChangeEvents` ×2, `TrySetStatUserData` ×2, `TrySetBaseValueToStats`, `TrySetCurrentValueToStats`, `TrySetDataToStats` |
| `+ReadOnly.cs` | `AsReadOnly()` và view `ReadOnly` — chỉ getter, song song an toàn |
| `+Persistence.cs` | `TryRemapOwner`, `TryRemapOwners` |

Hàm mutate nhận `ref StatWorldData<…>`. `TryUpdateStat` và `TryUpdateAllStats` trả `void` — stat không
tồn tại là no-op, không phải lỗi.

### `StatReader<TValuePair, TStat>`

Sinh ra thành `RpgStatSystem.Reader`, và là thứ `ApplyInternal` của modifier nhận được. Hai chế độ: trên
một `StatBufferLookup<TStat>` (mọi owner) hoặc trên một `StatBuffer<TStat>` (một owner) — `UseLookup`
cho biết đang ở chế độ nào. Thành viên: `Contains` ×2, `TryGetStatData`, `TryGetStat`, `TryGetStatValue`.

### `StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver>`

Sinh ra thành `RpgStatSystem.WorldData`. Hai event list, năm scratch list, một reference modifier-stack.

```csharp
new RpgStatSystem.WorldData(initialCapacity, allocator)
```

| Thành viên | |
|---|---|
| `GetStatChangeEvents(allocator)` / `(NativeList)` | |
| `GetModifierTriggerEvents(allocator)` / `(NativeList)` | |
| `AddStatChangeEvent`, `AddModifierTriggerEvent` | tự đẩy event vào |
| `ClearStatChangeEvents`, `ClearModifierTriggerEvents`, `Clear` | `Clear` xoá cả hai |
| `SetStatModifierStack(in TStack)` | gieo stack dùng chung |
| `IsCreated`, `Dispose()`, `Dispose(JobHandle)` | |

**Không thread-safe, không reentrant.** Một cái cho một luồng update.

### `StatBuilder<6>`

Sinh ra thành `RpgStatSystem.Builder`, và được bọc lần nữa theo từng collection thành `RpgStats.Builder`.
Thay `StatBaker` của bản gốc — không `IBaker`, không baking pipeline. `Owner`, `Clear(composer)`,
`CreateStatHandle` ×3, `Contains` ×2, `SetStatOrCreateHandle`, `SetStat` ×2, `TryAddStatModifier`.

### `StatAPI`

Static, generic, và là tầng mà mọi thứ khác gọi xuống. Sinh ra thành `RpgStatSystem.API` (không generic).
Thành viên đáng chú ý:

| | |
|---|---|
| `CreateStatOwnerHandle<6>(ref store, out owner, out builder, capacities…, composer)` | **thứ duy nhất gieo None stat ở index 0** |
| `CreateStatHandle` ×3, `GetStatData` / `TryGetStatData`, `GetStatValue` / `TryGetStatValue`, `Contains` ×4, `GetStat` / `TryGetStat` / `GetStats` | đọc |
| `TryGetModifierCount`, `TryGetModifiersOfStat`, `TryGetObserverCount`, `TryGetObserversOfStat`, `TryGetAllObservers` | soi đồ thị |
| `OwnerHasAnyOtherDependantStats`, `GetOtherDependantStatsOfOwner`, `GetOtherDependantOwnersOfOwner`, `GetOwnersThatOwnerDependsOn` | truy vấn chéo owner, mỗi hàm ×2 overload |
| `MakeStatData<TValuePair, TStatData>` | |

`AddStatComponents` (các overload EntityManager / ECB / ParallelWriter),
`BakeStatComponents(IBaker, …)` và `GetStatComponentTypeSet<>()` của bản gốc không có tương đương ở đây.

### Job

`DeferredUpdateStatListJob<6>`, `DeferredUpdateStatQueueJob<6>`, `DeferredUpdateStatStreamJob<6>` trong
`StatJobs.cs` — generic mở, tiêu thụ lần lượt `NativeList<StatHandle>`, `NativeQueue<StatHandle>` và
`NativeStream.Reader`. Mỗi cái có ba field public: `statAccessor`, `statWorldData`, `statsToUpdate`.

**Dùng bản sinh ra** (`RpgStatSystem.DeferredUpdateStat*Job`). Chúng là wrapper không generic mang
`[BurstCompile]` bọc quanh job đã đóng kiểu. Generic mở cố ý không mang `[BurstCompile]` — attribute đó
ở đấy không có tác dụng.

## Bề mặt sinh ra

### Từ `[StatSystem]`

| Type | |
|---|---|
| `Stat : IStat<ValuePair>`, `Stat<TStatData>` | struct stat cụ thể |
| `ValuePair : IStatValuePair`, `ValuePair.Composer` | value pair + composer có hook `OnCompose` |
| `StatDataStore` (internal) | union explicit layout, đúng `maxDataSize` byte |
| `StatModifier` (partial), `StatModifier.Stack` (partial) | **bạn hoàn thiện** — [bảy hook](03-DECLARING-STATS.md#bảy-hook) |
| `StatObserver : IStatObserver` | |
| `API`, `Reader`, `Accessor`, `Accessor.ReadOnly`, `Builder`, `WorldData` | facade không generic |
| `ModifierTriggerEvent`, `StatModifierRecord` | alias cụ thể |
| `DeferredUpdateStat{List,Queue,Stream}Job` | `[BurstCompile]` |
| `IsCompatible(StatVariantType, bool isPair)` | bảng kiểu, tra được lúc runtime |

### Từ `[StatCollection]` + `[StatData]`

| Thành viên | |
|---|---|
| `enum Type` | một member mỗi stat, tên theo struct của bạn |
| `TypeId` | `EncodeToStatUserData`, `DecodeFromStatUserData` ×2, `ValidateStatUserData` ×2, `ValidateType`, `ValidateStat<T>`, `Types`, `OFFSET`, `LENGTH` |
| `Index`, `Index<TStatData>` | vị trí trong collection; chuyển ngầm sang `StatIndex` |
| `IndexRecord`, `StatIndexRecord`, `StatHandleRecord` | bộ ba `(value, type, isValid)`, kèm `ToRecords` / `ToValidRecords` |
| `Indices`, `StatIndices`, `StatHandles` | bộ cố định — enumerate bằng span, `GetIndexFor<T>()`, `GetStatHandleFor<T>()` |
| `Options.Data`, `Options.ProduceChangeEvents` | bundle theo stat cho hàm batch, mọi tham số tuỳ chọn |
| `<Stat>.Params.Create(...)` | tới 3 overload: chỉ cờ, một giá trị, base + current |
| `Builder` / `Builder<T>` | `Build(ref store, out owner, composer)`, `CreateAllStats`, `CreateStat`, `CreateStats`, `SetStat`, `SetStats`, `SetOrCreateStat`, `SetOrCreateStats`, `Reset`, `ToStats()`, `As<T>()` |
| `Accessor` / `Accessor<T>` | `Create(owner, statCollection, accessor, worldData)`, rồi nối chuỗi `TrySetStatBaseValue`, `TrySetStatCurrentValue`, `TrySetStatValues`, `TryCreateStat`, `TryCreateAllStats`, `TryCreateOrSetStat`, `TrySet*ToStats`, `TrySetProduceChangeEventsFor*`, `TryGetStat`, `TryGetStatData`, `FindValidStats` |
| `Reader` / `Reader<T>` | `Create(owner, statCollection, statBuffer)`, rồi `Contains`, `TryGetStat`, `TryGetStatData`, `GetStatDataOptions`, `GetProduceChangeEventsOptions`, `FindValidStats` |
| `<Collection>Extensions` | `GetStatHandlesFrom`, `GetStatIndicesFrom`, `GetIndicesFrom`, `ToComponent<T>`, `TryGetValuePair` |
| trên chính struct collection | `GetStatHandles(owner)`, `GetStatIndices()`, `ToStatHandles(owner)`, `ToStatIndices()`, `Indices` |

Hàm của accessor collection trả về chính accessor nên nối chuỗi được; trạng thái thành công đi ra bằng
`out bool`.

## Debugging

| Type | |
|---|---|
| `IStatStoreDebug` | `Name`, `IsCreated`, `OwnerCount`, `GetOwners`, `GetStats`, `GetObserverEdges` |
| `StatStoreDebug<TValuePair, TStat, TStatModifier, TStatObserver>` | `(name, store, nameLookup = null)` — implementation cụ thể |
| `StatDebugRegistry` | `Register`, `Unregister`, `Clear`, `Stores`, `Changed` |
| `StatDebugInfo`, `StatObserverEdge` | thứ cửa sổ render |
| `ValidationDefines` | tên các symbol `[Conditional]` |

Unregister trước khi dispose store. Xem [Tooling](07-TOOLING.md#stat-debugger).

## Authoring

| Type | |
|---|---|
| `SerializableStatVariant` | `Type`, `ToStatVariant()`, `From(in StatVariant)`, `Of(float/int/bool/float4)` |
| `StatDefinitionEntry` | `Id`, `BaseValue`, `ProduceChangeEvents`, `UserData` |
| `StatDefinitionAsset` | `Entries`, `TryGetEntry(id, out entry)`, `TryGetBaseValue(id, out variant)` |

## Chẩn đoán

Mười sáu mã. Mười ba bắt lỗi khai báo; ba báo generator lỗi nội bộ thay vì sinh ra code hỏng.

### `[StatSystem]`

| Mã | Mức | |
|---|---|---|
| `AGS_STAT_SYSTEM_0001` | error | `[StatSystem]` không đặt được lên type generic |
| `AGS_STAT_SYSTEM_0002` | error | assembly dùng `[StatSystem]` nhưng không tham chiếu `EncosyTower.Core` |
| `AGS_STAT_SYSTEM_UNKNOWN_0001` | error | generator lỗi |

### `[StatCollection]`

| Mã | Mức | |
|---|---|---|
| `AGS_STAT_COLLECTION_0001` | error | tham số `typeof` phải resolve về type có `[StatSystem]` |
| `AGS_STAT_COLLECTION_0002` | error | `typeIdOffset` + số member `[StatData]` vượt `uint.MaxValue` |
| `AGS_STAT_COLLECTION_0003` | error | `[StatCollection]` chỉ đặt được lên struct |
| `AGS_STAT_COLLECTION_0004` | error | `[StatCollection]` không đặt được lên struct generic |
| `AGS_STAT_COLLECTION_0005` | warning | struct lồng không có `[StatData]`, nên không sinh stat nào cho nó |
| `AGS_STAT_COLLECTION_UNKNOWN_0001` | error | generator lỗi |

### `[StatData]`

| Mã | Mức | |
|---|---|---|
| `AGS_STAT_DATA_0001` | error | `[StatData]` chỉ đặt được lên struct |
| `AGS_STAT_DATA_0002` | error | `[StatData]` không đặt được lên struct generic |
| `AGS_STAT_DATA_0003` | error | `StatVariantType.None` không phải tham số hợp lệ |
| `AGS_STAT_DATA_0004` | error | tham số `typeof` phải là kiểu enum |
| `AGS_STAT_DATA_0005` | error | kiểu giá trị không vừa `maxDataSize` — value pair cần gấp đôi size; đặt `SingleValue = true` hoặc tăng ngân sách |
| `AGS_STAT_DATA_0006` | warning | modifier lưu `StatHandle` nhưng không có `RemapObservedStatsInternal`, nên handle của nó không sống qua save/load |
| `AGS_STAT_DATA_UNKNOWN_0001` | error | generator lỗi |

`AGS_STAT_COLLECTION_0005`, `AGS_STAT_DATA_0005` và `AGS_STAT_DATA_0006` mới có ở bản port này. Mỗi cái
phủ một trường hợp mà generator bản gốc âm thầm bỏ mất thứ gì.

## Symbol tiền xử lý

| Symbol | |
|---|---|
| `APEXION_RUNTIME_CHECKS` | bật guard runtime trong một build |
| `APEXION_STATS_RUNTIME_CHECKS` | guard riêng của stats |
| `DISABLE_APEXION_CHECKS` | tắt mọi thứ trên |
| `APEXION_STAT_VALUE_TYPES_GENERATOR` | **tạm thời** — compile generator bảng kiểu phía editor; bỏ sau khi sinh lại |
| `ENABLE_UNITY_COLLECTIONS_CHECKS` | của Unity; điều khiển guard `AtomicSafetyHandle` trên store và buffer |
