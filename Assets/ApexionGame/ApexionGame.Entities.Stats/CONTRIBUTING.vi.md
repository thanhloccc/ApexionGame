# Góp code

*[English](CONTRIBUTING.md)*

Issue và pull request đều hoan nghênh, bằng tiếng Việt hoặc tiếng Anh.

## Trước khi sửa runtime

Đọc [DEC-005](Documentation~/07-DECISIONS.md#dec-005) trước. Vòng lan truyền **cố ý không có
visited-set**, và lý do đi ngược trực giác: một stat quay lại worklist không phải lãng phí, nó chính
là cơ chế hội tụ. Thêm visited-set lại là cách dễ nhất để làm thư viện này sai trong khi mọi test
hiển nhiên vẫn xanh.

[Decision log](Documentation~/07-DECISIONS.md) ghi mọi chỗ lệch so với bản gốc kèm lý do. Nếu thay đổi
của bạn thêm một chỗ lệch nữa, thêm một mục vào đó.

## Cấu trúc

| Đường dẫn | Chứa gì |
|---|---|
| `ApexionGame.Entities.Stats/` | runtime — lớp generic viết tay |
| `ApexionGame.Entities.Stats/Generators/` | codegen editor cho bảng kiểu (`*.gen.cs`) |
| `ApexionGame.Entities.Stats/SourceGenerators/` | DLL Roslyn build sẵn, do MSBuild deploy vào |
| `ApexionGame.Entities.Stats.Authoring/` | authoring bằng `ScriptableObject` |
| `ApexionGame.Entities.Stats.Editor/` | cửa sổ Stat Debugger |
| `ApexionGame.Entities.Stats.Samples/` | sample chạy được + cửa sổ playground |
| `ApexionGame.Entities.Stats.Tests/` | 95 test, chỉ Editor |
| `Plugins/SourceGenerator.ApexionGame/` | solution Roslyn — generator, analyzer, refactoring, test |

Chi tiết ở [05-ASSEMBLY-LAYOUT.md](Documentation~/05-ASSEMBLY-LAYOUT.md).

## Build generator

Các project Roslyn nằm trong solution riêng và không build cùng Unity. Một MSBuild target copy output
vào `ApexionGame.Entities.Stats/SourceGenerators/` kèm đúng asset label, nên chỉ cần build thường:

```powershell
dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release
```

Unity nhận DLL mới ở lần domain reload kế tiếp. Xem [04-CODEGEN.md](Documentation~/04-CODEGEN.md) để
debug generator trên một compilation thật.

## Test

Ba bộ, và một thay đổi ở runtime hay generator phải giữ cả ba xanh:

```powershell
# 1. Runtime — Unity, không cần mở Editor
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Tests"

# 2. Generator và analyzer — ngoài Unity
dotnet test Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Tests

# 3. Sample, compile qua generator thật, ngoài Unity.
#    Đây là chốt chặn hồi quy cho cách dùng đã ghi trong tài liệu.
dotnet build Plugins/SourceGenerator.ApexionGame/Samples/Samples.RpgStats/Samples.RpgStats.csproj
```

Hai điều cần biết về bộ 1 và 2:

- Test runner của Unity **loại test `[Explicit]` kể cả khi gọi đích danh trong `--filter`**. Đó là lý
  do benchmark không mang `[Explicit]` — cả class chạy chưa tới 2 giây.
- `dotnet test` in `Passed!` kể cả khi test trả về `Inconclusive`. Đọc số skip, đừng đọc dòng banner.

Đặt biến môi trường `UNITY_OS_INSTALL_ROOT` trỏ tới thư mục cài Unity một lần là lệnh `unity test` ở
trên chạy được từ bất kỳ shell nào.

## Quy ước code

`.editorconfig` ở gốc repo là chuẩn, và
[CODING-CONVENTIONS.md](../../../CODING-CONVENTIONS.md) giải thích những quy ước mà formatter không
kiểm được. Ba điều quan trọng nhất ở đây:

- Mọi thứ trong core là `unmanaged`. Không `class`, không delegate, không managed field trên hot path.
- API public fail qua `bool TryXxx(...)`; validation nằm trong `ThrowHelper` có `[Conditional]` nên
  biến mất ở release build.
- Code sinh mới theo đúng khuôn `Spec` + `WriteCode` hiện có, để diff so với generator của EncosyTower
  còn đọc được.

## Về commit và PR

- Một thay đổi logic một commit.
- Nếu sửa output của generator, ghi rõ diff của code sinh ra trông thế nào — reviewer không thấy được.
- Nếu bạn vá một lỗi mà bản gốc cũng có, ghi lại, để có thể đề xuất ngược lên upstream.
