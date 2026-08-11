# ApexionGame.Entities.Stats — Design Documentation

> Bộ tài liệu thiết kế cho stat system non-ECS, port từ `EncosyTower.Entities.Stats`
> (bản thân EncosyTower port từ **Trove Stats** — MIT, © 2023 Philippe St-Amand).

> 📖 **Tìm cách dùng thư viện, không phải cách nó được thiết kế?**
> → [`guide/`](guide/README.md) ([tiếng Việt](guide/vi/README.md)) — tài liệu sử dụng công khai:
> cài đặt, khái niệm, codegen, modifier, lưu/tải, job, tooling, API reference, chỗ dễ vấp.
> Thư mục hiện tại là *nhật ký thiết kế*: nó ghi vì sao mọi thứ như thế, không phải cách dùng.

## Trạng thái

| | |
|---|---|
| Phase | **Design đã chốt** — sẵn sàng code (DEC-001..004 đã quyết, DEC-005 để mặc định) |
| Target assembly | `ApexionGame.Entities.Stats` |
| Target namespace | `ApexionGame.Entities.Stats` |
| Unity | 6000.3.20f1, C# 10 (`csc.rsp: -langversion:10`) |
| Runtime deps | `Unity.Collections`, `Unity.Mathematics`, `Unity.Burst`, **`EncosyTower.Core`** |
| Editor-only dep | `AnnulusGames.UnityCodeGen.Editor` (sinh bảng kiểu `.gen.cs`, giống bản gốc) |
| **Không** phụ thuộc | `Unity.Entities`, `Unity.Entities.Hybrid`, `Latios.Core` |
| Identity type | `StatOwnerHandle { int Index; int Version; }` |
| Roslyn codegen | solution riêng `Plugins/SourceGenerator.ApexionGame/`, không `ProjectReference` sang EncosyTower |

## Thứ tự đọc

| # | File | Nội dung |
|---|---|---|
| 1 | [01-OVERVIEW.md](01-OVERVIEW.md) | Mục tiêu, non-goals, nguyên tắc port, bảng mapping ECS → non-ECS |
| 2 | [02-ARCHITECTURE.md](02-ARCHITECTURE.md) | Data model, storage, thuật toán update/modifier, threading, Burst, lifecycle |
| 3 | [03-API-SURFACE.md](03-API-SURFACE.md) | Public API dự kiến — hand-written layer + generated layer + ví dụ end-to-end |
| 4 | [04-CODEGEN.md](04-CODEGEN.md) | Source generator riêng: solution, generators, analyzers, deploy vào Unity |
| 5 | [05-ASSEMBLY-LAYOUT.md](05-ASSEMBLY-LAYOUT.md) | Cây thư mục, asmdef, define symbols, chiến lược phụ thuộc EncosyTower |
| 6 | [06-ROADMAP.md](06-ROADMAP.md) | Phase, deliverable, test plan, risk |
| 7 | [07-DECISIONS.md](07-DECISIONS.md) | Decision log — DEC-001..004 đã chốt, 9 quyết định thiết kế, 7 sai lệch skeleton cần sửa |

## TL;DR của thiết kế

Toàn bộ giá trị của EncosyTower stats nằm ở **thuật toán** (đồ thị observer, modifier
range, chống vòng lặp vô hạn, lan truyền thay đổi) — không nằm ở ECS. ECS chỉ cung cấp
**4 primitive về lưu trữ**:

```
Entity                  →  identity của một chủ thể mang stats
DynamicBuffer<T>        →  danh sách growable per-chủ-thể
BufferLookup<T>         →  Entity → DynamicBuffer<T>
ComponentLookup<T>      →  Entity → component (StatOwner.modifierIdCounter)
```

Bản port thay đúng 4 thứ đó bằng một `StatStore` tự viết trên `UnsafeList<T>*`:

```
StatOwnerHandle (index+version)  →  identity
StatBuffer<T>              →  wrapper quanh UnsafeList<T>*  (API ≡ DynamicBuffer<T>)
StatStore<TStat,TMod,TObs> →  lookup + owner slot + free-list + modifierIdCounter
```

Sau khi có seam đó, `StatAPI` / `StatAccessor` / `StatReader` / `StatWorldData` port
gần như **nguyên xi** — cùng thuật toán, cùng invariant, cùng thứ tự sắp xếp buffer.
Source generator cũng chỉ đổi tên type ở phần emit, giữ nguyên khung.

## Ranh giới với EncosyTower

| | |
|---|---|
| **Runtime** | hard-ref `EncosyTower.Core` — dùng `ByteBool`, `Option<T>`, `HashValue`, `IIsValid`, collection extensions, `[WrapType]`, `[EnumExtensions]` |
| **Roslyn generator** (chạy mỗi lần compile) | **độc lập hoàn toàn** — solution riêng, DLL riêng, không `ProjectReference` sang `EncosyTower.SourceGen.*`, vendoring dạng snapshot |
| **Editor codegen bảng kiểu** (chạy tay khi đổi bảng) | **theo tác giả** — UnityCodeGen + `EncosyTower.CodeGen.Printer`, output commit vào repo |
| **Validation defines** | riêng (`APEXION_*`), không dùng `ENCOSY_*` |

Nguyên tắc: *type và editor-tool thì dùng chung được, Roslyn generator thì không* — vì chỉ
Roslyn generator mới quyết định API public của code sinh ra lúc compile.
Đã verify cơ chế skip-attribute của EncosyTower là **theo từng area** nên hai hệ generator
chạy song song không giao thoa —
[§5.3.1](05-ASSEMBLY-LAYOUT.md#531-cơ-chế-skip-attribute--đã-verify-không-có-xung-đột).

## Việc tiếp theo

> ⚠ **Còn một bước dọn dẹp:** define `APEXION_STAT_VALUE_TYPES_GENERATOR` vẫn đang bật trong
> `ProjectSettings`. Bỏ nó đi (bước 3 của [§4.6.6](04-CODEGEN.md#466-workflow-đổi-bảng-kiểu)),
> nếu không 7 file trong `Generators/` sẽ compile vào assembly ở mọi lần build.

Xong: task **1.0** (skeleton), **1.7/1.8** (storage seam + 30 unit test), **1.1** (bảng kiểu +
6 editor generator).

**Phase 1 đã đóng.** Toàn bộ L1 + L2 xong, assembly build 0 warning / 0 error, 61 unit test
(`StatBuffer` 15, `StatStore` 12, `StatVariant` 22, `StatHandle` 12). Ba type dời sang phase 2
theo [I-09](07-DECISIONS.md#i-09--istatmodifier--2-type-kéo-theo-dời-sang-phase-2).

**Phase 2 đã xong 2.0–2.3** — cụm phụ thuộc vòng `StatWorldData` → `ModifierTriggerEvent` →
`IStatModifier` → `StatReader` → `StatAPI` → `StatWorldData`. Assembly xanh, 61 test vẫn build sạch.

**Phase 2 đã đóng** — toàn bộ L3 xong (2.0 → 2.8), assembly build 0 warning / 0 error,
61 test phase 1 vẫn sạch.

**Golden test đã viết** — 22 test phủ 13 scenario, dùng bộ type tay `TestStatSystem.cs` đóng vai
code sinh ra của phase 3. Tổng cộng **83 test** trong assembly test.

⚠ **Chưa ai chạy chúng.** Test runner cần Unity Editor; ở đây mới chỉ compile sạch. Việc tiếp theo
là mở Test Runner và chạy — cho tới lúc đó phase 2 **chưa** đóng.

**Phase 3 đã dựng xong hạ tầng** (3.0–3.5): `Plugins/SourceGenerator.ApexionGame/` với 4 project
build sạch, vendoring 20 file `SourceGen.Common`, `Samples.Entities.Stats`, `launchSettings.json`
để debug generator, và ba attribute `Annotations/` (vốn bị bỏ quên — [I-21](07-DECISIONS.md#i-21--annotations-không-thuộc-task-nào-của-phase-1)).

**Cả ba generator đã xong và đã chạy thật** (3.6, 3.7, 3.8) — không chỉ compile: chạy qua
`CSharpGeneratorDriver` trên compilation tham chiếu `ApexionGame.Entities.Stats.dll` thật.
Với `[StatSystem]` + `[StatCollection]` 3 stat: **5 file, 5774 dòng**, và compilation sau khi
sinh **không còn lỗi thật** (8 lỗi còn lại là artifact của harness net10, xem
[06-ROADMAP.md](06-ROADMAP.md#38--statcollectiongenerator)).

**Phase 3 đã đóng.** Exit criteria đạt đủ:

| | |
|---|---|
| `dotnet build … -c Release` | ✅ xanh, 4 DLL đã deploy kèm `.meta` đúng label |
| `Samples.Entities.Stats` compile sạch ngoài Unity | ✅ 0 lỗi |
| `dotnet test` | ✅ **17 pass, 0 skip** — chỉ đúng sau khi sửa hai lỗi ở phase 4 ([chi tiết](06-ROADMAP.md#hai-lỗi-làm-14-test-tích-hợp-chưa-từng-chạy)); trước đó là 3 pass + 14 `Inconclusive` mà `dotnet test` vẫn in `Passed!` |
| `ApexionGame.Entities.Stats` sau khi deploy | ✅ 0 lỗi — skip-attribute chặn generator đúng như thiết kế |

**Phase 4 đã xong phần code.** 4.1 persistence, 4.2 authoring, 4.5 D-01, 4.6 allocator,
4.7 `README.md` + `CHANGELOG.md` công khai ở gốc package, 4.8 sample chạy được. 4.3 và 4.4
code xong nhưng **kết quả phải chạy trong Unity mới có** (Burst Inspector, benchmark).

Kết quả đáng chú ý nhất của phase 4 là **4.5 chốt ngược với kế hoạch**: visited-set trong
`TryUpdateStat` của bản gốc không phải tối ưu mà là **lỗi đúng/sai** — trên DAG lệch tầng nó
chặn đúng lượt tính lại có nhiệm vụ sửa giá trị, để lại kết quả sai vĩnh viễn. Bản port bỏ
visited-set ở cả hai lối lan truyền. Số đo đầy đủ ở [DEC-005](07-DECISIONS.md#dec-005).

**Test đã chạy và đã xanh** (2026-08-04, Unity 6000.3.20f1, qua Unity CLI ở chế độ batch):
toàn project **928 pass / 0 fail / 0 skip**, riêng `ApexionGame.Entities.Stats.Tests` là
**95 pass** (90 chức năng + 5 benchmark). Benchmark có số thật — xem
[4.4](06-ROADMAP.md#số-đo-thật-đã-chạy). Lệnh và ba cái bẫy của CLI ghi ở
[phần đầu 06-ROADMAP](06-ROADMAP.md#chạy-test-unity-không-cần-mở-editor).

**Burst đã xác nhận** (4.3): ba job sinh ra compile ra mã máy trong Burst Inspector; ba job
generic mở bị bỏ qua đúng như thiết kế. Chi tiết ở
[4.3](06-ROADMAP.md#kết-quả-burst-inspector-2026-08-04).

**Phase 0–5 đã đóng.** Phase 5 gồm: `StatDebugRegistry` + cửa sổ *Stat Debugger* (5.1), đồ thị
observer vẽ bằng `Painter2D` với phát hiện chu trình (5.2), và ba code refactoring sinh skeleton
`[StatSystem]`/`[StatCollection]` (5.3, 9 test). Chi tiết ở
[phase 5](06-ROADMAP.md#phase-5--tooling-).

Còn một việc chưa ai nhìn tận mắt: ba cửa sổ UI Toolkit (*Stats Playground*, *Stat Debugger*) và
scene `stats-playground.unity` mới chỉ được **compile**, chưa được **render** lần nào.

Đặt biến môi trường một lần cho tiện: `UNITY_OS_INSTALL_ROOT` trỏ tới thư mục cài Unity.
