# Tooling

*[English](../07-TOOLING.md) · [Mục lục hướng dẫn](README.md)*

## Stat Debugger

**ApexionGame ▸ Stats ▸ Stat Debugger**

Bày một store đang chạy: mọi owner, mọi stat dạng `base → current` kèm số modifier, và đồ thị observer
vẽ thành sơ đồ node.

`StatStore` là native memory do *code của bạn* sở hữu. Không có gì tìm ra nó được, nên bạn tự khai:

```csharp
_debug = new StatStoreDebug<
      RpgStatSystem.ValuePair, RpgStatSystem.Stat
    , RpgStatSystem.StatModifier, RpgStatSystem.StatObserver>("battle", _store, ResolveStatName);

StatDebugRegistry.Register(_debug);

// ... và Unregister TRƯỚC khi dispose store.
StatDebugRegistry.Unregister(_debug);
```

Không khai thì không tốn gì: runtime không đọc registry ở bất kỳ đâu.

**Unregister trước `store.Dispose()`.** Một registration sống lâu hơn store của nó sẽ đưa cho debugger
một pointer treo.

### Tên stat

Tham số thứ ba là tuỳ chọn và biến `#3` thành `attack`:

```csharp
private string ResolveStatName(StatOwnerHandle owner, int index)
{
    if (owner == _heroOwner) { return ((RpgStats.Type)index).ToString(); }
    if (owner == _auraOwner) { return ((AuraStats.Type)index).ToString(); }
    return string.Empty;
}
```

Nó phải là callback chứ không phải một bảng thư viện tự dựng, vì ánh xạ này là **theo owner, không phải
theo store**. Một store có thể giữ owner dựng từ các `[StatCollection]` khác nhau, và một `StatHandle`
trơ không mang dấu vết collection nào dựng nó — chỉ code tạo owner mới biết. Để null thì stat hiện là
`#N`.

Đặt cho debug view một **tên ổn định**. Đặt tên theo trạng thái game hiện tại làm dropdown store của
debugger bị viết lại mỗi lần thay đổi, đọc ra như store biến mất rồi hiện lại.

### Đồ thị

Node là stat, cạnh là quan hệ observer. **Node đỏ nghĩa là phát hiện chu trình** — điều đáng lẽ không
thể xảy ra, vì `TryAddStatModifier` từ chối mọi modifier khép vòng. Nếu nó sáng lên thì đó là bug của
runtime, không phải tính năng của dữ liệu bạn.

## Stats Playground

**ApexionGame ▸ Stats ▸ Stats Playground**

Mười case bấm được, kèm giá trị sống bên cạnh: phụ thuộc chéo owner, kim cương lệch tầng, từ chối vòng
lặp, destroy owner đang bị observe, save/load/remap, và dump event. Không cần Play — store là native
memory do cửa sổ sở hữu, đóng cửa sổ là giải phóng.

Mười case đó cũng nằm trên một GameObject trong scene
([`stats-playground.unity`](../../../../ApexionGame.Entities.Stats.Samples/), component đánh
`[ExecuteAlways]`), nếu bạn thích Inspector hơn. Mỗi nút dựng lại world sạch trước, nên bấm thứ tự nào
cũng được.

Chi tiết: [README của sample](../../../../ApexionGame.Entities.Stats.Samples/README.vi.md).

## Quick Actions

Đặt con trỏ vào một `partial class` hoặc `partial struct` rồi bấm **Ctrl+.**:

| Hành động | Sinh ra |
|---|---|
| *Make this a stat system* | attribute `[StatSystem]` |
| *Generate stat modifier skeleton* | `StatModifier` + `Stack` với đủ bảy hook `partial void ...Internal` |
| *Generate stat collection skeleton* | `[StatCollection]` với seed type-id tránh những số đã dùng trong project |

Skeleton modifier là cái đáng với tới. Hook nào tồn tại, và hook nào phải `readonly`, thì không đoán
được — và một chữ `readonly` sai chỗ làm compiler lặng lẽ không ghép partial method, không diagnostic
nào ở đâu cả.

## Authoring

`StatVariant` mang `[Serializable]` **và** mười chín field ở `[FieldOffset(0)]`. Unity bỏ qua explicit
layout khi serialize, nên author trực tiếp nó ra kết quả rác. Assembly `.Authoring` tồn tại vì lý do đó.

```csharp
using ApexionGame.Entities.Stats.Authoring;

[SerializeField] private SerializableStatVariant _hp;

// lúc runtime
StatVariant value = _hp.ToStatVariant();
```

`SerializableStatVariant.Of(...)` dựng một cái từ `float`, `int`, `bool` hoặc `float4`;
`From(in StatVariant)` đổi một giá trị runtime có sẵn về lại.

`StatDefinitionAsset` là `ScriptableObject` giữ một bộ stat đã author, có tên:

```csharp
[CreateAssetMenu] // ApexionGame/Stats/Stat Definition
```

| Field mỗi entry | |
|---|---|
| `Id` | key chuỗi của bạn — khớp nó với một `TypeId` sinh ra khi apply |
| `BaseValue` | một `SerializableStatVariant` |
| `ProduceChangeEvents` | cờ theo stat |
| `UserData` | payload type-id thô |

Nó cố ý **không** tham chiếu type sinh ra nào. Một asset có kiểu chặt sẽ phải được sinh theo từng
`[StatCollection]`; thay vào đó asset chỉ mang giá trị và project bạn quyết định map `Id` sang handle
sinh ra — thường là một `switch` nhỏ cho mỗi collection.

Tạo owner vẫn đi qua `RpgStats.Builder.Build(ref store)`, vì đó là đường duy nhất gieo None stat.

## Sinh lại bảng kiểu giá trị

`Common/StatVariant.gen.cs`, `StatVariantType.gen.cs`, `StatDataSize.gen.cs`,
`StatVariantTypeExtensions.gen.cs` và `StatSingleExtensions.gen.cs` được sinh từ
`Generators/StatTypeTable.cs` bởi một generator **editor**, và output đã commit. Người dùng bình thường
không bao giờ chạy cái này. Bạn chỉ cần nó khi thêm một kiểu giá trị — ví dụ `float3x3` cho một stat
tensor.

1. Project Settings ▸ Player ▸ Scripting Define Symbols: thêm `APEXION_STAT_VALUE_TYPES_GENERATOR`.
2. Sửa `Generators/StatTypeTable.cs` — thêm kiểu, hoặc sửa gameplay profile.
3. Menu **Tools ▸ UnityCodeGen ▸ Generate**.
4. **Bỏ define ở bước 1.**
5. Build lại Roslyn generator, vì bảng phía Roslyn cũng đổi:
   `dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release`
6. Commit các file `.gen.cs` và DLL.

> Bước 4 không phải tuỳ chọn. Để define bật thì bảy file trong `Generators/` compile vào assembly ở mọi
> lần build.

Nhớ quy tắc size ở [Khai báo stat](03-DECLARING-STATS.md#kiểu-giá-trị-và-ngân-sách-size): một kiểu mới
chỉ dùng được nếu `size <= maxDataSize`, và chỉ dùng được ở chế độ value pair nếu
`size <= maxDataSize / 2`.

## Define validation

Runtime check nằm trong `ThrowHelper` sau các attribute `[Conditional]`, nên chúng biến mất ở release
build. `Debugging/ValidationDefines.cs` đặt tên các symbol:

| Symbol | Tác dụng |
|---|---|
| `UNITY_EDITOR`, `DEBUG` | check bật (ngầm định) |
| `APEXION_RUNTIME_CHECKS` | bật check trong một build |
| `APEXION_STATS_RUNTIME_CHECKS` | check riêng của stats |
| `ENABLE_UNITY_COLLECTIONS_CHECKS` | guard `AtomicSafetyHandle` trên `StatStore` / `StatBuffer<T>` |
| `DISABLE_APEXION_CHECKS` | cửa thoát — vô hiệu hoá mọi symbol trên |

Để ý chỗ bất đối xứng: nút *Safety Checks Off* của Burst **không** gỡ các guard `APEXION_*`, vì chúng
không bám vào `ENABLE_UNITY_COLLECTIONS_CHECKS`. Cần gỡ thì dùng `DISABLE_APEXION_CHECKS`. Đo trước —
các guard hiện tại chưa từng nổi lên trong profile.

## Kiểm thử ngoài Unity

Ba thứ kiểm được mà không cần mở Editor, đáng biết khi đang lặp trên runtime hoặc một generator:

```powershell
# generator và analyzer
dotnet test Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Tests

# sample, compile qua generator thật — chốt chặn hồi quy cho cách dùng đã ghi trong tài liệu
dotnet build Plugins/SourceGenerator.ApexionGame/Samples/Samples.RpgStats/Samples.RpgStats.csproj

# test Unity, batch mode, không mở cửa sổ Editor
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Tests"
```

Hai cái bẫy ở hai lệnh cuối:

- Test runner của Unity **loại test `[Explicit]` kể cả khi gọi đích danh trong `--filter`**. Đó là lý do
  benchmark không mang `[Explicit]`.
- `dotnet test` in `Passed!` kể cả khi test trả về `Inconclusive`. Đọc số skip.

Đặt `UNITY_OS_INSTALL_ROOT` trỏ tới thư mục cài Unity một lần là `unity test` chạy được từ mọi shell.
