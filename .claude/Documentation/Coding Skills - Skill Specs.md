# Coding Skills — Đặc tả skill

*[Tổng quan](Coding Skills - Overview.md) · [Quyết định](Coding Skills - Decisions.md)*

Mỗi skill một mục: `description` frontmatter cần viết, các file reference, nội dung bắt buộc, và
**skill đó không được trùng lặp cái gì**.

Toàn bộ file skill viết **tiếng Anh** (DEC-008 của bộ trước, áp tiếp ở đây), `description` giữ cụm
trigger tiếng Việt.

---

## 1. `coding-standards` — project-level

Bám EncosyTower convention nên **không portable** — nó tồn tại để sở hữu một tài liệu cụ thể của
repo này.

> **description** — REQUIRED when writing or reviewing the C# itself: how a line is formatted, how a
> type or public API is shaped, which attributes a member carries, how a comment is written, and how
> async and conditional compilation are expressed. Owns the sections of the repo's coding-conventions
> document that no other skill covers, and routes the rest to whichever skill does. Load it when
> writing new code, when unsure whether something matches house style, and before hand-shaping a
> public API. Triggers on "style", "convention", "format", "formatting", "brace", "indent", "line
> length", "comment", "XML doc", "attribute", "API design", "public API", "signature", "async
> method", "conditional compilation", "coding standard", and Vietnamese phrasing "đúng style chưa",
> "quy ước code", "định dạng", "viết comment", "thiết kế API", "đặt attribute".

### Bảng phân quyền — sản phẩm quan trọng nhất của skill này

Nằm ngay đầu `SKILL.md`. Nó giải quyết đúng vấn đề hiện tại: muốn tra một mục thì phải đọc cả 1063
dòng.

| Mục `CODING-CONVENTIONS.md` | Đọc ở đâu |
|---|---|
| §1 Naming | `encosy-tower` → `references/structure-and-naming.md` §6 |
| **§2 Formatting** | **`references/formatting.md`** |
| §3 Usings & Namespaces | `encosy-tower` → `structure-and-naming.md` §7 |
| **§4 Everyday Code Style** | **`references/formatting.md`** |
| **§5 Comments** | **`references/comments-and-async.md`** |
| §6 Files & Layout | `encosy-tower` → `structure-and-naming.md` §5 |
| §7 Member Ordering | `encosy-tower` → `structure-and-naming.md` §8 |
| **§8 Type & API Design** | **`references/api-design.md`** |
| **§9 Members & Attributes** | **`references/members-and-attributes.md`** |
| §10 Errors & Validation | `encosy-tower` → `references/structured-errors.md` |
| **§11 Performance Annotations** | **`references/members-and-attributes.md`** (chú thích); ngân sách và đo đạc → `midcore-perf-budget` |
| **§12 Async, Logging & Conditional** | **`references/comments-and-async.md`** |
| §13 Unsafe & Native Containers | `encosy-tower` → `references/collections-and-math.md` |
| **§14 Quick Checklist** | **`SKILL.md`** (ngay trong skill, để dùng trước khi bàn giao) |

Bảy dòng in đậm là phạm vi sở hữu. Bảy dòng còn lại **chỉ trỏ, không viết lại một dòng nào**.

### Reference

| File | Gốc | Nội dung |
|---|---|---|
| `formatting.md` | §2 (157) + §4 (66) | 4 space, LF, ≤100 cột (120 cứng). Dấu ngoặc nhọn xuống dòng riêng; **braces trên mọi khối điều khiển kể cả `if` một câu lệnh**. Dòng trống trên/dưới mỗi câu lệnh mở scope, trừ khi các khối chạm nhau có chủ đích (`if`/`else`, `try`/`catch`) hoặc khối mở/đóng scope cha. Xuống dòng kiểu **dấu phẩy đứng đầu** cho signature và lời gọi nhiều tham số. Không căn cột. Nhóm field `const` → `static readonly` → `static` → instance. Cộng thói quen hằng ngày ở §4. |
| `api-design.md` | §8 (113) | Thiết kế type mà người khác dùng: chọn `readonly struct` / `class` / `record`, bề mặt public tối thiểu, tham số `in`/`ref`/`out`, trả `Option<T>`/`Result<T,TError>` thay vì `null` hay `bool`+`out`, quy ước tiền tố `Try*` / `*OrError` / `*OrThrow` / `*OrDefault`, và **type nào bắt buộc `partial`** vì source generator. |
| `members-and-attributes.md` | §9 (111) + §11 (54) | Attribute nào đặt ở đâu và vì sao: `[MethodImpl(AggressiveInlining)]` cho hot path, `[MethodImpl(NoInlining)]` + `[HideInCallstack, StackTraceHidden]` cho helper ném/log lạnh, `[Conditional(...)]` với `ValidationDefines`, `[Preserve]`, `[Serializable]`, field serialize. Kèm luật: chú thích hiệu năng là **quy ước**, còn có đáng tối ưu hay không thì `midcore-perf-budget` quyết. |
| `comments-and-async.md` | §5 (22) + §12 (43) | Comment **nói vì sao, không nói cái gì** — cùng luật đang áp cho feature doc. XML doc ở đâu là bắt buộc. Async: hậu tố `Async`, mẫu alias `UnityTask` (chi tiết ở `encosy-tower/references/recipes.md`, ở đây chỉ nêu luật), `CancellationToken` truyền tới đâu, logging bị strip theo define, `#if` đặt ở đâu cho khỏi vỡ khi đọc. |

### Không được trùng lặp

Đặt bảng "không sở hữu" ngay trong `SKILL.md`:

| Câu hỏi | Chủ |
|---|---|
| Đặt tên type/member/file/folder/namespace | `encosy-tower` → `structure-and-naming.md` |
| Thứ tự member trong một type | `encosy-tower` → `structure-and-naming.md` §8 |
| Mô hình hoá error, `Result<T,TError>` | `encosy-tower` → `structured-errors.md` |
| Chọn collection / kiểu toán | `encosy-tower` → `collections-and-math.md` |
| Có đáng tối ưu không, đo thế nào | `midcore-perf-budget` |
| Rà diff tìm chỗ gọn hơn | `simplify` (built-in) |

**Xong khi:** một phiên mới hỏi "§9 đọc ở đâu" trả lời được ngay từ bảng, và viết một API public mới
mà không mở `CODING-CONVENTIONS.md`.

---

## 2. `refactoring` — portable

> **description** — REQUIRED before changing the structure of code that already works — extracting,
> inlining, renaming, moving, splitting, merging, changing a signature, or replacing an
> implementation. Covers the operation order that keeps the tree compiling at every step, keeping a
> refactor from growing, proving behaviour was preserved, and recognising the refactors that are not
> worth doing. The rule underneath everything: one change alters structure OR behaviour, never both,
> because that is what makes verification possible. Load it before touching working code for
> structural reasons, and when a refactor has started growing beyond its original scope. Triggers on
> "refactor", "restructure", "extract", "inline", "rename", "move", "split", "merge", "clean up",
> "reorganize", "change signature", "replace implementation", "technical debt", and Vietnamese
> phrasing "tách hàm", "tách class", "gộp lại", "dọn code", "đổi cấu trúc", "sửa lại cho gọn",
> "nợ kỹ thuật".

### Luật cốt lõi

> **Một thay đổi đổi *cấu trúc* HOẶC đổi *hành vi*. Không bao giờ cả hai.**

Đây là thứ khiến kiểm chứng khả thi: thay đổi cấu trúc phải chứng minh được là bảo toàn hành vi, nên
test không được đổi. Thay đổi hành vi thì test đổi, nhưng cấu trúc đứng yên nên diff đọc được.

Trộn hai thứ vào một thay đổi là lý do "refactor" mang tiếng xấu: không ai review được, không ai
bisect được, và khi có lỗi thì không biết do bên nào.

### Reference

| File | Nội dung |
|---|---|
| `safe-sequences.md` | Một công thức cho mỗi loại: rename, extract method/type, inline, move giữa type, đổi signature, tách một type thành hai, gộp hai thành một, thay implementation sau một interface. Mỗi cái nêu **thứ tự thao tác giữ cho cây luôn compile được** và **cái bẫy** riêng của nó. Luật xuyên suốt: thêm cái mới → chuyển consumer sang → xoá cái cũ, ba bước, không phải một. |
| `scope-control.md` | Cái bẫy "tiện tay sửa luôn": refactor phình ra tới mức không review nổi rồi bị bỏ dở. Cách chốt phạm vi trước khi bắt đầu, ghi lại thứ phát hiện dọc đường **mà không sửa nó**, và dấu hiệu phải dừng. Kèm luật: refactor chạm ranh giới assembly thì trước hết là bài toán của `midcore-assembly-architecture`. |
| `verification.md` | Chứng minh hành vi không đổi: characterisation test viết **trước** khi refactor cho code chưa có test, golden run, so sánh output, review diff theo lớp. Luật: **nếu test phải sửa thì đó không còn là refactor** — hoặc là đổi hành vi, hoặc test đang bám vào cấu trúc nội bộ (mà bản thân nó là một phát hiện). |
| `when-not-to.md` | Refactor không đáng làm: code sắp bị xoá, code không ai đọc và không ai sửa, "cho đẹp" mà không giảm chi phí thay đổi nào, refactor đuổi theo một trừu tượng chưa có bằng chứng. Kèm cái bẫy ngược: **hoãn refactor cho tới khi buộc phải làm giữa lúc gấp** — lúc tệ nhất để làm. |

### Không được trùng lặp

| Câu hỏi | Chủ |
|---|---|
| Rà diff hiện tại tìm chỗ gọn hơn | `simplify` (built-in) — đó là một **lượt rà**, đây là **phương pháp** |
| Đổi cấu trúc có vượt ranh giới assembly | `midcore-assembly-architecture` |
| Refactor chạm type được persist | `midcore-save-migration` — không còn là refactor, là migration |
| Refactor vì hiệu năng | `midcore-perf-budget` — phải có số before/after |
| Test nào là đủ | `midcore-testing` |
| Đặt tên và bố cục file sau khi tách | authority `structure_naming` trong profile |

**Xong khi:** một phiên được yêu cầu "tách class này" sẽ tách làm hai thay đổi, nói rõ cái nào bảo
toàn hành vi, và nêu cách chứng minh.

---

## 3. `debugging` — portable

> **description** — REQUIRED when investigating why something is broken — a crash, a wrong value, a
> hang, an intermittent failure, or behaviour that does not match expectation. Covers reproducing
> reliably before changing anything, narrowing by bisection across code, data, time and
> configuration, reading evidence rather than guessing, and the debugging anti-patterns that produce
> changes nobody can explain. The rule underneath everything: never fix a bug you cannot explain — a
> symptom that disappears without a known mechanism is not a fix. Load it when investigating a
> failure, and before changing code in response to one. Triggers on "bug", "crash", "broken", "why
> does", "not working", "unexpected", "wrong value", "hang", "freeze", "intermittent", "reproduce",
> "repro", "investigate", "root cause", "stack trace", and Vietnamese phrasing "sao nó lỗi", "bị
> crash", "không chạy", "sai kết quả", "treo", "lỗi lúc có lúc không", "tìm nguyên nhân".

### Luật cốt lõi

> **Không sửa lỗi mà anh không giải thích được.**

Một thay đổi làm triệu chứng biến mất mà không có cơ chế thì không phải là đã sửa — nó là một sự
trùng hợp đã được commit. Lỗi vẫn còn, giờ khó tìm hơn, và sẽ quay lại ở chỗ khác.

Hệ quả trực tiếp: **tái hiện trước khi sửa**, vì lỗi không tái hiện ổn định thì không xác nhận sửa
xong được.

### Reference

| File | Nội dung |
|---|---|
| `reproduce.md` | Tái hiện ổn định là điều kiện tiên quyết, không phải bước tuỳ chọn. Thu nhỏ ca tái hiện; loại bỏ nguồn bất định (seed, timestep, thứ tự, đồng hồ — cùng luật với `midcore-testing/determinism-and-golden.md`, trỏ sang chứ không viết lại). Lỗi lúc có lúc không: tần suất là dữ liệu, ghi lại nó. Lỗi chỉ xảy ra ở một môi trường: liệt kê điểm khác biệt trước khi đoán. |
| `narrowing.md` | Chia đôi trên bốn trục: **code** (bisect lịch sử), **dữ liệu** (bỏ nửa nội dung), **thời gian** (từ lúc nào bắt đầu hỏng), **cấu hình** (define, platform, cài đặt). Vòng lặp giả thuyết: phát biểu điều có thể sai, dự đoán quan sát sẽ thấy nếu đúng, kiểm, **giết giả thuyết**. Luật: một phép kiểm loại được nửa không gian đáng giá hơn mười phép kiểm khẳng định điều đã tin. |
| `evidence.md` | Đọc bằng chứng cho đúng: đọc lỗi **đầu tiên** chứ không phải cuối; stack trace đọc từ trong ra; log đọc **toàn bộ**, không đọc phần đuôi. Công cụ nói dối chỗ nào — chỉ số bị đệm, log bị nuốt, timestamp không đồng bộ, debugger làm đổi timing. Chèn đo đạc có mục tiêu thay vì rải log khắp nơi. |
| `anti-patterns.md` | Những kiểu debug hỏng, xếp theo mức độ tốn kém: **shotgun** (đổi nhiều thứ cùng lúc rồi mất khả năng quy trách nhiệm), **sửa triệu chứng** (kẹp `null` check quanh chỗ ném), **"giờ chạy rồi"** mà không biết vì sao, **đổ lỗi cho công cụ** trước khi loại trừ code của mình, **debug bằng cách đoán** thay vì đọc. Mỗi cái kèm dấu hiệu nhận biết đang mắc phải. |

### Không được trùng lặp

| Câu hỏi | Chủ |
|---|---|
| Tìm lỗi **chưa** biểu hiện, rà diff | `code-review` (built-in) |
| Generated member không tồn tại; guard âm thầm false | `unity-cli-workflow` + `midcore-assembly-architecture/feature-flags.md` |
| Chỉ hỏng trong bản build, không hỏng trong editor; stripping; symbolicate crash | `midcore-release-pipeline/versioning-and-symbols.md` |
| Test lúc xanh lúc đỏ | `midcore-testing/determinism-and-golden.md` §3 |
| Chậm chứ không sai | `midcore-perf-budget` |
| Save không đọc được | `midcore-save-migration/safety-and-recovery.md` |

Bảng này chính là giá trị của việc để `debugging` trung lập: nó **định tuyến** tới nơi triệu chứng
đã được mô tả sẵn, thay vì viết lại lần thứ hai và trôi khỏi bản gốc.

**Xong khi:** một phiên được đưa một triệu chứng sẽ tái hiện trước, thu hẹp trước, và từ chối commit
một bản sửa mà nó không giải thích được cơ chế.

---

## Yêu cầu xuyên suốt

1. **Portable** — `refactoring` và `debugging` không chứa `EncosyTower`, `ApexionGame`,
   `Assets/Game`, `com.laicasaane`, `Unity`, hay đường dẫn tuyệt đối. `coding-standards` được miễn.
2. **Không nhân đôi** — không dòng luật nào đã có chủ được viết lại. Trỏ, không chép.
3. **Tiếng Anh**, trigger tiếng Việt trong `description` (DEC-008).
4. **Độ dài** — `SKILL.md` 100–180 dòng; chiều sâu đẩy vào `references/`.
5. **Đọc profile** — `refactoring` và `debugging` mở đầu bằng khối bốn dòng đọc
   `.claude/project-profile.md` như bộ `midcore-*`. `coding-standards` không cần: nó **là** thẩm
   quyền mà profile trỏ tới.
