# Architecture Skill — Quyết định

*[Tổng quan](Architecture Skill - Overview.md) · [Đặc tả skill](Architecture Skill - Skill Specs.md)*

Đánh số từ `DEC-201`. `chốt` = đã quyết, `open` = còn cần trả lời.

---

## DEC-201 — Một skill phủ cả cấu trúc lẫn phán đoán

**Trạng thái:** chốt (anh chọn ở vòng hỏi)

`system-design` sở hữu cả thiết kế dưới mức assembly (phân rã, sở hữu state, giao tiếp) lẫn phán
đoán kỹ sư (đánh đổi, phản biện, nợ kỹ thuật, mức hiệu năng).

**Vì sao không tách hai:** phán đoán mà không có chủ đề cụ thể để bám thì thành khẩu hiệu — "hãy cân
nhắc đánh đổi" không giúp được ai. Ngược lại, cấu trúc mà không có phán đoán thành danh sách luật,
và luật thì không biết khi nào mình không nên áp dụng. Hai nửa chỉ có giá trị khi đi cùng nhau, và
tách ra thì hai skill sẽ luôn phải nạp cùng lúc.

---

## DEC-202 — Đây là cổng thường trực, không phải tài liệu tra cứu

**Trạng thái:** chốt (theo tin nhắn của anh giữa lúc lập plan)

> *"mọi tính năng tôi yêu cầu làm cho game đều phải có architecture thật tốt chuẩn chỉ như 1 Senior
> Engineer thực thụ"*

Câu này đổi bản chất skill. Một skill tra cứu chỉ nạp khi ai đó nói "architecture"; một cổng thì nạp
trên **mọi** yêu cầu feature và **chặn** plan chưa đạt.

Hệ quả kéo theo, phải chấp nhận cả gói:

- `description` phải bắt "làm feature", "implement", "thêm hệ thống" — **trùng có chủ đích** với
  `encosy-tower`, vì cả hai đều phải nạp (xem DEC-203).
- `CLAUDE.md` Phase 2 phải đổi — nếu không thì cổng chỉ tồn tại trong skill và cổng plan-first vẫn
  cho qua một plan không có thiết kế.
- Mỗi feature phải trả thêm chi phí: 4–5 mục nữa trong plan doc. Đó là chi phí có chủ đích, không
  phải tác dụng phụ.

---

## DEC-203 — Trùng trigger với `encosy-tower` là cố ý

**Trạng thái:** chốt

Cả hai cùng bắt "làm feature X", "implement". Bình thường trùng trigger là lỗi thiết kế; ở đây là
yêu cầu.

| Skill | Trả lời |
|---|---|
| `encosy-tower` | Dùng **module nào** của thư viện, đặt file ở đâu, đặt tên thế nào |
| `system-design` | Feature này có **hình dạng gì** — chia làm gì, ai giữ state, các mảnh nói chuyện ra sao |

Hai câu hỏi khác nhau, và câu thứ hai đắt hơn nhiều để sửa sai: **chọn sai module thì đổi được;
chia sai state thì phải viết lại.**

Ranh giới ghi vào khối "See also" của **cả hai** skill, để không skill nào trả lời câu của skill kia.

---

## DEC-204 — Ba câu hỏi, không phải mười

**Trạng thái:** chốt

Cổng chỉ hỏi: **ai sở hữu state nào · các mảnh nói chuyện bằng gì · đã loại phương án nào**.

Cám dỗ là thêm câu: khả năng test, khả năng mở rộng, xử lý lỗi, logging, khả năng quan sát… Bác bỏ vì
**một cổng quá dài là một cổng bị điền cho có**. Ba câu này đã kéo theo phần lớn những thứ kia:

- sở hữu state rõ → gần như luôn test được;
- giao tiếp rõ → ranh giới rõ;
- nêu được phương án đã loại → đã thực sự cân nhắc.

Mục thứ tư (mức hiệu năng) là **có điều kiện** đúng vì lý do này — thêm nó vào mọi feature sẽ làm
cổng bị điền máy móc, và bản thân điều đó là over-engineering cái cổng.

---

## DEC-205 — ADR đặt ở đâu

**Trạng thái:** **open** — khuyến nghị bên dưới

Cần một chỗ cho quyết định phát sinh **giữa lúc code**, khi không có feature doc để ghi vào.

**Khuyến nghị:** `Documentation~/` của assembly sở hữu, đặt tên `ADR-NNNN - <tiêu đề>.md` — cùng chỗ,
cùng quy ước đặt tên với feature doc đang có. Quyết định **cắt ngang nhiều assembly** thì lên
`Documentation~/` ở gốc package.

**Vì sao không đặt trong `.claude/`:** quyết định kiến trúc thuộc về **dự án**, không thuộc về công
cụ. Người không dùng Claude cũng phải đọc được, và nó phải sống cùng code mà nó nói về.

**Phương án đã cân nhắc:** một thư mục `adr/` tập trung ở gốc repo. Bác bỏ — nó tách quyết định khỏi
code liên quan, và repo này đã có quy ước `Documentation~/` theo assembly chạy tốt.

**Cần từ anh:** xác nhận `Documentation~/ADR-NNNN - <tiêu đề>.md`, hay anh muốn chỗ khác.

---

## DEC-206 — Hiệu năng là quyết định thiết kế, gộp vào skill này

**Trạng thái:** chốt (theo tin nhắn của anh giữa lúc lập plan)

> *"ưu tiên đánh giá và sử dụng những cái mang lại hiệu năng cao, nhưng không bị overEngineering"*

Đặt vào `system-design` thay vì mở skill thứ tư, vì đây **là** một bài toán đánh đổi ở thời điểm
thiết kế — cùng hoạt động với phần còn lại của skill.

Phân vai ba bên, ghi vào `performance-by-design.md`:

| Câu hỏi | Chủ |
|---|---|
| Dự án có sẵn kiểu collection/toán học nào | authority `structure_naming` — **danh mục** |
| Ngân sách bao nhiêu, đo thế nào, bằng chứng nào đủ | `midcore-perf-budget` — **đo đạc sau** |
| Feature này xứng đáng mức máy móc nào | `system-design` — **quyết định lúc thiết kế** |

Chỗ trống có thật: hôm nay **không skill nào** trả lời được *"có nên dùng Burst + Job cho tính năng
này không"* **trước khi** có code. `midcore-perf-budget` nói "đo rồi mới kết luận", mà chưa có code
thì chưa đo được — nên câu hỏi rơi vào khoảng không.

**Phương án bị bác:** một skill `performance-decisions` riêng. Nó sẽ luôn phải nạp cùng
`system-design` (vì mức hiệu năng là một phần của thiết kế feature), và một skill luôn đi kèm skill
khác nên là một reference của skill đó.

---

## DEC-207 — Sửa `CLAUDE.md` tới mức nào

**Trạng thái:** **open** — khuyến nghị bên dưới

Cổng chỉ có hiệu lực nếu cổng plan-first trong `CLAUDE.md` yêu cầu mục Design. Skill tự nó không
chặn được gì nếu Phase 2 vẫn cho qua plan không có thiết kế.

**Khuyến nghị — sửa tối thiểu:** thêm một gạch đầu dòng vào Phase 2 liệt kê 4–5 mục bắt buộc, và một
dòng ở "Where things live" trỏ `system-design`. Không viết lại Phase 2.

**Phương án đã cân nhắc:** viết lại hẳn Phase 2 quanh ba câu hỏi. Bác bỏ — `CLAUDE.md` nạp mỗi phiên
nên mỗi dòng thêm vào là chi phí thường trực, và chi tiết thuộc về skill chứ không thuộc về file
luôn được nạp.

**Cần từ anh:** xác nhận được sửa `CLAUDE.md`. Nó là file điều phối toàn bộ, nên tôi không tự đổi
hành vi cổng của nó mà không hỏi.

---

## DEC-208 — Không làm catalogue design pattern

**Trạng thái:** chốt

Skill dạy **phán đoán**, không liệt kê pattern.

**Vì sao:** một danh sách pattern khiến người đọc đi tìm chỗ áp dụng — chính xác ngược chiều với yêu
cầu chống over-engineering của anh. Pattern là **hệ quả** của một quyết định thiết kế đúng, không
phải đầu vào của nó.

Khi một pattern là câu trả lời đúng, skill sẽ tới được nó qua ba câu hỏi (sở hữu state, giao tiếp,
phương án đã loại) mà không cần gọi tên nó. Và nếu có gọi tên thì đó là để giao tiếp cho gọn, không
phải để biện minh cho lựa chọn.

---

## DEC-209 — Tên `system-design`, không phải `architecture`

**Trạng thái:** chốt

`midcore-assembly-architecture` đã chiếm từ "architecture" ở mức assembly. Một skill tên
`architecture` sẽ khiến hai skill nghe như cùng phạm vi, và lúc chọn skill sẽ nhập nhằng.

`system-design` mô tả đúng việc nó làm: thiết kế một hệ thống **bên trong** một ranh giới đã có.
Không mang tiền tố `midcore-` vì nó áp ở mọi quy mô (DEC-106 của bộ trước, cùng lý do).
