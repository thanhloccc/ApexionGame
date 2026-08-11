# RTS Battle — sample

*[English](README.md) · [Thư viện](../ApexionGame.Entities.Stats/README.vi.md) ·
[Doc thiết kế](Documentation~/RTS-BATTLE-DESIGN.vi.md)*

Một game RTS nhỏ **chơi được thật**, dựng để trả lời một câu hỏi trong 30 giây đầu:

> tại sao tôi cần một dependency-graph stat system thay vì `Dictionary<string, float>`?

Spawn lính, mua research, cast buff/debuff, ra tướng, đập vỡ Stronghold địch. Mỗi cơ chế trong đó tồn tại
vì nó đưa một tính năng của thư viện lên màn hình.

Mở **`rts-battle.unity`** và bấm Play.

## Điều khiển

| | |
|---|---|
| `Q` `W` `E` `R` | spawn Footman / Archer / Knight / Warchief |
| `1` … `6` | chọn phép, rồi **click chuột trái xuống bàn đấu** để cast tại đó |
| click trái vào lính | xem stat của nó — đây là panel quan trọng nhất |
| kéo chuột phải, phím mũi tên | pan · lăn chuột: zoom |
| `Space` | tạm dừng · `F5`: chơi lại · `Esc`: bỏ chọn phép |

Team B do AI chơi. Nút **auto-play** ở thanh trên giao cả bên bạn cho AI, để sample tự chạy không cần ai
ngồi trước máy.

## Xem cái gì

**1 — Panel Inspector.** Click một Footman. `Attack` hiện `14 → 29`, và ngay dưới ghi
`▸ + Team A/node.AttackBonus`. Con số đó không được lưu trên unit; nó được tính từ một stat nằm trên **một
owner khác**, và nó đã đúng từ trước khi có ai hỏi tới.

**2 — Research.** Mua *Weapons*. Một lệnh `TrySetStatBaseValue` lên node team, và log cho bạn biết bao
nhiêu stat đã đổi. Trong toàn bộ project này không có vòng lặp nào chạy qua quân đội — kể cả unit spawn
sau đó vài phút cũng đúng ngay.

**3 — Tướng.** Ra Warchief và xem Attack của cả quân đội nhảy lên. Để nó giết 3 mạng là lên level: một
lệnh ghi vào `AuraPower`, log đếm số stat đi theo. Rồi để nó chết — quân đội mất aura ngay trong cùng lần
propagate đó.

**4 — Buff là modifier, còn thời hạn là việc của bạn.** Cast Bloodlust, Inspector của mục tiêu mọc thêm 2
dòng. Khi hết hạn, log ghi `Bloodlust expired on 14 unit(s): 11 of 14 modifier(s) given back` — 3 cái còn
lại đã chết cùng chủ của chúng. Runtime **không** có khái niệm thời hạn; `RtsEffectSystem`
chính là thứ cấp cho nó khái niệm đó.

**5 — Cái rò rỉ bạn không nhìn thấy.** Panel **STAT GRAPH** đếm observer trên node của từng team. Bấm
`cull 5 of mine` → số đó tụt. Bấm `sloppy` rồi bấm `cull 5 of mine` lần nữa → số đó **không** tụt, vì
destroy một owner không xoá những observer entry mà modifier của nó để lại trên node của người khác. Đó là
lỗi đắt nhất khi dùng thư viện này, và ở đây nó là một con số trên màn hình.

Đáng bấm nữa: `cycle` (xin runtime một aura tạo thành vòng, và bị từ chối), `recalc` (chạy lại mọi thứ qua
Burst job được generate), `prune` (xoá modifier đã báo mất nguồn), và `node` (xem chính node bonus dùng
chung — base value là research, còn modifier là aura tướng).

## Ba bài học

**Một unit tốn đúng 5 modifier, mãi mãi.** Ba link tới node của team, một chặn `Hp ≤ MaxHp`, một sàn
`AttackInterval`. Không phải 5 cho mỗi tướng, không phải 5 cho mỗi cấp research — là 5. Đó là thứ node dùng
chung mua được: nếu cho mỗi unit trỏ thẳng tới N tướng thì mọi lần spawn, chết, lên level đều tỉ lệ với N.
Có một test khoá bất biến này.

**Chết là phải remove modifier **trước** khi destroy owner.** Thứ gì unit đang chết đặt lên **owner khác**
thì phải tự tay tháo — các link của nó (đọc node) và, với tướng, các term aura (nằm trên node). Thứ gì nằm
trên stat của chính nó thì không cần làm gì: `DestroyOwner` xoá buffer của owner đó. Quy tắc không phải
"remove hết", mà là "remove mọi thứ chạm tới một owner khác owner đang bị destroy".

**`AuraPower` tồn tại để phá một cái vòng.** Cách viết aura hiển nhiên là "bonus Attack của team = 25%
Attack của tướng" — nhưng tướng cũng là unit, nên Attack của nó *đã* đọc bonus đó. `TryAddStatModifier` từ
chối vòng lặp ngay lúc insert và trả về `false` mà không throw — đó là kết quả tốt, và cũng là kết quả dễ
bị bỏ qua nhất. `AuraPower` là stat không nằm dưới hạ lưu của aura, nên chiều đó hợp lệ.

## Dựng như thế nào

**Hai assembly**, để layering là luật của compiler chứ không phải thói quen: `Rts.Game` tham chiếu
`Rts.Core`, còn `Rts.Core` không hề biết `Rts.Game` tồn tại. Không có chỗ nào trong tầng mô phỏng chạm được
tới `GameObject`, và có một test chứng minh điều đó.

```
Rts.Core/                 ← game. Không MonoBehaviour, không scene, không UI.
├── Stats/                [StatSystem], 6 modifier kind, hai collection
├── Content/              toàn bộ số của game, bất biến, trong RtsContent.cs
├── Model/                RtsUnit · RtsTeam · RtsEffect · RtsBattleFeed · RtsGeometry
├── World/RtsStatWorld    type DUY NHẤT chạm vào store, accessor, world data
├── Systems/              một file cho một cơ chế:
│   ├── RtsSpawnSystem        5 modifier mỗi unit sinh ra đã có
│   ├── RtsMovementSystem     chọn mục tiêu, đi, đẩy nhau
│   ├── RtsCombatSystem       nhát đánh, damage, hồi máu — chỉ ghi Hp
│   ├── RtsEffectSystem       phép, và cái "thời hạn" mà runtime không có
│   ├── RtsEconomySystem      supply, và một lệnh ghi của research
│   └── RtsDeathSystem        remove modifier trước khi destroy owner
├── Diagnostics/          RtsGraphProbe (đo) · RtsGraphExperiments (chọc) · RtsStatInspector (giải thích)
├── Journal/              chuyện đã xảy ra, ở dạng dữ liệu — không có câu chữ trong sim
├── Ai/RtsAiDirector      chơi một bên, không dùng random
└── Match/RtsMatch        giữ đúng hai thứ: ai tồn tại, và thứ tự mọi việc xảy ra

Rts.Game/                 ← trình bày. Biết Core; Core không biết nó.
├── RtsGame.cs            MonoBehaviour duy nhất: nối dây, và một Update
├── RtsMatchLoop.cs       vào là thời gian frame, ra là số tick nguyên + hệ số nội suy
├── RtsPlayerController   mọi động từ mà bàn phím và HUD dùng chung
├── Input/ · World/       đọc Input System · primitive, view lính, camera rig
└── Hud/                  một theme, một bộ widget, và một class cho mỗi panel
                          (top bar · command · graph · spell bar · journal · inspector)

Rts.Tests/                ← 20 test EditMode: bất biến graph, effect, dòng chảy ván đấu, layering
```

`RtsMatch` còn ~250 dòng và giữ đúng hai thứ: ai tồn tại, và thứ tự mọi việc xảy ra. Mỗi cơ chế là một
system đọc được độc lập — đó là khác biệt giữa bản này và class partial 2000 dòng mà nó thay thế.

Scene chỉ có một camera, một GameObject và một `UIDocument`. Bàn đấu, lính và toàn bộ giao diện đều dựng
lúc runtime.

**Collection.** Dùng `FasterList<T>`, `ArrayMap<K,V>` và `ArraySetNative<T>` của EncosyTower xuyên suốt, nên
phần việc mỗi tick — drain change event, lọc trùng stat đã đổi, theo dõi modifier dangling — không cấp phát
gì cả.

**Không có art asset nào.** Thân lính là primitive của Unity với material `Universal Render Pipeline/Unlit`
tạo lúc runtime; thanh máu là quad. Copy folder này sang project khác là chạy. (Dùng primitive chứ không
dùng sprite vì project này cấu hình Universal Renderer 3D, nơi đường sprite là cái bẫy material hồng chẳng
liên quan gì tới điều sample muốn dạy.)

## Test

```powershell
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Samples.Rts.Tests"
```

Hai mươi test trong bốn file, mỗi cái khoá một lỗi mà khi mắc thì không nhìn thấy được.

| | |
|---|---|
| `RtsGraphInvariantTests` | bất biến 5 modifier; research và aura chạm tới unit spawn sau; chết sạch không để lại observer — và test ngược lại chứng minh chết bẩn **có** rò, để cảnh báo phía trên là chuyện đã kiểm chứng chứ không phải truyền miệng |
| `RtsEffectTests` | hết hạn thì trả lại modifier; cast lại là refresh chứ không stack; hồi máu quá mức dừng đúng ở `MaxHp` chỉ nhờ modifier `ClampMax`; cast bị từ chối thì không mất supply lẫn cooldown |
| `RtsMatchFlowTests` | determinism với seed cố định; một ván đấu chạy tới khi có người thắng mà không còn modifier dangling; lệnh bị từ chối sau khi ván đã phân định |
| `RtsLayeringTests` | không có scene object trong tầng mô phỏng, không tham chiếu tầng trình bày, content không có public field mutable |

Test drive cả ván đấu mà không cần load scene, và dùng đúng API mà game dùng — trong tầng mô phỏng không có
hook nào tồn tại chỉ để phục vụ test. `cull 5 of mine` là một nút trong HUD trước, là công cụ test sau.

## Vài setting đáng chỉnh

Trên GameObject `rts-battle`:

| | |
|---|---|
| `Tick Seconds` | 0.05 — bước cố định; view nội suy giữa hai bước |
| `Supply Per Second` | toàn bộ nền kinh tế nằm trong một con số |
| `Max Units Per Team` | mặc định 60; bước separation là O(n²) mỗi team |
| `Sloppy Deaths` | cái rò rỉ, bật/tắt được ngay trong HUD |
| `Random Seed` | cố định, nên cùng chuỗi lệnh sẽ ra cùng kết quả |
| `Auto Play Player Team` | giao bên bạn cho AI ngay từ đầu |
