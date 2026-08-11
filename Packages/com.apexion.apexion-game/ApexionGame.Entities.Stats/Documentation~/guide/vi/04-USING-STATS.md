# Dùng stat

*[English](../04-USING-STATS.md) · [Mục lục hướng dẫn](README.md)*

Hai đường tới cùng một runtime, và bạn sẽ dùng cả hai:

| | Tầm với | Dùng cho |
|---|---|---|
| **Accessor của collection** — `RpgStats.Accessor.Create(owner, stats, accessor, worldData)` | một owner, một collection | gọi có kiểu, nối chuỗi được; code setup |
| **Accessor của system** — `RpgStatSystem.Accessor` | mọi owner trong store | modifier, việc chéo owner, lan truyền |

Accessor của collection là wrapper có kiểu, mỏng, *nằm trên* accessor của system. Mọi thứ nó làm được
thì accessor của system cũng làm được với một `StatHandle` tường minh.

## Tạo owner

```csharp
var stats = RpgStats.Builder
    .Build(ref store, out var owner)
    .CreateAllStats(produceChangeEvents: true)
    .ToStats();

var handles = stats.GetStatHandles(owner);
```

`Build` cấp slot owner **và gieo None stat ở index 0**, đó là lý do bạn đi qua nó chứ không phải
`store.CreateOwner()`.

`ToStats()` trả về một struct nhỏ chứa các `StatIndex` — không pointer, không alloc. Cất ở đâu tuỳ
project: field của class nhân vật, `NativeList<RpgStats>`, hay dictionary theo entity id của bạn. Ghép
với owner handle là ra `StatHandles` bất cứ lúc nào.

Dựng chi tiết hơn, nếu bạn không muốn tạo hết mọi stat:

```csharp
var stats = RpgStats.Builder
    .Build(ref store, out var owner)
    .CreateStat(RpgStats.Hp.Params.Create(100f, produceChangeEvents: true))
    .CreateStat(RpgStats.Attack.Params.Create(10f))
    .ToStats();
```

Builder còn có `CreateStats(...)` cho nhiều stat một lượt, `SetStat` / `SetOrCreateStat` để author lại,
và `Reset(composer)` để làm lại từ đầu trên cùng owner.

**`produceChangeEvents` là theo từng stat và mặc định tắt.** Stat tắt cờ này vẫn lan truyền bình
thường — nó chỉ không đẩy `StatChangeEvent`. Chỉ bật cho stat mà có thứ gì đó thật sự lắng nghe; đây là
tối ưu rẻ nhất ở đây.

## Đọc

```csharp
// có kiểu, qua accessor của collection
var access = RpgStats.Accessor.Create(owner, stats, accessor, worldData);
access.TryGetStatData(out RpgStats.Hp hp, out _, out _);
float current = hp.currentValue;

// không kiểu, qua accessor của system
if (accessor.TryGetStatValue(handles.hp, out var pair))
{
    float value = pair.GetCurrentValueOrDefault().Float;
}
```

Mỗi struct `[StatData]` mang cả hai nửa — `baseValue` và `currentValue` — nên một lần đọc cho bạn cả
giá trị đã author và giá trị modifier tạo ra.

Với đường chỉ đọc, lấy một view. Nó chỉ expose getter, và nhiều view dùng song song được miễn không ai
ghi:

```csharp
var view = accessor.AsReadOnly();
view.TryGetStatValue(handles.attack, out var attack);
```

`accessor.GetStats(owner)` trả `NativeArray<Stat>.ReadOnly` của cả owner — nhớ index 0 là None stat.

## Ghi

```csharp
accessor.TrySetStatBaseValue(handles.hp, new RpgStatSystem.ValuePair(new StatVariant(120f)), ref worldData);
```

Lệnh ghi set base value, tính lại stat đó, và lan truyền tới mọi thứ phụ thuộc **trước khi** trả về.
Không có bước "commit", không có flush hoãn.

Qua accessor của collection thì cùng lệnh ghi đó có kiểu và nối chuỗi được:

```csharp
RpgStats.Accessor.Create(owner, stats, accessor, worldData)
    .TrySetStatBaseValue(new RpgStats.Hp(120f), out _)
    .TrySetStatBaseValue(new RpgStats.Attack(14f), out _);
```

Để ý hình dạng: giá trị trả về là chính accessor nên nối chuỗi được, và trạng thái thành công đi ra
bằng `out bool`. `out _` là thứ bình thường nên viết khi một lần fail nghĩa là bug ở chỗ khác.

Các lệnh ghi khác trên accessor của system:

| Method | |
|---|---|
| `TrySetStatCurrentValue` | ghi đè giá trị đã tính — lượt tính lại kế tiếp ghi đè lại |
| `TrySetStatValues` / `TrySetStatData` | cả hai nửa một lượt |
| `TrySetStatProduceChangeEvents` | bật/tắt cờ event của stat |
| `TrySetStatUserData` | ghi lại payload type-id |
| `TryUpdateStat(handle, ref worldData)` | tính lại theo yêu cầu rồi lan truyền |
| `TryUpdateAllStats(owner, ref worldData)` | tính lại mọi stat của một owner |

`TryUpdateStat` là thứ bạn gọi khi một input đổi mà đồ thị không biết — một modifier đọc thời gian
thật, hoặc một owner bị observe đã bị destroy.

### Ghi theo batch

Ba hàm batch trên accessor của system nhận nhiều handle một lượt, tự gom theo owner, và lan truyền một
lần:

```csharp
accessor.TrySetBaseValueToStats(paramsForStats, results, ref worldData, Allocator.TempJob);
```

`TrySetCurrentValueToStats` và `TrySetDataToStats` giống vậy. Tham số `Allocator` ở cuối mặc định là
`Allocator.Temp`; truyền khác đi khi ở trong job dài, nơi `Temp` không phù hợp. Cả hai container nội bộ
đều được dispose trước khi trả về, nên allocator nào cũng an toàn.

Accessor của collection expose đúng việc đó nhưng lái bằng một bundle tuỳ chọn, đọc dễ hơn trong code
setup:

```csharp
RpgStats.Accessor.Create(owner, stats, accessor, worldData)
    .TrySetBaseValueToStats(new RpgStats.Options.Data(
          hp: new RpgStats.Hp(100f)
        , attack: new RpgStats.Attack(10f)
    ));
```

Tham số của `Options.Data` là `Option<TStatData>` — chính struct stat, **không** phải
`StatDataParams`. `Params.Create` thuộc về `Builder.CreateStat` / `SetStat`, vì mấy hàm đó nhận cả cờ
và handle chứ không chỉ giá trị. Bỏ trống một tham số ở đây là `Option.None`, nên stat giữ nguyên giá
trị cũ chứ không bị ghi về 0.

Còn một overload nhận từng tham số kiểu `Option<float>` (hay kiểu giá trị của stat) nếu bạn không muốn
gọi tên type:

```csharp
RpgStats.Accessor.Create(owner, stats, accessor, worldData)
    .TrySetBaseValueToStats(hp: 100f, attack: 10f);
```

## Modifier

```csharp
accessor.TryAddStatModifier(
      handles.attack                                          // stat bị ảnh hưởng
    , RpgStatSystem.StatModifier.AddFrom(aura.attackBonus)    // struct modifier của bạn
    , out var modifierHandle
    , ref worldData
);
```

Bên trong xảy ra gì, theo thứ tự:

1. `AddObservedStatsToListInternal` của bạn được gọi để biết modifier đọc gì.
2. Mọi observed stat được kiểm tra tồn tại — thiếu một cái là call fail **trước khi mutate bất cứ
   gì**. Không có modifier nửa vời.
3. Modifier nhận một id, cục bộ theo owner.
4. Chuỗi observer được duyệt; nếu modifier khép vòng, call trả `false` và không gì đổi.
5. Modifier được insert vào khối modifier của owner và range phía sau dịch theo.
6. Cạnh ngược được ghi cho từng observed stat.
7. Stat bị ảnh hưởng được tính lại, và thay đổi lan truyền.

Nên `false` nghĩa là một trong ba: stat bị ảnh hưởng không tồn tại, một observed stat không tồn tại,
hoặc modifier tạo vòng. Cả ba trường hợp store không bị đụng tới.

Gỡ:

```csharp
accessor.TryRemoveStatModifier(modifierHandle, ref worldData);    // một cái
accessor.TryRemoveModifiersOfStat(handles.attack, ref worldData); // hết trên một stat
```

Gỡ cũng lan truyền y như thêm.

### Thêm theo batch

`TryAddStatModifiersBatch` thêm nhiều cái một lượt, hoãn việc dịch range và chỉ update một lần ở cuối.
Nhanh hơn đáng kể khi dựng một nhân vật từ cả tá buff. Ràng buộc thừa hưởng từ bản gốc và đáng biết:
trong lúc hoãn dịch, `startIndex` của các stat phía sau điểm insert là **cũ**, nên không gì trong batch
được phép đọc modifier range của stat khác. Bản thân API batch không đọc, và đó là lý do việc này an
toàn.

### Soi

```csharp
accessor.TryGetModifierCount(handles.attack, out var count);
accessor.TryGetModifiersOfStat(handles.attack, modifiers);   // NativeList<StatModifier>
accessor.TryGetObserversOfStat(handles.attack, observers);   // ai phụ thuộc stat này
accessor.TryGetAllObservers(owner, observers);               // mọi cạnh ngược trên owner này
accessor.TryGetStatModifier(modifierHandle, out var modifier);
```

## Event

Hai dòng event tích luỹ trong `WorldData`:

```csharp
var events = worldData.GetStatChangeEvents(Allocator.Temp);

for (var i = 0; i < events.Length; i++)
{
    var (handle, prev, next) = events[i];
    // ...
}

events.Dispose();

// Xoá một lần mỗi frame, sau khi thứ tiêu thụ chúng đã chạy.
worldData.Clear();
```

| | |
|---|---|
| `StatChangeEvent<TValuePair>` | `{ statHandle, prevValue, newValue }` — cho stat có `ProduceChangeEvents` |
| `ModifierTriggerEvent<…>` | `{ handle, modifier }` — đẩy ra khi `ApplyInternal` của bạn bật cờ trigger |

Có overload điền vào `NativeList` thay vì alloc `NativeArray`, cộng
`ClearStatChangeEvents()` / `ClearModifierTriggerEvents()` để xoá một dòng, và
`AddStatChangeEvent` / `AddModifierTriggerEvent` để tự đẩy event vào.

**Gộp theo `statHandle` trước khi hành động.** Một lần ghi có thể sinh nhiều event cho cùng một stat —
trên DAG có nhánh dài ngắn khác nhau, một giá trị trung gian được phát ra trước giá trị đã hội tụ. Lái
logic "vừa bị đánh" theo *số lượng* event sẽ sai. Xem [Khái niệm cốt lõi](02-CONCEPTS.md#một-lần-ghi-lan-truyền-thế-nào).

**Không có gì xoá list hộ bạn.** Quên `Clear()` là chúng phình suốt vòng đời của `WorldData`.

## Destroy owner

```csharp
store.DestroyOwner(owner);                                    // đường nhanh
accessor.DestroyOwnerAndUpdateObservers(owner, ref worldData); // đồng thời làm mới bên phụ thuộc
```

`DestroyOwner` một mình không đụng tới observer — bản gốc cũng thiếu chỗ này, và nó không phải lỗi
đúng/sai: stat trỏ tới owner đã chết resolve về không có gì và bị bỏ qua khi lan truyền. Nhưng nó có
nghĩa stat phụ thuộc giữ **giá trị tính lần cuối** tới khi có gì đó tính lại.
`DestroyOwnerAndUpdateObservers` snapshot bên phụ thuộc trước, destroy, rồi update từng cái.

Dùng cái nào: cái trơn khi bên phụ thuộc cũng sắp bị destroy hoặc dù sao cũng sẽ được tính lại trong
frame này; cái của accessor khi một nguồn buff biến mất và nhân vật khác phải phản ứng ngay.

Đằng nào cũng viết `ApplyInternal` của modifier sao cho observed stat thiếu thì đóng góp 0 chứ không
fail:

```csharp
case Kind.AddFromStat:
    if (reader.TryGetStatValue(observedStat, out var other))
    {
        stack.add += other.GetCurrentValueOrDefault(new StatVariant(0f));
    }
    break;
```

Destroy rẻ — 0.166 µs mỗi owner trong benchmark — vì nó xoá buffer và pool slot chứ không giải phóng
bộ nhớ. Chỉ `store.Dispose()` mới free.

## Làm việc với `StatVariant`

`StatVariant` là union giá trị. Nó có toán tử số học và so sánh, hàm toán học, và một `ToString()` phủ
mọi kiểu, nên log nó ra là thấy giá trị chứ không phải tên kiểu.

```csharp
var v = new StatVariant(12.5f);
float f = v.Float;              // reinterpret — người gọi tự khẳng định kiểu
v = v + new StatVariant(2f);
```

Đọc sai field là lỗi kiểu mà union không bắt được. Dưới `APEXION_STATS_RUNTIME_CHECKS`, toán tử lệch
kiểu sẽ throw; ở release các check biến mất. Nên đi qua struct `[StatData]` có kiểu và
`GetCurrentValueOrDefault` — chỉ chạm `StatVariant` thô trong code modifier, nơi kiểu đã biết sẵn.

**`StatVariant` không author được trong Inspector** — nó có 19 field ở `[FieldOffset(0)]`. Dùng
`SerializableStatVariant` từ assembly `.Authoring`. Xem [Tooling ▸ Authoring](07-TOOLING.md#authoring).
