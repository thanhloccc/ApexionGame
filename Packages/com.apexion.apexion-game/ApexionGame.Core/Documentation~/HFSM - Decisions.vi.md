# HFSM — Decisions

*[English](HFSM%20-%20Decisions.md) · [Index](README.vi.md)*

Mỗi dòng một lựa chọn: chọn gì, loại gì, và vì sao. `chốt` = đã quyết, `open` = còn đáng tranh luận
lúc duyệt.

| id | Chủ đề | Trạng thái |
|---|---|---|
| [DEC-001](#dec-001) | Namespace là `ApexionGame.HFSM`, không phải `ApexionGame.Core.HFSM` | chốt (người dùng) |
| [DEC-002](#dec-002) | Class state + fluent builder, không source generator | chốt (người dùng) |
| [DEC-003](#dec-003) | `TContext : class` | chốt |
| [DEC-004](#dec-004) | Hai hình dạng delegate guard; `GuardInfo` truyền theo giá trị | chốt |
| [DEC-005](#dec-005) | `EndComposite()` tường minh thay vì lambda lồng | chốt |
| [DEC-006](#dec-006) | `TriggerId` = `TypeId` + ordinal | chốt |
| [DEC-007](#dec-007) | Trigger không khớp thì bỏ, không giữ lại | chốt |
| [DEC-008](#dec-008) | State data trong blob `byte[]` qua `UnsafeUtility.As`, không phải `NativeArray` | chốt |
| [DEC-009](#dec-009) | `StateInfo` mang `NodeIndex`, không phải `TState` | chốt |
| [DEC-010](#dec-010) | `CurrentState` trả về leaf của region 0 | chốt |
| [DEC-011](#dec-011) | `ApexionGame.Core` không tham chiếu `EncosyTower.Core.Extended` | chốt |
| [DEC-012](#dec-012) | Một transition mỗi tick mỗi region | chốt |
| [DEC-013](#dec-013) | Guard trong authoring tra qua catalog, không phải `ScriptableObject` guard serialize | **open** |
| [DEC-014](#dec-014) | `int[]` dày đặc cho active/history, không phải `ArrayMap` | chốt |
| [DEC-015](#dec-015) | `[CallerArgumentExpression]` cho nguyên văn guard, chấp nhận chi phí chuỗi trong bản release | chốt |
| [DEC-016](#dec-016) | Đăng ký debug là tự động, khác với đăng ký tự nguyện của Stats | chốt |
| [DEC-017](#dec-017) | Hiển thị giá trị guard là heuristic lúc build, không phân tích expression tree | chốt |
| [DEC-018](#dec-018) | Mặc định không phát state change qua `PubSub` | chốt |
| [DEC-019](#dec-019) | Tên type không viết tắt; `HierarchicalStateMachine` + `Machine*` + tên trần | chốt (người dùng) |

---

## DEC-001

**Namespace là `ApexionGame.HFSM`, không phải `ApexionGame.Core.HFSM`.** — *chốt, người dùng xác nhận*

Assembly của EncosyTower là `EncosyTower.Core`, nhưng module của nó là `EncosyTower.PubSub`,
`EncosyTower.Collections`, `EncosyTower.Pooling` — đoạn `.Core` gọi tên một assembly, không bao giờ
là tiền tố namespace. Soi gương điều đó cho người dùng `using ApexionGame.HFSM;`.

Loại: `ApexionGame.Core.HFSM`, đoán theo đường dẫn thư mục thì dễ hơn nhưng phá quy ước mà phần còn
lại của codebase đang theo, và làm mọi `using` phía người dùng dài ra mà không được gì.

Loại: `ApexionGame.StateMachines` kèm đổi tên thư mục cho khớp, phương án gỡ acronym khỏi mọi chỗ.
Người dùng giữ `HFSM/` — acronym ở lại nơi nó đọc như một acronym, và biến mất khỏi định danh
([DEC-019](#dec-019)).

## DEC-002

**State là class nối dây bằng fluent builder; không source generator.** — *chốt, do người dùng chọn*

Loại: `[StateMachine]` partial struct với switch dispatch sinh ra. Nó sẽ nhanh hơn và Burst được,
nhưng kéo theo một project Roslyn generator cùng cỡ với bản của Stats (~5–10k LOC) và làm việc bước
qua một transition trong debugger khó hơn nhiều. Tải đã nêu (vài trăm agent) không cần thứ nó mua về.

Loại: cấu hình thuần delegate không subclass. Ít code phải viết nhất, nhưng logic của mười lăm state
dồn hết vào một hàm khổng lồ, breakpoint rơi vào lambda, và closure có capture là nguồn dễ dãi của
trạng thái dùng chung ngoài ý muốn.

## DEC-003

**`TContext` ràng buộc là `class`.** — *chốt*

Blackboard bị ghi từ trong `OnUpdate`. Với `class`, `OnUpdate(TContext ctx, …)` cho behaviour sửa
field một cách tự nhiên. Với `TContext` không ràng buộc thì chữ ký phải là `ref TContext`, mà với
kiểu tham chiếu điều đó nghĩa là behaviour có thể gán lại context của cỗ máy — một cái bẫy đổi lấy
không gì.

Cái giá: context kiểu struct là bất khả, nên context luôn là một lần cấp phát cho mỗi agent. Ở vài
trăm agent đó là vài trăm object sống lâu, không phải bài toán mà thiết kế này cần giải.

## DEC-004

**Hai hình dạng delegate guard, và `GuardInfo` truyền theo giá trị.** — *chốt*

`.When(static c => c.Health < 20f)` là trường hợp phổ biến và phải giữ nguyên một dòng ngắn.
`.When(static (c, i) => i.TimeInState > 5f)` phủ phần còn lại. Hai overload, phân biệt bằng một cờ
trên transition, nên dạng một tham số không tốn thêm gì lúc gọi.

`GuardInfo` là `readonly struct` 24 byte truyền **theo giá trị**, không phải `in`. Với `in`, một
lambda sẽ phải khai kiểu tham số tường minh — `static (EnemyContext c, in GuardInfo i) => …` — và
mất luôn sự ngắn gọn làm cho builder đọc được. Chép 24 byte mỗi lần đánh giá guard rẻ hơn cái mất về
độ dễ đọc.

## DEC-005

**Scope đóng bằng `EndComposite()` / `EndParallel()` tường minh.** — *chốt*

Loại: lambda builder lồng, `.Composite(Combat, c => c.Child<…>()…)`, làm cho lệch scope là bất khả
ngay lúc viết. Nó đọc êm nhưng lồng khai báo sâu thêm hai tầng cho mỗi composite, và một cỗ máy phân
cấp sâu sẽ thành một kim tự tháp.

Rủi ro lệch được xử lý bằng validate thay thế: `MachineError.UnbalancedScope` gọi tên node và độ
sâu, và có một test cho nó. Một thông báo lỗi lúc build là thay thế chấp nhận được cho một điều bất khả lúc
compile ở đây, vì `Build()` chạy lúc khởi động, không phải ngoài chiến trường.

## DEC-006

**`TriggerId` là `TypeId<TTrigger>` cộng ordinal.** — *chốt*

`.On(EnemyTrigger.AttackFinished)` đổi thành viên enum thành một int. Nếu id chỉ là con int đó thì
`EnemyTrigger.Staggered` và `BossTrigger.Staggered` — cả hai ordinal 0 — sẽ là cùng một trigger, và
cú stun của boss sẽ bắn transition của enemy. Ghép với `TypeId` từ `EncosyTower.Types` làm chúng khác
nhau, đổi lấy 8 byte.

Loại: tham số generic thứ ba `HierarchicalStateMachine<TContext, TState, TTrigger>`. Nó chặn va
chạm ngay lúc compile nhưng buộc mọi chữ ký, mọi field và mọi khai báo của người dùng phải mang thêm một tham số kiểu, và
chặn luôn việc dùng chung một enum trigger cho nhiều máy.

## DEC-007

**Trigger đã xếp hàng mà không khớp gì ở cấu hình hiện tại thì bị bỏ.** — *chốt*

Loại: giữ nó trong hàng cho tới khi có ai xử lý. Nghĩa là một `Hit` bắn lúc cỗ máy đang ở một state
phớt lờ đòn đánh sẽ trồi lên vài phút sau, ngay khi cỗ máy tới một state có xử lý nó. Loại bug đó rất
khó lần ngược về nguyên nhân.

Việc bỏ được ghi vào log transition với dấu `unmatched` và log ở mức Warning dưới define debug, nên
nó nhìn thấy được chứ không im lặng — và đó mới là phần thật sự quan trọng.

## DEC-008

**Dữ liệu theo state nằm trong blob `byte[]` managed, truy cập bằng `UnsafeUtility.As`.** — *chốt*

`UnsafeUtility.As<byte, TData>(ref blob[offset])` trả về một **managed reference được GC theo dõi**.
Mảng không bao giờ bị pin, bộ thu gom vẫn dời được nó, và không có `GCHandle` cũng không có khối
`unsafe` nào trên đường tick.

Loại: `NativeArray<byte>` cho mỗi instance. Nó cho một con trỏ ổn định mà thiết kế này không bao giờ
cần, đổi lấy nghĩa vụ `Dispose` trên mọi agent và một lần cấp phát native cho mỗi lần spawn.

Loại: `byte[]` pin bằng `GCHandle`. 500 mảng bị pin làm phân mảnh managed heap mà không hơn gì cách
dùng `As`.

Ràng buộc nó tạo ra: offset phải căn theo `UnsafeUtility.AlignOf<TData>()` lúc build. Một `ref` lệch
căn tới một `double` là hành vi không định nghĩa trên vài nền tảng, nên việc căn lề được bake trong
`Build()` và có `MachineStateDataTests` phủ.

## DEC-009

**`StateInfo` mang `NodeIndex`, không phải `TState`.** — *chốt*

`StateBehaviour<TContext>` chỉ generic trên context, nên một class behaviour tái dùng được giữa các
máy có enum state khác nhau. Nếu `StateInfo` mang `TState` thì base của behaviour sẽ cần tham số
kiểu thứ ba và khả năng tái dùng đó biến mất.

Cái giá: đọc tên state từ trong một behaviour cần `machine.StateOf(info.Node)` thay vì một field trực
tiếp. Behaviour hiếm khi cần — chúng biết mình là state nào.

## DEC-010

**`CurrentState` trả về leaf sâu nhất của region 0.** — *chốt*

Có parallel region thì không có một state hiện tại duy nhất, và một property buộc phải trả về một
cái thì phải chọn. Region 0 là region khai báo đầu tiên, cũng là cái người ta nghĩ tới như "cái
chính" (movement, theo bố cục thường gặp).

Loại: ném lỗi khi cỗ máy có parallel region. `CurrentState` được dùng liên tục ở máy không parallel
và trong log; làm nó phụ thuộc hình dạng máy là thù địch với người dùng.

`GetActiveStates(Span<TState>)` là API trung thực cho trường hợp parallel, và doc comment của
`CurrentState` trỏ tới nó.

## DEC-011

**`ApexionGame.Core` không bao giờ tham chiếu `EncosyTower.Core.Extended`.** — *chốt*

Phần tích hợp console trong game (`IVisualCommand` bật/tắt overlay) thật sự có ích, nhưng nó nằm
trong `EncosyTower.Core.Extended`. Tham chiếu nó từ assembly runtime sẽ khiến mọi người dùng
`ApexionGame.Core` về sau — kể cả những ai chỉ muốn mỗi cỗ máy trạng thái — phải phụ thuộc vào nó.

Vì vậy command nằm trong `ApexionGame.Core.Samples`, ở đó nó cũng đóng vai ví dụ mẫu về cách nối dây
trong một project thật.

## DEC-012

**Tối đa một transition mỗi tick, mỗi region.** — *chốt*

Loại: lặp cho tới khi không còn transition nào đủ điều kiện. Điều đó làm một frame mất biên — một cặp
guard cấu hình sai có thể ping-pong mãi mãi — và biến log transition thành một bức tường mục trong
đúng một frame.

Cái giá là chuỗi `A → B → C` mất ba tick. Chỗ nào thật sự cần một chuỗi tức thì thì `.Internal()` kèm
`.Do(action)` làm được việc mà không đổi state, hoặc state trung gian đó thật ra không phải một state
và nên gộp vào action.

## DEC-013

**Guard dựng từ asset tra qua một catalog có kiểu, không phải `ScriptableObject` guard serialize.** — *open*

Một `GuardAsset : ScriptableObject` với `abstract bool Evaluate(object ctx, …)` sẽ là thiết kế
đậm chất Unity nhất, nhưng nó box context ở mỗi lần đánh giá và đặt một virtual call cộng một phép ép
kiểu lên đường tick — đúng thứ mà phần còn lại của thiết kế tránh.

Catalog — `GuardCatalog<TContext>` ánh xạ `StringId → Guard<TContext>`, điền bằng code — giữ
guard có kiểu và không cấp phát, mà vẫn cho asset tham chiếu chúng theo tên. Đánh đổi là designer
không thêm được guard *mới* nếu không có lập trình viên, chỉ nối lại những cái đã có.

Đánh dấu **open** vì nó phụ thuộc vào việc designer thật sự được tự chủ tới đâu trong khâu authoring,
mà phase 9 còn đủ xa để chưa cần chốt bây giờ.

## DEC-014

**`_activeChild` và `_historyChild` là `int[]` dày đặc, không phải `ArrayMap<int,int>`.** — *chốt*

Chỉ composite mới cần một mục, nên một map thưa trông có vẻ đúng hình dạng. Ở 20–40 node thì không:
mảng node cộng mảng bucket của `ArrayMap` lớn hơn một `int[]` phẳng cùng số node, và nó đặt một phép
băm cùng một lần dò lên đường transition.

Luật đang theo là luật thật trong phần performance defaults — `ArrayMap` thay `Dictionary` *khi bạn
cần một map* — chứ không phải "luôn với tay lấy container hào nhoáng nhất".

## DEC-015

**Nguyên văn guard đến từ `[CallerArgumentExpression]`, và chuỗi literal ở lại trong bản release.** — *chốt*

`[CallerArgumentExpression(nameof(guard))]` khiến compiler truyền vào nguyên văn mã nguồn của lambda.
Khung guard nhờ đó hiện `c.Health < 20f` mà người dùng không phải đặt tên gì — tính năng debug có giá
trị cao nhất trên mỗi đơn vị công sức của người dùng.

Cái giá là những chuỗi literal đó được nướng vào chỗ gọi và ở lại trong bản release ngay cả khi
`APEXION_HFSM_DEBUG` tắt. Sự tồn tại của một tham số không `#if` đi được nếu không đổi chữ ký. Với
một project có vài chục cỗ máy thì đó là vài trăm byte; lối còn lại — bắt viết
`.When(guard, "SeesPlayer")` ở khắp nơi — đổi một chi phí ergonomic thật lấy một khoản dung lượng
không đáng kể.

Phần *lưu trữ* thì vẫn có gate: `_guardMeta` chỉ cấp phát khi có define, nên lúc chạy không giữ gì.

## DEC-016

**Máy tự đăng ký vào registry debug; store của Stats thì đăng ký tự nguyện.** — *chốt*

Module Stats để đăng ký là chọn-tham-gia vì một lý do đúng: một `StatStore` là bộ nhớ native, phải
dựng một object adapter để xoá tham số kiểu của nó, và một đăng ký sống lâu hơn store là một con trỏ
treo.

Không điều nào trong đó đúng ở đây. `HierarchicalStateMachine<TContext, TState>` tự hiện thực
`IMachineDebug`, nên không có adapter; nó là object managed, nên một đăng ký cũ là chỗ rò chứ không phải crash; và lời gọi đăng ký
là `[Conditional]`, nên bản release thậm chí không gọi.

Đăng ký tự động chính là thứ khiến debugger chạy được với 0 dòng code của người dùng, và đó là mục
đích.

## DEC-017

**Hiển thị giá trị ở khung guard là một phép khớp mẫu lúc build, không phải phân tích expression tree.** — *chốt*

Hiện `Health = 74.0` cạnh `c.Health < 20f` đòi phải biết guard đọc field nào của context. Làm điều đó
cho tử tế nghĩa là `Expression<Func<…>>` thay cho một delegate trần — thứ đó cấp phát, compile chậm,
và không dùng được với IL2CPP nếu không cẩn thận.

Thay vào đó, nguyên văn bắt được ở DEC-015 được khớp một lần lúc build với mẫu
`c.<Field> <op> <literal>`. Khớp thì field được giải bằng reflection **một lần** và cache thành một
delegate getter. Không khớp thì hàng đó hiện biểu thức và kết quả boolean.

Một heuristic phủ trường hợp phổ biến và suy giảm về thứ vẫn dùng được thì hơn một giải pháp đầy đủ
mà bắt đường tick trả giá.

## DEC-018

**Mặc định không phát state change qua PubSub.** — *chốt*

`HierarchicalStateMachine.StateChanged` là một `Action<TState, TState>` trần. Phát mọi transition
qua `GlobalMessenger` nghĩa là 500 agent × N transition đi qua một bộ điều phối toàn cục mỗi giây, cho một thông tin gần
như không ai nghe.

Chỗ nào thật sự cần một người nghe tách rời — một hệ thống thành tựu phản ứng với "boss vào phase 2"
— thì chủ sở hữu đăng ký `StateChanged` rồi tự phát message domain của mình. Đó là một dòng ở đúng
một chỗ cần nó, thay vì một chi phí mà mọi cỗ máy đều phải trả.

## DEC-019

**Tên type không mang viết tắt. Ba tầng: cỗ máy, `Machine*`, và tên khái niệm trần.** — *chốt, người dùng chọn*

Bản nháp đầu gắn tiền tố `Hfsm` cho mọi thứ — `HfsmBuilder`, `HfsmNode`, `HfsmError`. Gọn, nhưng là
một viết tắt mà người đọc phải bung ra trong đầu ở từng định danh một.

Sơ đồ hiện tại:

| Tầng | Luật | Ví dụ |
|---|---|---|
| Cỗ máy | Viết ra hết | `HierarchicalStateMachine<TContext, TState>` |
| Thuộc về một *cỗ máy* | `Machine*` | `MachineBuilder`, `MachineDefinition`, `MachineRunner`, `MachineError`, `MachineOptions`, `MachinePhase`, `IMachineControl`, `IMachineTickable`, `IMachineDebug` |
| Còn lại | Tên trần | `StateNode`, `Transition`, `StateInfo`, `GuardInfo`, `NodeIndex`, `TriggerId`, `Guard<TContext>`, `TransitionLog`, `HistoryMode`, `TickMode`, `AsyncPolicy`, `TransitionCause` |

"Hierarchical" xuất hiện đúng một lần, vì nó mô tả cỗ máy — không phải một builder, không phải một
node.

Loại: bung tiền tố một cách cơ học ở mọi nơi, cho ra
`HierarchicalStateMachineTransitionBuilder<TContext, TState>` và
`HierarchicalStateMachineTransitionLogEntry`. Nhất quán, nhưng một dòng khai báo field xuống dòng
trước cả khi nó nói được điều gì.

Loại: `StateMachine*` cho tầng giữa (`StateMachineBuilder`, `StateMachineError`). Đọc êm nhưng lặp
chữ "State" đối lại `StateNode`, `StateInfo`, `StateBehaviour`, nơi "state" nghĩa là node chứ không
phải cỗ máy — hai nghĩa của một từ trong cùng một namespace.

**Cái giá, chấp nhận có ý thức:** tên trần dễ va chạm hơn. `Transition` đụng với
`UnityEngine.UIElements.Transition`, và điều đó cắn ở `ApexionGame.Core.Editor` nơi cần cả hai
`using`. Cách xử lý là một dòng alias trong các file bị ảnh hưởng, ghi ở
[Layout §1.2](HFSM%20-%20Layout.vi.md#12-va-chạm-mà-nó-tạo-ra-và-cách-xử-lý). `NodeIndex`,
`StateInfo`, `GuardInfo` và `TriggerId` đã đối chiếu với các assembly Unity và EncosyTower được tham
chiếu, và sạch.

**Một sửa đổi kéo theo.** NPC mẫu ở
[API Surface §9](HFSM%20-%20API%20Surface.vi.md#9-ví-dụ-đầy-đủ) tên là `Guard` (`GuardState`,
`GuardContext`, `GuardBrain`). Nay delegate *guard* mang tên `Guard<TContext>`, câu đó đọc thành
`Guard<GuardContext>` — hợp lệ với C#, không đọc nổi với người. NPC mẫu đổi tên thành `Sentry`.

**Một bug thật mà lần đổi tên này phơi ra.** Bản nháp đầu khai cả
`public static class Hfsm<TContext, TState>` (giữ `Define`) lẫn
`public sealed class Hfsm<TContext, TState>` (cỗ máy) — cùng tên và cùng arity hai lần trong một
namespace, thứ đó không compile. `Define` nay là **thành viên static của chính class máy**, nên
`HierarchicalStateMachine<EnemyContext, EnemyState>.Define("EnemyBrain")` là điểm vào và không có
type factory riêng.
