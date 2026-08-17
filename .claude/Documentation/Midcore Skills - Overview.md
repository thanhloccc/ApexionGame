# Midcore Skills — Tổng quan

*[Đặc tả skill](Midcore Skills - Skill Specs.md) · [Quyết định](Midcore Skills - Decisions.md)*

## Trạng thái

| | |
|---|---|
| Giai đoạn | **Đã triển khai** — 2026-08-15, cả 14 bước |
| Sản phẩm | 7 skill portable (33 file) + 1 file profile cho mỗi repo |
| Vị trí skill | `.claude/skills/midcore-*/` — **trong project, commit lên git** (DEC-011) |
| Vị trí overlay | `.claude/project-profile.md` (một file, không phải bảy overlay) |
| Skill hiện có bị đụng | `encosy-tower`, `unity-cli-workflow` — chỉ thêm liên kết chéo, không viết lại |
| Dùng ngay cho | dựng `Assets/Game` từ scaffolding rỗng |
| Quyết định còn mở | DEC-004 (backend live-ops), DEC-007 (cách nạp profile) |

---

## 1. Tóm tắt yêu cầu

Xây bộ skill phục vụ **repo game scope midcore trở lên** — tái sử dụng được cho các dự án sau của
studio, không riêng Land of Souls. Vòng hỏi đã chốt:

| Câu hỏi | Trả lời |
|---|---|
| Phục vụ ai? | **Portable core + project overlay.** Skill viết trung lập; mỗi repo tự pin fact của mình. |
| Nhóm nền tảng | **Cả bốn** — data & content pipeline, save versioning & migration, performance budget & profiling, testing & verification. |
| Nhóm mở rộng | **Architecture at scale, live-ops, build/release & CI.** Netcode loại hẳn — game single-player. |
| Dùng ngay vào | **Dựng `Assets/Game` từ đầu**, hiện đang rỗng. Skill viết theo hướng kê đơn, không phải sách tra cứu trung lập. |

"Midcore" ở đây nghĩa là gì thì đọc thẳng từ repo ra được. Thư mục đã commit
`Assets/Game/Addressables/assets-shared/database/` chứa mười một `DataTableAsset`:

```
ammo · equipment · melee_weapon · ranged_weapon
player_archetype · player_level · player_skill · player_status
quest · quest_chapter · game_database
```

Tức là: action RPG single-player với hệ vũ khí/đạn, trang bị, tiến trình theo archetype-và-level,
cây kỹ năng, hiệu ứng trạng thái, và tuyến nhiệm vụ chia chương — dữ liệu chạy từ spreadsheet, đặt
tên `snake_lower`, phát hành qua Addressables. Mọi skill bên dưới bám vào hình dạng đó, không bám vào
khái niệm chung chung về "một game lớn".

## 2. Kết quả mong đợi

### 2.1 Trên đĩa sẽ có gì khi xong

```
.claude/skills/                                     ← cùng chỗ với encosy-tower, unity-cli-workflow
├── midcore-assembly-architecture/
│   ├── SKILL.md
│   └── references/{dependency-rules, splitting-and-compile-time, feature-flags}.md
├── midcore-data-pipeline/
│   ├── SKILL.md
│   └── references/{table-design, authoring-and-import, validation, keys-and-groups}.md
├── midcore-save-migration/
│   ├── SKILL.md
│   └── references/{schema-versioning, migration-recipes, safety-and-recovery, testing-migrations}.md
├── midcore-testing/
│   ├── SKILL.md
│   └── references/{test-tiers, data-and-balance-tests, determinism-and-golden, merge-gates}.md
├── midcore-perf-budget/
│   ├── SKILL.md
│   └── references/{device-tiers-and-budgets, profiling-workflow, allocation-audit, evidence-rules}.md
├── midcore-release-pipeline/
│   ├── SKILL.md
│   └── references/{build-matrix, versioning-and-symbols, ci-gates}.md
└── midcore-live-ops/
    ├── SKILL.md
    └── references/{remote-config, events-and-seasons, telemetry, kill-switches}.md

.claude/
├── project-profile.md                              ← MỚI: overlay, điền sẵn cho repo này
├── CLAUDE.md                                       ← thêm dòng trỏ tới profile + bộ midcore
└── memory/midcore-skills-are-portable.md           ← MỚI
```

**33 file SKILL/reference + 1 profile + 2 chỉnh sửa nhỏ.** Không có code, không có asset Unity.

Tất cả nằm trong `.claude/` nên **commit lên git được** — cả team dùng chung một bản, và mọi thay
đổi luật có lịch sử. Nội dung skill vẫn viết portable (DEC-003), nên áp sang repo khác là copy thư
mục `midcore-*` rồi viết một `project-profile.md` mới.

### 2.2 Cơ chế, gói trong một hình

Skill portable không bao giờ nhắc tên EncosyTower, `ApexionGame.*`, hay đường dẫn dưới `Assets/Game`.
Nó nói về **năng lực**, rồi phân giải qua profile:

```
midcore-data-pipeline/SKILL.md
   │  "Công nghệ bảng dữ liệu và quy ước id của dự án được pin trong
   │   `.claude/project-profile.md`. Đọc nó trước khi đề xuất schema.
   │   Nếu thiếu profile thì nói ra và hỏi — không đoán, không giả định
   │   một package hay standard library cụ thể."
   ▼
<repo>/.claude/project-profile.md
      data_pipeline:
        technology:     EncosyTower Databases + BakingSheet 6.3.0-pre.1
        authority_skill: encosy-tower          # recipe, attribute chính xác
        naming_policy:  NameCasing.SnakeLower
        asset_root:     Assets/Game/Addressables/assets-shared/database/
        tables:         ammo, equipment, melee_weapon, ranged_weapon, …
```

Một file cho mỗi repo, không phải bảy overlay — dự án mới chỉ viết một profile là thừa hưởng cả bảy
skill (DEC-001).

### 2.3 Một phiên làm việc thay đổi cụ thể ra sao

| Prompt hôm nay | Prompt sau khi có skill |
|---|---|
| "thêm bảng quest_reward" → tự bịa schema, đoán kiểu id, quên validate lúc import | nạp `midcore-data-pipeline`, đọc profile, đề xuất bảng khớp với 11 bảng đang có, thêm luật validate, đặt tên Addressables group |
| "đổi PlayerData thêm field" → sửa class, ship bản save âm thầm xoá sạch người chơi cũ | nạp `midcore-save-migration`, tăng `Version`, viết migration N→N+1, thêm golden-save fixture |
| "tối ưu cái này" → viết lại code, tuyên bố đã nhanh hơn | nạp `midcore-perf-budget`, từ chối kết luận nếu chưa có số before/after trên máy đúng tier |
| "tạo assembly mới" → tạo xong, phụ thuộc trỏ ngược hướng, compile time phình dần | nạp `midcore-assembly-architecture`, đặt vào đúng tầng, kiểm hướng phụ thuộc |

### 2.4 Làm sao biết là chạy được

1. **Quét đường dẫn chết** — mọi path, package, file được trích trong 33 file đều phải phân giải
   được. Đúng phép kiểm đã tóm được các con trỏ `PlayerError.cs` chết ở lần review trước.
2. **Kiểm tính portable** — `grep -rE "EncosyTower|ApexionGame|Assets/Game|com\.laicasaane"` trên
   `.claude/skills/midcore-*` phải ra **không kết quả**. Có kết quả nghĩa là fact đã rò vào core.
3. **Kiểm đụng trigger** — không skill mới nào tranh cùng một prompt với `encosy-tower` hay
   `unity-cli-workflow`.
4. **Smoke test** — ba prompt đại diện (một tiếng Việt), xác nhận đúng skill được nạp và có đọc
   profile. Liệt kê ở Bước §14.

## 3. Các bước

| # | Hành động | Xong khi | File |
|---|---|---|---|
| 1 | ~~Tạo thư mục skill user-level~~ → **bỏ**, skill nằm trong project (DEC-011) | `.claude/skills/` đã có sẵn | — |
| 2 | Thiết kế schema profile, rồi điền cho repo này từ fact đã kiểm (engine 6000.3.20f1, BakingSheet 6.3.0-pre.1, Addressables 2.9.1, 11 bảng, `ApexionGame.Tests.EditorMode`, chưa có backend live-ops) | Mọi field hoặc có giá trị đã kiểm, hoặc ghi rõ `unknown — ask` | `.claude/project-profile.md` |
| 3 | Trỏ `CLAUDE.md` tới profile | Một dòng dưới mục "Where things live"; không sửa gì khác | `.claude/CLAUDE.md` |
| 4 | Viết `midcore-assembly-architecture` — **làm trước, vì `Assets/Game` rỗng và cần đồ thị asmdef trước file đầu tiên** | SKILL.md + 3 reference; mô hình tầng + luật hướng + cách phát hiện vi phạm | `.claude/skills/midcore-assembly-architecture/**` |
| 5 | Viết `midcore-data-pipeline` | SKILL.md + 4 reference; có ví dụ tái hiện hình dạng 11 bảng theo cách trung lập | `…/midcore-data-pipeline/**` |
| 6 | Viết `midcore-save-migration` | SKILL.md + 4 reference; luật chuỗi N→N+1 và mẫu golden-fixture đều có ví dụ | `…/midcore-save-migration/**` |
| 7 | Viết `midcore-testing` | SKILL.md + 4 reference; bốn tầng test, và tầng nào chặn merge | `…/midcore-testing/**` |
| 8 | Viết `midcore-perf-budget` | SKILL.md + 4 reference; có bảng ngân sách điền sẵn khung và luật bằng chứng | `…/midcore-perf-budget/**` |
| 9 | Viết `midcore-release-pipeline` | SKILL.md + 3 reference; nối tiếp đúng chỗ `unity-cli-workflow` dừng (local → CI) | `…/midcore-release-pipeline/**` |
| 10 | Viết `midcore-live-ops` | SKILL.md + 4 reference; trung lập backend, nói thẳng repo này chưa cài backend nào | `…/midcore-live-ops/**` |
| 11 | Liên kết chéo từ hai skill hiện có | `encosy-tower` và `unity-cli-workflow` mỗi cái thêm một khối "see also" ngắn; không dời hay viết lại luật nào | `.claude/skills/*/SKILL.md` |
| 12 | Ghi lại bố cục vào memory | Một file memory + một dòng trong `MEMORY.md` | `.claude/memory/midcore-skills-are-portable.md`, `MEMORY.md` |
| 13 | Chạy kiểm đường dẫn chết, tính portable, đụng trigger (§2.4 mục 1–3) | Cả ba sạch; rò rỉ nào thì sửa và chạy lại | — |
| 14 | Smoke test: `"thêm bảng quest_reward"` → data-pipeline; `"đổi schema save"` → save-migration; `"tạo assembly Game.Combat"` → assembly-architecture | Mỗi cái nạp đúng skill **và** trích lại được profile | — |

Bước 4–10 độc lập với nhau, viết theo thứ tự nào hoặc song song đều được; chúng chỉ phụ thuộc Bước 2.
Bước 13–14 phụ thuộc toàn bộ 4–10.

## 4. Mục tiêu và không phải mục tiêu

**Mục tiêu**

- Bảy skill đứng vững ở scope midcore và còn dùng được cho dự án kế tiếp của studio.
- Một file overlay cho mỗi repo — áp bộ này lên dự án mới là một file, không phải bảy bản fork.
- Đủ tính kê đơn để dẫn việc dựng `Assets/Game` từ đầu ngay bây giờ.
- Không chồng lấn `encosy-tower` và `unity-cli-workflow`; hai skill đó vẫn là thẩm quyền về
  cấu trúc/đặt tên/collection/error và về thao tác Unity.

**Không phải mục tiêu**

- **Netcode và multiplayer** — loại hẳn theo vòng hỏi.
- Viết lại hay nuốt hai skill hiện có. Chúng giữ nguyên phạm vi; skill mới nhường lại cho chúng.
- Viết bất kỳ code game, asmdef, hay asset Unity nào. Plan này chỉ tạo ra skill.
- Một skill thứ mười về "cách viết skill". Không ai yêu cầu, và thêm một lớp gián tiếp nữa.
- Monetisation/IAP/tuân thủ store — kề cận live-ops nhưng là chuyên môn riêng; nếu bị hỏi thì nói
  thẳng là ngoài phạm vi thay vì phủ được một nửa.

## 5. Bảy skill nhìn một lượt

Xếp theo lúc một dự án greenfield cần đến, **không** theo thứ tự lúc chọn.

| # | Skill | Trả lời câu hỏi | Cần từ lúc |
|---|---|---|---|
| 1 | `midcore-assembly-architecture` | "assembly này đặt đâu, và được phép trỏ hướng nào?" | file đầu tiên của `Assets/Game` |
| 2 | `midcore-data-pipeline` | "làm sao designer sở hữu hàng trăm dòng mà không vỡ build?" | bảng nội dung đầu tiên |
| 3 | `midcore-save-migration` | "save viết bởi v1.0 làm sao còn đọc được ở v1.7?" | **trước** khi định dạng save đầu tiên ship |
| 4 | `midcore-testing` | "trong game thì test cái gì là đáng, và cái gì chặn merge?" | khi các hệ thống lần lượt xong |
| 5 | `midcore-perf-budget` | "ngân sách frame là bao nhiêu, và thay đổi vừa rồi có giúp không?" | khi đã có thứ để đo |
| 6 | `midcore-release-pipeline` | "build ra sao để tái lập được và symbolicate được?" | bản build lên máy thật đầu tiên |
| 7 | `midcore-live-ops` | "cái gì đổi được mà không cần cập nhật client?" | trước soft launch |

Đặc tả đầy đủ từng skill — description frontmatter, trigger, dàn ý reference, và cái gì **không**
được trùng lặp — nằm ở [Midcore Skills - Skill Specs](Midcore Skills - Skill Specs.md).

## 6. Quan hệ với các skill hiện có

Phân vai ba bên, nói một lần để không skill nào đem ra tranh lại:

| Câu hỏi | Thẩm quyền |
|---|---|
| Dùng module/API nào, đặt tên file ra sao, chọn collection nào, mô hình hoá error thế nào | **`encosy-tower`** (project-level) |
| Chạy Editor, test, hay build **trên máy này** ra sao | **`unity-cli-workflow`** (project-level) |
| Cấu trúc, validate, versioning, đo đạc, ship và vận hành một game ở quy mô lớn | **`midcore-*`** (portable) |

Cụ thể: `midcore-data-pipeline` nói *"validate mọi tham chiếu chéo bảng lúc import và cho import
fail, đừng để fail lúc chơi"*. Nó **không** nói `[Database(NameCasing.SnakeLower)]` — đó là recipe
của `encosy-tower`, tới được qua field `authority_skill` trong profile. Tương tự, `midcore-testing`
nói tầng nào chặn merge; `unity-cli-workflow` sở hữu lệnh `unity test` chạy chúng.

Đây chính là luật giữ cho core còn portable (DEC-003), và phép kiểm ở §2.4 mục 2 cưỡng chế nó bằng
máy chứ không bằng review.
