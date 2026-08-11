# 06 — Roadmap

Ước lượng LOC là *sản phẩm cuối*, không tính test.

## Phase 0 — Chốt thiết kế ✅

| Deliverable | Trạng thái |
|---|---|
| Bộ doc `Documentation~/` | ✅ |
| DEC-001 = `StatOwnerHandle` · DEC-002 = B · DEC-003 = **B** (đảo lại) · DEC-004 = A | ✅ |
| DEC-005 để mặc định A (giữ hành vi bản gốc tới phase 4) | ✅ |
| Verify cơ chế skip-attribute của EncosyTower (điều kiện để DEC-002 = B chạy được) | ✅ [§5.3.1](05-ASSEMBLY-LAYOUT.md#531-cơ-chế-skip-attribute--đã-verify-không-có-xung-đột) |
| Verify cơ chế UnityCodeGen + `EncosyTower.CodeGen` (dẫn tới đảo DEC-003) | ✅ [§4.6.1](04-CODEGEN.md#461-cơ-chế-unitycodegen--đã-verify) |
| Sửa `asmdef`: `allowUnsafeCode: true`, thêm 4 `versionDefines` | ✅ |
| Sửa `SkipSourceGeneratorsForAssemblyAttribute.cs` (đang là `class` không kế thừa `Attribute`) | ✅ |
| Điền `AssemblyInfo.cs` (đang rỗng) | ✅ |

## Phase 1 — L1 + L2: primitives & storage seam

Thứ tự thực hiện thật khác thứ tự đánh số: storage seam (1.7/1.8) làm trước vì rủi ro cao
nhất (R1) và không phụ thuộc bảng kiểu.

| # | Việc | LOC | Phụ thuộc | Trạng thái |
|---|---|---|---|---|
| 1.0 | Sửa 4 sai lệch skeleton (`allowUnsafeCode`, `versionDefines`, skip attribute, `AssemblyInfo`) | — | — | ✅ |
| 1.1 | `Generators/StatTypeTable.cs` + 6 editor generator (UnityCodeGen, dùng `EncosyTower.CodeGen.Printer`) | ~750 | 1.0 | ✅ |
| 1.2 | Chạy `Tools/UnityCodeGen/Generate` → 5 file `.gen.cs` + `StatTypeTable.gen.cs` phía Roslyn (profile `gameplay`) | ~1.45k (gen) | 1.1 | ✅ |
| 1.3 | `StatVariant.cs` (operator, thủ công) — 19 nhánh/operator thay 65 | ~320 | 1.2 | ✅ |
| 1.4 | `Debugging/ValidationDefines.cs` + `Common/ThrowHelper.cs` | ~230 | 1.2 | ✅ |
| 1.5 | Handles: `StatOwnerHandle`, `StatIndex` (`[WrapType]`), `StatHandle`, `StatModifierHandle` | ~450 | 1.4 | ✅ |
| 1.6 | Ranges + events + `StatData<T>`/`Params` + `None` + `StatSingle` + `StatUserDataSize` | ~330 | 1.5 | 🔶 `ModifierTriggerEvent`/`StatModifierRecord` dời sang phase 2 (I-09) |
| 1.7 | **`StatBuffer<T>`** | ~280 | 1.4 | ✅ |
| 1.8 | **`StatStore<3>`** (slot, free-list, pooling, safety handle, Dispose) | ~520 | 1.7 | ✅ |
| 1.9 | Contracts (6 interface, kèm `RemapObservedStats`) + `StatOwnerRemap` | ~250 | 1.6 | 🔶 `IStatModifier` dời sang phase 2 (I-09) |

### ⚠ Cụm phụ thuộc quanh 1.2 — bảng phụ thuộc gốc bị ngược

`StatVariantType` (enum sinh bởi 1.2) là nút cổ chai của **toàn bộ** L1:

```
1.2 ─┬─▶ ThrowHelper (1.4)      : mọi guard nhận StatVariantType, dùng .ToStringFast()/.IsDefined()
     ├─▶ IStatValuePair/IStatData (1.9)
     ├─▶ StatVariant.cs (1.3)
     └─▶ StatData<T>, StatChangeEvent, ModifierTriggerEvent (1.6)
                │
                └─▶ StatIndex<TStatData>, StatHandle<TStatData> (1.5)  [where TStatData : IStatData]
```

Ngược lại, **output** của 1.2 cũng cần 1.3/1.4/1.6 mới compile: `StatVariant.gen.cs` gọi
`ThrowHelper.ThrowIfDestinationTypeMismatch/ThrowIfUnsupportedType/ThrowIfExplicitlyConvertToWrongType`
và cần `partial struct StatVariant`; `StatSingleExtensions.gen.cs` cần `StatSingle<T>`.

→ Đây là **cụm không thể xếp tuyến tính**. Cách làm: chạy 1.2 trước, chấp nhận assembly đỏ,
rồi lấp 1.3 + 1.4 + 1.9 + phần còn lại của 1.5/1.6 cho tới khi xanh trở lại.
`StatSingle<T>` đã làm sẵn để bớt một mắt xích.

Không còn task `Internals/` (~450 LOC) — DEC-002 = B lấy sẵn từ `EncosyTower.Common`.
`StatVariant.cs` giảm mạnh nhờ profile `gameplay` (DEC-003).

**Exit criteria:** unit test cho `StatBuffer`/`StatStore` — create/destroy 10k owner, recycle,
`Insert`/`RemoveAt` giữ đúng thứ tự, `StatBuffer` không dangling khi tạo owner mới,
`Dispose` không leak (`LeakDetection.Verbose`).

**Thêm vào exit criteria:** `StatVariant` là file duy nhất của phase 1 **viết lại** thay vì
port 1:1 (19 nhánh thay 65 — [I-11](07-DECISIONS.md#i-11--tập-kiểu-của-từng-toán-tử-co-lại-theo-profile-gameplay)),
nên rủi ro cao nhất nằm ở đó. `StatVariantTests` (22 test) khoá lại tập kiểu của từng toán tử:
unary `-` từ chối unsigned, `!` chỉ nhận `bool`, `& | ^ ~ << >>` từ chối floating-point,
`Min`/`Max` không so sánh unsigned qua đường signed, `Clamp` component-wise trên `float3`,
lệch kiểu toán hạng ném `StatVariantTypeException`. `StatHandleTests` (12 test) khoá
[I-07](07-DECISIONS.md#i-07--stathandleisvalid-dựa-vào-ownerisvalid-không-phải-so-sánh-với-null)
— handle có `Version == 0` phải **không** hợp lệ — cùng vòng đời `StatOwnerRemap`.

**Trạng thái: phase 1 đóng.** `ApexionGame.Entities.Stats` build 0 warning / 0 error,
assembly test build sạch (4 CS0649 có sẵn trong struct fake).

## Phase 2 — L3: port thuật toán

| # | Việc | LOC | Trạng thái |
|---|---|---|---|
| 2.0 | Ba type hoãn từ [I-09](07-DECISIONS.md#i-09--istatmodifier--2-type-kéo-theo-dời-sang-phase-2): `IStatModifier<3>`, `ModifierTriggerEvent<4>`, `StatModifierRecord<4>` | ~150 | ✅ |
| 2.1 | `StatAPI.cs` | ~1.3k | ✅ |
| 2.2 | `StatWorldData.cs` | ~200 | ✅ |
| 2.3 | `StatReader.cs` | ~90 | ✅ |
| 2.4a | `StatAccessor.cs` — lõi: create/read, set base/current, 4 hàm lan truyền, add/remove modifier | ~1.2k | ✅ |
| 2.4b | `` StatAccessor`6+Batch.cs `` — `TrySetStatData` ×4, `TrySetStatValues` ×4, `TrySet*ToStats` ×3 (gom theo owner), `TrySetStatProduceChangeEvents`/`UserData` ×2, 3 helper private | ~900 | ✅ |
| 2.5 | `` StatAccessor`6+ReadOnly.cs `` | ~180 | ✅ |
| 2.6 | `` StatBuilder`6.cs `` + `StatAPI.CreateStatOwnerHandle` | ~300 | ✅ |
| 2.7 | `StatJobs.cs` | ~130 | ✅ |
| 2.8 | `DestroyOwnerAndUpdateObservers` (bù TODO của bản gốc) | ~60 | ✅ |

2.0–2.3 là **một cụm phụ thuộc vòng** (`StatWorldData` → `ModifierTriggerEvent` → `IStatModifier`
→ `StatReader` → `StatAPI` → `StatWorldData`), buộc phải land cùng lúc. Đã xong, assembly xanh.

`StatAPI` bỏ 6 hàm ECS-only (`BakeStatComponents`, `GetStatComponentTypeSet`, 3× `AddStatComponents`,
2 helper của baker). `CreateStatOwner` mà [§3.3.1](03-API-SURFACE.md#331-statapi-static) dự kiến
được dời sang **2.6** cùng `StatBuilder`, vì nó trả về builder — cũng là nơi *None stat* ở index 0
được thêm vào buffer ([D-102](07-DECISIONS.md#d-102--giữ-none-stat-ở-index-0)).

**Exit criteria — golden test.** Đây là cửa chất lượng quan trọng nhất.

Đã viết: `TestStatSystem.cs` (bộ type tay đóng vai code sinh ra của phase 3 — `TestStat`,
`TestValuePair`, `TestModifier`, `TestModifierStack`, `TestObserver`, `TestComposer`) và
`StatGoldenTests.cs` (**22 test** phủ 13 scenario). Chạy được ngay mà không cần chờ codegen,
và sau này dùng làm tham chiếu để đối chiếu output của generator.

⚠ **Chưa chạy** — test runner cần Unity Editor. Mới chỉ compile sạch.

⚠ Phần "chạy trên **cả hai** hệ rồi so chuỗi event" **không** làm được: `EncosyTower.Entities.Stats`
có `defineConstraints: ["UNITY_ENTITIES"]` và project không cài `com.unity.entities`, nên assembly
gốc không hề được compile ở đây ([§5.3.1](05-ASSEMBLY-LAYOUT.md#531-cơ-chế-skip-attribute--đã-verify-không-có-xung-đột)).
Golden test vì vậy khẳng định **bất biến của thuật toán** (giá trị hội tụ, range shift, từ chối
vòng lặp) chứ không diff byte-đối-byte với bản gốc. Muốn diff thật thì phải cài `com.unity.entities`
tạm thời — quyết định riêng, chưa làm.

13 scenario:

1. Set base value không modifier.
2. 1 modifier Add, 1 Multiply, thứ tự áp dụng.
3. Modifier `AddFromStat` cross-owner → set stat nguồn → observer lan truyền.
4. Chuỗi 5 tầng A→B→C→D→E, set A.
5. Đồ thị kim cương A→{B,C}→D, set A → D được recalc bao nhiêu lần (ghi nhận D-01).
6. Từ chối self-observe.
7. Từ chối vòng lặp 2 tầng, 3 tầng.
8. `TryAddStatModifiersBatch` 20 modifier — `ModifierRange` cuối cùng đúng.
9. Remove modifier giữa range → `startIndex` các stat sau đúng.
10. `TryRemoveModifiersOfStat` → range về 0, observer sạch.
11. Destroy owner đang bị observe → lan truyền không crash, observer bị bỏ qua.
12. `ProduceChangeEvents = false` → không có event nhưng vẫn lan truyền.
13. `UserData` encode/decode `TypeId` với `TypeIdOffset`.

## Phase 3 — Codegen

| # | Việc | LOC | Trạng thái |
|---|---|---|---|
| 3.0 | `Annotations/` — `StatSystemAttribute`, `StatCollectionAttribute`, `StatDataAttribute` | ~145 | ✅ **thiếu trong kế hoạch gốc** ([I-21](07-DECISIONS.md#i-21--annotations-không-thuộc-task-nào-của-phase-1)) |
| 3.1 | `.slnx` + `Directory.Build.props` (output path + `CopyBuildArtifacts` target) + `.gitignore` — [§4.5.1](04-CODEGEN.md#451-deploy-bằng-msbuild-target-không-bằng-script) | — | ✅ |
| 3.2 | `Samples/Directory.Build.props` + `Samples.Entities.Stats` — [§4.5.2](04-CODEGEN.md#452-samples--dev-loop-thật-của-source-generator) | ~120 | ✅ build 0 lỗi |
| 3.3 | `launchSettings.json` `DebugRoslynComponent` cho 2 project generator — [§4.5.4](04-CODEGEN.md#454-debug-generator) | — | ✅ |
| 3.4 | Vendoring `ApexionGame.SourceGen.Common` | ~2.5k | ✅ 20 file ([I-22](07-DECISIONS.md#i-22--vendoring-copy-nguyên-chưa-rút-gọn-bỏ-2-file-không-dùng)) |
| 3.5 | `ApexionGame.SourceGen.Helpers` (nhận `StatTypeTable.gen.cs` từ task 1.2) | ~250 | ✅ |
| 3.6 | `StatDataGenerator` (dễ nhất, làm trước để chạy thông toàn bộ pipeline) | ~540 | ✅ **đã chạy thật, không chỉ compile** |
| 3.7 | `StatSystemGenerator` | ~4.8k | ✅ sinh 3757 dòng, compile sạch |
| 3.8 | `StatCollectionGenerator` | ~4.4k | ✅ sinh 2753 dòng, compile sạch |
| 3.9 | 3 analyzer + 16 diagnostic (9 port + 4 mới + 3 UNKNOWN) — [§4.8](04-CODEGEN.md#48-analyzer--diagnostic-id) | ~520 | ✅ 13 rule + 3 UNKNOWN, đã chạy thật |
| 3.10 | `ApexionGame.SourceGen.Tests` + `UnityDllPaths.targets` — [§4.5.3](04-CODEGEN.md#453-tests--nạp-dll-unity-vào-roslyn-compilation) | ~1.3k | ✅ **17/17 pass** |
| 3.11 | Commit `.meta` (labels `RoslynAnalyzer` / `RunOnlyOnAssembliesWithReference` / `SourceGenerator`) cho 4 DLL | — | ✅ đã deploy |

### Cách port 3.7 — dùng compiler làm danh sách việc

`StatSystemSpec+WriteCode.cs` là 4058 dòng, gần như toàn bộ là chuỗi emit **không đổi** giữa hai
bản. Thay vì chép tay: copy nguyên → thay thế có kiểm soát → **xoá khối hằng số ECS**, để compiler
tự liệt kê mọi chỗ còn phụ thuộc ECS. Kết quả: 19 điểm, sửa hết trong một lượt.

⚠ Dùng `-creplace` (phân biệt hoa thường), **không** dùng `-replace`: mặc định của PowerShell là
case-insensitive, lần đầu tôi đã để `\bENTITY\b` nuốt luôn `entity` viết thường nằm trong chuỗi emit.

| Việc | Ghi chú |
|---|---|
| `Accessor(ref SystemState)` / `ReadOnly(...)` / `Update()` | → ctor nhận `Store`; `Update` **xoá hẳn** ([D-101](07-DECISIONS.md#d-101--stataccessor-giữ-1-statstore-thay-4-lookup)) |
| `IBufferElementData` khỏi base list của `Stat`/`StatObserver`/`StatModifier` | [§1.5](01-OVERVIEW.md#15-bảng-mapping-ecs--non-ecs) |
| `GetStatComponentTypeSet` + 3× `AddStatComponents` + `BakeStatComponents` | thay bằng **`CreateStatOwner`** — điểm vào duy nhất gieo *None stat* ([I-20](07-DECISIONS.md#i-20--statbuilder-thay-statbaker-ba-thứ-mất-một-thứ-thêm)) |
| wrapper `Baker` → `Builder`, `IBaker`/`Entity` → `Owner` | |
| 4 hàm truy vấn phụ thuộc đổi tên; `entity`→`owner` | [I-14](07-DECISIONS.md#i-14--_tmpsameentityupdatedstats-đổi-tên-thành-_tmpsameownerupdatedstats) |
| bỏ nhánh `references.latiosCore` | [D-104](07-DECISIONS.md#d-104--bỏ-nhánh-if-latios_framework) |
| bỏ `ref` trước mọi tham số `StatBuffer` emit | [I-13](07-DECISIONS.md#i-13--statbuffert-truyền-theo-giá-trị-không-ref) |
| **thêm `RemapObservedStats` + `partial void RemapObservedStatsInternal`** | [DEC-004](07-DECISIONS.md#dec-004--đã-chốt-a--thêm-remapobservedstats-vào-istatmodifier-ngay-) — bản gốc không có |
| `ReadOnlySpan`/`Span<bool>` → `S.ReadOnlySpan`/`S.Span` | bản gốc emit thiếu alias, chỉ compile khi user tình cờ có `using System;` |

**Nghiệm thu:** generator sinh **3757 dòng** cho `[StatSystem(StatDataSize.Size8)]`, compilation sau
khi sinh còn 4 lỗi `CS0433` — **artifact của harness**, không phải lỗi port: harness net10 nạp cả
`System.Private.CoreLib` lẫn `EncosyTower.Core`, hai bên cùng định nghĩa `StackTraceHiddenAttribute`.
Bằng chứng: assembly runtime của ta cũng dùng `[StackTraceHidden]` và build sạch dưới netstandard2.1.

### 3.8 — `StatCollectionGenerator`

Cùng phương pháp. [§4.7.5](04-CODEGEN.md#475-ba-generator--khác-biệt-riêng) dự đoán đúng ba thay
đổi, và thực tế đúng vậy:

| | |
|---|---|
| bỏ ràng buộc `IComponentData` | `where TComponentData : unmanaged` ([D-105](07-DECISIONS.md#d-105--statcollection-không-còn-yêu-cầu-icomponentdata)) |
| `Baker`/`Baker<T>` → `Builder`/`Builder<T>` | |
| `Bake(IBaker, Entity)` → **`Build(ref store, out owner)`**; `CreateComponentData<T>()` + `AddComponentToEntity()` → **`ToStats()`** | phần `UnsafeUtility.As` còn giữ dưới tên `As<TComponentData>()` |

Bỏ thêm 3 overload `SetComponentToEntity` (EntityManager / ECB / ECB.ParallelWriter).

**Nghiệm thu chung cho cả 3 generator** — chạy trên `[StatSystem]` + `[StatCollection]` 3 stat:

| | |
|---|---|
| File sinh ra | 5 (`StatSystem` 3757 dòng, `Stats` 2753, 3× `StatData` 121) — **5774 dòng** |
| Lỗi compile của output | **8**, toàn bộ là `CS0433 StackTraceHiddenAttribute` — artifact harness, không phải lỗi port |
| Dấu vết ECS còn lại | `IComponentData` 0 · `UECS` 0 · `DynamicBuffer` 0 · `IBaker` 0 · `EntityManager` 0 · `AsNativeArray` 0 · `BufferLookup` 32/32 đều là `StatBufferLookup` |
| `RemapObservedStats` (DEC-004) | có, 3 chỗ |
| API khớp [§3.5.3](03-API-SURFACE.md#353-tạo-một-chủ-thể-thay-baking) | `Stats.Builder.Build(ref store)` … `.ToStats()` ✓ · `Stats.Hp.Params.Create(hp)` ✓ |

### 3.9 — analyzer

13 rule (`SYSTEM` 2, `COLLECTION` 5, `DATA` 6) + 3 `*_UNKNOWN_0001` trong generator = **16 ID**,
đúng bảng [§4.8](04-CODEGEN.md#48-analyzer--diagnostic-id).

Bốn rule mới đều **đã nối dây và chạy thật**, không chỉ khai báo:

| ID | Kiểm chứng |
|---|---|
| `AGS_STAT_SYSTEM_0002` | dò `EncosyTower.Common.ByteBool` trong compilation — ByteBool đại diện cho cả `EncosyTower.Core` vì `Stat`/`ValuePair` sinh ra đều dùng nó |
| `AGS_STAT_COLLECTION_0005` | bắn trên struct lồng thiếu `[StatData]`; bỏ qua 12 tên type mà generator tự khai (`TypeId`, `Indices`, `StatHandles`, …) |
| `AGS_STAT_DATA_0005` | đi ngược `StatData → [StatCollection] → [StatSystem]` lấy `MaxDataSize`; `Float4` trong hệ `Size8` báo *"needs 32 bytes but the stat system allows 8"* |
| `AGS_STAT_DATA_0006` | guard [DEC-004](07-DECISIONS.md#dec-004--đã-chốt-a--thêm-remapobservedstats-vào-istatmodifier-ngay-) — `StatModifier` có field `StatHandle` mà `RemapObservedStatsInternal` chỉ là partial-declaration không thân |

Nghiệm thu: chạy analyzer trên một file cố tình sai, 6 rule bắn đúng vị trí và đúng thông điệp
(0001–0005 của DATA, 0005 của COLLECTION).

### 3.10 — Tests

```
$env:UNITY_OS_INSTALL_ROOT = "C:\Program Files\Unity\Hub\Editor\6000.3.20f1"
dotnet test Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Tests/ApexionGame.SourceGen.Tests.csproj
```

**17/17 pass, 0 skipped** — nhưng chỉ đúng **sau khi sửa hai lỗi ở phase 4**; xem
[bên dưới](#hai-lỗi-làm-14-test-tích-hợp-chưa-từng-chạy). Ba tầng:

| Tầng | Test | Khẳng định |
|---|---|---|
| Smoke | 3 | ba generator không ném trên input rỗng |
| Integration | 6 | code sinh ra **compile 0 lỗi** với `ApexionGame.Entities.Stats.dll` thật; 5 file; **không còn token ECS nào**; có `RemapObservedStats` (DEC-004), `CreateStatOwner` (D-102), `Build`/`ToStats` |
| Analyzer | 8 | mỗi rule một file vi phạm đúng nó, kèm một ca **âm tính** (`SingleValue = true` thì `DATA_0005` phải im) |

Hai điểm khác bản gốc:

- `UnityDllPaths.targets` **không** `<Error>` khi thiếu đường dẫn Unity mà chỉ `<Warning>`, rồi sinh
  danh sách rỗng; test tích hợp báo `Inconclusive` với thông điệp hướng dẫn. Bản gốc fail cả build,
  kéo sập luôn tầng smoke vốn không cần Unity — trái với thiết kế hai tầng của
  [§4.5.3](04-CODEGEN.md#453-tests--nạp-dll-unity-vào-roslyn-compilation).
- Không cần gói NuGet `Unity3D`: `UnityModulesPath`/`UnityScriptAssembliesPath` suy trực tiếp từ
  `UNITY_OS_INSTALL_ROOT` và `UnityProjectPath`, nên test project restore được không cần gói đó.
- `Generators`/`Analyzers` thêm `<InternalsVisibleTo Include="ApexionGame.SourceGen.Tests" />` vì
  các type đó là `internal`.

#### Hai lỗi làm 14 test tích hợp chưa từng chạy

Ghi lại vì bài học đắt hơn lỗi: **`dotnet test` in `Passed!` cả khi phần lớn test bị skip.** Con
số "17/17 pass" ghi ở phase 3 lấy từ dòng tổng đó và **sai** — thực tế là 3 pass + 14
`Inconclusive`. Phát hiện ở phase 4 khi chạy lại và để ý cột `Skipped`.

**Lỗi 1 — `UnityDllPaths.g.cs` chưa bao giờ được biên dịch.** Target sinh nó ở `BeforeBuild`,
nhưng glob `**/*.cs` mặc định của SDK đã được **đánh giá xong trước khi bất kỳ target nào chạy**,
và `DeleteUnityDllPaths` xoá file sau mỗi build → glob không bao giờ thấy. Các `partial void`
không có thân → `All` rỗng → `IsResolved` false → mọi test tích hợp `Inconclusive`. Sửa: target
tự `<Compile Include>` file nó vừa ghi (`@(Compile)` được đọc lúc `CoreCompile` chạy, nên kịp).

**Lỗi 2 — lộ ra ngay sau khi sửa lỗi 1.** Batch metadata trên item list **rỗng** vẫn sinh **một**
dòng, với metadata giãn thành chuỗi rỗng. Kết quả là `new[] { @"", }` — `IsResolved` thành true
với một đường dẫn rỗng, và cả 17 test chết trong Roslyn ở `MetadataReference.CreateFromFile("")`.
Sửa: lặp lại `Condition` trên `ItemGroup` đó, đổi `new[]` thành `new string[]` (mảng rỗng
implicitly-typed là CS0826), và cho `IsResolved` kiểm tra `File.Exists` thật.

Đã xác nhận **cả hai chế độ**:

| | Kết quả |
|---|---|
| có `UNITY_OS_INSTALL_ROOT` | 17 pass, **0 skip** |
| không có | 3 pass, 14 skip, build 0 lỗi — đúng thiết kế hai tầng |

Đặt `UNITY_OS_INSTALL_ROOT` trỏ tới thư mục phiên bản, ví dụ
`C:\Program Files\Unity\Hub\Editor\6000.3.20f1`.

> Khi đặt biến này, build `Samples/*` in **MSB3106**: `$(UnityEditorPath)` do gói NuGet `Unity3D`
> dựng bị lặp đoạn version (`…/6000.3.20f1/6000.3.20f1/…`). Vô hại — sample không dùng API nào
> của `UnityEditor`, và cả hai project vẫn compile 0 lỗi khi tham chiếu đó không resolve được.
> Không sửa vì đụng vào cấu hình đang chạy tốt ở đường còn lại chỉ để dẹp một cảnh báo.

`GeneratorTestHelper` bỏ qua `CS0433 StackTraceHiddenAttribute` — test host nạp BCL hiện đại vốn
có sẵn type mà `EncosyTower.Core` polyfill cho netstandard2.1; Unity không bao giờ thấy cả hai.

### 3.11 — deploy

```
$env:UNITY_OS_INSTALL_ROOT = "C:\Program Files\Unity\Hub\Editor\6000.3.20f1"
dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release
```

Xanh. `CopyBuildArtifacts` thả 4 DLL vào `Assets/…/SourceGenerators/`; `.meta` được ghi **ngay
trong cùng một lệnh** để Unity (đang mở) không kịp import DLL trần rồi tự sinh `.meta` mặc định —
`isExplicitlyReferenced: 0` sẽ auto-reference chúng vào mọi assembly.

| DLL | | |
|---|---|---|
| `Common` 83 KB · `Helpers` 18 KB · `Generators` 279 KB · `Analyzers` 22 KB | mỗi cái một `.meta` GUID riêng | labels `RunOnlyOnAssembliesWithReference` / `RoslynAnalyzer` / `SourceGenerator`, `isExplicitlyReferenced: 1`, mọi platform `enabled: 0` |

Sau khi deploy, `ApexionGame.Entities.Stats` và assembly test vẫn build **0 lỗi** — nghĩa là
`[assembly: SkipSourceGeneratorsForAssembly]` chặn generator chạy lên chính assembly runtime,
đúng như [§4.4](04-CODEGEN.md#44-deploy-vào-unity) thiết kế.

### Điều mà `-c Release` mới lộ ra: `Samples` thiếu `UnityEngine.CoreModule`

Lần đầu `Samples.Entities.Stats` được build thật, nó fail: `UnityEngine.Debug` và
`Unity.Collections.NativeArray<>` không tìm thấy. Nguyên nhân **không** phải generator sai — mà
`$(UnityModulesPath)` do gói NuGet `Unity3D` cung cấp bị rỗng, nên `UnityEngine.CoreModule.dll`
âm thầm không được reference. Cả hai type đó đều nằm trong DLL này.

Sửa giống cách đã làm cho Tests: suy đường dẫn trực tiếp từ `UNITY_OS_INSTALL_ROOT`, cộng một
target `VerifyUnityPaths` báo lỗi rõ ràng thay vì để triệu chứng hiện ra dưới dạng "generator sinh
code sai". Sau đó `Samples` build **0 lỗi**. Target `CopyBuildArtifacts` sẽ thả DLL vào
`Assets/…/SourceGenerators/`; làm điều đó **trước** khi có `.meta` đúng label (task 3.11) sẽ khiến
Unity import chúng như plugin thường và auto-reference vào mọi assembly. Deploy và 3.11 phải đi
cùng nhau.

Thứ tự 3.1 → 3.3 trước khi viết generator là **có chủ đích**: có `Samples` + debug profile ngay
từ đầu thì viết `StatSystemSpec+WriteCode` (4.8k LOC emit code) mới khả thi; không thì mỗi lần
sửa phải quay lại Unity chờ reimport.

**Exit criteria:** `dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release`
xanh, `Samples.Entities.Stats` (khai báo `[StatSystem]` + `[StatCollection]` 5 stat)
**compile sạch ngoài Unity**, và scenario ở [§3.5](03-API-SURFACE.md#35-ví-dụ-end-to-end-dự-kiến)
chạy được trong Unity.

**Cách verify generator không cần `Samples`** (dùng khi thiếu `UNITY_OS_INSTALL_ROOT`): dựng một
`CSharpCompilation` trỏ tới `Library/ScriptAssemblies/ApexionGame.Entities.Stats.dll` + DLL Unity,
chạy `CSharpGeneratorDriver`, rồi đếm `GetDiagnostics()` của **compilation sau khi sinh**. Đây
chính là cách task 3.10 sẽ làm; đã dùng để nghiệm thu 3.6 và cho kết quả **0 lỗi**.
Giá trị phụ: nó kiểm tra chéo cả phase 1 — `StatVariant.gen.cs`, `StatVariantType`, `IStatData`
phải khớp thật thì code sinh mới compile.

**Điều kiện môi trường:** biến môi trường `UNITY_OS_INSTALL_ROOT` + đã mở Unity một lần để
`Library/ScriptAssemblies/ApexionGame.Entities.Stats.dll` tồn tại.

## Chạy test Unity không cần mở Editor

Chặn suốt từ phase 2: 90 test viết ra mà không ai xác nhận, vì chạy chúng đòi mở Test Runner
bằng tay. **Unity CLI giải quyết được.**

```powershell
# Cài một lần. Kênh stable chưa có manifest (CLI còn experimental) nên phải chỉ định beta.
Invoke-WebRequest -UseBasicParsing `
  -Uri "https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1" -OutFile install.ps1
$env:UNITY_CLI_CHANNEL = "beta"; ./install.ps1          # -> %LOCALAPPDATA%\Unity\bin\unity.exe

# Chạy toàn bộ EditMode
unity test . --mode EditMode --output results.xml --timeout 2400 --non-interactive

# Chỉ benchmark
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Tests.StatBenchmarkTests" `
             --output bench.xml
```

Ba điều vấp phải, ghi lại cho lần sau:

- **Unity phải đóng.** Batch mode không mở được project đang bị Editor khoá. Không có cách nào
  lách: `unity command` chạy lệnh trong Editor đang mở, nhưng cần package *Pipeline* mà project
  này không có.
- **`[Explicit]` bị loại kể cả khi filter đích danh.** Đó là lý do benchmark đã bỏ `[Explicit]`.
- **Output của CLI lẫn nhiều lỗi telemetry** (`[Experiment] Fetch failed`, `connect ECONNREFUSED
  127.0.0.1:443`). Vô hại và không liên quan tới kết quả test — đọc file XML, đừng đọc stdout.

### Kết quả (2026-08-04, Unity 6000.3.20f1)

| | |
|---|---|
| Toàn project EditMode | **928 pass, 0 fail, 0 skip** |
| `ApexionGame.Entities.Stats.Tests` | **95 pass, 0 fail** (90 chức năng + 5 benchmark) |

Lần đầu tiên bộ test này được xác nhận. Trong đó có
`S05_UnequalDepthDiamond_JoinIsRepairedNotLeftStale` — test hồi quy chốt bản vá DEC-005.

Đợt chạy này cũng gián tiếp xác nhận `ApexionGame.Entities.Stats.Samples` và
`.Samples.Editor` compile được: một lỗi biên dịch ở bất kỳ assembly nào cũng làm hỏng cả lượt.

## Phase 4 — Hoàn thiện

| # | Việc | Trạng thái |
|---|---|---|
| 4.1 | Persistence: `TryCopyOwnerTo` / `TryRestoreOwner` + pass fixup observer & `RemapObservedStats` (contract đã có từ 1.9) | ✅ + 6 test |
| 4.2 | `ApexionGame.Entities.Stats.Authoring` — `ScriptableObject` định nghĩa stat | ✅ |
| 4.3 | Burst verification: `[BurstCompile]` mọi job sinh ra, bật *Burst Inspector* kiểm tra không có managed call | ✅ **đã xác nhận trong Burst Inspector** |
| 4.4 | Benchmark: 10k owner × 8 stat × 4 modifier | ✅ 5 benchmark, **đã chạy, có số** |
| 4.5 | Dedupe cho `UpdateStatRef` (D-01/DEC-005) | ✅ chốt **ngược** — bỏ dedupe ở `TryUpdateStat` |
| 4.6 | Batch API nhận allocator (D-02) | ✅ |
| 4.7 | `README.md` public + `CHANGELOG.md` | ✅ |
| 4.8 | Sample chạy được: `ApexionGame.Entities.Stats.Samples` | ✅ |

### 4.8 — sample

`Assets/ApexionGame/ApexionGame.Entities.Stats.Samples/` — assembly riêng, ba file:

| File | Trả lời |
|---|---|
| `RpgStats.cs` | khai báo stat: hai `[StatCollection]` (`RpgStats`, `AuraStats`) trên cùng một `[StatSystem]` |
| `RpgStatSystem.cs` | modifier làm gì — điền các hook `partial void ...Internal` mà generator để lại |
| `RpgStatsSample.cs` | `MonoBehaviour` chạy thật, log ra console |

Sample cố tình dựng đúng **tình huống mà cả hệ thống tồn tại vì nó**: `Attack` của hero mang
`AddFrom(aura.attackBonus)` — modifier **chéo owner**. Đổi `AttackBonus` từ 5 lên 20 mà không
chạm gì tới hero, `Attack` tự đi từ `(10+5)*1.2 = 18` lên `(10+20)*1.2 = 36`.

Bỏ phần chéo owner đó đi thì cả hệ này không hơn gì một `Dictionary<string, float>`.

**Bàn thí nghiệm bấm nút:** `StatsPlayground.cs` + `Editor/StatsPlaygroundEditor.cs` — mười nút
trong Inspector, mỗi nút một hành vi đáng xem. Mỗi lần bấm **dựng lại world sạch** rồi chạy đúng
một case, nên bấm thứ tự nào cũng được. `[ExecuteAlways]` nên chạy được ngoài play mode: store là
native memory thuần, không cần player loop.

| Nút | Cho thấy |
|---|---|
| 1 | set base value, không modifier |
| 2 | `(base + add) * multiply` — hai modifier trên một stat |
| 3 | modifier chéo owner: đổi aura, hero tự theo |
| 4 | chuỗi 4 tầng, ghi vào đầu chuỗi |
| 5 | **kim cương lệch tầng (DEC-005)** — thứ mà visited-set làm sai |
| 6 | gỡ modifier cũng lan truyền |
| 7 | vòng lặp bị từ chối: tự observe, 2 tầng, 3 tầng |
| 8 | destroy owner đang bị observe → đóng góp 0, không ném |
| 9 | **save / load / remap (DEC-004)** — in cả giá trị *trước* khi remap để thấy rõ cái mất |
| 10 | dump `StatChangeEvent` của một lần ghi |

Nút dùng `System.Action` trực tiếp, **không** `MonoBehaviour.Invoke(name)`: `Invoke` xếp lịch lên
player loop, mà player loop không chạy ở edit mode → nút sẽ im lặng không làm gì.

Scene `stats-playground.unity` viết tay (`.meta` GUID cố định để scene tham chiếu được script
trước khi Unity import). Mở scene, chọn `stats-playground`, bấm nút.

#### Cửa sổ UI Toolkit — `ApexionGame > Stats > Stats Playground`

Console chỉ in text; muốn *thấy* đồ thị làm gì thì phải nhìn số. Cửa sổ này bày
`base → current` cho từng stat, kèm số modifier, và tô đậm cột `current` khi nó lệch khỏi
`base` — khoảng lệch đó chính là thứ modifier stack tạo ra.

Model được tách khỏi `MonoBehaviour` thành `StatsPlaygroundWorld` (thuần C#, `IDisposable`),
nên scene và cửa sổ **dùng chung một model** thay vì chép logic hai lần.

Bám quy ước UI Toolkit của EncosyTower — tham khảo `EncosyTower.Editor/VisualDebugging/Commands`
và `EncosyTower.Core.Extended/VisualDebugging/Commands`:

| Quy ước | Áp dụng |
|---|---|
| Dựng cây bằng C#, **không** `.uxml` | 3 view class, tất cả dựng con trong constructor |
| `*View.cs` = element + `*ViewController` + `static *API` | `StatEntryView.cs`, `StatOwnerView.cs`, `StatsPlaygroundView.cs` |
| `public static readonly string UssClassName` + nội suy BEM | `$"{UssClassName}__header-label"` |
| `.tss` chỉ `@import`; `.uss` chỉ `var(--…)`; mã màu chỉ nằm trong `_Dark`/`_Light` | 4 file trong `Editor/StyleSheets/` |
| Đường dẫn stylesheet là `const` ghép từ `nameof(TheType)` | `FILE_NAME = nameof(StatsPlaygroundWindow)` |
| `internal sealed class *Window : EditorWindow`, `#if UNITY_EDITOR`, `CreateGUI()`, dọn ở `OnDestroy()` | `StatsPlaygroundWindow.cs` |
| `view.userData = this` để controller tìm lại được | cả 3 controller |

Lệch một chỗ có chủ ý: **không object pool** view. EncosyTower pool vì số command không biết
trước; ở đây một owner có dăm ba stat, pool chỉ thêm nhiễu. Ghi rõ trong comment.

Verify: nửa runtime (`Views/`) compile qua `Samples.RpgStats`; nửa editor compile qua một csproj
tạm tham chiếu assembly Unity thật. Cái bẫy mất thời gian nhất: phải lấy DLL ở
`Editor/Data/Managed/**UnityEngine/**` — đó mới là bộ facade Unity tự compile với. Bộ ở
`Editor/Data/Managed/` là assembly nguyên khối cũ; trộn hai bộ thì mọi kiểu dùng chung đều
CS0433.

**Kèm project verify:** `Plugins/SourceGenerator.ApexionGame/Samples/Samples.RpgStats/` compile
chính ba file `.cs` đó qua generator thật, ngoài Unity. Đây không phải sample thứ hai mà là
**chốt chặn hồi quy**: sửa runtime hoặc sửa generator mà làm hỏng cách dùng đã ghi trong tài
liệu thì `dotnet build` báo trong vài giây, thay vì phát hiện lúc domain reload.

Đã chạy: `Build succeeded`, 0 lỗi 0 warning. Lần compile đầu ra **6 lỗi** — tôi đoán sai
`initialCapacity`, `ToStatHandles`, `StatHandles.owner`, `Hp.Value`. Đó chính là lý do phải
compile sample chứ không viết từ trí nhớ.

### 4.5 — D-01: kết luận ngược với kế hoạch

Kế hoạch cũ: *bật* dedupe cho `UpdateStatRef` cho khớp `TryUpdateStat`. Đo xong thì hoá ra
phải làm ngược lại — **bỏ** dedupe khỏi `TryUpdateStat`, vì visited-set là một **lỗi đúng
sai**, không phải tối ưu.

Kim cương lệch tầng `A→B→D` (1 chặng) và `A→C→E→D` (2 chặng), đặt `A = 5`: giá trị đúng của
D là 10; có visited-set cho ra **6 và không bao giờ sửa**. Worklist chạm D trước khi E kịp
cập nhật, lượt duyệt thứ hai của D chính là lượt sửa — và visited-set chặn đúng lượt đó.

Bảng số đầy đủ (3 case, k = 1..14) ở [DEC-005](07-DECISIONS.md#dec-005). Đo bằng cách dựng
lại nguyên thuật toán thành chương trình managed chạy ngoài Unity, không phải bằng suy luận.

Đánh đổi: xấu nhất O(k²) thay vì O(k) trên đồ thị dày kim cương. Muốn vừa đúng vừa tuyến
tính thì cần lan truyền theo thứ tự topo — thiết kế khác, chưa làm.

Test hồi quy: `S05_UnequalDepthDiamond_JoinIsRepairedNotLeftStale`, phủ cả hai lối lan truyền.

### 4.3 — Burst

Phần sinh code **đã đúng ngay từ phase 3**, không phải sửa gì: ba job runtime
(`DeferredUpdateStatListJob/QueueJob/StreamJob`) là **generic mở**, mà Burst không compile
generic mở. Generator vì vậy sinh ra ba struct **không generic** bọc ngoài, mỗi cái mang
`[UB.BurstCompile]` và giữ job đã đóng kiểu trong `_jobData`
(`StatSystemSpec+WriteCode.cs:1276-1281`). Đây là lý do không cần `[RegisterGenericJobType]`.

Ba job trong `StatJobs.cs` **cố ý không** mang `[BurstCompile]` — đánh dấu trên generic mở
không có tác dụng gì.

#### Kết quả Burst Inspector (2026-08-04)

Mở *Jobs → Burst → Open Inspector* trên project có `RpgStatSystem` của sample:

- Dưới `ApexionGame.Entities.Stats.Samples.RpgStatSystem` hiện đủ **ba** job
  `DeferredUpdateStatListJob` / `QueueJob` / `StreamJob`, đều `(IJob)`, **không xám**, và panel
  Assembly ra mã máy thật (kèm inline site từ `UnsafeList.cs`, `AllocatorManager.cs`,
  `SharedStatic.cs`). Burst thất bại thì chỗ đó hiện lỗi chứ không hiện assembly.
- Ba dòng `ApexionGame.Entities.Stats.DeferredUpdateStat*Job\`6[...]` — job **generic mở** trong
  `StatJobs.cs` — bị **xám**, tức Burst bỏ qua. Đúng như thiết kế, và là bằng chứng trực tiếp
  cho việc generator phải sinh struct không generic bọc ngoài.

**Cảnh báo cũ về `ThrowHelper` là thừa.** Tôi từng lo nội suy chuỗi (`$"..."` → `string.Format`)
làm Burst từ chối trong Editor. Không xảy ra. Các `[Conditional]` đó bám vào `UNITY_EDITOR` /
`APEXION_*` chứ không phải `ENABLE_UNITY_COLLECTIONS_CHECKS`, nên nút *Safety Check Off* không
gỡ chúng — chúng vẫn nằm trong IL và Burst vẫn compile được.

`DISABLE_APEXION_CHECKS` (`Debugging/ValidationDefines.cs`) vẫn còn làm cửa thoát nếu sau này
thêm check nặng hơn, nhưng **hiện không cần dùng**.

### 4.4 — benchmark

`StatBenchmarkTests.cs`, 5 phép đo trên 10k owner × 8 stat × 4 modifier (mỗi owner là một
chuỗi 8 tầng, stat sau observe stat trước):

| | Đo gì |
|---|---|
| B01 | dựng world |
| B02 | `TrySetStatBaseValue` + lan truyền, mỗi owner một lần |
| B03 | `TryUpdateAllStats` mỗi owner |
| B04 | đọc toàn bộ 80k stat |
| B05 | destroy toàn bộ rồi dựng lại — kiểm chứng free-list của D-100 |

#### Số đo thật (đã chạy)

`unity test . --mode EditMode --filter "…StatBenchmarkTests"`, Unity 6000.3.20f1, Editor/Mono,
safety check bật, Burst tắt. Cả 5 chạy hết trong **1.86s**.

| | Tổng | Mỗi thao tác |
|---|---|---|
| B01 dựng world (10k owner, 80k stat, 320k modifier) | 291.4 ms | 0.911 µs/modifier |
| B02 `TrySetStatBaseValue` + lan truyền, mỗi owner | 28.2 ms | **2.824 µs/owner** |
| B03 `TryUpdateAllStats` mỗi owner | 31.7 ms | 3.170 µs/owner |
| B04 đọc toàn bộ 80k stat | 6.0 ms | 0.075 µs/stat |
| B05 destroy 10k owner | 1.7 ms | **0.166 µs/owner** |
| B05 dựng lại (warm) | 266.1 ms | 0.832 µs/modifier |

Hai điều số liệu này khẳng định:

- **Free-list của D-100 đúng như thiết kế.** Destroy chỉ tốn 0.166 µs/owner — đó là `Clear()` cộng
  một lần đẩy vào free-list, không phải giải phóng bộ nhớ. Dựng lại sau đó rẻ hơn lần đầu
  (266.1 ms so với 291.4 ms) vì cấp phát được tái dùng.
- **Ghi một stat là O(owner), không phải O(world).** 2.824 µs cho một lần ghi lan truyền qua
  chuỗi 8 tầng, không phụ thuộc việc world có 10k owner khác.

Ban đầu đánh `[Explicit]` cho khỏi chạy chung suite. **Đã bỏ**: test runner của Unity loại test
`[Explicit]` kể cả khi gọi đích danh trong `--filter`, nên số đo nằm ngoài tầm với của
`unity test`. Cả class chạy chưa tới 2 giây — không đáng để đánh đổi.

⚠ **Chỉ dùng để so tương đối**: Mono, safety check bật, Burst tắt. Giá trị tuyệt đối kém build
player nhiều lần. Cái nó bắt được là một thao tác lỡ thành O(world) thay vì O(owner).

So sánh với `DynamicBuffer` như tiêu đề task ban đầu **không làm được**: project không cài
`com.unity.entities`, cùng lý do đã ghi ở phần golden test.

### 4.2 — authoring

Bài toán thật, không phải tiện ích: **`StatVariant` không author được trong Inspector.** Nó vừa
mang `[Serializable]` vừa có **19 field cùng ở `[FieldOffset(0)]`**. Unity bỏ qua explicit layout,
nên nó serialize cả 19 field thành 19 mục riêng; khi load, tất cả ghi vào cùng một vùng nhớ và giá
trị cuối cùng thắng. Không có lớp authoring thì không đặt được giá trị stat từ asset.

`SerializableStatVariant` lưu giá trị trong ba field thường, chọn sao cho **mọi kiểu của profile
`gameplay` round-trip không mất mát**:

| Field | Phủ |
|---|---|
| `double _scalar` | `bool`, mọi kiểu nguyên 8/16/32-bit, `half`, `float`, `double` |
| `long _integer` | `long` và `ulong` — hai kiểu `double` không giữ chính xác được |
| `float4 _vector` | `half2/3/4`, `float2/3/4` |

`StatDefinitionAsset` cố ý **không** đụng tới type sinh ra. Collection được sinh theo từng
`[StatCollection]`, nên một asset strongly-typed cũng phải được sinh theo; asset này chỉ mang giá
trị, còn ánh xạ `Id → handle` để project tự làm trong một factory nhỏ mỗi collection.

**Không** làm: editor drawer, validation theo `StatVariantType`, asset sinh tự động. Chưa có nhu
cầu cụ thể nên chưa xây.

### 4.1 — persistence

| Nơi | Vì sao ở đó |
|---|---|
| `` StatStore`3+Persistence.cs `` — `TryCopyOwnerTo` / `TryRestoreOwner` | thuần dữ liệu, chạy được với tham số kiểu `unmanaged` không ràng buộc |
| `` StatAccessor`6+Persistence.cs `` — `TryRemapOwner` / `TryRemapOwners` | cần `IStatObserver.ObserverHandle` và `IStatModifier.RemapObservedStats`, mà `StatStore` **không** ràng buộc hai interface đó nên tự làm được |

Đây là chi tiết [§2.9](02-ARCHITECTURE.md#29-persistence) chưa nói rõ: pass remap **không thể**
nằm trong store.

**Thứ tự bắt buộc:** restore *toàn bộ* owner trước, rồi mới remap. Remap ngay sau mỗi owner sẽ
khiến tham chiếu tới owner chưa restore âm thầm thành `StatHandle.Null`.

6 test, trong đó hai cái đáng chú ý:

- `RestoreTwoOwners_RemapsCrossOwnerModifier` — destroy cả hai owner, **đốt thêm một slot** để
  owner restore không rơi trúng index cũ do may mắn, restore, remap, rồi set giá trị nguồn và
  kiểm tra đích đi theo. Đây là bài toán mà DEC-004 sinh ra để giải.
- `Remap_WithoutRemapPass_LosesTheLink` — bỏ pass remap thì modifier trỏ vào owner đã chết, đóng
  góp 0 và **không báo lỗi**. Test này khoá lại chính xác chế độ hỏng im lặng đó.

## Phase 5 — Tooling ✅

| # | Việc | Trạng thái |
|---|---|---|
| 5.1 | `ApexionGame.Entities.Stats.Editor`: xem giá trị + modifier của store đang chạy | ✅ |
| 5.2 | Stat graph debugger (vẽ đồ thị observer, bắt vòng lặp) | ✅ |
| 5.3 | Code refactor provider: sinh sẵn skeleton `[StatSystem]` / `[StatCollection]` | ✅ + 9 test |

### 5.1 — làm sao tìm được store đang chạy

Vấn đề thật, không phải chuyện UI: `StatStore` là **native memory do code của bạn sở hữu**.
Không có world, không singleton, không có gì để một cửa sổ đi duyệt. Trước khi vẽ được cái gì
thì phải giải bài toán "tìm store" đã.

Cách chọn: **đăng ký tự nguyện**.

```csharp
_debug = new StatStoreDebug<ValuePair, Stat, StatModifier, StatObserver>("battle", _store);
StatDebugRegistry.Register(_debug);   // và Unregister trước khi Dispose store
```

`StatStoreDebug<4>` xoá tham số kiểu, phơi ra `IStatStoreDebug` cho tooling. Nó **giữ một bản
copy của struct store** — mà struct đó chỉ là mấy con trỏ, nên tooling đọc thẳng vào bộ nhớ
người khác đang ghi: số hiện ra là **sống**, không phải snapshot lúc đăng ký.

Ba thứ được giữ đúng:

- **Không đăng ký thì không tốn gì.** Runtime không đọc registry ở bất kỳ đâu.
- **Adapter không sở hữu store**, không bao giờ Dispose nó.
- **Phải Unregister trước khi Dispose.** Đăng ký sống lâu hơn store là con trỏ treo — cửa sổ
  hiện "store disposed" nếu gặp, nhưng đúng cách vẫn là gỡ đăng ký trước.

Runtime thêm đúng **một** API: `StatStore.TryGetOwnerAt(slotIndex, out owner)`. Không có nó thì
không cách nào liệt kê owner mà mình chưa cầm handle. Gameplay không nên dùng — index slot không
phải identity, phải index **cộng** version mới là.

#### Bản đầu sai ở đâu, và sửa thế nào

Bản đầu poll 0.25s rồi **dựng lại toàn bộ cây UI** mỗi lần. Kết quả: không cuộn được (scroll bị
reset), chọn owner bị ghi đè giữa chừng, đồ thị nhấp nháy, cấp phát dictionary liên tục.

Bài học: **poll không có nghĩa là dựng lại.** Giá trị stat nằm trong native memory, không có gì
phát event, nên buộc phải poll — nhưng mỗi lượt phải so **hình dạng** dữ liệu (store nào, owner
nào, bao nhiêu stat, cạnh nào) với thứ đang hiện, và chỉ đụng vào cây khi hình dạng đổi. Con số
thì ghi thẳng vào label sẵn có, rẻ.

Ba tầng, tách rời hẳn:

| Đổi gì | Làm gì |
|---|---|
| danh sách store | `SetStores` — chỉ khi tên khác đi |
| danh sách owner | `SetOwners` — chỉ khi tập handle khác đi |
| tập cạnh / số stat | `SetTopology` + `SetStatCount` — dựng lại node và hàng |
| chỉ giá trị | `SetValues` — ghi vào label, **mỗi lượt poll** |

Kéo theo bốn lỗi phụ cũng phải sửa: dropdown nhúng số owner vào chuỗi chọn (count đổi là mất
selection); `SetSelectionWithoutNotify` chạy mỗi poll đè lên thao tác người dùng; sample đặt tên
store theo tên case nên store "biến mất" mỗi lần bấm nút; và ba thông báo rỗng hiện cùng lúc.

Thêm nút **Auto** để đóng băng khung nhìn, và `TwoPaneSplitView` để kéo chia bảng/đồ thị.

### 5.2 — đồ thị observer

Node xếp cột theo **longest-path depth**, không phải shortest. Cố ý: khoảng cách cột giữa hai
nhánh cùng đổ về một node **chính là** tình huống của [DEC-005](07-DECISIONS.md#dec-005), mà
shortest-path sẽ giấu mất nó đi.

Cạnh vẽ bằng `Painter2D` trong `generateVisualContent`, đọc `layout` đã resolve — nên phải
repaint lại sau mỗi lần geometry ổn định (`GeometryChangedEvent`).

Ba thứ đồ thị nói ra được mà bảng số không nói được:

| | |
|---|---|
| node viền sáng | modifier đã kéo `current` lệch khỏi `base` |
| node mờ, viền xám | **endpoint ở owner khác** — không vẽ thì cạnh chéo owner sẽ cụt, và đồ thị nói dối về cái gì nuôi cái gì |
| node đỏ + băng cảnh báo | **có chu trình** |

Cạnh có **đầu mũi tên**. Thiếu nó thì đường cong chỉ nói "hai cái này liên quan", trong khi
*chiều* mới là toàn bộ ý nghĩa của đồ thị observer.

**Chưa làm:** click một hàng trong bảng chưa highlight node tương ứng trong đồ thị. Đáng có,
nhưng chưa làm.

Chu trình đáng lẽ **không bao giờ xuất hiện**: `TryAddStatModifier` từ chối modifier khép vòng.
Nên đây không phải tính năng hiển thị mà là một **assertion sống** — nó sáng lên là kiểm tra
vòng lặp có lỗ, và đó là bug thật.

Phát hiện bằng cách bóc dần node không có cạnh vào (Kahn); cái gì còn sót lại là nằm trên chu
trình hoặc bị chu trình nuôi.

**Không** làm: ghi lại các modifier *đã bị từ chối*. Muốn thế phải cắm hook vào
`TryAddStatModifier` — trả giá ở hot path cho một tính năng debug. Hành vi từ chối đã được khoá
bằng golden test S06/S07 rồi.

### 5.3 — code refactor provider

Project mới `ApexionGame.SourceGen.CodeRefactors`, deploy cùng chỗ với generator, `.meta` mang
label `RunOnlyOnAssembliesWithReference` + `RoslynAnalyzer` (**không** có `SourceGenerator` — nó
không sinh code lúc biên dịch), khớp cách EncosyTower deploy `SourceGen.CodeRefactors.dll`.

| Refactoring | Trên | Làm gì |
|---|---|---|
| *Make this a stat system* | `partial class` chưa có attribute | thêm `[StatSystem(StatDataSize.Size8)]` |
| *Generate stat modifier skeleton* | `partial class` chưa có `StatModifier` | viết cả 7 hook `partial void ...Internal` |
| *Generate stat collection skeleton* | `partial struct` top-level chưa có attribute | thêm `[StatCollection(typeof(<system>), <seed>)]` + một `[StatData]` mẫu |

Cái đáng giá nhất là refactoring giữa. Hook nào tồn tại, và hook nào `readonly`, **không đoán
được** — phải đọc code sinh ra mới biết, và `readonly` đặt sai chỗ thì compiler lặng lẽ không
ghép partial. Chính tôi đã phải làm đúng việc đó khi viết sample.

Hai chi tiết nhỏ nhưng đúng:

- Seed type-id **né số đã dùng** trong file thay vì luôn ghi 1000 — hai collection trùng seed
  thì đụng nhau ở `UserData`.
- Type trong skeleton **fully-qualified**, nên refactoring không phải sửa `using`.

Kiểu sinh ra đều fully-qualified nên không cần đụng tới `using`. 9 test phủ: có/không offer,
nội dung sinh ra, né seed, và từ chối struct lồng (`[StatData]` là struct lồng — biến nó thành
collection sẽ lồng collection trong collection).

## Ước lượng tổng

| Layer | LOC sản phẩm | So bản gốc |
|---|---|---|
| L1 (primitives + generated tables) | ~2.5k | ~7.2k — nhờ profile `gameplay` (DEC-003) |
| L2 (storage seam) | ~800 | *(mới)* |
| L3 (algorithm) | ~4.4k | ~4.7k — bỏ overload ECS |
| Editor codegen bảng kiểu | ~750 | ~1.1k |
| Roslyn codegen (gồm vendored Common) | ~14.3k | ~11.6k — cộng vendored `SourceGen.Common` |
| **Tổng** | **~22.75k** | ~23.6k |

## Risk register

| # | Risk | Mức | Giảm thiểu |
|---|---|---|---|
| R1 | `StatBuffer<T>` dangling do quản pointer sai | **Cao** | Cấp phát header rời (§2.3.3); `AtomicSafetyHandle` dưới `ENABLE_UNITY_COLLECTIONS_CHECKS`; test 1.8 chuyên đề |
| R2 | Port sai một bước dịch `startIndex` → đồ thị hỏng âm thầm | **Cao** | Golden test 8/9/10; thêm `ValidateInvariants()` internal chỉ bật khi `APEXION_STATS_RUNTIME_CHECKS` |
| R3 | Codegen sinh code không Burst-compile được | Trung | Sample project + Burst Inspector từ phase 3, không để tới cuối |
| R4 | Vendoring `SourceGen.Common` bị lệch khi EncosyTower nâng Roslyn | Trung | Pin `Microsoft.CodeAnalysis.CSharp 4.3.1`; vendoring là *snapshot*, không sync |
| R5 | 3 allocation/owner gây áp lực khi spawn ồ ạt | Trung | Slot pooling (§2.3.3); benchmark 4.4; nếu cần thì thêm chế độ "shared arena cho owner tĩnh" |
| R6 | Bảng kiểu rút gọn (profile `gameplay`) sau này thiếu kiểu | Thấp | Đổi profile + chạy tool; `IsCompatible` báo lỗi rõ ràng lúc compile |
| R7 | Không có system scheduling → user tự quản `JobHandle` sai | Thấp | Doc "how to schedule" + sample; `AtomicSafetyHandle` sẽ bắt race |
| R8 | Update EncosyTower đổi output `[WrapType]`/`[EnumExtensions]` (DEC-002 = B) | Trung | Escape hatch: viết tay `StatIndex` ~70 LOC/type; `ZeroVariant`/`OneVariant` đã do generator của ta sinh — xem [§5.3.2](05-ASSEMBLY-LAYOUT.md#532-rủi-ro-còn-lại-của-b-phải-theo-dõi) |
| R9 | `RemapObservedStats` (DEC-004) khiến modifier viết cho bản EncosyTower không copy thẳng sang | Thấp | `AGS_STAT_DATA_0006` cảnh báo khi `StatModifier` có field `StatHandle` mà chưa implement `OnRemapObservedStats` |
