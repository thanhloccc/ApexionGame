# Khai báo stat

*[English](../03-DECLARING-STATS.md) · [Mục lục hướng dẫn](README.md)*

Ba attribute điều khiển toàn bộ codegen. Mọi thứ bạn gọi lúc runtime đều ra từ chúng.

| Attribute | Đặt lên | Sinh ra |
|---|---|---|
| `[StatSystem]` | một `static partial class` (hoặc `partial struct`) | các type lưu trữ, accessor, reader, builder, world data, job |
| `[StatCollection]` | một `partial struct` | bộ handle có kiểu, builder, accessor, reader cho một nhóm stat |
| `[StatData]` | một `partial struct` lồng trong collection | một stat có kiểu |

## `[StatSystem]`

```csharp
[StatSystem(StatDataSize.Size8)]
public static partial class RpgStatSystem { }

// hoặc, với trường type-id rộng hơn:
[StatSystem(StatDataSize.Size8, StatUserDataSize.Size2)]
public static partial class RpgStatSystem { }
```

Một stat system định nghĩa **bộ type cho đúng một store**. Mọi collection trỏ về cùng một
`[StatSystem]` chia sẻ một `StatStore`, và đó chính là thứ cho phép modifier bắc từ collection này sang
collection kia. Hai system nghĩa là hai store và không có cạnh nào giữa chúng.

| Tham số | Mặc định | Nghĩa |
|---|---|---|
| `maxDataSize` | bắt buộc | số byte dành cho giá trị của một stat |
| `maxUserDataSize` | `Size1` | độ rộng của `Stat.UserData` — `byte`, `ushort` hoặc `uint` |

`maxUserDataSize` chặn số stat type mà cả system có thể định danh. `Size1` cho 255 type-id trên mọi
collection; nếu seed của các `[StatCollection]` cách xa nhau (2100, 2200, …) thì bạn cần `Size2` hoặc
`Size4`.

### Kiểu giá trị và ngân sách size

`StatVariant` là union 19 thành viên — 18 kiểu giá trị cụ thể cộng `None` — với explicit layout:

| Byte | Kiểu |
|---|---|
| 1 | `bool`, `sbyte`, `byte`, enum có underlying type 1 byte |
| 2 | `short`, `ushort`, `half` |
| 4 | `int`, `uint`, `float`, `half2` |
| 6 | `half3` |
| 8 | `long`, `ulong`, `half4`, `float2`, `double` |
| 12 | `float3` |
| 16 | `float4` |

`StatDataSize` nhận `Size2`, `Size4`, `Size6`, `Size8`, `Size12`, `Size16`.

**Một stat lưu cả base *và* current trong ngân sách đó.** Nên quy tắc là:

| Size kiểu so với `maxDataSize` | Dùng được? |
|---|---|
| `size <= maxDataSize / 2` | được, ở chế độ value pair — trường hợp thường |
| `maxDataSize / 2 < size <= maxDataSize` | chỉ với `SingleValue = true` |
| `size > maxDataSize` | không |

Với `Size8`, `float` và `int` là cặp; `double` và `float2` cần `SingleValue = true`; `float4` cần
`Size16` và chỉ dùng được ở chế độ single. Analyzer báo `AGS_STAT_DATA_0005` kèm số cụ thể, nên cứ
chọn một size rồi để nó sửa bạn.

Lớn hơn không miễn phí: size của union là *mỗi stat*, và nó là thứ chi phối bộ nhớ của store. Size8
phủ phần lớn stat gameplay.

### `[StatSystem]` sinh ra gì

Bên trong class của bạn:

| Type | Vai trò |
|---|---|
| `Stat : IStat<ValuePair>` | struct stat cụ thể — value pair, range, user data, cờ |
| `Stat<TStatData>` | như trên, phantom-typed |
| `ValuePair : IStatValuePair` | `{ union dữ liệu; StatVariantType type; bool isPair; }` |
| `ValuePair.Composer` | dựng value pair; có hook `partial void OnCompose(...)` để clamp hoặc round |
| `StatModifier` (partial) | **bạn hoàn thiện** — xem dưới |
| `StatModifier.Stack` (partial) | **bạn hoàn thiện** |
| `StatObserver : IStatObserver` | bản ghi cạnh ngược |
| `API` | facade không generic quanh `StatAPI` |
| `Reader`, `Accessor`, `Accessor.ReadOnly`, `Builder`, `WorldData` | facade không generic quanh các generic L3 |
| `ModifierTriggerEvent`, `StatModifierRecord` | alias event cụ thể |
| `DeferredUpdateStat{List,Queue,Stream}Job` | `[BurstCompile]` |
| `IsCompatible(StatVariantType, bool isPair)` | bảng kiểu compile-time, tra được lúc runtime |

Để ý cái **không** được sinh: store. Bạn tự khởi tạo
`StatStore<RpgStatSystem.Stat, RpgStatSystem.StatModifier, RpgStatSystem.StatObserver>`.

## `[StatCollection]` và `[StatData]`

```csharp
[StatCollection(typeof(RpgStatSystem), 2100)]
public partial struct RpgStats
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct Attack { }
    [StatData(typeof(DirectionType))] public partial struct Direction { }
    [StatData(StatVariantType.Double, SingleValue = true)] public partial struct Score { }
}
```

`[StatData]` nhận một `StatVariantType` hoặc `typeof(SomeEnum)`. `SingleValue = true` chỉ lưu current
value, giảm nửa chỗ một stat cần.

Tham số thứ hai của `[StatCollection]` là **seed type-id**. Nó dịch id của collection này bên trong
`Stat.UserData` để hai collection không đè nhau — nhờ đó lấy một `Stat` trơ và hỏi được nó ra từ khai
báo nào. Cho mỗi collection một số riêng và chừa khoảng: id chạy từ seed lên, một id một `[StatData]`.

Struct lồng không có `[StatData]` không sinh ra stat nào, và **cảnh báo**
(`AGS_STAT_COLLECTION_0005`) thay vì biến mất trong im lặng.

### Một collection sinh ra gì

| Thành viên | Dùng để |
|---|---|
| `enum Type` | một member mỗi stat, tên đúng như struct của bạn |
| `TypeId` | `EncodeToStatUserData` / `DecodeFromStatUserData` / `ValidateStatUserData` |
| `Index`, `Index<TStatData>` | vị trí trong collection, chuyển được sang `StatIndex` |
| `Indices`, `StatIndices`, `StatHandles` | bộ cố định, enumerate bằng span, kèm `ToRecords(...)` |
| `<Stat>.Params.Create(...)` | tham số stat thân thiện authoring, xem dưới |
| `Options.Data`, `Options.ProduceChangeEvents` | bundle tuỳ chọn theo stat cho các hàm batch |
| `Builder`, `Builder<T>` | tạo owner |
| `Accessor`, `Accessor<T>` | đọc/ghi gắn với một owner |
| `Reader`, `Reader<T>` | chỉ đọc, trên một stat buffer |
| `<Collection>Extensions` | `ToComponent<T>`, `TryGetValuePair`, … |

`Params.Create` có tới ba overload mỗi stat:

```csharp
RpgStats.Hp.Params.Create(100f);                       // base = current = 100
RpgStats.Hp.Params.Create(100f, 80f);                  // base 100, current 80
RpgStats.Hp.Params.Create(produceChangeEvents: true);  // giá trị mặc định, chỉ set cờ
```

Mỗi cái mang sẵn type-id đã encode, nên `Builder.CreateStat(...)` không cần gì thêm.

`StatHandles` là thứ bạn cất. Nó là một struct nhỏ chứa handle, một field mỗi stat, tên theo khai báo
của bạn ở dạng camelCase:

```csharp
var stats   = RpgStats.Builder.Build(ref store, out var owner).CreateAllStats().ToStats();
var handles = stats.GetStatHandles(owner);

handles.hp;        // StatHandle
handles.attack;    // StatHandle
```

## Bảy hook

Generator viết phần nối interface của `StatModifier` và để lại một `partial void ...Internal` cho mỗi
quyết định nó không làm hộ được. Có bảy cái, và cả chữ ký chính xác lẫn tính `readonly` đều quan trọng.

| Hook | Trên | `readonly` | Bạn viết gì |
|---|---|---|---|
| `GetIdInternal(ref uint id)` | `StatModifier` | có | trả lại id bạn lưu |
| `SetIdInternal(uint value)` | `StatModifier` | không | lưu id runtime gán |
| `AddObservedStatsToListInternal(NativeList<StatHandle>)` | `StatModifier` | có | **khai báo mọi stat modifier này đọc** |
| `ApplyInternal(Reader, ref Stack, ref bool)` | `StatModifier` | không | đóng góp vào stack |
| `RemapObservedStatsInternal(in StatOwnerRemap)` | `StatModifier` | không | viết lại handle đã lưu sau khi load |
| `ResetInternal(in Stat)` | `Stack` | không | đặt phần tử đơn vị |
| `ApplyInternal(in StatVariant, ref StatVariant)` | `Stack` | không | kết hợp base value và stack thành current value |

> **Đừng gõ từ ký ức.** Đặt con trỏ vào `partial class`, bấm **Ctrl+.** rồi chọn *Generate stat
> modifier skeleton* và điền vào. Một chữ `readonly` sai chỗ làm compiler không ghép partial method —
> lặng lẽ, không diagnostic nào, và hook đơn giản là không bao giờ chạy.

### `AddObservedStatsToListInternal` là hợp đồng

Đây là method quan trọng nhất trong cách codebase của bạn dùng thư viện này. Nó là cách runtime biết
phải dựng cạnh nào.

```csharp
readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observedStatHandles)
{
    if (kind == Kind.AddFromStat)
    {
        observedStatHandles.Add(observedStat);
    }
}
```

Bỏ sót một handle mà modifier thật sự đọc thì stat phụ thuộc **không bao giờ** được tính lại khi nguồn
đó đổi. Giá trị cũ chỉ nằm im. Không exception, không warning, không dòng log nào. Đây là loại bug khó
nhất mà hệ này sinh ra, và cách phòng duy nhất là bổ sung method này mỗi lần bạn thêm một field
`StatHandle`.

### `RemapObservedStatsInternal` và lưu/tải

`StatOwnerHandle` là chỉ số slot, nên handle lưu bên trong một modifier mất nghĩa sau một vòng
save/load. Hook này là nơi bạn viết lại chúng:

```csharp
partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
{
    if (kind == Kind.AddFromStat)
    {
        observedStat = remap.RemapOrNull(observedStat);
    }
}
```

Một analyzer cảnh báo (`AGS_STAT_DATA_0006`) nếu modifier của bạn có field `StatHandle` mà không
implement hook này. Method này **mới có ở bản port** — modifier viết cho bản gốc sẽ không compile cho
tới khi bạn thêm nó. Toàn bộ luồng ở [Lưu / tải](05-PERSISTENCE.md).

### `ResetInternal` và phần tử đơn vị

```csharp
partial void ResetInternal(in Stat stat)
{
    var type = stat.ValuePair.Type;
    add = type.ZeroVariant();
    multiply = type.OneVariant();
}
```

Đọc phần tử đơn vị từ kiểu của stat thay vì viết `new StatVariant(0f)` / `(1f)`. Một stack chốt cứng
`float` sinh ra lệch kiểu âm thầm vào ngày có người khai một stat `int`.

### Thứ tự stack là quyết định thiết kế của bạn

```csharp
partial void ApplyInternal(in StatVariant baseValue, ref StatVariant currentValue)
    => currentValue = (baseValue + add) * multiply;
```

`(base + add) * multiply` nghĩa là bonus phẳng vào trước phần trăm — quy ước RPG thông thường, và là
thứ sample dùng. Runtime không áp đặt điều đó; đổi thứ tự chỉ cách một dòng, và từng modifier không cần
biết.

## Chẩn đoán

Mười sáu mã chẩn đoán đi kèm generator: mười ba cho lỗi khai báo, ba báo generator lỗi nội bộ thay vì
sinh ra code hỏng. Bảng đầy đủ ở [API reference](08-API-REFERENCE.md#chẩn-đoán).

Hai cái bạn gặp nhiều nhất:

| Mã | Nghĩa |
|---|---|
| `AGS_STAT_DATA_0005` | kiểu giá trị không vừa — tăng `maxDataSize` hoặc đặt `SingleValue = true` |
| `AGS_STAT_SYSTEM_0002` | assembly dùng `[StatSystem]` nhưng không tham chiếu `EncosyTower.Core` |

## Quick Actions

Ba refactoring, đặt con trỏ trong `partial class` hoặc `partial struct` (**Ctrl+.**):

| Hành động | Sinh ra |
|---|---|
| *Make this a stat system* | attribute `[StatSystem]` |
| *Generate stat modifier skeleton* | `StatModifier` + `Stack` với đủ bảy hook, chữ ký và `readonly` đúng |
| *Generate stat collection skeleton* | `[StatCollection]` với seed type-id không đụng số đã dùng trong project |

Cái giữa đáng dùng nhất. Hai cái còn lại đỡ gõ.
