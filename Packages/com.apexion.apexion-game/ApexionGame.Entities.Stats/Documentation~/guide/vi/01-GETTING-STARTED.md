# Bắt đầu

*[English](../01-GETTING-STARTED.md) · [Mục lục hướng dẫn](README.md)*

## Yêu cầu

| | |
|---|---|
| Unity | **6000.3** trở lên (phát triển trên 6000.3.20f1) |
| C# | 10 — `csc.rsp` cạnh asmdef runtime chốt `-langversion:10` |
| Package runtime | `com.unity.collections` 2.6+, `com.unity.mathematics` 1.3+, `com.unity.burst` 1.8+ |
| Library runtime | [`com.laicasaane.encosy-tower`](https://github.com/laicasaane/EncosyTower) 0.1.7-preview.3+ |
| Chỉ editor | `com.annulusgames.unity-codegen` 1.0.0 |
| Không cần | `com.unity.entities`, `Unity.Entities.Hybrid`, Latios Framework |

**Về hai dependency ngoài Unity.**

`EncosyTower.Core` là hard reference lúc runtime. Nó cung cấp `ByteBool`, `Option<T>`, `HashValue`,
`IIsValid`, collection extensions, và generator `[WrapType]` / `[EnumExtensions]` — khoảng 450 dòng
không đáng viết lại. Một analyzer (`AGS_STAT_SYSTEM_0002`) báo lỗi nếu assembly dùng `[StatSystem]` mà
không tham chiếu nó, nên bạn biết ngay thay vì lần qua một rừng lỗi thiếu type.

`AnnulusGames.UnityCodeGen.Editor` **chỉ** cần khi **sinh lại** bảng kiểu giá trị
(`Common/*.gen.cs`). Các file đó đã commit, nên người dùng bình thường không bao giờ chạy codegen ấy.
Xem [Tooling ▸ Sinh lại bảng kiểu](07-TOOLING.md#sinh-lại-bảng-kiểu-giá-trị).

## Cài đặt

Copy các thư mục bạn cần vào `Assets/`:

| Thư mục | Bắt buộc? | Chứa |
|---|---|---|
| `ApexionGame.Entities.Stats` | **có** | runtime + Roslyn generator build sẵn |
| `ApexionGame.Entities.Stats.Authoring` | tuỳ | `SerializableStatVariant`, `StatDefinitionAsset` |
| `ApexionGame.Entities.Stats.Editor` | tuỳ | *ApexionGame ▸ Stats ▸ Stat Debugger* |
| `ApexionGame.Entities.Stats.Samples` | tuỳ | sample chạy được + cửa sổ playground |
| `ApexionGame.Entities.Stats.Tests` | tuỳ | 95 test, chỉ Editor |

Rồi thêm `ApexionGame.Entities.Stats` vào `references` của asmdef. Đó là toàn bộ phần setup:
generator đã build sẵn trong `SourceGenerators/` kèm asset label Unity cần, nên nó chạy ở lần compile
kế tiếp. Không có bước build, không có menu item nào phải bấm.

**Kiểm tra codegen còn sống** trước khi viết gì thật. Khai báo một `[StatSystem]`, save, xem
`RpgStatSystem.Accessor` có resolve trong IDE không. Nếu không:

- `ApexionGame.Entities.Stats` có trong `references` của asmdef chưa?
- `EncosyTower.Core` có ở đó chưa? (Tìm `AGS_STAT_SYSTEM_0002` trong console.)
- DLL trong `SourceGenerators/` còn label `RoslynAnalyzer` không? Import lại bằng tay có thể làm mất.

## Thứ nhỏ nhất chạy được

Bốn khai báo, một `MonoBehaviour`. Đây là bản gọn của
[`RpgStatsSample.cs`](../../../../ApexionGame.Entities.Stats.Samples/RpgStatsSample.cs).

### 1. Một stat system

```csharp
using ApexionGame.Entities.Stats;

[StatSystem(StatDataSize.Size8)]
public static partial class RpgStatSystem { }
```

`StatDataSize.Size8` là ngân sách byte cho giá trị của một stat. Một stat thường lưu **cặp** — base
value và current value cạnh nhau — nên tám byte phủ mọi kiểu tới bốn byte: `float`, `int`, `half2`,
mọi enum. Kiểu lớn hơn cần ngân sách lớn hơn hoặc `SingleValue = true`. Analyzer từ chối `[StatData]`
không vừa, nên bạn không thể sai trong im lặng; quy tắc ở
[Khai báo stat](03-DECLARING-STATS.md#kiểu-giá-trị-và-ngân-sách-size).

Mọi type sinh ra đều móc vào khai báo này: `Stat`, `ValuePair`, `StatModifier`, `Stack`,
`StatObserver`, `Accessor`, `Reader`, `Builder`, `WorldData`, và ba job `[BurstCompile]`.

### 2. Một collection stat

```csharp
[StatCollection(typeof(RpgStatSystem), 2100)]
public partial struct RpgStats
{
    [StatData(StatVariantType.Float)] public partial struct Hp { }
    [StatData(StatVariantType.Float)] public partial struct Attack { }
}
```

`2100` là seed type-id của collection này. Cho mỗi collection một số riêng để giá trị `UserData` của
chúng không đè nhau — đó là thứ cho phép lấy một `Stat` trơ và truy ra nó đến từ khai báo nào.

### 3. Modifier làm gì

Generator nối dây interface hộ, nhưng không biết công thức của game bạn. Bạn điền các hook
`partial void`:

```csharp
public static partial class RpgStatSystem
{
    public partial struct StatModifier
    {
        public enum Kind : byte { Add, Multiply, AddFromStat }

        public Kind kind;
        public StatVariant value;
        public StatHandle observedStat;

        private uint _id;

        public static StatModifier Add(float amount)
            => new() { kind = Kind.Add, value = new StatVariant(amount) };

        public static StatModifier AddFrom(StatHandle observed)
            => new() { kind = Kind.AddFromStat, observedStat = observed };

        readonly partial void GetIdInternal(ref uint id) => id = _id;

        partial void SetIdInternal(uint value) => _id = value;

        // Khai báo mọi stat mà modifier này đọc. Bỏ sót một cái thì stat phụ thuộc
        // âm thầm bị cũ khi nguồn đổi.
        readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observed)
        {
            if (kind == Kind.AddFromStat)
            {
                observed.Add(observedStat);
            }
        }

        partial void ApplyInternal(Reader reader, ref Stack stack, ref bool shouldProduceTriggerEvent)
        {
            switch (kind)
            {
                case Kind.Add:
                    stack.add += value;
                    break;

                case Kind.Multiply:
                    stack.multiply *= value;
                    break;

                case Kind.AddFromStat:
                    // Owner bị observe có thể đã chết. Đóng góp 0 thay vì fail.
                    if (reader.TryGetStatValue(observedStat, out var other))
                    {
                        stack.add += other.GetCurrentValueOrDefault(new StatVariant(0f));
                    }
                    break;
            }
        }

        partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
        {
            if (kind == Kind.AddFromStat)
            {
                observedStat = remap.RemapOrNull(observedStat);
            }
        }

        public partial struct Stack
        {
            public StatVariant add;
            public StatVariant multiply;

            partial void ResetInternal(in Stat stat)
            {
                var type = stat.ValuePair.Type;
                add = type.ZeroVariant();
                multiply = type.OneVariant();
            }

            partial void ApplyInternal(in StatVariant baseValue, ref StatVariant currentValue)
                => currentValue = (baseValue + add) * multiply;
        }
    }
}
```

Bảy hook tất cả, và hook nào tồn tại — hook nào phải `readonly` — thì không đoán được. Đừng gõ từ ký
ức: đặt con trỏ vào `partial class`, bấm **Ctrl+.** rồi chọn *Generate stat modifier skeleton*. Một
chữ `readonly` sai chỗ làm compiler lặng lẽ không ghép partial, không báo gì cả. Chi tiết ở
[Khai báo stat](03-DECLARING-STATS.md#bảy-hook).

### 4. Chạy

```csharp
public sealed class Hero : MonoBehaviour
{
    private StatStore<RpgStatSystem.Stat, RpgStatSystem.StatModifier, RpgStatSystem.StatObserver> _store;
    private RpgStatSystem.Accessor _accessor;
    private RpgStatSystem.WorldData _worldData;

    private void Start()
    {
        _store = new(initialOwnerCapacity: 8, Allocator.Persistent);
        _accessor = new(_store);
        _worldData = new(32, Allocator.Persistent);

        var stats = RpgStats.Builder
            .Build(ref _store, out var owner)
            .CreateAllStats(produceChangeEvents: true)
            .ToStats();

        var handles = stats.GetStatHandles(owner);

        RpgStats.Accessor
            .Create(owner, stats, _accessor, _worldData)
            .TrySetStatBaseValue(new RpgStats.Hp(100f), out _)
            .TrySetStatBaseValue(new RpgStats.Attack(10f), out _);

        _accessor.TryAddStatModifier(
            handles.attack, RpgStatSystem.StatModifier.Add(5f), out _, ref _worldData);

        _accessor.TryGetStatValue(handles.attack, out var attack);
        Debug.Log($"attack = {attack.GetCurrentValueOrDefault().Float}");   // 15
    }

    private void OnDestroy()
    {
        // WorldData trước: nó sở hữu các scratch list mà buffer của store được đọc vào.
        if (_worldData.IsCreated) { _worldData.Dispose(); }
        if (_store.IsCreated) { _store.Dispose(); }
    }
}
```

## Quy tắc vòng đời

**Ba đối tượng, và bạn sở hữu cả ba.** Không có gì trong thư viện này là singleton, `MonoBehaviour`,
hay Unity system. Không ai tạo hay dispose hộ bạn.

| Đối tượng | Sống bằng | Ghi chú |
|---|---|---|
| `StatStore<…>` | world của bạn | Native memory. Giữ stat, modifier, observer của mọi owner. |
| `Accessor` | store | Bản copy giá trị của handle store. Copy rẻ, cất vào field an toàn. |
| `WorldData` | một luồng update | Event list + năm scratch buffer. **Không** thread-safe, **không** reentrant. |

**Thứ tự dispose là `worldData` rồi `store`.** WorldData giữ các scratch list mà buffer của store được
đọc vào; dispose store trước thì thao tác WorldData kế tiếp đọc vào bộ nhớ đã giải phóng.

**Một `WorldData` cho một luồng update.** Đừng share giữa hai job chạy song song, đừng gọi hàm mutate
trong lúc đang iterate event list của nó. Cần song song thì mẫu là *thu thập song song → apply đơn
luồng* — xem [Job & hiệu năng](06-JOBS-AND-PERFORMANCE.md).

**`WorldData` tích luỹ event tới khi bạn xoá.** Gọi `worldData.Clear()` một lần mỗi frame sau khi thứ
tiêu thụ event đã chạy, không thì list phình vô hạn.

## Đi tiếp

- [Khái niệm cốt lõi](02-CONCEPTS.md) — thật ra chuyện gì xảy ra khi bạn ghi một giá trị.
- [Sample](../../../../ApexionGame.Entities.Stats.Samples/README.vi.md) — mười case bấm được, gồm phụ
  thuộc chéo owner, từ chối vòng lặp, và save/load.
- [Chỗ dễ vấp & FAQ](09-PITFALLS.md) — đọc trước con bug đầu tiên, đừng đọc sau.
