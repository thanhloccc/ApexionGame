# 04 — Codegen

## 4.0 Hai cơ chế codegen — đừng lẫn

EncosyTower có **hai** hệ codegen hoàn toàn khác nhau. Bản port giữ đúng cả hai, mỗi cái theo
cách của tác giả:

| | **Roslyn source generator** | **UnityCodeGen editor generator** |
|---|---|---|
| Nguồn | `Plugins/SourceGenerator/*.csproj` (netstandard2.0) | `EncosyTower.Entities.Stats/Generators/*.cs` (trong asmdef Unity) |
| Sản phẩm | **DLL** → `EncosyTower.Core/SourceGenerators/*.dll` (commit) | **file `.gen.cs`** → `Common/*.gen.cs` (commit) |
| Chạy khi nào | **mỗi lần compile**, tự động, output chỉ ở bộ nhớ | **chạy tay** qua menu, khi maintainer đổi bảng kiểu |
| Xử lý gì | `[StatSystem]`, `[StatCollection]`, `[StatData]` | bảng kiểu: `StatVariant`, `StatVariantType`, `StatDataSize`, extensions |
| Bàn ở | **§4.1–4.5** ← phần chính | §4.6 |
| Bản port | solution riêng, DLL riêng, **không** ref `EncosyTower.SourceGen.*` | theo tác giả, dùng `EncosyTower.CodeGen.Printer` |

Phần "SourceGen" mà bạn nhắc — build ra DLL — chính là §4.1–4.5 dưới đây. `Generators/` ở §4.6
là chuyện khác, chỉ để sinh bảng kiểu và không liên quan tới DLL.

## 4.1 Yêu cầu

- Không `ProjectReference` sang bất kỳ project nào trong `Plugins/SourceGenerator/`
  (EncosyTower) → update package EncosyTower không làm vỡ generator của ta.
- Solution riêng, build riêng, output DLL riêng, deploy vào asmdef của ta.
- Diff so với generator EncosyTower phải đọc được (P4) → giữ nguyên khung
  `IIncrementalGenerator` + `Spec` + `Spec+WriteCode` + `Printer`.
- **Hạ tầng build/deploy/test/debug: copy nguyên xi cách tác giả làm.** Đã đọc
  `Plugins/SourceGenerator/Directory.Build.props`, `EncosyTower.SourceGen.slnx`,
  `Samples/Directory.Build.props`, `EncosyTower.SourceGen.Tests/{Directory.Build.props,UnityDllPaths.targets}`,
  `README.md`.

## 4.2 Cây solution — theo khuôn tác giả

```
Plugins/SourceGenerator.ApexionGame/
├── ApexionGame.SourceGen.slnx               # định dạng .slnx như tác giả, không .sln
├── Directory.Build.props                    # ★ output path + target copy DLL (§4.5)
├── .editorconfig  .gitignore  README.md
├── Build/                                   # gitignore — bin/obj tập trung ở đây
│
├── ApexionGame.SourceGen.Common/            # ~2.5k LOC vendored từ EncosyTower.SourceGen.Common
│   ├── Printer.cs                           # 532 LOC — bắt buộc, cả WriteCode dựa vào nó
│   ├── EquatableArray.cs                    # 225 — cache key của incremental generator
│   ├── HashValue.cs                         # rút gọn từ 965 → ~120 (chỉ overload cần dùng)
│   ├── LocationInfo.cs
│   ├── SourceGenHelpers.cs                  # 167 — GetSourceGenConfigProvider, BuildSourceFilePath, OutputSource
│   ├── TypeCreationHelpers.cs               # 241 — GenerateOpeningAndClosingSource
│   ├── CompilationInfo.cs                   # references + skip-attribute detection
│   ├── SymbolExtensions.cs                  # rút gọn từ 2279 → ~300 (ToValidIdentifier, ToFileName, ...)
│   ├── SyntaxNodeExtensions.cs              # rút gọn → GetHintName, ...
│   ├── StringExtensions.cs  ExceptionExtensions.cs  ImmutableArrayBuilder.cs
│   └── SourceGenVersion.cs
│
├── ApexionGame.SourceGen.Helpers/
│   ├── Properties/launchSettings.json       # DebugRoslynComponent (§4.5.4)
│   └── Entities.Stats/
│       ├── StatGeneratorAPI.cs              # NAMESPACE, SKIP_ATTRIBUTE, EnumTypeMap
│       └── StatTypeTable.gen.cs             # 6 cột — SINH RA bởi editor generator (§4.6.3)
│
├── ApexionGame.SourceGen.Generators/
│   ├── Properties/launchSettings.json
│   └── Entities.Stats/
│       ├── StatSystemGenerator.cs           StatSystemSpec.cs      StatSystemSpec+WriteCode.cs
│       ├── StatCollectionGenerator.cs       StatCollectionSpec.cs  StatCollectionSpec+WriteCode.cs
│       └── StatDataGenerator.cs             StatDataSpec.cs        StatDataSpec+WriteCode.cs
│
├── ApexionGame.SourceGen.Analyzers/
│   └── Entities.Stats/
│       ├── StatSystemDiagnosticAnalyzer.cs
│       ├── StatCollectionDiagnosticAnalyzer.cs
│       └── StatDataDiagnosticAnalyzer.cs
│
├── ApexionGame.SourceGen.Tests/             # ★ §4.5.3
│   ├── Directory.Build.props                # UNITY_OS_INSTALL_ROOT + UnityProjectPath
│   ├── UnityDllPaths.targets                # sinh Helpers/UnityDllPaths.g.cs trước build
│   └── Entities.Stats/
│       ├── StatGeneratorTests.cs
│       ├── Stat{System,Collection,Data}DiagnosticAnalyzerTests.cs
│       └── StatsAnalyzerStubs.cs
│
└── Samples/                                 # ★ §4.5.2 — vũ khí chính của dev loop
    ├── Directory.Build.props                # ref Unity DLL + ref generator dạng Analyzer
    └── Samples.Entities.Stats/
        ├── Samples.Entities.Stats.csproj    # 5 dòng, mọi cấu hình ở Directory.Build.props
        ├── Program.cs
        └── Samples.Entities.Stats.cs        # khai báo [StatSystem] + [StatCollection] thật
```

**Vendoring:** mọi file copy từ EncosyTower giữ license header gốc + thêm dòng nguồn, theo
[CODING-CONVENTIONS.md §6](../../../../CODING-CONVENTIONS.md). Namespace đổi sang
`ApexionGame.SourceGen.*`. Rút gọn `HashValue`/`SymbolExtensions` là *có chủ đích*: chỉ mang
theo overload thực sự dùng.

## 4.3 csproj chuẩn

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>10.0</LangVersion>
    <IsRoslynComponent>true</IsRoslynComponent>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    <IncludeSymbols>false</IncludeSymbols>
    <DevelopmentDependency>true</DevelopmentDependency>
  </PropertyGroup>
  <ItemGroup>
    <Compile Remove="bin/**" /><EmbeddedResource Remove="bin/**" /><None Remove="bin/**" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

`Microsoft.CodeAnalysis.CSharp` **4.3.1** — lý do chính xác từ README của tác giả: *Unity 2022.3
và 6000.0 đều dùng Roslyn Analyzer 4.3.1*, kiểm tra được ở
`[UnityInstallRoot]/Editor/Data/DotNetSdkRoslyn/csc.deps.json`. Không nâng: version cao hơn
Roslyn host sẽ không load được analyzer.

`Microsoft.CodeAnalysis.Workspaces.Common 4.3.1` chỉ cần khi làm **code fix / refactor** — bản
port phase 3 chưa làm, để phase 5 (task 5.3).

## 4.4 Deploy vào Unity

Đích: `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/SourceGenerators/`

```
SourceGenerators/
├── ApexionGame.SourceGen.Common.dll      (+ .meta)
├── ApexionGame.SourceGen.Helpers.dll     (+ .meta)
├── ApexionGame.SourceGen.Generators.dll  (+ .meta)
└── ApexionGame.SourceGen.Analyzers.dll   (+ .meta)
```

`.meta` phải có (copy khuôn từ `EncosyTower.Core/SourceGenerators/*.dll.meta`):

```yaml
labels:
- RunOnlyOnAssembliesWithReference     # chỉ áp cho assembly có reference tới asmdef này
- RoslynAnalyzer                       # Unity nhận diện là analyzer
- SourceGenerator
PluginImporter:
  isExplicitlyReferenced: 1
  validateReferences: 0
  platformData:                        # TẤT CẢ platform enabled: 0
  - first: { : Any }
    second: { enabled: 0, settings: { Exclude Editor: 1, Exclude Win64: 1, ... } }
```

Vì DLL nằm trong thư mục do `ApexionGame.Entities.Stats.asmdef` quản lý + label
`RunOnlyOnAssembliesWithReference`, generator chạy trên **assembly game nào reference tới
`ApexionGame.Entities.Stats`** — đúng nơi user viết `[StatSystem]`/`[StatCollection]`.

**Chặn generator chạy trên chính assembly runtime:**

```csharp
// SkipSourceGeneratorsForAssemblyAttribute.cs  ← hiện là placeholder, cần sửa
using System;

namespace ApexionGame.Entities.Stats
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class SkipSourceGeneratorsForAssemblyAttribute : Attribute { }
}
```

```csharp
// AssemblyInfo.cs  ← hiện đang rỗng
[assembly: ApexionGame.Entities.Stats.SkipSourceGeneratorsForAssembly]
```

Mọi generator đọc cờ này qua `CompilationInfo.GetCompilation(x, c, NAMESPACE, SKIP_ATTRIBUTE)`
— y như EncosyTower.

## 4.5 Build, deploy, test, debug — copy nguyên cách tác giả

### 4.5.1 Deploy bằng MSBuild target, KHÔNG bằng script

Tác giả không có `build.ps1`. Tất cả nằm trong một `Directory.Build.props` ở gốc solution:

```xml
<!-- Plugins/SourceGenerator.ApexionGame/Directory.Build.props -->
<Project>
    <PropertyGroup>
        <BaseOutputPath>$(MSBuildThisFileDirectory)Build/$(MSBuildProjectName)/bin/</BaseOutputPath>
        <BaseIntermediateOutputPath>$(MSBuildThisFileDirectory)Build/$(MSBuildProjectName)/obj/</BaseIntermediateOutputPath>
        <ResultsDirectory>$(MSBuildThisFileDirectory)Build/$(MSBuildProjectName)/TestResults/</ResultsDirectory>
        <LangVersion>10.0</LangVersion>
        <NoWarn>$(NoWarn);CS1591</NoWarn>
    </PropertyGroup>

    <Target Name="CopyBuildArtifacts" AfterTargets="Build"
            Condition="'$(Configuration)'=='Release' AND '$(MSBuildProjectName)'!='ApexionGame.SourceGen.Tests'">
        <ItemGroup>
            <DataFiles Include="$(OutDir)*.dll" />
        </ItemGroup>
        <Copy SourceFiles="@(DataFiles)"
              DestinationFolder="$(MSBuildThisFileDirectory)../../Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/SourceGenerators/"
              SkipUnchangedFiles="true" />
    </Target>
</Project>
```

Lệnh build duy nhất:

```
dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release
```

DLL tự vào đúng chỗ. Ba chi tiết đáng học:

| | |
|---|---|
| `BaseOutputPath` → `Build/<Project>/bin/` | mọi `bin`/`obj` tập trung một thư mục, `.gitignore` một dòng |
| `Condition` loại project Tests | tránh copy DLL test + dependency test vào Unity |
| `SkipUnchangedFiles="true"` | không touch file khi nội dung không đổi → Unity không reimport, không mất `.meta` |

`.meta` commit sẵn và `Copy` chỉ ghi `.dll` → GUID + labels giữ nguyên qua mọi lần build.

### 4.5.2 `Samples/` — dev loop thật của source generator

Đây là thứ giá trị nhất trong setup của tác giả, và là lý do **không cần** mở Unity để phát
triển generator: một project netstandard2.1 **Exe** reference (a) các DLL Unity thật, (b) các
project generator dưới dạng **Analyzer**. Generated code được compile ngay trong IDE, có
IntelliSense, có lỗi compile thật, step-debug được.

```xml
<!-- Samples/Directory.Build.props -->
<Project>
    <PropertyGroup>
        <UnityInstallRootPath>$([System.Environment]::GetEnvironmentVariable('UNITY_OS_INSTALL_ROOT'))</UnityInstallRootPath>
        <UnityProjectPath>$(MSBuildProjectDirectory)/../../../../</UnityProjectPath>
        <BaseOutputPath>$(MSBuildThisFileDirectory)../Build/$(MSBuildProjectName)/bin/</BaseOutputPath>
        <BaseIntermediateOutputPath>$(MSBuildThisFileDirectory)../Build/$(MSBuildProjectName)/obj/</BaseIntermediateOutputPath>
    </PropertyGroup>

    <PropertyGroup>
        <LangVersion>10.0</LangVersion>          <!-- khớp csc.rsp của asmdef -->
        <OutputType>Exe</OutputType>
        <TargetFramework>netstandard2.1</TargetFramework>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <ImplicitUsings>disable</ImplicitUsings>
        <Nullable>disable</Nullable>
    </PropertyGroup>

    <!-- giả lập define của Unity -->
    <PropertyGroup>
        <DefineConstants>$(DefineConstants);UNITY_5_3_OR_NEWER;UNITY_EDITOR;UNITY_BURST;UNITY_COLLECTIONS;UNITY_MATHEMATICS</DefineConstants>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Unity3D" Version="3.1.1" />
    </ItemGroup>

    <ItemGroup>
        <Reference Include="$(UnityEditorPath)" Private="false" />
        <Reference Remove="$(UnityEnginePath)" />
        <Reference Include="$(UnityModulesPath)/UnityEngine.CoreModule.dll" Private="false" />
        <Reference Include="$(UnityScriptAssembliesPath)/Unity.Burst.dll" Private="false" />
        <Reference Include="$(UnityScriptAssembliesPath)/Unity.Collections.dll" Private="false" />
        <Reference Include="$(UnityScriptAssembliesPath)/Unity.Mathematics.dll" Private="false" />
        <Reference Include="$(UnityScriptAssembliesPath)/EncosyTower.Core.dll" Private="false" />
        <Reference Include="$(UnityScriptAssembliesPath)/ApexionGame.Entities.Stats.dll" Private="false" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="$(MSBuildThisFileDirectory)../ApexionGame.SourceGen.Common/ApexionGame.SourceGen.Common.csproj"
                          OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
        <ProjectReference Include="$(MSBuildThisFileDirectory)../ApexionGame.SourceGen.Helpers/ApexionGame.SourceGen.Helpers.csproj"
                          OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
        <ProjectReference Include="$(MSBuildThisFileDirectory)../ApexionGame.SourceGen.Generators/ApexionGame.SourceGen.Generators.csproj"
                          OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
        <ProjectReference Include="$(MSBuildThisFileDirectory)../ApexionGame.SourceGen.Analyzers/ApexionGame.SourceGen.Analyzers.csproj"
                          OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    </ItemGroup>
</Project>
```

Nhờ vậy mỗi sample csproj chỉ còn 5 dòng:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Samples.Entities.Stats</RootNamespace>
    </PropertyGroup>
</Project>
```

**Điều kiện:** biến môi trường `UNITY_OS_INSTALL_ROOT` trỏ tới thư mục cài Unity, và
`Library/ScriptAssemblies/` đã có `ApexionGame.Entities.Stats.dll` — nghĩa là **Unity phải
compile asmdef của ta ít nhất một lần** trước khi sample build được. Đúng ràng buộc tác giả có.

Hệ quả cho roadmap: exit criteria của phase 3 chuyển từ "compile sạch trong Unity" thành
"**`Samples.Entities.Stats` compile sạch bằng `dotnet build`**" — nhanh hơn nhiều, và chạy được
trên CI (chỗ này CI *thật sự* có ý nghĩa, khác với lý do sai tôi từng nêu ở DEC-003).

### 4.5.3 Tests — nạp DLL Unity vào Roslyn compilation

`EncosyTower.SourceGen.Tests` có `UnityDllPaths.targets` sinh `Helpers/UnityDllPaths.g.cs`
trước mỗi build rồi xoá sau, điền các partial method `InitAll` / `InitUnityModulesPath` /
`InitUnityScriptAssembliesPath` bằng đường dẫn tuyệt đối của từng DLL Unity. Test dùng danh
sách đó làm `MetadataReference` cho `CSharpCompilation` → generator được test trên **kiểu Unity
thật**, không phải stub.

Target còn `<Error Condition="!Exists(...)">` với message chỉ rõ phải set `UNITY_OS_INSTALL_ROOT`
hoặc phải mở Unity một lần — port nguyên cách làm này.

Bản port giữ cả hai tầng: `StatsAnalyzerStubs.cs` cho test nhanh không cần Unity, và
`UnityDllPaths` cho test tích hợp.

### 4.5.4 Debug generator

`Properties/launchSettings.json` trong project generator:

```json
{
  "profiles": {
    "ApexionGame.SourceGen.Generators": {
      "commandName": "DebugRoslynComponent",
      "targetProject": "../Samples/Samples.Entities.Stats/Samples.Entities.Stats.csproj"
    }
  }
}
```

Yêu cầu: cài **.NET Compiler Platform SDK**, set project generator làm startup project. Sau đó
F5 → breakpoint trong `WriteCode` dừng thật, xem được `Spec` và output từng bước.

Không có cái này thì debug generator = `Debug.Log` qua diagnostic. Rất nên có từ đầu phase 3.

## 4.6 Bảng kiểu — codegen editor-time (DEC-003 = B)

> **Hai loại codegen, đừng lẫn.** §4.2–4.5 nói về **Roslyn source generator** (chạy mỗi lần
> compile, output không nằm trên đĩa, độc lập hoàn toàn với EncosyTower). §4.6 nói về
> **editor-time codegen** sinh bảng kiểu `.gen.cs` — chạy tay khi maintainer đổi bảng, output
> commit vào repo, và dùng đúng cơ chế của tác giả (UnityCodeGen + `EncosyTower.CodeGen.Printer`).

### 4.6.1 Cơ chế UnityCodeGen — đã verify

| Thành phần | Sự thật |
|---|---|
| Discovery | `ScriptFileGenerator.Generate()` → `TypeCache.GetTypesDerivedFrom<ICodeGenerator>()` lọc `[Generator]` → chạy từ **mọi assembly đang load trong Editor**, kể cả asmdef runtime |
| Trigger | Menu `Tools/UnityCodeGen/Generate`, hoặc auto-on-compile (tắt mặc định, lưu `EditorUserSettings` per-user) |
| Ghi file | `File.WriteAllText` + `Directory.CreateDirectory` thuần → **ghi được ra ngoài `Assets/`** |
| Idempotent | So nội dung cũ, chỉ ghi khi khác, xong `AssetDatabase.Refresh()` |
| Output path | `CodeGenAPI.GetOutputFolderPathFromCaller([CallerFilePath], pathCombine)` → tính từ vị trí file source của generator, di chuyển thư mục không vỡ |
| `Printer` | `EncosyTower.Core/CodeGen/Printer.cs` (639 LOC) **không** bọc `#if UNITY_EDITOR` → dùng trực tiếp, không vendoring |
| asmdef ref | `AnnulusGames.UnityCodeGen.Editor` là `includePlatforms: ["Editor"]`. `EncosyTower.Core.asmdef` (all-platform) đã reference nó và chạy bình thường trong project này → pattern đã được chứng minh |
| Bật/tắt | Cả 6 file bọc `#if UNITY_EDITOR && ANNULUS_CODEGEN && APEXION_STAT_VALUE_TYPES_GENERATOR`; define **không** có trong Project Settings → compile-out ở build thường |

### 4.6.2 Layout

```
Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/Generators/
├── StatTypeTable.cs                       # ★ NGUỒN DUY NHẤT (C# thuần, type-checked)
├── StatVariantTypeGenerator.cs            → ../Common/StatVariantType.gen.cs
├── StatVariantGenerator.cs                → ../Common/StatVariant.gen.cs
├── StatDataSizeGenerator.cs               → ../Common/StatDataSize.gen.cs
├── StatVariantTypeExtensionsGenerator.cs  → ../Common/StatVariantTypeExtensions.gen.cs
├── StatSingleExtensionsGenerator.cs       → ../Common/StatSingleExtensions.gen.cs
└── StatTypeTableGenerator.cs              → ★ ../../../../Plugins/SourceGenerator.ApexionGame/
                                           #     ApexionGame.SourceGen.Helpers/Entities.Stats/
                                           #     StatTypeTable.gen.cs
```

### 4.6.3 Cải tiến 1 — một nguồn bảng kiểu

Bản gốc giữ bảng ở **hai nơi phải khớp tay**:

| Nơi | Cột |
|---|---|
| `EncosyTower.Entities.Stats/Generators/GeneratorAPI.cs` (Unity-side, editor-only) | `Types`, `TypeNames`, `Sizes`, `OneConstructors`, `EqualOperators` |
| `EncosyTower.SourceGen.Helpers/Entities.Stats/StatGeneratorAPI.cs` (Roslyn-side) | `Types`, `TypeNames`, `Sizes`, `Namespaces`, `EnumTypeMap` |

Hai file nằm ở **hai compilation unit khác nhau** (Unity asmdef vs netstandard2.0 csproj) nên
không share được file nguồn. Nhưng vì `ScriptFileGenerator` ghi bằng `File.WriteAllText`,
`StatTypeTableGenerator` **emit luôn** bảng cho phía Roslyn:

```csharp
// StatTypeTable.cs — master, 6 cột hợp nhất
internal readonly struct TypeRecord
{
    public readonly string Type;    // "float2"
    public readonly string Name;    // "Float2"
    public readonly int Size;       // 8
    public readonly string Ns;      // "Unity.Mathematics"  ("" = BCL, không cần prefix)
    public readonly string One;     // "new float2(1f)"     → OneVariant()
    public readonly bool Eq;        // có ==/!= hay không   (bool2x2 thì không)
}

internal static class StatTypeTable
{
    public static readonly TypeRecord[] All = { /* 65 kiểu */ };

    public static readonly string[] GameplayProfile = {
        "None", "bool",
        "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong",
        "half", "half2", "half3", "half4",
        "float", "float2", "float3", "float4",
        "double",
    };

    public static TypeRecord[] Active => Filter(GameplayProfile);
}
```

Đánh đổi: sau khi đổi bảng phải chạy lại `dotnet build -c Release` để build lại DLL generator. Một bước
có tài liệu, thay cho "sync tay hai file" của bản gốc.

Dùng C# thuần chứ không JSON: compiler bắt lỗi ngay, không cần parse, `TypeCache` thấy được.

### 4.6.4 Cải tiến 2 — profile `gameplay`

Độc lập với cơ chế, chỉ là lọc `StatTypeTable`. 19 kiểu, bỏ toàn bộ matrix `*x*` và
`bool2..bool4x4`:

| File | Bản gốc | Bản port |
|---|---|---|
| `StatVariant.gen.cs` | 3672 LOC | ~1.1k LOC |
| `StatVariantType.gen.cs` | 426 | ~130 |
| `StatSingleExtensions.gen.cs` | 322 | ~100 |
| `StatDataSize.gen.cs` | 194 | ~70 |
| `StatVariantTypeExtensions.gen.cs` | 115 | ~50 |
| switch/operator trong generated `StatDataStore` | 65 nhánh | 19 nhánh |

### 4.6.5 Bàn giao với `[EnumExtensions]`

`StatVariantTypeGenerator` emit đúng các `partial` stub như bản gốc:

```csharp
partial interface IStatVariantTypeExtensions { }
partial struct StatVariantTypeExtended { }
static partial class StatVariantTypeExtensions
{
    partial class DisplayNames { } partial class FixedDisplayNames { }
    partial class FixedNames { }   partial class Names { }
    partial class UnderlyingValues { } partial class Values { }
}
```

→ generator `[EnumExtensions]` của EncosyTower lấp phần còn lại.
`ZeroVariant`/`OneVariant` do generator **của ta** sinh (`StatVariantTypeExtensionsGenerator`)
nên không phụ thuộc EncosyTower — giảm rủi ro B2 ở
[§5.3.2](05-ASSEMBLY-LAYOUT.md#532-rủi-ro-còn-lại-của-b-phải-theo-dõi).

### 4.6.6 Workflow đổi bảng kiểu

1. Project Settings → Scripting Define Symbols: thêm `APEXION_STAT_VALUE_TYPES_GENERATOR`.
2. Sửa `Generators/StatTypeTable.cs` (thêm kiểu, hoặc sửa `GameplayProfile`).
3. Menu `Tools/UnityCodeGen/Generate`.
4. Bỏ define ở bước 1.
5. `dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release` (vì `StatTypeTable.gen.cs` phía Roslyn đã đổi).
6. Commit `.gen.cs` + DLL.

Nếu cần `float3x3`/`float4x4` (tensor, rotation): `size` phải ≤ `MaxDataSize` của `[StatSystem]`
mới dùng được, và ≤ `MaxDataSize / 2` mới dùng được ở chế độ pair.

## 4.7 Khung generator — mirror 1:1 theo tác giả

Đã đọc toàn bộ 9 file ở `EncosyTower.SourceGen.Generators/Entities.Stats/`. Cả ba generator
dùng **đúng một khuôn**. Port là điền lại khuôn đó, không thiết kế mới.

### 4.7.1 Khuôn `*Generator.cs` — giống hệt ở cả ba

```csharp
[Generator]
internal sealed class StatXxxGenerator : IIncrementalGenerator
{
    private const string NAMESPACE = StatGeneratorAPI.NAMESPACE;
    private const string SKIP_ATTRIBUTE = StatGeneratorAPI.SKIP_ATTRIBUTE;
    private const string XXX_ATTRIBUTE = $"global::{NAMESPACE}.StatXxxAttribute";
    private const string XXX_ATTRIBUTE_METADATA_NAME = $"{NAMESPACE}.StatXxxAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

        var compilationProvider = context.CompilationProvider
            .Select(static (x, c) => CompilationInfo.GetCompilation(x, c, NAMESPACE, SKIP_ATTRIBUTE));

        var candidateProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                  XXX_ATTRIBUTE_METADATA_NAME
                , static (node, _) => node is StructDeclarationSyntax { TypeParameterList: null }
                , ExtractSpec)
            .Where(static t => t.IsValid);

        var combined = candidateProvider
            .Combine(compilationProvider)
            .Combine(projectPathProvider)
            .Where(static t => t.Left.Right.isValid);      // ← chốt skip-attribute nằm ở đây

        context.RegisterSourceOutput(combined, static (spc, source) => GenerateOutput(
              spc, source.Left.Right, source.Left.Left
            , source.Right.projectPath, source.Right.outputSourceGenFiles));
    }

    private static StatXxxSpec ExtractSpec(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // 1. narrow syntax + symbol, return default nếu sai
        // 2. đọc ConstructorArguments / NamedArguments
        // 3. typeIdentifier = symbol.ToValidIdentifier();  hintName = syntaxTree.GetHintName(syntax, symbol.ToFileName());
        // 4. TypeCreationHelpers.GenerateOpeningAndClosingSource(syntax, token,
        //        out openingSource, out closingSource, printAdditionalUsings: PrintAdditionalUsings);
        // 5. return new StatXxxSpec { ..., location = LocationInfo.From(syntax.GetLocation()) };

        static void PrintAdditionalUsings(ref Printer p) { /* bảng alias — §4.7.4 */ }
    }

    private static void GenerateOutput(SourceProductionContext context, CompilationInfo compilation
        , StatXxxSpec candidate, string projectPath, bool outputSourceGenFiles)
    {
        if (candidate.IsValid == false) { return; }

        context.CancellationToken.ThrowIfCancellationRequested();

        try
        {
            var sourceFilePath = SourceGenHelpers.BuildSourceFilePath(
                compilation.assemblyName, candidate.hintName, projectPath);

            context.OutputSource(outputSourceGenFiles, candidate.openingSource
                , candidate.WriteCode(), candidate.closingSource
                , candidate.hintName, sourceFilePath, projectPath);
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException) { throw; }

            context.ReportDiagnostic(Diagnostic.Create(
                s_errorDescriptor, candidate.location.ToLocation(), e.ToUnityPrintableString()));
        }
    }

    private static readonly DiagnosticDescriptor s_errorDescriptor = new(
        "AGS_STAT_XXX_UNKNOWN_0001", "Stat Xxx Generator Error"
        , "This error indicates a bug in the Stat Xxx source generators. Error message: '{0}'."
        , XXX_ATTRIBUTE, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "");
}
```

Bốn điểm cần bám sát:

| # | Chi tiết | Vì sao |
|---|---|---|
| 1 | `try/catch` quanh `WriteCode()`, **re-throw `OperationCanceledException`**, còn lại báo diagnostic `*_UNKNOWN_0001` | generator crash không được làm chết cả compilation; nhưng huỷ thì phải để Roslyn xử lý |
| 2 | `.Where(static t => t.Left.Right.isValid)` sau khi `Combine(compilationProvider)` | `CompilationInfo.GetCompilation(x, c, NAMESPACE, SKIP_ATTRIBUTE)` đặt `isValid = false` khi assembly mang skip-attribute → toàn bộ generator no-op |
| 3 | `StatXxxSpec` phải implement `IEquatable<T>` **có `GetHashCode` ổn định**, và mọi collection dùng `EquatableArray<T>` | đây là cache key của incremental pipeline; sai chỗ này thì generator chạy lại mỗi keystroke |
| 4 | `hintName` từ `syntaxTree.GetHintName(syntax, symbol.ToFileName())` | tránh trùng hintName khi hai type cùng tên ở namespace khác |

### 4.7.2 Khuôn `*Spec.cs`

Struct phẳng chỉ chứa `string`/`int`/`bool`/`EquatableArray<T>` — **không** giữ `ISymbol`,
`SyntaxNode`, hay `Compilation` (giữ là rò rỉ bộ nhớ + phá cache). Vị trí lưu bằng
`LocationInfo` chứ không `Location`.

```csharp
internal partial struct StatXxxSpec : IEquatable<StatXxxSpec>
{
    public string typeName, typeNamespace, typeIdentifier, hintName, openingSource, closingSource;
    public LocationInfo location;
    // ... field riêng của từng loại

    public readonly bool IsValid => /* điều kiện tối thiểu để WriteCode chạy được */;

    public readonly bool Equals(StatXxxSpec other)
        => string.Equals(typeName, other.typeName, StringComparison.Ordinal) && /* ... */;

    public readonly override int GetHashCode()
        => HashValue.Combine(typeName, typeNamespace, /* ... */).Add(/* ... */);
}
```

Chú ý: `Equals`/`GetHashCode` của tác giả **cố ý bỏ qua** `hintName`/`openingSource`/`closingSource`/
`location` — chúng là dẫn xuất, đưa vào chỉ làm cache miss vô cớ.

### 4.7.3 Khuôn `*Spec+WriteCode.cs`

```csharp
partial struct StatXxxSpec
{
    private const string PR_AGGRESSIVE_INLINING = "[SRCS.MethodImpl(SRCS.MethodImplOptions.AggressiveInlining)]";
    private const string PR_EXCLUDE_COVERAGE = "[SDCA.ExcludeFromCodeCoverage]";
    private const string PR_GENERATED_CODE = $"[SCDC.GeneratedCode(\"...StatXxxGenerator\", \"{SourceGenVersion.VALUE}\")]";
    // ... hằng chuỗi cho MỌI type name dùng trong output

    public readonly string WriteCode()
    {
        var p = Printer.DefaultLarge;              // hoặc new Printer(0, 1024 * 512) cho output lớn

        p.PrintEndLine();
        p.Print("#pragma warning disable").PrintEndLine();
        p.PrintEndLine();

        p = p.IncreasedIndent();
        {
            WriteA(ref p); WriteB(ref p); /* ... một hàm cho mỗi vùng output */
        }
        p = p.DecreasedIndent();

        return p.Result;
    }

    private readonly void WriteA(ref Printer p) { /* p.PrintLine / OpenScope / CloseScope */ }
}
```

Quy ước của tác giả trong `WriteCode` (bám theo để diff đọc được):

- Mọi type name trong output là `private const string PR_*` ở đầu file, **không** viết chuỗi
  inline giữa thân hàm. `StatSystemSpec+WriteCode.cs` có ~90 hằng như vậy (dòng 10–140).
- Mỗi vùng output là một `private static void Write<Vùng>(ref Printer p, …)` riêng, mở đầu bằng
  `p.Print("#region    TÊN VÙNG")` + `#endregion` — nhờ vậy file 4.5k dòng vẫn định vị được.
- `Printer` API: `Print`, `PrintLine`, `PrintBeginLine`/`PrintEndLine`, `PrintIf`,
  `PrintLineIf`, `PrintRepeat`, `OpenScope`/`CloseScope`, `WithIncreasedIndent`,
  `IncreasedIndent`/`DecreasedIndent`.
- Output chia theo *khai báo partial nhiều lần* của cùng một type, mỗi lần một vùng
  (`partial class StatSystem // Stat`, `// ValuePair`, `// Impl: StatDataStore`, …) thay vì một
  khối khổng lồ.
- `#if UNITY_EDITOR || DEVELOPMENT_BUILD` được emit **quanh nhánh validate + `UnityDebug.LogError`**
  trong setter của `[StatData]` (xem `StatDataSpec+WriteCode.cs:144`) — release build chỉ còn
  đường gán thẳng.

### 4.7.4 Bảng alias `PrintAdditionalUsings`

Tác giả emit alias ngắn cho mọi namespace, đặt trong `#pragma warning disable CS0105` để không
cảnh báo using trùng. Bản port giữ y khuôn, đổi nội dung:

| Bản gốc | Bản port |
|---|---|
| `using S = global::System;` … `SCDC`, `SD`, `SDCA`, `SRCS`, `SRIS` | **giữ nguyên** |
| `using ET = global::EncosyTower.Common;` | **giữ nguyên** (DEC-002 = B) |
| `using ETCol = global::EncosyTower.Collections;` | **giữ nguyên** |
| `using ETL = global::EncosyTower.Logging;` | **giữ nguyên** |
| `using ETDVD = global::EncosyTower.Debugging.ValidationDefines;` | → `using AGDVD = global::ApexionGame.Entities.Stats.Debugging.ValidationDefines;` |
| `using ETES = global::EncosyTower.Entities.Stats;` | → `using AGES = global::ApexionGame.Entities.Stats;` |
| `using UC`, `UCLU`, `UM`, `UE`, `UB`, `UJ` | **giữ nguyên** (Collections / LowLevel.Unsafe / Mathematics / UnityEngine / Burst / Jobs) |
| `using UECS = global::Unity.Entities;` | **xoá** |
| `using StatSystem = <statSystemFullTypeName>;` (chỉ ở `StatCollectionGenerator`) | **giữ nguyên** — mẹo hay: alias động theo `[StatCollection(typeof(TSystem))]` để output không cần full name |

Và các type ECS bị thay:

| Bản gốc | Bản port |
|---|---|
| `UECS.IBufferElementData` | *xoá* |
| `UECS.Entity` | `AGES.StatOwnerHandle` |
| `UECS.DynamicBuffer<Stat>` | `AGES.StatBuffer<Stat>` |
| `UECS.BufferLookup<Stat>` / `ComponentLookup<StatOwner>` | `AGES.StatStore<Stat, StatModifier, StatObserver>` |
| `UECS.SystemState`, `IBaker`, `ComponentTypeSet`, `EntityManager`, `EntityCommandBuffer*` | *xoá* |

Vì giữ `ET.*`, assembly tiêu thụ `[StatSystem]` **phải reference `EncosyTower.Core`** →
analyzer `AGS_STAT_SYSTEM_0002` báo lỗi rõ ràng nếu thiếu.

### 4.7.5 Ba generator — khác biệt riêng

| Generator | Trigger | Đặc thù |
|---|---|---|
| `StatSystemGenerator` | `[StatSystem]` trên `TypeDeclarationSyntax { TypeParameterList: null }` | Đọc `maxDataSize` (byte) + `maxUserDataSize` (byte → làm tròn 1/2/4). `FilterTypes` chia bảng kiểu thành `singleTypes` (size ≤ MaxDataSize), `pairTypes` (size ≤ MaxDataSize/2), `incompatTypes`. 21 hàm `Write*`, emit ~4.8k dòng |
| `StatCollectionGenerator` | `[StatCollection]` trên `StructDeclarationSyntax { TypeParameterList: null }` | Validate `statSystemType.HasAttribute(STAT_SYSTEM_ATTRIBUTE)`; quét `ChildNodes()` tìm nested struct có `[StatData]` bằng `syntax.GetAttribute(NAMESPACE, "StatData", token)` (**syntax-level**, không semantic); `fieldName = Identifier.ValueText.ToPublicFieldName()`; chặn `typeIdOffset + count > uint.MaxValue`; `enumUnderlyingType` chọn `byte`/`ushort`/`int` theo số stat. 16 hàm `Write*` |
| `StatDataGenerator` | `[StatData]` trên `StructDeclarationSyntax { TypeParameterList: null }` | Hai nhánh: `TypedConstantKind.Enum` → tra bảng kiểu theo index; `TypedConstantKind.Type` → phải là enum, tra `EnumTypeMap` theo underlying type, `GetUnmanagedSize`. Đọc named arg `SingleValue`. Chỉ 3 hàm `Write*` (~237 dòng) — **làm trước tiên** vì nhỏ nhất mà chạy thông cả pipeline |

Bản port đổi đúng ba chỗ ở `StatCollectionGenerator`: bỏ ràng buộc `IComponentData`, emit
`Builder`/`Builder<T>` thay `Baker`/`Baker<T>`, và `Build(ref store)` + `ToStats()` thay
`Bake(ibaker, entity)` + `CreateComponentData<T>()` + `AddComponentToEntity()`.
`StatDataGenerator` **không đổi gì** (không liên quan ECS).

## 4.8 Analyzer & diagnostic ID

Tác giả đánh ID **theo từng area**, không phải một dãy phẳng. Bản port giữ đúng cách đó,
đổi prefix `SG_` → `AGS_`:

| Bản gốc | Bản port | Mức | Nội dung |
|---|---|---|---|
| `SG_STAT_SYSTEM_0001` | `AGS_STAT_SYSTEM_0001` | Error | `[StatSystem]` không được là generic type |
| *(mới)* | `AGS_STAT_SYSTEM_0002` | Error | Assembly dùng `[StatSystem]` nhưng không reference `EncosyTower.Core` |
| `SG_STAT_COLLECTION_0001` | `AGS_STAT_COLLECTION_0001` | Error | Type argument của `[StatCollection]` phải mang `[StatSystem]` |
| `SG_STAT_COLLECTION_0002` | `AGS_STAT_COLLECTION_0002` | Error | `typeIdOffset + StatData count` vượt `uint.MaxValue` |
| `SG_STAT_COLLECTION_0003` | `AGS_STAT_COLLECTION_0003` | Error | `[StatCollection]` chỉ áp được cho struct |
| `SG_STAT_COLLECTION_0004` | `AGS_STAT_COLLECTION_0004` | Error | `[StatCollection]` không được là generic struct |
| *(mới)* | `AGS_STAT_COLLECTION_0005` | Warning | Nested struct trong `[StatCollection]` thiếu `[StatData]` |
| `SG_STAT_DATA_0001` | `AGS_STAT_DATA_0001` | Error | `[StatData]` chỉ áp được cho struct |
| `SG_STAT_DATA_0002` | `AGS_STAT_DATA_0002` | Error | `[StatData]` không được là generic struct |
| `SG_STAT_DATA_0003` | `AGS_STAT_DATA_0003` | Error | `StatVariantType.None` không hợp lệ |
| `SG_STAT_DATA_0004` | `AGS_STAT_DATA_0004` | Error | `typeof` argument phải là enum |
| *(mới)* | `AGS_STAT_DATA_0005` | Error | `size` vượt `MaxDataSize`; hoặc `SingleValue = false` mà `size > MaxDataSize / 2` |
| *(mới)* | `AGS_STAT_DATA_0006` | Warning | `StatModifier` có field `StatHandle` nhưng chưa implement `OnRemapObservedStats` (DEC-004) |
| `SG_ENTITIES_STAT_{SYSTEM,COLLECTION,DATA}_UNKNOWN_0001` | `AGS_STAT_{SYSTEM,COLLECTION,DATA}_UNKNOWN_0001` | Error | Lỗi nội bộ generator (bug), kèm `e.ToUnityPrintableString()` |

Khuôn analyzer (theo `StatDataDiagnosticAnalyzer`, 151 LOC):

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class StatXxxDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule1 = new(
        id: "AGS_STAT_XXX_0001", title: "…", messageFormat: "\"{0}\" …"
        , category: "StatXxxGenerator", defaultSeverity: DiagnosticSeverity.Error
        , isEnabledByDefault: true, description: "…");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule1, /* … */);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }
    // AnalyzeSymbol: check tuần tự, mỗi lỗi report rồi return sớm
    // location: attrib.ApplicationSyntaxReference?.GetSyntax(token)?.GetLocation() ?? typeSymbol.Locations[0]
}
```

Hai chi tiết đáng bám: `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` để
analyzer không tự soi code do generator sinh ra, và `EnableConcurrentExecution()`.

## 4.9 Test generator

`ApexionGame.SourceGen.Tests` dùng `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing`
+ `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` (xUnit), mô hình theo
`EncosyTower.SourceGen.Tests/Entities.Stats/StatGeneratorTests.cs` + `Stats/StatsAnalyzerStubs.cs`
(stub các runtime type để không cần Unity assembly).

Ba nhóm test:
1. **Generator snapshot** — input `[StatSystem]`/`[StatCollection]` → assert generated source
   compile được và chứa các member kỳ vọng.
2. **Analyzer** — mỗi diagnostic ID một case dương + một case âm.
3. **Incrementality** — sửa file không liên quan → generator không chạy lại (`EquatableArray`
   + `Spec.Equals` hoạt động đúng).
