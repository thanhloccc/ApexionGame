# Midcore Skills — Đặc tả skill

*[Tổng quan](Midcore Skills - Overview.md) · [Quyết định](Midcore Skills - Decisions.md)*

Mỗi skill một mục. Mỗi mục nêu `description` frontmatter cần viết, các file reference, nội dung bắt
buộc có trong đó, và — quan trọng không kém — **skill đó không được trùng lặp cái gì**.

Mọi `SKILL.md` mở đầu bằng đúng bốn dòng này (DEC-003), viết bằng tiếng Anh:

```markdown
Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.
```

---

## 0. `.claude/project-profile.md` — lớp overlay

Không phải skill; là hợp đồng mà cả bảy skill đọc. Viết bằng Markdown với một khối `yaml` để vừa dễ
đọc cho người vừa không mơ hồ khi đọc bằng mắt.

```yaml
engine:            Unity 6000.3.20f1
standard_library:  EncosyTower 0.1.7-preview.3
authority_skills:
  structure_naming:  encosy-tower      # file, thư mục, namespace, collection, error contract
  unity_operations:  unity-cli-workflow # compile, test, build, điều khiển Editor
package_roots:
  - Packages/com.laicasaane.encosy-tower
  - Packages/com.apexion.apexion-game   # first-party: HFSM + bản fork stats
assembly_map:
  layers:  [Game.Common, Game.Data(+.Authoring), Game.Gameplay, Game.Input]
  status:  "Assets/Game là scaffolding rỗng — chưa có .asmdef nào"
  package: "code tái dùng được thì vào com.apexion.apexion-game, không vào Assets/Game"
data_pipeline:
  technology:    EncosyTower Databases + com.laicasaane.bakingsheet 6.3.0-pre.1
  naming_policy: NameCasing.SnakeLower
  asset_root:    Assets/Game/Addressables/assets-shared/database/
  tables:        [ammo, equipment, melee_weapon, ranged_weapon, player_archetype,
                  player_level, player_skill, player_status, quest, quest_chapter]
  delivery:      Addressables 2.9.1
  localization:  com.unity.localization 1.5.12
save:
  technology:  EncosyTower Persistences
  status:      "chưa ship định dạng save nào — luật versioning áp dụng từ cái đầu tiên"
device_tiers:  unknown — ask trước khi nêu bất kỳ ngân sách frame hay memory nào
testing:
  assemblies:  [ApexionGame.Tests.EditorMode]
  playmode:    "không có — đừng ám chỉ là đã có coverage PlayMode"
  runner:      "unity test --mode EditMode (xem unity-cli-workflow)"
ci:            unknown — ask
live_ops:
  backend:     chưa cài (manifest không có package analytics / remote-config / purchasing)
```

Field nào thực sự chưa biết thì ghi `unknown — ask`, tuyệt đối không đoán. Skill nào chạm phải một
field như vậy thì **dừng và hỏi** thay vì bịa ra con số (DEC-005).

---

## 1. `midcore-assembly-architecture`

> **description** — REQUIRED trước khi tạo, tách, gộp hay đổi tên một assembly trong dự án game có
> nhiều hơn một nhúm module. Sở hữu đồ thị phụ thuộc: assembly thuộc tầng nào, tham chiếu được phép
> trỏ hướng nào, phá vòng lặp ra sao, thêm một assembly tốn bao nhiêu compile time, và cách gate một
> module sau feature flag. Nạp trước file đầu tiên của assembly mới, và mỗi khi thêm một tham chiếu
> giữa hai assembly có sẵn. Trigger: "new assembly", "asmdef", "circular dependency", "compile time",
> "module", "layer", "feature flag", "refactor into", và tiếng Việt "tạo assembly", "tách module",
> "phụ thuộc vòng", "compile chậm".

**Vì sao làm trước.** `Assets/Game` đang rỗng. File đầu tiên tạo ra là quyết định một assembly, và đồ
thị assembly là thứ thực sự đắt để sửa về sau.

| Reference | Nội dung |
|---|---|
| `dependency-rules.md` | Mô hình tầng — Common → Data → Systems → Presentation → Composition. Luật duy nhất (**tham chiếu trỏ xuống, không bao giờ trỏ lên, không ngang giữa các peer**), vi phạm trông ra sao, và cách phát hiện chỉ từ file asmdef. Lối thoát khi có vòng lặp thật: một assembly `Contracts`, một bus sự kiện/message, hoặc đảo phụ thuộc — và cách chọn giữa chúng. |
| `splitting-and-compile-time.md` | Một assembly thực sự tốn gì. Khi nào nên tách (vòng đời riêng, gate riêng, đối tượng dùng riêng, mặt editor/test) và khi nào **không** nên (một type duy nhất, "cho gọn", tái dùng trên lý thuyết). Dấu hiệu tách là quá muộn: sửa một khái niệm mà bắt build lại một khái niệm không liên quan. |
| `feature-flags.md` | Compile một module ra ngoài: define symbol, `versionDefines`, `defineConstraints`, gate theo package tuỳ chọn. Kiểu lỗi ngốn hàng giờ — một guard âm thầm cho ra false vì define chưa được truyền sang assembly mới — và cách kiểm. |

**Không được trùng lặp:** bố cục thư mục, luật namespace-so-với-thư-mục, đặt tên file, thứ tự member.
Đó thuộc thẩm quyền `structure_naming`. Skill này dừng ở ranh giới assembly rồi bàn giao.

**Xong khi:** một phiên mới có thể đặt một `Game.Combat` giả định vào đúng tầng, nói được nó được
tham chiếu những assembly nào, và nêu phép kiểm chứng minh nó không tạo vòng lặp.

---

## 2. `midcore-data-pipeline`

> **description** — REQUIRED mỗi khi nội dung game được định nghĩa bằng dữ liệu thay vì code: bảng
> config, số cân bằng, định nghĩa item/skill/quest, bảng rơi đồ, khoá localization, hoặc bất cứ thứ
> gì designer sửa trong spreadsheet. Bao gồm thiết kế bảng và id, luồng import spreadsheet→asset,
> validate lúc import, toàn vẹn tham chiếu chéo bảng, và cách nội dung được đóng gói phát hành. Nạp
> trước khi thêm, tách hay đổi hình dạng bất kỳ bảng nội dung nào, và khi import bắt đầu lỗi.
> Trigger: "table", "database", "spreadsheet", "sheet", "CSV", "balance", "config", "content",
> "designer", "localization key", "drop rate", "import", và tiếng Việt "bảng dữ liệu", "cân bằng chỉ
> số", "thêm bảng", "nhập dữ liệu", "designer chỉnh".

| Reference | Nội dung |
|---|---|
| `table-design.md` | Một bảng cho một khái niệm, không phải cho một màn hình. **Id có kiểu thay vì string/int trần, luôn luôn** — luật đòn bẩy cao nhất ở đây. Khi nào denormalize, mô hình hoá tham chiếu chéo bảng ra sao, mô hình hoá dòng tuỳ chọn/biến thể mà không cần cột nullable. Ví dụ chạy được: cụm weapon/ammo/equipment dùng chung id, phản chiếu đúng hình dạng đang có trong repo này mà không gọi tên nó. |
| `authoring-and-import.md` | Ai sở hữu sheet nào, chính sách đặt tên (một kiểu casing, chốt một lần, áp cho sheet+cột+asset), converter cho kiểu mà spreadsheet không diễn đạt được, và vòng tròn: designer sửa → import → asset sinh ra → runtime đọc. Làm gì khi một cột bị đổi tên hoặc bỏ đi — câu chuyện migration cho *nội dung*, khác với câu chuyện cho *save*. |
| `validation.md` | **Cho import fail, đừng cho phiên chơi fail.** Các tầng validate: schema (kiểu parse được), tham chiếu (mọi id ngoại phân giải được), miền giá trị (khoảng, không rỗng, không trùng id), và bất biến chéo bảng (mọi chương nhiệm vụ có ≥1 nhiệm vụ). Cái gì phải fail cứng, cái gì chỉ cảnh báo. Vì sao crash vì thiếu tham chiếu sau ba tiếng chơi chính là thứ này chặn. |
| `keys-and-groups.md` | Phát hành nội dung: bố cục addressable group khớp bố cục bảng, đặt tên khoá, cái gì nằm trong build và cái gì tải về, cùng hệ quả về dung lượng/patch của cách gom nhóm. Khoá localization coi như nội dung, chịu cùng luật validate. |

**Không được trùng lặp:** attribute cụ thể, hành vi generator, hay các bước trong cửa sổ import của
một công nghệ bảng nào — chúng nằm ở skill thẩm quyền `structure_naming`, tới qua profile.

**Xong khi:** một phiên mới có thể đề xuất một bảng mới cho repo này khớp với mười một bảng có sẵn về
chiến lược id, đặt tên và validate, mà không cần được mô tả trước chúng trông ra sao.

---

## 3. `midcore-save-migration`

> **description** — REQUIRED mỗi khi dữ liệu người chơi được lưu bị đổi hình dạng: thêm, bỏ, đổi tên
> hay đổi kiểu một field, tách hay gộp một blob save, hoặc đổi cách khoá một collection. Bao gồm
> versioning schema, chuỗi migration, tương thích xuôi/ngược, phục hồi khi hỏng hoặc lỗi giữa chừng,
> và cách test migration bằng golden fixture. **Nạp trước khi sửa bất kỳ type nào được serialize
> xuống máy người chơi**, không phải sau. Trigger: "save", "load", "persistence", "player data",
> "schema", "migration", "version", "backward compatible", "wipe", "corrupt", và tiếng Việt "lưu
> game", "đổi schema", "mất dữ liệu", "tương thích bản cũ".

Skill nhiều rủi ro nhất trong bộ: mọi lỗi khác đều vá được ở bản sau, riêng lỗi này phá huỷ vĩnh viễn
tài khoản của người chơi thật.

| Reference | Nội dung |
|---|---|
| `schema-versioning.md` | **Định dạng save đã ship là vĩnh viễn.** Mọi type được lưu mang một version tường minh; tăng nó trong đúng commit đổi hình dạng, không để sau. Một tên field, khi đã ship, không bao giờ được tái sử dụng với ý nghĩa khác — deprecate rồi thêm mới, không đổi công dụng. "Đã ship" nghĩa là gì (đã tới một máy thật, kể cả bản QA). |
| `migration-recipes.md` | Migration là **hàm thuần từ version N sang N+1, nối chuỗi** — không phải một hàm rẽ nhánh theo mọi version lịch sử. Mỗi loại thay đổi một công thức: thêm field (giá trị mặc định), bỏ field (bỏ qua, giữ reader khoan dung), đổi tên, đổi kiểu, tách một blob thành hai, gộp hai thành một, khoá lại dictionary khi kiểu id đổi. Kèm cái bẫy khiến mỗi loại âm thầm mất dữ liệu. |
| `safety-and-recovery.md` | Sao lưu trước khi migrate; migrate sang file mới rồi hoán đổi khi thành công, không sửa tại chỗ. Lỗi giữa chừng khi có nhiều blob — hoặc tất cả hoặc không, so với từng blob. Làm gì với save không đọc được (tuyệt đối không âm thầm reset về game mới). Mã hoá và versioning tương tác ra sao: version phải đọc được **trước** khi giải mã thành công, nếu không thì save sai khoá và save hỏng không phân biệt được. |
| `testing-migrations.md` | **Giữ một golden save fixture cho mọi version đã ship, mãi mãi, trong repo.** Ma trận test: mọi version đã ship → hiện tại, không chỉ N-1 → N. Test round-trip, và khẳng định thực sự bắt được mất mát — so sánh nội dung ngữ nghĩa, không so byte. Đây là thứ khiến cả skill này cưỡng chế được thay vì chỉ là nguyện vọng. |

**Không được trùng lặp:** attribute và cách nối dây store của thư viện persistence. Skill này sở hữu
*khi nào và bằng cách nào hình dạng được đổi*; skill thẩm quyền trong profile sở hữu *khai báo ra sao*.

**Xong khi:** một phiên mới được yêu cầu thêm field vào một type được lưu sẽ tự tăng version, viết
migration N→N+1, và thêm golden fixture — không cần nhắc.

---

## 4. `midcore-testing`

> **description** — REQUIRED khi quyết định test cái gì, viết test, hoặc phán xét một thay đổi đã
> được kiểm chứng hay chưa trong dự án game. Bao gồm các tầng test thực sự có lời trong game
> (logic/golden, validate bảng nội dung, migration save, benchmark) so với các tầng chủ yếu tốn thời
> gian, tính tất định và fixture, và tầng nào phải xanh trước khi coi một thay đổi là xong. Nạp khi
> thêm test, khi bị hỏi "test đủ chưa", và trước khi tuyên bố một thay đổi đã được kiểm chứng.
> Trigger: "test", "unit test", "coverage", "golden", "fixture", "benchmark", "regression", "verify",
> "flaky", và tiếng Việt "viết test", "kiểm thử", "test đủ chưa", "hồi quy".

| Reference | Nội dung |
|---|---|
| `test-tiers.md` | Bốn tầng, xếp theo giá trị trên mỗi phút bỏ ra **riêng trong game**: (1) validate bảng nội dung, (2) migration save, (3) logic tất định/golden, (4) benchmark trên vòng lặp nóng. Mỗi tầng bắt được gì và không bắt được gì. Vì sao test nặng chạy trong phiên chơi ở editor là tỉ lệ tệ nhất trong bộ và nên hiếm và có mục tiêu rõ. |
| `data-and-balance-tests.md` | Test khẳng định trên **nội dung**, không phải trên code: không tham chiếu mồ côi, không trùng id, mọi đường cong tiến trình đơn điệu, mọi phần thưởng với tới được, không dòng nào không được tham chiếu. Chúng bắt lỗi designer mà không test code nào bắt được, và chúng rẻ. |
| `determinism-and-golden.md` | Làm cho mô phỏng test được: random có seed, timestep cố định, logic không phụ thuộc đồng hồ hệ thống hay frame rate. Ghi một lượt chạy golden rồi khẳng định lại nó; cách cập nhật golden một cách có chủ đích thay vì vô tình đóng dấu cho một hồi quy. |
| `merge-gates.md` | Cái gì phải xanh trước khi coi là xong, theo từng loại thay đổi — sửa nội dung, đổi logic, đổi type được lưu, đổi hot path. Luật: một lượt chạy xanh mà không test nào được thực thi là một lượt **thất bại**, không phải pass. |

**Không được trùng lặp:** dòng lệnh test runner và các flag của nó — đó là `unity_operations`.

**Xong khi:** một phiên mới nói được tầng nào áp cho một thay đổi cụ thể và vì sao, và từ chối gọi
một thay đổi type được lưu là "đã kiểm chứng" khi chưa có test migration.

---

## 5. `midcore-perf-budget`

> **description** — REQUIRED mỗi khi hiệu năng được tuyên bố, đo đạc, lập ngân sách hay tối ưu: thời
> gian frame, bộ nhớ, thời gian load, áp lực GC, hay câu "đủ nhanh chưa". Bao gồm định nghĩa tier
> thiết bị và ngân sách frame/memory theo tier, chia ngân sách đó cho từng hệ thống, quy trình
> profiling và những gì editor báo sai, audit cấp phát, và bằng chứng bắt buộc trước khi gọi một thay
> đổi là cải thiện. Nạp trước khi tối ưu bất cứ thứ gì và trước khi chấp nhận bất kỳ tuyên bố hiệu
> năng nào. Trigger: "performance", "optimize", "slow", "fps", "frame time", "memory", "GC",
> "allocation", "profiler", "budget", "stutter", "load time", và tiếng Việt "tối ưu", "giật lag",
> "chậm", "tốn ram", "đo hiệu năng".

| Reference | Nội dung |
|---|---|
| `device-tiers-and-budgets.md` | Đặt tên các tier mục tiêu (min-spec / mid / high) kèm một máy tham chiếu thật cho mỗi tier, rồi một **bảng ngân sách điền sẵn khung**: tổng ms/frame mỗi tier, chia cho mô phỏng, render, UI, và phần dự phòng; trần bộ nhớ chia cho nội dung, texture, audio, managed heap. Ý chính: "đủ nhanh" là vô nghĩa cho tới khi có một con số. Kèm câu hỏi phải đặt khi profile ghi `device_tiers: unknown`. |
| `profiling-workflow.md` | Đo trên đúng tier mục tiêu, không đo trên máy dev — và những gì editor báo sai một cách hệ thống (cấp phát chỉ có ở editor, domain reload, thời gian không đại diện, chi phí deep-profile). Cô lập một frame; phân biệt gai nhọn với chi phí kéo dài; chụp before/after trong cùng điều kiện. |
| `allocation-audit.md` | Khi nào áp lực GC thực sự quan trọng và khi nào đuổi theo nó là phí công. Tìm cấp phát mỗi frame, các nguồn quen thuộc (closure, boxing, LINQ, dựng string, cấp phát enumerator trong vòng lặp nóng), và cách xác nhận một cái là thật chứ không phải trên lý thuyết. |
| `evidence-rules.md` | **Không tuyên bố hiệu năng nào mà không có số before/after, trên một tier đã nêu, trong điều kiện đã nêu.** "Chắc là nhanh hơn" không phải kết quả. Đồng thời: khi nào một hồi quy đo được là chấp nhận được vì nó đổi lấy thứ khác, và ghi lại thế nào để người sau không "sửa" nó. |

**Không được trùng lặp:** nên chọn collection hay kiểu toán nào — đó là mặc định thường trực thuộc
thẩm quyền `structure_naming`. Skill này lo *đo đạc và lập ngân sách*, không lo *chọn kiểu*.

**Xong khi:** một phiên được yêu cầu tối ưu sẽ nêu tier và ngân sách nó đang làm việc đối chiếu, và
từ chối báo cáo thắng lợi khi chưa có số.

---

## 6. `midcore-release-pipeline`

> **description** — REQUIRED khi tạo, đánh version hay tự động hoá một bản build rời khỏi máy lập
> trình viên: bản build release, CI, ma trận đa nền tảng, đánh số build, xử lý symbol và symbolicate
> crash, và các cổng một bản build phải qua trước khi được phát hành. Nạp khi dựng hay gỡ lỗi CI, khi
> cắt một bản phát hành, hoặc khi chẩn đoán một crash chỉ tái hiện trong player đã build. Trigger:
> "release", "CI", "pipeline", "build number", "version", "IL2CPP", "symbols", "crash", "store
> build", "signing", và tiếng Việt "lên bản", "build release", "đánh version", "crash trên máy thật".

Nối tiếp đúng chỗ `unity_operations` dừng lại: skill đó chạy một bản build trên máy này; skill này lo
những bản build được ship.

| Reference | Nội dung |
|---|---|
| `build-matrix.md` | Liệt kê cái gì thực sự thay đổi — nền tảng, cấu hình (dev/release), backend, biến thể nội dung, cửa hàng — và cố ý giữ ma trận nhỏ. Cái gì phải giống hệt nhau trên toàn ma trận và cái gì được khác. Tính tái lập: một bản build phải là hàm của một commit cộng một cấu hình, không lấy gì từ trạng thái cục bộ của lập trình viên. |
| `versioning-and-symbols.md` | Một lược đồ version sống sót qua hotfix; số build nối một crash ngược về commit. Giữ symbol IL2CPP/native cho mọi bản build, mãi mãi, và vì sao một crash không symbolicate được từ bản đã ship là vô dụng. Thiết lập stripping và đoạn code chúng âm thầm gỡ đi. |
| `ci-gates.md` | Danh sách cổng có thứ tự trước khi một bản build được phát hành: compile → validate nội dung → test migration → test logic → build thành công → smoke-boot. Fail sớm, rẻ trước. Cổng nào chặn và cổng nào chỉ cảnh báo, cùng luật thường trực: cổng bị bỏ qua phải được báo cáo tường minh, không bao giờ âm thầm bỏ. |

**Không được trùng lặp:** lệnh gọi Editor cục bộ và các flag — `unity_operations` sở hữu chúng và đã
đầy đủ trên máy này.

**Xong khi:** một phiên mới nói được thứ tự cổng cho một bản build release và artifact nào chứng minh
từng cổng đã qua.

---

## 7. `midcore-live-ops`

> **description** — Dùng khi hành vi hoặc nội dung game phải thay đổi mà không ship bản cập nhật
> client: cấu hình từ xa, sự kiện và mùa giải có giới hạn thời gian, A/B test, kill-switch tính năng,
> và telemetry cần thiết để biết những thứ đó có hiệu quả không. Bao gồm cái gì an toàn để điều khiển
> từ xa so với cái gì phải nằm trong build, xử lý giờ server và lệch đồng hồ, phân loại sự kiện, và
> an toàn khi triển khai. Nạp trước khi làm bất cứ thứ gì có thể cấu hình từ xa. Trigger: "remote
> config", "live ops", "event", "season", "A/B test", "feature flag at runtime", "analytics",
> "telemetry", "funnel", "kill switch", "rollout", và tiếng Việt "cấu hình từ xa", "sự kiện trong
> game", "mùa giải", "thống kê người chơi", "tắt tính năng từ xa".

**Trung lập backend.** Profile hiện ghi `live_ops.backend: chưa cài` — đã kiểm đối chiếu manifest.
Skill phải nói thẳng rằng chọn backend là một quyết định package mà người dùng chưa đưa ra, và không
được giả định nhà cung cấp nào (DEC-004).

| Reference | Nội dung |
|---|---|
| `remote-config.md` | Lằn ranh: **giá trị thì an toàn để điều khiển từ xa, hình dạng thì không**. Số tinh chỉnh, công tắc và mức khả dụng của nội dung có thể ở xa; bất cứ thứ gì mà cấu trúc code phía client phụ thuộc vào thì không. Mọi giá trị từ xa cần một mặc định đã ship, an toàn khi fetch lỗi, chậm, hoặc trả về rác — luật offline-first. |
| `events-and-seasons.md` | Nội dung có giới hạn thời gian: **giờ server là đồng hồ duy nhất**, không bao giờ dùng đồng hồ máy. Xử lý lệch đồng hồ, chơi offline, sự kiện kết thúc giữa phiên, và người chơi bước vào một sự kiện đã đóng. Lên lịch nội dung phải nằm sẵn trong build trước khi được bật. |
| `telemetry.md` | Một phân loại sự kiện chốt sớm — tên và thuộc tính vĩnh viễn ngang schema save, vì cùng một lý do: dữ liệu lịch sử đã ghi theo chúng rồi. Cần đo gì cho một game tiến trình midcore (funnel, tiến trình, nguồn/bể của kinh tế, hình dạng phiên chơi), và cái bẫy về khối lượng. |
| `kill-switches.md` | Mọi tính năng điều khiển từ xa cần một công tắc tắt đưa về hành vi mặc định đã ship, được test trước khi ra mắt chứ không phải trong lúc sự cố. Triển khai theo giai đoạn, theo dõi gì trong lúc đó, và định nghĩa trước điều kiện rollback. |

**Không được trùng lặp:** monetisation, IAP và tuân thủ cửa hàng — kề cận, thực sự riêng biệt, và
tường minh nằm ngoài phạm vi (Tổng quan §4). Nếu bị hỏi thì nói thẳng thay vì phủ được một nửa.

**Xong khi:** một phiên được yêu cầu làm thứ gì đó cấu hình từ xa sẽ nêu mặc định đã ship, hành vi khi
lỗi, và kill switch trước khi viết bất cứ dòng nào.

---

## Yêu cầu xuyên suốt cho cả bảy

1. **Tính portable** — không xuất hiện `EncosyTower`, `ApexionGame`, `Assets/Game`, `com.laicasaane`,
   hay bất kỳ đường dẫn tuyệt đối nào. Cưỡng chế bằng lệnh grep ở Tổng quan §2.4 mục 2.
2. **Mọi fact được trích đều phân giải được** — đúng chuẩn đường-dẫn-chết đã áp ở lần review trước.
3. **Tiếng Anh, kèm từ khoá trigger tiếng Việt** (DEC-008). Mọi `SKILL.md` và mọi file trong
   `references/` viết **bằng tiếng Anh**, một bản duy nhất — không có `.vi.md`. Ngoại lệ duy nhất là
   `description` frontmatter, vẫn giữ một danh sách ngắn cụm trigger tiếng Việt, vì đó là trường
   quyết định skill nào được nạp và prompt thực tế là tiếng Việt. Đây đúng là cách `encosy-tower` và
   `unity-cli-workflow` đang làm. (Bộ plan này thì ngược lại: chỉ tiếng Việt.)
4. **Độ dài** — `SKILL.md` giữ khoảng 120–200 dòng, mang tính quyết định; phần chiều sâu đẩy vào
   `references/`, đọc khi cần. Lấy SKILL.md của `encosy-tower` làm mốc hiệu chỉnh.
5. **Nhường quyền tường minh** — mỗi skill nêu rõ nó **không** sở hữu cái gì và thẩm quyền nào sở hữu,
   để không bao giờ có hai skill trả lời cùng một câu hỏi theo hai hướng khác nhau.
