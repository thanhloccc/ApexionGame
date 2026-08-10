# Khái niệm cốt lõi

*[English](../02-CONCEPTS.md) · [Mục lục hướng dẫn](README.md)*

## Một stat là một node, không phải một con số

```
                 ┌──────────────────────────────────────────┐
                 │ Stat                                     │
   base value ──▶│  ValuePair { base, current }             │──▶ current value
                 │  ModifierRange (startIndex, count)       │
                 │  ObserverRange (startIndex, count)       │
                 │  UserData, ProduceChangeEvents           │
                 └──────────────────────────────────────────┘
                        │                        ▲
      modifier của nó   │                        │ khi stat này đổi giá trị,
      đọc stat khác ────┘                        │ mọi observer được tính lại
      (observed stats)                           │
```

Hai giá trị mỗi stat:

- **base value** — thứ bạn author hoặc ghi vào. `TrySetStatBaseValue` set giá trị này.
- **current value** — thứ modifier stack tạo ra từ base value. Đây là giá trị gameplay đọc. Không có
  modifier thì hai giá trị bằng nhau.

Hai quan hệ có hướng, và chúng là cùng một cạnh nhìn từ hai đầu:

- **Modifier** thuộc về stat mà nó ảnh hưởng, và có thể *đọc* stat khác — **observed stats** của nó.
- **Observer** là cạnh ngược: "khi tôi đổi, tính lại stat này". Runtime tự duy trì các cạnh này mỗi
  lần bạn thêm hoặc gỡ modifier. Bạn không bao giờ ghi chúng.

## Tính lại một stat

Mỗi lần một stat cần tính lại, đúng những bước sau xảy ra (`StatAPI.UpdateSingleStatCommon`):

1. Snapshot stat, để so sánh về sau.
2. `stack.Reset(stat)` — xoá bộ tích luỹ. Reset đọc *kiểu* giá trị của stat, đó là lý do phần tử đơn
   vị lấy từ `type.ZeroVariant()` / `type.OneVariant()` chứ không phải literal `0f` và `1f`.
3. Với mỗi modifier trong `ModifierRange`, gọi `modifier.Apply(reader, ref stack, out trigger)`.
   Modifier được truyền **bằng ref**, nên nó được phép tự mutate state của mình (ví dụ một bộ đếm thời
   gian). Nếu nó bật `trigger`, một `ModifierTriggerEvent` được đẩy vào hàng đợi.
4. `stack.Apply(baseValue, ref currentValue)` cho ra current value cuối cùng, và composer ghi nó trở
   lại `ValuePair` của stat.
5. **Nếu value pair không đổi, dừng ở đây.** Đây là cái phanh chính của lan truyền — không phải
   visited-set.
6. Nếu stat có `ProduceChangeEvents`, đẩy một `StatChangeEvent { statHandle, prevValue, newValue }`.
7. Duyệt `ObserverRange` và xếp từng observer vào hàng đợi: observer cùng owner vào một worklist,
   observer khác owner vào worklist còn lại.

Việc chia đôi ở bước 7 để phần cùng owner tái dùng ba buffer đang mở, không phải lookup lần nữa.

## Một lần ghi lan truyền thế nào

`TrySetStatBaseValue` ghi base value, tính lại stat đó, rồi rút cạn hai worklist — theo bề rộng, tới
khi không còn gì đổi. Cạnh chéo owner được đi theo y như cạnh cùng owner; không khác về hành vi, chỉ
khác ở việc buffer nào phải lấy ra.

Hai tính chất làm việc này an toàn:

**Vòng lặp không thể tồn tại.** `TryAddStatModifier` đi theo chuỗi observer *trước khi* commit, và trả
`false` nếu modifier mới khép vòng. Việc từ chối xảy ra *lúc thêm* — không có gì bị mutate, không có
modifier nửa vời còn lại. Nhờ đó đồ thị luôn là DAG và lan truyền luôn kết thúc.

```csharp
// cả ba trả false, và store không đổi gì
accessor.TryAddStatModifier(hero.attack, Modifier.AddFrom(hero.attack), out _, ref worldData);
accessor.TryAddStatModifier(hero.hp,     Modifier.AddFrom(hero.attack), out _, ref worldData); // hp <-> attack
accessor.TryAddStatModifier(hero.hp,     Modifier.AddFrom(hero.defense), out _, ref worldData); // vòng 3 chặng
```

**Một stat có thể được tính lại nhiều lần trong một lần lan truyền, và đó là đúng.** Điều này làm ai
cũng bất ngờ, nên nói cho cụ thể. Lấy một kim cương có hai nhánh dài ngắn khác nhau:

```
        ┌──────────────▶ moveSpeed        (1 chặng)
   hp ──┤
        └──▶ attack ──▶ defense ──▶ moveSpeed   (3 chặng)
```

Worklist chạm `moveSpeed` qua nhánh ngắn trước, lúc `defense` còn giữ giá trị cũ. `moveSpeed` được
tính — và nó sai. Sau đó `defense` cập nhật, đẩy `moveSpeed` trở lại worklist, và *lượt đó* sửa nó.

Bản gốc có visited-set chặn đúng lượt thứ hai này, để lại giá trị sai vĩnh viễn. Bản port đã bỏ nó ở
cả hai lối lan truyền. Cái đánh đổi là xấu nhất **O(k²)** thay vì O(k) trên đồ thị dày kim cương, để
lấy giá trị luôn đúng. Số đo, gồm sai số ở từng độ sâu, ở [DEC-005](../../07-DECISIONS.md#dec-005).

Hệ quả thực tế nếu bạn tiêu thụ event: **gộp `StatChangeEvent` theo `statHandle`.** Một lần ghi có thể
phát nhiều event cho cùng một stat, và chỉ event cuối là giá trị đã hội tụ.

## Lưu trữ

Ba buffer mỗi owner, và mỗi owner độc lập:

```
owner "hero"
  stats     : [ None ][ Hp ][ Attack ][ Defense ]        ← index 0 luôn là None stat
  modifiers : [ Hp.m0 ][ Hp.m1 ][ Attack.m0 ]            ← gom theo stat bị ảnh hưởng
  observers : [ obs-of-Hp ][ obs-of-Attack ][ obs-of-Attack ]
                ▲                ▲
                │                └─ Attack.ObserverRange = (1, 2)
                └─ Hp.ObserverRange = (0, 1)
```

Hai buffer `modifiers` và `observers` là **arena chia khối**: khối của stat *i* phải nằm trước khối của
stat *i+1*. Mỗi lần insert hay remove đều phải dịch `startIndex` của mọi stat phía sau. Bất biến này
là bức tường chịu lực của cả hệ thống — nếu bạn sửa runtime, đây là chỗ phải cẩn thận.

Hai hệ quả bạn cảm được từ bên ngoài:

- Thêm một modifier là O(số stat của owner đó), không phải O(world). Đây là lý do lưu trữ theo từng
  owner chứ không phải một arena toàn cục.
- `DestroyOwner` **không** giải phóng bộ nhớ. Nó xoá ba buffer, vô hiệu hoá version, và đẩy slot vào
  free-list; `CreateOwner` tái dùng slot đó, kể cả capacity. Sau warm-up, spawn/despawn không alloc.
  Chỉ `Dispose()` mới free thật.

### Handle

| Type | Là | Hợp lệ khi |
|---|---|---|
| `StatOwnerHandle` | `{ int Index; int Version; }` | version của slot còn khớp |
| `StatIndex` | vị trí trong buffer `stats` của owner | `value > 0` |
| `StatHandle` | `{ StatOwnerHandle owner; StatIndex index; }` | owner hợp lệ |
| `StatHandle<TStatData>` | như trên, có kiểu | như trên |
| `StatModifierHandle` | `{ StatHandle affectedStatHandle; uint modifierId; }` | — |

**Index 0 là None stat.** Mọi stat thật bắt đầu từ 1. Chỉ `StatAPI.CreateStatOwnerHandle` — và
`Builder.Build` sinh ra gọi xuống nó — gieo slot đó. Owner tạo bằng `store.CreateOwner()` trơ không có
None stat, và handle trỏ vào đó sẽ không hành xử đúng.

**Handle của owner đã chết fail trong im lặng.** Version không còn khớp, nên lookup trả `false` và lan
truyền bỏ qua cạnh đó. Không exception. Điều này là có chủ ý: một buff mà nguồn đã chết nên đóng góp 0,
chứ không nên làm sập frame. Nó cũng có nghĩa một handle cũ trông giống hệt một modifier thiếu đóng
góp — đáng nhớ khi một giá trị thấp một cách bí ẩn.

**`StatOwnerHandle` không ổn định qua các phiên.** Nó là chỉ số slot. Xem [Lưu / tải](05-PERSISTENCE.md).

## Cái gì thread-safe

| | |
|---|---|
| Đọc song song (`Accessor.ReadOnly`, `Reader`) | ✅ bao nhiêu job cũng được, miễn không ai ghi |
| Ghi song song | ❌ theo thiết kế, như bản gốc |
| Ghi trong một `IJob` đơn luồng | ✅ mẫu chính |
| Thu thập song song → apply đơn luồng | ✅ qua ba `DeferredUpdateStat*Job` |
| Burst | ✅ mọi thứ blittable; generator sinh struct job không generic nên Burst compile được |

`StatWorldData` là chỗ giới hạn: nó giữ một reference modifier-stack dùng chung và năm scratch list, và
mọi hàm mutate của accessor đều `Clear()` rồi tái dùng chúng. Một cái cho một luồng update, không share,
không reentrant. Chi tiết và mẫu schedule ở [Job & hiệu năng](06-JOBS-AND-PERFORMANCE.md).

## Bốn tầng

Hữu ích khi đọc source hay một stack trace:

```
L4  Sinh ra theo từng [StatSystem] / [StatCollection]
    RpgStatSystem.{Stat, ValuePair, StatModifier, Stack, StatObserver,
                   API, Reader, Accessor, Accessor.ReadOnly, Builder, WorldData,
                   DeferredUpdateStat{List,Queue,Stream}Job}
    RpgStats.{Type, TypeId, Index, Indices, StatIndices, StatHandles,
              Options, Params, Builder<T>, Accessor<T>, Reader<T>}
              ▲  wrapper mỏng, không generic
L3  Thuật toán — StatAPI, StatAccessor<6>, StatReader<2>, StatWorldData<5>, StatBuilder<6>
              ▲
L2  Seam lưu trữ — StatOwnerHandle, StatBuffer<T>, StatBufferLookup<T>, StatStore<3>
              ▲
L1  Contract + giá trị — IStat, IStatData, IStatModifier, IStatModifierStack, IStatObserver,
                         StatVariant, StatHandle, range, event
```

Mọi thứ bạn gọi trong code thường ngày là L4. L3 là nơi thuật toán sống và là thứ tài liệu thiết kế
mô tả. L2 là tầng đã thay thế ECS — và là tầng duy nhất phải viết từ đầu cho bản port này.
