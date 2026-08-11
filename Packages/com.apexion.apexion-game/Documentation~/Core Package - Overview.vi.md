# ApexionGame Core Package — Tổng quan

*[English](Core%20Package%20-%20Overview.md)*

> **Đã thay thế.** Plan này đã được thực thi — xem [`Package Setup - Overview.vi.md`](Package%20Setup%20-%20Overview.vi.md)
> để biết thực tế đã triển khai, gồm một điểm khác so với plan gốc ở đây: `ApexionGame.Entities.Stats.Samples.Rts`
> giữ lại dạng module live thay vì chuyển vào `Samples~/`, vì `ApexionGame.Tests.EditorMode` test hồi
> quy trực tiếp vào nó và Unity không compile bất cứ gì trong `Samples~/`. Giữ lại nội dung bên dưới
> để có bối cảnh yêu cầu gốc và quá trình ra quyết định.

## Trạng thái

| | |
|---|---|
| Giai đoạn | **Lên kế hoạch — chờ review** |
| Package id | `com.apexion.apexion-game` |
| Những gì package này chứa | `ApexionGame.Core` (+ `.Samples`, `.Samples.Editor`), `ApexionGame.Editor`, `ApexionGame.Entities.Stats` (+ `.Authoring`, `.Editor`, `.Samples`) |
| Những gì vẫn nằm trong `Assets/` | `ApexionGame.Tests.EditorMode` — không di chuyển, không sửa |
| Phụ thuộc | `com.laicasaane.encosy-tower` (embedded, không có entry trong manifest — đúng kiểu package này cũng sẽ dùng) |

---

## 1. Tóm tắt yêu cầu

Trong lúc dựng sample playground cho HFSM (`ApexionGame.Core.Samples` — HFSM - Roadmap.md phase 8),
người dùng yêu cầu setup sample **theo cách EncosyTower làm**: mỗi sample một asmdef, nằm trong
`Samples~/` của một package, import/gỡ được từ Unity Package Manager — giống hệt
`Packages/com.laicasaane.encosy-tower/Samples~/EncosyTower.Samples.*`.

Cơ chế đó chỉ tồn tại cho một UPM package thật — thư mục dấu ngã (`Samples~/`) bị Unity ẩn hoàn
toàn và không có nút import nếu không có `package.json` bên cạnh khai báo mục `"samples"`.
`ApexionGame.Core` hiện tại chỉ là code project thuần trong `Assets/ApexionGame/`, không phải
package, nên yêu cầu này đòi hỏi phải biến nó thành package trước.

Hai câu hỏi phạm vi đã được hỏi trực tiếp người dùng (`AskUserQuestion`) và đã có câu trả lời:

| Câu hỏi | Câu trả lời |
|---|---|
| Phạm vi package | **Toàn bộ họ `ApexionGame.*`** — `Core`, assembly `Editor` dùng chung, và `Entities.Stats` (+ các sibling Authoring/Editor/Samples của nó) đều chuyển vào cùng một package, không chỉ riêng `Core`. Đây là lựa chọn lớn hơn trong hai phương án được đưa ra; người dùng chọn nó dù biết nó đụng tới cả module `Entities.Stats` đã test xong (95/95) chứ không riêng HFSM (phase 1–7, tests đã xanh). |
| Package id | **`com.apexion.apexion-game`** — câu trả lời tự do của người dùng, không phải hai gợi ý ban đầu. |

Vì *toàn bộ* họ module chuyển cùng lúc, vấn đề đã nêu ở câu hỏi đầu — `MachineDebuggerWindow` nằm
trong assembly `ApexionGame.Editor` *dùng chung* với `Entities.Stats` — không còn tồn tại:
`ApexionGame.Editor` không cần tách, cứ chuyển nguyên như hiện có.

---

## 2. Kết quả mong đợi

### 2.1 Cây thư mục khi hoàn tất

```
Packages/com.apexion.apexion-game/
├── package.json                              mới — name, version, unity, dependencies, samples[]
├── ApexionGame.Core/                          chuyển từ Assets/ApexionGame/ApexionGame.Core/ — giữ nguyên
│   ├── ApexionGame.Core.asmdef
│   ├── Documentation~/                        HFSM - *.md, chuyển nguyên
│   └── HFSM/
├── ApexionGame.Editor/                        chuyển từ Assets/ApexionGame/ApexionGame.Editor/ — giữ nguyên
├── ApexionGame.Entities.Stats/                chuyển — giữ nguyên
├── ApexionGame.Entities.Stats.Authoring/      chuyển — giữ nguyên
├── ApexionGame.Entities.Stats.Editor/         chuyển — giữ nguyên
└── Samples~/                                  mới — Unity ẩn hoàn toàn cho tới khi import
    ├── ApexionGame.Core.Samples/              chuyển+đổi tên từ Assets/.../ApexionGame.Core.Samples/
    │   ├── ApexionGame.Core.Samples.asmdef
    │   ├── Enemy/**
    │   ├── Scenes/hfsm-playground.unity
    │   └── Editor/                            lồng vào trong — trước là sibling .Samples.Editor riêng
    │       ├── ApexionGame.Core.Samples.Editor.asmdef
    │       └── MachinePlaygroundEditor.cs
    └── ApexionGame.Entities.Stats.Samples/    chuyển+đổi tên từ Assets/.../ApexionGame.Entities.Stats.Samples/

Assets/ApexionGame/
└── ApexionGame.Tests.EditorMode/              giữ nguyên — asmdef reference theo TÊN assembly,
                                                không theo đường dẫn, nên không cần sửa gì ở đây
```

Không có file `.meta` nào tồn tại dưới `Samples~/` — Unity bỏ qua hoàn toàn thư mục dấu ngã, nên
`.meta` ở đó là thừa. Mọi `.meta` đi cùng một assembly *sống* khi di chuyển (`Core`, `Editor`,
`Entities.Stats*`) giữ nguyên GUID gốc nhờ `git mv`, nên mọi tham chiếu hiện có (asmdef theo tên,
scene/prefab theo GUID) vẫn resolve đúng.

### 2.2 `package.json` (bản phác)

```json
{
    "name": "com.apexion.apexion-game",
    "displayName": "Apexion Game",
    "version": "0.1.0",
    "unity": "6000.3",
    "dependencies": {},
    "description": "Reusable ApexionGames runtime: hierarchical state machine, DOTS-free entity stats, and the shared editor tooling for both.",
    "samples": [
        {
            "displayName": "ApexionGame.Core.Samples",
            "description": "HFSM playground: EnemyBrain demo, ten scenario buttons, hand-authored scene.",
            "path": "Samples~/ApexionGame.Core.Samples"
        },
        {
            "displayName": "ApexionGame.Entities.Stats.Samples",
            "description": "RTS stats demo.",
            "path": "Samples~/ApexionGame.Entities.Stats.Samples"
        }
    ]
}
```

`dependencies` để trống thay vì khai `com.laicasaane.encosy-tower`: package đó cũng đang embedded,
không có version để registry resolver kiểm tra, và `Packages/manifest.json` đã có nó rồi — cần kiểm
chứng thực tế ở bước 5 xem Unity's resolver chấp nhận một entry tương tự ở đây hay chỉ cảnh báo;
dù thế nào cũng không chặn compile.

### 2.3 Các phép kiểm tra chứng minh nó hoạt động

| # | Kiểm tra | Cách chạy |
|---|---|---|
| C1 | Package hiện ra trong Package Manager như một embedded package, không cần sửa `Packages/manifest.json` | mở Package Manager → In Project, hoặc `unity command … package_list` |
| C2 | Toàn project compile, 0 lỗi 0 warning | `unity command … recompile` + `recompile_status` |
| C3 | `ApexionGame.Tests.EditorMode` — ~90 test của HFSM và 95 test của Stats — vẫn pass, **không di chuyển**, chỉ tham chiếu package qua tên assembly | `unity test . --mode EditMode` |
| C4 | Tab **Samples** của Package Manager liệt kê cả hai sample với nút **Import** hoạt động, mỗi sample import vào `Assets/Samples/Apexion Game/<version>/<name>/` | thủ công, trong Editor |
| C5 | `ApexionGame > HFSM > Debugger` và cửa sổ debugger của Stats vẫn mở và hoạt động sau khi `ApexionGame.Editor` di chuyển | thủ công |
| C6 | Scene của sample HFSM đã import (`hfsm-playground.unity`) mở được và cả mười nút vẫn chạy, từ bản đã import — không phải từ thư mục package gốc | thủ công, sau C4 |

---

## 3. Các bước

Theo thứ tự phụ thuộc. Mỗi lần di chuyển là **`git mv` cấp thư mục**, không bao giờ tạo lại từ đầu
— đó là điều giữ nguyên GUID của mọi `.meta` và giữ mọi tham chiếu hiện có tiếp tục resolve đúng.

| # | Hành động | Xong khi | File/đường dẫn |
|---|---|---|---|
| 1 | `git mv Assets/ApexionGame/ApexionGame.Core Packages/com.apexion.apexion-game/ApexionGame.Core` (và `.meta` của nó) | thư mục tồn tại ở đường dẫn mới, giữ nguyên history | `ApexionGame.Core*` |
| 2 | `git mv Assets/ApexionGame/ApexionGame.Editor Packages/com.apexion.apexion-game/ApexionGame.Editor` (và `.meta`) | tương tự | `ApexionGame.Editor*` |
| 3 | `git mv` từng thư mục `ApexionGame.Entities.Stats`, `.Authoring`, `.Editor` vào root package | tương tự | `ApexionGame.Entities.Stats*` (không gồm `.Samples`) |
| 4 | Tạo `Packages/com.apexion.apexion-game/package.json` theo §2.2 | file tồn tại, JSON hợp lệ | `package.json` |
| 5 | Kiểm tra `Packages/manifest.json` có cần entry không (embedded-package tự phát hiện, giống `com.laicasaane.encosy-tower` hiện tại) — chỉ thêm entry nếu Unity Package Manager không tự liệt kê | `unity command … package_list` hiện package mới mà không cần sửa manifest, hoặc ghi lại chỗ sửa tối thiểu tại đây | `Packages/manifest.json` (nhiều khả năng không đổi) |
| 6 | `git mv Assets/ApexionGame/ApexionGame.Core.Samples Packages/com.apexion.apexion-game/Samples~/ApexionGame.Core.Samples`, sau đó `git mv .../ApexionGame.Core.Samples.Editor` **vào trong** thư mục đó thành `Editor/` (asmdef lồng vào, không phải sibling) | file nằm dưới `Samples~/`, asmdef editor lồng sâu hơn một cấp | `ApexionGame.Core.Samples*` |
| 7 | Xoá mọi file `.meta` dưới cây `Samples~/ApexionGame.Core.Samples/` mới (kể cả meta của thư mục) — thư mục dấu ngã không có meta nào cả | `git status` chỉ hiện xoá file `.meta` trong cây đó | `Samples~/ApexionGame.Core.Samples/**/*.meta` |
| 8 | `git mv Assets/ApexionGame/ApexionGame.Entities.Stats.Samples Packages/com.apexion.apexion-game/Samples~/ApexionGame.Entities.Stats.Samples`, rồi xoá `.meta` như bước 7 | cùng khuôn mẫu bước 6–7 | `ApexionGame.Entities.Stats.Samples*` |
| 9 | Xác nhận `Assets/ApexionGame/ApexionGame.Tests.EditorMode/ApexionGame.Core.Tests.asmdef` mảng `"references"` vẫn ghi `"ApexionGame.Core"` / `"ApexionGame.Entities.Stats"` (theo tên) — kỳ vọng **không cần sửa** | diff của asmdef rỗng | `ApexionGame.Tests.EditorMode/**/*.asmdef` |
| 10 | Compile qua Editor đang mở sẵn (đã mở từ trước — attach qua Pipeline, không chạy batch process cạnh tranh) | C2 đạt | — |
| 11 | Chạy bộ test EditMode của `ApexionGame.Tests.EditorMode` | C3 đạt | — |
| 12 | Mở Package Manager → In Project → Apexion Game → tab Samples; import cả hai sample để kiểm thử, xác nhận C4–C6 | C4, C5, C6 đạt | — |
| 13 | Sửa các liên kết chéo trong `Documentation~/` của hai module nếu còn trỏ tới đường dẫn cũ `Assets/ApexionGame/...` (kiểm tra bảng vị trí assembly ở HFSM - Layout.md §2, hiện đang ghi `Assets/ApexionGame/ApexionGame.Core/`) | đường dẫn trong doc khớp cây mới | `ApexionGame.Core/Documentation~/HFSM - Layout.md` và bản `.vi.md` |

---

## 4. Mục tiêu và không phải mục tiêu

### 4.1 Mục tiêu

| | |
|---|---|
| G1 | Sample HFSM (và sample Stats) trở thành sample thật của Package Manager — import/gỡ từ Package Manager, giống hệt `EncosyTower.Samples.*`. |
| G2 | `ApexionGame.Tests.EditorMode` vẫn pass mà **không sửa một dòng code nào** — chứng minh việc resolve asmdef theo tên khiến việc di chuyển an toàn. |
| G3 | Mọi GUID sống sót qua lần di chuyển (`git mv`, không tạo lại), nên không tham chiếu scene/asmdef/prefab nào bị vỡ. |

### 4.2 Không phải mục tiêu

| | Lý do |
|---|---|
| Publish lên UPM registry / install qua git URL | Không được yêu cầu; đây là package *local, embedded*, cùng tầng với `com.laicasaane.encosy-tower` hiện tại. |
| Tách `ApexionGame.Editor` theo từng module | Bị từ chối rõ ràng — người dùng chọn chuyển nguyên assembly dùng chung thay vì tách riêng debugger của HFSM ra. |
| CHANGELOG.md / kỷ luật semver cho package mới | Không được yêu cầu; `version: 0.1.0` chỉ là placeholder, không phải cam kết. |

---

## 5. Quyết định

- **Package id là `com.apexion.apexion-game`**, câu trả lời tự do của người dùng — không phải
  `com.apexiongames.core` hay `com.apexiongame.core` như đã gợi ý. Lưu ý id là `apexion-game`
  (có gạch nối), khác với `ApexionGame` (không gạch nối) dùng xuyên suốt tên assembly/namespace
  hiện có; không có gì ở phía C# bị đổi tên, chỉ folder/id của package bị ảnh hưởng.
- **Toàn bộ họ `ApexionGame.*` vào một package**, không riêng `Core` — được chọn thay cho phương án
  hẹp hơn, rủi ro thấp hơn, sau khi đã nói rõ đánh đổi (đụng cả `Entities.Stats`). Hệ quả trực tiếp:
  `ApexionGame.Editor` không cần tách.
- **`ApexionGame.Tests.EditorMode` vẫn ở `Assets/`** — đây là assembly test cấp project, đã chứa
  cả test của HFSM và Stats cạnh nhau; asmdef reference resolve theo tên assembly bất kể assembly đó
  nằm ở `Assets/` hay `Packages/`, nên không cần sửa đường dẫn hay reference, chỉ cần chạy lại test
  sau khi di chuyển để xác nhận.
- **Tên thư mục trong Samples~ khớp tên asmdef** (`Samples~/ApexionGame.Core.Samples/`,
  `Samples~/ApexionGame.Entities.Stats.Samples/`), giống chính xác `EncosyTower.Samples.*`.
  `ApexionGame.Core.Samples.Editor` lồng thành `Samples~/ApexionGame.Core.Samples/Editor/` thay vì
  một entry `Samples~` riêng, để import một sample là mang theo cả nút bấm editor trong cùng một
  lần copy — asmdef lồng trong thư mục sample được copy và compile cùng nhau khi import.

---

## 6. Rủi ro

| # | Rủi ro | Cách giảm thiểu |
|---|---|---|
| R1 | Tạo lại thay vì di chuyển sẽ làm vỡ âm thầm mọi tham chiếu GUID. | Mọi bước là `git mv` cấp thư mục; không file nào bị viết lại từ đầu. |
| R2 | File `.meta` cũ còn sót dưới `Samples~/` khiến người sau tưởng nó có ý nghĩa. | Bước 7/8 xoá hẳn, không chỉ "không thêm mới". |
| R3 | `EncosyTower.Core.Extended`, dùng bởi `MachineOverlayCommand` trong sample HFSM, không có dependency nào được khai khi package này dùng ở một project *khác*. | Tạm gác lại: `dependencies` trong `package.json` để trống theo §2.2, chờ kiểm tra manifest ở bước 5; xem lại nếu sample từng được import vào project không có EncosyTower. |
| R4 | Editor đang mở sẵn cho đúng project này (đã xác nhận: PID 20572). Không được chạy một Unity batch-mode process cạnh tranh trong lúc di chuyển. | Attach qua các lệnh Pipeline (`recompile`, `recompile_status`) như đã dùng trong session này; không gọi `Unity.exe -batchmode` nhằm vào đường dẫn project này. |
