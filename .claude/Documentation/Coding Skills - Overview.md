# Coding Skills — Tổng quan

*[Đặc tả skill](Coding Skills - Skill Specs.md) · [Quyết định](Coding Skills - Decisions.md)*

## Trạng thái

| | |
|---|---|
| Giai đoạn | **Đã triển khai** — 2026-08-15 |
| Sản phẩm | 3 skill — 1 project-level, 2 portable |
| Vị trí | `.claude/skills/{coding-standards,refactoring,debugging}/` |
| Skill hiện có bị đụng | `encosy-tower` (xoá digest, thêm trỏ chéo), `unity-cli-workflow` (trỏ chéo) |
| Quyết định còn mở | DEC-105 (số phận `setup.md` §Coding conventions) |
| Phụ thuộc | Bộ `midcore-*` đã xong; dùng chung `.claude/project-profile.md` |

---

## 1. Tóm tắt yêu cầu

Sau khi bộ `midcore-*` xong, anh hỏi bộ skill đã có phần Coding chưa. Câu trả lời: **chưa có skill
nào**, và có hai lỗ hổng khác nhau. Anh yêu cầu plan cho cả hai.

### Lỗ hổng 1 — không ai sở hữu bộ quy ước

`CODING-CONVENTIONS.md` ở gốc repo dài **1063 dòng, 14 mục**, là tài liệu kỹ thuật dày nhất repo.
Không skill nào sở hữu nó. Nó được nhắc ở 6 chỗ dưới dạng "hãy theo nó", cộng một bản **digest 30
dòng** trong `encosy-tower/references/setup.md` — bản digest này bỏ mất 12/14 mục.

Chính `encosy-tower/references/planning-workflow.md:24` đã tự thú nhận:

> *"There is no dedicated dotnet/C# skill — `encosy-tower` plus `CODING-CONVENTIONS.md` covers that
> ground here."*

**Nhưng hơn một nửa tài liệu đã có chủ.** Audit thực tế:

| Mục | Dòng | Ai sở hữu hôm nay |
|---|---|---|
| §1 Naming | 33 | `encosy-tower/references/structure-and-naming.md` §6 |
| §2 Formatting | **157** | **vô chủ** |
| §3 Usings & Namespaces | 31 | `structure-and-naming.md` §7 |
| §4 Everyday Code Style | **66** | **vô chủ** |
| §5 Comments | **22** | **vô chủ** (chỉ được nhắc 1 lần trong `feature-docs.md`) |
| §6 Files & Layout | 91 | `structure-and-naming.md` §5 |
| §7 Member Ordering | 54 | `structure-and-naming.md` §8 |
| §8 Type & API Design | **113** | **vô chủ** |
| §9 Members & Attributes | **111** | **vô chủ** |
| §10 Errors & Validation | 164 | `encosy-tower/references/structured-errors.md` |
| §11 Performance Annotations | 54 | một phần — `encosy-tower` + `midcore-perf-budget` |
| §12 Async, Logging & Conditional | **43** | **vô chủ** (chỉ alias `UnityTask` trong `recipes.md`) |
| §13 Unsafe & Native Containers | 68 | `encosy-tower/references/collections-and-math.md` |
| §14 Quick Checklist | **46** | **vô chủ** |

**Vô chủ: ~558/1063 dòng.** Đó chính xác là phạm vi của `coding-standards`.

### Lỗ hổng 2 — không có gì về sửa code đã có

Cả 9 skill hiện tại đều nói về **viết mới**: chọn module, đặt file ở đâu, thiết kế bảng, versioning
save, ngân sách hiệu năng. Không skill nào nói về **đổi code đang chạy** hay **tìm ra vì sao nó
hỏng**.

Harness đã có sẵn `code-review`, `simplify`, `security-review` — nên phần review và dọn dẹp chất
lượng đã có người lo. Còn lại đúng hai thứ: **refactor an toàn** và **phương pháp debug**.

## 2. Kết quả mong đợi

### 2.1 Trên đĩa sẽ có gì khi xong

```
.claude/skills/
├── coding-standards/                              ← project-level, bám EncosyTower convention
│   ├── SKILL.md                                     bảng phân quyền 14 mục + checklist §14
│   └── references/
│       ├── formatting.md                            §2 + §4
│       ├── api-design.md                            §8
│       ├── members-and-attributes.md                §9 + §11
│       └── comments-and-async.md                    §5 + §12
├── refactoring/                                   ← portable, đọc project-profile.md
│   ├── SKILL.md
│   └── references/
│       ├── safe-sequences.md                        thứ tự thao tác cho từng loại refactor
│       ├── scope-control.md                         giữ refactor khỏi phình
│       ├── verification.md                          chứng minh hành vi không đổi
│       └── when-not-to.md                           lúc không nên refactor
└── debugging/                                     ← portable, đọc project-profile.md
    ├── SKILL.md
    └── references/
        ├── reproduce.md                             tái hiện ổn định trước đã
        ├── narrowing.md                             chia đôi, giả thuyết, thang bằng chứng
        ├── evidence.md                              đọc log/stack, công cụ nói dối chỗ nào
        └── anti-patterns.md                         những kiểu debug hỏng

.claude/
├── project-profile.md                             ← thêm authority_skills.code_standards
├── memory/coding-skills-split.md                  ← MỚI
└── skills/encosy-tower/references/setup.md        ← xoá digest 30 dòng, thay bằng trỏ chéo
```

**15 file SKILL/reference + 3 chỉnh sửa.** Không có code sản phẩm.

### 2.2 Ba luật cốt lõi

Mỗi skill có đúng một luật, và mọi thứ còn lại là hệ quả của nó:

| Skill | Luật |
|---|---|
| `coding-standards` | **Mục nào của `CODING-CONVENTIONS.md` cũng có đúng một chủ.** Skill này sở hữu 7 mục vô chủ, và trỏ 7 mục còn lại về chủ của chúng — không viết lại. |
| `refactoring` | **Một thay đổi đổi *cấu trúc* HOẶC đổi *hành vi*, không bao giờ cả hai.** Đó là thứ khiến kiểm chứng khả thi. |
| `debugging` | **Không sửa lỗi mà anh không giải thích được.** Triệu chứng biến mất mà không có cơ chế thì không phải là đã sửa. |

### 2.3 Một phiên làm việc thay đổi cụ thể ra sao

| Prompt hôm nay | Prompt sau khi có skill |
|---|---|
| "viết hàm này đúng style chưa" | không skill nào chứa luật style → nạp `coding-standards`, tra đúng mục |
| "tách class này ra" | sửa luôn, trộn cả đổi hành vi vào giữa, không ai kiểm được | nạp `refactoring`, tách hai bước, chứng minh bước 1 không đổi hành vi |
| "sao chỗ này crash" | đoán, sửa, "chắc được rồi" | nạp `debugging`, tái hiện trước, thu hẹp, giải thích được mới sửa |
| "sao generated member không tồn tại" | nạp `debugging` → nhận ra là triệu chứng đặc thù → trỏ sang `unity-cli-workflow` và `midcore-assembly-architecture/feature-flags.md` |

### 2.4 Làm sao biết là chạy được

1. **Bảng phân quyền đủ 14 mục** — mọi mục của `CODING-CONVENTIONS.md` có đúng một chủ, không mục
   nào hai chủ, không mục nào vô chủ. Đây là phép kiểm chính của lỗ hổng 1.
2. **Không nhân đôi** — `grep` các cụm đặc trưng của `structure-and-naming.md` và
   `structured-errors.md` trong `coding-standards/` phải ra rỗng.
3. **Kiểm tính portable** — `grep -rE "EncosyTower|ApexionGame|Assets/Game|com\.laicasaane|Unity"`
   trên `refactoring/` và `debugging/` ra **không kết quả** (`coding-standards` được miễn — nó là
   project-level).
4. **Không đụng built-in** — description của `refactoring` không tranh prompt với `simplify`, của
   `debugging` không tranh với `code-review`.
5. **Smoke test** — ba prompt, một tiếng Việt.

## 3. Các bước

| # | Hành động | Xong khi | File |
|---|---|---|---|
| 1 | Chốt bảng phân quyền 14 mục, đối chiếu từng mục với reference đang sở hữu nó | Đủ 14 dòng, mỗi mục đúng một chủ | (đưa vào `coding-standards/SKILL.md`) |
| 2 | Viết `coding-standards` SKILL.md — bảng phân quyền + checklist §14 + luật cốt lõi | Một phiên mới tra được "§8 đọc ở đâu" mà không mở file 1063 dòng | `.claude/skills/coding-standards/SKILL.md` |
| 3 | Viết 4 reference cho phần vô chủ (~558 dòng gốc, chưng cất lại) | Đủ §2, §4, §5, §8, §9, §11, §12; mỗi file nêu rõ nó **không** sở hữu gì | `…/coding-standards/references/**` |
| 4 | Xoá digest 30 dòng trong `setup.md`, thay bằng trỏ chéo | `setup.md` không còn bản sao luật style | `encosy-tower/references/setup.md` |
| 5 | Sửa `planning-workflow.md:24` — câu "không có skill C# riêng" giờ đã sai | Câu đó trỏ tới `coding-standards` | `encosy-tower/references/planning-workflow.md` |
| 6 | Viết `refactoring` SKILL.md + 4 reference | Luật cấu-trúc-HOẶC-hành-vi nêu rõ; mỗi loại refactor có thứ tự thao tác và cái bẫy của nó | `.claude/skills/refactoring/**` |
| 7 | Viết `debugging` SKILL.md + 4 reference | Luật không-sửa-cái-không-giải-thích-được nêu rõ; triệu chứng Unity trỏ đi chỗ khác, không viết lại | `.claude/skills/debugging/**` |
| 8 | Thêm `authority_skills.code_standards: coding-standards` vào profile | 2 skill portable phân giải được "quy ước style của dự án" | `.claude/project-profile.md` |
| 9 | Trỏ chéo hai chiều: `encosy-tower`, `unity-cli-workflow`, `midcore-*` ↔ 3 skill mới | Mỗi bên có khối "See also"; không luật nào bị dời | `.claude/skills/*/SKILL.md` |
| 10 | Memory + dòng index | Một file + một dòng | `.claude/memory/coding-skills-split.md`, `MEMORY.md` |
| 11 | Chạy 4 phép kiểm §2.4 mục 1–4 | Cả bốn sạch | — |
| 12 | Smoke test: `"đoạn này đúng style chưa"` → coding-standards; `"tách class này"` → refactoring; `"sao nó crash"` → debugging | Mỗi cái nạp đúng skill | — |

Bước 2–3 phụ thuộc bước 1. Bước 6 và 7 độc lập với mọi thứ khác. Bước 11–12 phụ thuộc tất cả.

## 4. Mục tiêu và không phải mục tiêu

**Mục tiêu**

- Mọi mục của `CODING-CONVENTIONS.md` có đúng một chủ, tra được trong vài giây.
- Hai skill portable phủ **sửa code đã có** — thứ 9 skill hiện tại không chạm tới.
- Không nhân đôi một dòng luật nào đã có chủ.
- `refactoring` và `debugging` dùng lại được cho dự án sau, như bộ `midcore-*`.

**Không phải mục tiêu**

- **Viết lại `CODING-CONVENTIONS.md`.** Nó vẫn là nguồn chính thức, người ngoài Claude cũng đọc.
- **Code review và dọn dẹp chất lượng** — harness đã có `code-review` và `simplify`. Skill mới nhường.
- **Triệu chứng đặc thù Unity trong `debugging`** — đã nằm ở `unity-cli-workflow`,
  `midcore-assembly-architecture/feature-flags.md`, `midcore-release-pipeline/versioning-and-symbols.md`.
- **Kiểm tra style tự động** (analyzer, `.editorconfig`). Là việc tooling riêng, không phải skill.
- Skill về testing — đã có `midcore-testing`.

## 5. Quan hệ với các skill hiện có

Phân vai sau khi thêm ba skill này:

| Câu hỏi | Thẩm quyền |
|---|---|
| Dùng module/API nào, đặt file/namespace/assembly ở đâu, chọn collection nào, mô hình hoá error thế nào | `encosy-tower` |
| Viết dòng code đó ra sao — format, thiết kế API, attribute, comment, async | **`coding-standards`** |
| Đổi code đã có mà không làm vỡ | **`refactoring`** |
| Tìm ra vì sao nó hỏng | **`debugging`** |
| Chạy Editor, test, build trên máy này | `unity-cli-workflow` |
| Cấu trúc, validate, versioning, đo đạc, ship, vận hành ở quy mô lớn | `midcore-*` |
| Rà soát diff, dọn dẹp chất lượng, quét bảo mật | `code-review`, `simplify`, `security-review` (built-in) |

Ranh giới dễ nhầm nhất, nói rõ một lần:

- **`refactoring` vs `simplify`** — `simplify` là một **lượt rà** trên diff hiện tại để tìm chỗ gọn
  hơn. `refactoring` là **phương pháp** đổi cấu trúc an toàn khi anh đã biết muốn đổi gì.
- **`debugging` vs `code-review`** — `code-review` tìm lỗi **chưa biểu hiện**. `debugging` truy
  nguyên một lỗi **đã biểu hiện**.
- **`coding-standards` vs `encosy-tower`** — `encosy-tower` trả lời *dùng gì và đặt ở đâu*;
  `coding-standards` trả lời *gõ ra trông thế nào*. Ranh giới cụ thể là bảng 14 mục ở §1.

Đặc tả đầy đủ: [Coding Skills - Skill Specs](Coding Skills - Skill Specs.md).
