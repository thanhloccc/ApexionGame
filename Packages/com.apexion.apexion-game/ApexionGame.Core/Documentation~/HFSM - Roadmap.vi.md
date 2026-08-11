# HFSM — Roadmap

*[English](HFSM%20-%20Roadmap.md) · [Index](README.vi.md)*

Phase, khối lượng, kế hoạch test, rủi ro. Các bước hành động chi tiết nằm ở
[HFSM - Overview §3](HFSM%20-%20Overview.vi.md#3-các-bước); file này là hình dạng của cả công việc và
những chỗ nó có thể hỏng.

---

## 1. Phase

Ước lượng LOC là code sản phẩm, không tính test.

| Phase | Sản phẩm | LOC | Phụ thuộc | Trạng thái |
|---|---|---|---|---|
| 0 | Bộ doc này | — | — | ✅ chờ duyệt |
| 1 | Sáu asmdef, `ValidationDefines`, `NodeIndex`/`TriggerId`, `MachineError` | ~450 | 0 | ✅ xong — chỉ một asmdef, xem §1.2 |
| 2 | Lõi: node, builder, validate, definition, máy, transition LCA | ~1 800 | 1 | ✅ xong, test xanh |
| 3 | Trigger, any-state, priority, min-duration, internal, history | ~500 | 2 | ✅ xong, test xanh |
| 4 | Parallel region | ~450 | 3 | ✅ xong, test xanh |
| 5 | Async enter/exit + policy huỷ | ~400 | 4 | ✅ xong, test xanh |
| 6 | Runner, pooling, `Reset`, benchmark | ~350 | 5 | ✅ xong, test xanh |
| 7 | Debugging: registry, log, bắt guard, cửa sổ editor, overlay | ~1 900 | 6 | ✅ xong — đồ thị cửa sổ vẽ theo cột/độ sâu phẳng, không phải box lồng nhau; xem §1.2 |
| 8 | Sample playground, `EnemyBrain`, scene | ~700 | 7 | ⬜ |
| 9 | Authoring ScriptableObject + catalog | ~600 | 6 | ⬜ |
| 10 | Ba code refactoring | ~700 | 2 | ⬜ |
| | **Tổng** | **~7 850** | | |

Phase 9 và 10 treo vào các phase sớm hơn, không phải vào phase 8 — chúng hoãn vô thời hạn được mà
không chặn gì. Phase 1–8 là xương sống.

### 1.2 Những chỗ phase 1–7 lệch khỏi kế hoạch này

Ghi lại đây thay vì để ai đó tự phát hiện lại:

- **Một assembly editor, không phải sáu.** Dự án chốt dùng chung một assembly
  `ApexionGame.Editor` cho mọi công cụ editor của `ApexionGame.*` thay vì một `ApexionGame.Core.Editor`
  riêng — `MachineDebuggerWindow` cùng `Views/`/`StyleSheets/` của nó nằm ở đó, dưới namespace
  `ApexionGame.HFSM.Editor` như kế hoạch. `ApexionGame.Core.Authoring`, `.Samples`, `.Samples.Editor`
  và một `.Tests` riêng chưa được tạo; test thay vào đó nằm trong assembly chung của cả project
  `ApexionGame.Tests.EditorMode` (thư mục con `ApexionGame.Core/HFSM/`), assembly này có từ trước
  tính năng này và đã chứa test của module Stats theo cùng cách.
- **Struct debug info nằm chung file, không mỗi cái một file.** `NodeDebugInfo`, `GuardDebugInfo`,
  `GuardMarker` và `TransitionDebugInfo` nằm trong `Debugging/IMachineDebug.cs` ngay cạnh interface
  trả về chúng, không phải các file riêng `StateNodeDebugInfo.cs`/`GuardDebugInfo.cs`/`GuardMeta.cs`
  — không có nơi dùng thứ hai cần tách chúng ra, và nhóm một interface nhỏ với dữ liệu thường nó trả
  về khớp với cách `NodeDebugInfo` đã được viết từ trước khi đợt này chạm vào file. `TransitionLog.cs`
  cũng chưa từng tách — ring buffer là vài field và method riêng ngay trên
  `HierarchicalStateMachine`2+Debug.cs`.
- **`Internals/ActiveSet.cs`, `StateDataBlob.cs`, `ScopeStack.cs` không tồn tại như type riêng.**
  Các phép toán bitset của active set là method private ngay trên máy
  (`HierarchicalStateMachine`2+Transitions.cs`), toán offset state-data nằm trên
  `MachineBuilder`2+Validate.cs`, và việc theo dõi scope đang mở của builder là một field
  `List<int>` bình thường trên `MachineBuilder`2.cs`. Mỗi cái hoá ra chỉ là vài method trên field
  của một type đã có sẵn, chưa đủ lý do để có file riêng — xem lại chỉ khi một trong số chúng có
  thêm lý do thứ hai để tồn tại độc lập.
- **Đồ thị vẽ box phẳng theo cột/độ sâu, không phải box lồng nhau.** HFSM - Debugging.md §4.3 mô tả
  một composite được vẽ như một container bọc quanh các con của nó. `StateGraphView` thay vào đó đặt
  mọi node (kể cả composite) tại `(độ sâu, thứ tự khai báo)`, tô theo active/active-leaf/parallel/
  dimmed — cùng cách layout `StatGraphView` dùng cho module Stats, tái dùng ở đây vì một cây không
  cần dò chu trình hay nới đường đi để đọc được. Containment lồng nhau là một khối lượng layout
  đáng kể cho thông tin mà `ActivePathView` thụt lề và cách tô node đã truyền tải rồi; để lại cho
  một đợt sau nếu hoá ra bị thiếu.
- **Cột nguyên nhân của transition log không nhúng nguyên văn source của guard.** Một dòng hiện
  `guard`, `trigger: AttackFinished`, `timer`, … — không phải `guard: c.Dist…<2f` như bản vẽ ở
  Overview §2.3. Guard pane là nơi giữ nguyên văn chính xác và giá trị sống; nhân đôi nó vào mỗi
  dòng log sẽ cần log giữ thêm một string mỗi entry cho một trường hợp guard pane đã phủ rồi.
- **Nút bật/tắt overlay trong game (`IVisualCommand`) chưa được nối.** Nó được khoanh vùng tường
  minh vào `ApexionGame.Core.Samples` (DEC-011), thứ phase 8 chưa tạo. Tới lúc đó,
  `MachineOverlayBehaviour.SetVisible(bool)` là nút bật/tắt.

### 1.1 Thứ tự không phải là thứ tự phụ thuộc

Phase 4 (parallel region) là mảnh rủi ro nhất và phase 7 (debugging) là mảnh lớn nhất. Cả hai nằm
muộn. Đó là cố ý:

- **Parallel đi sau history**, vì history-theo-region chỉ có nghĩa khi history một region đã chạy,
  và làm cả hai cùng lúc sẽ khiến một test fail trở nên mơ hồ về việc tính năng nào hỏng.
- **Debugging đi sau khi runtime hoàn chỉnh**, vì một debugger viết dựa trên một API đang động sẽ
  phải viết lại. Log transition là ngoại lệ duy nhất — nó rẻ và sẽ có ích trong phase 2–5, nên hãy
  dựng log (7.2) sớm nếu phase 3 hoặc 4 bắt đầu chống trả.

### 1.2 Bản dùng được đầu tiên rơi vào đâu

**Cuối phase 3** là điểm đầu tiên hệ thống thật sự dùng được: phân cấp, guard, trigger, timer,
history. Chừng đó phủ phần lớn AI enemy. Một team có thể bắt đầu viết brain từ đó trong khi phase
4–7 chạy tiếp.

Nói ra để kế hoạch không bị đọc thành được-ăn-cả-ngã-về-không: nếu buộc phải cắt phạm vi, cắt từ phía
sau. Parallel region và authoring bằng asset là hai mục dễ bỏ nhất.

---

## 2. Kế hoạch test

`ApexionGame.Core.Tests`, chạy qua Unity CLI với Editor **đã đóng**:

```powershell
unity test . --mode EditMode --output results.xml --timeout 2400 --non-interactive
unity test . --mode EditMode --filter "ApexionGame.HFSM.Tests.MachineBenchmarkTests" --output bench.xml
```

Ba ghi chú mang sang từ bản port Stats, mỗi cái đều đã tốn thời gian ở đó:

- **Unity phải đóng.** Batch mode không mở được project đang bị Editor khoá.
- **Test `[Explicit]` bị loại kể cả khi gọi đích danh trong `--filter`.** Benchmark vì vậy không mang
  `[Explicit]`.
- **Đọc file XML, không đọc stdout.** Output của CLI đầy lỗi telemetry không liên quan.

### 2.1 Golden test thứ tự — cửa chất lượng

Đây là những test quan trọng nhất. Mỗi cái khẳng định **một chuỗi ký tự chính xác**, không phải một
tập kiểm tra "contains", vì một lỗi thứ tự tạo ra đúng các lời gọi nhưng sai thứ tự chính là thứ mà
"contains" bỏ sót.

`RecordingBehaviour` nối `Enter:Chase`, `Update:Combat`, `Exit:Attack` vào một `StringBuilder` chung;
test so cả bản ghi.

| # | Kịch bản | Khoá lại |
|---|---|---|
| S01 | Vòng đời đầy đủ `Idle → Combat/Chase → Combat/Attack → Flee → Combat` | [Flows §10](HFSM%20-%20Flows.vi.md#10-diễn-giải-theo-trình-tự) từ đầu tới cuối |
| S02 | Thứ tự vào là ngoài-trước, thứ tự ra là trong-trước | [Flows §3.2](HFSM%20-%20Flows.vi.md#32-thứ-tự-vào), §3.4 |
| S03 | Thứ tự update bằng thứ tự vào | §3.3 |
| S04 | Transition giữa hai anh em trong một composite **không** vào lại composite | chính là lý do phân cấp tồn tại |
| S05 | Cây 4 tầng, transition giữa hai leaf sâu dưới một ông chung | tính đúng của LCA |
| S06 | `AnyState` thắng transition của leaf trong cùng một tick | §2 |
| S07 | Node sâu hơn thắng node nông hơn; `Priority` thắng thứ tự khai báo trong một node | §2 |
| S08 | `MinDuration` chặn, rồi thả | §2 |
| S09 | Trigger bắn trong `OnUpdate` được tiêu thụ ở tick sau, đúng một lần | §5 |
| S10 | Trigger không khớp bị bỏ và được ghi log | §5, DEC-007 |
| S11 | Shallow history quay về child; deep history quay về leaf | §6 |
| S12 | `.To(descendant)` tường minh đè lên history | §6 |
| S13 | Parallel: mọi region vào theo thứ tự khai báo, ra theo thứ tự ngược | §7 |
| S14 | Parallel: hai region chuyển độc lập trong cùng một tick | §7 |
| S15 | Parallel: rời node parallel thì thoát mọi region | §7 |
| S16 | Async enter await qua nhiều tick; không `OnUpdate` khi `Phase != Idle` | §8 |
| S17 | `CancelAndReplace` thoát các node đã vào trước khi khởi động cái thay thế | §8.1 |
| S18 | `Dispose` giữa chừng không chạy `OnEnter` nào còn treo | §8.2 |
| S19 | Self-transition vào lại; `.Internal()` thì không | §4.1 |
| S20 | `Reset` xoá history, timer và state data | §6, Data Model §4.2 |

### 2.2 Các bộ test khác

| Bộ | Số lượng | Phủ |
|---|---|---|
| `MachineBuilderValidationTests` | 12 | mỗi case `MachineError` một cái, mỗi cái từ một input sai thật |
| `MachineErrorTests` | 4 | `Undefined`, thông điệp chính xác của một case có payload, `Prefix`, `default(MachineError)` |
| `MachineStateDataTests` | 6 | cô lập theo instance qua 3 máy, căn lề cho `double`/`float3`, về 0 khi `Reset`, đường blob độ dài 0 |
| `MachineRunnerTests` | 6 | đăng ký trong tick, dispose trong tick, máy ném lỗi bị gỡ đăng ký, swap-remove không bỏ sót máy nào |
| Bộ debug | 7 | [Debugging §7](HFSM%20-%20Debugging.vi.md#7-test-cho-tầng-debug) |
| `MachineBenchmarkTests` | 5 | §4 bên dưới |

Mục tiêu: **~90 test**, khớp mật độ mà module Stats kết thúc (95).

---

## 3. "Xong" nghĩa là gì ở từng phase

Chép lại đây để một phiên làm việc mới không phải suy ra:

| Phase | Tiêu chí ra |
|---|---|
| 1 | Sáu assembly compile qua Unity; factory của `MachineError` tồn tại **sau một lần compile Unity**, không chỉ trong IDE |
| 2 | S01–S05 pass; mọi case `MachineError` có input sinh ra nó; 1 000 tick không cấp phát |
| 3 | S06–S12 pass |
| 4 | S13–S15 pass; `TransitionCrossesParallelRegion` bắn trên một transition cắt ngang thật |
| 5 | S16–S18 pass |
| 6 | C4 (0 GC) và C5 (< 0.3 ms) đo được, số dán vào §4 |
| 7 | Cửa sổ giải thích được một transition bị kẹt; ảnh chụp trong file này; bộ test debug pass |
| 8 | Cả mười nút playground cho ra đúng output đã ghi |
| 9 | Máy dựng từ asset và máy dựng từ code cho log transition trùng khớp qua 100 tick |
| 10 | 9 test refactor pass qua `dotnet test` |

---

## 4. Benchmark

`MachineBenchmarkTests`, 500 máy × 20 node × 4 behaviour mỗi cái 16 B state data, 1 000 tick.
Editor/Mono, safety check bật — **chỉ để so tương đối**, bản build player nhanh hơn nhiều. Cái nó bắt
được là một thao tác lỡ thành O(máy) thay vì O(1).

| # | Đo gì | Mục tiêu |
|---|---|---|
| B01 | Build definition một lần | < 5 ms |
| B02 | Tạo 500 instance | < 3 ms, mỗi cái một đợt cấp phát |
| B03 | 1 000 tick không transition nào (mọi guard false) | < 0.15 ms/tick cho 500 máy |
| B04 | 1 000 tick với 10 % số máy chuyển state mỗi tick | < 0.30 ms/tick |
| B05 | Dispose 500 rồi tạo lại từ pool | tạo lại rẻ hơn B02 |
| B06 | **0 GC**: B03 và B04 dưới `Is.Not.AllocatingGCMemory()` | 0 byte sau warm-up |

> Số điền vào sau khi phase 6 chạy thật. Để bảng trống tới lúc đó là cố ý — một kế hoạch viết sẵn kết
> quả của chính nó là một kế hoạch sẽ không nhận ra khi thực tế nói khác.

---

## 5. Sổ rủi ro

| # | Rủi ro | Mức | Giảm thiểu |
|---|---|---|---|
| R1 | **Parallel region tương tác xấu với history và `AnyState`.** Ba tính năng mà tổ hợp của chúng là chỗ các bản hiện thực statechart thường vỡ. | **Cao** | Phase 4 đáp sau history và được test cùng nó (S13–S15 cộng một ca history-trong-parallel). Nếu nó chống trả, nó là ứng viên bị bỏ đã được chỉ định — phần còn lại của hệ thống không phụ thuộc vào nó. |
| R2 | **Huỷ async để lại cỗ máy ở cấu hình vào dở.** Đã vào vài node, bị huỷ, và tập active không còn khớp thực tế. | **Cao** | Tập active chỉ được chốt sau khi một chuỗi hoàn tất; một chuỗi bị huỷ thoát đúng những gì nó đã vào, theo dõi trong một list đã thuê. S17/S18 khẳng định cấu hình kết quả một cách tường minh, không chỉ "không ném lỗi". |
| R3 | **Căn lề của `UnsafeUtility.As`.** Một `ref` lệch căn tới `double` hoặc `float3` là hành vi không định nghĩa trên vài nền tảng và sẽ không fail trong Editor. | Trung | Offset căn theo `AlignOf<TData>()` lúc build; `MachineStateDataTests` dùng đúng `double` và `float3`; xác minh trên một bản build IL2CPP trước khi phase 8 đóng. |
| R4 | **Debugger dựng lại cây ở mỗi lượt poll**, lặp lại bản đầu của cửa sổ Stats — cuộn và lựa chọn không dùng được. | Trung | [Debugging §4.1](HFSM%20-%20Debugging.vi.md#41-luật-mà-debugger-của-stats-đã-trả-giá-để-học) viết như một yêu cầu kèm bốn lỗi cụ thể được gọi tên. Duyệt 7.4 đối chiếu đúng danh sách đó. |
| R5 | **Quên `versionDefines` ở một trong sáu asmdef**, khiến `#if UNITASK` âm thầm xoá bề mặt async khỏi assembly đó. | Trung | Bước 1.1/1.2 chép khối vào cả sáu. Kiểm một lần bằng cách bọc một lỗi cú pháp cố ý trong `#if UNITASK` và xác nhận compiler có kêu. |
| R6 | **Lambda guard capture ngoài ý muốn**, cấp phát mỗi lần build máy và, tệ hơn, dùng chung trạng thái giữa các máy dựng từ cùng một đường code. | Trung | Quy ước là lambda `static` ở mọi nơi trong doc và sample. Benchmark 0-cấp-phát bắt được phần cấp phát; review code bắt được phần dùng chung. Cân nhắc một analyzer về sau. |
| R7 | **Builder theo scope cho lỗi khó hiểu** khi thiếu `EndComposite()` — cỗ máy build "thành công" với hình dạng sai. | Thấp | `UnbalancedScope` bắn bất cứ khi nào ngăn xếp scope còn phần tử lúc `Build()`, và nó gọi tên node còn mở. Có test. |
| R8 | **500 lời gọi interface mỗi frame qua `IMachineTickable`** chậm hơn dự kiến trên IL2CPP. | Thấp | B03/B04 đo đúng cái đó. Nếu nó thành vấn đề, runner giữ một list có kiểu theo từng definition — một thay đổi gọn nằm sau cùng một API. |
| R9 | **Phình phạm vi thành behaviour tree.** Mọi HFSM cuối cùng đều bị hỏi xin utility scoring hoặc một cây con. | Thấp | Đã gọi tên là phi mục tiêu ở [Overview §4.2](HFSM%20-%20Overview.vi.md#42-phi-mục-tiêu). BT là một module riêng có thể *chứa* một HFSM, không phải mọc bên trong nó. |
| R10 | **Hai máy dùng chung một instance behaviour có state data.** Behaviour là flyweight nên dùng chung là hợp lệ — nhưng ô data là theo node, và một instance behaviour đặt ở hai node sẽ có hai ô, điều đó đúng nhưng gây bất ngờ. | Thấp | Ghi trong Data Model §3.3 và có test khẳng định hai ô giữ độc lập. |

---

## 6. Những thứ cố ý không đưa vào kế hoạch

Liệt kê ra để một phiên làm việc sau không coi sự vắng mặt của chúng là một thiếu sót:

| | Vì sao không |
|---|---|
| Đường tick Burst/job | Tải là vài trăm agent; một tầng native sẽ buộc state không được giữ reference nào. Chỉ xem lại khi có phép đo cho thấy đường managed là nút thắt. |
| Serialize một cỗ máy đang chạy vào save | Cần định danh node có version, sống sót qua một lần sắp xếp lại enum. Đường được hỗ trợ là `Reset()` rồi vào lại. |
| *Trình soạn* node-graph trực quan | Đồ thị của debugger là chỉ đọc và phủ được nhu cầu "cái này hình dạng ra sao". Một trình soạn là một dự án riêng. |
| Animation transition / blend tree | Thuộc về hệ animation; HFSM bắn trigger rồi tránh đường. |
| Máy có networking / an toàn rollback | Timer `float` làm rollback không vững. Một biến thể tất định là một thiết kế khác, không phải một cái cờ. |
| Analyzer bắt lambda guard không `static` | Sẽ thật sự có ích, nhưng cần mở rộng project Roslyn analyzer; xem lại sau khi phase 10 chứng minh provider refactoring chạy được. |
