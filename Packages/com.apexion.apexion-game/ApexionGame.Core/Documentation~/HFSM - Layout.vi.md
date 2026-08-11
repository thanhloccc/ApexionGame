# HFSM — Layout

*[English](HFSM%20-%20Layout.md) · [Index](README.vi.md)*

Thư mục, namespace, tên file và nối dây asmdef chính xác. Theo
`.claude/skills/encosy-tower/references/structure-and-naming.md`; chỗ nào lệch khỏi nó đều được nêu
và biện minh.

---

## 1. Đặt tên module

| | |
|---|---|
| Thư mục | `HFSM/` |
| Namespace | `ApexionGame.HFSM` ([DEC-001](HFSM%20-%20Decisions.vi.md#dec-001)) |
| Tên type | **Không viết tắt trong định danh** ([DEC-019](HFSM%20-%20Decisions.vi.md#dec-019)) |

### 1.1 Ba tầng tên type

| Tầng | Luật | Ví dụ |
|---|---|---|
| Cỗ máy | Tên đầy đủ, viết ra hết | `HierarchicalStateMachine<TContext, TState>` |
| Thứ thuộc về một *cỗ máy* | `Machine*` | `MachineBuilder`, `MachineDefinition`, `MachineRunner`, `MachineError`, `MachineOptions`, `MachinePhase`, `IMachineControl` |
| Còn lại | Tên khái niệm trần, không tiền tố | `StateNode`, `Transition`, `StateInfo`, `GuardInfo`, `NodeIndex`, `TriggerId`, `TransitionLog`, `HistoryMode`, `TickMode`, `AsyncPolicy` |

"Hierarchical" xuất hiện đúng một lần, trên cỗ máy — nó là thuộc tính của cỗ máy, không phải của một
builder hay một node. Namespace cung cấp phần ngữ cảnh còn lại, và đó chính là việc của namespace.

Viết tắt chỉ sống sót ở chỗ nó là acronym viết HOA và đọc như một từ: thư mục `HFSM/`, đoạn namespace
`ApexionGame.HFSM`, define `APEXION_HFSM_DEBUG`, đường dẫn menu `ApexionGame > HFSM > Debugger`.
Dạng chữ hoa-thường `Hfsm` không xuất hiện ở đâu cả.

### 1.2 Va chạm mà nó tạo ra, và cách xử lý

`Transition` đụng với `UnityEngine.UIElements.Transition`. Điều đó chỉ ảnh hưởng đúng một chỗ —
assembly editor, nơi cần cả hai `using`:

```csharp
using UnityEngine.UIElements;
using Transition = ApexionGame.HFSM.Transition;   // bắt buộc trong ApexionGame.Core.Editor
```

Phần lớn code editor chạm tới `TransitionDebugInfo` và `TransitionLogEntry` chứ không phải chính
`Transition`, nên alias chỉ cần ở một hai file. Nêu ra đây để nó là một chi phí đã biết chứ không
phải một bất ngờ. `NodeIndex`, `StateInfo` và `GuardInfo` đã đối chiếu với các assembly Unity và
EncosyTower được tham chiếu — không va chạm.

### 1.3 Vì sao namespace giữ acronym

`ApexionGame.HFSM`, không phải `ApexionGame.Core.HFSM`: assembly của EncosyTower là
`EncosyTower.Core` trong khi module của nó nằm ở `EncosyTower.PubSub`, `EncosyTower.Collections` —
đoạn `.Core` là tên assembly, không phải tiền tố namespace. Soi gương điều đó giữ cho `using` phía
người dùng ngắn.

---

## 2. Assembly

Kế hoạch ban đầu là sáu anh em. Thực tế tồn tại, tính đến hết phase 1–7:

| Assembly | Thư mục | Tham chiếu | Mục đích |
|---|---|---|---|
| `ApexionGame.Core` | `Packages/com.apexion.apexion-game/ApexionGame.Core/` | `EncosyTower.Core`, `UniTask`, `Unity.Burst`, `Unity.Collections`, `Unity.Mathematics` (đã có sẵn) | runtime |
| `ApexionGame.Editor` | `Packages/com.apexion.apexion-game/ApexionGame.Editor/` | `ApexionGame.Core`, `EncosyTower.Core`, `EncosyTower.Editor` | assembly editor **dùng chung** cho mọi module `ApexionGame.*` — `MachineDebuggerWindow` nằm ở đây dưới `ApexionGame.HFSM.Editor`, không phải một `ApexionGame.Core.Editor` riêng |
| `ApexionGame.Tests.EditorMode` | `Packages/com.apexion.apexion-game/ApexionGame.Tests.EditorMode/ApexionGame.Core/HFSM/` | assembly test chung của cả project | golden test + benchmark, cùng chỗ với test của module Stats |

`ApexionGame.Core.Authoring`, `.Samples` và `.Samples.Editor` (phase 9 và phase 8) chưa được tạo —
chúng vẫn sẽ là assembly anh em riêng như kế hoạch gốc khi các phase đó bắt đầu, vì không có gì
khác trong project dùng chung code authoring hay sample theo cách `ApexionGame.Editor` đã dùng
chung công cụ editor.

`EncosyTower.Core.Extended` sẽ chỉ được `Samples` tham chiếu khi phase 8 tồn tại, để assembly
runtime giữ ít phụ thuộc ([DEC-011](HFSM%20-%20Decisions.vi.md#dec-011)).

### 2.1 Cái bẫy `versionDefines`

`ApexionGame.Core.asmdef` hiện có `"versionDefines": []`. Mọi `#if UNITASK`, `#if UNITY_COLLECTIONS`,
`#if UNITY_MATHEMATICS` bên trong nó đánh giá **false**, và code bị gác biến mất trong im lặng — kể
cả toàn bộ bề mặt async. Bước 1.1 chép nguyên khối từ `EncosyTower.Core.asmdef` vào cả sáu asmdef.

Kiểu hỏng này đáng gọi tên vì nó im lặng: assembly vẫn compile, chỉ là code không có ở đó. Kiểm bằng
cách bọc `#if UNITASK` quanh một lỗi cú pháp cố ý một lần và xác nhận compiler có kêu.

### 2.2 `allowUnsafeCode`

`ApexionGame.Core.asmdef` đã có `"allowUnsafeCode": true`. Thiết kế hiện không cần nó —
`UnsafeUtility.As` là một lời gọi phương thức bình thường, không phải khối `unsafe` — nhưng để bật
thì không tốn gì và khớp `ApexionGame.Entities.Stats`.

---

## 3. Cây runtime

```
Packages/com.apexion.apexion-game/ApexionGame.Core/
├── ApexionGame.Core.asmdef
├── AssemblyInfo.cs                          InternalsVisibleTo → .Tests, .Editor, .Authoring
├── Documentation~/                          bộ doc này (không .meta — Unity bỏ qua `~`)
└── HFSM/
    ├── HierarchicalStateMachine`2.cs                            máy đang sống: field, Tick, Fire, property
    ├── HierarchicalStateMachine`2+Transitions.cs                LCA, chuỗi exit/enter, ghi/khôi phục history
    ├── HierarchicalStateMachine`2+Async.cs                      driver async và xử lý AsyncPolicy
    ├── HierarchicalStateMachine`2+Debug.cs                      hiện thực IMachineDebug, có gate
    ├── MachineDefinition`2.cs                  mảng đã bake, CreateInstance, pool instance
    ├── MachineBuilder`2.cs                     mặt trước fluent, con trỏ scope
    ├── MachineBuilder`2+Validate.cs            bake + mọi case MachineError
    ├── TransitionBuilder`2.cs           struct `.To(...).When(...)`
    ├── MachineRunner.cs
    ├── MachineRunnerBehaviour.cs
    ├── MachineError.cs
    ├── StateBehaviour`1.cs
    ├── StateBehaviour`2.cs
    ├── AsyncStateBehaviour`1.cs             #if UNITASK || UNITY_6000_0_OR_NEWER
    ├── ThrowHelper.cs
    ├── Common/
    │   ├── StateNode.cs
    │   ├── Transition.cs
    │   ├── NodeIndex.cs
    │   ├── TriggerId.cs
    │   ├── StateInfo.cs
    │   ├── GuardInfo.cs
    │   ├── MachineOptions.cs
    │   ├── MachineDelegates.cs                 ba khai báo delegate, một file
    │   ├── StateNodeKind.cs
    │   ├── HistoryMode.cs
    │   ├── MachinePhase.cs
    │   ├── AsyncPolicy.cs
    │   ├── TickMode.cs
    │   └── TransitionCause.cs
    ├── Contracts/
    │   ├── IMachineControl.cs
    │   └── IMachineTickable.cs
    ├── Internals/
    │   ├── ActiveSet.cs                 bitset + sổ sách active-child
    │   ├── StateDataBlob.cs             bố trí offset + truy cập ô có kiểu
    │   └── ScopeStack.cs                chỉ dùng khi build, theo dõi Composite/Parallel đang mở
    └── Debugging/
        ├── ValidationDefines.cs
        ├── IMachineDebug.cs
        ├── MachineDebugRegistry.cs
        ├── StateNodeDebugInfo.cs
        ├── TransitionDebugInfo.cs
        ├── GuardDebugInfo.cs
        ├── GuardMeta.cs
        ├── TransitionLog.cs
        ├── TransitionLogEntry.cs
        ├── MachineOverlayPanel.cs
        └── MachineOverlayBehaviour.cs
```

### 3.1 Thư mục → namespace

| Thư mục | Namespace | Lý do |
|---|---|---|
| `HFSM/` | `ApexionGame.HFSM` | bề mặt công khai của module, một namespace phẳng, một `using` cho người dùng |
| `HFSM/Common/` | `ApexionGame.HFSM` | thư mục con để tổ chức; nó **không** thêm đoạn namespace |
| `HFSM/Contracts/` | `ApexionGame.HFSM` | như trên |
| `HFSM/Internals/` | `ApexionGame.HFSM.Internals` | `Internals` là một trong những đoạn *có* quyền có namespace riêng |
| `HFSM/Debugging/` | `ApexionGame.HFSM.Debugging` | tương tự — và khớp `ApexionGame.Entities.Stats.Debugging` |

Vậy người dùng viết đúng một `using ApexionGame.HFSM;` là có builder, máy, behaviour, enum và error.
Bề mặt debug là một `using` thứ hai, chọn tham gia một cách có chủ đích.

### 3.2 Đặt tên file

- `` HierarchicalStateMachine`2.cs `` — dạng backtick-arity, khớp `` StatStore`3.cs `` trong module Stats.
- `HierarchicalStateMachine`2+Transitions.cs` — chia partial kiểu `+Aspect`, khớp `` StatAccessor`6+Batch.cs ``.
- Mỗi file một type chính. `MachineDelegates.cs` chứa ba khai báo delegate là ngoại lệ duy nhất, vì một
  delegate một dòng cho mỗi file là nhiễu; nêu ra ở đây để nó không bị đọc như một sơ suất.
- Không có `*.gen.cs` — module này không có code generator riêng. Phần Roslyn ở phase 10 là một
  provider *refactoring*, nó ghi vào file của người dùng và không sinh gì lúc build.

---

## 4. Cây editor

Nằm trong assembly editor dùng chung `ApexionGame.Editor` (§2), không phải một anh em riêng:

```
Packages/com.apexion.apexion-game/ApexionGame.Editor/
├── ApexionGame.Editor.asmdef                includePlatforms: [Editor]
├── MachineDebuggerWindow.cs                    menu: ApexionGame > HFSM > Debugger
├── Views/
│   ├── MachineDebuggerView.cs                  toàn bộ thân cửa sổ
│   ├── MachineDebuggerViewController.cs        poll, diff hình dạng, lựa chọn
│   ├── ActivePathView.cs                khung trái: node active + thời gian + history
│   ├── StateGraphView.cs                     cây Painter2D + cạnh transition
│   ├── GuardListView.cs                 guard đi ra kèm kết quả sống
│   ├── TransitionLogView.cs             ring buffer thành các hàng
│   └── StateNodeElement.cs                   một hộp node trong đồ thị
└── StyleSheets/
    ├── MachineDebuggerWindow.tss               chỉ @import
    ├── MachineDebuggerWindow.uss               chỉ var(--…), không mã màu trực tiếp
    ├── MachineDebuggerWindow_Dark.uss          giá trị màu
    └── MachineDebuggerWindow_Light.uss         giá trị màu
```

Namespace: `ApexionGame.HFSM.Editor` cho cửa sổ, và cũng `ApexionGame.HFSM.Editor` cho `Views/` —
`Views` và `StyleSheets` không thêm đoạn namespace, cùng luật với `Common/`. Namespace độc lập với
tên assembly; `rootNamespace` của `ApexionGame.Editor.asmdef` chính nó là `ApexionGame.Editor`, một
mặc định cho các file không cần namespace riêng của HFSM.

Đường dẫn stylesheet là chuỗi `const` ghép từ `nameof(MachineDebuggerWindow)`, nạp qua
`WithEditorStyleSheet(...)`. UI dựng bằng C#; không có `.uxml`
([luật UI Toolkit](.claude/skills/encosy-tower/references/ui-toolkit.md)).

Overlay runtime (`MachineOverlayPanel`, `MachineOverlayBehaviour`, `MachineOverlayTheme`) **không**
nằm ở đây — nó phải chạy được trong bản build player, nên nằm ở
`ApexionGame.Core/HFSM/Debugging/` cùng registry và log, gác bởi đúng
`#if (UNITY_EDITOR || DEVELOPMENT_BUILD || APEXION_HFSM_DEBUG) && !DISABLE_APEXION_CHECKS` mà các
method `[Conditional]` dùng, viết bằng `#if` trực tiếp thay vì qua `ValidationDefines` vì
preprocessor không thấy được một `const string` của C#.

Các struct debug info mà cửa sổ dùng (`NodeDebugInfo`, `GuardDebugInfo`, `GuardMarker`,
`TransitionDebugInfo`) nằm trong `HFSM/Debugging/IMachineDebug.cs`, ngay cạnh interface trả về
chúng — không phải các file riêng `StateNodeDebugInfo.cs` / `GuardDebugInfo.cs` / `GuardMeta.cs`
như bản vẽ đầu tiên. `TransitionLog.cs` cũng chưa từng tách ra; ring buffer là field và method
private ngay trên `HierarchicalStateMachine`2+Debug.cs`.

---

## 5. Ba assembly còn lại

```
Packages/com.apexion.apexion-game/ApexionGame.Core.Authoring/
├── ApexionGame.Core.Authoring.asmdef
├── MachineGraphAsset.cs                        ScriptableObject: node + transition đã serialize
├── MachineGraphAsset+ToDefinition.cs           asset → MachineDefinition, trả Result
├── GuardCatalog`1.cs                    StringId → Guard<TContext>
├── BehaviourCatalog`1.cs                StringId → StateBehaviour<TContext>
└── Common/
    ├── StateNodeRecord.cs                    hàng authoring [Serializable]
    └── TransitionRecord.cs

Packages/com.apexion.apexion-game/ApexionGame.Core.Tests/
├── ApexionGame.Core.Tests.asmdef
├── MachineErrorTests.cs
├── MachineBuilderValidationTests.cs            mỗi case MachineError một test
├── MachineOrderingTests.cs                     chuỗi enter/exit/update golden
├── MachineTransitionTests.cs                   LCA, self, internal, nhắm vào composite
├── MachineTriggerTests.cs
├── MachineHistoryTests.cs
├── MachineParallelTests.cs
├── MachineAsyncTests.cs
├── MachineStateDataTests.cs                    cô lập theo instance, căn lề, Reset về 0
├── MachineRunnerTests.cs                       đăng ký/dispose trong Tick, máy ném lỗi
├── MachineBenchmarkTests.cs                    500 × 20 × 1000, khẳng định 0 cấp phát
└── Common/
    ├── TestContext.cs
    ├── TestStates.cs                        các enum dùng chung trong test
    └── RecordingBehaviour.cs                nối "Enter:Chase" v.v. vào một StringBuilder chung

Packages/com.apexion.apexion-game/ApexionGame.Core.Samples/
├── ApexionGame.Core.Samples.asmdef
├── MachinePlaygroundWorld.cs                   C# thuần, IDisposable, mỗi tình huống một hàm
├── MachinePlayground.cs                        MonoBehaviour [ExecuteAlways]
├── MachineOverlayCommand.cs                    IVisualCommand bật/tắt overlay
├── Enemy/
│   ├── EnemyContext.cs
│   ├── EnemyStates.cs                       hai enum
│   ├── EnemyBrain.cs                        definition
│   └── Behaviours/                          IdleBehaviour.cs, ChaseBehaviour.cs, …
└── Scenes/
    ├── hfsm-playground.unity                viết tay, .meta GUID cố định
    └── hfsm-playground.unity.meta

Packages/com.apexion.apexion-game/ApexionGame.Core.Samples.Editor/
├── ApexionGame.Core.Samples.Editor.asmdef
└── MachinePlaygroundEditor.cs                  mười nút, System.Action, không phải Invoke(name)
```

Namespace: `ApexionGame.HFSM.Authoring`, `ApexionGame.HFSM.Tests`, `ApexionGame.HFSM.Samples`,
`ApexionGame.HFSM.Samples.Editor`. Test và sample dùng namespace file-scoped; code production dùng
block-scoped.

`Enemy/Behaviours/` không thêm đoạn namespace — mọi type của demo đều nằm trong
`ApexionGame.HFSM.Samples`.

---

## 6. Project source generator

```
Plugins/SourceGenerator.ApexionGame/
└── ApexionGame.SourceGen.CodeRefactors/     ← đã có sẵn, mở rộng ở phase 10
    ├── HierarchicalStateMachineStateSkeletonRefactoring.cs
    ├── HierarchicalStateMachineStateDataRefactoring.cs
    └── HierarchicalStateMachineMachineSkeletonRefactoring.cs
```

Deploy vào `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/SourceGenerators/` cạnh các DLL sẵn có, qua
target MSBuild `CopyBuildArtifacts` đã dựng. Label `.meta`: `RunOnlyOnAssembliesWithReference` +
`RoslynAnalyzer`, **không** có label `SourceGenerator` — một provider code refactoring không phát code
lúc biên dịch. `isExplicitlyReferenced: 1`, mọi platform `enabled: 0`.

Ghi `.meta` **trong cùng một lệnh** với bước copy DLL. Nếu không, Unity đang mở sẽ import DLL trần rồi
tự sinh `.meta` mặc định với `isExplicitlyReferenced: 0`, auto-reference nó vào mọi assembly — đúng
cái bẫy mà phase 3.11 của bản port Stats đã dính.

---

## 7. Checklist trước khi tạo bất kỳ file nào

1. Namespace có theo §3.1 không — phẳng cho bề mặt công khai, chỉ thêm đoạn cho `Internals` và
   `Debugging`?
2. Using có nằm **ngoài** khối namespace block-scoped, xếp alphabet, ba nhóm không có dòng trống ở
   giữa: `System*`, rồi `ApexionGame*`/`EncosyTower*`, rồi `Unity*`?
3. Thứ tự thành viên có phải field → ctor → indexer → property → operator → method → type lồng, và
   trong mỗi nhóm là `const` → `static readonly` → `static` → instance, rồi `public` → `protected` →
   `private`?
4. Field private có `_camelCase`, static private có `s_camelCase`, const có `ALL_UPPER`?
5. Nếu file bị dời, `.meta` có đi theo không?
6. Mọi type mang attribute của EncosyTower có `partial` chưa?
