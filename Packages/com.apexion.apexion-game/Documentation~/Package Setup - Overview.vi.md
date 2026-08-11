# Package Setup — Tổng quan

## Tóm tắt yêu cầu

Chuyển `Assets/ApexionGame/*` thành một embedded UPM package đúng nghĩa,
`Packages/com.apexion.apexion-game`, được tổ chức giống hệt cách
`Packages/com.laicasaane.encosy-tower` được tổ chức trong repo EncosyTower
(`C:\Users\ADMIN\Documents\Github\EncosyTower`). `Assets/Game/*` (chính game — hiện chỉ
`Game.Input` có nội dung; `Game.Common`/`Game.Data`/`Game.Data.Authoring`/`Game.Gameplay`/
`Game.Gameplay.Editor`/`Game.Gameplay.Tests` đang là các folder trống) giữ nguyên vị trí và trở thành
bên tiêu thụ cả `com.laicasaane.encosy-tower` và package mới `com.apexion.apexion-game`.

## Kết quả mong đợi

- Một embedded package mới tại `Packages/com.apexion.apexion-game/` với `package.json`
  (`name: com.apexion.apexion-game`, `displayName: Apexion Game`, `version: 0.1.0`), một `README.md`
  thật, và `CHANGELOG.md` mới bắt đầu từ `[Unreleased]` (theo chuẩn Keep-a-Changelog). Không có
  `LICENSE.md` — package này là proprietary, khác với EncosyTower (MIT).
- Mọi assembly hiện đang nằm dưới `Assets/ApexionGame/` được chuyển 1-1 sang
  `Packages/com.apexion.apexion-game/`, mang theo file `.meta` để GUID (và do đó mọi tham chiếu từ
  scene/prefab/asset vào các assembly này) không bị đứt.
- `ApexionGame.Core.Samples(.Editor)` (HFSM playground, không có test nào phụ thuộc) được chuyển
  thành folder `Samples~/` opt-in, khai báo trong mảng `samples` của `package.json` — đúng theo
  convention của EncosyTower.
- `ApexionGame.Entities.Stats.Samples/Rts.*` (RTS reference implementation) **giữ lại dạng module
  luôn compile** ở root package, không opt-in — phát hiện giữa lúc thực thi rằng
  `ApexionGame.Tests.EditorMode` tham chiếu cứng tới `ApexionGame.Entities.Stats.Samples.Rts.Core`
  cho test hồi quy (`RtsEffectTests`, `RtsMatchFlowTests`, …); Unity không hề compile bất cứ gì nằm
  trong `Samples~/`, nên nếu để opt-in sẽ làm vỡ compile toàn project. Xem mục Quyết định.
- `Assets/ApexionGame/` bị xoá khi đã trống hoàn toàn.
- `Packages/manifest.json` có thêm entry `"com.apexion.apexion-game": "file:com.apexion.apexion-game"`,
  đồng thời 2 lỗ hổng có từ trước với `com.laicasaane.encosy-tower` và
  `com.laicasaane.encosy-tower.dev-tools` (đã embedded và chạy được nhờ `packages-lock.json`, nhưng
  thiếu trong `dependencies` của `manifest.json`) được sửa luôn.
- Mọi file doc/skill/memory đang hard-code đường dẫn cũ `Assets/ApexionGame/...` được cập nhật sang
  đường dẫn mới `Packages/com.apexion.apexion-game/...`.
- Project mở trong Unity không sinh lỗi console mới, Package Manager liệt kê "Apexion Game" như một
  embedded package, và sample opt-in duy nhất import sạch từ Package Manager UI.

## Các bước thực hiện

| # | Hành động | File liên quan | Điều kiện hoàn thành |
|---|---|---|---|
| 1 | Tạo package manifest | `Packages/com.apexion.apexion-game/package.json` (mới) | JSON hợp lệ theo mục [Package manifest](#package-manifest) bên dưới |
| 2 | Tạo README cho package | `Packages/com.apexion.apexion-game/README.md` (mới) | Doc ngắn, thật: tên, mục đích, danh sách module, link tới `Documentation~/` |
| 3 | Tạo changelog cho package | `Packages/com.apexion.apexion-game/CHANGELOG.md` (mới) | Header Keep-a-Changelog + `## [Unreleased]` |
| 4 | Chuyển module Core | `Assets/ApexionGame/ApexionGame.Core/**` → `Packages/com.apexion.apexion-game/ApexionGame.Core/**` (kèm `Documentation~/HFSM - *.md`) | `git mv`, `.meta` được mang theo, folder biến mất khỏi `Assets/` |
| 5 | Chuyển module Editor | `Assets/ApexionGame/ApexionGame.Editor/**` → `Packages/com.apexion.apexion-game/ApexionGame.Editor/**` | như trên |
| 6 | Chuyển module Entities.Stats | `Assets/ApexionGame/ApexionGame.Entities.Stats/**` → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/**` (kèm `Documentation~/01-OVERVIEW.md`…`07-DECISIONS.md` + `guide/`) | như trên |
| 7 | Chuyển module Entities.Stats.Authoring | `Assets/ApexionGame/ApexionGame.Entities.Stats.Authoring/**` → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Authoring/**` | như trên |
| 8 | Chuyển module Entities.Stats.Editor | `Assets/ApexionGame/ApexionGame.Entities.Stats.Editor/**` → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Editor/**` | như trên |
| 9 | Chuyển module Tests | `Assets/ApexionGame/ApexionGame.Tests.EditorMode/**` → `Packages/com.apexion.apexion-game/ApexionGame.Tests.EditorMode/**` | như trên |
| 10 | Chuyển doc cấp package | `Assets/ApexionGame/Documentation~/Core Package - Overview*.md` → `Packages/com.apexion.apexion-game/Documentation~/Core Package - Overview*.md` | như trên |
| 11 | Gộp Core samples vào một folder `Samples~` | `Assets/ApexionGame/ApexionGame.Core.Samples/**` + `ApexionGame.Core.Samples.Editor/**` → `Packages/com.apexion.apexion-game/Samples~/ApexionGame.Core.Samples/**` (asmdef Editor nằm nested như cấu trúc hiện tại) | Cả hai asmdef đều nằm trong một folder `Samples~` |
| 12 | Chuyển module RTS stats (đổi tên, giữ live — **không** vào `Samples~`) | `Assets/ApexionGame/ApexionGame.Entities.Stats.Samples/**` (kèm `Documentation~/RTS-BATTLE-DESIGN.vi.md`) → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Samples.Rts/**` (`Rts.Core/`, `Rts.Game/` giữ nested) | Cả hai asmdef tồn tại, folder nằm ở root package (không dưới `Samples~/`) |
| 13 | Khai báo sample opt-in duy nhất trong manifest | `Packages/com.apexion.apexion-game/package.json` | Mảng `samples` có đúng 1 entry, từ bước 11 |
| 14 | Xoá folder nguồn đã trống | `Assets/ApexionGame/` (+ `.meta`) | Folder không còn tồn tại |
| 15 | Nối dây manifest | `Packages/manifest.json` | Có đủ 3 entry `"file:"`: `com.apexion.apexion-game`, `com.laicasaane.encosy-tower`, `com.laicasaane.encosy-tower.dev-tools` |
| 16 | Sửa các đường dẫn cũ hard-code | Xem mục [Các file đang tham chiếu đường dẫn cũ](#các-file-đang-tham-chiếu-đường-dẫn-cũ) | `grep` sạch, không còn hit `Assets/ApexionGame` cho các assembly đã chuyển (loại trừ các mention `Documentation~` nội bộ package) |
| 17 | Kiểm chứng trong Unity | — | Project mở không sinh lỗi compile mới; Package Manager → In Project hiện "Apexion Game" (embedded); cả hai sample import không lỗi |

Các bước 4–12 phải chạy qua `unity-cli-workflow` (hoặc đóng Unity khi thao tác) để file `.meta` di
chuyển đồng bộ cùng asset — di chuyển bằng filesystem thuần mà không mang `.meta` sẽ làm đứt mọi tham
chiếu GUID.

## Trạng thái

Đã lên plan — chưa bắt đầu. Chờ yêu cầu "implement" rõ ràng trước khi đụng vào file nào.

## Mục tiêu / Không phải mục tiêu

**Mục tiêu**
- Đóng gói `Assets/ApexionGame/*` thành một embedded UPM package độc lập, có cấu trúc giống EncosyTower.
- Giữ nguyên mọi GUID asset (scene/prefab tham chiếu vào type HFSM hay Entities.Stats vẫn hoạt động).
- Biến bề mặt sample HFSM thành thực sự opt-in, giống sample của EncosyTower — bề mặt RTS giữ live
  vì có test hồi quy phụ thuộc thật (xem mục Quyết định).
- Sửa luôn phần lệch giữa `manifest.json` / `packages-lock.json` cho cả 3 embedded package trong lúc đụng vào file này.

**Không phải mục tiêu**
- Khôi phục hoặc thiết kế nội dung cho các folder trống `Assets/Game/Game.Common` / `Game.Data` /
  `Game.Gameplay*` — ngoài phạm vi; được nêu riêng, không chặn migration này.
- Viết lại `CHANGELOG.md` / `CODING-CONVENTIONS.md` / `.editorconfig` ở root repo — vẫn đang là bản
  sao byte-identical từ template EncosyTower mà repo này khởi tạo từ đó — không đụng vào ở đây.
- Đổi tên bất kỳ assembly, namespace, hay type nào. `ApexionGame.Core`, `ApexionGame.Entities.Stats`,
  v.v. giữ nguyên tên; chỉ đổi vị trí folder sang dưới `Packages/`.
- Publish package ra ngoài (không `LICENSE.md`, không hướng dẫn install qua OpenUPM/git-URL — đây
  chỉ là embedded package nội bộ).
- Sửa lỗi `rootNamespace` của `ApexionGame.Entities.Stats.Samples.Rts.Core` (thiếu suffix `.Core`) —
  có từ trước, không liên quan tới việc chuyển này, giữ nguyên.

## Encosy module mapping

Không có — đây là việc tái cấu trúc repo/tooling thuần túy, không phải feature dùng runtime API của
EncosyTower. "Module" duy nhất liên quan là **convention đóng gói package** của chính EncosyTower
(cấu trúc embedded package, các ref optional gate qua `versionDefines`, `Samples~` +
`package.json.samples`), plan này sao chép lại về cấu trúc chứ không tự viết lại — xem reference
`setup` của skill `encosy-tower` để có bản convention chuẩn.

## Data model

Không áp dụng — migration này không tạo ra data type mới nào.

## Cấu trúc folder / namespace / file chính xác

(Xem cây folder trong bản tiếng Anh — giữ nguyên, không dịch code/path.)

### Package manifest

(Xem JSON trong bản tiếng Anh — giữ nguyên, không dịch code.)

`com.laicasaane.encosy-tower` là dependency thật trong `package.json` (không chỉ là gate
`versionDefines`) vì `ApexionGame.Core.asmdef` và `ApexionGame.Entities.Stats.asmdef` tham chiếu cứng
tới `EncosyTower.Core` một cách vô điều kiện. `com.unity.burst/collections/mathematics` cũng là tham
chiếu cứng trong các asmdef đó, giống chính cách `package.json` của `EncosyTower.Core` chỉ liệt kê 3
package Unity thật sự bắt buộc, còn mọi thứ optional thì gate qua `versionDefines`.

### `Packages/manifest.json` — entry cần thêm

(Xem JSON trong bản tiếng Anh — giữ nguyên, không dịch code.)

### Các file đang tham chiếu đường dẫn cũ

Grep `Assets/ApexionGame` trên toàn bộ `*.md` hiện đang hit, ngoài `Documentation~/*` của chính
package (sẽ tự di chuyển theo assembly ở bước 4/6/10, không cần sửa thêm gì ngoài việc đó):

- `.claude/CLAUDE.md` — mục "Project facts" nêu `Assets/ApexionGame/ApexionGame.Entities.Stats` là
  DOTS-free fork; mục "Where things live" trích `Assets/ApexionGame/ApexionGame.Entities.Stats/Documentation~/`.
- `.claude/skills/encosy-tower/references/setup.md`
- `.claude/skills/encosy-tower/references/module-map.md`
- `.claude/skills/encosy-tower/references/structure-and-naming.md`
- `.claude/skills/encosy-tower/references/ui-toolkit.md`
- `.claude/memory/apexion-entities-stats-is-dots-free-fork.md`
- `.claude/memory/gameplay-assembly-map.md`
- `.claude/memory/ui-is-csharp-not-uxml.md`
- `.codex/skills/encosy-tower/references/project-setup.md`
- `.codex/memory/MEMORY.md`

Mỗi hit được sửa đường dẫn `Assets/ApexionGame/...` thành đường dẫn tương ứng
`Packages/com.apexion.apexion-game/...`; phần văn bản xung quanh giữ nguyên.

## API surface

Không có — không đổi API public nào. Tên assembly, namespace, và signature type giữ nguyên; chỉ đổi
vị trí filesystem và metadata đóng gói.

## Quyết định

- **Phạm vi**: chỉ `Assets/ApexionGame/*` (lớp framework tái sử dụng) trở thành package.
  `Assets/Game/*` (chính game, hiện chỉ `Game.Input` có nội dung) vẫn nằm dưới `Assets/` và tiêu thụ
  cả `com.laicasaane.encosy-tower` và `com.apexion.apexion-game` — giống hệt cách repo EncosyTower
  giữ một `Assets/` host project mỏng quanh embedded package của chính nó.
- **Samples — kết quả tách ra, phát hiện giữa lúc thực thi**: `ApexionGame.Core.Samples(.Editor)`
  (HFSM playground) chuyển thành folder `Samples~/` opt-in như plan gốc. `ApexionGame.Entities.Stats.
  Samples.Rts` (`Rts.Core`, `Rts.Game`) thì **không** — chuyển nó vào `Samples~/` làm
  `ApexionGame.Tests.EditorMode` không resolve được tham chiếu tới
  `ApexionGame.Entities.Stats.Samples.Rts.Core`, vì Unity không compile bất cứ gì trong `Samples~/`,
  mà nhiều test (`RtsEffectTests`, `RtsGraphInvariantTests`, `RtsLayeringTests`, `RtsMatchFlowTests`)
  dùng type của nó trực tiếp. Nó giữ lại dạng module live, luôn compile ở root package — đúng cho code
  tham chiếu có test hồi quy, dù ban đầu tưởng "chỉ là sample."
- **Dọn manifest**: gộp luôn việc sửa 2 entry `file:` thiếu cho `com.laicasaane.encosy-tower` và
  `.dev-tools` vào cùng thay đổi này, vì đây là cùng loại lỗ hổng mà package mới sẽ lặp lại nếu không sửa.
- **Không có `LICENSE.md`**: package này không publish ra ngoài, khác với package MIT của EncosyTower
  — có file license sẽ gây nhiễu, sai lệch.
- **`README.md`/`CHANGELOG.md` là thật, không phải stub kiểu EncosyTower**: README/CHANGELOG nội bộ
  package của EncosyTower là placeholder redirect về root repo vì root đã có doc chuẩn. Repo này chưa
  có `README.md` ở root, còn `CHANGELOG.md` ở root vẫn là bản leftover chưa sửa từ EncosyTower — nên
  redirect về root ở đây sẽ trỏ vào chỗ không có gì hữu ích. Package sẽ có doc thật, tối giản của
  riêng nó; doc cấp root là việc riêng, để sau (xem mục Không phải mục tiêu).
- **Version bắt đầu từ `0.1.0`**, semver thường (không `-preview.N`) — package này không đi qua kênh
  preview công khai nào.
