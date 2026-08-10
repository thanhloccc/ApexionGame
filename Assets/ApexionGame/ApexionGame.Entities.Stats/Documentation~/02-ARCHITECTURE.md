# 02 — Architecture

## 2.1 Mental model

Một *stat* không phải một con số, mà là **một node trong đồ thị tính toán**:

```
                 ┌──────────────────────────────────────────┐
                 │ Stat                                     │
   base value ──▶│  ValuePair { base, current }             │──▶ current value
                 │  ModifierRange (startIndex, count)       │
                 │  ObserverRange (startIndex, count)       │
                 │  UserData, ProduceChangeEvents           │
                 └──────────────────────────────────────────┘
                        │                        ▲
      modifier đọc      │                        │ khi stat này đổi giá trị,
      các stat khác ────┘                        │ mọi observer được recalculate
      (observed stats)                           │
```

- **Modifier** thuộc *về* stat mà nó ảnh hưởng (affected stat). Nó có thể **đọc** stat khác
  (observed stats) → tạo phụ thuộc.
- **Observer** là chiều ngược lại: stat A ghi vào `ObserverRange` của stat B danh sách
  "ai cần recalculate khi B đổi". Quan hệ này được duy trì tự động khi add/remove modifier.
- Recalculate một stat = `Stack.Reset(stat)` → chạy lần lượt mọi modifier trong
  `ModifierRange` (`modifier.Apply(reader, ref stack, out trigger)`) → `Stack.Apply(base, ref current)`
  → nếu `current` đổi thì phát `StatChangeEvent` và đẩy observer vào hàng đợi lan truyền.

Ba buffer, mỗi owner một bộ:

```
owner "Player"
  stats     : [ None ][ Hp ][ MoveSpeed ][ Armor ]        ← index 0 luôn là None stat
  modifiers : [ Hp.m0 ][ Hp.m1 ][ MoveSpeed.m0 ]          ← sắp xếp theo affected stat
  observers : [ obs-of-Hp ][ obs-of-Armor ][ obs-of-Armor ]← sắp xếp theo observed stat
                ▲                ▲
                │                └─ Armor.ObserverRange = (1, 2)
                └─ Hp.ObserverRange = (0, 1)
```

**Invariant sống-chết của cả hệ thống:** hai buffer `modifiers` / `observers` là *arena
chia khối*, khối của stat `i` phải nằm trước khối của stat `i+1`. Mọi `Insert`/`RemoveAt`
đều phải dịch `startIndex` của toàn bộ stat phía sau. Cả `StatAPI.AddStatAsObserverOfOtherStat`,
`TryAddStatModifier`, `TryRemoveStatModifier` đều làm đúng việc này — port phải giữ nguyên.

## 2.2 Layer

```
┌────────────────────────────────────────────────────────────────────┐
│ L4  Generated (per [StatSystem] / [StatCollection])                │
│     StatSystem.{Stat, ValuePair, StatModifier, Stack, StatObserver,│
│                 API, Reader, Accessor, Accessor.ReadOnly,          │
│                 Builder, WorldData, DeferredUpdate*Job}            │
│     Stats.{TypeId, Indices, StatIndices, StatHandles, Options,     │
│            Params, Builder<T>, Accessor<T>, Reader<T>}             │
└────────────────────────────────────────────────────────────────────┘
                                 ▲ alias / thin wrapper
┌────────────────────────────────────────────────────────────────────┐
│ L3  Algorithm (generic, hand-written)                              │
│     StatAPI (static)      — create/get/set, quan hệ observer        │
│     StatAccessor<6>       — mutation + lan truyền                   │
│     StatAccessor.ReadOnly — read path                               │
│     StatReader<2>         — read path cho modifier                  │
│     StatBuilder<6>        — authoring/khởi tạo                      │
│     StatWorldData<5>      — event + scratch                         │
│     DeferredUpdateStat*Job<6>                                       │
└────────────────────────────────────────────────────────────────────┘
                                 ▲
┌────────────────────────────────────────────────────────────────────┐
│ L2  Storage seam  ★ phần duy nhất khác EncosyTower                  │
│     StatOwnerHandle  — handle {index, version}                     │
│     StatBuffer<T>    — ≡ DynamicBuffer<T> trên UnsafeList<T>*      │
│     StatStore<3>     — owner slots, free-list, lookup              │
└────────────────────────────────────────────────────────────────────┘
                                 ▲
┌────────────────────────────────────────────────────────────────────┐
│ L1  Value & primitives                                             │
│     StatVariant / StatVariantType / StatDataSize / StatUserDataSize │
│     StatHandle / StatIndex / StatModifierHandle                     │
│     ModifierRange / ObserverRange / StatChangeEvent / ModifierTrigger│
│     EncosyTower.Common: ByteBool, Option<T>, HashValue, IIsValid    │
└────────────────────────────────────────────────────────────────────┘
```

## 2.3 L2 — Storage seam

### 2.3.1 `StatOwnerHandle`

```csharp
public readonly struct StatOwnerHandle : IEquatable<StatOwnerHandle>, IIsValid
{
    public static readonly StatOwnerHandle Null = default;

    public readonly int Index;    // slot trong StatStore
    public readonly int Version;   // > 0 khi sống; tăng mỗi lần slot được tái sử dụng

    public bool IsValid => Version > 0;
}
```

Thay thế `Entity` trong `StatHandle`:

```csharp
public struct StatHandle : IEquatable<StatHandle>, IIsValid
{
    public StatOwnerHandle owner;   // trước là: public Entity entity;
    public StatIndex index;
}
```

`StatHandle<TStatData>`, `StatModifierHandle` giữ nguyên hình dạng, chỉ đổi field.

### 2.3.2 `StatBuffer<T>`

```csharp
public unsafe struct StatBuffer<T> : IIsCreated
    where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    internal UnsafeList<T>* _list;

    public bool IsCreated { get; }
    public int Length { get; }
    public T this[int index] { get; set; }

    public ref T ElementAt(int index);
    public void Add(in T item);
    public void Insert(int index, in T item);
    public void RemoveAt(int index);
    public void Clear();

    public NativeArray<T> AsArray();               // alias view, không copy
    public Span<T> AsSpan();
    public ReadOnlySpan<T> AsReadOnlySpan();
}
```

Bề mặt này chọn đúng bằng **tập con của `DynamicBuffer<T>` mà thuật toán thực sự dùng**
(tôi đã soát toàn bộ `StatAPI.cs` + `StatAccessor.cs` + `StatBaker.cs`). Nhờ vậy port là
đổi tên kiểu, không phải viết lại logic.

Hợp đồng invalidation giống `DynamicBuffer<T>`: một `StatBuffer<T>` **hết hạn** khi
owner của nó bị destroy. Khác `DynamicBuffer<T>`: `Add`/`Insert` gây realloc **không**
làm hỏng `StatBuffer<T>` (vì header `UnsafeList<T>` có địa chỉ ổn định — xem dưới), nhưng
`ref T` lấy từ `ElementAt` thì hỏng — giống ECS. Quy tắc thực dụng: *không giữ `ref` qua
một lệnh mutate cấu trúc*.

### 2.3.3 `StatStore<TStat, TStatModifier, TStatObserver>`

> ⚠ **Đã đổi ở [I-12](07-DECISIONS.md#i-12--statownerslot-tách-thành-mảng-song-song-thêm-statbufferlookupt).**
> Slot dưới đây gom cả ba con trỏ nên không dựng được view generic-1-kiểu, khiến
> `StatReader<TValuePair, TStat>` không thể giữ 2 tham số kiểu. Bản thực tế tách thành
> `UnsafeList<StatOwnerSlot>` (version + counter) cộng ba mảng song song
> `UnsafeList<StatBufferSlot<T>>`. Mọi thứ khác trong mục này — kể cả lý do cấp phát header rời
> và slot pooling — **không đổi**.

```csharp
internal unsafe struct StatOwnerSlot
{
    public int version;                        // âm/0 = slot rỗng
    public uint modifierIdCounter;             // trước là StatOwner : IComponentData
    public UnsafeList<TStat>* stats;
    public UnsafeList<TStatModifier>* modifiers;
    public UnsafeList<TStatObserver>* observers;
}

public unsafe struct StatStore<TStat, TStatModifier, TStatObserver>
    : IDisposable, INativeDisposable, IIsCreated
    where TStat : unmanaged
    where TStatModifier : unmanaged
    where TStatObserver : unmanaged
{
    // nội bộ: UnsafeList<StatOwnerSlot> _slots; UnsafeList<int> _freeSlots;
    //         AllocatorManager.AllocatorHandle _allocator;

    public StatOwnerHandle CreateOwner(int statCapacity, int modifierCapacity, int observerCapacity);
    public bool DestroyOwner(StatOwnerHandle owner);
    public bool Exists(StatOwnerHandle owner);

    public bool TryGetStats(StatOwnerHandle owner, out StatBuffer<TStat> buffer);
    public bool TryGetModifiers(StatOwnerHandle owner, out StatBuffer<TStatModifier> buffer);
    public bool TryGetObservers(StatOwnerHandle owner, out StatBuffer<TStatObserver> buffer);
    public bool TryGetBuffers(StatOwnerHandle owner, out StatBuffer<TStat> stats
        , out StatBuffer<TStatModifier> modifiers, out StatBuffer<TStatObserver> observers);

    internal bool TryGetSlotRef(StatOwnerHandle owner, out /* ref */ StatOwnerSlot* slot);
}
```

**Tại sao ba `UnsafeList<T>*` cấp phát rời thay vì `UnsafeList<T>` nhúng trong slot:**
`ElementAt` trả `ref`, `Insert`/`RemoveAt` mutate *header* (`Length`, `Ptr`). Nếu header nằm
trong `_slots` thì mỗi lần `CreateOwner` làm `_slots` realloc → mọi `StatBuffer<T>` đang
sống thành dangling. Cấp phát header rời (`UnsafeList<T>.Create(cap, alloc)`) cho địa chỉ
ổn định suốt đời owner. Giá phải trả: 3 allocation/owner.

**Giảm giá bằng slot pooling:** `DestroyOwner` **không** free ba list — chỉ `Clear()` và
đẩy slot index vào `_freeSlots`, `version` bị vô hiệu hoá. `CreateOwner` lấy slot cũ →
tái dùng capacity đã có → sau warm-up, spawn/despawn không alloc. Chỉ `Dispose()` mới
free thật.

**Thay `TryGetBuffer` bằng gì:** mọi chỗ bản gốc viết

```csharp
if (lookupStats.TryGetBuffer(entity, out var statBuffer) == false
    || lookupModifiers.TryGetBuffer(entity, out var modifierBuffer) == false
    || lookupObservers.TryGetBuffer(entity, out var observerBuffer) == false)
{ return false; }
```

port thành một lệnh:

```csharp
if (_store.TryGetBuffers(owner, out var statBuffer, out var modifierBuffer, out var observerBuffer) == false)
{ return false; }
```

→ `StatAccessor` mất 4 field lookup, còn **1 field `StatStore`**, và mất luôn
`Update(ref SystemState)`.

### 2.3.4 Alternative đã cân nhắc và loại

| Phương án | Vì sao loại |
|---|---|
| **B — Arena toàn cục + range per owner** (một `UnsafeList<TStat>` cho mọi owner) | `Insert` một modifier phải dịch `startIndex` của *mọi stat của mọi owner* → O(N toàn world) mỗi lần add modifier. Bản gốc chỉ O(số stat của 1 entity). |
| **C — Fixed-capacity block per owner** (`FixedList512Bytes`-style) | Chặn cứng số stat/modifier; hỏng ngay khi buff stack sâu. Premature. |
| **D — `NativeList<UnsafeList<T>>`** (header nhúng) | Dangling như đã phân tích ở §2.3.3. |
| **E — Giữ `DynamicBuffer<T>` bằng cách ref `Unity.Entities`** | Vi phạm mục tiêu chính. |

## 2.4 Thuật toán update (port nguyên văn)

### 2.4.1 `UpdateSingleStatCommon` — hạt nhân

Bản gốc: `StatAPI.cs:386`. Trình tự:

1. `initialStat = statRef` (snapshot để so sánh).
2. `ref stack = ref worldData._modifierStackRef` → `stack.Reset(statRef)`.
3. Duyệt `[ModifierRange.startIndex, ExclusiveEnd)` trong `modifierBuffer`,
   gọi `modifierRef.Apply(statsReader, ref stack, out addTriggerEvent)`
   **bằng ref** — modifier được phép tự mutate state của chính nó (ví dụ đếm thời gian).
   Nếu `addTriggerEvent` → `worldData.AddModifierTriggerEvent(...)`.
4. `stack.Apply(baseValue, ref currentValue)` → `statRef.ValuePair = composer.Compose(isPair, base, current)`.
5. Nếu `ValuePair` **không đổi** → `return` (cắt lan truyền, đây là điểm chống bùng nổ chính).
6. Nếu `ProduceChangeEvents` → push `StatChangeEvent { statHandle, prevValue, newValue }`.
7. Duyệt `ObserverRange`: observer **cùng owner** → `_tmpSameEntityUpdatedStats`,
   observer **khác owner** → `_tmpGlobalUpdatedStats`.

Chia hai hàng đợi để tận dụng ba buffer đang có trong tay (không lookup lại) — port giữ.

### 2.4.2 `TryUpdateStat` — lan truyền toàn cục

Bản gốc: `StatAccessor.cs:1266`. BFS trên `_tmpGlobalUpdatedStats`. Bên trong mỗi bước, xử lý
hết `_tmpSameEntityUpdatedStats` bằng buffer đang mở.

Bản gốc còn có `_tmpVisitedObserverHandles` để mỗi stat chỉ recalculate một lần mỗi pass.
**Port đã bỏ** (task 4.5). Một stat quay lại worklist không phải lãng phí — trên DAG có hai
nhánh dài ngắn khác nhau đổ về cùng một điểm, lượt thứ hai chính là lượt **sửa** giá trị đã
tính từ nhánh dài còn cũ. Visited-set chặn đúng lượt sửa đó và để lại giá trị sai vĩnh viễn.
Số đo đầy đủ ở [DEC-005](07-DECISIONS.md#dec-005).

Điều kiện dừng thật nằm ở bước 5 của §2.4.1 (`ValuePair` không đổi → cắt lan truyền), không
nằm ở visited-set. Chu trình thì bị chặn từ lúc `TryAddStatModifier` nên vòng lặp luôn kết thúc.

### 2.4.3 `UpdateStatRef` — sau khi set giá trị

Bản gốc: `StatAccessor.cs:1427`. Cùng cấu trúc, nhưng bắt đầu từ một `ref TStat` đã có.
Không có visited set — và sau task 4.5 thì `TryUpdateStat` cũng vậy, nên hai lối lan truyền
hành xử giống hệt nhau. D-01 khép lại.

### 2.4.4 `TryAddStatModifier`

Bản gốc: `StatAccessor.cs:1770`. Bảy bước, port nguyên văn:

1. `modifier.AddObservedStatsToList(tmp)`.
2. **Validate reachability**: mọi observed stat phải tồn tại → không thì fail *trước khi*
   thay đổi gì (tránh modifier chết).
3. `slot.modifierIdCounter++` → `modifier.Id` (id **local theo owner**).
4. **Loop detection**: nếu `affectedStat` nằm trong observed stats → từ chối. Sau đó đi
   theo chuỗi observer của affected stat; khi gặp một observer trùng observed stat của
   modifier thì "giả lập" thêm affected stat vào chuỗi; nếu bắt gặp lại affected stat →
   từ chối. Có `_tmpVisitedObserverHandles` chống expand lặp.
5. `modifierBuffer.Insert(range.ExclusiveEnd, modifier)`; `range.count++`; dịch
   `startIndex` của mọi stat phía sau — **trừ khi** `deferRangeShift`.
6. `AddStatAsObserverOfOtherStat` cho từng observed stat (cũng insert + dịch `startIndex`).
7. `recalculateStat` → `TryUpdateStat` (hoặc `TryUpdateStatAssumeSingleEntity`).

`TryAddStatModifiersBatch` truyền `deferRangeShift: true, recalculateStat: false` cho mọi
phần tử, dịch range **một lần** ở cuối rồi update một lần. Comment cảnh báo trong bản gốc
(`StatAccessor.cs:1673`) phải được copy nguyên vào bản port: trong lúc defer, `startIndex`
của các stat phía sau là **stale**; chỉ an toàn vì không có gì trong batch đọc modifier
range của stat khác.

### 2.4.5 `TryRemoveStatModifier`

Bản gốc: `StatAccessor.cs:2055`. Tìm modifier theo `Id` trong range → `RemoveAt` → `count--`
→ dịch `startIndex` phía sau → với mỗi observed stat, xoá **một** entry observer trỏ về
affected stat (`break` sau lần đầu — cố ý, vì nhiều modifier có thể cùng observe) → dịch
`ObserverRange.startIndex` phía sau → `TryUpdateStat`.

## 2.5 `StatWorldData` — event & scratch

Port nguyên văn (`StatWorldData.cs`), chỉ đổi `Entity` → `StatOwnerHandle`:

```csharp
NativeList<StatChangeEvent<TValuePair>>          _statChangeEvents;
NativeList<ModifierTriggerEvent<...>>            _modifierTriggerEvents;
NativeList<StatHandle>                           _tmpModifierObservedStats;
NativeList<TStatObserver>                        _tmpStatObservers;
NativeList<StatHandle>                           _tmpGlobalUpdatedStats;
NativeList<StatHandle>                           _tmpSameEntityUpdatedStats;
NativeHashSet<StatHandle>                        _tmpVisitedObserverHandles;
NativeReference<TStatModifierStack>              _modifierStackRef;
```

**Hợp đồng (giữ nguyên, phải ghi lại trong XML doc):**

- Không thread-safe, không reentrant. Mọi hàm mutate của `StatAccessor` đều `Clear()` rồi
  tái dùng các scratch list này.
- Không share một `StatWorldData` giữa hai job song song.
- Không gọi hàm mutate trong lúc đang iterate event list.
- Một `StatWorldData` cho một luồng update đơn.

## 2.6 Threading, Burst, Jobs

| Khả năng | Trạng thái |
|---|---|
| Đọc song song (`Accessor.ReadOnly`, `Reader`) | ✅ nhiều job đọc cùng lúc, miễn không ai ghi |
| Ghi song song | ❌ theo thiết kế — như bản gốc |
| Ghi trong `IJob` đơn luồng | ✅ pattern chính |
| Thu thập song song → apply đơn luồng | ✅ `DeferredUpdateStat{List,Queue,Stream}Job` |
| Burst | ✅ mọi struct blittable; codegen emit job **cụ thể** nên Burst compile được |

Safety: `StatStore` và `StatBuffer<T>` mang `[NativeContainer]`-style guard dưới
`ENABLE_UNITY_COLLECTIONS_CHECKS` (`AtomicSafetyHandle` + `CheckReadAndThrow` /
`CheckWriteAndBumpSecondaryVersion`), theo đúng khuôn ở
[CODING-CONVENTIONS.md §13](../../../../CODING-CONVENTIONS.md).
`[NativeDisableUnsafePtrRestriction]` trên field pointer để job system chấp nhận.

Vì không có `SystemState`/dependency graph, **project tự quản `JobHandle`**. Doc phase 3
sẽ có mục "how to schedule" mô tả 3 pattern: sync tại chỗ, một update job/frame,
producer song song + consumer đơn luồng.

## 2.7 Lifecycle & an toàn handle

| Tình huống | Bản gốc (ECS) | Bản port |
|---|---|---|
| Destroy chủ thể | `EntityManager.DestroyEntity` — **không** tự update observer (TODO trong bản gốc) | `DestroyOwner` cũng không tự update; nhưng có `TryGetAllObservers(owner, list)` để gọi trước, và **bắt buộc** ghi rõ trong doc |
| Handle của chủ thể đã chết | `TryGetBuffer` fail → `continue` (im lặng) | `version` lệch → fail → `continue` (im lặng, nhưng **có thể** log dev warning dưới `APEXION_STATS_RUNTIME_CHECKS`) |
| Observer trỏ tới stat đã chết | bị bỏ qua khi lan truyền | như trên |
| Index recycling | `Entity.Version` | `StatOwnerHandle.Version` — cùng cơ chế |

Phase 2 sẽ thêm `DestroyOwnerAndUpdateObservers(owner, ref worldData)` như một tiện ích
gói sẵn: snapshot observer khác owner → destroy → `TryUpdateStat` từng cái.

## 2.8 Validation

Copy khuôn EncosyTower nhưng đổi symbol sang tiền tố riêng:

```csharp
// ApexionGame.Entities.Stats/Debugging/ValidationDefines.cs
public const string UNITY_EDITOR   = "UNITY_EDITOR";
public const string DEBUG          = "DEBUG";
public const string RUNTIME_CHECKS = "APEXION_RUNTIME_CHECKS";
public const string STATS_CHECKS   = "APEXION_STATS_RUNTIME_CHECKS";
```

File có nhánh validation inline dùng block:

```csharp
#if !(UNITY_EDITOR || DEBUG || APEXION_RUNTIME_CHECKS || APEXION_STATS_RUNTIME_CHECKS) || DISABLE_APEXION_CHECKS
#define __APEXION_NO_VALIDATION__
#else
#define __APEXION_VALIDATION__
#endif
```

`ThrowHelper` port từ `Common/ThrowHelper.cs` (206 LOC): `ThrowIfStatWorldDataIsNotCreated`,
`ThrowIfPairsMismatch`, `ThrowIfUnsupportedType`, `ThrowIfMismatchedOperatorTypes`,
`OperatorException`, `UnaryOperatorException`, `StatVariantTypeException` — cộng thêm
`ThrowIfStoreIsNotCreated`, `ThrowIfOwnerIsDead`.

## 2.9 Persistence

Trạng thái đầy đủ của một owner = 3 buffer + `modifierIdCounter`. Tất cả blittable
(`Stat` chứa `StatDataStore` là union `[StructLayout(Explicit)]`, `StatModifier` do user
định nghĩa nhưng buộc `unmanaged`).

API dự kiến (implement phase 4, nhưng **contract chốt từ phase 1**):

```csharp
public bool TryCopyOwnerTo(StatOwnerHandle owner, NativeList<TStat> stats
    , NativeList<TStatModifier> modifiers, NativeList<TStatObserver> observers, out uint modifierIdCounter);

public bool TryRestoreOwner(ReadOnlySpan<TStat> stats, ReadOnlySpan<TStatModifier> modifiers
    , ReadOnlySpan<TStatObserver> observers, uint modifierIdCounter, out StatOwnerHandle owner);
```

⚠ `StatHandle` **chứa `StatOwnerHandle.Index/Version`** → không serialize thô được giữa các
session. Cần remap qua 3 bước (DEC-004 = A, đã chốt):

1. Restore từng owner → thu `StatOwnerRemap` (`NativeHashMap<StatOwnerHandle, StatOwnerHandle>`)
   ánh xạ handle cũ → handle mới. `StatIndex` **không** đổi (thứ tự stat trong buffer được
   giữ nguyên khi restore).
2. Fixup `StatObserver.ObserverHandle` — store tự làm được, vì `IStatObserver` expose
   property đó.
3. Fixup observed stats **bên trong `TStatModifier`** — store không biết field nào chứa
   `StatHandle`, nên `IStatModifier` có thêm:

   ```csharp
   void RemapObservedStats(in StatOwnerRemap remap);
   ```

   Đây là **thay đổi contract so với bản gốc**: modifier viết cho bản EncosyTower không copy
   nguyên văn sang được, phải bổ sung method này. `StatSystemGenerator` sinh implement gọi
   `partial void OnRemapObservedStats(in StatOwnerRemap remap)` để user điền — đối xứng với
   cách bản gốc để user tự viết `AddObservedStatsToList`.

Handle nào không remap được (owner không có trong save) → `StatHandle.Null`; lan truyền sẽ
bỏ qua an toàn (§2.7).
