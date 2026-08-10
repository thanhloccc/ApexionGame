# ApexionGame.Core — Tài liệu

*[English](README.md)*

Runtime dùng chung, không gắn với một game cụ thể, cho các project của ApexionGames. Mọi thứ ở đây
dựng trên **EncosyTower** (`Packages/com.laicasaane.encosy-tower`) và được thiết kế để sống lâu hơn
tựa game hiện tại.

## Module

| Module | Namespace | Trạng thái | Tài liệu |
|---|---|---|---|
| `HFSM/` — máy trạng thái hữu hạn phân cấp | `ApexionGame.HFSM` | **Chờ duyệt** | [HFSM - Overview](HFSM%20-%20Overview.vi.md) |

## HFSM — thứ tự đọc

| # | File | Đọc khi |
|---|---|---|
| 1 | [HFSM - Overview](HFSM%20-%20Overview.vi.md) | luôn đọc đầu tiên — yêu cầu, kết quả mong đợi, bảng bước |
| 2 | [HFSM - API Surface](HFSM%20-%20API%20Surface.vi.md) | muốn thấy người dùng viết cái gì |
| 3 | [HFSM - Flows](HFSM%20-%20Flows.vi.md) | cần thuật toán transition, luật thứ tự, ngữ nghĩa parallel/history/async |
| 4 | [HFSM - Data Model](HFSM%20-%20Data%20Model.vi.md) | cần danh sách type, collection chọn cho từng type, và ngân sách bộ nhớ |
| 5 | [HFSM - Layout](HFSM%20-%20Layout.vi.md) | đang tạo file hoặc asmdef |
| 6 | [HFSM - Debugging](HFSM%20-%20Debugging.vi.md) | đang dựng bốn mặt debug |
| 7 | [HFSM - Decisions](HFSM%20-%20Decisions.vi.md) | không đồng ý điều gì đó và muốn biết vì sao nó như vậy |
| 8 | [HFSM - Roadmap](HFSM%20-%20Roadmap.vi.md) | đang lên kế hoạch hoặc thực thi một phase |

Mỗi file đều có bản `.vi.md` giữ đồng bộ từng mục một.

## Quy ước

- Style: `CODING-CONVENTIONS.md` ở gốc repo.
- Kiến trúc và chọn module: `.claude/skills/encosy-tower/`.
- Thao tác Unity (compile, test, build): `.claude/skills/unity-cli-workflow/`.
- Bộ `ApexionGame.Entities.Stats/Documentation~/` bên cạnh là tham chiếu cho văn hoá tài liệu của
  project này — ở đây kỳ vọng cùng mức chi tiết.
