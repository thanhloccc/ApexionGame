# Combat — Tổng quan

*[Lộ trình](Gameplay - Roadmap.md) · [Weapon Logic](Equipment System - Weapon Logic.md) · [Index](README.md)*

## Trạng thái

| | |
|---|---|
| Phase | **C — đã triển khai**, 105/105 test EditMode xanh |
| Assembly đích | `Game.Gameplay` (không tạo assembly mới) |
| Namespace đích | `Game.Gameplay.Combat` |
| Module EncosyTower | `Collections` (`ArrayMap`, `FasterList`, `FasterListPool`), `Common` (`Option`, `Result`, `Success`), `PolyEnum`, `EnumExtensions`, `PubSub` |
| Package ApexionGame | `Entities.Stats` (đọc `Armor`, `MaxHealth`) |
| Unity deps | `Unity.Mathematics` (`float3`, `math.*`) |
| Bậc hiệu năng | **Tier 1 — managed, main thread** (§7) |
| Quyết định mở | DEC-005 |

---

## 1. Tóm tắt yêu cầu

Làm phần **đánh nhau**: đòn đánh trúng ai, trúng thì mất bao nhiêu máu, hết máu thì chết.

Vòng hỏi đáp chốt: mốc đang nhắm là **vòng chơi tối giản chạy được**, nên Combat chỉ làm
đủ để "bắn — trúng — mất máu — chết" hoạt động và **đo được bằng test**, không làm VFX,
không làm UI, không làm trạng thái bị thương/hồi máu/hiệu ứng kéo dài.

Phase này còn vá luôn **hai seam đang hở** từ phase A/B, vì cả hai nằm đúng trên đường đi:

- `EquipmentStatBinding` không code sản phẩm nào gọi → trang bị đồ không cộng chỉ số.
- `WeaponController.Events` không ai rút → bắn ra sự kiện rồi rơi vào hư không.

**Điểm thuận lợi cần nói rõ:** các bảng dữ liệu **đã có sẵn** mọi trường Combat cần —
`MeleeWeapon.Range/Radius/ArcDegrees`, `RangedWeapon.ProjectileSpeed/ProjectileRadius/ProjectileRange/SpreadDegrees`,
`Ammo.DamageMultiplier/ArmorPenetration`, `Equipment.ArmorRating`. Phase A/B đã đặt sẵn
hình học và sát thương mà chưa ai đọc. Phase C **tiêu thụ dữ liệu đã có**, gần như không
thêm trường mới.

---

## 2. Đầu ra kỳ vọng

### 2.1 Cây file sau khi xong

```
Assets/Game/Game.Gameplay/Combat/
├── Common/
│   ├── CombatError.cs           PolyEnum — hợp đồng lỗi
│   ├── CombatEvent.cs           CombatEventKind + CombatEvent
│   └── DamageInfo.cs            gói sát thương một đòn, trước khi trừ giáp
├── CombatantId.cs               [WrapRecord] over int — handle runtime
├── CombatantSpatial.cs          struct: vị trí, hướng, bán kính, phe, còn sống
├── CombatantRegistry.cs         mảng liền khối + tra cứu; mọi truy vấn không gian
├── Health.cs                    struct: máu hiện tại, ApplyDamage → HealthChange
├── DamageResolver.cs            static thuần: raw + giáp + xuyên giáp → cuối cùng
├── HitQuery.cs                  static thuần: quét cung (gần) và quét tia (xa)
├── Combatant.cs                 gộp per-nhân-vật — CHỖ VÁ seam orphan
├── AttackResolver.cs            đường ống một tick: rút → truy vấn → tính → phát
└── CombatWorld.cs               sở hữu StatStore + các Combatant + Tick
```

Không tạo assembly mới. `Game.Gameplay.asmdef` thêm `Unity.Mathematics`.

### 2.2 Người viết code sẽ gọi như thế nào

```csharp
// dựng thế giới một lần
using var world = new CombatWorld(catalog, capacity: 64);

// tạo hai nhân vật
var hero    = world.CreateCombatant(team: 0, weightCapacity: 40f);
var bandit  = world.CreateCombatant(team: 1, weightCapacity: 20f);

// trang bị — MỘT lời gọi làm đủ ba việc: loadout + chỉ số + máy vũ khí
world[hero].Equip(new EquippedItem(TestItems.Pistol, rounds: 12), EquipmentSlot.PrimaryWeapon);

// đặt vị trí (phase D sẽ đồng bộ từ Transform; phase C tự đặt tay)
world.Registry.SetTransform(hero,   position: float3.zero, forward: math.forward());
world.Registry.SetTransform(bandit, position: new float3(0f, 0f, 3f), forward: -math.forward());

// bắn
world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);
world.Tick(deltaTime: 1f / 60f);

// đọc kết quả
foreach (var e in world.Events)
{
    // CombatEventKind.DamageDealt: e.Target == bandit, e.Amount == sát thương sau giáp
    // CombatEventKind.Died:        phát đúng MỘT lần, ngay tick máu chạm 0
}
```

### 2.3 Hành vi quan sát được

| Việc làm | Kết quả |
|---|---|
| Trang bị mũ | `Armor` của nhân vật tăng đúng bằng chỉ số của mũ |
| Tháo mũ | `Armor` về **đúng** giá trị gốc, không sót modifier |
| Bắn về phía địch trong tầm | Địch mất máu; sinh `DamageDealt` |
| Bắn về phía địch **ngoài** `ProjectileRange` | Không mất máu, không sinh sự kiện |
| Bắn về phía **đồng đội** (cùng `team`) | Không mất máu — lọc phe trước khi tính |
| Đánh gần với địch ở sau lưng | Không trúng — nằm ngoài `ArcDegrees` |
| Sát thương làm máu ≤ 0 | Sinh `Died` **đúng một lần**; các đòn sau không sinh thêm |
| Địch giáp cao | Sát thương giảm theo §6.3, **không bao giờ âm, không bao giờ bằng 0 tuyệt đối** |
| Đạn có `ArmorPenetration` | Giáp hiệu dụng giảm tương ứng, sát thương lên cao |

### 2.4 Cách kiểm chứng

```powershell
unity command run_tests --project-path=<repo> --non-interactive --mode EditMode `
  --filter "Game.Gameplay.Tests"
```

**Kết quả thực tế: 105/105 xanh, 6.51s** — 48 test cũ còn nguyên, 57 test mới. Không test nào cần Play Mode.

---

## 3. Mục tiêu và không phải mục tiêu

**Mục tiêu**

1. Đòn đánh gần và đòn bắn xa tìm được mục tiêu, bằng hình học thuần, test được EditMode.
2. Sát thương tính từ dữ liệu đã có: sát thương gốc × hệ số đạn, trừ giáp có tính xuyên giáp.
3. Máu hiện tại có **một** chủ sở hữu rõ ràng, và cái chết là sự kiện xảy ra đúng một lần.
4. Vá seam `EquipmentStatBinding` — trang bị đồ phải cộng chỉ số trong code sản phẩm.
5. Vá seam `WeaponController.Events` — có người rút và xử lý.

**Không phải mục tiêu** (nói rõ để khỏi trôi phạm vi)

- VFX, âm thanh, số sát thương bay lên, HUD máu.
- Đạn bay có mô phỏng thời gian (projectile chuyển động qua nhiều frame) — xem DEC-003.
- Trạng thái kéo dài: chảy máu, choáng, hồi máu, miễn nhiễm.
- Bắn xuyên tường / che chắn — chưa có level geometry.
- Hồi sinh, xác chết, rơi đồ.
- AI biết đánh trả — đó là phase F.
- Multiplayer / netcode.

---

## 4. Phân rã — các mảnh, và vì sao là chúng

| # | Mảnh | Việc **duy nhất** nó làm | Vì sao tách riêng |
|---|---|---|---|
| 1 | `CombatantId` | định danh một combatant | `[WrapRecord]` over `int` — không lẫn với `ItemId`. **Không** đặt ở `Game.Common` vì nó là handle runtime, không bao giờ xuất hiện trong dữ liệu. |
| 2 | `CombatantSpatial` | struct dữ liệu không gian | Tách khỏi `Combatant` để vòng quét O(N) chạy trên mảng struct liền khối — xem §7 |
| 3 | `CombatantRegistry` | ai đang ở đâu, phe nào, còn sống không | **Chỗ duy nhất** biết về không gian. Muốn đổi sang spatial hash sau này thì sửa đúng file này. |
| 4 | `Health` | máu hiện tại + phép trừ máu | `struct` thuần, không phụ thuộc gì — test được riêng, và quy tắc "chết đúng một lần" nằm gọn một chỗ |
| 5 | `DamageResolver` | công thức sát thương | `static` thuần, không state. Công thức là thứ sẽ chỉnh nhiều nhất; tách ra để chỉnh mà không đụng ai. |
| 6 | `HitQuery` | hình học: quét cung, quét tia | `static` thuần, ghi vào buffer do caller cấp → không cấp phát. Không biết gì về máu hay sát thương. |
| 7 | `Combatant` | gộp mọi thứ thuộc **một** nhân vật | **Đây là chủ sở hữu còn thiếu** khiến `EquipmentStatBinding` mồ côi — xem DEC-001 |
| 8 | `CombatWorld` | sở hữu `StatStore`, các `Combatant`, và vòng `Tick` | `StatStore` là cấp thế giới (`initialOwnerCapacity`), không thể để trong `Combatant` |
| 9 | `CombatEvent` | báo ra ngoài: đã gây sát thương, đã chết | Combat không được biết về VFX/UI/AI |
| 10 | `CombatError` | hợp đồng lỗi | Theo luật `structured-errors` — PolyEnum, không phải enum phẳng |
| 11 | `AttackResolver` | đường ống một tick | **Thêm lúc triển khai** — xem DEC-010. Không sở hữu state; mọi thứ nó đọc/ghi đều là tham số |

**Vì sao `Combatant` không phải là chỗ nhét cho tiện.** Nó sở hữu 5 thứ, nghe như thần
thánh. Nhưng cả 5 có **cùng vòng đời** (một nhân vật) và bị buộc vào nhau bởi **một giao
dịch duy nhất**: trang bị món đồ = ghi loadout + cộng chỉ số + gắn máy vũ khí, cả ba
phải cùng thành công hoặc cùng không. Hôm nay không ai làm chủ giao dịch đó, nên nó
không xảy ra. `Combatant` chính là chủ sở hữu đã luôn thiếu, không phải miếng vá.

---

## 5. Sở hữu state — ai được ghi cái gì

Bảng này là phần quan trọng nhất của plan. Mọi bug "hai chỗ cùng sửa một thứ" đều bắt
đầu từ một dòng sai ở đây.

| State | Chủ sở hữu | Ai được **ghi** | Ai chỉ được **đọc** |
|---|---|---|---|
| Máu hiện tại | `Health` (trong `Combatant`) | **chỉ** `CombatWorld.Tick` qua `Combatant.ApplyDamage` | ai cũng đọc được qua `Combatant.Health` |
| Máu tối đa, Giáp, Tốc chạy | `StatStore` (trong `CombatWorld`) | **chỉ** `EquipmentStatBinding` | `DamageResolver`, phase D |
| Handle modifier đã áp | `EquipmentStatBinding` | chính nó | không ai |
| Món đồ đang mặc + số đạn | `Loadout` | **chỉ** `Combatant.Equip/Unequip` | `WeaponController`, `ItemCatalog` |
| Trạng thái máy vũ khí | `WeaponController` | chính nó, qua trigger | `CombatWorld` đọc qua `Events` |
| Vị trí, hướng, phe | `CombatantRegistry` | phase D (đồng bộ từ `Transform`); phase C đặt tay trong test | `HitQuery` |
| Cờ còn sống | `CombatantRegistry` | **chỉ** `CombatWorld` khi máu chạm 0 | `HitQuery` lọc mục tiêu |
| Buffer sự kiện vũ khí | `WeaponController` | chính nó ghi, `CombatWorld` xoá sau khi rút | — |
| Buffer sự kiện combat | `CombatWorld` | chính nó | người gọi đọc, xoá đầu tick sau |

**Chỗ dễ sai nhất, ghi rõ:** máu tối đa nằm trong stat store, máu hiện tại thì không.
Hai chủ sở hữu khác nhau cho hai thứ nghe rất giống nhau. Lý do ở DEC-002.

**Cờ còn sống nằm ở registry chứ không ở `Health`** — dù `Health` biết máu bằng 0. Vì
`HitQuery` phải lọc người chết ra khỏi mảng quét mà **không** được deref sang phía
managed; cờ phải nằm cùng chỗ với vị trí. `Health` vẫn là nguồn sự thật, registry là bản
sao do `CombatWorld` cập nhật cùng tick. Một chủ ghi duy nhất, nên không lệch được.

---

## 6. Giao tiếp — cơ chế nào giữa mảnh nào

| Từ → Đến | Cơ chế | Vì sao **cơ chế này** |
|---|---|---|
| `WeaponController` → `CombatWorld` | **rút buffer** (`Events` + `ClearEvents`) | Đã có sẵn. Kiểu pull giữ **thứ tự tất định** trong một tick — quan trọng khi hai người bắn nhau cùng frame. Push/callback sẽ khiến thứ tự phụ thuộc vào thứ tự duyệt map. |
| `CombatWorld` → `HitQuery` | gọi hàm static, buffer do caller cấp | Hình học thuần, không state, không cấp phát |
| `CombatWorld` → `Combatant.ApplyDamage` | **gọi trực tiếp** | Cùng tick, chủ sở hữu đã biết rõ. Thêm indirection ở đây chỉ làm khó debug. |
| `Combatant` → `Loadout`/`Binding`/`Weapons` | **gọi trực tiếp, theo thứ tự cố định** | Đây *là* giao dịch trang bị (§6.1). Phải tuần tự và roll back được. |
| `CombatWorld` → ngoài (VFX, HUD, AI) | **`EncosyTower.PubSub`** | Tập người nghe **mở** và chưa tồn tại; thứ tự không quan trọng. Đúng chỗ để dùng PubSub. |

**Vì sao hai cơ chế khác nhau chứ không dùng một.** Trong tick cần tất định và không cấp
phát → buffer. Ra ngoài tick cần nới lỏng phụ thuộc → PubSub. Dùng PubSub cho cả phần
trong tick sẽ làm thứ tự sát thương phụ thuộc thứ tự đăng ký; dùng buffer cho cả phần ra
ngoài sẽ buộc Combat phải biết trước có những ai nghe.

**PubSub bật ở phase nào:** `CombatWorld` luôn ghi vào buffer nội bộ; việc publish ra
PubSub là **tuỳ chọn** bật bằng một cờ trong constructor. Phase C để **tắt** — chưa ai
nghe, và test đọc thẳng buffer. Xem DEC-005.

### 6.1 Giao dịch trang bị — và chuyện roll back

`Combatant.Equip` làm ba việc theo đúng thứ tự:

```
1. _loadout.Equip(item, slot)        → hỏng thì DỪNG, chưa đụng gì
2. _binding.Apply(slot, item.Id, …)  → cộng chỉ số
3. _weapons.OnEquipped(slot)         → gắn máy vũ khí
```

**Bước 3 hỏng thì phải hoàn tác bước 1 và 2.** Nếu không, loadout sẽ chứa một khẩu súng
mà `WeaponController` không gắn máy — và `RequestAttack` sẽ **im lặng không làm gì**.
Loại lỗi tệ nhất: không có exception, không có log, chỉ là bấm chuột mà không bắn.

`Unequip` chạy ngược lại: `_weapons.OnUnequipped` → `_binding.Remove` → `_loadout.Unequip`.
Gỡ máy vũ khí trước để nó không tick trên một slot đang rỗng dở chừng.

### 6.2 Một tick của `CombatWorld` diễn ra thế nào

```
CombatWorld.Tick(dt)
│
├─ 1. xoá buffer sự kiện combat của tick trước
│
├─ 2. mỗi combatant: weapons.Tick(dt)              ← máy vũ khí chạy, ghi WeaponEvent
│
├─ 3. mỗi combatant: rút weapons.Events
│     └─ với mỗi AttackActivated / ShotFired:
│        ├─ mượn buffer hit từ FasterListPool<CombatantId>
│        ├─ HitQuery.Melee(...) hoặc HitQuery.Ranged(...)   ← lọc phe + còn sống + tầm
│        ├─ với mỗi hit:
│        │  ├─ DamageResolver.Resolve(raw, ammo, giáp mục tiêu)
│        │  ├─ target.ApplyDamage(...)  → HealthChange
│        │  ├─ ghi CombatEvent.DamageDealt
│        │  └─ nếu vừa chết: registry.MarkDead(id), ghi CombatEvent.Died
│        └─ trả buffer về pool
│     └─ weapons.ClearEvents()
│
└─ 4. (tuỳ chọn) publish buffer combat ra PubSub
```

Bước 2 chạy **hết** cho mọi người trước khi bước 3 bắt đầu. Nghĩa là trong một tick, ai
cũng đánh vào trạng thái **đầu tick** — hai người bắn nhau cùng frame thì cả hai đều
trúng, không phụ thuộc ai được duyệt trước. Đó là hành vi đúng cho đấu súng, và nó là
**hệ quả trực tiếp** của việc tách hai bước, không phải tình cờ.

### 6.3 Công thức sát thương

```csharp
// DamageResolver.Resolve
raw            = weaponBaseDamage * ammoDamageMultiplier      // ammo = 1.0 cho vũ khí gần
effectiveArmor = max(0, targetArmor * (1 - ammoArmorPenetration))
mitigation     = effectiveArmor / (effectiveArmor + ARMOR_CONSTANT)   // ARMOR_CONSTANT = 100
final          = max(MIN_DAMAGE, raw * (1 - mitigation))              // MIN_DAMAGE = 1
```

Chọn dạng `armor / (armor + K)` chứ không phải `raw - armor` vì:

- **không bao giờ ra số âm**, không cần kẹp ở hai đầu;
- **giảm dần đều** — giáp 100 chặn 50%, giáp 200 chặn 67%, không có ngưỡng mà thêm giáp
  thành vô dụng hoặc bất tử;
- một hằng số `K` duy nhất để cân bằng, đọc được ý nghĩa: *giáp bằng K thì chặn một nửa*.

`MIN_DAMAGE = 1` để mục tiêu giáp cực cao vẫn chết được — tránh trạng thái bế tắc mà
người chơi bắn mãi không thấy gì xảy ra.

---

## 7. Bậc hiệu năng — **Tier 1: managed, main thread**

### Quy mô thật của bài toán

| Đại lượng | Ước lượng | Căn cứ |
|---|---|---|
| Combatant cùng lúc | ~20–60 | game kiểu Duckov, cảnh nhỏ |
| Đòn đánh / giây (tất cả) | ~10–30 | súng liên thanh 10 viên/s, vài người bắn |
| Phép kiểm tra khoảng cách / giây | **~600–1800** | đòn/giây × số combatant |

600–1800 phép `distancesq` mỗi giây. Đó là **vài microsecond**. Bài toán này nhỏ.

### Vì sao không lên cao hơn (Job + Burst)

- Việc **thưa và theo sự kiện**, không phải mỗi frame mỗi entity. Chi phí schedule một
  job đã lớn hơn toàn bộ công việc.
- **DOTS không cài** trong project — xem `.claude/CLAUDE.md`. Dùng job sẽ buộc registry
  sang bộ nhớ native.
- Sẽ **mất khả năng test EditMode**, mà 48 test hiện có đều là EditMode thuần. Đánh đổi
  này quá đắt cho thứ không đo được là chậm.

### Vì sao không xuống thấp hơn (`List` + `foreach` + LINQ + `Vector3`)

- Vòng quét cung là chỗ O(N) **duy nhất** và chạy mỗi đòn đánh. Cho nó chạy trên
  `FasterList<CombatantSpatial>` liền khối, đọc `float3` bằng `math.distancesq` /
  `math.dot`, tốn **đúng bằng** công viết kiểu ngây thơ nhưng thân thiện cache.
- Buffer hit **mượn từ `FasterListPool<CombatantId>`** → không cấp phát mỗi đòn đánh.
  Đấu súng gây rác mỗi viên đạn là kiểu GC spike tệ nhất: đúng lúc đông nhất.
- Tách `CombatantSpatial` khỏi `Combatant` để vòng quét **không deref sang class**. Chi
  phí: một mảng phải giữ đồng bộ. Lợi ích: hot loop sạch, và sau này jobify được mà
  không đụng gì khác.

### Khi nào xem lại

| Dấu hiệu | Phản ứng |
|---|---|
| Combatant > ~500 | Thêm spatial hash **bên trong** `CombatantRegistry` — API không đổi |
| AoE tick mỗi frame thay vì mỗi đòn | Lúc đó mới là bài toán per-frame; đo lại rồi tính |
| Profiler chỉ đúng `HitQuery` | `CombatantSpatial` đã là struct liền khối, jobify được |

Không làm sẵn thứ nào ở trên. Cả ba đều là **một file đổi**, và đó chính là lý do phân rã
như §4.

---

## 8. Mô hình dữ liệu — kiểu và collection

| Kiểu | Hình dạng | Collection / lý do |
|---|---|---|
| `CombatantId` | `[WrapRecord]` over `int` | id có kiểu, không lẫn `ItemId` |
| `CombatantSpatial` | `struct`: `float3 Position`, `float3 Forward`, `float Radius`, `byte Team`, `bool IsAlive` | 33 byte, blittable. `float3` vì nằm trong vòng quét |
| `CombatantRegistry` | `FasterList<CombatantSpatial>` (dense) + `ArrayMap<CombatantId, int>` (id → chỉ số) | mảng dense để quét liền khối; map để tra O(1). Xoá dùng swap-back. |
| `Health` | `struct`: `float Current`, `bool IsDead` | máu tối đa **không** ở đây — đọc từ stat store lúc cần |
| `HealthChange` | `readonly record struct`: `float Applied`, `bool JustDied` | `JustDied` chỉ true đúng **một** lần → quy tắc "chết một lần" nằm trong `Health` |
| `DamageInfo` | `readonly record struct`: `CombatantId Attacker`, `ItemId Weapon`, `float Raw`, `float AmmoMultiplier`, `float ArmorPenetration` | gói tham số trước khi trừ giáp |
| `CombatEvent` | `readonly record struct`: `CombatEventKind Kind`, `CombatantId Attacker`, `CombatantId Target`, `ItemId Weapon`, `float Amount`, `float AtTime` | giống hệt hình dạng `WeaponEvent` — nhất quán có chủ ý |
| `CombatEventKind` | `[EnumExtensions] enum : byte` | `Undefined`, `DamageDealt`, `Died` |
| `CombatError` | `[PolyEnumFactoryFor]` + `[PolyEnumStruct]` | theo luật `structured-errors`, kèm `ToFixedString()` |
| Buffer sự kiện combat | `FasterList<CombatEvent>`, cấp sẵn 64 | cùng kiểu vòng đời với `WeaponController._events` |
| Buffer hit tạm | **`FasterListPool<CombatantId>`** | mượn/trả trong một đòn đánh → không cấp phát |
| Các `Combatant` | `ArrayMap<CombatantId, Combatant>` | tra O(1) theo id; duyệt để tick |

**Không có gì đi vào job**, nên không dùng họ `Shared*` và không dùng `Unity.Collections`
ngoài phần `StatStore` vốn đã yêu cầu (`Allocator.Persistent`).

### Trường dữ liệu cần thêm

Chỉ **một**, và không chắc chắn — xem DEC-009:

| Bảng | Trường | Vì sao |
|---|---|---|
| `equipment` | *(không thêm)* | `ArmorRating` đã có; nhưng hôm nay chỉ được đọc qua `ItemData.Modifiers`. Cần chốt xem giáp đi qua modifier hay đọc thẳng. |

Mọi trường khác Combat cần **đã tồn tại**.

---

## 9. Kế hoạch test

Assembly `Game.Gameplay.Tests`, EditMode, không cần scene. Khoảng 30 test mới.

| File | Kiểm cái gì |
|---|---|
| `Combat/DamageResolverTests.cs` | giáp 0 → nguyên sát thương · giáp = K → đúng một nửa · giáp cực lớn → chạm `MIN_DAMAGE`, không âm · xuyên giáp 100% bỏ qua giáp hoàn toàn · hệ số đạn nhân đúng |
| `Combat/HealthTests.cs` | trừ máu đúng lượng · máu chạm 0 → `JustDied` true · đòn tiếp theo → `JustDied` **false** · sát thương thừa không làm máu âm |
| `Combat/HitQueryTests.cs` | địch trong cung → trúng · sau lưng → trượt · đúng biên `ArcDegrees` · ngoài `Range` → trượt · cùng phe → bỏ qua · đã chết → bỏ qua · nhiều mục tiêu → tất cả vào buffer |
| `Combat/CombatantTests.cs` | `Equip` cộng chỉ số **và** gắn máy vũ khí · `Unequip` trả chỉ số về đúng gốc · **máy vũ khí hỏng → loadout roll back sạch** · trang bị đè lên slot đang có |
| `Combat/CombatWorldTests.cs` | bắn → địch mất máu · ngoài tầm → không · `Died` phát đúng một lần · hai người bắn nhau cùng tick → **cả hai** trúng · buffer sự kiện xoá đúng đầu tick |
| `Combat/CombatErrorTests.cs` | mọi case có message · `default` an toàn · `ToFixedString` khớp `ToString` |

Test **quan trọng nhất** là `Equip cộng chỉ số và gắn máy vũ khí` — nó chính là cái mà
hôm nay không có, và là lý do seam orphan tồn tại mà 48 test vẫn xanh.

---

## 10. Các bước thực hiện

Theo thứ tự phụ thuộc. Mỗi bước compile được và test xanh trước khi sang bước sau.

| # | Việc | Xong khi | File đụng tới |
|---|---|---|---|
| 1 | Thêm `Unity.Mathematics` vào asmdef | compile sạch | `Game.Gameplay.asmdef` |
| 2 | `CombatantId` (`[WrapRecord]` over `int`) | dùng làm key `ArrayMap` được | `Combat/CombatantId.cs` |
| 3 | `CombatEventKind` + `CombatEvent` + `DamageInfo` | compile | `Combat/Common/CombatEvent.cs`, `DamageInfo.cs` |
| 4 | `CombatError` PolyEnum, ~6 case, có `ToFixedString()` | `CombatErrorTests` xanh | `Combat/Common/CombatError.cs` |
| 5 | `Health` + `HealthChange` | `HealthTests` xanh | `Combat/Health.cs` |
| 6 | `DamageResolver` static thuần | `DamageResolverTests` xanh | `Combat/DamageResolver.cs` |
| 7 | `CombatantSpatial` + `CombatantRegistry` (add/remove swap-back, `SetTransform`, `MarkDead`) | thêm/xoá 100 lượt không lệch chỉ số | `Combat/CombatantSpatial.cs`, `CombatantRegistry.cs` |
| 8 | `HitQuery.Melee` + `HitQuery.Ranged`, ghi vào buffer caller | `HitQueryTests` xanh | `Combat/HitQuery.cs` |
| 9 | **`Combatant`** — gộp Loadout + Binding + Weapons + Health, `Equip`/`Unequip` có roll back | `CombatantTests` xanh, **seam orphan đóng** | `Combat/Combatant.cs` |
| 10 | `CombatWorld` — sở hữu `StatStore`, tạo/huỷ combatant, `Tick` theo §6.2 | `CombatWorldTests` xanh | `Combat/CombatWorld.cs` |
| 11 | Xoá `MeleeWeaponContext.AttackRequested` và `RangedWeaponContext.HasWeapon` | compile sạch, 48 test cũ vẫn xanh | 2 file `Weapons/*Context.cs` |
| 12 | Bỏ tham số `slot` không dùng của `EquipmentRules.CanSetRounds` | compile sạch | `Equipment/EquipmentRules.cs` |
| 13 | Chạy toàn bộ test, cập nhật `README.md` + bảng trạng thái doc này | 48 + ~30 test xanh | `Documentation~/README.md`, file này |

Bước 9 là bước thật sự quan trọng; 1–8 là dọn đường cho nó.

---

## 11. Quyết định

| Id | Quyết định | Đã loại | Trạng thái |
|---|---|---|---|
| **DEC-001** | **`Combatant` là chủ sở hữu per-nhân-vật**, sở hữu Loadout + Binding + Weapons + Health; `Equip` là một giao dịch có roll back | *Cho `Loadout` gọi thẳng `EquipmentStatBinding`* — hỏng vì `Loadout` sẽ phải nhận `ref Accessor, ref WorldData` và như thế nó buộc phải biết stats tồn tại, đúng thứ mà phân rã phase A cố ý cấm. *Để nguyên như hiện tại* — thì trang bị đồ vĩnh viễn không cộng chỉ số. | **chốt** |
| **DEC-002** | **Máu hiện tại KHÔNG nằm trong stat store.** Máu tối đa là stat; máu hiện tại là state runtime riêng | *Thêm `[StatData] CurrentHealth` vào `CharacterStats`* — sai bản chất: stat là giá trị **suy ra từ modifier**, mỗi lần trúng đòn sẽ phải ghi base value và đá nhau với hệ modifier. Trang bị áo giáp lẽ ra tăng máu tối đa, chứ không hồi máu. | **chốt** |
| **DEC-003** | **Đạn trúng ngay (hitscan)** ở phase C, kể cả khi `ProjectileSpeed > 0` | *Mô phỏng đạn bay qua nhiều frame* — cần vòng đời projectile, pooling, tick riêng. Không cần cho mốc hiện tại. Dữ liệu `ProjectileSpeed`/`ProjectileRadius` **giữ nguyên**, phase sau đọc: `ProjectileSpeed <= 0` → hitscan, ngược lại → đạn bay. Quyết định này **không** khoá đường. | **chốt** |
| **DEC-004** | **Tier 1 — managed, main thread** | *Job + Burst* — công việc thưa, chi phí schedule > công việc, DOTS không cài, mất test EditMode. Xem §7. | **chốt** |
| **DEC-005** | Trong tick dùng **buffer**; ra ngoài dùng **PubSub**, tắt mặc định ở phase C | *PubSub cho tất cả* — thứ tự sát thương sẽ phụ thuộc thứ tự đăng ký. *Buffer cho tất cả* — Combat phải biết trước ai nghe. | **mở** — chốt khi phase D/HUD xuất hiện người nghe thật |
| **DEC-006** | **Hình học thuần, không dùng physics collider / `OnTriggerEnter`** | *Physics Unity* — đẩy logic combat vào MonoBehaviour, thứ tự không tất định, và **không test EditMode được**. 48 test hiện có đều EditMode không scene; giữ Combat thuần là giữ được điều đó. Đánh đổi: không tự có che chắn/xuyên tường — chấp nhận, chưa có level geometry. | **chốt** |
| **DEC-007** | Giáp dạng **`armor / (armor + K)`**, `K = 100`, sàn `MIN_DAMAGE = 1` | *`raw - armor`* — ra số âm, và có ngưỡng khiến giáp cao thành bất tử. *Nhân thẳng phần trăm* — không có điểm giảm dần, cân bằng khó. Xem §6.3. | **chốt** |
| **DEC-008** | **`CombatantSpatial` tách khỏi `Combatant`**, giữ trong mảng dense | *Để vị trí trong `Combatant`* — vòng quét sẽ deref class mỗi phần tử. Chi phí của việc tách: một mảng phải giữ đồng bộ, và `CombatWorld` là chủ ghi **duy nhất** nên không lệch được. | **chốt** |
| **DEC-009** | Giáp từ trang bị đi qua **`ItemData.Modifiers`**, `Equipment.ArmorRating` chỉ để hiển thị | *Đọc thẳng `ArmorRating` và cộng tay* — sẽ thành **đường thứ hai** sửa cùng một chỉ số, đúng kiểu bug ở §5. | **chốt** — modifiers là đường duy nhất. Cân nhắc xoá `ArmorRating` ở phase E |
| **DEC-010** | **Tách `AttackResolver` khỏi `CombatWorld`** | `CombatWorld` ban đầu 389 dòng làm 8 việc — vi phạm SRP thật. Tách theo trục thay đổi: `CombatWorld` giữ vòng đời và sở hữu (250 dòng), `AttackResolver` giữ số học một tick (205 dòng). *Để nguyên* — thêm đạn bay sẽ phải sửa đúng cái file đang sở hữu `StatStore`. | **chốt** — thêm lúc triển khai |
| **DEC-011** | Chuỗi `if` theo loại vũ khí trong `AttackResolver.FindHits` **chưa trừu tượng hoá** | *Tách interface hit-profile ngay* — hai nhánh chưa trả nổi chi phí gián tiếp. **Điểm cần theo dõi: loại vũ khí thứ ba xuất hiện (đạn bay, DEC-003) thì tách ngay.** | **chốt, có điều kiện** |

---

## 12. Phát sinh lúc triển khai — không có trong plan

| # | Việc | Vì sao quan trọng |
|---|---|---|
| 1 | `Equip` phải kiểm **món đồ có phải vũ khí không** trước khi gọi `OnEquipped` | Không kiểm thì đội cái mũ sẽ nhận `NotAWeapon` và **kích hoạt rollback oan**. Khoá lại bằng test `EquippingArmour_DoesNotFailLookingForAWeaponMachine` |
| 2 | Fixture test có `ProjectileRange = 0` | Các trường projectile là tham số tuỳ chọn mặc định 0, nên **mọi phát bắn đều trượt**. Phải thêm tầm bắn vào `TestItems` |
| 3 | `WeaponAttachFailed` mang `Slot` nhưng **không in ra** | Test bắt được. Slot là thứ cần nhất để chẩn đoán; payload có mà thông điệp thiếu thì vô dụng |
| 4 | `HitQuery.Ranged` trả **mục tiêu gần nhất**, không phải mọi mục tiêu | Đạn dừng ở thân người đầu tiên. Đánh gần thì ngược lại — quét hết mọi người trong cung |
| 5 | Chỉ số vũ khí đọc từ bảng đã có, **không thêm trường nào** | `Range`/`ArcDegrees`/`ProjectileRange`/`ArmorPenetration` đều có sẵn từ phase A/B |

---

## 13. Câu hỏi cho người review

1. ~~DEC-009~~ — đã chốt: qua `Modifiers`. Chưa có phản hồi ngược lại nên giữ nguyên.
2. **Ước lượng ~20–60 combatant** ở §7 có đúng với hình dung không? Nếu thật ra là vài
   trăm thì DEC-004 vẫn đứng, nhưng cần thêm spatial hash sớm hơn.
3. **Bắn nhầm đồng đội** — hiện lọc thẳng theo `Team`. Có cần bắn được đồng đội không?
