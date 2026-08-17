# Equipment System — Tổng quan

*[Weapon Logic](Equipment System - Weapon Logic.md) · [Index](README.md)*

## Trạng thái

| | |
|---|---|
| Phase | **A và B: ĐÃ TRIỂN KHAI** (2026-08-15). Combat (phase C) dựng tiếp lên trên — xem [Combat](Combat - Overview.md) |
| Chia phase | **A** = dữ liệu + trang bị (tài liệu này) · **B** = logic weapon ([Weapon Logic](Equipment System - Weapon Logic.md)) |
| Assembly sở hữu | `Game.Gameplay` (tạo mới trong feature này) |
| Namespace | `Game.Gameplay.Items`, `Game.Gameplay.Equipment` |
| Assembly kéo theo | `Game.Common`, `Game.Data`, `Game.Gameplay`, `Game.Gameplay.Tests` — **cả bốn đều chưa tồn tại** |
| Module EncosyTower | Databases, Collections (`ArrayMap`), Common (`Result`/`Option`), PolyEnumStructs, **UnionIds**, TypeWraps, EnumExtensions, AddressableKeys |
| Module first-party | `ApexionGame.Entities.Stats` (bản fork DOTS-free) |
| Bậc hiệu năng | **Tier 1** — xem §7 |
| Quyết định mở | không — DEC-004 đã chốt: `Assets/Game/Game.Gameplay.Tests` |

---

## 1. Tóm tắt yêu cầu

Hệ thống trang bị cho game kiểu **Escape from Duckov** — extraction looter, single-player. **Bao gồm
cả vũ khí**: kiếm, dao, súng.

Hai vòng hỏi đã chốt:

| Câu hỏi | Trả lời |
|---|---|
| Mô hình | **Slot + trọng lượng + stat modifier.** Không grid inventory ở lát cắt này |
| Phạm vi | **Data + logic + test.** Chưa UI, chưa persistence |
| Chỉ số | Nối vào **`ApexionGame.Entities.Stats`**, không tự cộng |
| Bằng chứng | **Test EditMode xanh, có số executed/passed** |
| Weapon | **Dữ liệu đầy đủ + trang bị được. Chưa có logic bắn/chém** |
| Id | **Chung một không gian id** — `ItemId` là `[UnionId]` |
| State runtime | **Có số đạn trong băng** |
| Logic weapon | **Có** — vòng đời đòn đánh + bắn/nạp đạn, chưa dò trúng, chưa sát thương |
| Projectile | **Chỉ là dữ liệu**, chưa mô phỏng vật thể bay |

Và một chỉ đạo: **không lấy code cũ.**

### Về schema cũ

Bốn asset đã commit còn giữ schema của thiết kế trước — `equipment`, `melee_weapon`, `ranged_weapon`,
`ammo`. Code sinh ra chúng đã biến mất.

Theo chỉ đạo, chúng **chỉ là bằng chứng về ý định**, không phải bản mẫu. Nhưng bằng chứng đó có giá:
`melee_weapon` có `_windUp/_active/_recoverySeconds` và `_arcDegrees`; `ranged_weapon` có `_ammo`,
`_fireMode`, `_magazineCapacity`, `_spreadDegrees`. Tức là ý định ban đầu **có** phần combat. Lát cắt
này mô hình hoá đủ dữ liệu đó nhưng **chưa** viết logic tấn công (DEC-009).

Mọi `_entries` đều rỗng nên không mất nội dung thật. Asset sinh lại từ schema mới.

## 2. Kết quả mong đợi

### 2.1 Trên đĩa

```
Assets/Game/
├── Game.Common/
│   ├── Game.Common.asmdef              name = rootNamespace = "Game.Common"
│   ├── csc.rsp                         -langversion:10
│   └── Ids/
│       ├── ItemId.cs                   [UnionId] — Equipment|MeleeWeapon|RangedWeapon|Ammo
│       ├── EquipmentId.cs              [WrapRecord]   ─┐
│       ├── MeleeWeaponId.cs            [WrapRecord]    │ bốn kind của ItemId
│       ├── RangedWeaponId.cs           [WrapRecord]    │
│       ├── AmmoId.cs                   [WrapRecord]   ─┘
│       ├── EquipmentSlot.cs            enum + [EnumExtensions]
│       ├── EquipmentSlotMask.cs        [Flags] enum + [EnumExtensions]
│       └── WeaponFireMode.cs           enum + [EnumExtensions]
├── Game.Data/
│   ├── Game.Data.asmdef
│   ├── csc.rsp
│   └── Items/
│       ├── ItemData.cs                 [Data] — BẢNG GỐC: mọi item đều có
│       ├── ItemTableAsset.cs           [DataTableAsset]
│       ├── EquipmentData.cs            [Data] — bảng mở rộng
│       ├── EquipmentTableAsset.cs
│       ├── MeleeWeaponData.cs          [Data] — bảng mở rộng
│       ├── MeleeWeaponTableAsset.cs
│       ├── RangedWeaponData.cs         [Data] — bảng mở rộng
│       ├── RangedWeaponTableAsset.cs
│       ├── AmmoData.cs                 [Data] — bảng mở rộng
│       ├── AmmoTableAsset.cs
│       └── StatModifierData.cs
├── Game.Gameplay/
│   ├── Game.Gameplay.asmdef
│   ├── csc.rsp
│   ├── Documentation~/                 ← tài liệu này
│   ├── Items/
│   │   ├── Common/ItemError.cs         [PolyEnumFactoryFor] + [PolyEnumStruct]
│   │   └── ItemCatalog.cs              tra cứu ItemId → dữ liệu (ArrayMap)
│   └── Equipment/
│       ├── EquipmentRules.cs           hàm thuần: slot, trọng lượng, số bản
│       ├── EquippedItem.cs             readonly record struct { ItemId, Option<int> Rounds }
│       ├── Loadout.cs                  CHỦ của slot → item, và số đạn trong băng
│       └── EquipmentStatBinding.cs     CHỦ của các modifier handle đã tạo
└── Game.Gameplay.Tests/
    ├── Game.Gameplay.Tests.asmdef
    └── Equipment/
        ├── ItemCatalogTests.cs
        ├── EquipmentRulesTests.cs
        ├── LoadoutTests.cs
        ├── MagazineTests.cs
        └── EquipmentStatBindingTests.cs
```

**~26 file nguồn + 4 asmdef.** Không scene, không prefab, không UI.

### 2.2 API công khai

```csharp
// Game.Common
[UnionId(Size = UnionIdSize.ULong, KindSettings = UnionIdKindSettings.PreserveOrder)]
[UnionIdKind(typeof(EquipmentId),    0, "Equipment")]
[UnionIdKind(typeof(MeleeWeaponId),  1, "MeleeWeapon")]
[UnionIdKind(typeof(RangedWeaponId), 2, "RangedWeapon")]
[UnionIdKind(typeof(AmmoId),         3, "Ammo")]
public readonly partial struct ItemId { }

public enum EquipmentSlot : byte
{
    Undefined = 0,
    Head, Body, Back, Hands, Legs, Feet, Trinket,
    PrimaryWeapon, SecondaryWeapon, MeleeWeapon,
}

// Game.Gameplay.Equipment
public readonly record struct EquippedItem(ItemId Id, Option<int> Rounds);

public sealed class Loadout
{
    public Loadout(ItemCatalog catalog, float weightCapacity);

    public float CarriedWeight { get; }                        // SUY RA, không lưu
    public Option<EquippedItem> GetEquipped(EquipmentSlot slot);

    public Result<EquipChange, ItemError> Equip(EquippedItem item, EquipmentSlot slot);
    public Result<EquippedItem, ItemError> Unequip(EquipmentSlot slot);   // TRẢ CẢ SỐ ĐẠN

    public Result<int, ItemError> SetRounds(EquipmentSlot slot, int rounds);
}

public readonly record struct EquipChange(
      EquippedItem Equipped
    , EquipmentSlot Slot
    , Option<EquippedItem> Replaced        // trả lại cả băng đạn của món bị thay
);
```

### 2.3 Cách biết là chạy

`unity test . --mode EditMode --filter "Game.Gameplay.Tests"` — xanh, có số, phủ:

| # | Nhóm | Khẳng định |
|---|---|---|
| 1–3 | Catalog | `ItemId` mỗi kind tra ra đúng bảng mở rộng · id không tồn tại → `Option.None` · kind sai bảng → `ItemError.KindMismatch` |
| 4 | Trang bị | Vào slot hợp lệ thành công |
| 5 | Trang bị | Slot không tương thích → `ItemError.SlotNotCompatible(id, slot)` |
| 6 | Trang bị | Đè lên slot đang có → `Replaced` chứa **cả item lẫn số đạn** cũ |
| 7 | Trang bị | Vượt sức chứa → `ItemError.OverWeight(...)`, loadout **không đổi** |
| 8 | Trang bị | Vượt số bản tối đa → `ItemError.TooManyCopies(id, max)` |
| 9 | Trang bị | Tháo slot rỗng → `ItemError.SlotEmpty(slot)` |
| 10 | Weapon | Súng vào slot vũ khí được; áo giáp vào slot vũ khí bị từ chối |
| 11 | Weapon | Kiếm/dao vào `MeleeWeapon` slot được |
| 12 | Băng đạn | Trang bị súng mới → số đạn khởi tạo theo `MagazineCapacity` |
| 13 | Băng đạn | `SetRounds` vượt sức chứa → `ItemError.MagazineOverflow(id, capacity)` |
| 14 | Băng đạn | **Tháo súng đang có 12 viên → giá trị trả về mang theo 12 viên** |
| 15 | Băng đạn | Tháo rồi trang bị lại đúng vật đó → số đạn giữ nguyên |
| 16 | Băng đạn | `SetRounds` trên slot không phải súng → `ItemError.NotARangedWeapon(slot)` |
| 17 | Trọng lượng | `CarriedWeight` bằng tổng trọng lượng đang đeo, sau nhiều thao tác |
| 18 | Chỉ số | Trang bị → chỉ số tăng đúng |
| 19 | Chỉ số | Tháo → chỉ số trở về **đúng giá trị ban đầu** |
| 20 | Chỉ số | Trang bị/tháo 100 lần → số modifier trên stat về 0 |
| 21 | Lỗi | `default(ItemError).ToString()` an toàn, ra `Undefined` |

**Test 14, 19, 20 là quan trọng nhất** — chúng chặn hai kiểu mất mát âm thầm mà §4 chỉ ra: mất băng
đạn khi tháo, và rò rỉ stat modifier.

---

## 3. Phân rã

Chia theo **thứ thay đổi cùng nhau**.

| Mảnh | Sở hữu | Vì sao tách |
|---|---|---|
| `ItemId` + 4 id thành phần, `EquipmentSlot`, `WeaponFireMode` | không gì — kiểu giá trị | Mọi tầng gọi tên; không mang luật |
| `ItemData` (**bảng gốc**) | nội dung chung mọi item | Trọng lượng/slot/modifier đúng cho **mọi** loại item |
| `EquipmentData` / `MeleeWeaponData` / `RangedWeaponData` / `AmmoData` | nội dung riêng từng loại | Mỗi loại có trường riêng; xem DEC-007 |
| `ItemCatalog` | tra cứu id → dữ liệu | Ranh giới dữ liệu ↔ luật; nơi đặt quyết định hiệu năng (§7) |
| `EquipmentRules` | **không gì** — hàm thuần | Đổi khi luật chơi đổi. Không state ⇒ dễ test nhất |
| `Loadout` | **slot → item, và số đạn** | State một nhân vật |
| `EquipmentStatBinding` | **modifier handle đã tạo** | Đổi khi cách nối stats đổi |
| `ItemError` | không gì | Hợp đồng lỗi |

**Không có `EquipmentManager`, không có `WeaponManager`.** Không mảnh nào tồn tại chỉ để điều phối.

## 4. Sở hữu state

| State | Chủ | Ai đọc | Ai được ghi | Vòng đời |
|---|---|---|---|---|
| item ở mỗi slot | **`Loadout`** | rules, binding, (UI sau) | chỉ `Loadout` | theo nhân vật; **chưa persist** |
| **số đạn trong băng mỗi slot** | **`Loadout`** | như trên | chỉ `Loadout`, qua `SetRounds`/`Equip` | **theo món đồ**, không theo slot — xem dưới |
| trọng lượng đang mang | **SUY RA** từ loadout + catalog | ai cũng được | **không ai** | tính lúc đọc, không lưu |
| modifier handle mỗi item đang đeo | **`EquipmentStatBinding`** | chính nó | chính nó | khớp đúng lần equip |
| giá trị chỉ số | **`StatStore` của stats fork** | ta chỉ đọc | accessor của stats | theo world |
| định nghĩa item | các table asset | mọi nơi | chỉ lúc import | bất biến khi chạy |

### Số đạn thuộc về món đồ, không thuộc về slot — điểm thiết kế mới

Thêm số đạn làm equipment **không còn là "slot nào giữ id nào"**. Hai khẩu súng cùng loại có **cùng
definition id** nhưng phải có số đạn riêng. Nghĩa là cái được trang bị là một **thực thể**, không
phải một định nghĩa.

Cách làm đúng về lâu dài là có `ItemInstanceId` và một sổ đăng ký thực thể. **Lát cắt này không làm
vậy** — chưa có inventory nào để chứa thực thể chưa đeo, nên dựng sổ đăng ký bây giờ là suy đoán.

Giải pháp nhỏ nhất mà vẫn đúng: **`Unequip` trả về `EquippedItem` mang theo số đạn**, và `Equip` nhận
vào `EquippedItem` chứ không phải `ItemId` trần.

Hệ quả: **không có chỗ nào làm mất số đạn một cách âm thầm.** `Loadout` giữ số đạn khi món đồ còn
đeo; lúc tháo, số đạn đi theo giá trị trả về, và người gọi quyết định cất nó vào đâu. Khi inventory
xuất hiện, nó nhận đúng giá trị đó — không phải sửa `Loadout`.

Test 14 và 15 tồn tại để chặn đúng chỗ này.

**Khi có persistence:** số đạn là state gốc, không suy ra được, nên nó **vào save**. Lúc đó hình
dạng của nó là hợp đồng vĩnh viễn — `midcore-save-migration`. Ghi ra đây để lần sau không ai coi nó
là chuyện nhỏ.

### Vì sao trọng lượng là suy ra

Lưu `_carriedWeight` song song với loadout tạo **hai bản của một sự thật**; chúng sẽ lệch ở nhánh
không ai test. Tính lúc đọc: 10 slot, tra `ArrayMap`. Nếu sau này đo được là đắt thì cache **thuộc về
`Loadout`**, vô hiệu hoá ngay trong `Equip`/`Unequip` — không bao giờ ở phía người đọc.

### Vì sao modifier handle phải có chủ

`TryAddStatModifier` trả về `StatModifierHandle`; `TryRemoveStatModifier` **cần đúng handle đó**.
Trang bị thêm N modifier, tháo phải gỡ đúng N cái ấy. Không giữ handle ⇒ **rò rỉ modifier**: chỉ số
lớn dần mỗi vòng equip/unequip, không gì báo lỗi, chỉ lộ sau nhiều giờ chơi.

`EquipmentStatBinding` giữ `ArrayMap<EquipmentSlot, FasterList<StatModifierHandle>>`. Test 19–20
chặn đúng lỗi này.

## 5. Giao tiếp

| Từ | Tới | Cơ chế | Vì sao |
|---|---|---|---|
| người gọi | `Loadout` | **gọi trực tiếp**, trả `Result<T, ItemError>` | Cần kết quả, và được phép biết `Loadout` |
| `Loadout` | `EquipmentRules` | **gọi trực tiếp** | Hàm thuần |
| `Loadout` | `ItemCatalog` | **gọi trực tiếp** | Tra cứu thuần |
| `Loadout` | `EquipmentStatBinding` | **gọi trực tiếp** | `Loadout` sở hữu chuyển đổi; binding thực thi |
| `EquipmentStatBinding` | accessor của stats | **gọi trực tiếp** | API thư viện |
| `Loadout` | hệ thống khác | **không gì** | Chưa có ai nghe |

Dòng cuối cố ý. Thêm event lúc này là suy đoán — chưa có subscriber, và event trả giá bằng việc không
lần được luồng điều khiển. Khi UI xuất hiện mới quyết, lúc đó đã có bằng chứng.

## 6. Phương án đã loại

| Phương án | Vì sao loại |
|---|---|
| **Grid inventory kiểu Tarkov/Duckov** | Lớn hơn bản thân equipment; slot + trọng lượng kiểm chứng tầng luật trước |
| **Logic tấn công trong phase này** | Là **hệ combat**: vòng đời đòn đánh, projectile, collision, hitbox. Lớn hơn equipment và cần hệ khác — DEC-009 |
| **Lặp cột trang bị ở từng bảng weapon** | `slotMask`/`weight`/`modifiers` giống nhau ở 4 bảng ⇒ sửa một luật phải sửa bốn chỗ — DEC-007 |
| **Một bảng item duy nhất có cột `kind`** | Phần lớn cột rỗng ở phần lớn dòng; runtime phải suy ra loại từ cột nào trống |
| **Id riêng từng bảng** | `Loadout` phải biết mọi loại; thêm loại mới là sửa `Loadout` — sai hướng phụ thuộc |
| **Số đạn gắn theo slot, mất khi tháo** | Mất dữ liệu âm thầm — §4 |
| **Sổ đăng ký `ItemInstanceId` ngay bây giờ** | Chưa có inventory chứa thực thể chưa đeo ⇒ suy đoán. Giá trị trả về của `Unequip` đã đủ và không mất gì |
| **Equipment tự cộng tổng chỉ số** | Nhân đôi stats fork; chắc chắn phải viết lại |
| **Lưu `CarriedWeight` thành field** | Hai bản của một sự thật |
| **`enum ItemError`** | Luật cứng: `TError` không bao giờ là enum phẳng |
| **`Loadout` là `MonoBehaviour`** | Không test được nếu không dựng scene |

## 7. Bậc hiệu năng

**Chọn: Tier 1** — cấu trúc dữ liệu khớp pattern truy cập.

Cụ thể: `ItemCatalog` dùng **`ArrayMap`** cho bảng gốc và mỗi bảng mở rộng, thay vì quét tuyến tính.
`Loadout` giữ mảng cố định 10 phần tử theo slot.

**Vì sao không cao hơn.** Equip/unequip/nạp đạn là hành động người chơi, không chạy mỗi frame. N = 10
slot và vài trăm dòng bảng. Không có công việc độc lập theo phần tử ở quy mô lớn ⇒ tier 4–5 không áp
dụng. Và **chưa có số đo**, mà đó là điều kiện bắt buộc để lên tier 2 trở lên.

**Vì sao không thấp hơn.** `CarriedWeight` duyệt loadout và tra catalog mỗi lần đọc; HUD nhiều khả
năng đọc nó mỗi frame. Với **hai lần tra** cho mỗi món (bảng gốc + bảng mở rộng), quét tuyến tính sẽ
thấy rõ. Tier 1 gần như miễn phí và **đắt để sửa sau** vì kiểu dữ liệu ngấm vào mọi call site.

**Ghi chú:** `performance.device_tiers` trong profile là `unknown — ask`. Không ngân sách frame nào
được nêu ở đây. **Xem lại khi**: có tier thiết bị thật, hoặc `CarriedWeight` chuyển sang chạy mỗi
frame, hoặc số item lên hàng nghìn (grid inventory).

## 8. Nối vào stats fork

Dùng đúng API trong `ApexionGame.Entities.Stats.Documentation~/guide/01-GETTING-STARTED.md`:

```csharp
[StatSystem(StatDataSize.Size8)]
public static partial class GameStatSystem { }

[StatCollection(typeof(GameStatSystem), 1000)]
public partial struct CharacterStats
{
    [StatData(StatVariantType.Float)] public partial struct MaxHealth { }
    [StatData(StatVariantType.Float)] public partial struct Armor { }
    [StatData(StatVariantType.Float)] public partial struct MoveSpeed { }
    [StatData(StatVariantType.Float)] public partial struct CarryCapacity { }
}
```

Ba ràng buộc rút từ tài liệu của fork, **không đoán**:

1. **`StatModifier` có bảy `partial` hook**, đặt sai `readonly` khiến compiler *âm thầm* không khớp
   partial — không chẩn đoán nào. Dùng code-refactor *Generate stat modifier skeleton*.
2. **`WorldData` không thread-safe, không reentrant.** Lát cắt này chạy toàn bộ trên luồng chính, và
   điều đó được ghi lại chứ không giả định ngầm.
3. **Thứ tự dispose: `WorldData` trước, rồi `StatStore`.**

`StatStore` do **người gọi** sở hữu, không phải equipment. `EquipmentStatBinding` nhận accessor +
owner handle từ ngoài — state của stats thuộc stats, ta chỉ mượn.

## 9. Các bước

| # | Hành động | Xong khi | File |
|---|---|---|---|
| 1 | `Game.Common.asmdef` — tham chiếu `EncosyTower.Core`, **chép nguyên khối `versionDefines`**; `csc.rsp` `-langversion:10` | Compile; một `#if UNITASK` thử nghiệm cho ra **true** | `Assets/Game/Game.Common/**` |
| 2 | 4 id thành phần `[WrapRecord]` + `ItemId` `[UnionId]` + `EquipmentSlot`, `EquipmentSlotMask`, `WeaponFireMode` | Compile qua Unity; `ItemId.Kind`, `TryParse` do generator sinh đã tồn tại | `Game.Common/Ids/*.cs` |
| 3 | `Game.Data.asmdef` + `ItemData` (**bảng gốc**) + `ItemTableAsset` | Compile; `Get_X()` sinh ra tồn tại | `Assets/Game/Game.Data/**` |
| 4 | 4 bảng mở rộng: `EquipmentData`, `MeleeWeaponData`, `RangedWeaponData`, `AmmoData` + table asset của mỗi bảng | Compile; `RangedWeaponData.AmmoId` tham chiếu `AmmoId` có kiểu | `Game.Data/Items/*.cs` |
| 5 | `Game.Gameplay.asmdef` (+ `Game.Common`, `Game.Data`, `ApexionGame.Entities.Stats`) + `ItemError` theo mẫu `MachineError.cs` — có `Undefined`, `Prefix`, **`ToFixedString()`** | Compile; `default(ItemError).ToString()` chạy được | `Game.Gameplay/Items/Common/` |
| 6 | `ItemCatalog` — `ArrayMap` cho bảng gốc + mỗi bảng mở rộng; tra theo `ItemId.Kind` | Test 1–3 xanh | `Game.Gameplay/Items/` |
| 7 | `EquipmentRules` — hàm thuần: slot tương thích, sức chứa, số bản | Không giữ state nào; test 4–11 xanh | `Game.Gameplay/Equipment/` |
| 8 | `EquippedItem` + `Loadout` — mảng 10 slot, `Equip`/`Unequip`/`SetRounds`, `CarriedWeight` **suy ra** | **Không có field `_carriedWeight` nào**; test 12–17 xanh | `Game.Gameplay/Equipment/` |
| 9 | `EquipmentStatBinding` — giữ `StatModifierHandle` theo slot, gỡ đúng chúng lúc tháo | Test 18–20 xanh | `Game.Gameplay/Equipment/` |
| 10 | `Game.Gameplay.Tests.asmdef` (**xem DEC-004**) + 21 test §2.3 | `unity test --mode EditMode` xanh, executed = passed, **executed > 0** | `Assets/Game/Game.Gameplay.Tests/**` |
| 11 | Sinh lại 4 table asset theo schema mới, thêm `item_table_asset`; bỏ asset cũ | Asset load được, `_entries` khớp schema mới | `Assets/Game/Addressables/…/database/` |
| 12 | Cập nhật `.claude/project-profile.md`: `assembly_map.planned` → đã tồn tại; thêm test assembly; thêm `item` vào `data_pipeline.tables` | Profile khớp thực tế trên đĩa | `.claude/project-profile.md` |

Bước 1 chặn tất cả. Bước 7–9 phụ thuộc 2–6. Bước 10 phụ thuộc 7–9.

## 10. Quyết định

### DEC-001 — Slot + trọng lượng + modifier, chưa grid · **chốt**

Grid inventory là hệ thống lớn hơn equipment; làm tầng luật trước để có thứ kiểm chứng được.

### DEC-002 — Chỉ số qua stats fork · **chốt**

Kéo theo ràng buộc thật ở §4: phải giữ `StatModifierHandle`.

### DEC-003 — `CarriedWeight` là suy ra · **chốt**

### DEC-004 — Test assembly đặt ở đâu · **MỞ — cần anh**

Profile ghi *"tests tập trung ở `ApexionGame.Tests.EditorMode`"*. **Không áp dụng được**: assembly đó
nằm trong package, mà package **không thể tham chiếu code trong `Assets/`** — phụ thuộc một chiều.

**Khuyến nghị:** tạo `Assets/Game/Game.Gameplay.Tests`, và sửa quy ước trong profile thành *"test của
package ở `ApexionGame.Tests.EditorMode`; test của code game ở `Assets/Game/<Assembly>.Tests`"*.
Ràng buộc kỹ thuật, không phải sở thích.

### DEC-005 — Không giữ tương thích với asset cũ · **chốt**

### DEC-006 — `ItemId` là `[UnionId]` ngay từ đầu · **chốt** *(thay quyết định cũ ở bản 1)*

Bản 1 hoãn UnionId vì chỉ có equipment — một trường hợp, chưa thấy hình dạng. **Weapon vào phạm vi
làm nó thành bốn trường hợp thật**, nên hình dạng đã rõ và đây không còn là suy đoán.

Lợi ích cụ thể: `Loadout` giữ `ItemId` nên **không cần biết có bao nhiêu loại item**. Thêm loại mới
(consumable, phụ kiện) là thêm một `[UnionIdKind]` và một bảng — **không sửa `Loadout`**.

### DEC-007 — Bảng gốc `item` + bảng mở rộng theo loại · **chốt**

Bốn loại đều cần `displayKey`, `weight`, `slotMask`, `maxEquippedCopies`, `view`, `modifiers`. Ba
cách:

| Cách | Vì sao không |
|---|---|
| Lặp cột ở cả 4 bảng | Sửa một luật phải sửa bốn chỗ; bốn chỗ để lệch nhau |
| Một bảng có cột `kind` | Phần lớn cột rỗng ở phần lớn dòng; runtime suy ra loại từ cột nào trống |
| **Bảng gốc + bảng mở rộng, nối theo `ItemId`** | **Chọn** |

Cái giá: một lần join, và một luật validate ở tầng bất biến — *mọi `ItemId` kind `RangedWeapon` phải
có dòng trong `ranged_weapon`*. Luật đó chạy lúc import, không phải lúc chơi.

`ammo` cũng nằm trong bảng gốc dù không đeo được: nó có trọng lượng, và `slotMask = None` nói rõ là
không vào slot nào.

### DEC-008 — Số đạn đi theo giá trị trả về của `Unequip` · **chốt**

Xem §4. Không dựng sổ đăng ký thực thể lúc này — chưa có inventory chứa thực thể chưa đeo. `Unequip`
trả `EquippedItem` mang theo số đạn ⇒ **không có chỗ nào mất dữ liệu âm thầm**, và khi inventory xuất
hiện nó nhận đúng giá trị đó mà không phải sửa `Loadout`.

### DEC-009 — Logic tấn công **có**, nhưng ở Phase B · **đảo quyết định bản 2**

Bản 2 hoãn hẳn logic tấn công. Anh yêu cầu đưa vào, nên nó vào — thiết kế đầy đủ ở
[Weapon Logic](Equipment System - Weapon Logic.md).

Ranh giới giữ nguyên chỗ khác: Phase B làm **vòng đời đòn đánh, chế độ bắn, tốn đạn, nạp đạn** —
tất tần tật là logic thuần theo thời gian, test được 100% không cần scene. **Không** dò trúng,
**không** sát thương, **không** vật thể bay. Ba thứ đó cần physics và một mô hình mục tiêu, tức là
một hệ combat riêng.

Mô hình dữ liệu đầy đủ ngay từ Phase A vì cột là **schema**, và schema đổi sau thì phải đổi cả sheet
lẫn code lẫn asset cùng lúc.

### DEC-011 — Tách hai phase, plan cả hai ngay · **chốt**

Phase A dựng **toàn bộ `Assets/Game` từ con số không** — 4 assembly chưa từng compile. Gộp logic
weapon vào cùng một lần nghĩa là nếu hỏng ở bước asmdef thì phải gỡ ngược qua ~40 file chưa bao giờ
biên dịch được.

Nên: **A phải xanh trước khi bắt đầu B.** Nhưng thiết kế của B viết ngay bây giờ, vì nó quyết định
vài cột trong schema của A — nếu để sau thì gần như chắc chắn phải mở lại bảng dữ liệu.

### DEC-012 — Projectile chỉ là dữ liệu ở cả hai phase · **chốt**

`projectileSpeed/radius/range` là cột trong bảng và đi kèm sự kiện bắn, nhưng **không có vật thể nào
bay**. Vật thể bay cần pooling, tick mỗi frame và physics — đó là lúc bậc hiệu năng thực sự đổi, và
nó thuộc hệ combat.

### DEC-010 — `EquipmentSlot` có ba slot vũ khí · **chốt**

`PrimaryWeapon`, `SecondaryWeapon`, `MeleeWeapon` — đúng kiểu Duckov: hai vị trí súng và một cận
chiến. `slotMask` của từng món quyết định nó vào được đâu, nên đổi bố cục slot sau này là đổi **dữ
liệu**, không phải đổi code.
