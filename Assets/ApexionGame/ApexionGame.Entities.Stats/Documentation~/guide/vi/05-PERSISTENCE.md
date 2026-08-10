# Lưu / tải

*[English](../05-PERSISTENCE.md) · [Mục lục hướng dẫn](README.md)*

Thư viện cho bạn trạng thái blittable và một lượt remap. Nó **không** chọn định dạng file — không
JSON, không binary layout, không tích hợp Addressables. Bytes đi đâu là quyết định của project bạn.

Trạng thái đầy đủ của một owner là ba buffer cộng một counter. Tất cả đều `unmanaged`, nên đi thẳng vào
writer bạn đang có được.

## Lưu

```csharp
var stats     = new NativeList<RpgStatSystem.Stat>(8, Allocator.Temp);
var modifiers = new NativeList<RpgStatSystem.StatModifier>(8, Allocator.Temp);
var observers = new NativeList<RpgStatSystem.StatObserver>(8, Allocator.Temp);

store.TryCopyOwnerTo(owner, stats, modifiers, observers, out var modifierIdCounter);

// ghi bốn thứ này xuống đĩa, theo cách bạn muốn
```

Lưu cả counter. Không có nó, id modifier khởi động lại từ đầu và có thể đụng id trong các modifier đã
khôi phục.

## Tải

```csharp
store.TryRestoreOwner(
      stats.AsArray().AsReadOnlySpan()
    , modifiers.AsArray().AsReadOnlySpan()
    , observers.AsArray().AsReadOnlySpan()
    , modifierIdCounter
    , out var restoredOwner
);
```

`TryRestoreOwner` lấy một slot rỗng và ghi buffer trở lại nguyên văn. **Giá trị `StatIndex` không đổi**
— thứ tự stat trong buffer được giữ nguyên — nên một struct `RpgStats` đã lưu vẫn hợp lệ với owner vừa
khôi phục. `StatOwnerHandle` thì không: owner gần như chắc chắn rơi vào slot khác với version khác.

## Lượt remap — đừng bỏ

`StatHandle` chứa index và version của owner. Vì thế mọi modifier chéo owner trong save của bạn đang
trỏ tới một số slot giờ mang nghĩa khác, hoặc không mang nghĩa gì.

```csharp
// 1. Khôi phục MỌI owner trước, thu handle cũ -> handle mới
var map = new NativeHashMap<StatOwnerHandle, StatOwnerHandle>(count, Allocator.Temp);
map.Add(oldAuraOwner, newAuraOwner);
map.Add(oldHeroOwner, newHeroOwner);

// 2. Một lượt remap trên các owner đã khôi phục
var remap = new StatOwnerRemap(map);
accessor.TryRemapOwners(restoredOwners, in remap);   // hoặc TryRemapOwner(owner, in remap) từng cái

// 3. Tính lại, để liên kết đã vá cho ra giá trị
accessor.TryUpdateAllStats(newHeroOwner, ref worldData);

map.Dispose();
```

Remap làm hai việc: nó vá `StatObserver.ObserverHandle` (store tự làm được, vì `IStatObserver` expose
property đó), và nó gọi `RemapObservedStatsInternal` trên từng modifier để *bạn* vá handle lưu trong
field của mình:

```csharp
partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
{
    if (kind == Kind.AddFromStat)
    {
        observedStat = remap.RemapOrNull(observedStat);
    }
}
```

`StatOwnerRemap` có `TryRemap(StatOwnerHandle, out …)`, `TryRemap(StatHandle, out …)` — giữ `StatIndex`
và chỉ ghi lại owner — và `RemapOrNull(StatHandle)`, trả `StatHandle.Null` cho owner không có trong
save.

### Bỏ bước này thì thấy gì

Không thấy gì. Đó chính là vấn đề.

Modifier chéo owner mà handle không resolve được thì **đóng góp 0** và không báo gì — cùng một đường
với một nguồn bị destroy hợp lệ. Hero load lên với attack thấp hơn một chút và không có lỗi ở đâu cả.
Test `Remap_WithoutRemapPass_LosesTheLink` chốt đúng hành vi đó để nó không bị "sửa" thành exception
một cách vô tình.

Case 9 của sample in giá trị cả trước và sau remap để thấy rõ khác biệt:
[`StatsPlaygroundWorld.Case09_SaveLoadRemap`](../../../../ApexionGame.Entities.Stats.Samples/StatsPlaygroundWorld.cs).

Analyzer giúp được một phần: `AGS_STAT_DATA_0006` cảnh báo khi modifier có field `StatHandle` mà không
có `RemapObservedStatsInternal`. Nó không biết implement đó *đủ* hay chưa, nên một modifier có hai field
handle mà chỉ remap một cái vẫn lọt. Giữ hook ngay cạnh những field nó bảo trì.

## Quy tắc thứ tự

1. **Khôi phục mọi owner trước khi remap bất cứ gì.** Map phải đầy đủ, không thì handle trỏ tới owner
   được khôi phục sau sẽ resolve về null và mất.
2. **Rồi remap.**
3. **Rồi tính lại.** Giá trị khôi phục là giá trị đã lưu; chúng chỉ đúng nếu không có gì trong world
   thay đổi. `TryUpdateAllStats` cho từng owner đã khôi phục là mặc định an toàn.
4. **Đừng tái dùng handle bắt được trước khi save.** Chúng trỏ tới owner đã chết. Dựng lại:
   `stats.GetStatHandles(restoredOwner)`.

## Vài chỗ nên kiểm trong code save của bạn

- **Version là theo slot, không phải toàn cục.** Hai save load theo thứ tự khác nhau cho ra handle khác
  nhau. Đừng bao giờ persist một `StatOwnerHandle` làm tham chiếu chéo trong dữ liệu của bạn — hãy
  persist id ổn định của riêng bạn rồi dựng lại map lúc load.
- **`modifierIdCounter` là theo owner.** Id modifier cục bộ theo owner, nên một `StatModifierHandle`
  từ phiên trước chỉ có nghĩa sau khi remap.
- **`ProduceChangeEvents` và `UserData` round-trip bên trong `Stat`,** nên không cần làm gì thêm để giữ
  chúng.
- **Kiểm lại seed type-id chưa xê dịch.** `UserData` encode một type-id suy ra từ seed của
  `[StatCollection]` và thứ tự khai báo các member `[StatData]`. Đổi thứ tự member hoặc đổi seed là vô
  hiệu hoá mọi save đang có. Thêm stat mới vào cuối.
