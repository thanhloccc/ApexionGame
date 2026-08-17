# Architecture Skill — Đặc tả skill

*[Tổng quan](Architecture Skill - Overview.md) · [Quyết định](Architecture Skill - Decisions.md)*

Một skill: `system-design`. File viết **tiếng Anh**, `description` giữ cụm trigger tiếng Việt.

---

## 0. `description` frontmatter

Phải bắt được **mọi yêu cầu feature**, không chỉ prompt có chữ "architecture" — vì đây là cổng
thường trực, không phải tài liệu tra cứu.

> **description** — REQUIRED on every feature request, before any plan is written and before any
> code exists. Owns the design of a feature below the assembly boundary: how it decomposes into
> collaborating pieces, **who owns each piece of state and who may write it**, how the pieces
> communicate, which alternatives were rejected and why, and how much performance machinery the
> feature actually warrants. Also owns the engineering judgment around those choices — naming a
> tradeoff instead of hiding it, pushing back with a concrete cost when a request leads to a bad
> design, reading unfamiliar code before changing it, and recognising technical debt. A design that
> cannot say who owns its state is not a design, it is an intention. Load it alongside the project's
> module-selection skill on any "build X" request, when reviewing a design, when deciding whether
> something warrants jobs/parallelism or a specialised data structure, and when a decision needs to
> be recorded. Triggers on "design", "architecture", "how should I structure", "who owns", "state",
> "coupling", "abstraction", "interface", "tradeoff", "over-engineering", "premature optimization",
> "should I use jobs", "worth optimizing", "technical debt", "build", "implement", "add a system",
> "new feature", and Vietnamese phrasing "thiết kế", "kiến trúc", "làm feature", "thêm hệ thống",
> "cấu trúc thế nào", "ai giữ state", "có nên dùng job không", "tối ưu sớm", "nợ kỹ thuật",
> "để chỗ này thế nào cho đúng".

**Rủi ro đã biết:** trigger trùng `encosy-tower` (cũng bắt "làm feature", "implement"). Đó là **cố
ý** — cả hai phải cùng nạp. Ranh giới ghi rõ trong cả hai skill: `encosy-tower` trả lời *dùng module
nào*, `system-design` trả lời *feature có hình dạng gì*.

---

## 1. `SKILL.md`

### Mở đầu — đọc profile

Bốn dòng chuẩn như bộ `midcore-*`, cộng một dòng riêng:

> Check `performance.device_tiers` before choosing a performance tier. If it says `unknown — ask`,
> pick the lowest tier that plausibly works and say what would change the answer — do not escalate
> machinery to cover an unknown.

### Ba câu hỏi bắt buộc

> **Ai sở hữu state nào · Các mảnh nói chuyện bằng gì · Anh đã loại phương án nào và vì sao**

Kèm một câu giải thích vì sao đúng ba câu này chứ không phải mười.

### Cổng feature

Bảng 5 mục ở [Overview §2.3], kèm luật: **plan thiếu một mục thì chưa đưa ra review.** Mục thứ năm
(mức hiệu năng) có điều kiện.

### Bốn hành vi Senior

Bảng ở [Overview §2.4], mỗi hành vi một dòng, chi tiết đẩy vào reference.

Riêng phản biện phải nêu đủ **ba nhịp**, vì thiếu nhịp ba là biến phản biện thành cản trở:

1. nêu cái giá **cụ thể** (không phải "cách này không tốt");
2. đề xuất phương án thay thế;
3. **anh giữ nguyên quyết định → làm theo, đầy đủ**, và ghi lại là đã cảnh báo.

### Bảng nhường quyền

| Câu hỏi | Chủ |
|---|---|
| Assembly nào, tầng nào, phụ thuộc vòng, compile time | `midcore-assembly-architecture` |
| Dùng module/API nào của thư viện | authority `structure_naming` trong profile |
| Đặt tên, folder, namespace, thứ tự member | authority `structure_naming` |
| Type/API viết ra sao, format, attribute | `coding-standards` |
| Đổi cấu trúc code **đã có** | `refactoring` |
| Truy nguyên một lỗi đã biểu hiện | `debugging` |
| Ngân sách frame/memory, profiling, bằng chứng before/after | `midcore-perf-budget` |
| Bảng dữ liệu nội dung | `midcore-data-pipeline` |
| Hình dạng dữ liệu được persist | `midcore-save-migration` |
| Rà diff, tìm lỗi chưa biểu hiện | `code-review`, `simplify` (built-in) |

---

## 2. Reference

### `state-ownership.md` — quan trọng nhất, viết trước

Luật: **mỗi mẩu state có đúng một chủ.** Chủ là nơi duy nhất được ghi; mọi nơi khác đọc, hoặc yêu
cầu chủ đổi.

Nội dung:

- **Mẫu bảng sở hữu** — mẩu state · chủ · ai được đọc · ai được yêu cầu đổi · sống từ khi nào tới khi
  nào. Đây là artifact plan doc phải chứa.
- **State gốc vs state suy ra.** State suy ra không được lưu song song với gốc — nó được tính. Hai
  bản của cùng một sự thật là bug đang chờ, và là nguồn của cả một lớp lỗi "UI hiển thị sai".
- **Vòng đời** — tạo lúc nào, huỷ lúc nào, cái gì sống qua đổi scene, cái gì vào save. Nối sang
  `midcore-save-migration`: state vào save thì hình dạng của nó là hợp đồng vĩnh viễn.
- **Kiểu hỏng thường gặp:** hai hệ cùng nhận là chủ · state không ai nhận (thường là một `static`
  ai cũng ghi) · chủ là một MonoBehaviour trên scene nên không test được · UI giữ state gốc.
- **Phép kiểm:** với mỗi mẩu state, hỏi *"nếu hai chỗ cùng ghi trong một frame thì ai thắng?"* Không
  trả lời được nghĩa là chưa có chủ.

### `decomposition.md`

- Chia theo **thứ thay đổi cùng nhau**, không chia theo tầng kỹ thuật hay theo màn hình.
- Seam đáng cắt trong một game: **mô phỏng / trình bày / lưu trữ** — vì ba thứ đó thay đổi vì ba lý
  do khác nhau và có ba nhịp độ khác nhau.
- Dấu hiệu một mảnh làm quá nhiều: mô tả nó phải dùng chữ "và"; test nó phải dựng nửa game; đổi hai
  thứ không liên quan đều phải sửa nó.
- Dấu hiệu chia **quá** nhỏ: một mảnh chỉ chuyển tiếp lời gọi; đọc một luồng phải mở năm file; mọi
  thay đổi đều đụng ba mảnh.
- **Kích cỡ đúng là kích cỡ đọc được**, không phải kích cỡ nhỏ nhất.

### `communication.md`

Bảng chọn cơ chế, kèm **giá phải trả** — đây là chỗ hầu hết thiết kế trả giá âm thầm:

| Cơ chế | Dùng khi | Giá |
|---|---|---|
| Gọi trực tiếp | Bên gọi cần kết quả, và được phép biết bên kia | Ràng buộc cứng, nhưng đọc được và debug được |
| Query / request | Cần trả lời, không muốn biết ai trả lời | Một chỗ gián tiếp; phải có đúng một người trả lời |
| Event / message | Không cần kết quả, 0..N người nghe | **Luồng điều khiển khó lần**; thứ tự không đảm bảo |
| State chung + poll | Nhiều bên đọc, đọc mỗi frame | Không biết ai đổi lúc nào; dễ vỡ nếu không có chủ rõ |

Luật: **dùng thứ trực tiếp nhất mà không tạo phụ thuộc ngược hướng.** Event không phải mặc định —
nó là công cụ để phá một phụ thuộc mà anh **không được phép có**, và nó trả giá bằng khả năng lần
luồng. Dùng event để "cho đỡ ràng buộc" khi ràng buộc đó vốn hợp lệ là một cách làm code khó đọc mà
không đổi lại được gì.

Nối lên `midcore-assembly-architecture`: nếu cơ chế được chọn chỉ vì phụ thuộc trỏ sai hướng, thì đó
là bài toán tầng assembly, không phải bài toán giao tiếp.

### `tradeoffs-and-pushback.md`

- **Gọi tên trục.** Bộ trục hay gặp: tốc độ giao vs chi phí thay đổi sau · đơn giản vs tổng quát ·
  hiệu năng vs đọc được · an toàn vs linh hoạt. Nêu trục, **chọn một bên**, nói vì sao.
- **Cấm "còn tùy" trần.** Được phép nói "còn tùy", nhưng phải kèm: tùy vào **cái gì**, và **nếu là
  tôi thì tôi chọn gì**.
- **Thiết kế cho thay đổi anh biết sẽ tới**, không phải mọi thay đổi có thể tới. Đây là ranh giới
  giữa mở rộng hợp lý và over-engineering, và nó là câu hỏi thực nghiệm: *có bằng chứng nào cho thấy
  thay đổi này sẽ tới không?*
- **Ba nhịp phản biện** (§1), kèm kịch bản mẫu và cách ghi lại cảnh báo.

### `performance-by-design.md`

Hiện thực hoá *"ưu tiên hiệu năng cao nhưng không over-engineering"*. Cấu trúc là một **thang bậc**,
và luật là **bắt đầu ở bậc thấp nhất khả dĩ, lên bậc chỉ khi có lý do nêu được**.

| Bậc | Là gì | Lên bậc này khi | Giá phải trả |
|---|---|---|---|
| 0 | Code thẳng, kiểu dữ liệu chuẩn | mặc định | — |
| 1 | Chọn đúng cấu trúc dữ liệu cho pattern truy cập | truy cập không hợp kiểu chuẩn (tra cứu trong vòng lặp, duyệt tuần tự nhiều) | gần như bằng 0 — **đây là bậc bị bỏ qua oan nhất** |
| 2 | Tránh cấp phát trong vòng lặp nóng; tái dùng buffer | có cấp phát **đo được** mỗi frame | quản lý vòng đời |
| 3 | Bố trí dữ liệu cho cache; tách hot/cold | nhiều thực thể, duyệt mỗi frame, đã đo | dữ liệu khó đọc hơn |
| 4 | Job / song song hoá | công việc **độc lập theo phần tử**, số lượng lớn, đã đo là chi phối | chia dữ liệu, đồng bộ, debug khó hơn hẳn |
| 5 | Burst / mã nguyên gốc | vòng lặp số học chặt, đã ở bậc 4, đã đo | ràng buộc kiểu unmanaged, sai số nền tảng, rào cản người đọc |

**Bộ câu hỏi trước khi jobify** — trả lời được hết mới lên bậc 4:

1. Công việc có **độc lập theo phần tử** không? Nếu có phụ thuộc thứ tự thì job không giúp.
2. Số lượng có đủ lớn **ở tier thấp nhất** không? Vài chục phần tử thì chi phí lập lịch ăn hết lợi.
3. Dữ liệu đã **unmanaged** hoặc chuyển sang được mà không phải copy mỗi frame chưa?
4. Có **số đo** cho thấy đây là chỗ chi phối chưa? (`midcore-perf-budget`)
5. Ai sẽ debug nó lúc 2 giờ sáng, và họ có đọc được không?

Không trả lời được câu 4 thì dừng — chưa đo mà lên bậc 4–5 là định nghĩa của over-engineering.

**Chốt chặn hai đầu**, và cả hai đều phải nêu:

- **Đầu trên:** không lên bậc "cho chắc". Mỗi bậc phải nêu được lý do **và** cái giá đã chấp nhận.
- **Đầu dưới:** không dừng ở bậc 0 khi feature rõ ràng có mặt hiệu năng. Bậc 1 gần như miễn phí và
  bị bỏ qua nhiều nhất — chọn sai cấu trúc dữ liệu cho pattern truy cập là thứ **đắt để sửa sau**
  vì nó ngấm vào mọi call site, trong khi bậc 2–3 sửa sau vẫn được.

**Ranh giới với các skill khác** — phải ghi rõ, vì đây là chỗ dễ chồng nhất:

| Câu hỏi | Chủ |
|---|---|
| Dự án này có sẵn kiểu collection/toán học nào, dùng cái nào | authority `structure_naming` (profile) |
| Ngân sách frame là bao nhiêu, đo thế nào, bằng chứng nào đủ | `midcore-perf-budget` |
| **Feature này xứng đáng mức máy móc nào, ngay từ đầu** | **file này** |

Nói gọn: `structure_naming` là **danh mục**, `midcore-perf-budget` là **đo đạc sau**, file này là
**quyết định lúc thiết kế**.

### `reading-before-changing.md`

Quy trình vào một vùng code lạ, kết thúc bằng một artifact chứ không phải cảm giác:

1. Tìm **ranh giới** — cái gì gọi vào, cái gì gọi ra.
2. Tìm **chủ của state** — dựng bảng sở hữu cho vùng đó, kể cả khi tác giả gốc chưa từng viết.
3. Tìm **bất biến** — điều gì phải luôn đúng. Thường nằm ở tên biến, ở guard, ở comment.
4. Tìm **chỗ đã bị vá** — nơi có nhiều thay đổi nhỏ liên tiếp thường là nơi mô hình sai.
5. Chỉ sau đó mới đề xuất thay đổi, và nêu bất biến nào có thể bị đụng.

Luật: **không đề xuất thay đổi cho vùng code chưa dựng được bảng sở hữu.** Nối sang `debugging` khi
mục tiêu là tìm lỗi, và `refactoring` khi mục tiêu là đổi cấu trúc.

### `technical-debt.md`

Ma trận hai trục — **cố ý/vô tình** × **lãi cao/lãi thấp**:

| | Lãi cao (cản trở hàng ngày) | Lãi thấp (nằm im) |
|---|---|---|
| **Cố ý** | trả sớm; đã biết từ đầu nên rẻ nhất | ghi lại, sống chung, xem lại theo mốc |
| **Vô tình** | trả, nhưng **hiểu trước đã** — thường là mô hình sai chứ không phải code xấu | ghi lại; đừng đụng nếu không có lý do khác |

- **Nợ lãi cao là nợ chặn thay đổi**, không phải nợ trông xấu. Code xấu mà không ai sửa tới thì lãi
  bằng không.
- **Ghi nợ ra chỗ thấy được** — cạnh code, kèm cái giá và điều kiện trả. Nợ vô hình là nợ được trả
  bằng thời gian của người không biết mình đang trả.
- **Không trả nợ giữa lúc gấp.** Đó là lúc tệ nhất, và là lý do refactor mang tiếng xấu.
- Nối sang `refactoring` cho phần thao tác, và `midcore-perf-budget` khi "nợ" thật ra là một giả
  định hiệu năng chưa từng đo.

### `decision-records.md`

Mẫu ADR ngắn, **≤ một trang**:

```markdown
# ADR-0007 — <quyết định, viết ở thể khẳng định>

| | |
|---|---|
| Trạng thái | đề xuất / chốt / đã thay bởi ADR-00NN |
| Ngày | YYYY-MM-DD |
| Liên quan | feature doc, ADR khác |

## Bối cảnh
Điều gì buộc phải quyết. Sự thật, không phải ý kiến.

## Quyết định
Chọn gì. Một câu.

## Phương án đã loại
Mỗi phương án một dòng, kèm lý do loại.

## Hệ quả
Cái gì trở nên dễ hơn, cái gì trở nên khó hơn. **Cả hai chiều** — chỉ ghi mặt tốt là quảng cáo.
```

**Khi nào ADR, khi nào mục trong feature doc:**

| Tình huống | Ghi ở đâu |
|---|---|
| Quyết định thuộc về một feature đang có plan doc | mục Decisions của feature doc đó (`DEC-xxx`) |
| Quyết định phát sinh **giữa lúc code**, không có plan doc | **ADR** |
| Quyết định **cắt ngang nhiều feature** | **ADR**, ở cấp cao hơn |
| Quyết định **đảo một quyết định cũ** | **ADR mới**, đánh dấu ADR cũ là đã thay |

Luật: **ADR không bao giờ bị sửa, chỉ bị thay.** Lịch sử quyết định là thứ có giá trị; ghi đè nó
biến bản ghi thành ảnh chụp hiện tại, và ảnh chụp hiện tại thì đã có code rồi.

---

## 3. Yêu cầu xuyên suốt

1. **Portable** — không `EncosyTower`, `ApexionGame`, `Assets/Game`, `com.laicasaane`, `Unity`, hay
   đường dẫn tuyệt đối. Kể cả trong `performance-by-design.md`: nói "job / song song hoá", không nói
   tên API cụ thể.
2. **Không đụng `midcore-assembly-architecture`** — không dòng nào về asmdef, tầng assembly, phụ
   thuộc vòng mức assembly.
3. **Không thành catalogue pattern.** Skill dạy phán đoán. Một danh sách pattern khiến người đọc đi
   tìm chỗ áp dụng — ngược chiều với chống over-engineering.
4. **Tiếng Anh**, trigger tiếng Việt trong `description`.
5. **Độ dài** — `SKILL.md` 130–200 dòng; chiều sâu ở `references/`.
