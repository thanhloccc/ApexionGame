# ApexionGame.Entities.Stats

Hệ thống stat có **đồ thị phụ thuộc** cho Unity. Một stat có thể được tính từ stat khác — kể cả stat
của chủ thể khác — và thay đổi lan truyền theo đồ thị đó. Không cần ECS.

*[English](README.md) · [Hướng dẫn](Documentation~/guide/vi/README.md) · [Tài liệu thiết kế](Documentation~/README.md)*

```csharp
// hero.attack đọc aura.attackBonus, mà aura là một owner khác
accessor.TryAddStatModifier(hero.attack, StatModifier.AddFrom(aura.attackBonus), out _, ref worldData);

accessor.TrySetStatBaseValue(aura.attackBonus, new ValuePair(20f), ref worldData);
// hero.attack đã đúng. Không ai gọi hàm tính lại.
```

Dòng cuối là toàn bộ lý do thư viện này tồn tại. Bỏ nó đi thì một `Dictionary<string, float>` là đủ.

---

## Nó là gì

Đây là bản port của [`EncosyTower.Entities.Stats`][encosy] — chính nó là bản port của
[**Trove Stats**][trove] (MIT, © 2023 Philippe St-Amand) — sang project **không có
`com.unity.entities`**. Thuật toán giữ nguyên. Chỉ đúng bốn primitive lưu trữ của ECS bị thay:

| Unity.Entities | Bản này |
|---|---|
| `Entity` | `StatOwnerHandle` (index + version) |
| `DynamicBuffer<T>` | `StatBuffer<T>` |
| `BufferLookup<T>` | `StatBufferLookup<T>` |
| `ComponentLookup<StatOwner>` | `StatStore<TStat, TStatModifier, TStatObserver>` |

Mọi thứ còn lại — đồ thị observer, modifier stack, chống vòng lặp, lan truyền thay đổi, deferred
update job, source generator, API sinh ra thân thiện với authoring — hành xử như bản gốc.

**Một chỗ lệch có chủ ý:** visited-set trong lan truyền đã bị bỏ, vì bản gốc để lại giá trị **sai
vĩnh viễn** trên DAG lệch tầng. Số đo và lý lẽ ở [DEC-005](Documentation~/07-DECISIONS.md#dec-005).
Nếu bạn định sửa runtime, đọc chỗ đó trước.

## Tóm tắt tính năng

- **Phụ thuộc chéo owner.** Modifier khai báo mình đọc stat nào; runtime tự dựng cạnh ngược và tính
  lại stat phụ thuộc khi nguồn đổi.
- **Vòng lặp bị từ chối ngay lúc thêm modifier,** không phải phát hiện lúc chạy — đồ thị luôn là DAG
  nên lan truyền không thể treo.
- **Blittable, Burst-friendly.** Native memory toàn bộ, không managed state trong core, không alloc
  trên hot path.
- **API do codegen sinh.** `[StatSystem]` / `[StatCollection]` / `[StatData]` sinh handle có kiểu,
  builder, accessor, reader và job không generic mang `[BurstCompile]`.
- **18 kiểu giá trị** trong một union 1–16 byte (`bool` … `float4`, `half`…`half4`, `double`, enum).
- **Hỗ trợ lưu/tải** — trạng thái owner blittable cộng một lượt remap handle cho liên kết chéo owner.
- **Tooling editor** — cửa sổ soi store đang chạy kèm đồ thị observer, và ba refactoring Quick Action
  sinh phần boilerplate không thể đoán được.

## Yêu cầu

| | |
|---|---|
| Unity | **6000.3** trở lên (phát triển trên 6000.3.20f1) |
| C# | 10 (`csc.rsp` chốt `-langversion:10`) |
| Package runtime | `com.unity.collections` 2.6+, `com.unity.mathematics` 1.3+, `com.unity.burst` 1.8+ |
| Library runtime | [`com.laicasaane.encosy-tower`][encosy] 0.1.7-preview.3+ — chỉ `EncosyTower.Core` |
| Chỉ editor | `com.annulusgames.unity-codegen` 1.0.0 — **chỉ** cần khi sinh lại bảng kiểu |
| Không cần | `com.unity.entities`, `Unity.Entities.Hybrid`, Latios Framework |

## Cài đặt

Copy các thư mục sau vào `Assets/` của project:

| Thư mục | Bắt buộc? |
|---|---|
| `ApexionGame.Entities.Stats` | có — runtime + generator đã build sẵn |
| `ApexionGame.Entities.Stats.Authoring` | tuỳ — author giá trị stat bằng `ScriptableObject` |
| `ApexionGame.Entities.Stats.Editor` | tuỳ — *ApexionGame ▸ Stats ▸ Stat Debugger* |
| `ApexionGame.Entities.Stats.Samples` | tuỳ — ví dụ chạy được, nên đọc trước |
| `ApexionGame.Entities.Stats.Tests` | tuỳ — 95 test (chỉ Editor) |

Rồi tham chiếu `ApexionGame.Entities.Stats` trong `.asmdef` của bạn. Roslyn generator đã build sẵn
trong `SourceGenerators/` với đúng asset label, nên chỉ cần có tham chiếu là generator tự chạy —
không cài gì, không bước build nào.

Hướng dẫn đầy đủ: [Bắt đầu](Documentation~/guide/vi/01-GETTING-STARTED.md).

## Ba bước

**1 — Khai báo**

```csharp
using ApexionGame.Entities.Stats;

[StatSystem(StatDataSize.Size8)]
public static partial class RpgStatSystem { }

[StatCollection(typeof(RpgStatSystem), 2100)]
public partial struct RpgStats
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct Attack { }
}
```

`StatDataSize.Size8` là ngân sách byte cho giá trị của một stat. Analyzer báo lỗi với bất kỳ
`[StatData]` nào không vừa.

**2 — Tạo store và owner**

```csharp
var store = new StatStore<RpgStatSystem.Stat, RpgStatSystem.StatModifier, RpgStatSystem.StatObserver>(
    initialOwnerCapacity: 256, Allocator.Persistent);

var accessor  = new RpgStatSystem.Accessor(store);
var worldData = new RpgStatSystem.WorldData(64, Allocator.Persistent);

var stats = RpgStats.Builder
    .Build(ref store, out var owner)
    .CreateAllStats(produceChangeEvents: true)
    .ToStats();
```

`stats` là một struct nhỏ chỉ chứa các `StatIndex`. Cất ở đâu cũng được — field của class nhân vật,
`NativeList`, hay dictionary theo id của bạn.

**3 — Đọc / ghi**

```csharp
var access = RpgStats.Accessor.Create(owner, stats, accessor, worldData);

access.TrySetStatBaseValue(new RpgStats.Hp(100f), out _)
      .TrySetStatBaseValue(new RpgStats.Attack(10f), out _);

access.TryGetStatData(out RpgStats.Hp hp, out _, out _);
```

Mọi hàm trên accessor sinh ra đều trả về chính nó nên nối chuỗi được, và trạng thái thành công đi ra
bằng `out bool` chứ không phải giá trị trả về.

Dispose theo thứ tự: `worldData.Dispose()` rồi `store.Dispose()`.

## Tài liệu

| | |
|---|---|
| [**Hướng dẫn**](Documentation~/guide/vi/README.md) | Cài đặt, khái niệm, codegen, modifier, lưu/tải, job, tooling, API reference, chỗ dễ vấp |
| [Sample](../ApexionGame.Entities.Stats.Samples/README.vi.md) | Mười case chạy được; một nhân vật có trang bị, buff, debuff; và một trận RTS hai phe với node bonus dùng chung, spawn và chết |
| [Tài liệu thiết kế](Documentation~/README.md) | Kiến trúc, decision log, toàn bộ chỗ lệch so với bản gốc |
| [Changelog](CHANGELOG.vi.md) | |

Mới bắt đầu? [Bắt đầu](Documentation~/guide/vi/01-GETTING-STARTED.md) →
[Khái niệm cốt lõi](Documentation~/guide/vi/02-CONCEPTS.md) →
[sample](../ApexionGame.Entities.Stats.Samples/README.vi.md).

## Ba điều nên biết trước khi bắt đầu

**`AddObservedStatsToListInternal` là hợp đồng, không phải tiện ích.** Đây là nơi modifier khai báo
mình đọc stat nào. Bỏ sót một cái thì stat đích **không** được tính lại khi nguồn đổi — giá trị cũ
nằm im, không exception, không warning. Đây là loại bug khó truy nhất mà hệ này sinh ra.

**Stat ở index 0 là *None*.** `StatIndex.IsValid => value > 0`. Chỉ
`StatAPI.CreateStatOwnerHandle` (và `Builder.Build` sinh ra gọi xuống nó) gieo stat này. Đừng tự dựng
owner bằng `store.CreateOwner()` rồi mong handle hoạt động.

**Một stat có thể được tính lại nhiều lần trong một lần lan truyền, và đó là đúng.** Trên DAG có hai
nhánh dài ngắn khác nhau đổ về cùng một điểm, lượt tính thứ hai chính là lượt sửa giá trị đã tính khi
nhánh dài còn cũ. Nếu bạn đếm `StatChangeEvent` để chạy logic gameplay, hãy gộp theo `statHandle` —
đừng giả định mỗi stat một event.

Thêm ở [Chỗ dễ vấp & FAQ](Documentation~/guide/vi/09-PITFALLS.md).

## Hiệu năng

10k owner × 8 stat × 4 modifier, Unity 6000.3.20f1, Editor/Mono, safety check **bật**, Burst **tắt** —
chỉ dùng để so tương đối, build player nhanh hơn nhiều:

| | Tổng | Mỗi thao tác |
|---|---|---|
| Dựng world (10k owner, 80k stat, 320k modifier) | 291.4 ms | 0.911 µs/modifier |
| `TrySetStatBaseValue` + lan truyền, mỗi owner một lần | 28.2 ms | **2.824 µs/owner** |
| `TryUpdateAllStats` mỗi owner | 31.7 ms | 3.170 µs/owner |
| Đọc toàn bộ 80k stat | 6.0 ms | 0.075 µs/stat |
| Destroy 10k owner | 1.7 ms | **0.166 µs/owner** |

Con số đáng chú ý: một lần ghi lan truyền qua chuỗi 8 tầng tốn 2.8 µs và **không** phụ thuộc 10k
owner khác trong world. Chi tiết và cảnh báo ở
[Job & hiệu năng](Documentation~/guide/vi/06-JOBS-AND-PERFORMANCE.md).

## Kiểm thử

95 test trong `ApexionGame.Entities.Stats.Tests` (90 test chức năng — gồm 23 golden test khoá bất biến
thuật toán — cộng 5 benchmark), và 17 test generator/analyzer chạy ngoài Unity. Lần chạy đầy đủ gần
nhất: **928 pass / 0 fail / 0 skip** toàn project, Unity 6000.3.20f1, 2026-08-04.

```powershell
# Test Unity, không cần mở Editor
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Tests"

# Generator và analyzer
dotnet test Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Tests
```

## Góp code

Xem [CONTRIBUTING.vi.md](CONTRIBUTING.vi.md). Issue và pull request bằng tiếng Việt hoặc tiếng Anh
đều được.

## Giấy phép

[MIT](LICENSE.md).

Thuật toán © 2023 Philippe St-Amand ([Trove Stats][trove], MIT), qua [EncosyTower][encosy]
(MIT, © Laicasaane). Cả hai giấy phép được ghi lại trong [LICENSE.md](LICENSE.md).

[trove]: https://github.com/PhilSA/Trove
[encosy]: https://github.com/laicasaane/EncosyTower
