# HFSM — Flows

*[English](HFSM%20-%20Flows.md) · [Index](README.vi.md)*

Mọi luật thứ tự trong file này là **hợp đồng**, bị khoá bằng một golden test khẳng định một chuỗi
ký tự chính xác. Chỗ nào luật là tuỳ chọn nhưng buộc phải chốt một cái, lý do nằm ở
[HFSM - Decisions](HFSM%20-%20Decisions.vi.md).

Ví dụ xuyên suốt:

```
Root
├── Idle                          leaf
├── Patrol                        leaf
├── Combat            composite, history = Shallow, initial = Chase
│   ├── Chase                     leaf
│   └── Attack                    leaf
└── Flee                          leaf
```

---

## 1. Một tick, từ trên xuống

```
Tick(dt)
 │
 ├─ 1. nếu Phase != Idle  →  AdvanceAsync(dt); return          (§8)
 │
 ├─ 2. timer:  TimeInMachine += dt
 │             với mỗi node active n:  TimeInNode[n] += dt
 │
 ├─ 3. rút hàng đợi trigger                                    (§5)
 │        với mỗi trigger đang xếp, giải một lần; dừng ở cái đầu tiên khớp
 │
 ├─ 4. nếu không trigger nào khớp  →  đánh giá polled guard     (§2, §3)
 │
 ├─ 5. nếu đã chọn được một transition  →  thực thi nó          (§4)
 │        (chuỗi async để Phase != Idle; tick kết thúc ở đây)
 │
 └─ 6. cập nhật cấu hình active, ngoài → trong                  (§3.3)
          với mỗi node active theo thứ tự vào:  OnUpdate(ctx, ref data, info, dt)
          trigger bắn ở đây rơi vào hàng đợi cho bước 3 của tick SAU
```

Hai hệ quả nên nói thẳng, vì cả hai đều dễ làm sai và cả hai đều có test:

- **Tối đa một transition mỗi tick mỗi region.** Một chuỗi transition không thể bắn hết trong một
  frame. Điều này làm frame có biên và làm log transition đọc được. Máy nào cần chuỗi tức thì thì
  dùng action `.Internal()` thay thế ([DEC-012](HFSM%20-%20Decisions.vi.md#dec-012)).
- **Transition được quyết định *trước khi* `OnUpdate` chạy.** State được tick là state mà máy đã
  chốt ở frame này — một state không bao giờ chạy `OnUpdate` trong chính frame nó thoát.

---

## 2. Thứ tự đánh giá

Với một region, transition được gom **từ leaf active đi ngược lên root**, và cái đầu tiên có guard
đúng sẽ thắng.

```
ưu tiên 1:  transition AnyState              — sắp theo Priority giảm, rồi thứ tự khai báo
ưu tiên 2:  transition của leaf active       — sắp theo Priority giảm, rồi thứ tự khai báo
ưu tiên 3:  transition của node cha
     …      …lên tới Root
```

| Luật | Vì sao như vậy |
|---|---|
| `AnyState` trước tiên | Nó tồn tại chính là để cắt ngang — "bị stun từ bất cứ đâu", "chết từ bất cứ đâu". Nếu thua transition của leaf thì nó vô dụng. |
| Node sâu nhất kế tiếp | State cụ thể nhất biết nhiều nhất về tình huống. `Attack → Chase` nên thắng `Combat → Idle`. |
| Trong một node: `Priority` giảm, rồi thứ tự khai báo | Thứ tự khai báo phần lớn là đủ; `Priority` là cửa thoát không bắt phải sắp xếp lại builder. |
| Khớp đầu tiên thắng, dừng đánh giá | Guard có thể không có tác dụng phụ nhưng không bắt buộc phải rẻ. Đánh giá tiếp sau khi đã có người thắng là phí thời gian và làm rối khung guard. |

Một transition **đủ điều kiện** chỉ khi đủ cả:

1. `MinDuration` của nó đã trôi qua ở state nguồn (mặc định 0);
2. trigger của nó khớp, nếu nó có trigger — không thì nó là polled;
3. guard trả `true`, nếu nó có guard; transition không trigger, không guard, không `.After()` là vô
   điều kiện và bắn ngay lần đánh giá đầu tiên.

### 2.1 Ví dụ thứ tự cụ thể

Cấu hình active `Root/Combat/Attack`, và frame này `c.Health` tụt xuống 15 trong khi
`c.DistanceToPlayer` là 4:

```
AnyState  → Flee    guard c.Health < 20f        → true    ✔ được chọn, dừng
Attack    → Chase   trigger AttackFinished      (không có trong hàng đợi)
Attack    → Chase   guard c.DistanceToPlayer>3f → sẽ true, nhưng không bao giờ được đánh giá
Combat    → Idle    guard !c.SeesPlayer         → không bao giờ được đánh giá
```

Khung guard hiện cả bốn dòng, hai dòng không được đánh giá thì làm mờ, nên lý do transition "hiển
nhiên" không xảy ra là thứ nhìn thấy được chứ không phải suy đoán.

---

## 3. Cấu hình active

### 3.1 "Active" nghĩa là gì

Cấu hình active của một máy là một **tập** node, không phải một node:

- vào một leaf sẽ kích hoạt leaf đó và mọi tổ tiên lên tới Root;
- vào một composite sẽ kích hoạt đúng một con của nó (initial, hoặc history);
- vào một node parallel sẽ kích hoạt **mọi** region của nó (§7).

Khi không có node parallel, tập active là một đường duy nhất từ root tới leaf.
`HierarchicalStateMachine.CurrentState` trả về leaf sâu nhất của region 0.

### 3.2 Thứ tự vào

Tổ tiên trước con cháu — **ngoài trước**. Vào `Combat/Chase` từ `Idle`:

```
OnExit  (Idle)
OnEnter (Combat)          ← cha vào trước con
OnEnter (Chase)
```

Viết vậy vì `OnEnter` của một composite thường dựng sẵn thứ mà các con dựa vào (một animation layer,
một cấu hình nav-mesh agent). Thứ tự ngược lại sẽ khiến con tự cấu hình rồi bị cha ghi đè.

### 3.3 Thứ tự update

Giống thứ tự vào — **ngoài trước**, tổ tiên rồi con cháu:

```
OnUpdate(Combat)
OnUpdate(Chase)
```

Một composite có tick thì phủ quyết hoặc điều chỉnh được việc các con nó làm ngay trong frame đó. Nó
cũng khớp thứ tự vào, tức là một mô hình tư duy thay vì hai.

### 3.4 Thứ tự ra

Đảo hoàn toàn — **trong trước**:

```
OnExit(Chase)
OnExit(Combat)
```

Con nhả thứ nó lấy trước khi cha nhả thứ mà con đang giữ.

---

## 4. Thực thi một transition

Cho một transition đã chọn từ nguồn `S` tới đích `T`:

```
1. lca ← LowestCommonAncestor(S, T)
2. exit  mọi node active từ con cháu active sâu nhất lên tới (nhưng không gồm) lca,
         trong trước, ghi history ở mọi composite có history                 (§6)
3. chạy  .Do(action) của transition, nếu có
4. enter mọi node trên đường từ lca (không gồm) xuống T, ngoài trước
5. đi xuống từ T tới một leaf:
        composite → child history nếu đã ghi, không thì child Initial
        parallel  → mọi region, mỗi region đi xuống theo cùng cách           (§7)
        leaf      → dừng
6. reset TimeInNode cho mọi node vừa vào; PreviousNode ← leaf cũ
7. ghi vào log transition kèm TransitionCause                            (Debugging §3)
```

`LowestCommonAncestor` đẩy node sâu hơn đi lên cho tới khi bằng độ sâu, rồi đẩy cả hai lên song
song. `Depth` được bake vào `StateNode` lúc build, nên đây là O(depth), không cấp phát, không tìm kiếm.

### 4.1 Self-transition

`.To(x)` khi `x` chính là nguồn thì **là external**: nó exit rồi enter lại, nên cả `OnExit` lẫn
`OnEnter` đều chạy. Đó thường chính là mục đích của self-transition (khởi động lại nhát chém).

`.Internal()` làm nó thành internal: không `OnExit`, không `OnEnter`, chỉ `.Do(action)`. Dùng cho
"phản ứng mà không khởi động lại".

### 4.2 Nhắm thẳng vào một composite

`.To(Combat)` là hợp lệ. Bước 5 đi xuống child history hoặc child `Initial` của `Combat`. Nhắm vào
một composite không có `Initial` và không có history là lỗi lúc build — `MachineError.NoInitialChild`.

---

## 5. Trigger

`Fire(trigger)` nối vào hàng đợi của từng instance; nó không bao giờ chuyển state đồng bộ. Hàng đợi
được rút ở bước 3 của tick kế tiếp — hoặc của *chính* tick đó, nếu `Fire` được gọi từ ngoài tick.

| Tình huống | Điều gì xảy ra |
|---|---|
| `Fire` từ code gameplay, ngoài `Tick` | xếp hàng; tiêu thụ ở bước 3 của tick kế |
| `Fire` từ trong `OnUpdate` (bước 6) | xếp hàng; tiêu thụ ở tick **sau**, vì bước 3 đã đi qua |
| `Fire` từ trong `OnEnter`/`OnExit` (lúc bước 2/4) | xếp hàng; tiêu thụ ở tick kế |
| Hai trigger trong hàng, cả hai khớp một transition | cái xếp trước mà khớp một transition thắng; phần còn lại vẫn nằm trong hàng |
| Một trigger trong hàng không khớp gì ở cấu hình hiện tại | **bị bỏ** lúc rút, và ghi vào log debug là `unmatched` |

Bỏ trigger không khớp là cố ý: giữ lại nghĩa là một `Hit` bắn lúc đã chết sẽ trồi lên vài phút sau,
khi máy tình cờ tới một state có xử lý nó ([DEC-007](HFSM%20-%20Decisions.vi.md#dec-007)). Log debug
làm việc bỏ đó nhìn thấy được, và đó mới là phần quan trọng.

`TryFire` trả `Result<Unit, MachineError>` và báo `MachineError.MachineNotRunning` hoặc
`MachineError.TriggerQueueFull` (hàng đợi chặn ở 16 mỗi instance). `Fire` là dạng bắn-rồi-quên, log
thay vì trả về.

---

## 6. History

`WithHistory(...)` trên một composite ghi lại nó đang ở đâu lúc rời đi, và khôi phục lúc vào lại.

| Chế độ | Ghi lúc exit | Khôi phục lúc entry |
|---|---|---|
| `None` (mặc định) | không gì | luôn dùng `Initial` |
| `Shallow` | child **trực tiếp** đang active của composite | child đó, rồi `Initial`/history của chính child đó ở tầng dưới |
| `Deep` | nguyên đường active dưới composite, xuống tới leaf | nguyên đường, chính xác |

Shallow và deep chỉ khác nhau khi child được nhớ lại chính nó là một composite:

```
Combat (deep)
└── Ranged (composite, initial = Aim)
    ├── Aim
    └── Fire        ← đang active lúc Combat bị thoát

Shallow → vào lại Combat/Ranged/Aim      (nhớ Ranged, rồi lấy Initial của Ranged)
Deep    → vào lại Combat/Ranged/Fire     (nhớ nguyên đường)
```

Các luật mà test khoá lại:

- History được ghi trong chuỗi exit (bước 2), **trước khi** `OnExit` của chính composite chạy, để
  `OnExit` vẫn đọc được child đang active.
- `Reset()` xoá mọi ô history. Vào lại sau `Reset` sẽ dùng `Initial`.
- Máy chưa từng vào một composite thì không có history cho nó, và dùng `Initial`.
- Transition nhắm thẳng vào một con cháu cụ thể (`.To(Attack)`) **đè lên** history — đích tường minh
  thắng. History chỉ áp dụng khi điểm vào là chính composite.

---

## 7. Parallel region

Một node `Parallel` sở hữu các con `Region`. Vào nó thì kích hoạt mọi region; mỗi region sau đó hành
xử như một composite độc lập.

```
Fighter (parallel)
├── Movement (region)      Idle | Walk | Dash
├── Weapon   (region)      Holstered | Drawn | Firing
└── Buff     (region)      None | Hasted
```

| Thao tác | Thứ tự |
|---|---|
| Vào node parallel | `OnEnter(Fighter)`, rồi region 0 trọn vẹn (gốc region rồi con cháu), rồi region 1, rồi region 2 — **thứ tự khai báo, theo chiều sâu** |
| Update | `OnUpdate(Fighter)`, rồi từng region theo thứ tự khai báo, mỗi region ngoài-trước |
| Đánh giá transition | region 0 trước, rồi region 1, … — mỗi region giải độc lập và có thể bắn transition riêng trong cùng một tick |
| Ra khỏi node parallel | region 2 trọn vẹn, region 1, region 0 — **đảo thứ tự khai báo** — rồi `OnExit(Fighter)` |

### 7.1 Những luật giữ cho nó còn hiểu được

- **Transition không được cắt ngang region.** Nguồn ở `Movement` và đích ở `Weapon` là
  `MachineError.TransitionCrossesParallelRegion` lúc build. Cắt ngang nghĩa là một region giật leaf của
  region khác ra khỏi tay nó, mà không có thứ tự nào được định nghĩa.
- **Transition rời khỏi node parallel sẽ thoát mọi region**, theo thứ tự khai báo đảo ngược, rồi tới
  chính node parallel. Đó là cách duy nhất region kết thúc.
- **"Một transition mỗi tick" là tính theo region.** Ba region có thể mỗi cái bắn một transition
  trong cùng một tick. Giới hạn tồn tại để chặn biên một frame, và ba region độc lập thì đúng bằng
  khối lượng của ba frame theo thiết kế.
- **History trên node parallel là theo từng region.** Mỗi region ghi riêng; khôi phục thì khôi phục
  hết.
- Transition `AnyState` được đánh giá một lần cho mỗi region, dựa trên leaf active của region đó. Vì
  vậy `AnyState → Stunned` nhắm ra ngoài node parallel sẽ thoát mọi region — đúng là hành vi mong
  muốn của một cú stun.

---

## 8. Transition async

Một behaviour override `OnEnterAsync` hoặc `OnExitAsync` sẽ làm mọi transition đi qua nó thành bất
đồng bộ. Máy khi đó chạy transition trải qua nhiều tick.

```
Phase: Idle  ──chọn transition──▶  Exiting
                                     │  await từng OnExitAsync trong chuỗi exit,
                                     │  trong trước; OnExit đồng bộ chạy tại chỗ
                                     ▼
                                  Entering
                                     │  await từng OnEnterAsync trong chuỗi enter,
                                     │  ngoài trước
                                     ▼
                                   Idle
```

Khi `Phase != Idle`:

- `Tick` đẩy `TimeInMachine` và bơm bộ điều khiển async; **không `OnUpdate` nào chạy** cho bất kỳ
  node nào — state nguồn đã thoát và state đích chưa vào xong;
- polled guard không được đánh giá;
- `Fire` vẫn xếp hàng.

### 8.1 Transition thứ hai giữa chừng thì sao

`AsyncPolicy`, chọn theo từng instance qua `MachineOptions`:

| Policy | Hành vi |
|---|---|
| `CancelAndReplace` (mặc định) | Huỷ token của chuỗi đang chạy. Node nào đã vào thì được thoát (đồng bộ, trong trước — `OnExitAsync` của chúng **không** được await trong lúc huỷ). Rồi bắt đầu transition mới. |
| `Queue` | Nhớ đích mới; chạy nó ngay khi chuỗi hiện tại về `Idle`. Chỉ nhớ một cái — cái thứ ba thay chỗ cái thứ hai. |
| `Ignore` | Bỏ transition mới. Ghi là `ignored` trong log transition. |

`CancelAndReplace` là mặc định vì trường hợp async phổ biến là chuyển màn hình hoặc chiêu thức, ở đó
ý định mới nhất mới là đúng, và vì một hàng đợi âm thầm phát lại một transition cũ về sau là con bug
khó tìm hơn.

### 8.2 Huỷ và giải phóng

- `Dispose()` giữa chừng huỷ token, thoát đồng bộ mọi node đã vào, và không bao giờ chạy `OnEnter`
  còn treo.
- Một `OnEnterAsync` bị huỷ phải quan sát `CancellationToken` của nó; behaviour nào phớt lờ thì chỉ
  đơn giản là kết thúc muộn, và máy vứt kết quả đi. Không có gì hỏng, nhưng tác dụng phụ của state
  đó vẫn đáp xuống — điều này ghi lại như một cạm bẫy chứ không phòng thủ chống lại.
- Máy không bao giờ ném `OperationCanceledException` ra khỏi `Tick`. Nó bị bắt ở ranh giới driver và
  log qua `DevLogger` dưới `APEXION_HFSM_DEBUG`.

---

## 9. Runner

```
MachineRunner.Tick(dt)
 │
 ├─ _ticking ← true
 ├─ với i trong 0.._machines.Count:  _machines[i].Tick(dt)
 ├─ _ticking ← false
 └─ áp dụng phần hoãn: xoá trước (swap-back), rồi thêm
```

- Đăng ký hoặc dispose một máy bên trong `Tick` là an toàn — cả hai đều hoãn tới cuối lượt. Máy bị
  dispose giữa lượt vẫn nhận tick của lượt đó; máy tạo giữa lượt thì không. Cả hai được ghi rõ để
  hành vi lúc spawn-khi-chết không thành bất ngờ.
- Xoá là swap-với-cuối, nên **thứ tự tick không ổn định** qua các lần xoá. Không được phụ thuộc vào
  nó. Thứ tự giữa các máy không phải khái niệm được hỗ trợ — cần trình tự cụ thể thì dùng
  `TickMode.Manual`.
- Máy nào có `Tick` ném lỗi thì bị bắt, log kèm tên debug, và **gỡ đăng ký**, để một agent hỏng
  không kéo sập frame mãi mãi. Đây là nơi duy nhất runtime nuốt một exception, và nó được gate để
  log thật to.

---

## 10. Diễn giải theo trình tự

`Idle → Combat/Chase → Combat/Attack → Flee`, với `Combat` có shallow history:

```
frame  sự kiện                        lời gọi, theo thứ tự
─────  ─────────────────────────────  ─────────────────────────────────────────────
  1    Start()                        OnEnter(Idle)
                                      OnUpdate(Idle)
  …
 45    guard c.SeesPlayer → true      OnExit(Idle)
                                      OnEnter(Combat)
                                      OnEnter(Chase)              ← Initial, chưa có history
                                      OnUpdate(Combat), OnUpdate(Chase)
  …
 92    guard Distance < 2f → true     OnExit(Chase)
                                      OnEnter(Attack)             ← lca = Combat, không bị thoát
                                      OnUpdate(Combat), OnUpdate(Attack)
  …
131    AnyState guard Health<20 → true  history(Combat) ← Attack   ← ghi trước OnExit
                                        OnExit(Attack)
                                        OnExit(Combat)
                                        OnEnter(Flee)
                                        OnUpdate(Flee)
  …
180    trigger Healed → To(Combat)    OnExit(Flee)
                                      OnEnter(Combat)
                                      OnEnter(Attack)             ← shallow history khôi phục Attack
                                      OnUpdate(Combat), OnUpdate(Attack)
```

Frame 92 là điểm mấu chốt của cả thiết kế: `Combat` không bị thoát rồi vào lại khi di chuyển giữa
các con của chính nó. Một FSM phẳng thì buộc phải thế, và mọi tác dụng phụ của `OnEnter(Combat)` sẽ
phát lại.

Chuỗi này chính là `S01_FullLifecycle_OrderIsExact` trong `ApexionGame.Core.Tests`, khẳng định dưới
dạng một chuỗi ký tự.
