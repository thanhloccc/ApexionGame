# HFSM — Debugging

*[English](HFSM%20-%20Debugging.md) · [Index](README.vi.md)*

Bốn mặt, cả bốn đều được yêu cầu. Chúng tồn tại để trả lời bốn câu hỏi khác nhau:

| Mặt | Câu hỏi nó trả lời |
|---|---|
| Khung active path | *Nó đang ở đâu, và bao lâu rồi?* |
| Guard inspector | ***Vì sao nó không chuyển?*** — cái làm nên giá trị của cả tính năng |
| Log transition | *Vừa xảy ra chuyện gì, và do đâu?* |
| Graph view | *Máy này hình dạng ra sao, và nhánh nào đang sống?* |
| Overlay on-screen | tất cả những cái trên, trên thiết bị, trong một bản build |

---

## 1. Tắt đi thì tốn gì

Mọi thứ trong tài liệu này đều gate sau `APEXION_HFSM_DEBUG`, tự động bật trong `UNITY_EDITOR` và
`DEVELOPMENT_BUILD`, và gỡ được bằng `DISABLE_APEXION_CHECKS`.

```csharp
namespace ApexionGame.HFSM.Debugging
{
    public static class ValidationDefines
    {
#if DISABLE_APEXION_CHECKS
        public const string UNITY_EDITOR = "__DISABLE_APEXION_CHECKS__";
        public const string HFSM_DEBUG = "__DISABLE_APEXION_CHECKS__";
        public const string RUNTIME_CHECKS = "__DISABLE_APEXION_CHECKS__";
#else
        public const string UNITY_EDITOR = "UNITY_EDITOR";
        public const string HFSM_DEBUG = "APEXION_HFSM_DEBUG";
        public const string RUNTIME_CHECKS = "APEXION_RUNTIME_CHECKS";
#endif
    }
}
```

Khi define tắt, một bản release trả giá:

| | |
|---|---|
| Đăng ký registry | không gì — lời gọi là `[Conditional]` |
| Log transition | không gì — ring buffer không bao giờ được cấp phát |
| Metadata guard (nguyên văn, tên) | không gì lúc chạy; chuỗi literal của `[CallerArgumentExpression]` vẫn nằm trong assembly, tổng cộng vài trăm byte ([DEC-015](HFSM%20-%20Decisions.vi.md#dec-015)) |
| Ghi lại kết quả đánh giá guard | không gì — nhánh ghi nằm bên trong một hàm `[Conditional]` |

Cùng cấu trúc với `ApexionGame.Entities.Stats/Debugging/ValidationDefines.cs`, nên một mô hình tư duy
phủ được cả hai module.

---

## 2. Tìm máy đang sống

Một cỗ máy là một object managed bình thường do người tạo nó sở hữu. Không có world, không có
singleton để đi duyệt — đúng bài toán mà debugger của Stats đã phải giải cho bộ nhớ native của store.

Module Stats giải bằng **đăng ký tự nguyện**: người dùng dựng một `StatStoreDebug<4>` rồi gọi
`StatDebugRegistry.Register`. Ở đó điều đó đúng, vì một store là bộ nhớ native trần và việc đăng ký
nó có chi phí thật.

Ở đây, **đăng ký là tự động**:

```csharp
// trong constructor của HierarchicalStateMachine<TContext, TState>
RegisterForDebugging();

[Conditional(ValidationDefines.HFSM_DEBUG)]
private void RegisterForDebugging() => MachineDebugRegistry.Register(this);
```

`HierarchicalStateMachine<TContext, TState>` hiện thực `IMachineDebug` ngay trong
`HierarchicalStateMachine`2+Debug.cs`, nên không có object adapter. Khi define bật, một cỗ máy xuất hiện trong debugger ngay lúc nó tồn tại; khi define tắt,
`RegisterForDebugging` thậm chí không phải một lời gọi.

Đây là chỗ cố ý lệch khỏi mẫu của Stats, biện minh bằng khác biệt chi phí: một object managed trong
`List<IMachineDebug>` tốn một reference, trong khi `StatStoreDebug` bọc bộ nhớ native mà registry
không được phép kéo dài vòng đời ([DEC-016](HFSM%20-%20Decisions.vi.md#dec-016)).

**`Dispose()` bắt buộc phải gỡ đăng ký.** Một đăng ký sống lâu hơn cỗ máy của nó là một chỗ rò giữ
context sống mãi. `Dispose` làm việc đó; registry cũng bỏ những mục có `IsAlive` false lúc cửa sổ
poll, nên một cỗ máy bị bỏ rơi không `Dispose` sẽ biến mất chứ không lởn vởn như một bóng ma.

---

## 3. Log transition

Ring buffer theo từng instance, 32 mục, chỉ cấp phát khi có define.

```csharp
public readonly struct TransitionLogEntry
{
    public readonly NodeIndex From;
    public readonly NodeIndex To;
    public readonly TransitionCause Cause;   // Initial | Guard | Trigger | Timer | Request | History
    public readonly int CauseIndex;              // index transition — truy ngược ra nguyên văn guard
    public readonly float TimeInSource;          // đã ngồi ở state nguồn bao lâu
    public readonly int Frame;
    public readonly float RealtimeSinceStartup;
}
```

Cũng ghi luôn những cái sau, vì đó mới là sự kiện người ta thật sự đi săn:

| Sự kiện | Ghi là |
|---|---|
| Một trigger bắn nhưng không khớp gì ở cấu hình hiện tại | `Cause = Trigger`, `To = default`, đánh dấu `unmatched` |
| Một transition bị `MinDuration` từ chối | không ghi — sẽ ngập log; khung guard hiện nó theo thời gian thực thay thế |
| Một transition async bị huỷ và thay thế | hai mục: cái bị huỷ đánh dấu `cancelled`, rồi cái thay thế |
| Một transition async bị bỏ dưới `AsyncPolicy.Ignore` | một mục đánh dấu `ignored` |
| `Reset()` | một mục, `Cause = Initial` |

Buffer là một mảng cố định với con trỏ ghi. 10 000 transition không cấp phát gì — khẳng định trong
`MachineRunnerTests`.

---

## 4. Cửa sổ editor

`ApexionGame > HFSM > Debugger`, thuộc `ApexionGame.Core.Editor`, UI Toolkit dựng bằng C#.

```
┌─ HFSM Debugger ──────────────────────────────────────── [Auto ✓] [⏸] ─┐
│ Machine: [ EnemyBrain #0034            ▾ ]   17 live · 4 defs         │
├──────────────────────────┬────────────────────────────────────────────┤
│ ACTIVE PATH              │  STATE TREE                                │
│  Root                    │                 ┌────────┐                 │
│  └ Combat        4.82 s  │    ┌───────┐    │ Combat │◀── active       │
│    └ Attack      0.31 s  │    │ Idle  │───▶│════════│                 │
│                          │    └───────┘    │ Chase  │                 │
│ history(Combat) = Chase  │        │        │ Attack │◀── leaf active  │
│                          │        ▼        └────────┘                 │
│ ── GUARDS (từ Attack)    │    ┌────────┐        │                     │
│  ● Attack→Chase          │    │ Patrol │        ▼                     │
│      on AttackFinished   │    └────────┘    ┌──────┐                  │
│  ○ *→Flee   false        │                  │ Flee │                  │
│      c.Health < 20f      │                  └──────┘                  │
│      Health = 74.0       │                                            │
├──────────────────────────┴────────────────────────────────────────────┤
│ TRANSITION LOG                      frame     Δt      cause           │
│  Chase   → Attack                    5142   0.42s   guard: c.Dist…<2f │
│  Combat  → Combat/Chase (history)    5106   —       enter             │
│  Patrol  → Combat                    5104   1.90s   guard: c.SeesPl…  │
└───────────────────────────────────────────────────────────────────────┘
```

### 4.1 Luật mà debugger của Stats đã trả giá để học

**Poll không có nghĩa là dựng lại.**

Bản đầu của debugger Stats poll mỗi 0.25 s rồi dựng lại toàn bộ cây UI mỗi lần. Cuộn bị reset, lựa
chọn bị ghi đè giữa lúc thao tác, đồ thị nhấp nháy, và dictionary bị cấp phát liên tục. Cách sửa là
so **hình dạng** của dữ liệu với thứ đang hiện và chỉ đụng vào cây khi hình dạng đổi; con số thì ghi
thẳng vào các label sẵn có.

Cùng cách phân tầng áp dụng ở đây, và đó là yêu cầu, không phải lời khuyên:

| Cái gì đổi | Controller làm gì | Thực tế xảy ra bao lâu một lần |
|---|---|---|
| Tập máy đang sống | `SetMachines` — dựng lại dropdown | lúc spawn/despawn |
| Definition của máy được chọn | `SetTopology` — dựng lại node và cạnh của đồ thị | lúc đổi lựa chọn |
| Cấu hình active | `SetActive` — bật/tắt class USS trên các element node sẵn có | lúc có transition |
| Thời gian, kết quả guard, hàng log | `SetValues` — ghi vào label sẵn có | **mỗi lượt poll** |

Bốn lỗi cụ thể mà cửa sổ Stats đã dính, ở đây tránh được ngay từ cấu trúc:

1. Nhãn dropdown nhúng một con số sống (`"battle (17 owners)"`), nên lựa chọn mất mỗi khi số đó đổi.
   **Nhãn máy chỉ được mang định danh** — tên và id instance, không có số sống.
2. `SetSelectionWithoutNotify` chạy mỗi lượt poll và đè lên thứ người dùng vừa bấm. Chỉ đặt lựa chọn
   khi mục đang chọn thật sự biến mất.
3. Sample đặt tên store theo từng tình huống, nên store như biến mất mỗi lần bấm nút. Máy trong
   playground giữ một `DebugName` ổn định.
4. Ba thông báo "không có gì để hiện" cùng lúc. Một empty state duy nhất, chọn theo thứ tự ưu tiên.

Cũng mang sang: nút **Auto** để đóng băng khung nhìn, và `TwoPaneSplitView` cho vạch chia bảng/đồ thị.

### 4.2 Guard inspector

Khung làm nên giá trị của cả tính năng. Với cấu hình đang active, nó liệt kê **mọi** transition đi
ra, theo đúng thứ tự đánh giá ([Flows §2](HFSM%20-%20Flows.vi.md#2-thứ-tự-đánh-giá)), kèm:

| Cột | Nội dung |
|---|---|
| Dấu | `●` đủ điều kiện và sẽ bắn · `○` đã đánh giá, false · `◌` không được đánh giá ở frame này (một cái ưu tiên cao hơn đã thắng) · `⏳` bị `MinDuration` chặn, kèm thời gian còn lại |
| Đường | `Attack → Chase`, hoặc `* → Flee` với transition `AnyState` |
| Điều kiện | **nguyên văn mã nguồn** của guard từ `[CallerArgumentExpression]`, hoặc `on AttackFinished` với trigger, hoặc `after 2.0s` với timer |
| Giá trị | với guard là kết quả sống; chỗ nào guard là một phép so sánh đơn với một field của context thì hiện luôn giá trị hiện tại của field bên cạnh |

`Health = 74.0` đặt cạnh `c.Health < 20f` là khác biệt giữa "guard đang false" và biết *vì sao*. Nó
là nỗ lực tốt nhất có thể: dòng giá trị chỉ hiện khi nguyên văn khớp mẫu đơn giản
`c.<Field> <op> <literal>`, phân tích một lần lúc build. Không khớp thì hàng đó chỉ hiện biểu thức và
kết quả boolean, không gì hơn — một heuristic suy giảm về thứ vẫn còn dùng được
([DEC-017](HFSM%20-%20Decisions.vi.md#dec-017)).

**Đánh giá guard để hiển thị không được ảnh hưởng tới cỗ máy.** `EvaluateOutgoingGuards` gọi đúng
những delegate mà đường tick gọi, và điều đó chỉ an toàn vì guard được kỳ vọng là thuần. Kỳ vọng đó
được ghi trong tài liệu API và nhắc lại ở đây; một guard có tác dụng phụ sẽ áp dụng nó hai lần khi
debugger đang mở, và đó là bug của guard.

### 4.3 Graph view

Vẽ bằng `Painter2D` trong `generateVisualContent`, đọc `layout` đã resolve, nên repaint lại sau
`GeometryChangedEvent`. Tái dùng thẳng cách làm của đồ thị observer trong Stats.

| Hình ảnh | Ý nghĩa |
|---|---|
| Hộp node, viền sáng | nằm trên đường active |
| Hộp node, tô đặc | chính là **leaf** active |
| Hộp node, làm mờ | không active |
| Hộp lồng | composite vẽ như một khung bao quanh các con — phân cấp mới là điểm chính |
| Khung nét đứt | node parallel; mỗi region một ngăn riêng |
| Cạnh có đầu mũi tên | một transition; chiều mới là toàn bộ ý nghĩa |
| Cạnh dày | vừa bắn trong một giây gần đây |
| Cạnh chấm | do trigger chứ không phải polled |
| Huy hiệu đồng hồ nhỏ | transition có `MinDuration` hoặc là một `.After()` |

Bố cục: node xếp theo độ sâu cây (cột) và thứ tự anh em (hàng). Khác đồ thị Stats, ở đây không có
chuyện tinh vi về longest-path — cấu trúc là cây, không phải DAG, nên độ sâu là rõ ràng.

**Không làm:** kéo thả node tự do, lưu bố cục, sửa cỗ máy từ đồ thị. Nó là khung nhìn chỉ đọc của một
cấu trúc được định nghĩa bằng code.

### 4.4 Bấm để đi xuyên

Chọn một hàng ở khung guard sẽ tô sáng cạnh tương ứng trong đồ thị, và chọn một node trong đồ thị sẽ
lọc khung guard về các transition của node đó. Debugger của Stats ghi rõ điều này là *"đáng có, chưa
làm"* — ở đây nó nằm trong phạm vi, vì với phân cấp cộng parallel region thì số hàng lớn hơn và đồ
thị là cách duy nhất để giữ chúng còn định vị được.

---

## 5. Overlay on-screen

UI Toolkit runtime, nên nó chạy trong development build nơi không có cửa sổ Editor nào.

```
┌ HFSM · EnemyBrain #0034 ─────────┐
│ Root/Combat/Attack        0.31 s │
│ last: Chase→Attack  (guard)      │
└──────────────────────────────────┘
```

| | |
|---|---|
| Element | `MachineOverlayPanel : VisualElement` trong `HFSM/Debugging/`, dựng bằng C#, style inline qua một cặp `*Theme`/`*Widgets` nhỏ — không `StyleSheet` serialize, nên chạy mà không cần nối dây asset |
| Vật chủ | `MachineOverlayBehaviour : MonoBehaviour` kèm `UIDocument`; thả vào scene, hoặc spawn từ code |
| Lựa chọn | xoay vòng qua các máy đã đăng ký, hoặc lọc theo tiền tố `DebugName` |
| Nội dung | active path, thời gian ở state, transition cuối và nguyên nhân; tuỳ chọn thêm 3 hàng guard đầu |
| Chi phí | poll 10 Hz, ghi vào label sẵn có; cả panel nằm trong `#if APEXION_HFSM_DEBUG` |
| Bật/tắt | một `IVisualCommand` đăng ký từ **`ApexionGame.Core.Samples`**, để `ApexionGame.Core` không bao giờ tham chiếu `EncosyTower.Core.Extended` ([DEC-011](HFSM%20-%20Decisions.vi.md#dec-011)) |

Overlay cố ý hiện ít hơn cửa sổ. Màn hình thiết bị không có chỗ cho một đồ thị, và câu hỏi người ta
hỏi trên thiết bị là "nó kẹt ở state nào" và "chuyện cuối cùng xảy ra là gì".

---

## 6. Logging

`DevLogger` từ `EncosyTower.Logging`, không bao giờ `UnityEngine.Debug` trực tiếp.

| Sự kiện | Mức | Gate bởi |
|---|---|---|
| `Tick` của một máy ném lỗi và nó bị gỡ đăng ký | Error | luôn luôn — cái này phải kêu to cả trong bản release |
| Một trigger bị bỏ vì không khớp | Warning | `APEXION_HFSM_DEBUG` |
| Một transition async bị huỷ | Info | `APEXION_HFSM_DEBUG` |
| `BuildOrThrow` thất bại | Exception, kèm `MachineError.ToString()` | luôn luôn |
| Mọi transition | — | **mặc định không log.** Log từng transition ở 500 agent là một bức tường chữ; log transition tồn tại đúng để không cần cái đó. Một `machine.VerboseLogging = true` chọn tham gia phủ trường hợp ai đó thật sự muốn nó cho một agent. |

---

## 7. Test cho tầng debug

Tầng debug cũng cần test — một debugger nói dối còn tệ hơn không có.

| Test | Khẳng định |
|---|---|
| `Registry_MachineAppearsAndDisappears` | tạo → có mặt; `Dispose` → biến mất |
| `Log_RingBufferWrapsWithoutAllocation` | 10 000 transition, giữ lại 32, `Is.Not.AllocatingGCMemory()` |
| `Log_RecordsCauseAndSourceDuration` | một transition do guard ghi `Cause = Guard`, đúng index transition, và đúng thời gian thật ở nguồn |
| `Log_UnmatchedTriggerIsRecorded` | một trigger không khớp transition nào sinh ra một mục có đánh dấu |
| `Guards_EvaluationDoesNotMutateMachine` | trạng thái máy trước và sau `EvaluateOutgoingGuards` là như nhau |
| `Guards_OrderMatchesTickOrder` | thứ tự khung guard bằng thứ tự đánh giá ở [Flows §2](HFSM%20-%20Flows.vi.md#2-thứ-tự-đánh-giá) |
| `DebugDisabled_NoRegistrationNoLog` | compile với define tắt, registry rỗng và mảng log là null |
