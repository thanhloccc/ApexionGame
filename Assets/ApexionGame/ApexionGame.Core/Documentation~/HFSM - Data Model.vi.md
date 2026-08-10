# HFSM — Data Model

*[English](HFSM%20-%20Data%20Model.md) · [Index](README.vi.md)*

Mọi type, collection chọn cho nó, và lý do. Luật mà tài liệu này sinh ra để giữ: **definition** build
một lần và dùng chung, **instance** nhỏ và cấp phát theo từng agent, và không gì trên đường tick cấp
phát.

---

## 1. Hai nửa

```
        ┌────────── dùng chung, bất biến, mỗi hình dạng máy một cái ──────────────────────────┐
        │  MachineDefinition<TContext, TState>                                                  │
        │  ├─ StateNode[]              node, index dày đặc, bake sẵn Depth/Parent/Kind        │
        │  ├─ Transition[]        sắp theo nguồn; mỗi node giữ (start, count)            │
        │  ├─ StateBehaviour<TContext>[]   mỗi node có behaviour thì một instance flyweight  │
        │  ├─ Guard<TContext>[]   delegate guard                                         │
        │  └─ GuardMeta[]         nguyên văn + priority   (chỉ dưới APEXION_HFSM_DEBUG)  │
        └────────────────────────────────────────────────────────────────────────────────────┘
                                             ▲  tham chiếu, không bao giờ chép
        ┌────────────────────────────────────┴──────── mỗi agent một cái, ~250 B + blob ─────┐
        │  HierarchicalStateMachine<TContext, TState>                                                            │
        │  ├─ TContext                blackboard của agent                                   │
        │  ├─ int[]   _activeChild    theo composite: con nào đang active, −1 = không        │
        │  ├─ int[]   _historyChild   theo composite: con được nhớ                           │
        │  ├─ float[] _timeInNode     theo node                                              │
        │  ├─ ulong[] _activeMask     bitset trên các node                                   │
        │  ├─ byte[]  _stateData      blob mà mọi StateBehaviour<_, TData> ghi vào           │
        │  └─ FasterList<TriggerId> _pendingTriggers  (chặn ở 16)                        │
        └────────────────────────────────────────────────────────────────────────────────────┘
```

500 agent trên máy 20 node: **một** definition (~2 KB) cộng 500 × (~250 B + blob). Với bốn behaviour
mỗi cái khai 16 byte data, đó là 500 × 314 B ≈ **153 KB tổng**. Lối còn lại — mỗi agent một cây
object state — sẽ là 500 × 20 object ≈ 10 000 lần cấp phát.

---

## 2. Type định danh

| Type | Hình dạng | Collection / lưu trữ | Vì sao |
|---|---|---|---|
| `TState` | `enum` của người dùng, `unmanaged` | — | Đổi thành `int` dày đặc lúc build qua `EnumExtensions.ToIndex()`. Không bao giờ lưu dạng enum trong mảng nóng. |
| `NodeIndex` | `[WrapType(typeof(int))] readonly partial struct` | giá trị | Index có kiểu, để không truyền nhầm node index vào chỗ cần transition index. `default` là giá trị không hợp lệ, khớp `StatIndex` của module Stats. |
| `TriggerId` | `readonly struct { TypeId Type; int Value; }` — 8 byte | giá trị | `TypeId<TTrigger>` từ `EncosyTower.Types` cộng ordinal, để hai enum trigger khác nhau không đụng nhau ở ordinal 0. |

`TState → NodeIndex` được giải một lần, lúc build. Lúc chạy máy làm việc hoàn toàn bằng node
index `int` dày đặc; enum chỉ xuất hiện lại ở biên API (`CurrentState`, `GetActiveStates`) và trong
debugger.

---

## 3. Phía definition

### 3.1 `StateNode` — 32 byte

```csharp
public readonly struct StateNode
{
    public readonly int Parent;             // −1 với Root
    public readonly int FirstChild;         // −1 với leaf
    public readonly int ChildCount;
    public readonly int InitialChild;       // −1 khi Kind == Leaf
    public readonly int TransitionStart;    // index vào Transition[]
    public readonly int TransitionCount;
    public readonly int StateDataOffset;    // offset byte trong blob của instance, −1 nếu không có
    public readonly ushort Depth;           // Root = 0; làm LCA thành O(depth), không phải tìm kiếm
    public readonly byte KindAndHistory;    // StateNodeKind nibble thấp, HistoryMode nibble cao
    public readonly byte BehaviourIndex;    // 0xFF = không có behaviour
}
```

Lưu dạng `StateNode[]`, không phải `FasterList<T>` — số lượng đã chốt sau `Build()`, và mảng trần là
thứ rẻ nhất để index trên đường tick. `FasterList<T>` dùng trong lúc *dựng* nó.

Việc bake sẵn `Depth` là thứ biến `LowestCommonAncestor` thành một vòng lặp `Depth` bước thay vì một
cuộc tìm kiếm ([Flows §4](HFSM%20-%20Flows.vi.md#4-thực-thi-một-transition)).

### 3.2 `Transition` — 32 byte

```csharp
public readonly struct Transition
{
    public readonly int Source;             // −1 với transition AnyState
    public readonly int Target;
    public readonly int GuardIndex;         // −1 = không guard
    public readonly int ActionIndex;        // −1 = không action
    public readonly TriggerId Trigger;  // default = polled, không phải triggered
    public readonly float MinDuration;
    public readonly short Priority;
    public readonly byte Flags;             // Internal | HasAfter | …
}
```

Lưu dạng `Transition[]`, **sắp theo node nguồn, rồi priority giảm dần, rồi thứ tự khai báo**. Nhờ
đã sắp, transition đi ra của một node là một lát liền kề gọi tên bằng
`(TransitionStart, TransitionCount)` — đánh giá một node là một lượt đi tuyến tính trên bộ nhớ kề
nhau trong cache, không gián tiếp, không object list riêng cho từng node.

Transition `AnyState` chiếm lát ở index 0, nên chúng cũng liền kề và được đọc trước.

### 3.3 Behaviour và guard

| Mảng | Type | Ghi chú |
|---|---|---|
| `_behaviours` | `StateBehaviour<TContext>[]` | Mỗi node có khai thì một instance. **Dùng chung cho mọi agent** — đây chính là flyweight. Behaviour không có `TData` còn dùng chung được giữa nhiều node. |
| `_guards` | `object[]` giữ `Guard<TContext>` hoặc `GuardWithInfo<TContext>` | Hai hình dạng delegate, phân biệt bằng một cờ trên transition, để lambda một tham số dạng ngắn không tốn thêm gì lúc gọi. |
| `_actions` | `TransitionAction<TContext>[]` | Thân của `.Do(...)`. |
| `_guardMeta` | `GuardMeta[]` | Nguyên văn từ `[CallerArgumentExpression]`, tên node. **Chỉ cấp phát dưới `APEXION_HFSM_DEBUG`.** |

Một instance behaviour **không giữ field nào theo agent**. Đó là bất biến cứng, không phải quy ước:
nó chính là thứ khiến một definition phục vụ được 500 agent. Dữ liệu theo agent nằm ở `TData` (§4.2)
hoặc ở `TContext`.

---

## 4. Phía instance

### 4.1 Mảng, kích thước theo số node

| Field | Type | Cỡ với 20 node | Vì sao type này |
|---|---|---|---|
| `_activeChild` | `int[]` | 80 B | Index theo node, ghi ở mọi transition. Ở cỡ này mảng trần thắng mọi loại map. |
| `_historyChild` | `int[]` | 80 B | Cùng hình dạng; chỉ chạm tới với composite có history, nhưng một mảng song song rẻ hơn một map thưa. |
| `_timeInNode` | `float[]` | 80 B | Chỉ tăng cho node active, nhưng index theo node nên không phải tra cứu. |
| `_activeMask` | `ulong[]` | 8 B | Bitset. `IsActive(node)` là một phép dịch và một phép test — cần thiết vì với parallel region, "X có active không" không còn là "X có nằm trên đường hiện tại không". |
| `_pendingTriggers` | `FasterList<TriggerId>` | trần 128 B | Chặn ở 16; `Fire` quá số đó trả `MachineError.TriggerQueueFull` thay vì phình vô hạn. |

Tổng chi phí cố định ≈ **250 B** cho máy 20 node, một đợt cấp phát lúc dựng, tái dùng qua `Reset()`
và pooling.

Đã cân nhắc `ArrayMap<K,V>` cho `_activeChild`/`_historyChild` vì chỉ composite mới cần mục. Bị loại:
ở 20–40 node, `int[]` dày đặc nhỏ hơn cả mảng node của map *và* bỏ được bước băm khỏi đường
transition ([DEC-014](HFSM%20-%20Decisions.vi.md#dec-014)).

### 4.2 Blob state data

```
_stateData : byte[]        một lần cấp phát, bố trí lúc build

 offset 0        16        24                    40
 ┌──────────────┬─────────┬────────────────────┬─────────────┐
 │ ChaseData    │ (đệm)   │ AttackData         │ FleeData    │
 │ 12 B, align 4│         │ 16 B, align 8      │ 8 B, align 8│
 └──────────────┴─────────┴────────────────────┴─────────────┘
     node 4         —          node 5              node 6
```

- Offset gán trong `Build()`, mỗi cái căn theo `UnsafeUtility.AlignOf<TData>()`, và lưu vào
  `StateNode.StateDataOffset`. Tổng là `MachineDefinition.StateDataSize`.
- `StateBehaviour<TContext, TData>` chạm ô của nó bằng
  `UnsafeUtility.As<byte, TData>(ref blob[offset])`, cho ra một **managed `ref` được GC theo dõi** —
  không pin, không `GCHandle`, bộ thu gom vẫn dời được mảng ([DEC-008](HFSM%20-%20Decisions.vi.md#dec-008)).
- Máy nào có behaviour không khai data thì cấp phát mảng độ dài 0, không phải `null`, để đường
  dispatch không có nhánh rẽ.
- `Reset()` xoá blob bằng `Array.Clear`. Vì vậy struct data luôn bắt đầu từ 0 — đây là bảo đảm được
  ghi rõ, nên `OnEnter` không phải khởi tạo phòng thủ từng field.

`NativeArray<byte>` là phương án thay thế. Bị loại: nó thêm nghĩa vụ `Dispose` và một lần cấp phát
native cho mỗi agent, để đổi lấy một con trỏ ổn định mà thiết kế này không bao giờ cần.

---

## 5. Cấp phát tạm thời, và chỗ nào tránh được

| Đường | Nó cần gì | Cách giữ 0 cấp phát |
|---|---|---|
| Thực thi một transition | chuỗi exit và chuỗi enter, dạng danh sách node có thứ tự | Thuê từ `FasterListPool<int>` và trả ngay trong cùng lời gọi. Không phải field của instance — một bộ đệm theo instance sẽ là 500 bộ đệm nằm không để phục vụ một transition tại một thời điểm. |
| Lúc build | danh sách node, danh sách transition, map `TState → index`, ngăn xếp scope | `FasterList<T>` cho các danh sách, `ArrayMap<int,int>` thuê từ `ArrayMapPool` cho map, cả hai trả lại lúc `Build()` kết thúc. Build là đường lạnh; ở đó rõ ràng thắng tinh chỉnh, nhưng không rò rỉ gì. |
| Đánh giá guard | không gì | Guard theo quy ước là lambda `static`, nên không cấp phát closure. Lambda có capture vẫn chạy nhưng cấp phát một lần lúc build, không bao giờ theo tick. |
| Hàng đợi trigger | tối đa 16 id | `FasterList` đặt sẵn dung lượng lúc dựng; `Clear()` khi rút, không bao giờ cấp phát lại. |
| Poll debug | danh sách info node/transition/guard | Các hàm `IMachineDebug` *nối thêm* vào list do người gọi sở hữu, nên cửa sổ debugger tái dùng cùng bộ list qua mọi lượt poll. Cùng hợp đồng với `IStatStoreDebug`. |
| Sự kiện `StateChanged` | — | `Action<TState, TState>` với `TState` là enum có thể box ở vài lối gọi; ở đây thì không, vì delegate generic trên value type. Xác nhận bằng benchmark 0-cấp-phát chứ không giả định. |

Mỗi tick, mỗi transition, mỗi trigger: **0 cấp phát GC**, khẳng định bằng
`Is.Not.AllocatingGCMemory()` trong `MachineBenchmarkTests`.

---

## 6. Hợp đồng lỗi

`MachineError` là wrapper `[PolyEnumFactoryFor]` trên union `[PolyEnumStruct]` — không bao giờ là enum
phẳng, theo luật thường trực của project. Payload là index có kiểu, không bao giờ là chuỗi ở chỗ đã
có index.

```csharp
[PolyEnumFactoryFor(typeof(Error))]
public readonly partial struct MachineError
{
    private readonly FixedString64Bytes _prefix;
    private readonly Error _error;

    public MachineError Prefix(in FixedString64Bytes prefix) => new(_error, prefix);
    public override string ToString() => _error.ToMessage(_prefix).ToString();

    [PolyEnumStruct]
    readonly partial struct Error
    {
        partial interface IEnumCase
        {
            FixedString512Bytes ToMessage(in FixedString64Bytes prefix);
        }

        public readonly partial struct Undefined { … }
        public readonly partial record struct UnknownState(NodeIndex Node) { … }
        // …
    }
}
```

| Case | Payload | Bắn khi |
|---|---|---|
| `Undefined` | — | `default(MachineError)`; luôn có mặt để `default.ToString()` an toàn |
| `UnknownState` | `NodeIndex` | một transition nhắm vào thành viên enum chưa từng được khai làm node |
| `DuplicateState` | `NodeIndex` | cùng một thành viên enum khai hai lần |
| `NoInitialChild` | `NodeIndex` | vào một composite không có `Initial()` và không có history |
| `InitialChildNotAChild` | `NodeIndex parent, NodeIndex child` | `Initial()` gọi tên một node không phải con của composite này |
| `LeafHasChildren` | `NodeIndex` | gọi `Child()` bên trong một node đã khai là leaf |
| `UnbalancedScope` | `NodeIndex, int depth` | thiếu hoặc thừa `EndComposite`/`EndParallel` |
| `TransitionCrossesParallelRegion` | `NodeIndex source, NodeIndex target` | nguồn và đích nằm ở hai region khác nhau của một node parallel |
| `CycleInInitialChain` | `NodeIndex` | chuỗi `Initial` chạy vòng thay vì tới một leaf |
| `MachineNotRunning` | — | `Fire`/`RequestTransition` trước `Start()` hoặc sau `Dispose()` |
| `TriggerQueueFull` | `TriggerId, int capacity` | quá 16 trigger xếp hàng cho một instance |
| `TransitionInFlight` | `NodeIndex source, NodeIndex target` | `RequestTransition` giữa một chuỗi async dưới `AsyncPolicy.Ignore` |

Chỗ gọi dùng factory sinh ra kèm dấu ngoặc: `MachineError.NoInitialChild(node)`. Mỗi case đều có một
test sinh nó ra từ một input sai thật — một case không gì bắn ra được là một lời nói dối trong bề
mặt API.

Luật đầy đủ cho hình dạng này: `.claude/skills/encosy-tower/references/structured-errors.md`.

---

## 7. Toán

Gần như không có, và điều đó đáng nói ra thay vì với tay lấy `Unity.Mathematics` theo thói quen.
Đường tick làm phép cộng `float` trên timer và so sánh `float` trên `MinDuration`. `math.max` dùng ở
chỗ cần chặn. Không vector, không quaternion, không job.

`Unity.Mathematics` vẫn được asmdef tham chiếu, vì `GuardInfo` và context của người dùng nằm
cạnh code gameplay có dùng `float3`, và ép chuyển đổi ở biên đó còn tệ hơn một reference không dùng.

---

## 8. Ngân sách bộ nhớ, mục tiêu đo được

| Kịch bản | Definition | Mỗi instance | 500 agent |
|---|---|---|---|
| 8 node, không state data | ~0.9 KB | 112 B | 55 KB |
| 20 node, 4 × 16 B state data | ~2.1 KB | 314 B | 153 KB |
| 40 node, parallel ×3, 8 × 16 B | ~4.4 KB | 620 B | 303 KB |

Đây là những con số mà benchmark ở [Roadmap §4](HFSM%20-%20Roadmap.vi.md#4-benchmark) đối chiếu. Nếu
một phép đo thật không khớp bảng này thì bảng sai và phải sửa — không phải ngược lại.
