# Midcore Skills — Quyết định

*[Tổng quan](Midcore Skills - Overview.md) · [Đặc tả skill](Midcore Skills - Skill Specs.md)*

Mỗi dòng một ngã rẽ thật. `chốt` = đã quyết, `open` = còn cần câu trả lời (có đánh dấu khuyến nghị).

---

## DEC-001 — Một file profile cho mỗi repo, không phải bảy skill overlay

**Trạng thái:** chốt

Vòng hỏi chọn "portable core + project overlay". Cách hiểu hiển nhiên là bảy skill portable cộng bảy
skill overlay cho từng repo. Bác bỏ: mười bốn file phải giữ đồng bộ, và Claude Code hiển thị skill
project-level cùng user-level chung một chỗ, nên hai cái trùng tên sẽ tranh nhau lúc chọn.

**Chọn:** bảy skill portable đọc **một** file, `.claude/project-profile.md`, trong bất cứ repo nào
chúng chạy. Áp cả bộ lên dự án mới là một file.

**Phương án bị bác**

| Phương án | Vì sao không |
|---|---|
| Bảy skill overlay | Gấp đôi số file, đụng tên, core và overlay trôi khỏi nhau |
| Nhét fact của repo vào thẳng skill portable | Phá hỏng mục đích; dự án sau phải fork cả bảy |
| Đọc fact từ `CLAUDE.md` | Đó là văn xuôi viết cho người; skill cần field có tên ổn định, và `CLAUDE.md` vốn đã dài |

---

## DEC-002 — Tiền tố tên `midcore-`

**Trạng thái:** chốt

Tên: `midcore-assembly-architecture`, `midcore-data-pipeline`, `midcore-save-migration`,
`midcore-testing`, `midcore-perf-budget`, `midcore-release-pipeline`, `midcore-live-ops`.

Tiền tố gom chúng lại trong danh sách skill, báo hiệu đây là bộ portable chứ không phải skill của dự
án, và đảm bảo không đụng bất cứ tên nào repo tự định nghĩa. Cái giá: gõ `/midcore-testing` dài hơn.

**Phương án bị bác:** tên trần (`testing`, `live-ops`) — quá chung, nguy cơ đụng cao; tiền tố `game-`
— đúng nhưng không nói lên tầng scope mà bộ này được hiệu chỉnh cho.

---

## DEC-003 — Skill portable không bao giờ gọi tên nhà cung cấp, package hay đường dẫn

**Trạng thái:** chốt

Skill portable nêu **luật**; profile cung cấp **công nghệ**. `midcore-data-pipeline` nói "validate mọi
tham chiếu chéo bảng lúc import và cho import fail"; nó không bao giờ nói
`[Database(NameCasing.SnakeLower)]`.

**Vì sao quan trọng:** đây là thứ duy nhất phân biệt một skill thực sự tái dùng được với một bản sao
`encosy-tower` bị xoá số seri. Nó cũng kiểm được bằng máy, nên Tổng quan §2.4 mục 2 dùng grep tìm rò
rỉ thay vì tin vào review.

**Hệ quả phải chấp nhận:** skill portable hơi kém hành động-được-ngay so với skill của dự án. Field
`authority_skills` trong profile bù lại bằng cách chỉ đích danh nên hỏi ai.

---

## DEC-004 — Live-ops trung lập backend; chưa chọn nhà cung cấp

**Trạng thái:** **open** — khuyến nghị bên dưới

Đã kiểm đối chiếu `Packages/manifest.json`: không có package analytics, remote-config hay purchasing
nào được cài. Nên `midcore-live-ops` không thể giả định Unity Gaming Services, Firebase, PlayFab hay
bất cứ thứ gì mà không bịa ra một quyết định người dùng chưa đưa ra.

**Khuyến nghị:** viết skill hoàn toàn trung lập backend ngay bây giờ, với `live_ops.backend: chưa
cài` trong profile và một chỉ dẫn tường minh là phải nêu lựa chọn ra thay vì tự giả định. Xem lại khi
đã chọn backend — lúc đó profile có thêm giá trị `backend:` và có thể thêm một file reference riêng
cho nhà cung cấp, còn bản thân skill giữ nguyên.

**Cần từ anh:** backend đã được chốt nhưng chưa cài hay chưa? Nếu rồi thì cho biết cái nào, skill sẽ
viết bám theo nó.

---

## DEC-005 — Field profile chưa biết thì ghi `unknown — ask`, không đoán

**Trạng thái:** chốt

`device_tiers` và `ci` hiện không có giá trị nào kiểm chứng được trong repo. Skill nào cần một trong
hai thì **dừng và hỏi** thay vì nêu một con số nghe hợp lý.

**Vì sao:** một ngân sách frame do agent bịa ra rồi lặp lại qua hàng chục phiên sẽ thành truyền
thuyết dự án mà không ai nhớ đã quyết lúc nào. Đây đúng là kiểu lỗi đã sinh ra các con trỏ
`PlayerError.cs` chết tìm thấy ở lần review skill trước — một fact nói chắc nịch mà không có nguồn.

---

## DEC-006 — Thứ tự làm theo nhu cầu greenfield, không theo cách nhóm của vòng hỏi

**Trạng thái:** chốt

Vòng hỏi chia "nền tảng" và "mở rộng", đẩy `midcore-assembly-architecture` xuống nhóm hai. Thứ tự
xây đảo lại: nó làm **trước**, vì `Assets/Game` đang rỗng và file đầu tiên tạo ra là quyết định một
assembly. Đồ thị assembly là thứ đắt nhất để sửa về sau.

Thứ tự: architecture → data-pipeline → save-migration → testing → perf-budget → release-pipeline →
live-ops. Bước 4–10 độc lập và viết song song được; thứ tự quan trọng ở chỗ cái nào hữu ích sớm nhất,
không phải ở tính đúng đắn.

---

## DEC-007 — Profile được đọc bằng cách nào

**Trạng thái:** **open** — khuyến nghị bên dưới

Mỗi `SKILL.md` mở đầu bằng "đọc `.claude/project-profile.md`". Cách đó chạy được nhưng phụ thuộc vào
việc skill được nạp trước — một prompt không kích hoạt skill nào thì không bao giờ thấy profile.

**Khuyến nghị:** thêm **đúng một dòng** vào `.claude/CLAUDE.md` trỏ tới profile, để sự tồn tại của nó
luôn có trong context kể cả khi không skill nào được nạp. Giữ nội dung ở profile, không đưa vào
`CLAUDE.md` — file đó vốn đã dài và nhân đôi field là chắc chắn trôi khỏi nhau.

**Phương án đã cân nhắc:** nhét nguyên profile vào `CLAUDE.md`. Bác bỏ — làm skill portable phụ thuộc
vào bố cục `CLAUDE.md` của riêng repo này, và `CLAUDE.md` được nạp mỗi phiên bất kể có liên quan hay
không.

**Cần từ anh:** xác nhận thêm một dòng vào `CLAUDE.md` là chấp nhận được.

---

## DEC-008 — Plan viết tiếng Việt, SKILL viết tiếng Anh, mỗi loại một bản duy nhất

**Trạng thái:** chốt (theo chỉ đạo của anh, giữa lúc lập plan)

Ngôn ngữ chia theo **loại artifact**, không phải theo quy ước song ngữ mặc định của dự án:

| Artifact | Ngôn ngữ | Bản song ngữ? |
|---|---|---|
| Tài liệu plan (`.claude/Documentation/Midcore Skills - *.md`) | **Tiếng Việt** | Không — bỏ hẳn bản `.md` tiếng Anh |
| `SKILL.md` và mọi file trong `references/` | **Tiếng Anh** | Không — không có bản `.vi.md` |
| `description` frontmatter của skill | Tiếng Anh + **cụm trigger tiếng Việt** | Đây là ngoại lệ duy nhất |

Vì sao có ngoại lệ ở `description`: trường này là thứ quyết định skill nào được nạp. Prompt của anh
là tiếng Việt ("thêm bảng dữ liệu", "đổi schema save", "tối ưu"), nên trigger tiếng Việt phải nằm
trong đó, nếu không skill viết ra sẽ không bao giờ được chọn. `encosy-tower` và `unity-cli-workflow`
đang chạy đúng như vậy, và đó là lý do chúng bắt được prompt tiếng Việt.

**Lệch với quy ước dự án, có chủ đích.** `CLAUDE.md` quy định feature doc phải song ngữ
(`X.md` + `X.vi.md`). Luật đó áp cho feature doc trong `Documentation~` của assembly, nơi có thể có
người đọc tiếng Anh. Bộ plan này là tài liệu nội bộ về tooling, một người đọc — nhân đôi ngôn ngữ chỉ
tạo thêm chỗ để trôi khỏi nhau.

Anh nói một tiếng nếu muốn bỏ luôn từ khoá trigger tiếng Việt khỏi `description`; làm vậy sẽ khiến
việc chọn skill từ prompt tiếng Việt yếu đi thấy rõ, nên khuyến nghị là giữ.

---

## DEC-009 — Netcode nằm ngoài, và giữ nguyên như vậy

**Trạng thái:** chốt

Loại ở vòng hỏi; game là single-player. Ghi lại ở đây để một phiên sau không đọc khoảng trống này
thành thiếu sót rồi tự động đi lấp. Nếu sau này có multiplayer thì đó là một skill mới, không phải
phần mở rộng của skill có sẵn — hai mối bận tâm gần như không giao nhau.

---

## DEC-011 — Skill nằm trong project để commit lên git, không phải user-level

**Trạng thái:** chốt (theo chỉ đạo của anh, giữa lúc thực thi)

Kế hoạch ban đầu đặt bộ `midcore-*` ở `~/.claude/skills/` (user-level, tự động có ở mọi repo). Đổi
sang `.claude/skills/` — cùng chỗ với `encosy-tower` và `unity-cli-workflow`.

**Vì sao đổi:** user-level thì không commit được. Đặt trong project thì:

- cả team dùng chung **một** bản, không phải mỗi máy một bản trôi khác nhau;
- mọi thay đổi luật có **lịch sử git** — đọc được ai đổi luật gì, khi nào, vì sao;
- review được qua pull request như mọi thứ khác trong repo;
- một checkout mới là có ngay bộ skill, không cần cài đặt máy.

**Cái mất:** không tự động có ở repo khác. Nhưng nội dung skill vẫn viết portable (DEC-003), nên áp
sang dự án mới là copy thư mục `midcore-*` rồi viết một `project-profile.md` — vẫn đúng ý đồ
"portable core + project overlay", chỉ khác cách phân phối là copy thay vì kế thừa.

**Hệ quả:** DEC-001 giữ nguyên (một profile, không phải bảy overlay). Phép kiểm tính portable ở
Tổng quan §2.4 mục 2 càng quan trọng hơn — giờ skill nằm cạnh code của repo này, nên rò rỉ fact vào
core dễ xảy ra hơn và phải grep để chặn.

---

## DEC-010 — Monetisation và tuân thủ cửa hàng nằm ngoài phạm vi

**Trạng thái:** chốt

Kề cận `midcore-live-ops` và là ứng viên hợp lý cho skill thứ tám, nhưng là một chuyên môn thực sự
riêng (IAP, xác thực hoá đơn, chính sách cửa hàng, giá theo khu vực, phân loại độ tuổi). Phủ một nửa
nó bên trong live-ops còn tệ hơn là không phủ. `midcore-live-ops` nêu ranh giới tường minh để chỗ
thiếu này đọc ra là có chủ đích.

**Xem lại khi:** game tiến gần soft launch.
