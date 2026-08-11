# 07 — Decision Log

Trạng thái: **DEC-001..005 đã chốt** (2026-08-04).
DEC-005 chốt ngược với kế hoạch ban đầu — xem phần đo đạc ở dưới.

## DEC-001 — Identity type: `StatOwnerHandle` ✅

```csharp
public readonly struct StatOwnerHandle : IEquatable<StatOwnerHandle>, IIsValid
{
    public static readonly StatOwnerHandle Null = default;

    public readonly int Index;     // slot trong StatStore
    public readonly int Version;   // > 0 khi sống; tăng mỗi lần slot tái sử dụng

    public bool IsValid => Version > 0;
}
```

`StatHandle { StatOwnerHandle owner; StatIndex index; }`.

Lý do chọn tên dài nhất trong ba phương án: rõ ràng tuyệt đối, không đụng
`StatOwner : IComponentData` của bản gốc, và tránh chữ "Entity" gây hiểu nhầm ECS. Đổi lại
tên xuất hiện nhiều trong generated code — chấp nhận được vì generated code không ai đọc thủ công.

Hệ quả cho codegen: alias `UECS.Entity` → `AGES.StatOwnerHandle`
([§4.7.1](04-CODEGEN.md#474-bảng-alias-printadditionalusings)).

---

## DEC-002 — Đã chốt: **B** (hard-ref `EncosyTower.Core`, dùng cả generator của họ) ✅

`ApexionGame.Entities.Stats.asmdef` **giữ** reference `EncosyTower.Core`. Dùng trực tiếp:

| Từ EncosyTower | Dùng cho |
|---|---|
| `EncosyTower.Common.ByteBool` | `Stat._valueIsPair`, `_produceChangeEvents`, `ValuePair._isPair`, `StatReader._isCreated` |
| `EncosyTower.Common.Option<T>` | `StatDataParams<T>`, `StatValueParams<T>`, `Options.Data` |
| `EncosyTower.Common.HashValue` | `GetHashCode` mọi handle |
| `EncosyTower.Common.IIsValid` / `IIsCreated` | capability interface |
| `EncosyTower.Collections.*` | `InsertRangeSpan`, `IncreaseCapacityTo`, `AsReadOnlySpan` |
| `EncosyTower.Collections.Unsafe.ValueAsUnsafeRefRW` | `worldData._modifierStackRef` → `ref stack` |
| `[EncosyTower.TypeWraps.WrapType]` | `StatIndex`, `StatIndex<TStatData>` |
| `[EncosyTower.EnumExtensions.EnumExtensions]` | `StatVariantType` |
| `EncosyTower.CodeGen.Printer` + `CodeGenAPI` | 6 editor generator sinh bảng kiểu (DEC-003 = B) |

**Không** dùng: `EncosyTower.Debugging.ValidationDefines` — ta có bộ riêng
(`APEXION_*`) để guard của stats có công tắc độc lập với cấu hình EncosyTower của project.

### Điều kiện tiên quyết — đã verify

Tôi đã soát 31 khai báo `SKIP_ATTRIBUTE` trong `Plugins/SourceGenerator/`. **Mỗi area của
EncosyTower có skip-attribute riêng theo namespace của area**, không phải một attribute dùng chung:

```
TypeWrapGenerator        → EncosyTower.TypeWraps.SkipSourceGeneratorsForAssemblyAttribute
EnumExtensionsGenerator  → EncosyTower.EnumExtensions.SkipSourceGeneratorsForAssemblyAttribute
Stat*Generator           → EncosyTower.Entities.Stats.SkipSourceGeneratorsForAssemblyAttribute
```

Nên `[assembly: ApexionGame.Entities.Stats.SkipSourceGeneratorsForAssembly]` chỉ tắt
**generator của ta** trên assembly runtime của ta, còn `[WrapType]`/`[EnumExtensions]` của
EncosyTower vẫn chạy bình thường. Mối lo "mâu thuẫn skip-attribute" tôi nêu lúc đầu **không
tồn tại** → xem [§5.3.1](05-ASSEMBLY-LAYOUT.md#531-cơ-chế-skip-attribute--đã-verify-không-có-xung-đột).

Thêm nữa: attribute của ta là `ApexionGame.Entities.Stats.StatSystemAttribute`, metadata name
khác `EncosyTower.Entities.Stats.StatSystemAttribute` → generator stats của EncosyTower không
trigger lên khai báo của ta. Hai hệ chạy song song không giao thoa.

### Rủi ro nhận + escape hatch

| # | Rủi ro | Escape hatch |
|---|---|---|
| B1 | Update EncosyTower đổi output `[WrapType]` → API `StatIndex` đổi | Viết tay `StatIndex` (~70 LOC/type: `value`, operator, `IsValid`, `ToFixedString`) |
| B2 | Update EncosyTower đổi output `[EnumExtensions]` | Chỉ dùng `Names`/`Values`/`ToStringFast`; `ZeroVariant`/`OneVariant` do generator của ta sinh |
| B3 | Không dùng được ở project không có EncosyTower | Chấp nhận — repo nội bộ, EncosyTower là dep mặc định |
| B4 | Consumer assembly quên ref `EncosyTower.Core` → generated code không compile | Analyzer `AGS_STAT_SYSTEM_0002` báo lỗi rõ ràng |

**Không** làm assembly bridge `ApexionGame.Entities.Stats.EncosyTower` nữa (chỉ cần thiết ở
phương án A). Tích hợp PubSub/Logging nếu cần thì viết thẳng trong core.

---

## DEC-003 — **Đã đảo lại: B** — theo tác giả, dùng UnityCodeGen ✅

> **Lịch sử:** ban đầu tôi khuyến nghị A (tool dotnet riêng) và bạn đã chọn A. Sau khi bạn
> yêu cầu nghiên cứu thêm, tôi soát lại cơ chế thật của UnityCodeGen + `EncosyTower.CodeGen`
> và thấy **cả hai lý do tôi đưa ra đều không đứng được**. Đảo về B — cách của tác giả —
> nhưng giữ hai cải tiến độc lập với cơ chế (§dưới).

### Cách tác giả làm — đã đọc code, xác nhận từng bước

5 generator ở `EncosyTower.Entities.Stats/Generators/`, mỗi file mở đầu bằng:

```csharp
#if UNITY_EDITOR && ANNULUS_CODEGEN && ENCOSY_STAT_VALUE_TYPES_GENERATOR

using EncosyTower.CodeGen;   // Printer — nằm trong EncosyTower.Core, KHÔNG editor-only
using UnityCodeGen;

[Generator]
[ApiForEditor]
internal sealed class StatVariantTypeGenerator : ICodeGenerator
{
    public void Execute(GeneratorContext context)
    {
        var p = Printer.DefaultLarge;
        p.PrintAutoGeneratedBlock(nameof(StatVariantTypeGenerator));
        // ... emit
        context.OverrideFolderPath(CodeGenAPI.GetOutputFolderPathFromCaller(pathCombine: "../Common"));
        context.AddCode("StatVariantType.gen.cs", p.Result);
    }
}

#endif
```

| Thành phần | Sự thật đã verify |
|---|---|
| Discovery | `ScriptFileGenerator.Generate()` dùng `TypeCache.GetTypesDerivedFrom<ICodeGenerator>()` + lọc `[Generator]` → chạy được từ **bất kỳ assembly nào đang load trong Editor**, kể cả asmdef runtime |
| Trigger | Menu `Tools/UnityCodeGen/Generate`, hoặc auto-on-compile (tắt mặc định, lưu ở `EditorUserSettings` — per-user, không commit) |
| Ghi file | `File.WriteAllText` + `Directory.CreateDirectory` thuần, **không** qua `AssetDatabase` → **ghi được ra ngoài `Assets/`**, ví dụ vào thư mục package hoặc `Plugins/` |
| Idempotent | So nội dung cũ, chỉ ghi khi khác; xong thì `AssetDatabase.Refresh()` |
| Đường dẫn output | `CodeGenAPI.GetOutputFolderPathFromCaller([CallerFilePath])` → tính từ vị trí **file source của chính generator**, nên di chuyển thư mục không vỡ |
| Bật/tắt | `ENCOSY_STAT_VALUE_TYPES_GENERATOR` **không** có trong `ProjectSettings/ProjectSettings.asset` → generator bị compile-out hoàn toàn ở project bình thường. Đây là **tool của maintainer**, bật khi cần đổi bảng kiểu rồi tắt lại |
| `Printer` | `EncosyTower.Core/CodeGen/Printer.cs` (639 LOC) **không** bọc `#if UNITY_EDITOR` → dùng được ngay, không cần vendoring |
| Reference `AnnulusGames.UnityCodeGen.Editor` | asmdef của nó `includePlatforms: ["Editor"]`. `EncosyTower.Core.asmdef` (all-platform, **đang compile trong project này**) đã reference nó và chạy bình thường — pattern đã được chứng minh ngay tại đây |

### Vì sao hai lý do của tôi sai

**Lý do 1 — "bỏ được reference `AnnulusGames.UnityCodeGen.Editor`":** không phải lợi ích.
Package đã có trong `manifest.json` (`com.annulusgames.unity-codegen: 1.0.0`), asmdef của nó
là Editor-only nên không vào player build, và **`EncosyTower.Core` — thứ ta hard-ref theo
DEC-002 = B — đã reference nó rồi**. Bỏ khỏi asmdef của ta không giảm được dependency nào của
project. Chi phí thực = 0.

**Lý do 2 — "codegen chạy được trên CI không cần Unity":** giải quyết vấn đề không tồn tại.
Các file `.gen.cs` được **commit vào repo**; CI chỉ *biên dịch* chúng, không bao giờ sinh lại.
Bảng kiểu thay đổi gần như không bao giờ sau khi chốt profile. Một pipeline headless sinh
`.gen.cs` là năng lực thuần lý thuyết.

### Và phương án A còn có chi phí tôi chưa cân

| # | Chi phí của A |
|---|---|
| A1 | Phải vendoring lại `Printer` ~530–639 LOC — trong khi `EncosyTower.CodeGen.Printer` **miễn phí** theo DEC-002 = B |
| A2 | Mất `CodeGenAPI.GetOutputFolderPathFromCaller` → phải hard-code đường dẫn tương đối từ repo root, vỡ khi di chuyển thư mục |
| A3 | Thêm 1 project + 1 build step phải bảo trì, cộng rủi ro lệch giữa schema JSON và emitter |
| A4 | Mất `AssetDatabase.Refresh()` — sinh xong vẫn phải quay lại Unity, nên "không cần Unity" chỉ đúng một nửa |
| A5 | JSON phải parse lúc chạy, không được type-check; bảng C# thuần thì compiler bắt lỗi ngay |

### Chốt: dùng UnityCodeGen, giữ 2 cải tiến

```
Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/Generators/
├── StatTypeTable.cs                       # ★ bảng kiểu — NGUỒN DUY NHẤT (C# thuần, không JSON)
├── StatVariantTypeGenerator.cs            → ../Common/StatVariantType.gen.cs
├── StatVariantGenerator.cs                → ../Common/StatVariant.gen.cs
├── StatDataSizeGenerator.cs               → ../Common/StatDataSize.gen.cs
├── StatVariantTypeExtensionsGenerator.cs  → ../Common/StatVariantTypeExtensions.gen.cs
├── StatSingleExtensionsGenerator.cs       → ../Common/StatSingleExtensions.gen.cs
└── StatTypeTableGenerator.cs              → ★ Plugins/SourceGenerator.ApexionGame/
                                           #     ApexionGame.SourceGen.Helpers/Entities.Stats/
                                           #     StatTypeTable.gen.cs
```

Tất cả bọc `#if UNITY_EDITOR && ANNULUS_CODEGEN && APEXION_STAT_VALUE_TYPES_GENERATOR`.

**Cải tiến 1 — một nguồn bảng kiểu duy nhất.** Bản gốc giữ bảng ở **hai nơi phải khớp tay**:
`GeneratorAPI` (runtime-side, 5 cột: `Types`/`TypeNames`/`Sizes`/`OneConstructors`/`EqualOperators`)
và `StatGeneratorAPI` (Roslyn-side, 4 cột: `Types`/`TypeNames`/`Sizes`/`Namespaces`).
Vì `ScriptFileGenerator` ghi file bằng `File.WriteAllText` (ghi được ngoài `Assets/`),
`StatTypeTableGenerator` **emit luôn** bảng cho phía Roslyn → chỉ còn một master
`StatTypeTable.cs` 6 cột. Đánh đổi: sau khi đổi bảng phải chạy lại `dotnet build -c Release` để build lại
DLL generator — một bước có tài liệu, thay cho "sync tay hai file" của bản gốc.

**Cải tiến 2 — profile `gameplay`.** Độc lập hoàn toàn với cơ chế; chỉ là lọc `StatTypeTable`.
19 kiểu (`None`, `bool`, 8 kiểu nguyên, `half`..`half4`, `float`..`float4`, `double`), bỏ toàn
bộ matrix `*x*` và `bool2..bool4x4`:

| File | Bản gốc | Bản port |
|---|---|---|
| `StatVariant.gen.cs` | 3672 | ~1.1k |
| `StatVariantType.gen.cs` | 426 | ~130 |
| `StatSingleExtensions.gen.cs` | 322 | ~100 |
| `StatDataSize.gen.cs` | 194 | ~70 |
| `StatVariantTypeExtensions.gen.cs` | 115 | ~50 |
| switch/operator trong generated `StatDataStore` | 65 nhánh | 19 nhánh |

**Workflow đổi bảng kiểu:** thêm define `APEXION_STAT_VALUE_TYPES_GENERATOR` vào Project
Settings → sửa `StatTypeTable.cs` → `Tools/UnityCodeGen/Generate` → bỏ define →
`dotnet build … -c Release` → commit `.gen.cs` + DLL.

**Điều duy nhất mất so với A:** không sinh được `.gen.cs` khi chưa mở Unity. Chấp nhận —
đây là tác vụ maintainer, tần suất gần bằng 0.

Chi tiết: [§4.6](04-CODEGEN.md#46-bảng-kiểu--codegen-editor-time-dec-003--b).

---

## DEC-004 — Đã chốt: **A** — thêm `RemapObservedStats` vào `IStatModifier` ngay ✅

```csharp
public interface IStatModifier<TValuePair, TStat, TStatModifierStack>
{
    uint Id { get; set; }
    void AddObservedStatsToList(NativeList<StatHandle> observedStatHandles);
    void Apply(StatReader<TValuePair, TStat> reader, ref TStatModifierStack stack
        , out bool shouldProduceModifierTriggerEvent);

    void RemapObservedStats(in StatOwnerRemap remap);   // ★ mới
}
```

**Đây là thay đổi contract duy nhất so với bản gốc.** Lý do cần: `StatHandle` chứa
`StatOwnerHandle.Index/Version`, nên sau khi load, mọi handle trong `StatObserver` **và** trong
`TStatModifier` phải remap. Store tự remap được `StatObserver.ObserverHandle` (có property),
nhưng không biết field nào của `TStatModifier` chứa `StatHandle` — struct đó do user viết.

Cơ chế: `StatSystemGenerator` sinh `RemapObservedStats` gọi
`partial void OnRemapObservedStats(in StatOwnerRemap remap)`; user điền ở partial của mình,
đối xứng với cách bản gốc để user tự viết `AddObservedStatsToList`.
Analyzer `AGS_STAT_DATA_0006` cảnh báo nếu `StatModifier` có field kiểu `StatHandle`/`StatHandle<T>`
mà `OnRemapObservedStats` chưa được implement.

Contract chốt ngay từ **phase 1** (task 1.9); implement `TryCopyOwnerTo`/`TryRestoreOwner`
để **phase 4** (task 4.1). Chi tiết: [§2.9](02-ARCHITECTURE.md#29-persistence).

Hệ quả cần nhớ: modifier viết cho bản EncosyTower **không copy nguyên văn** sang được — phải
bổ sung method này.

---

<a id="dec-005"></a>

## DEC-005 — Divergence D-01: ✅ chốt **bỏ dedupe ở cả hai phía** (2026-08-04, task 4.5)

Bản gốc: `TryUpdateStat` có visited-set dedupe (`StatAccessor.cs:1293`), `UpdateStatRef`
(`StatAccessor.cs:1484`) **không**. Giả thiết ban đầu — của cả tôi lẫn tài liệu — là dedupe
đúng còn thiếu dedupe chỉ tốn CPU. **Giả thiết đó sai, và sai theo hướng ngược lại.**

### Đo, không đoán

Thuật toán worklist ở `UpdateSingleStatCommon` / `TryUpdateStat` / `UpdateStatRef` được dựng
lại nguyên vẹn thành một chương trình managed chạy ngoài Unity (không `NativeList`, không
Burst — chỉ giữ đúng thứ tự duyệt và điều kiện dừng `prev == value` ở `StatAPI.cs:293`).

**Case 1 — kim cương lệch tầng.** `A→B→D` (1 chặng) và `A→C→E→D` (2 chặng). Đặt `A = 5`:

| | D |
|---|---|
| đúng | **10** |
| `TryUpdateStat` (có visited) | 6 ← **sai vĩnh viễn** |
| `UpdateStatRef` (không visited) | 10 |

Lý do: worklist chạm D **trước khi** E kịp cập nhật, nên D được tính từ E cũ. Khi E đổi, D
được đẩy vào worklist lần hai — và chính lần hai đó **sửa** giá trị. Visited-set chặn đúng
lần sửa ấy. Lượt duyệt lặp không phải lãng phí; nó là cơ chế hội tụ.

**Case 2 — chuỗi k kim cương *đều* tầng.** Cả hai luôn đúng; chi phí `3k+1` so với `4k+1`
recalc. Không có bùng nổ số mũ: điều kiện `prev == value` cắt lan truyền ngay khi giá trị
không đổi thật.

**Case 3 — chuỗi k kim cương *lệch* tầng.**

| k | có visited | không visited |
|---|---|---|
| 1 | 5 — **sai** | 6 — đúng |
| 5 | 21 — **sai** | 76 — đúng |
| 10 | 41 — **sai** | 276 — đúng |
| 14 | 57 — **sai** | 526 — đúng |

Không visited: bậc hai (~2.5k²), luôn đúng. Có visited: tuyến tính, **sai ở mọi k**.

### Quyết định

Bỏ visited-set khỏi `TryUpdateStat` và `TryUpdateStatAssumeSingleOwner`. Sau thay đổi này
hai lối lan truyền hành xử **giống hệt nhau** → D-01 không còn là divergence nữa.

Đánh đổi được nhận rõ: xấu nhất là **O(k²)** thay vì O(k) trên đồ thị dày kim cương. Đổi
tuyến tính lấy giá trị sai là đổi hớ. Muốn vừa đúng vừa tuyến tính thì phải lan truyền theo
thứ tự topo (mỗi stat mang depth, worklist là priority queue) — một thiết kế khác, ghi lại
làm việc tương lai chứ không làm trong port này.

`_tmpVisitedObserverHandles` **vẫn giữ**: nó còn được dùng cho phát hiện vòng lặp lúc
`TryAddStatModifier` (`StatAccessor`6.cs:1084`) — chỗ dedupe thực sự đúng, vì ở đó ta duyệt
cấu trúc đồ thị chứ không lan truyền giá trị.

An toàn kết thúc: bỏ dedupe không thể treo, vì chu trình bị **từ chối ngay lúc thêm
modifier** (`StatAccessor`6.cs:1042-1087`, golden test S06/S07) → đồ thị luôn là DAG.

Test hồi quy: `S05_UnequalDepthDiamond_JoinIsRepairedNotLeftStale` phủ cả hai lối
(`TrySetStatBaseValue` → `UpdateStatRef`, `TryRemoveStatModifier` → `TryUpdateStat`).

> Đây là lỗi **của bản gốc**, không phải của bản port. Ai định merge ngược lên upstream thì
> đây là chỗ cần vá.

---

# Đã quyết trong quá trình thiết kế

### D-100 — Lưu trữ: ba `UnsafeList<T>*` cấp phát rời per owner

Xem [§2.3.3](02-ARCHITECTURE.md#233-statstoretstat-tstatmodifier-tstatobserver) và bảng
alternative [§2.3.4](02-ARCHITECTURE.md#234-alternative-đã-cân-nhắc-và-loại).
Header có địa chỉ ổn định → `StatBuffer<T>` không dangling khi tạo owner mới;
`Insert`/`RemoveAt` giữ O(số phần tử của owner) như ECS thay vì O(toàn world).
`DestroyOwner` chỉ `Clear()` + đẩy slot vào free-list → spawn/despawn zero-alloc sau warm-up.

### D-101 — `StatAccessor` giữ 1 `StatStore` thay 4 lookup

Bỏ `_lookupOwner`/`_lookupStats`/`_lookupModifiers`/`_lookupObservers` và
`Update(ref SystemState)`. `ComponentLookup<StatOwner>` trong `Accessor.ReadOnly` của bản gốc
chỉ tồn tại để đăng ký read-dependency với ECS scheduler (có comment nói rõ ở
`StatAccessor+ReadOnly.cs:35`) → không còn ý nghĩa.

### D-102 — Giữ *None stat* ở index 0

`StatIndex.IsValid => value > 0`. Bản gốc dựa vào điều này ở `TryUpdateAllStats` (bắt đầu từ
`i = 1`) và `StatHandle.IsValid`. Không đổi.

### D-103 — Giữ nguyên hợp đồng không-thread-safe của `StatWorldData`

Pattern song song vẫn là *thu thập song song → apply đơn luồng* qua `DeferredUpdateStat*Job`.
Làm world data thread-safe sẽ phải bỏ scratch-list reuse → mất luôn đặc tính zero-alloc.

### D-104 — Bỏ nhánh `#if LATIOS_FRAMEWORK`

`DeferredUpdateStatUnsafeBlockListJob` cần `Latios.Unsafe.UnsafeParallelBlockList`; project
không có Latios trong `manifest.json`. Để lại extension point (user tự viết `IJob` gọi
`TryUpdateStat`).

### D-105 — `[StatCollection]` không còn yêu cầu `IComponentData`

Struct collection chỉ là data holder chứa `StatHandles`. Project tự chọn nơi lưu.

### D-106 — Diagnostic prefix `AGS####`

Thay `SG_ENTITIES_STAT_*` cho gọn và không đụng namespace ID của EncosyTower. 14 diagnostic,
bảng đầy đủ ở [§4.8](04-CODEGEN.md#48-analyzer--diagnostic-id).

### D-107 — Vendoring `SourceGen.Common` là snapshot, không sync

Copy một lần vào `ApexionGame.SourceGen.Common`, giữ license header + link nguồn, rút gọn
`HashValue` (965 → ~120) và `SymbolExtensions` (2279 → ~300) xuống phần thực dùng. Không thiết
lập cơ chế đồng bộ với EncosyTower — đó chính là mục tiêu tách rời codegen.

Lưu ý: đây là điểm **duy nhất** ta tách rời khỏi EncosyTower. Runtime thì hard-ref (DEC-002 = B),
Roslyn generator thì độc lập. Ranh giới: *type và editor-tool dùng chung được, Roslyn generator
thì không* — vì chỉ nó quyết định API public của code sinh ra lúc compile.

### D-110 — Hạ tầng build/test/debug SourceGen: copy nguyên cách tác giả

Đã đọc `Plugins/SourceGenerator/{Directory.Build.props, EncosyTower.SourceGen.slnx, README.md}`,
`Samples/Directory.Build.props`, `EncosyTower.SourceGen.Tests/{Directory.Build.props, UnityDllPaths.targets}`.
Bốn thứ port nguyên xi, **không** phát minh lại:

| # | Cách tác giả | Thay cho |
|---|---|---|
| 1 | `CopyBuildArtifacts` target trong `Directory.Build.props`: `AfterTargets="Build"`, `Condition` Release && không phải Tests, `Copy` `$(OutDir)*.dll` → thư mục `SourceGenerators/`, `SkipUnchangedFiles="true"` | `build.ps1` tôi đề xuất ban đầu — không cần script, `dotnet build -c Release` là đủ |
| 2 | `BaseOutputPath`/`BaseIntermediateOutputPath` → `Build/<Project>/{bin,obj}/` | bin/obj rải khắp mỗi project |
| 3 | `Samples/` — netstandard2.1 Exe, ref DLL Unity qua NuGet `Unity3D` 3.1.1 + `UNITY_OS_INSTALL_ROOT` + `Library/ScriptAssemblies`, ref generator dạng `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` | "compile trong Unity rồi chờ reimport" — dev loop chậm gấp nhiều lần |
| 4 | `launchSettings.json` `"commandName": "DebugRoslynComponent"` + `targetProject` trỏ sample | debug generator bằng diagnostic message |

Cộng thêm: `UnityDllPaths.targets` sinh `Helpers/UnityDllPaths.g.cs` trước build (xoá sau) để test
nạp **DLL Unity thật** vào `CSharpCompilation` thay vì chỉ stub.

Chi tiết: [§4.5](04-CODEGEN.md#45-build-deploy-test-debug--copy-nguyên-cách-tác-giả).

Riêng `Microsoft.CodeAnalysis.CSharp` **4.3.1**: README của tác giả cho lý do chính xác — Unity
2022.3 và 6000.0 đều dùng Roslyn Analyzer 4.3.1, kiểm được ở
`[UnityInstallRoot]/Editor/Data/DotNetSdkRoslyn/csc.deps.json`.

### D-108 — `ValidationDefines` riêng, không dùng `ENCOSY_*`

`APEXION_RUNTIME_CHECKS` / `APEXION_STATS_RUNTIME_CHECKS` / `DISABLE_APEXION_CHECKS`.
Guard của stats phải bật/tắt độc lập với cấu hình EncosyTower của project.

### D-109 — `DestroyOwnerAndUpdateObservers` bù TODO của bản gốc

Bản gốc có TODO: destroy entity **không** tự update observer
(`StatAPI.cs:1222`, `StatAccessor.cs:1585`). Bản port giữ `DestroyOwner` thuần (không tự
update, để không trả phí ngoài ý muốn) nhưng thêm tiện ích gói sẵn ở phase 2 task 2.8.

---

# Sai lệch phát sinh khi implement

### I-01 — `Size` tính bằng `UnsafeUtility.SizeOf<T>()`, không phải literal (task 1.1)

[§4.6.3](04-CODEGEN.md#463-cải-tiến-1--một-nguồn-bảng-kiểu) phác `TypeRecord.Size` là `int`
literal. Bản implement giữ cách của tác giả (`GeneratorAPI.Sizes` cũng dùng `UnsafeUtility.SizeOf`):
size là **giá trị đo được**, không phải con số chép tay. Literal chỉ xuất hiện ở phía Roslyn,
do `StatTypeTableGenerator` bake ra. Đổi lại `StatTypeTable.cs` chạy được trong Editor mà thôi
— chấp nhận, vì nó vốn là tool editor-time.

### I-02 — `uint.Eq = true` (task 1.1)

`GeneratorAPI.EqualOperators` của tác giả đánh `false` cho `uint` trong khi mọi kiểu nguyên
khác là `true` — không nhất quán (bản thân dãy comment `/// <see cref=...>` trong file đó cũng
lệch nhãn). Bản port đánh `true`. Không đổi hành vi: `Eq = false` chỉ khiến `Equals` sinh ra
`UInt.Equals(other.UInt)` thay vì `UInt == other.UInt`, cả hai đều là so sánh strongly-typed.

### I-03 — Bỏ comment `// TODO(unsafe-evolution)` trong code sinh (task 1.1)

`StatVariantGenerator` của tác giả emit một dòng TODO trên **mỗi** field overlapping. Với 19
kiểu là 20 dòng nhiễu trong file generated. Bỏ; ghi chú gốc không còn tác dụng vì mọi kiểu
trong bảng đều unmanaged.

### I-04 — `EnumTypeMap` phía Unity là `EnumUnderlyingTypes` (task 1.1)

Iterate `Dictionary` không đảm bảo thứ tự → output không ổn định → `ScriptFileGenerator` ghi đè
file dù nội dung không đổi về mặt logic. Phía Unity dùng `string[] EnumUnderlyingTypes` (thứ tự
cố định) rồi tra `Find()` để lấy `Name`; **file sinh ra phía Roslyn vẫn là
`Dictionary<string, string> EnumTypeMap` đúng như tác giả**.

### I-05 — Bảng phía Roslyn tên `StatTypeTable`, không phải `StatGeneratorAPI` (task 1.1)

Theo tên file `StatTypeTable.gen.cs` mà [§4.6.2](04-CODEGEN.md#462-layout) đã chốt, giữ
convention *tên type khớp tên file*. Phase 3 gọi `StatTypeTable.Types` thay
`StatGeneratorAPI.Types`.

### I-06 — `Common/None.cs` làm sớm ở task 1.1 thay vì 1.6

`StatTypeTable` cần `UnsafeUtility.SizeOf<None>()` nên type phải tồn tại trước. File 19 dòng,
port nguyên văn.

### I-07 — `StatHandle.IsValid` dựa vào `owner.IsValid`, không phải so sánh với `Null`

Bản gốc: `entity.Equals(Null.entity) == false && index.IsValid`. Port thẳng sang sẽ thành
`owner.Equals(Null.owner) == false`, tức coi `{Index = 5, Version = 0}` là **hợp lệ** —
sai, vì `Version == 0` nghĩa là slot chưa từng sống. Dùng `owner.IsValid && index.IsValid`
(`IsValid => Version > 0`, xem [DEC-001](#dec-001--identity-type-statownerhandle-)).
Chặt hơn bản gốc; `StatHandle.IsValid` là cửa chặn lan truyền nên chặt hơn là an toàn hơn.

### I-08 — Type generic tách ra file `` `1 `` riêng (task 1.5, 1.6)

Bản gốc gộp `StatIndex` + `StatIndex<TStatData>` vào `StatIndex.cs`, `StatHandle` +
`StatHandle<TStatData>` vào `StatHandle.cs`. [CODING-CONVENTIONS §6](../../../../CODING-CONVENTIONS.md)
bắt type generic nằm ở `` TypeName`N.cs `` — và `Storage/` đã theo đúng vậy
(`` StatBuffer`1.cs ``, `` StatStore`3.cs ``). Nên tách:
`` StatIndex.cs `` / `` StatIndex`1.cs ``, `` StatHandle.cs `` / `` StatHandle`1.cs ``,
`` StatSingle`1.cs ``.

Lợi ích ngoài dự tính: bản non-generic **không** phụ thuộc `IStatData` nên làm và verify được
ngay, trong khi bản generic còn chờ 1.2/1.9.

### I-09 — `IStatModifier` + 2 type kéo theo dời sang phase 2

`IStatModifier.Apply` nhận `StatReader<TValuePair, TStat>` — mà `StatReader` là **L3**
(task 2.3) và mọi method của nó gọi `StatAPI` (task 2.1). Ba type dính ràng buộc này:

| Type | Vì sao |
|---|---|
| `IStatModifier<3>` | tham số `StatReader<2>` trong `Apply` |
| `ModifierTriggerEvent<4>` | `where TStatModifier : IStatModifier<3>` |
| `StatModifierRecord<4>` | `where TStatModifier : IStatModifier<3>` |

Nếu viết chúng ở phase 1 thì assembly đỏ liên tục cho tới giữa phase 2 — mất luôn khả năng
chạy unit test của `StatBuffer`/`StatStore` (exit criteria phase 1). Dời cả ba xuống phase 2,
đặt ngay trước task 2.3. Không đổi thiết kế, chỉ đổi thứ tự. `RemapObservedStats` của
[DEC-004](#dec-004--đã-chốt-a--thêm-remapobservedstats-vào-istatmodifier-ngay-) vẫn được
thêm khi viết interface — chỉ là viết muộn hơn.

### I-10 — `StatVariant.Clamp` dùng `ref readonly` local

17 nhánh switch dạng
`new StatVariant(math.clamp(valueToClamp.Float3, lowerBound.Float3, upperBound.Float3))`
đều vượt 120 ký tự ([convention §2](../../../../CODING-CONVENTIONS.md)). Bind
`ref readonly var v = ref valueToClamp;` (và `lo`/`hi`) rồi dùng tên ngắn trong switch —
`ref readonly` nên **không copy** struct, khác với gán local thường. Tên tham số public
giữ nguyên như bản gốc.

### I-11 — Tập kiểu của từng toán tử co lại theo profile `gameplay`

Bản gốc: 65 nhánh/toán tử. Bản port giữ **đúng ngữ nghĩa** của tác giả về việc kiểu nào hỗ trợ
toán tử nào, chỉ giao với 19 kiểu của profile:

| Toán tử | Số nhánh | Tập kiểu |
|---|---|---|
| `+` (unary), `+ - * /`, `Min`/`Max`/`Clamp` | 17 | mọi kiểu số |
| `-` (unary) | 13 | bỏ `byte`/`uint`/`ulong`/`ushort` — đúng như bản gốc |
| `~`, `<<`, `>>` | 8 | kiểu nguyên |
| `&`, `|`, `^` | 9 | kiểu nguyên + `bool` |
| `!` | 1 | `bool` |

`Min`/`Max`/`Clamp` mất `double2..4`, `int2..4`, `uint2..4` **không phải** vì đổi ngữ nghĩa,
mà vì các kiểu đó không có trong profile.

### I-12 — `StatOwnerSlot` tách thành mảng song song; thêm `StatBufferLookup<T>`

**Lỗ hổng thiết kế phát hiện khi bắt đầu phase 2.** [§3.3.3](03-API-SURFACE.md#333-stataccessorreadonly--statreader2)
ghi `StatReader<TValuePair, TStat>` giữ hai chế độ, một trong đó là "`useStore`". Điều đó
**bất khả**: `StatStore<TStat, TStatModifier, TStatObserver>` có 3 tham số kiểu, không nhét vào
một struct 2 tham số kiểu được. Mà `StatReader` buộc phải giữ đúng 2, vì
`IStatModifier<TValuePair, TStat, TStatModifierStack>.Apply` nhận nó — thêm tham số vào
`StatReader` sẽ buộc `IStatModifier` nhận **chính nó** làm tham số kiểu (F-bounded), lan ra toàn
bộ API sinh ra ở [§3.4](03-API-SURFACE.md#34-generated-surface-l4--không-đổi-so-với-bản-gốc).

Bản gốc không gặp vấn đề này vì `BufferLookup<T>` generic đúng **một** kiểu. Nguyên nhân ở bản
port là `StatOwnerSlot` gom cả ba con trỏ vào một struct 3-generic
([§2.3.3](02-ARCHITECTURE.md#233-statstoretstat-tstatmodifier-tstatobserver)).

**Sửa:** giữ nguyên [D-100](#d-100--lưu-trữ-ba-unsafelistt-cấp-phát-rời-per-owner) — vẫn ba
`UnsafeList<T>*` cấp phát rời per owner, địa chỉ header vẫn ổn định — chỉ đổi cách **đánh chỉ mục**:

| Trước | Sau |
|---|---|
| `UnsafeList<StatOwnerSlot<TStat, TMod, TObs>>` (version + counter + 3 con trỏ) | `UnsafeList<StatOwnerSlot>` (chỉ version + counter, **hết generic**) |
| — | `UnsafeList<StatBufferSlot<TStat>>`, `…<TStatModifier>`, `…<TStatObserver>` — ba mảng song song |

Nhờ đó dựng được `StatBufferLookup<T>` chỉ generic **một** kiểu (`{ slots, buffers }`), tương ứng
1:1 với `BufferLookup<T>` của ECS, và `StatReader<TValuePair, TStat>` giữ nguyên hình dạng.
`StatStore` thêm `AsStatLookup()` / `AsModifierLookup()` / `AsObserverLookup()`.

C# không cho dùng con trỏ làm tham số kiểu generic (`UnsafeList<UnsafeList<T>*>` không hợp lệ),
nên cần struct bọc `StatBufferSlot<T> { UnsafeList<T>* list; }`.

**Bề mặt public của `StatStore` không đổi** — 27 test của task 1.7/1.8 compile lại không sửa
một dòng nào. Cần cập nhật §2.3.3 và §3.3.3 của doc.

### I-13 — `StatBuffer<T>` truyền theo giá trị, không `ref`

Bản gốc nhận `ref DynamicBuffer<TStat> statBuffer` ở ~15 hàm. `ref` ở đó là để tránh copy struct
`DynamicBuffer` và để `ElementAt` trả `ref` hợp lệ. `StatBuffer<T>` chỉ là một con trỏ
(`UnsafeList<T>*`) + safety handle, và **mọi** method của nó là `readonly` (mutate qua con trỏ),
nên bản copy vẫn ghi vào đúng list gốc và `ElementAt` vẫn trả `ref` trỏ vào bộ nhớ thật.
Bỏ `ref` cho gọn chữ ký; hành vi không đổi.

### I-14 — `_tmpSameEntityUpdatedStats` đổi tên thành `_tmpSameOwnerUpdatedStats`

Không còn khái niệm entity. Cùng lý do, bốn hàm truy vấn phụ thuộc đổi tên theo
[§3.3.1](03-API-SURFACE.md#331-statapi-static): `EntityHasAnyOtherDependantStatEntities` →
`OwnerHasAnyOtherDependantStats`, `GetOtherDependantStatsOfEntity` → `…OfOwner`,
`GetOtherDependantStatEntitiesOfEntity` → `GetOtherDependantOwnersOfOwner`,
`GetStatEntitiesThatEntityDependsOn` → `GetOwnersThatOwnerDependsOn`.
`NativeHashSet<Entity>` → `NativeHashSet<StatOwnerHandle>`.

### I-15 — `Unity.Assertions.Assert` thay bằng `ThrowHelper.ThrowIfOwnerIsDead`

`AddStatAsObserverOfOtherStat` của bản gốc mở đầu bằng
`Assert.IsTrue(observerStatHandle.entity != Entity.Null)` — kéo theo dependency `Unity.Entities`.
Thay bằng guard đã thêm sẵn ở task 1.4 theo [§2.8](02-ARCHITECTURE.md#28-validation);
đây chính là chỗ dùng đầu tiên của nó.

### I-16 — `ref StatOwner` thay bằng `_store.TryIncrementModifierId`

`TryAddStatModifier` của bản gốc nhận `ref StatOwner affectStatOwnerRef` (lấy từ
`_lookupOwner.GetRefRW(entity).ValueRW`) rồi `affectStatOwnerRef.modifierIdCounter++`.
Không còn component `StatOwner`, counter nằm trong `StatOwnerSlot` của store. Bỏ tham số,
gọi `_store.TryIncrementModifierId(owner, out var id)` **đúng tại vị trí cũ** để giữ nguyên
hành vi "modifier bị từ chối vẫn tiêu thụ một id".

Đánh đổi: batch add tra slot một lần cho mỗi modifier thay vì giữ một `ref` xuyên suốt.
Chi phí là một lần bounds-check + đọc mảng — không đáng kể so với phần còn lại của
`TryAddStatModifier`. Nếu benchmark 4.4 chỉ ra vấn đề thì thêm `ref uint` accessor vào store.

### I-17 — Hai `Assert.IsTrue` thành guard có `[Conditional]`

`TryRemoveStatModifier` dùng `Assert.IsTrue(range.count >= 0)` ×2 và
`TryUpdateStatAssumeSingleEntity` dùng `Assert.IsTrue(tmpGlobalUpdatedStats.Length == 0)`.
Cả ba là `Unity.Assertions` (kéo `Unity.Entities`) và **không** tắt được ở release.
Thay bằng `ThrowIfNegativeRangeCount` / `ThrowIfGlobalUpdateListIsNotEmpty` — private static,
`[HideInCallstack, StackTraceHidden]`, bộ `[Conditional]` của `APEXION_*`
([§10](../../../../CODING-CONVENTIONS.md)). Đây chính là loại guard mà
[R2](06-ROADMAP.md#risk-register) cần: range count âm là dấu hiệu đầu tiên của lỗi dịch
`startIndex`.

### I-18 — `…SingleEntity` → `…SingleOwner`

`TryUpdateStatAssumeSingleEntity` → `TryUpdateStatAssumeSingleOwner`,
`TryAddStatModifierSingleEntity` → `TryAddStatModifierSingleOwner`, tham số
`isGuaranteedSingleEntity` → `isGuaranteedSingleOwner`. Cùng lý do với
[I-14](#i-14--_tmpsameentityupdatedstats-đổi-tên-thành-_tmpsameownerupdatedstats).

### I-19 — Batch API: một `TryGetBuffers` thay ba lookup, và dispose container `Temp`

Ba hàm `TrySet*ToStats` của bản gốc gọi `lookupStats/lookupModifiers/lookupObservers.TryGetBuffer`
riêng lẻ cho mỗi owner. Bản port gọi `_store.TryGetBuffers(owner, out …, out …, out …)` — một lần
tra slot thay ba, đúng như [§2.3.3](02-ARCHITECTURE.md#233-statstoretstat-tstatmodifier-tstatobserver)
đã dự tính khi thiết kế `TryGetBuffers`.

Ngoài ra bản gốc **không** dispose `NativeParallelMultiHashMap` và `NativeHashSet` cấp phát bằng
`Allocator.Temp`. Bản port dispose tường minh — `Temp` tự thu hồi theo frame nên không phải rò rỉ
thật, nhưng leak detection trong job sẽ cảnh báo.

D-02 ✅ làm ở task 4.6: ba hàm `TrySet*ToStats` nhận thêm `Allocator allocator = Allocator.Temp`
ở cuối. Cố ý **không** đẩy tham số này xuống wrapper sinh ra: wrapper luôn cấp phát đúng
`LENGTH` phần tử — hằng số biên dịch, vài chục — còn accessor cấp phát theo **độ dài batch do
người gọi truyền vào**, tức là cái duy nhất có thể làm tràn block `Temp` trong job dài. Thêm
một tham số optional vào cuối chuỗi optional của API fluent còn là bẫy khi gọi theo vị trí.

### I-24 — generator quên bọc `TryRemapOwner`/`TryRemapOwners`

Phát hiện khi viết sample: `StatSystem.Accessor` sinh ra bọc `TryUpdateAllStats`,
`TryUpdateStat`, `TryAddStatModifier`… nhưng **không** bọc hai hàm remap. Nghĩa là toàn bộ
DEC-004 — thứ giữ cho modifier chéo owner không trỏ nhầm sau khi tải save — **không với tới
được từ API sinh ra**. Vẫn dùng được qua field `accessor` công khai, nhưng đó là lách ra API thô
chứ không phải dùng đúng.

Đã thêm hai wrapper vào `StatSystemSpec+WriteCode.cs`, ngay cạnh `TryUpdateAllStats`.

Bài học nhỏ: lỗi này không có test nào bắt được, vì test generator kiểm tra *code sinh ra compile
sạch* chứ không kiểm tra *có đủ thứ cần dùng*. Nó chỉ lộ ra khi có người thật sự ngồi viết code
dùng API đó — đấy là giá trị của sample, không phải chỉ để cho đẹp.

### I-20 — `StatBuilder` thay `StatBaker`: ba thứ mất, một thứ thêm

| Bản gốc | Bản port |
|---|---|
| `IBaker _ibaker` + `Entity _entity` | `StatStore<3> _store` + `StatOwnerHandle _owner` |
| `StatOwner _statOwner` (ghi lại bằng `_ibaker.SetComponent` sau khi add modifier) | *bỏ* — counter nằm trong slot của store, `TryIncrementModifierId` ghi thẳng |
| `this.GetStatWorldData(16, Allocator.Temp, out var worldData)` (extension trên baker) | tạo `StatWorldData` `Allocator.Temp` tại chỗ và **dispose** sau khi dùng |

`Clear()` vẫn `_statBuffer.Add(new TStat())` để giữ *None stat* ở index 0.

**Điểm vào mới: `StatAPI.CreateStatOwnerHandle`** — tạo owner trong store, thêm None stat, trả
`StatBuilder`. Đây là hàm duy nhất đảm bảo [D-102](#d-102--giữ-none-stat-ở-index-0); `store.CreateOwner`
gọi trực tiếp sẽ cho một buffer **rỗng**, không dùng cho stats được. Codegen phase 3 phải luôn
đi qua hàm này.

Vẫn giữ tối ưu `isGuaranteedSingleOwner: true` — builder chỉ làm việc trên một owner, và
`TryAddStatModifier` từ chối mọi modifier chạm tới owner khác.

### I-21 — `Annotations/` không thuộc task nào của phase 1

[§5.1](05-ASSEMBLY-LAYOUT.md#51-cây-thư-mục-runtime) liệt kê `Annotations/` với ba attribute, và
[§1.1](01-OVERVIEW.md#11-bản-gốc-có-gì) tính chúng ~145 LOC — nhưng **không task nào từ 1.0 đến 1.9
nhận chúng**. Phát hiện khi dựng `Samples.Entities.Stats`: không có `[StatSystem]` thì sample không
khai báo được gì. Đã port nguyên văn, đánh số **3.0** vì chúng là điều kiện cần của phase 3.

### I-22 — Vendoring copy nguyên, chưa rút gọn; bỏ 2 file không dùng

[D-107](#d-107--vendoring-sourcegencommon-là-snapshot-không-sync) dự tính rút `HashValue`
965 → ~120 và `SymbolExtensions` 2279 → ~300 ngay khi vendoring. **Chưa làm** — biết file nào
thừa đòi hỏi ba generator đã tồn tại. Copy nguyên trước, rút sau khi 3.6–3.8 xong, lúc đó dead
code đo được thay vì đoán. Copy thực hiện bằng script (đổi namespace + chèn header xuất xứ), an
toàn hơn chép tay 273 KB.

Đã bỏ `NamingPolicy.cs` và `NameCasing.cs`: chúng kéo theo cả thư mục con `Internals/`
(8 file naming policy) mà ba generator stats **không hề dùng**. Còn 20 file.

Đính chính [§4.2](04-CODEGEN.md#42-cây-solution--theo-khuôn-tác-giả): `CompilationInfo.cs`,
`LocationInfo.cs`, `SourceGenVersion.cs` được liệt kê nhầm trong `SourceGen.Common` — thực tế
chúng nằm ở `SourceGen.Helpers/Helpers/`. Bản port đặt đúng chỗ như tác giả.

### I-23 — Hook của DEC-004 tên là `RemapObservedStatsInternal`, không phải `OnRemapObservedStats`

[§3.1](03-API-SURFACE.md#31-contracts-l1) và [§4.8](04-CODEGEN.md#48-analyzer--diagnostic-id) gọi
partial hook là `OnRemapObservedStats`. Bản implement dùng `RemapObservedStatsInternal` cho khớp
ba hook anh em mà generator **đã** sinh sẵn: `AddObservedStatsToListInternal`, `ApplyInternal`,
`GetIdInternal`/`SetIdInternal`. Một quy ước đặt tên trong cùng một type quan trọng hơn việc bám
chữ trong doc. `AGS_STAT_DATA_0006` kiểm đúng tên này.

---

# Sai lệch trong skeleton hiện tại — sửa ở phase 1 task 1.0

**4 việc phải sửa:**

| File | Vấn đề | Sửa |
|---|---|---|
| `SkipSourceGeneratorsForAssemblyAttribute.cs` | `public class` **không** kế thừa `Attribute`, không có `[AttributeUsage]` → hiện tại vô tác dụng | `[AttributeUsage(AttributeTargets.Assembly)] public sealed class ... : Attribute { }` |
| `AssemblyInfo.cs` | Rỗng | `[assembly: SkipSourceGeneratorsForAssembly]` + 2 `InternalsVisibleTo` (Tests, Editor) |
| `ApexionGame.Entities.Stats.asmdef` | `allowUnsafeCode: false` | → `true` (bắt buộc: `UnsafeList<T>*`, `[StructLayout(Explicit)]`, `ref` return) |
| `ApexionGame.Entities.Stats.asmdef` | thiếu `versionDefines` | thêm `ANNULUS_CODEGEN` / `UNITY_BURST` / `UNITY_COLLECTIONS` / `UNITY_MATHEMATICS` |

**2 thứ giữ nguyên** (bạn đã làm đúng từ đầu):

| File | Giữ | Vì |
|---|---|---|
| `asmdef` | ref `EncosyTower.Core` | DEC-002 = B |
| `asmdef` | ref `AnnulusGames.UnityCodeGen.Editor` | DEC-003 = B |

**1 việc nhỏ:** `csc.rsp` thiếu newline cuối file
([convention §2](../../../../CODING-CONVENTIONS.md)).
