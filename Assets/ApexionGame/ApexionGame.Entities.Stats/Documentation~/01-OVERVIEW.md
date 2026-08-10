# 01 — Overview

## 1.1 Bản gốc có gì

`Packages/com.laicasaane.encosy-tower/EncosyTower.Entities.Stats` — 12.7k LOC runtime,
cộng ~11k LOC source generator ở `Plugins/SourceGenerator/*/Entities.Stats`.

**Runtime (chia theo vai trò):**

| Nhóm | File | LOC | Vai trò |
|---|---|---|---|
| Contracts | `IStat`, `IStatData`, `IStatModifier`, `IStatModifierStack`, `IStatObserver`, `IStatValuePair` | ~190 | Khung generic, user/codegen implement |
| Value | `StatVariant.cs` + `StatVariant.gen.cs` | 4.5k | Union 60+ kiểu (`float`, `half2`, `int4x4`, …) + toán tử |
| Enum tables | `StatVariantType.gen.cs`, `StatDataSize.gen.cs`, `StatVariantTypeExtensions.gen.cs`, `StatSingleExtensions.gen.cs` | 1.1k | Bảng kiểu + size, sinh bởi editor codegen |
| Handles | `StatHandle`, `StatIndex`, `StatModifierHandle` | 400 | Định danh stat / modifier |
| Ranges | `ModifierRange`, `ObserverRange` | 95 | `(startIndex, count)` — sub-list trong buffer dùng chung |
| Events | `StatChangeEvent`, `ModifierTriggerEvent` | 90 | Event value-type |
| **Algorithm** | `StatAPI.cs` | 1.4k | Hàm tĩnh: create/get/set stat, quan hệ observer, thu thập dependency |
| **Algorithm** | `StatAccessor.cs` | 2.2k | Mutation + lan truyền: `TryUpdateStat`, `TryAddStatModifier`, `TryRemoveStatModifier` |
| Read path | `StatAccessor+ReadOnly.cs`, `StatReader.cs` | 290 | Truy cập chỉ đọc |
| Scratch/Events | `StatWorldData.cs` | 190 | Event list + scratch buffer dùng chung, **không** thread-safe |
| Jobs | `StatJobs.cs` | 160 | `DeferredUpdateStat{List,Queue,Stream,UnsafeBlockList}Job` |
| Authoring | `StatBaker.cs` | 320 | Ghi stats trong baking pipeline (`IBaker`) |
| Guards | `ThrowHelper.cs` | 200 | Validation có `[Conditional]` |
| Annotations | `StatSystemAttribute`, `StatCollectionAttribute`, `StatDataAttribute` | 145 | Điểm vào của codegen |

**Codegen:**

| File | LOC | Sinh ra |
|---|---|---|
| `StatSystemGenerator` + `StatSystemSpec+WriteCode` | 4.8k | Từ `[StatSystem]`: `Stat`, `ValuePair`, `StatDataStore`, `StatModifier`, `Stack`, `StatObserver`, `API`, `Reader`, `Accessor`, `Accessor.ReadOnly`, `Baker`, `WorldData`, 3–4 job cụ thể |
| `StatCollectionGenerator` + `StatCollectionSpec+WriteCode` | 4.4k | Từ `[StatCollection]`: `Type` enum, `TypeId`, `Index`, `Indices`, `StatIndices`, `StatHandles`, `Options`, `Params.Create(...)`, `Baker`/`Baker<T>`, `Accessor`/`Accessor<T>`, `Reader<T>`, extensions |
| `StatDataGenerator` + `StatDataSpec+WriteCode` | 540 | Từ `[StatData]`: implement `IStatData` (BaseValue/CurrentValue/ValueType/IsValuePair) + ctor |
| 3 × `*DiagnosticAnalyzer` | 400 | Chẩn đoán lỗi khai báo |
| `StatGeneratorAPI` | 720 | Bảng kiểu: tên, size, namespace, enum map |

## 1.2 Mục tiêu bản port

1. **Chạy được ngoài ECS.** Không tham chiếu `Unity.Entities` ở bất kỳ đâu. Không cần
   World / SystemState / baking pipeline / EntityCommandBuffer.
2. **Giữ nguyên hiệu năng.** Vẫn `NativeCollection`, vẫn blittable, vẫn Burst-compilable,
   vẫn không alloc trên hot path, vẫn cho phép job hoá phần tính toán.
3. **Giữ nguyên năng lực.** Modifier stack, observer graph, chống vòng lặp, change event,
   modifier trigger event, deferred update qua job, `StatVariant` đa kiểu, codegen
   authoring-friendly (`Stats.Hp.Params.Create(100f)`).
4. **Source generator độc lập.** Solution riêng, DLL riêng, **không** `ProjectReference`
   sang `EncosyTower.SourceGen.*`, để bạn update package EncosyTower tự do.
5. **Ref thẳng `EncosyTower.Core` cho primitive, nhưng codegen thì tự chủ.**
   Dùng `ByteBool` / `Option<T>` / `HashValue` / `IIsValid` / collection extensions /
   `[WrapType]` / `[EnumExtensions]` của EncosyTower — khỏi viết lại ~450 LOC. Đổi lại,
   **ba generator của stats là của ta**, build riêng, không `ProjectReference` sang
   `EncosyTower.SourceGen.*`. → đã chốt tại [DEC-002](07-DECISIONS.md#dec-002--đã-chốt-b-hard-ref-encosytowercore-dùng-cả-generator-của-họ-).

## 1.3 Non-goals (phiên bản 1.0)

- **Không** làm lớp networking / prediction / rollback.
- **Không** làm ECS adapter hai chiều (nếu sau này cần ECS thì dùng bản EncosyTower).
- **Không** port `Latios.Unsafe.UnsafeParallelBlockList` job (chỉ để lại extension point).
- **Không** làm editor inspector/debugger ở phase 1 (đưa vào phase 5, xem roadmap).
- **Không** làm save/load *format* cố định (không chọn JSON/binary/Addressables cho bạn) —
  nhưng **có** làm đủ hạ tầng: state blittable, `TryCopyOwnerTo`/`TryRestoreOwner`,
  `StatOwnerRemap`, và `IStatModifier.RemapObservedStats` (xem
  [§2.9](02-ARCHITECTURE.md#29-persistence)).

## 1.4 Nguyên tắc port

| # | Nguyên tắc | Hệ quả |
|---|---|---|
| P1 | **Không sửa thuật toán khi port.** | Port 1:1 trước, tối ưu sau, mỗi divergence phải có mục trong decision log. |
| P2 | **Cô lập ECS vào một seam duy nhất.** | Tất cả `DynamicBuffer`/`BufferLookup`/`Entity` chỉ xuất hiện dưới dạng `StatBuffer`/`StatStore`/`StatOwnerHandle`. Không có `#if UNITY_ENTITIES` rải rác. |
| P3 | **Blittable-only.** | Không managed field trong bất kỳ struct nào của core. Không `class`, không delegate trong hot path. |
| P4 | **Codegen là bản sao mang tên khác.** | Cùng khung `Spec` + `WriteCode`, chỉ đổi namespace/type name và thay 4 primitive ECS. Diff so với EncosyTower phải đọc được. |
| P5 | **Fail an toàn, không throw trên happy path.** | Giữ pattern `bool TryXxx(...)`; validation nằm trong `ThrowIfXxx` có `[Conditional]`, biến mất ở release. |

## 1.5 Bảng mapping ECS → non-ECS

| Unity.Entities | ApexionGame.Entities.Stats | Ghi chú |
|---|---|---|
| `Entity` | `StatOwnerHandle` (readonly struct `{int index, int version}`) | Version chống dùng lại handle đã destroy → thay `Entity.Null` bằng `StatOwnerHandle.Null` |
| `StatOwner : IComponentData` (giữ `modifierIdCounter`) | field trong `StatOwnerSlot` (internal) | Không còn là "component" |
| `IComponentData` / `IBufferElementData` | *bỏ* | `Stat`, `StatModifier`, `StatObserver` chỉ là `unmanaged struct` |
| `DynamicBuffer<T>` | `StatBuffer<T>` — wrapper quanh `UnsafeList<T>*` | API con giống hệt: `Length`, `this[i]`, `ElementAt(i) → ref T`, `Add`, `Insert`, `RemoveAt`, `Clear`, `AsArray`, `AsSpan`, `AsReadOnlySpan`, `IsCreated` |
| `BufferLookup<T>.TryGetBuffer(e, out b)` | `StatStore.TryGetStats/TryGetModifiers/TryGetObservers(owner, out b)` | Trả `false` khi owner chết hoặc version lệch |
| `ComponentLookup<StatOwner>.GetRefRW(e)` | `StatStore.TryGetOwnerRef(owner, out ref slot)` | |
| `state.GetBufferLookup<T>(); lookup.Update(ref state)` | *bỏ* | Không có system scheduling → không cần refresh lookup |
| `IBaker`, `ibaker.AddBuffer<T>()` | `StatStore.CreateOwner(...) → StatBuilder` | Builder API, gọi lúc runtime hoặc lúc load scene |
| `EntityManager` / `EntityCommandBuffer` overloads của `AddStatComponents` | `StatStore.CreateOwner(capacityHints)` | Ba overload EM/ECB/ParallelWriter gộp thành một |
| `ComponentTypeSet` | *bỏ* | |
| `Unity.Assertions.Assert` | `ThrowHelper` nội bộ + `Debug.Assert` có `[Conditional]` | Bỏ dependency `Unity.Entities` |
| `RegisterGenericJobType` | codegen emit struct job **cụ thể** | Bản gốc cũng đã làm vậy |
| `Latios.Unsafe.UnsafeParallelBlockList` job (`#if LATIOS_FRAMEWORK`) | extension point, chưa implement | |

## 1.6 Điều gì *không* đổi

Để nhấn mạnh phạm vi: những thứ sau port nguyên văn, chỉ đổi kiểu tham số.

- `StatHandle`, `StatHandle<TStatData>`, `StatIndex`, `StatIndex<TStatData>`
  (index `0` vẫn là *None stat*, index hợp lệ `> 0`).
- `ModifierRange` / `ObserverRange` và **invariant sắp xếp**: modifier của stat `i`
  nằm liền khối trước modifier của stat `i+1`; observer cũng vậy. Mỗi lần insert/remove
  phải dịch `startIndex` của mọi stat phía sau.
- `StatVariant` + `StatVariantType` + `StatDataSize` + `StatUserDataSize` + `None`.
- `IStatModifierStack.Reset/Apply`, `IStatModifier.Apply/AddObservedStatsToList`
  (chỉ **thêm** `RemapObservedStats` — DEC-004; phần cũ không đổi).
- Thuật toán `UpdateSingleStatCommon`, `TryUpdateStat`, `TryAddStatModifier`
  (kể cả nhánh loop-detection và `deferRangeShift`), `TryRemoveStatModifier`.
- `StatWorldData`: event list + 5 scratch list + `_modifierStackRef`, kèm hợp đồng
  **không thread-safe / không reentrant**.
- Hình dạng API sinh ra: `Stats.Hp.Params.Create(...)`, `Stats.StatHandles`,
  `Stats.Options.Data`, `Stats.Accessor<T>`, `Stats.Reader<T>`, `StatSystem.API`, …
