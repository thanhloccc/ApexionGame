# Hướng dẫn

Tài liệu sử dụng cho `ApexionGame.Entities.Stats`.

*[English](../README.md) · [README package](../../../README.vi.md) · [Tài liệu thiết kế](../../README.md)*

## Thứ tự đọc

| # | Trang | Trả lời |
|---|---|---|
| 1 | [Bắt đầu](01-GETTING-STARTED.md) | Cài gì, và thứ nhỏ nhất chạy được là gì? |
| 2 | [Khái niệm cốt lõi](02-CONCEPTS.md) | Ở đây một stat là gì, và một lần ghi đi tới các stat phụ thuộc bằng cách nào? |
| 3 | [Khai báo stat](03-DECLARING-STATS.md) | Ba attribute sinh ra gì, và hook nào tôi phải tự viết? |
| 4 | [Dùng stat](04-USING-STATS.md) | Tạo owner, đọc, ghi, thêm/gỡ modifier, tiêu thụ event, destroy owner. |
| 5 | [Lưu / tải](05-PERSISTENCE.md) | Save, load, và lượt remap giữ liên kết chéo owner còn sống. |
| 6 | [Job & hiệu năng](06-JOBS-AND-PERFORMANCE.md) | Cái gì thread-safe, schedule thế nào, tốn bao nhiêu. |
| 7 | [Tooling](07-TOOLING.md) | Stat Debugger, Quick Actions, authoring asset, sinh lại bảng kiểu. |
| 8 | [API reference](08-API-REFERENCE.md) | Bề mặt theo từng type, viết tay và sinh ra, kèm bảng chẩn đoán. |
| 9 | [Chỗ dễ vấp & FAQ](09-PITFALLS.md) | Những lỗi im lặng. |

Ít thời gian: đọc [Khái niệm cốt lõi](02-CONCEPTS.md), rồi chạy
[sample](../../../../ApexionGame.Entities.Stats.Samples/README.vi.md) và bấm qua mười case của nó. Hai
thứ đó phủ phần lớn những gì các trang còn lại nói chi tiết.

## Nếu bạn đến từ bản gốc

API giống [`EncosyTower.Entities.Stats`][encosy] và [Trove Stats][trove] đủ để phần lớn code port
được chỉ bằng cách đổi tên kiểu. Chỗ khác thật sự:

| Thay đổi | Đụng ở đâu |
|---|---|
| `Entity` → `StatOwnerHandle`, `DynamicBuffer<T>` → `StatBuffer<T>` | chữ ký, mọi nơi |
| `StatBaker` → `StatBuilder`; không có baking pipeline | lúc tạo owner |
| `IStatModifier` thêm `RemapObservedStats` | modifier viết cho bản gốc **không compile** cho tới khi thêm hook |
| Lan truyền không có visited-set | không phải sửa gì, nhưng xem [DEC-005](../../07-DECISIONS.md#dec-005) |
| API batch nhận thêm `Allocator` (mặc định `Temp`) | tham số tuỳ chọn ở cuối |
| `Accessor.ReadOnly` bỏ `ComponentLookup<StatOwner>` | nó chỉ tồn tại để đăng ký read-dependency với ECS |

Danh sách đầy đủ — 23 chỗ lệch, mỗi chỗ kèm lý do — ở [decision log](../../07-DECISIONS.md).

[trove]: https://github.com/PhilSA/Trove
[encosy]: https://github.com/laicasaane/EncosyTower
