# Gameplay — Lộ trình

*[Index](README.md) · [Combat](Combat - Overview.md)*

## Trạng thái

| | |
|---|---|
| Mốc đang nhắm | **Vòng chơi tối giản chạy được** |
| Đã xong | Phase A (Equipment), B (Weapon logic), **C (Combat)** — 105 test EditMode xanh |
| Kế tiếp | Phase D1 — Character runtime (chưa lên plan) |
| Cập nhật | 2026-08-16 |

---

## 1. Mốc: "vòng chơi tối giản chạy được"

Định nghĩa cụ thể, để biết lúc nào thì đạt:

> Cầm tay chơi thử được — nhân vật di chuyển, trang bị súng, bắn, đạn trúng địch,
> địch mất máu và chết. Không cần UI đẹp, không cần nội dung nhiều, không cần save.

Mốc này quan trọng vì nó là **lần đầu thiết kế bị thực tế kiểm tra**. Trước đó mọi
quyết định đều dựa trên suy luận; sau đó mọi quyết định đều có căn cứ. Vì vậy mọi thứ
không nằm trên đường tới mốc này đều bị đẩy ra sau — kể cả những thứ đã có seam bỏ ngỏ.

---

## 2. Ta đang đứng ở đâu

```
Game.Common      ids, enum, CharacterStats (MaxHealth/Armor/MoveSpeed/CarryCapacity)
Game.Data        item + equipment + melee + ranged + ammo   ← có trường, chưa có nội dung
Game.Gameplay    ItemCatalog · EquipmentRules · Loadout · EquipmentStatBinding
                 WeaponController · MeleeAttackMachine · RangedFireMachine
                 CombatWorld · Combatant · AttackResolver · CombatantRegistry
                 HitQuery · DamageResolver · Health
```

Ba khoảng trống đã biết — hai cái đầu phase C đã đóng:

| # | Khoảng trống | Hệ quả | Vá ở phase nào |
|---|---|---|---|
| 1 | ~~`EquipmentStatBinding` không ai gọi~~ | ~~Trang bị đồ không cộng chỉ số~~ | ✅ **C đã vá** — qua `Combatant.Equip` |
| 2 | ~~`WeaponController.Events` không ai rút~~ | ~~Sự kiện rơi vào hư không~~ | ✅ **C đã vá** — `AttackResolver` rút |
| 3 | `Unequip` trả `EquippedItem` mang theo đạn, **không ai hứng** | Tháo đồ ra là mất | **G** |

Khoảng trống 3 chưa đóng, và cố ý — tháo đồ chưa phải việc người chơi làm trong vòng chơi
tối giản. `Combatant.Unequip` vẫn trả `EquippedItem` mang theo số đạn, chờ inventory ở phase G.

~~Ngoài ra: `AttackRequested` và `HasWeapon` không chỗ nào dùng.~~ ✅ đã xoá ở phase C, cùng
tham số `slot` thừa của `EquipmentRules.CanSetRounds`.

---

## 3. Các phase

| Phase | Tên | Vì sao ở đúng chỗ này | Trạng thái |
|---|---|---|---|
| **C** | **Combat** — dò trúng, sát thương, máu, chết | Mắt xích biến vũ khí từ số liệu thành thứ chơi được. Thuần C#, test EditMode toàn bộ. | ✅ **đã triển khai** |
| **D1** | **Character runtime thô** — MonoBehaviour, di chuyển, ngắm, input, **chưa anim** | Chỗ Unity lần đầu bước vào. Dùng prefab KayKit đứng yên là đủ; mục tiêu là xác nhận vị trí đồng bộ đúng vào `CombatantRegistry` và input gọi tới `WeaponController`. | chưa lên plan |
| **D2** | **Animation** — Animator controller + `CrossFade`, model vũ khí gắn tay | Tách khỏi D1 vì gắn anim vào thứ chưa chạy đúng là debug hai lớp cùng lúc. KayKit là art **tạm**, nên D2 làm mỏng nhất có thể. Xem §3.1. | chưa lên plan |
| **E** | **Content** — nối BakingSheet, nhập vũ khí thật | Hiện **không có nội dung nào**; test tự dựng dữ liệu. Không phụ thuộc C/D nên **chạy song song được** với D. | chưa lên plan |
| **F** | **Enemy AI** — đuổi và đánh, dùng HFSM | Cần C (đánh nhau) + D1 (di chuyển) đã chạy. HFSM đã sẵn, không phải viết mới. | chưa lên plan |
| | | **→ đạt mốc: vòng chơi tối giản** | |
| **G** | **Inventory** — túi đồ, nhặt/bỏ | Hứng đúng cái `Unequip` đang trả ra. Sau mốc, vì kiểu túi (lưới như Duckov hay danh sách phẳng) là quyết định lớn nên ra quyết định **sau khi** đã cầm tay chơi thử. | chưa lên plan |
| **H** | **Save** — `EncosyTower.Persistences` | Chỉ có ý nghĩa khi đã có thứ đáng lưu. | chưa lên plan |

### Vì sao C trước D

Cám dỗ là làm D trước — thấy nhân vật chạy trên màn hình sướng hơn nhiều. Nhưng:

- C thuần C#, test EditMode được 100%. D thì không.
- D ghi vị trí **vào** cấu trúc do C định nghĩa. Làm D trước thì phải bịa ra cấu trúc đó
  rồi sửa lại.
- Sai lầm ở C (công thức sát thương, ai sở hữu máu) rẻ khi sửa bằng test, đắt khi phải
  vào Play Mode mới thấy.

### 3.1 Luật Animator — chốt từ bây giờ, không đợi tới D2

HFSM **đã là** máy trạng thái. Animator cũng là máy trạng thái. Để cả hai cùng quyết định
thì thành hai nguồn sự thật đá nhau — đây là chỗ dự án hay chết nhất.

**HFSM quyết định, Animator chỉ nghe.**

- Animator controller **không có transition logic riêng** — chỉ là các state rời, code gọi
  `CrossFade` khi HFSM đổi state.
- `WindUpSeconds` / `ActiveSeconds` / `ReloadSeconds` trong bảng dữ liệu quyết định thời
  gian; clip co giãn cho vừa bằng `speed`. **Không** để độ dài clip quyết định nhịp gameplay.
- **Tuyệt đối không dùng Animation Event** để gọi `ConsumeRound()` hay gây sát thương.
  Animation Event không chạy trong EditMode — làm vậy là mất sạch 105 test hiện có. Toàn bộ
  kiến trúc thuần C# từ đầu tới giờ tồn tại để tránh đúng chuyện này.

Clip KayKit đã khớp gần một-một với state đã có: `Ranged_1H_Aiming/Shoot/Reload`,
`Melee_1H_Slash`, `Death_A`, `Hit_A`, `Walking_A`, `Running_A`. Thiếu duy nhất **Animator
controller** — hiện project có 0 cái.

### Vì sao E chạy song song được

E chỉ đụng `Game.Data` và đường ống import. Nó không thêm kiểu nào mà C hay D đọc —
các bảng **đã có đủ trường**. E chỉ đổ nội dung vào chỗ đã có.

---

## 4. Thứ đã cân nhắc và loại

| Loại | Vì sao |
|---|---|
| Làm Inventory trước Combat | Đóng được seam `Unequip` nhưng không đưa ta gần mốc hơn chút nào. Và kiểu túi nên quyết sau khi chơi thử. |
| Làm UI/HUD sớm | Trước khi có gì để hiển thị thì HUD chỉ là suy đoán. Máu và đạn đã có sự kiện từ phase C, HUD nối vào lúc nào cũng được. |
| Netcode / multiplayer | Chưa ai yêu cầu. Nhắc ở đây chỉ để nói rõ là **đang giả định single-player**; nếu sai thì `Combatant` và `CombatWorld` ở phase C là chỗ phải xem lại đầu tiên. |
| Nhảy thẳng vào DOTS/ECS | DOTS không cài trong project. Xem `Combat - Overview` §7 để biết vì sao quy mô này không cần. |

---

## 5. Rủi ro đang theo dõi

| Rủi ro | Dấu hiệu sẽ thấy | Phản ứng |
|---|---|---|
| Số lượng combatant vượt xa dự tính (~60) | Quét cung đánh gần thành điểm nóng trong profiler | Thêm spatial hash vào `CombatantRegistry` — đã tách sẵn nên đổi một chỗ |
| Loại vũ khí thứ ba (đạn bay) xuất hiện | Chuỗi `if` trong `AttackResolver.FindHits` dài ra | Tách hit-profile ngay lúc đó — xem `Combat - Overview` DEC-011 |
| Nhân vật người chơi và địch hoá ra cần cấu trúc khác nhau | `Combatant` mọc thêm cờ `isPlayer` | Tách khi cờ thứ hai xuất hiện, không tách trước |
| Đường ống content chặn phase F | Không có vũ khí thật để AI dùng | Phase F tạm dùng cùng bộ dữ liệu test của phase C |
