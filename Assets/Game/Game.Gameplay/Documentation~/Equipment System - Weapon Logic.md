# Equipment System — Weapon Logic (Phase B)

*[Tổng quan](Equipment System - Overview.md) · [Index](README.md)*

## Trạng thái

| | |
|---|---|
| Phase | **B — ĐÃ TRIỂN KHAI** (2026-08-15). Toàn bộ 48/48 test A+B xanh |
| Assembly sở hữu | `Game.Gameplay` |
| Namespace | `Game.Gameplay.Weapons` |
| Module first-party | **`ApexionGame.Core` HFSM** (`ApexionGame.HFSM`) — phase 1–7 done, tests green |
| Module EncosyTower | Common (`Result`/`Option`), Collections, PolyEnumStructs, EnumExtensions |
| Bậc hiệu năng | **Tier 1**, có giới hạn cứng từ thiết kế của HFSM — xem §6 |
| Quyết định mở | không |

---

## 1. Phạm vi

**Có:** vòng đời đòn đánh theo thời gian, chế độ bắn (đơn / loạt / liên thanh), tốc độ bắn, tốn đạn,
nạp đạn, huỷ đòn đánh khi tháo vũ khí.

**Không:** dò trúng, sát thương, vật thể bay, hitbox, mục tiêu. Ba thứ đầu cần physics; hai thứ sau
cần một mô hình mục tiêu. Đó là **hệ combat**, phase riêng.

Ranh giới nằm ở đúng một chỗ: **máy trạng thái phát ra "đòn đánh đã kích hoạt lúc T với tham số X",
rồi dừng.** Ai nghe sự kiện đó và làm gì với nó là chuyện của phase sau.

Hệ quả có lợi: **toàn bộ Phase B test được 100% trong EditMode**, không scene, không physics, không
frame thật.

## 2. Vì sao dùng HFSM chứ không tự viết timer

Luật thường trực: kiểm tra thư viện trước khi tự viết. `ApexionGame.Core/HFSM` khớp đúng bài toán:

| Nhu cầu của vòng đời đòn đánh | HFSM có sẵn |
|---|---|
| Trạng thái nối tiếp theo thời gian | `TransitionCause.Timer`, min-duration (phase 3 ✅) |
| Nhiều vũ khí cùng loại, khác trạng thái | Flyweight behaviour + data slot per-instance |
| Tick hàng loạt | `MachineRunner` (phase 6 ✅) |
| Huỷ giữa chừng khi tháo vũ khí | `Reset()` — xoá history, timer, data |
| Xem trạng thái lúc debug | registry + overlay chạy được cả trong build (phase 7 ✅) |
| Lỗi lúc dựng máy | `MachineError` — PolyEnum, có payload |

Tự viết sẽ là một `switch` trên enum cộng vài biến `float` đếm ngược. Nó chạy được, và nó sẽ mọc dần
thành: huỷ giữa chừng, hàng đợi trigger, ưu tiên, chuyển tiếp any-state, ghi log để debug — tức là
mọc thành một HFSM tệ hơn.

**Cái giá phải chấp nhận:** một `MachineDefinition` được bake cho mỗi *loại* vũ khí, và một instance
cho mỗi vũ khí đang đeo. Với người chơi là 3 máy. Với AI là vài trăm — vẫn nằm trong đúng khoảng
HFSM được thiết kế cho (§6).

## 3. Hai hình dạng máy, không phải một

Cận chiến và tầm xa **không cùng hình dạng**. Ép chung một máy sẽ tạo ra các trạng thái vô nghĩa cho
một nửa số vũ khí — đúng kiểu "cột rỗng ở phần lớn dòng" mà Phase A đã tránh ở tầng dữ liệu.

### 3.1 `MeleeAttackMachine`

```
                    ┌──────────────────────────────────┐
                    ▼                                  │
   ┌──────┐  Attack  ┌────────┐ timer  ┌────────┐ timer ┌──────────┐
   │ Idle │─────────▶│ WindUp │───────▶│ Active │──────▶│ Recovery │
   └──────┘  (guard) └────────┘        └────────┘       └──────────┘
      ▲                   │                 │                 │
      └───────────────────┴─────────────────┴─────────────────┘
                        Cancel  (any-state → Idle)
```

| Trạng thái | Thời lượng | Ý nghĩa |
|---|---|---|
| `Idle` | — | Sẵn sàng nhận `Attack` |
| `WindUp` | `windUpSeconds` | Vung lên. Chưa gây gì cả |
| `Active` | `activeSeconds` | **Cửa sổ đòn đánh có hiệu lực** — phát `AttackActivated` khi vào |
| `Recovery` | `recoverySeconds` | Không đánh tiếp được |

### 3.2 `RangedFireMachine`

```
   ┌──────┐  Attack   ┌────────┐  timer(1/RPM)  ┌─────────┐
   │ Idle │──────────▶│ Firing │───────────────▶│ Cycling │
   └──────┘  (guard:  └────────┘                └─────────┘
      │      rounds>0)     │                          │
      │                    │ hết đạn                  │ còn giữ nút + auto/burst
      │                    ▼                          └──────────┐
      │               ┌───────┐                                  │
      │               │ Empty │◀─────────────────────────────────┘
      │               └───────┘
      │  Reload           │  Reload
      ▼  (guard:          ▼
   ┌───────────┐  rounds<capacity
   │ Reloading │◀─────────┘
   └───────────┘
         │ timer(reloadSeconds)
         └──────────────────────▶ Idle
```

| Trạng thái | Thời lượng | Ý nghĩa |
|---|---|---|
| `Idle` | — | Sẵn sàng, còn đạn |
| `Firing` | tức thì | **Một phát nổ ra** — phát `ShotFired`, yêu cầu trừ 1 viên |
| `Cycling` | `60 / roundsPerMinute` | Khoảng cách giữa hai phát. Auto/burst quay lại `Firing` từ đây |
| `Empty` | — | Hết đạn. `Attack` không có tác dụng |
| `Reloading` | `reloadSeconds` | Nạp. Huỷ được bằng `Cancel` |

**Chế độ bắn** quyết định chuyện gì xảy ra ở cuối `Cycling`:

| `WeaponFireMode` | Hành vi |
|---|---|
| `Single` | → `Idle`. Muốn bắn tiếp phải `Attack` lần nữa |
| `Burst` | → `Firing` cho tới khi đủ `burstCount`, rồi → `Idle` |
| `Auto` | → `Firing` chừng nào `AttackHeld` còn true |

## 4. Sở hữu state

Đây là chỗ Phase B dễ sai nhất, vì số đạn **đã có chủ từ Phase A**.

| State | Chủ | Ai đọc | Ai được ghi | Vòng đời |
|---|---|---|---|---|
| trạng thái đòn đánh hiện tại | **instance HFSM** (một máy mỗi vũ khí đang đeo) | debug overlay | chỉ máy | theo lần trang bị |
| thời gian còn lại trong trạng thái | **instance HFSM** | — | chỉ máy | như trên |
| **số đạn trong băng** | **`Loadout`** — từ Phase A, **không phải máy** | máy đọc để làm guard | chỉ `Loadout`, qua `SetRounds` | theo món đồ |
| số phát đã bắn trong loạt | **instance HFSM** | — | chỉ máy | theo loạt bắn |
| nút tấn công đang giữ | **người gọi** (input) | máy đọc | người gọi | theo frame |
| thời gian | **được truyền vào**, không đọc từ engine | máy | người gọi | — |

### Số đạn không được có chủ thứ hai

Cám dỗ rõ ràng: cho máy giữ luôn `_rounds` vì nó là thứ trừ đạn. **Làm vậy là tạo chủ thứ hai** —
`Loadout` cũng giữ số đạn, và hai bên sẽ lệch. Lúc tháo vũ khí, `Loadout` trả ra con số của nó, còn
con số thật nằm trong máy vừa bị vứt đi.

Nên: **máy đọc số đạn từ `Loadout` để làm guard, và gọi `Loadout.SetRounds` để trừ.** Máy không bao
giờ giữ bản sao. Đây đúng là luật "muốn đổi thứ mình không sở hữu thì hỏi chủ của nó".

Cái giá: một lần gọi qua lại mỗi phát bắn. Không đáng kể — bắn là hành động rời rạc, không phải vòng
lặp nóng.

### Thời gian phải được truyền vào

`Tick(float deltaSeconds)`. Máy **không bao giờ** đọc đồng hồ của engine.

Không phải để cho đẹp: bằng chứng của feature này là test EditMode, và một máy trạng thái đọc
`Time.deltaTime` thì không test được nếu không chạy frame thật. Truyền thời gian vào ⇒ test tua được
"1.5 giây" trong một dòng, và cùng đầu vào luôn cho cùng đầu ra.

## 5. Giao tiếp

| Từ | Tới | Cơ chế | Vì sao |
|---|---|---|---|
| người gọi (input) | `WeaponController` | **gọi trực tiếp** — `RequestAttack()`, `RequestReload()`, `Cancel()` | Cần biết kết quả; được phép biết |
| `WeaponController` | máy HFSM | **gọi trực tiếp** — trigger + `Tick` | API thư viện |
| máy | `Loadout` | **gọi trực tiếp** — đọc số đạn, `SetRounds` | Hỏi chủ của state; §4 |
| máy | phase combat sau này | **`FasterList<WeaponEvent>` người gọi tự đọc** | Chưa có ai nghe — xem dưới |

### Vì sao là danh sách sự kiện, không phải event/message

Chưa có subscriber nào tồn tại. Dựng một kênh PubSub bây giờ là **suy đoán**, và event trả giá bằng
việc không lần được luồng điều khiển.

Cách nhỏ nhất mà vẫn đúng: máy ghi sự kiện vào một danh sách người gọi cung cấp, người gọi đọc và
xoá sau mỗi tick. Không cấp phát mỗi frame, lần được luồng, và test chỉ việc đọc danh sách.

Khi hệ combat xuất hiện và **thật sự** có nhiều bên nghe, chuyển sang PubSub là thay đổi cục bộ ở
`WeaponController` — vì lúc đó đã có bằng chứng.

```csharp
public readonly record struct WeaponEvent(
      WeaponEventKind Kind        // AttackActivated | ShotFired | ReloadStarted | ReloadFinished | WentEmpty
    , EquipmentSlot Slot
    , ItemId Weapon
    , float AtTime                // thời gian tích luỹ của máy, không phải giờ hệ thống
);
```

`AttackActivated` và `ShotFired` mang theo tham số mà hệ combat sau này cần — sát thương gốc, cung
quét/độ giật, thông số projectile — đọc từ catalog lúc phát sự kiện. **Dữ liệu đã có sẵn từ Phase A**,
đó là lý do Phase A mô hình đủ cột dù chưa dùng.

## 6. Bậc hiệu năng

**Chọn: Tier 1.** Và ở đây có một **giới hạn cứng không phải do ta chọn**.

`ApexionGame.Core/HFSM` tự quyết định là **managed only, không có tầng Burst/job** — đó là DEC của
chính thư viện, ghi trong `HFSM - Decisions.md`. Nên tier 4–5 **không khả dụng** cho phần này, bất kể
đo đạc ra sao. Muốn vượt qua đó thì phải không dùng HFSM nữa, và đó là một quyết định khác hẳn.

**Vì sao Tier 1 là đủ:**

- Máy tick mỗi frame ⇒ đây **là** công việc per-frame, khác Phase A.
- Nhưng N nhỏ: người chơi có ≤3 vũ khí đang đeo. AI vài trăm — đúng khoảng HFSM được thiết kế cho
  ("a few hundred AI agents ticking every frame", theo `HFSM - Overview.md`).
- Flyweight: một `MachineDefinition` bake cho mỗi **loại** vũ khí, không phải mỗi khẩu. Đây là tier 1
  áp ở tầng kiến trúc — dùng đúng hình dạng thư viện đã cho.
- `MachineRunner` tick hàng loạt thay vì mỗi máy tự `Update`.

**Vì sao không thấp hơn.** Nếu mỗi khẩu súng tự bake định nghĩa riêng thì chi phí dựng máy nhân theo
số vũ khí đang đeo, và với AI thì thấy rõ. Dùng flyweight gần như miễn phí và **đắt để sửa sau** vì
nó đổi cách dựng máy ở mọi call site.

**Xem lại khi:** số agent có máy vũ khí vượt vài trăm, hoặc profiling cho thấy tick máy vượt phần
ngân sách của nó. Lúc đó câu hỏi không phải "tối ưu HFSM" mà là "vũ khí AI có cần máy trạng thái đầy
đủ không, hay chỉ cần một bộ đếm".

**Ghi chú:** `performance.device_tiers` vẫn là `unknown — ask`.

## 7. Phương án đã loại

| Phương án | Vì sao loại |
|---|---|
| **Tự viết `switch` + biến đếm** | Chạy được lúc đầu, rồi mọc thành một HFSM tệ hơn khi thêm huỷ/hàng đợi/ưu tiên/debug |
| **Một máy chung cho cận chiến và tầm xa** | Nửa số trạng thái vô nghĩa với mỗi loại — đúng lỗi "cột rỗng" ở tầng logic |
| **Máy giữ luôn số đạn** | Chủ thứ hai cho một sự thật; lệch với `Loadout` lúc tháo vũ khí (§4) |
| **Đọc `Time.deltaTime` trong máy** | Không test được nếu không chạy frame thật — phá đúng bằng chứng của feature |
| **PubSub cho sự kiện vũ khí** | Chưa có subscriber ⇒ suy đoán, và mất khả năng lần luồng |
| **Coroutine cho vòng đời đòn đánh** | Không kiểm tra được trạng thái, không huỷ sạch, không debug được, không test được không cần scene |
| **Gộp Phase A và B làm một** | 4 assembly chưa từng compile + ~40 file; hỏng ở asmdef là gỡ ngược qua tất cả |

## 8. Cách biết là chạy

`unity test . --mode EditMode --filter "Game.Gameplay.Tests.Weapons"` — xanh, có số:

| # | Nhóm | Khẳng định |
|---|---|---|
| 1 | Cận chiến | `Attack` → `WindUp`; `AttackActivated` **chưa** phát |
| 2 | Cận chiến | Sau đúng `windUpSeconds` → `Active`, phát `AttackActivated` **một lần** |
| 3 | Cận chiến | Trong `Recovery`, `Attack` không có tác dụng |
| 4 | Cận chiến | Sau `recoverySeconds` → `Idle`, đánh lại được |
| 5 | Cận chiến | Tổng thời gian một đòn = windUp + active + recovery, sai số < 1 frame |
| 6 | Tầm xa | `Single`: một `Attack` → đúng **một** `ShotFired`, trừ **đúng 1** viên |
| 7 | Tầm xa | `Burst`: một `Attack` → đúng `burstCount` phát |
| 8 | Tầm xa | `Auto`: giữ nút 1 giây ở 600 RPM → **10** phát |
| 9 | Tầm xa | `Auto`: nhả nút giữa chừng → dừng ở `Idle`, không bắn thêm |
| 10 | Tầm xa | Khoảng cách giữa hai phát = `60 / roundsPerMinute` |
| 11 | Đạn | Bắn tới viên cuối → vào `Empty`, phát `WentEmpty` |
| 12 | Đạn | Ở `Empty`, `Attack` không phát `ShotFired` nào |
| 13 | Đạn | **Số đạn trong `Loadout` khớp số `ShotFired` đã phát** — không có chủ thứ hai |
| 14 | Nạp | `Reload` → `Reloading`, sau `reloadSeconds` về `Idle` với băng đầy |
| 15 | Nạp | `Reload` khi băng đã đầy → bị từ chối, không đổi trạng thái |
| 16 | Nạp | `Cancel` giữa lúc nạp → về `Idle`, **số đạn không đổi** |
| 17 | Tháo vũ khí | Tháo giữa `Active` → máy `Reset`, không phát thêm sự kiện |
| 18 | Tháo vũ khí | Trang bị lại → máy bắt đầu từ `Idle`, số đạn theo giá trị mang về từ Phase A |
| 19 | Tất định | Cùng chuỗi input + cùng delta → **cùng chuỗi sự kiện, byte-for-byte** |
| 20 | Tất định | Tick 1000 lần với delta cố định → không trôi thời gian tích luỹ |
| 21 | Lỗi | Dựng máy từ dữ liệu vũ khí sai (RPM = 0) → `MachineError` có payload, không ném exception |

**Test 13 và 19 quan trọng nhất.** Test 13 chặn chủ-thứ-hai của số đạn — kiểu hỏng mà §4 chỉ ra. Test
19 là điều kiện tiên quyết cho mọi thứ còn lại: máy không tất định thì không test nào ở trên đáng tin.

## 9. Các bước

**Chỉ bắt đầu sau khi Phase A xanh.**

| # | Hành động | Xong khi | File |
|---|---|---|---|
| B1 | Thêm `ApexionGame.Core` vào `Game.Gameplay.asmdef` | Compile; `HierarchicalStateMachine<,>` phân giải được | `Game.Gameplay.asmdef` |
| B2 | `WeaponEvent`, `WeaponEventKind`, `WeaponError` (PolyEnum, có `ToFixedString()`) | `default(WeaponError).ToString()` an toàn | `Game.Gameplay/Weapons/Common/` |
| B3 | `MeleeWeaponContext`, `RangedWeaponContext` — `class`, giữ tham chiếu `Loadout` + slot + catalog | Không có field `_rounds` nào trong context | `Game.Gameplay/Weapons/` |
| B4 | `MeleeAttackMachine` — 4 trạng thái, chuyển tiếp bằng timer, any-state `Cancel` | Test 1–5 xanh | `Game.Gameplay/Weapons/` |
| B5 | `RangedFireMachine` — 5 trạng thái, ba chế độ bắn, guard theo số đạn | Test 6–13 xanh | `Game.Gameplay/Weapons/` |
| B6 | Nạp đạn + huỷ | Test 14–16 xanh | `Game.Gameplay/Weapons/` |
| B7 | `WeaponController` — bake định nghĩa **theo loại vũ khí** (flyweight), tạo/huỷ instance theo equip/unequip, tick qua `MachineRunner` | Test 17–18 xanh; số `MachineDefinition` = số **loại** vũ khí, không phải số khẩu | `Game.Gameplay/Weapons/` |
| B8 | Test tất định + test lỗi | Test 19–21 xanh | `Game.Gameplay.Tests/Weapons/` |
| B9 | Chạy toàn bộ: Phase A + B | `unity test --mode EditMode` xanh, executed = passed, **executed > 0** | — |

Bước B1 chặn tất cả. B4 và B5 độc lập với nhau. B7 phụ thuộc B4–B6.

## 10. Quyết định

### DEC-B01 — Dùng HFSM, không tự viết timer · **chốt**

§2. Luật thường trực về kiểm tra thư viện trước, cộng với việc HFSM khớp đúng bài toán và đã done
phase 1–7 với tests green.

### DEC-B02 — Hai máy riêng cho cận chiến và tầm xa · **chốt**

§3. Ép chung một máy tạo trạng thái vô nghĩa cho nửa số vũ khí.

### DEC-B03 — Số đạn vẫn thuộc `Loadout`, máy không giữ bản sao · **chốt**

§4. Đây là quyết định dễ làm sai nhất trong Phase B, và test 13 tồn tại để chặn nó.

### DEC-B04 — Thời gian được truyền vào, không đọc từ engine · **chốt**

§4. Không có điều này thì bằng chứng của feature (test EditMode) không tồn tại được.

### DEC-B05 — Sự kiện đi qua danh sách người gọi cung cấp, chưa dùng PubSub · **chốt**

§5. Chưa có subscriber ⇒ PubSub là suy đoán. Chuyển sang PubSub sau là thay đổi cục bộ.

### DEC-B06 — Tier 4–5 không khả dụng, do thiết kế của HFSM · **chốt**

§6. `ApexionGame.Core/HFSM` là managed-only theo quyết định của chính nó. Ghi ra đây để phiên sau
không đi tìm cách jobify nó rồi mới phát hiện.

### DEC-B07 — Định nghĩa máy bake theo **loại** vũ khí, không theo khẩu · **chốt**

§6. Đúng hình dạng flyweight mà HFSM được thiết kế cho. Làm sai chỗ này thì chi phí nhân theo số vũ
khí đang đeo, và đắt để sửa sau vì nó đổi mọi call site dựng máy.

## 11. Phát hiện khi triển khai

Ba thứ chỉ lộ ra khi viết code thật, không suy ra được từ tài liệu.

### `.After(seconds)` không dùng được cho thời lượng lấy từ dữ liệu

`.After()` bake giá trị vào **definition**. Windup của dao khác của kiếm, nên dùng nó sẽ cần một
definition cho **mỗi món vũ khí** — phá vỡ DEC-B07.

Thay bằng guard đọc `info.TimeInState`, thời lượng lấy từ context:

```csharp
.To(MeleeAttackState.Active).When(static (c, i) => i.TimeInState >= c.WindUpSeconds)
```

Một definition cho mỗi **loại**, đúng như DEC-B07 dự kiến. `DefinitionCount` là **2**, bất kể có bao
nhiêu khẩu — có test khoá điều này.

### `.On()` và `.When()` loại trừ nhau

Cả hai đều trả `MachineBuilder`, nên một chuyển tiếp là trigger **hoặc** guard, không thể cả hai.
"Attack **và** còn đạn" phải tách thành `Idle --Attack--> Firing`, rồi `Firing --rounds<1--> Empty`
với priority cao hơn. `Firing` không phát `ShotFired` khi hết đạn.

### Chuyển tiếp theo thời gian trễ một tick

Một chuyển tiếp có guard `TimeInState >= X` fire ở tick **sau** khi ngưỡng bị vượt, không phải ngay
tick đó. Xác nhận bằng thí nghiệm: `windUp + 1 bước` vẫn ở `WindUp`, `+ 2 bước` mới sang `Active`.

Hệ quả cho test: **đừng khẳng định đúng một mốc tick.** Kiểm hai chiều — không kích hoạt *trước* khi
hết thời lượng, và có kích hoạt *sau* đó với vài tick dư. Khẳng định mốc chính xác là over-specify và
sẽ vỡ vì lý do không liên quan đến hành vi.

### Ba lỗi logic mà compile không bắt được

Cả ba đều compile xanh: `BeginBurst()` không bao giờ được gọi (loạt thứ hai chỉ bắn một phát) ·
thiếu `Idle → Reloading` (không nạp giữa chừng được) · `Refill(0f)` ghi `AtTime` sai. Sửa bằng cách
cho context giữ `CurrentTime` do controller cập nhật, và `Idle` reset bộ đếm loạt khi vào.

Đây là bằng chứng cho luật *compile xanh không phải là verified*.
