# Chỗ dễ vấp & FAQ

*[English](../09-PITFALLS.md) · [Mục lục hướng dẫn](README.md)*

Xếp theo lượng thời gian chúng ngốn, tệ nhất trước. Điểm chung: chúng fail **trong im lặng**.

## 1. Quên một observed stat

```csharp
readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observedStatHandles)
{
    if (kind == Kind.AddFromStat)
    {
        observedStatHandles.Add(observedStat);
    }
    // ... còn field handle thứ hai bạn thêm tuần trước thì sao?
}
```

**Biểu hiện:** một stat đúng lúc bạn set up và sai về sau. Nó không bao giờ tính lại khi input đổi.
Không exception, không warning, không dòng log nào.

**Nguyên nhân:** modifier đọc một stat nó không khai báo, nên runtime không dựng cạnh ngược, nên không
gì bảo stat phụ thuộc tính lại.

**Cách sửa:** mọi `StatHandle` mà modifier đọc phải được thêm ở đây. Coi hook này là một phần của việc
khai báo field — thêm field thì mở rộng hook trong cùng một lần sửa.

**Cách phát hiện:** mở đồ thị của Stat Debugger. Một modifier đọc stat mà không có cạnh vẽ tới chính là
bug này.

## 2. Bỏ lượt remap sau khi load

**Biểu hiện:** stat load lên hơi thấp. Buff chéo owner đơn giản là không có.

**Nguyên nhân:** `StatOwnerHandle` là chỉ số slot. Owner khôi phục rơi vào slot khác, và handle lưu bên
trong modifier giờ trỏ tới bất cứ ai đang chiếm slot cũ — hoặc không tới đâu. Một handle không resolve
được thì đóng góp 0, y như một nguồn bị destroy hợp lệ.

**Cách sửa:** khôi phục mọi owner, dựng `StatOwnerRemap`, gọi `TryRemapOwners`, rồi tính lại. Theo đúng
thứ tự đó. [Lưu / tải](05-PERSISTENCE.md).

**Cách phát hiện:** case 9 của sample in giá trị trước và sau remap cạnh nhau.

## 3. Đọc số lượng `StatChangeEvent` như "số stat đã đổi"

**Biểu hiện:** số damage hiện hai lần; âm thanh "stat changed" phát ba lần cho một cú đánh.

**Nguyên nhân:** một lần ghi có thể phát nhiều event cho cùng một stat. Trên DAG có nhánh dài ngắn khác
nhau, điểm giao được tính từ input cũ trước rồi được sửa ở lượt sau — cả hai lượt đều phát event.

**Cách sửa:** gộp theo `statHandle` và lấy giá trị cuối, hoặc lái gameplay theo giá trị bạn đọc sau khi
lan truyền xong thay vì theo event.

Đây không phải bug cần lách; đó là cách hội tụ hoạt động. Xem
[Khái niệm cốt lõi](02-CONCEPTS.md#một-lần-ghi-lan-truyền-thế-nào) và
[DEC-005](../../07-DECISIONS.md#dec-005).

## 4. Dùng `store.CreateOwner()` thay vì builder sinh ra

**Biểu hiện:** handle trỏ vào một owner mới hành xử lạ, hoặc stat index 1 giữ giá trị bạn tưởng ở index
0.

**Nguyên nhân:** stat index 0 phải là None stat. Chỉ `StatAPI.CreateStatOwnerHandle` — mà
`Builder.Build` sinh ra gọi xuống — gieo nó. `CreateOwner()` trơ cho bạn một owner với buffer rỗng.

**Cách sửa:** luôn tạo owner qua `RpgStats.Builder.Build(ref store, out var owner)`.
`store.CreateOwner()` tồn tại cho việc sổ sách nội bộ của store, không phải cho bạn.

## 5. Đặt `readonly` sai chỗ trên một hook

**Biểu hiện:** một hook đơn giản không bao giờ chạy. `ApplyInternal` không nổ, hoặc id trả về 0.

**Nguyên nhân:** khai báo partial method phải khớp chính xác, gồm cả `readonly`. Lệch một chút là
compiler coi hàm của bạn là hàm không liên quan và để declaration sinh ra không được implement — không
lỗi, vì một `partial void` không implement là hợp lệ.

**Cách sửa:** sinh skeleton bằng **Ctrl+.** ▸ *Generate stat modifier skeleton* rồi điền vào. Đừng bao
giờ gõ chữ ký từ ký ức. Vị trí `readonly` đúng ở
[Khai báo stat](03-DECLARING-STATS.md#bảy-hook).

## 6. Share một `WorldData`

**Biểu hiện:** giá trị sai lâu lâu một lần hoặc dữ liệu hỏng khi tải nặng, và không bao giờ tái hiện
được trong repro nhỏ.

**Nguyên nhân:** `StatWorldData` giữ năm scratch list và một reference modifier-stack mà mọi lệnh mutate
đều xoá rồi tái dùng. Hai luồng vào đó cùng lúc là scratch state đan vào nhau.

**Cách sửa:** một `WorldData` cho một luồng update. Cần song song: thu thập song song, apply đơn luồng.
[Job & hiệu năng](06-JOBS-AND-PERFORMANCE.md).

**Lưu ý:** không có gì bắt lỗi việc này. Guard `AtomicSafetyHandle` bảo vệ store và buffer của nó,
không bảo vệ `WorldData`.

## 7. Dispose sai thứ tự

```csharp
// sai
store.Dispose();
worldData.Dispose();
```

`WorldData` sở hữu các scratch list mà buffer của store được đọc vào. Dispose `worldData` trước, rồi
`store`.

## 8. Không bao giờ gọi `worldData.Clear()`

Event list phình suốt vòng đời của `WorldData`. Xoá một lần mỗi frame, sau khi thứ tiêu thụ event đã
chạy.

---

## FAQ

**Tôi có cần Unity ECS không?**
Không. Đó là mục đích của bản port. `com.unity.entities` không được tham chiếu ở đâu cả. Bạn cần
`Unity.Collections`, `Unity.Mathematics`, `Unity.Burst` và `EncosyTower.Core`.

**Dùng chung với ECS trong một project được không?**
Được — nó hoàn toàn không biết ECS là gì. Nhưng nếu stat của bạn đã sống trong ECS thì dùng
[`EncosyTower.Entities.Stats`](https://github.com/laicasaane/EncosyTower); cùng thuật toán với lưu trữ
ECS. Không có adapter hai chiều và không có kế hoạch làm.

**Sao `EncosyTower.Core` là hard dependency?**
`ByteBool`, `Option<T>`, `HashValue`, `IIsValid`, collection extensions và generator
`[WrapType]` / `[EnumExtensions]` — khoảng 450 dòng lẽ ra phải viết lại. Ngược lại, *Roslyn generator*
thì hoàn toàn độc lập: không `ProjectReference` sang `EncosyTower.SourceGen.*`, nên bạn update package
EncosyTower tự do mà không đụng tới package này.

**Một store hay nhiều store?**
Một store cho mỗi `[StatSystem]`. Các collection chia sẻ một system thì chia sẻ một store, và đó là thứ
cho phép modifier bắc qua collection. Muốn tách store thì phải tách `[StatSystem]` — và khi đó không
modifier nào qua được ranh giới.

**Chứa được bao nhiêu owner?**
Bị chặn bởi bộ nhớ. Ba allocation mỗi owner lúc lạnh, 0 sau warm-up nhờ slot pooling. Benchmark chạy
10k owner × 8 stat × 4 modifier thoải mái; lệnh ghi vẫn O(owner) bất kể world lớn cỡ nào.

**Vòng lặp thật sự không thể xảy ra?**
Đúng, và việc kiểm tra xảy ra lúc thêm modifier. `TryAddStatModifier` đi theo chuỗi observer và trả
`false` — không đụng gì tới store — nếu modifier khép vòng. Đây là thứ làm cho việc lan truyền không có
visited-set trở nên an toàn. Nếu đồ thị trong debugger từng hiện node đỏ thì đó là bug của runtime.

**Modifier có đọc được stat của owner đã bị destroy không?**
Nó thử được. Lookup trả `false` và `ApplyInternal` của bạn quyết định làm gì — đóng góp 0 là câu trả lời
đúng, và là thứ sample làm. Không có exception và không có lượt dọn dẹp nào; stat phụ thuộc giữ giá trị
tính lần cuối tới khi có gì đó tính lại. Dùng `DestroyOwnerAndUpdateObservers` nếu chúng phải phản ứng
ngay.

**Modifier tự mutate mình được không?**
Được. `ApplyInternal` nhận modifier bằng ref, nên một modifier có thể giữ một bộ đếm ngược hay số tầng
stack và cập nhật nó trong lúc tính lại. Bật cờ trigger để một `ModifierTriggerEvent` được đẩy vào hàng
đợi cho nó.

**Sao `StatVariant` không author được trong Inspector?**
Nó có mười chín field ở `[FieldOffset(0)]` và serializer của Unity bỏ qua explicit layout. Dùng
`SerializableStatVariant` từ assembly `.Authoring`. [Tooling ▸ Authoring](07-TOOLING.md#authoring).

**Thêm kiểu giá trị được không?**
Được — `float3x3`, `quaternion`, gì cũng được. Đó là một bước codegen phía editor, ghi ở
[Tooling ▸ Sinh lại bảng kiểu](07-TOOLING.md#sinh-lại-bảng-kiểu-giá-trị). Nhớ quy tắc size: dùng được
cần `size <= maxDataSize`; dùng được ở chế độ pair cần `size <= maxDataSize / 2`.

**Lan truyền tốn bao nhiêu?**
O(số bên phụ thuộc tới được), và nó dừng ở stat đầu tiên có giá trị không thật sự đổi. Xấu nhất là
O(k²) trên đồ thị dày kim cương lệch tầng — một đánh đổi có chủ ý lấy tính đúng, vì bản tuyến tính tính
ra giá trị sai. Số đo ở [Job & hiệu năng](06-JOBS-AND-PERFORMANCE.md#số-đo-thật).

**Có tất định không?**
Có, với một đồ thị và một chuỗi lệnh gọi cho trước. Thứ tự duyệt là cố định và không có hashing trên
đường lan truyền. *Giá trị* handle thì không ổn định qua các phiên, và đó là lý do liên kết chéo owner
cần lượt remap.

**Port modifier viết cho bản gốc được không?**
Gần như được. Đổi tên kiểu, rồi thêm `RemapObservedStatsInternal` — `IStatModifier` thêm
`RemapObservedStats` ở bản port này, nên modifier của bản gốc không compile cho tới khi có hook đó. Danh
sách chỗ lệch đầy đủ ở [decision log](../../07-DECISIONS.md).

**Có gì sai mà tôi không thấy tại đâu.**
Theo thứ tự này: mở Stat Debugger và so `base → current` với thứ bạn mong đợi; xem đồ thị có thiếu cạnh
không (chỗ dễ vấp 1); xem console có `AGS_*` nào không; rồi chạy case của
[sample](../../../../ApexionGame.Entities.Stats.Samples/README.vi.md) gần hình dạng của bạn nhất và diff
setup của bạn với nó.
