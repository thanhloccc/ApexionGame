# Architecture Skill — Tổng quan

*[Đặc tả skill](Architecture Skill - Skill Specs.md) · [Quyết định](Architecture Skill - Decisions.md)*

## Trạng thái

| | |
|---|---|
| Giai đoạn | **Đã triển khai** — 2026-08-15 |
| Sản phẩm | 1 skill `system-design` (SKILL.md + 8 reference) + 1 loại artifact mới (ADR) |
| Vị trí | `.claude/skills/system-design/` — portable, đọc `project-profile.md` |
| Bản chất | **Cổng chất lượng thường trực**, không phải tài liệu tra cứu |
| Skill hiện có bị đụng | `encosy-tower` (cổng plan-first), `CLAUDE.md` (Phase 2), `midcore-assembly-architecture` (trỏ chéo) |
| Quyết định còn mở | DEC-205 (ADR đặt ở đâu), DEC-207 (sửa CLAUDE.md tới mức nào) |

---

## 1. Tóm tắt yêu cầu

Anh yêu cầu thêm một plan về architecture "như một Senior Engineer". Hai tin nhắn tiếp theo mới là
phần quan trọng nhất, và chúng đổi bản chất của thứ cần xây:

> *"game midcore cũng cần phải có 1 architecture thật tốt ngay từ giai đoạn start dự án"*
>
> *"mọi tính năng tôi yêu cầu làm cho game đều phải có architecture thật tốt chuẩn chỉ như 1 Senior
> Engineer thực thụ"*
>
> *"ưu tiên đánh giá và sử dụng những cái mang lại hiệu năng cao, nhưng không bị overEngineering —
> như việc chỗ này có thể dùng collection nào tối ưu nhất, có nên dùng Burst + Job cho tính năng
> này không"*

Đọc ra ba điều:

1. **Đây không phải skill tra cứu khi cần.** Nó là **cổng thường trực** áp lên **mọi** feature —
   nằm bên trong cổng plan-first đã có, không phải nằm cạnh.
2. **"Ngay từ giai đoạn start" nghĩa là bây giờ.** `Assets/Game` đang rỗng. Feature đầu tiên định
   hình mọi feature sau — đây là thời điểm rẻ nhất và cũng là thời điểm duy nhất.
3. **Hiệu năng là quyết định thiết kế, có chốt chặn hai đầu.** Không phải "chọn thứ nhanh nhất" mà
   là **chọn mức máy móc tương xứng với nhu cầu thật** — đủ nhanh mà không over-engineering. Câu
   *"có nên dùng Burst + Job cho tính năng này không"* phải trả lời được **trước khi** có code, vì
   jobify một hệ đã viết xong đắt hơn nhiều lần thiết kế nó từ đầu.

Vòng hỏi chốt: một skill duy nhất phủ **cả cấu trúc lẫn phán đoán**, thêm **ADR nhẹ** cho quyết định
lẻ, làm **cả bốn** hành vi Senior (phản biện, đánh đổi có tên, đọc code lạ trước khi sửa, nợ kỹ
thuật), viết **trung lập** đọc profile.

### Chỗ trống được xác nhận bằng số

`midcore-assembly-architecture` tự kết thúc bằng *"This skill stops at the assembly boundary and
hands off"* — nhưng bảng "không sở hữu" của nó chỉ liệt kê folder/naming, chọn library, compile,
bảng dữ liệu. **Không ai nhận phần bàn giao.**

Đếm trên 9 skill hiện có:

| Khái niệm | Số file nhắc tới |
|---|---|
| `cohesion` | **0** |
| `abstraction` | **0** |
| `tradeoff` / `trade-off` | **0** |
| `YAGNI` | **0** |
| `design review` | **0** |
| `state ownership` | **0** |
| `technical debt` | **0** |
| `coupling` | 2 (đều ở mức assembly) |

Toàn bộ khoảng giữa *"assembly nào"* và *"gõ dòng đó ra sao"* là vô chủ. Đó chính là chỗ kiến trúc
một feature được quyết định.

## 2. Kết quả mong đợi

### 2.1 Trên đĩa sẽ có gì khi xong

```
.claude/skills/system-design/
├── SKILL.md                          ba câu hỏi bắt buộc + cổng feature + bảng nhường quyền
└── references/
    ├── decomposition.md              chia feature thành các mảnh cộng tác; cohesion/coupling mức type
    ├── state-ownership.md            ai sở hữu state nào — quyết định đắt nhất, làm trước tiên
    ├── communication.md              gọi / event / query / state chung — chọn cái nào, giá bao nhiêu
    ├── tradeoffs-and-pushback.md     gọi tên trục đánh đổi; phản biện rồi vẫn làm theo quyết định
    ├── performance-by-design.md      chọn mức máy móc tương xứng — collection, Burst/Job, layout
    ├── reading-before-changing.md    hiểu code lạ trước khi động vào
    ├── technical-debt.md             phân loại nợ, khi nào trả, khi nào sống chung
    └── decision-records.md           mẫu ADR + khi nào viết ADR thay vì mục trong feature doc

.claude/
├── CLAUDE.md                         Phase 2 thêm mục Design bắt buộc
└── memory/architecture-is-a-gate.md  MỚI

<assembly>/Documentation~/
└── ADR-0001 - <tiêu đề>.md           loại artifact mới (DEC-205)
```

**9 file skill + 2 chỉnh sửa + 1 loại artifact.** Không có code sản phẩm.

### 2.2 Luật cốt lõi

> **Mọi thiết kế trả lời được ba câu trước khi có dòng code nào:**
> **ai sở hữu state nào · các mảnh nói chuyện bằng gì · anh đã loại phương án nào và vì sao.**
>
> Thiết kế không trả lời được ba câu đó không phải thiết kế — nó là một ý định.

Ba câu này chọn có chủ đích, không phải cho đủ bộ:

- **Sở hữu state** là quyết định đắt nhất để đảo. Nó định đoạt: có test được không, có save được
  không, hai hệ có thể bất đồng về sự thật không.
- **Cách nói chuyện** định đoạt code có đọc được không sau sáu tháng, và ranh giới có giữ được không.
- **Phương án đã loại** là thứ phân biệt một quyết định với một mặc định. Không nêu được cái đã loại
  nghĩa là chưa từng cân nhắc.

### 2.3 Cổng feature — điểm khác biệt so với mọi skill hiện có

Đây là chỗ hiện thực hoá *"mọi tính năng đều phải có architecture chuẩn chỉ"*.

Cổng plan-first hiện tại (CLAUDE.md Phase 0–3) sinh ra một plan doc có Request summary → Expected
output → Steps, cộng Encosy mapping, data model, layout, API surface, decisions. **Không có mục nào
bắt buộc nói về thiết kế.** Một plan hôm nay có thể liệt kê đủ file cần tạo mà chưa từng nói ai sở
hữu state nào.

`system-design` chèn vào Phase 2 một **mục Design bắt buộc**, và plan chưa có nó thì chưa đưa ra
review:

| Mục bắt buộc | Nội dung | Không đạt khi |
|---|---|---|
| **Phân rã** | Feature gồm những mảnh nào, vì sao chia thế | Chỉ là danh sách file |
| **Sở hữu state** | Bảng: mỗi mẩu state → chủ → ai được ghi | Có state không ai nhận, hoặc hai chủ |
| **Giao tiếp** | Các mảnh nói chuyện bằng gì, vì sao cơ chế đó | "dùng event" mà không nói vì sao không gọi thẳng |
| **Phương án đã loại** | Ít nhất một, kèm lý do loại | Trống, hoặc loại một phương án bù nhìn |
| **Mức hiệu năng** *(có điều kiện)* | Bậc máy móc chọn cho feature này + vì sao không cao hơn **và** không thấp hơn | Chọn bậc cao "cho chắc", hoặc bỏ trống khi feature rõ ràng có mặt hiệu năng |

Mục thứ năm **chỉ bắt buộc khi feature có mặt hiệu năng** — chạy mỗi frame, nhiều thực thể, dữ liệu
lớn, hoặc đụng vòng lặp nóng đã có. Feature một màn hình cài đặt thì không. Bản thân cái cổng cũng
không được over-engineering.

### 2.4 Bốn hành vi Senior, cụ thể hoá

| Hành vi | Nghĩa là gì trong skill |
|---|---|
| **Phản biện** | Khi yêu cầu dẫn tới thiết kế tệ: nêu **cái giá cụ thể**, đề xuất phương án thay thế, **rồi vẫn làm theo nếu anh giữ nguyên** — và ghi lại là đã cảnh báo. Không phải từ chối, không phải im lặng làm. |
| **Đánh đổi có tên** | Gọi tên trục cụ thể (tốc độ giao vs chi phí thay đổi sau; đơn giản vs tổng quát), **chọn một bên**, giải thích. Cấm "còn tùy" mà không kèm khuyến nghị. |
| **Đọc trước khi sửa** | Vào vùng code lạ: tìm ranh giới, tìm chủ của state, dựng bản đồ, nêu bất biến — **trước** khi đề xuất thay đổi. |
| **Nợ kỹ thuật** | Phân loại (cố ý vs vô tình, lãi cao vs thấp), nói khi nào trả và khi nào sống chung, ghi lại để nợ không thành vô hình. |

### 2.5 Làm sao biết là chạy được

1. **Cổng có hiệu lực** — yêu cầu một feature giả định có mặt hiệu năng, plan sinh ra **phải** có
   đủ 5 mục ở §2.3. Thiếu một mục là cổng không hoạt động.
2. **Bảng sở hữu state kín** — trong plan thử nghiệm, không mẩu state nào vô chủ hoặc hai chủ.
3. **Phản biện thật sự xảy ra** — đưa một yêu cầu có thiết kế cố tình tệ (ví dụ: "cho UI đọc thẳng
   và ghi vào save"), skill phải nêu cái giá và đề xuất thay thế, rồi vẫn làm nếu bị giữ nguyên.
4. **Tính portable** — `grep -rE "EncosyTower|ApexionGame|Assets/Game|com\.laicasaane|Unity"` trên
   `system-design/` ra **không kết quả**.
5. **Không đụng `midcore-assembly-architecture`** — không dòng nào nói về asmdef, tầng assembly, hay
   phụ thuộc vòng ở mức assembly.
6. **Chốt chặn over-engineering hoạt động** — đưa một feature tầm thường (một màn hình cài đặt, một
   danh sách 20 phần tử dựng lúc khởi động), skill phải **không** đề xuất Burst/Job, **không** đề
   xuất collection chuyên dụng, và nói thẳng vì sao bậc thấp nhất là đúng. Đây là nửa dễ bị bỏ quên
   của yêu cầu, và là phép kiểm khó đạt hơn phép kiểm ngược lại.

## 3. Các bước

| # | Hành động | Xong khi | File |
|---|---|---|---|
| 1 | Viết `SKILL.md` — ba câu hỏi, cổng feature 4 mục, bảng nhường quyền | Một phiên mới đọc xong biết chính xác plan phải chứa gì mới được đưa review | `system-design/SKILL.md` |
| 2 | `state-ownership.md` — reference quan trọng nhất, viết trước | Có mẫu bảng sở hữu, luật một-chủ, phân biệt state gốc vs suy ra, vòng đời | `…/references/state-ownership.md` |
| 3 | `decomposition.md` | Nêu được seam nào đáng cắt trong một game, dấu hiệu một mảnh làm quá nhiều | `…/references/decomposition.md` |
| 4 | `communication.md` | Bảng chọn cơ chế + giá của từng cơ chế; luật "trực tiếp nhất mà không tạo phụ thuộc ngược" | `…/references/communication.md` |
| 5 | `tradeoffs-and-pushback.md` | Có kịch bản phản biện mẫu, và luật vẫn-làm-theo-quyết-định | `…/references/tradeoffs-and-pushback.md` |
| 6 | `performance-by-design.md` — **thang bậc máy móc + chốt chặn hai đầu** | Có thang bậc từ "code thẳng" tới "Burst + Job"; mỗi bậc nêu điều kiện lên bậc **và** cái giá; có bộ câu hỏi quyết định jobify trước khi viết code | `…/references/performance-by-design.md` |
| 7 | `reading-before-changing.md` | Quy trình dựng bản đồ vùng code lạ, kết thúc bằng danh sách bất biến | `…/references/reading-before-changing.md` |
| 8 | `technical-debt.md` | Ma trận phân loại + luật ghi nợ ra chỗ thấy được | `…/references/technical-debt.md` |
| 9 | `decision-records.md` — mẫu ADR + khi nào ADR vs mục trong feature doc | Mẫu ngắn (≤1 trang), đánh số, có trạng thái | `…/references/decision-records.md` |
| 10 | Sửa `CLAUDE.md` Phase 2 — thêm mục Design bắt buộc | Cổng plan-first yêu cầu 4 mục ở §2.3 | `.claude/CLAUDE.md` |
| 11 | Trỏ chéo hai chiều với `encosy-tower`, `midcore-assembly-architecture`, `midcore-save-migration` | Mỗi bên có khối "See also"; không luật nào bị dời | `.claude/skills/*/SKILL.md` |
| 12 | Memory + dòng index | Một file + một dòng | `.claude/memory/architecture-is-a-gate.md`, `MEMORY.md` |
| 13 | Chạy 6 phép kiểm §2.5 | Cả sáu đạt | — |

Bước 2 làm trước bước 3–4 vì hai file kia đều tham chiếu bảng sở hữu state. Bước 10 phụ thuộc bước 1.

## 4. Mục tiêu và không phải mục tiêu

**Mục tiêu**

- Mọi feature từ nay có kiến trúc được **nghĩ và ghi lại**, không phải phát sinh từ thứ tự gõ file.
- Chỗ trống dưới mức assembly có chủ.
- Bốn hành vi Senior thành hành vi mặc định, không phải thứ phải nhắc mỗi lần.
- Quyết định lẻ có chỗ ghi (ADR), thay vì bay mất khi phiên kết thúc.

**Không phải mục tiêu**

- **Đồ thị assembly, tầng, phụ thuộc vòng** — `midcore-assembly-architecture` đã sở hữu.
- **Đổi cấu trúc code đã có** — `refactoring` (đang chờ review ở plan trước).
- **Rà diff, tìm lỗi chưa biểu hiện** — `code-review`, `simplify` built-in.
- **Catalogue design pattern.** Skill này dạy phán đoán, không liệt kê pattern — một danh sách
  pattern làm người đọc đi tìm chỗ áp dụng, đó là ngược chiều.
- **Kiến trúc backend / server.** Game single-player; live-ops đã có skill riêng.

## 5. Quan hệ với các skill hiện có

Kiến trúc giờ có ba tầng, mỗi tầng một chủ:

| Tầng | Câu hỏi | Chủ |
|---|---|---|
| **Trên** | Assembly nào, tầng nào, được trỏ hướng nào | `midcore-assembly-architecture` |
| **Giữa** | **Feature này chia thành gì, ai giữ state, các mảnh nói chuyện ra sao** | **`system-design`** |
| **Dưới** | Type/API viết ra sao, format, attribute | `coding-standards` (chờ review) |

Và ranh giới dễ nhầm:

- **`system-design` vs `encosy-tower`** — cả hai đều nạp khi có yêu cầu feature. `encosy-tower` trả
  lời *dùng module nào của thư viện*; `system-design` trả lời *feature này có hình dạng gì*. Chọn sai
  module là sửa được; chia sai state thì không.
- **`system-design` vs `midcore-assembly-architecture`** — assembly là ranh giới **vật lý**,
  `system-design` lo **bên trong** một ranh giới. Khi thiết kế cho thấy cần một assembly mới, đó là
  lúc bàn giao ngược lên.
- **`system-design` vs `refactoring`** — thiết kế **trước khi** code tồn tại; refactor **sau khi** nó
  tồn tại. Cùng vốn từ, khác thời điểm.

Đặc tả đầy đủ: [Architecture Skill - Skill Specs](Architecture Skill - Skill Specs.md).
