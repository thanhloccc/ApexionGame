# Job & hiệu năng

*[English](../06-JOBS-AND-PERFORMANCE.md) · [Mục lục hướng dẫn](README.md)*

## Hợp đồng threading

| | |
|---|---|
| Đọc song song — `Accessor.ReadOnly`, `Reader` | ✅ bao nhiêu job cũng được, miễn không ai ghi |
| Ghi song song | ❌ theo thiết kế, như bản gốc |
| Ghi từ một `IJob` đơn luồng | ✅ mẫu chính |
| Thu thập song song → apply đơn luồng | ✅ ba `DeferredUpdateStat*Job` |
| Burst | ✅ mọi state blittable; generator sinh struct job không generic |

Chỗ giới hạn là `StatWorldData`. Nó giữ một reference modifier-stack dùng chung cộng năm scratch list,
và mọi hàm mutate đều `Clear()` rồi tái dùng chúng. Vì vậy:

- Một `WorldData` cho một luồng update. Đừng share giữa các job chạy đồng thời.
- Không reentrant. Đừng gọi hàm mutate trong lúc đang iterate event list của cùng `WorldData` đó.
- Không có gì bắt lỗi việc này. `StatStore` và `StatBuffer<T>` mang guard `AtomicSafetyHandle` dưới
  `ENABLE_UNITY_COLLECTIONS_CHECKS`, nên dùng buffer sai thì bị bắt; share `WorldData` thì không.

Vì không có `SystemState` và không có dependency graph, **project của bạn sở hữu mọi `JobHandle`.**
Không có gì schedule hộ bạn.

## Ba mẫu schedule

### 1. Đồng bộ

Mặc định, và đúng cho phần lớn game. Một lệnh ghi lan truyền xong trước khi trả về:

```csharp
accessor.TrySetStatBaseValue(handles.hp, new ValuePair(newHp), ref worldData);
```

Một lệnh ghi qua chuỗi 8 tầng đo được khoảng 2.8 µs. Vài nghìn lần mỗi frame trên main thread là ổn.

### 2. Một update job mỗi frame

Xếp handle cần tính lại trong frame, rồi rút cạn trong một job đơn luồng:

```csharp
var toUpdate = new NativeList<StatHandle>(64, Allocator.TempJob);
// ... gameplay điền vào toUpdate ...

var handle = new RpgStatSystem.DeferredUpdateStatListJob {
    statAccessor  = accessor,
    statWorldData = worldData,
    statsToUpdate = toUpdate,
}.Schedule();

handle.Complete();
toUpdate.Dispose();
```

### 3. Thu thập song song, apply đơn luồng

Mẫu cho quy mô lớn. Nhiều job quyết định *cái gì* đổi, song song; một job apply:

```csharp
var toUpdate = new NativeQueue<StatHandle>(Allocator.TempJob);

var damage = new ApplyDamageJob {
    stats         = accessor.AsReadOnly(),          // view chỉ đọc — song song an toàn
    statsToUpdate = toUpdate.AsParallelWriter(),
}.Schedule(count, 64);

var update = new RpgStatSystem.DeferredUpdateStatQueueJob {
    statAccessor  = accessor,
    statWorldData = worldData,
    statsToUpdate = toUpdate,
}.Schedule(damage);

update.Complete();
toUpdate.Dispose();
```

Ba loại collection được hỗ trợ, để bạn chọn theo producer của mình:

| Job | Tiêu thụ |
|---|---|
| `DeferredUpdateStatListJob` | `NativeList<StatHandle>` |
| `DeferredUpdateStatQueueJob` | `NativeQueue<StatHandle>` |
| `DeferredUpdateStatStreamJob` | `NativeStream.Reader` |

Cần gì đặc thù hơn thì viết một `IJob` ngắn của bạn gọi
`accessor.TryUpdateStat(handle, ref worldData)` trong vòng lặp. Ba job có sẵn cũng chỉ làm đúng vậy.

## Burst

Dùng job **sinh ra** — `RpgStatSystem.DeferredUpdateStatListJob` — chứ không phải job generic mở trong
`StatJobs.cs`. Burst không compile generic mở, nên generator sinh một wrapper không generic mang
`[BurstCompile]` bọc quanh job đã đóng kiểu. Đây cũng là lý do không cần `[RegisterGenericJobType]` ở
bất cứ đâu.

Các generic trong `StatJobs.cs` cố ý **không** mang `[BurstCompile]`; attribute đó ở đấy không có tác
dụng gì.

Đã xác nhận trong Burst Inspector (2026-08-04): cả ba job sinh ra hiện dưới namespace của stat system,
không bị xám, và panel Assembly ra mã máy thật. Ba dòng generic mở
``DeferredUpdateStat*Job`6[…]`` bị xám — Burst bỏ qua, đúng như thiết kế.

Nội suy chuỗi trong `ThrowHelper` **không** chặn Burst, trái với lo ngại từng ghi trong tài liệu thiết
kế. Các `[Conditional]` của nó bám vào `UNITY_EDITOR` / `APEXION_*` chứ không phải
`ENABLE_UNITY_COLLECTIONS_CHECKS`, nên chúng vẫn nằm trong IL và Burst vẫn compile.

## Số đo thật

`StatBenchmarkTests.cs`, năm phép đo trên **10k owner × 8 stat × 4 modifier**, mỗi owner là một chuỗi 8
tầng (stat sau observe stat trước). Unity 6000.3.20f1, Editor/Mono, safety check **bật**, Burst **tắt**.
Cả năm chạy hết trong 1.86 s.

| | Tổng | Mỗi thao tác |
|---|---|---|
| B01 dựng world (10k owner, 80k stat, 320k modifier) | 291.4 ms | 0.911 µs/modifier |
| B02 `TrySetStatBaseValue` + lan truyền, mỗi owner một lần | 28.2 ms | **2.824 µs/owner** |
| B03 `TryUpdateAllStats` mỗi owner | 31.7 ms | 3.170 µs/owner |
| B04 đọc toàn bộ 80k stat | 6.0 ms | 0.075 µs/stat |
| B05 destroy 10k owner | 1.7 ms | **0.166 µs/owner** |
| B05 dựng lại (warm) | 266.1 ms | 0.832 µs/modifier |

> ⚠ **Chỉ dùng để so tương đối.** Mono, safety check bật, Burst tắt. Build player nhanh hơn đáng kể.
> Việc của bộ benchmark này là bắt một thao tác lỡ thành O(world) thay vì O(owner) — không phải để trích
> dẫn thông lượng tuyệt đối.

Hai điều số liệu khẳng định:

- **Một lệnh ghi là O(owner), không phải O(world).** 2.824 µs để lan truyền qua chuỗi 8 tầng, bất kể
  10k owner khác trong store.
- **Slot pooling hoạt động.** Destroy tốn 0.166 µs/owner vì nó xoá buffer và đẩy slot vào free-list chứ
  không giải phóng bộ nhớ. Lần dựng lại sau đó *rẻ hơn* lần đầu (266 ms so với 291 ms) vì cấp phát được
  tái dùng.

So sánh trực tiếp với `DynamicBuffer` thì không làm được: project không cài `com.unity.entities`, mà đó
chính là mục đích của bản port.

## Độ phức tạp, và một trường hợp xấu nhất

| Thao tác | Chi phí |
|---|---|
| Đọc một stat | O(1) |
| Ghi một stat | O(số bên phụ thuộc tới được) — xem dưới |
| Thêm một modifier | O(số stat của owner đó) cho việc dịch range, cộng một lượt duyệt chống vòng |
| Gỡ một modifier | như trên |
| Tạo một owner | 3 allocation lúc lạnh, 0 sau warm-up |
| Destroy một owner | O(1) |

Lan truyền **không có visited-set**, nên xấu nhất là **O(k²)** trên đồ thị dày kim cương lệch tầng, chỗ
mà lan truyền theo thứ tự topo sẽ là O(k). Đây là đánh đổi có chủ ý: bản có visited-set thì tuyến tính
*và sai* — nó chặn đúng lượt thứ hai có nhiệm vụ sửa một điểm giao đã tính từ input cũ. Số đo ở từng độ
sâu có trong [DEC-005](../../07-DECISIONS.md#dec-005).

Trong thực tế cái phanh nằm ở chỗ khác. Việc tính lại dừng ngay khi value pair của một stat không đổi,
nên lan truyền tắt ở stat đầu tiên mà kết quả không bị ảnh hưởng. Trường hợp bậc hai cần một chuỗi kim
cương mà **mọi** giá trị đều thật sự đổi — một hình dạng nên tránh vì khó hiểu, không chỉ vì chi phí.

Nếu có lúc bạn cần vừa tuyến tính vừa đúng, thiết kế đã biết: mỗi stat mang một depth và worklist là
priority queue. Nó được ghi lại làm việc tương lai, chưa implement.

## Tinh chỉnh thực tế

**`produceChangeEvents: false` cho stat không ai lắng nghe.** Theo từng stat, rẻ, và bỏ được cả việc
đẩy event lẫn việc list phình ra.

**Định cỡ store một lần.** `new StatStore<…>(initialOwnerCapacity, allocator)` — chọn gần số owner đỉnh
để mảng slot không phải grow nhiều lần. Gợi ý capacity buffer theo owner nằm ở
`store.CreateOwner(statCapacity, modifierCapacity, observerCapacity)`; builder sinh ra dùng mặc định
(4 / 4 / 4).

**Dùng batch khi dựng nhân vật.** `TryAddStatModifiersBatch` hoãn việc dịch range và update một lần
thay vì một lần mỗi modifier.

**Truyền `Allocator` tường minh cho các hàm set batch trong job dài.** Chúng mặc định `Allocator.Temp`,
không đúng cho một job sống lâu hơn một frame. Cả hai container scratch đều được dispose trước khi trả
về, nên allocator nào cũng chạy.

**Tắt runtime check ở build phát hành nếu cần.** `DISABLE_APEXION_CHECKS` vô hiệu hoá mọi guard
`[Conditional]`. Các guard hiện tại rẻ đủ để chưa cần dùng tới — hãy đo trước khi với tới nó.
