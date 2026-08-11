# 05 — Assembly Layout & Dependencies

## 5.1 Cây thư mục runtime

```
Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/
├── ApexionGame.Entities.Stats.asmdef
├── AssemblyInfo.cs
├── SkipSourceGeneratorsForAssemblyAttribute.cs
├── csc.rsp                                  # -langversion:10
│
├── Documentation~/                           # bộ doc này (tilde → Unity bỏ qua)
├── SourceGenerators/                         # 4 DLL analyzer (§4.4)
│
├── Annotations/
│   ├── StatSystemAttribute.cs
│   ├── StatCollectionAttribute.cs
│   └── StatDataAttribute.cs
│
├── Contracts/
│   ├── IStat.cs
│   ├── IStatData.cs
│   ├── IStatModifier.cs
│   ├── IStatModifierStack.cs
│   ├── IStatObserver.cs
│   └── IStatValuePair.cs
│
├── Common/
│   ├── None.cs
│   ├── StatVariant.cs
│   ├── StatVariant.gen.cs                    ★ sinh bởi Generators/ (§4.6), commit vào repo
│   ├── StatVariantType.gen.cs                ★
│   ├── StatVariantTypeExtensions.gen.cs      ★
│   ├── StatDataSize.gen.cs                   ★
│   ├── StatSingle.cs
│   ├── StatSingleExtensions.gen.cs           ★
│   ├── StatUserDataSize.cs
│   ├── StatHandle.cs
│   ├── StatIndex.cs
│   ├── StatModifierHandle.cs
│   ├── StatModifierRecord.cs
│   ├── ModifierRange.cs
│   ├── ObserverRange.cs
│   ├── StatChangeEvent.cs
│   ├── ModifierTriggerEvent.cs
│   ├── StatData.cs                           # StatData<T>, StatDataParams<T>, StatValueParams<T>
│   └── ThrowHelper.cs
│
├── Storage/                                  ★ layer mới, thay ECS — ĐÃ IMPLEMENT
│   ├── StatOwnerHandle.cs
│   ├── StatOwnerSlot`3.cs                    # internal
│   ├── StatBuffer`1.cs
│   ├── StatStore`3.cs
│   └── StatStore`3+Lookup.cs
│
├── APIs/
│   ├── StatAPI.cs
│   ├── StatAccessor.cs
│   ├── StatAccessor+ReadOnly.cs
│   ├── StatReader.cs
│   ├── StatBuilder.cs                        # thay StatBaker.cs
│   ├── StatWorldData.cs
│   └── StatJobs.cs
│
├── Debugging/
│   └── ValidationDefines.cs                  # 5 const riêng của ta (không dùng ENCOSY_*)
│
└── Generators/                               ★ editor-time codegen (DEC-003 = B, §4.6)
    ├── StatTypeTable.cs                      # nguồn duy nhất của bảng kiểu
    ├── StatVariantTypeGenerator.cs
    ├── StatVariantGenerator.cs
    ├── StatDataSizeGenerator.cs
    ├── StatVariantTypeExtensionsGenerator.cs
    ├── StatSingleExtensionsGenerator.cs
    └── StatTypeTableGenerator.cs             # emit bảng cho phía Roslyn
```

Cả 7 file trong `Generators/` bọc
`#if UNITY_EDITOR && ANNULUS_CODEGEN && APEXION_STAT_VALUE_TYPES_GENERATOR` → compile-out
hoàn toàn ở build thường.

Không có thư mục `Internals/`: `ByteBool`, `Option<T>`, `HashValue`, `IIsValid`, `IIsCreated`,
`InsertRangeSpan`, `IncreaseCapacityTo`, `AsReadOnlySpan`, `ValueAsUnsafeRefRW` lấy trực tiếp
từ `EncosyTower.Core` (DEC-002 = B).

Tuân [CODING-CONVENTIONS.md §6](../../../../CODING-CONVENTIONS.md):
một primary type / file, partial split `TypeName+Aspect.cs`, generated `*.gen.cs` không sửa tay.

## 5.2 asmdef

**Hiện tại (bạn đã tạo):**

```json
{
    "name": "ApexionGame.Entities.Stats",
    "rootNamespace": "ApexionGame.Entities.Stats",
    "references": [
        "AnnulusGames.UnityCodeGen.Editor",
        "EncosyTower.Core",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Mathematics"
    ],
    "allowUnsafeCode": false
}
```

**Chốt (DEC-002 = B, DEC-003 = B):**

```json
{
    "name": "ApexionGame.Entities.Stats",
    "rootNamespace": "ApexionGame.Entities.Stats",
    "references": [
        "AnnulusGames.UnityCodeGen.Editor",
        "EncosyTower.Core",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Mathematics"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [
        { "name": "com.annulusgames.unity-codegen", "expression": "", "define": "ANNULUS_CODEGEN" },
        { "name": "com.unity.burst",               "expression": "", "define": "UNITY_BURST" },
        { "name": "com.unity.collections",         "expression": "", "define": "UNITY_COLLECTIONS" },
        { "name": "com.unity.mathematics",         "expression": "", "define": "UNITY_MATHEMATICS" }
    ],
    "noEngineReferences": false
}
```

So với asmdef hiện tại của bạn, chỉ **hai** thay đổi:

1. **`allowUnsafeCode: false` → `true` — bắt buộc.** `StatBuffer<T>` giữ `UnsafeList<T>*`;
   `StatDataStore` là `[StructLayout(Explicit)]` union; `StatAPI.GetStatRefUnsafe` trả `ref`.
   Không có đường tránh.
2. **Thêm 4 `versionDefines`** — `ANNULUS_CODEGEN` là cái quan trọng nhất: nó khiến
   `Generators/` compile-out khi package UnityCodeGen không có mặt.

`EncosyTower.Core` **giữ** (DEC-002 = B). `AnnulusGames.UnityCodeGen.Editor` **giữ**
(DEC-003 = B) — asmdef đó là `includePlatforms: ["Editor"]` nên không vào player build, và
`EncosyTower.Core.asmdef` (all-platform, đang compile trong project này) đã reference nó rồi,
nên đây là dependency đã hiện diện, chi phí thêm = 0.

## 5.3 Phụ thuộc EncosyTower (DEC-002 = B)

Những gì lấy từ `EncosyTower.Core`:

| Thứ dùng | Nơi dùng | Ghi chú |
|---|---|---|
| `EncosyTower.Common.ByteBool` | `Stat._valueIsPair`, `_produceChangeEvents`, `ValuePair._isPair`, `StatReader._isCreated` | struct 1 byte thay `bool` để layout xác định |
| `EncosyTower.Common.Option<T>` | `StatDataParams<T>`, `StatValueParams<T>`, `Options.Data` | optional value-type |
| `EncosyTower.Common.HashValue` | `GetHashCode` của mọi handle | |
| `EncosyTower.Common.IIsValid` / `IIsCreated` | capability interface | |
| `EncosyTower.Collections.InsertRangeSpan` | `AddObserversOfStatToList`, `TryGetObserversOfStat` | insert + trả `Span` để copy |
| `EncosyTower.Collections.IncreaseCapacityTo` | `TryGetModifiersOfStat`, batch API | |
| `EncosyTower.Collections.Unsafe.ValueAsUnsafeRefRW` | `worldData._modifierStackRef` → `ref stack` | |
| `AsReadOnlySpan()` trên `NativeArray<T>` | nhiều nơi | |
| `EncosyTower.TypeWraps.WrapTypeAttribute` | `StatIndex`, `StatIndex<T>` | source generator của EncosyTower — **có dùng** |
| `EncosyTower.EnumExtensions.EnumExtensionsAttribute` | `StatVariantType` | source generator của EncosyTower — **có dùng** |
| `EncosyTower.Debugging.ValidationDefines` | mọi `[Conditional]` guard | **KHÔNG dùng** — ta có `ValidationDefines` riêng để guard của stats có công tắc độc lập với `ENCOSY_*` |
| `EncosyTower.CodeGen.Printer` (639 LOC, không editor-only) | 6 editor generator trong `Generators/` | **có dùng** — DEC-003 = B, khỏi vendoring |
| `EncosyTower.CodeGen.CodeGenAPI.GetOutputFolderPathFromCaller` | tính output path từ `[CallerFilePath]` | **có dùng** |

### 5.3.1 Cơ chế skip-attribute — đã verify, không có xung đột

Tôi đã kiểm tra 31 chỗ khai báo `SKIP_ATTRIBUTE` trong `Plugins/SourceGenerator/`. Kết luận:
**mỗi area của EncosyTower có skip-attribute riêng, theo namespace của area đó.**

```
TypeWrapGenerator        → global::EncosyTower.TypeWraps.SkipSourceGeneratorsForAssemblyAttribute
EnumExtensionsGenerator  → global::EncosyTower.EnumExtensions.SkipSourceGeneratorsForAssemblyAttribute
Stat*Generator           → global::EncosyTower.Entities.Stats.SkipSourceGeneratorsForAssemblyAttribute
```

Hệ quả cho assembly của ta:

| | |
|---|---|
| Ta khai báo `[assembly: ApexionGame.Entities.Stats.SkipSourceGeneratorsForAssembly]` | → **generator của ta** bỏ qua assembly runtime của ta ✅ |
| Ta **không** khai báo `EncosyTower.TypeWraps.*` / `EncosyTower.EnumExtensions.*` skip attribute | → `[WrapType]` và `[EnumExtensions]` của EncosyTower **vẫn chạy** trên assembly của ta ✅ |
| Attribute của ta là `ApexionGame.Entities.Stats.StatSystemAttribute` (metadata name khác) | → `StatSystemGenerator` của EncosyTower **không** trigger trên khai báo của ta ✅ |
| `EncosyTower.Entities.Stats.asmdef` có `defineConstraints: ["UNITY_ENTITIES"]`, project không có `com.unity.entities` | → assembly stats của EncosyTower **không được compile** trong project này; chỉ DLL generator được load, và vì namespace attribute khác nhau nên không giao thoa ✅ |

→ Mối lo tôi nêu ban đầu ("phải xử lý mâu thuẫn skip-attribute") **không tồn tại**.
Phương án B chạy sạch.

### 5.3.2 Rủi ro còn lại của B (phải theo dõi)

| # | Rủi ro | Giảm thiểu |
|---|---|---|
| B1 | Update EncosyTower đổi output của `[WrapType]` → API public của `StatIndex` đổi theo | `StatIndex` chỉ cần `value` + operator + `IsValid` + `ToFixedString`. Nếu `[WrapType]` gây vỡ, **viết tay 70 LOC/type** là escape hatch — chi phí thấp, ghi nhận sẵn |
| B2 | Update EncosyTower đổi output của `[EnumExtensions]` → `StatVariantTypeExtensions` đổi | Chỉ dùng `Names`/`Values`/`ToStringFast` mức tối thiểu; `ZeroVariant`/`OneVariant` do **generator của ta** sinh nên không bị ảnh hưởng |
| B3 | Project khác (không có EncosyTower) không dùng được assembly này | Chấp nhận — đây là repo nội bộ ApexionGames, EncosyTower là dependency mặc định |
| B4 | Nâng Roslyn của EncosyTower ≠ Roslyn pin của generator ta (4.3.1) | Cả hai load vào cùng Roslyn host của Unity; pin thấp hơn thì an toàn. Nếu Unity nâng Roslyn thì cả hai đều phải nâng |

## 5.4 Define symbols

| Symbol | Nguồn | Ý nghĩa |
|---|---|---|
| `UNITY_BURST` / `UNITY_COLLECTIONS` / `UNITY_MATHEMATICS` | versionDefines | có package tương ứng |
| `ANNULUS_CODEGEN` | versionDefines | có `com.annulusgames.unity-codegen` → cho phép compile `Generators/` |
| `APEXION_STAT_VALUE_TYPES_GENERATOR` | maintainer bật tay ở Project Settings | bật 6 editor generator sinh bảng kiểu `.gen.cs`, bật xong thì **tắt lại** ([§4.6.6](04-CODEGEN.md#466-workflow-đổi-bảng-kiểu)) |
| `APEXION_RUNTIME_CHECKS` | user | bật mọi guard ở release |
| `APEXION_STATS_RUNTIME_CHECKS` | user | chỉ bật guard của stats |
| `DISABLE_APEXION_CHECKS` | user | tắt sạch guard |
| `ENABLE_UNITY_COLLECTIONS_CHECKS` | Unity | bật `AtomicSafetyHandle` trong `StatStore`/`StatBuffer` |

Cố ý **không** dùng `ENCOSY_RUNTIME_CHECKS`/`ENCOSY_STATS_RUNTIME_CHECKS`: guard của stats
phải có công tắc riêng, không bị bật/tắt kèm theo cấu hình EncosyTower của project.

## 5.5 AssemblyInfo.cs

```csharp
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApexionGame.Entities.Stats.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ApexionGame.Entities.Stats.Editor")]
[assembly: ApexionGame.Entities.Stats.SkipSourceGeneratorsForAssembly]
```

Chỉ khai báo skip-attribute **của ta**. Tuyệt đối không thêm
`EncosyTower.TypeWraps.SkipSourceGeneratorsForAssembly` hay
`EncosyTower.EnumExtensions.SkipSourceGeneratorsForAssembly` — thêm vào là `StatIndex` và
`StatVariantTypeExtensions` mất phần generated (xem §5.3.1).

## 5.6 Assembly bổ sung (phase sau)

| Assembly | Phase | Nội dung |
|---|---|---|
| `ApexionGame.Entities.Stats.Tests` | 2 | EditMode test (NUnit) — golden test đối chiếu hành vi bản gốc |
| `ApexionGame.Entities.Stats.PlayModeTests` | 3 | Burst/job integration test |
| `ApexionGame.Entities.Stats.Editor` | 5 | Inspector, stat graph debugger, `[ApiForEditor]` |
| `ApexionGame.Entities.Stats.Authoring` | 4 | `ScriptableObject` định nghĩa stat + factory |
