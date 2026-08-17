# Coding Skills — Quyết định

*[Tổng quan](Coding Skills - Overview.md) · [Đặc tả skill](Coding Skills - Skill Specs.md)*

Đánh số từ `DEC-101` để không đụng `DEC-0xx` của bộ `midcore-*`.
`chốt` = đã quyết, `open` = còn cần trả lời.

---

## DEC-101 — `coding-standards` chỉ sở hữu phần vô chủ; `CODING-CONVENTIONS.md` vẫn là gốc

**Trạng thái:** chốt (anh chọn ở vòng hỏi)

Skill viết ~558 dòng thuộc 7 mục chưa ai sở hữu (§2, §4, §5, §8, §9, §11, §12 + checklist §14). Bảy
mục còn lại **chỉ được trỏ**, không viết lại một dòng.

**Vì sao không nuốt cả 1063 dòng:** ba reference của `encosy-tower` đang trích thẳng §1, §3, §6, §7,
§10, §13. Nuốt hết nghĩa là hoặc nhân đôi chúng, hoặc phải mổ lại ba file đang chạy tốt. Và
`CODING-CONVENTIONS.md` là tài liệu người ngoài Claude cũng đọc — xoá nó là mất một artifact của repo
để đổi lấy sự tiện lợi cho một công cụ.

**Cái phải chấp nhận:** hai chỗ để nhìn. Bảng phân quyền 14 mục ở đầu `SKILL.md` chính là thứ trả
giá cho điều đó — nó biến "đọc 1063 dòng" thành "tra một dòng".

---

## DEC-102 — Tách `refactoring` và `debugging` thành hai skill

**Trạng thái:** chốt (anh chọn ở vòng hỏi)

Hai hoạt động khác nhau, kích hoạt bởi prompt khác hẳn nhau: *"tách hàm này ra"* và *"sao nó crash"*
không có từ nào chung. Gộp một skill thì `description` phải phủ cả hai miền, và một description ôm
quá rộng là description bắt sai.

Craft ở mức hàm/lớp (đặt tên, độ dài, khi nào trừu tượng hoá) đặt trong `refactoring` chứ không thành
skill thứ ba — nó gần như luôn xuất hiện dưới dạng "sửa lại cho gọn", và một `code-craft` riêng sẽ
tranh prompt với `simplify` có sẵn.

---

## DEC-103 — `refactoring` và `debugging` viết trung lập ngôn ngữ

**Trạng thái:** chốt (anh chọn ở vòng hỏi)

Phương pháp refactor và debug gần như không phụ thuộc ngôn ngữ. Viết trung lập + đọc
`.claude/project-profile.md` như bộ `midcore-*`, nên áp sang dự án sau chỉ là copy thư mục.

`coding-standards` thì ngược lại — nó **project-level** một cách có chủ đích, vì nó tồn tại để sở hữu
một tài liệu cụ thể của repo này. Không có gì để portable.

---

## DEC-104 — `debugging` định tuyến triệu chứng Unity, không viết lại

**Trạng thái:** chốt (anh chọn ở vòng hỏi)

Những thứ chỉ hỏng trong Unity — generated member không tồn tại, guard âm thầm false, code bị strip,
chỉ crash trên máy thật — **đã được mô tả** ở `unity-cli-workflow`,
`midcore-assembly-architecture/feature-flags.md`, `midcore-release-pipeline/versioning-and-symbols.md`.

`debugging` chứa một **bảng định tuyến** trỏ tới chúng. Viết lại lần hai nghĩa là hai bản sẽ trôi
khỏi nhau, và bản trong `debugging` sẽ là bản sai — vì nó xa nguồn hơn.

Đây cũng là điều làm `debugging` portable được: bảng định tuyến thay đổi theo repo, phương pháp thì
không.

---

## DEC-105 — Số phận mục "Coding conventions" trong `encosy-tower/references/setup.md`

**Trạng thái:** **open** — khuyến nghị bên dưới

`setup.md` đang chứa một digest ~30 dòng của `CODING-CONVENTIONS.md`. Sau khi có `coding-standards`,
nó thành bản sao thứ ba của cùng một luật, và là bản kém nhất (bỏ 12/14 mục).

**Khuyến nghị:** xoá mục đó, thay bằng ba dòng trỏ sang `coding-standards` và
`CODING-CONVENTIONS.md`. `setup.md` vốn là "module nào live trong dự án này" — luật style chưa bao
giờ thuộc về nó.

**Cần từ anh:** xác nhận được xoá. Nó đang là chỗ nhiều phiên quen đọc, nên xoá mà không nói là một
kiểu làm hụt.

Cùng lúc: `planning-workflow.md:24` đang viết *"There is no dedicated dotnet/C# skill"* — câu đó sẽ
sai ngay khi `coding-standards` tồn tại, và phải sửa trong cùng thay đổi.

---

## DEC-106 — Tên trần, không tiền tố

**Trạng thái:** chốt

`coding-standards`, `refactoring`, `debugging` — không mang tiền tố `midcore-`.

Bộ `midcore-*` mang tiền tố vì chúng là **một họ** được hiệu chỉnh cho một tầng scope cụ thể
(DEC-002). Ba skill này áp ở **mọi quy mô** — một prototype cũng cần refactor an toàn và debug có
phương pháp. Gắn `midcore-` vào sẽ nói sai về phạm vi áp dụng.

Đã kiểm không đụng built-in: harness có `code-review`, `simplify`, `security-review` — không trùng
tên nào.

---

## DEC-107 — Nhường `code-review` và `simplify` cho built-in

**Trạng thái:** chốt

Harness đã có sẵn `code-review`, `simplify`, `security-review`. Viết bản riêng là nhân đôi công cụ,
và hai bản sẽ tranh nhau lúc chọn skill.

Ranh giới ghi vào cả ba skill mới:

| | Built-in | Skill mới |
|---|---|---|
| Rà diff tìm chỗ gọn hơn | `simplify` | — |
| Tìm lỗi **chưa** biểu hiện | `code-review` | — |
| **Phương pháp** đổi cấu trúc an toàn | — | `refactoring` |
| Truy nguyên một lỗi **đã** biểu hiện | — | `debugging` |

Nói gọn: built-in là **một lượt rà**; skill mới là **phương pháp làm việc**.

---

## DEC-108 — Không làm kiểm tra style tự động

**Trạng thái:** chốt

Analyzer, `.editorconfig`, format-on-commit sẽ cưỡng chế §2 tốt hơn mọi skill — nhưng đó là việc
tooling, không phải skill, và không ai yêu cầu.

Nếu sau này làm, `coding-standards` là nơi ghi lại **luật nào đã được cưỡng chế bằng máy** để skill
thôi nhắc lại chúng. Đó là kết cục tốt: skill co lại còn phần máy không kiểm được — thiết kế API,
comment nói vì sao, chọn attribute.

**Xem lại khi:** có người dựng CI cho repo (`release.ci` trong profile hiện là `unknown — ask`).
