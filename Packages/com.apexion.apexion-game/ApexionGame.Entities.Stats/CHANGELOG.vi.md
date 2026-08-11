# Changelog

*[English](CHANGELOG.md)*

Theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/) và [SemVer](https://semver.org/lang/vi/).

## [0.1.0] — chưa phát hành

Bản port đầu tiên của `EncosyTower.Entities.Stats` sang môi trường không có
`com.unity.entities`.

### Added

- **Runtime** — `StatOwnerHandle`, `StatBuffer<T>`, `StatBufferLookup<T>`,
  `StatStore<TStat, TStatModifier, TStatObserver>` thay bốn primitive lưu trữ của ECS.
- **Runtime** — `StatAccessor<6>`, `StatReader<2>`, `StatWorldData<5>`, `StatBuilder<6>`,
  `StatAPI`, cùng ba deferred job (`List` / `Queue` / `Stream`).
- **Runtime** — `StatVariant`: union 19 kiểu của profile `gameplay`, kèm toán tử và hàm
  toán học sinh tự động.
- **Persistence** — `StatStore.TryCopyOwnerTo` / `TryRestoreOwner`, và
  `StatAccessor.TryRemapOwner` / `TryRemapOwners` để vá handle chéo owner sau khi tải.
- **Codegen** — ba generator (`[StatSystem]`, `[StatCollection]`, `[StatData]`) và ba
  analyzer, tổng 16 mã chẩn đoán. Job sinh ra được bọc trong struct không generic mang
  `[BurstCompile]`.
- **Authoring** — assembly `ApexionGame.Entities.Stats.Authoring` với
  `SerializableStatVariant` và `StatDefinitionAsset`.
- **Sample** — assembly `ApexionGame.Entities.Stats.Samples`: hai collection trên một system,
  modifier chéo owner, `MonoBehaviour` chạy thật. Thêm `StatsPlayground` + scene
  `stats-playground.unity`: 10 nút trong Inspector, mỗi nút một hành vi, chạy được ngoài play
  mode. Kèm project `Samples/Samples.RpgStats/` compile chính các file đó qua generator thật
  ngoài Unity, làm chốt chặn hồi quy cho cách dùng đã ghi trong tài liệu.
- **Sample** — sample thứ hai ở `Samples/Battle/`: `BattleStatSystem` với năm loại modifier
  (`Add`, `Multiply`, `AddFromStat`, `AddFractionOfStat`, `ClampMaxFromStat`) và stack biết chặn trên
  chứ không chỉ nhân, cùng `BattleSimulation` — một trận đánh tích luỹ, mỗi món đang mặc là một owner
  riêng, buff/debuff là tập modifier có thời hạn, sát thương theo tick, và modifier bị treo vì trang bị
  vỡ được tìm qua stream `ModifierTriggerEvent`. Vào từ GameObject `battle-sample` trong scene hoặc
  *ApexionGame > Stats > Battle Sample*.
- **Sample** — sample thứ ba ở `Samples/Rts/`: một trận hai phe, mọi lính đọc **một** node `TeamStats`
  dùng chung — research ghi base value, aura commander là modifier trên node — nên đổi cả quân là một
  lần ghi, và một lính tốn đúng năm modifier bất kể có bao nhiêu nguồn. Bao phủ những gì hai sample kia
  không có: spawn và chết ở quy mô lớn, số observer entry mà `DestroyOwner` không gỡ modifier trước làm
  rò rỉ trên node dùng chung (kèm con số hiển thị), cái chu trình mà một aura commander viết ngây thơ
  tạo ra cùng cách thiết kế tránh nó, và `DeferredUpdateStatListJob`. Vào từ GameObject `rts-sample`
  hoặc *ApexionGame > Stats > RTS Sample*.

### Fixed

- **Tài liệu** — `Hướng dẫn > Dùng stat` viết `Options.Data` được dựng từ `Params.Create(...)`. Tham số
  của nó là `Option<TStatData>`; `Params.Create` thuộc về `Builder.CreateStat` / `SetStat`.
- **Test** — 90 test cho runtime (gồm 23 golden test khoá bất biến thuật toán) và 17 test cho
  generator/analyzer. Thêm 5 benchmark ở quy mô 10k owner × 8 stat × 4 modifier — **không** đánh
  `[Explicit]`, vì test runner của Unity loại test `[Explicit]` kể cả khi gọi đích danh trong
  `--filter`.

### Changed so với bản gốc

Danh sách đầy đủ 23 chỗ lệch ở [`Documentation~/07-DECISIONS.md`](Documentation~/07-DECISIONS.md).
Đáng chú ý:

- **`StatStore` dùng mảng song song** (I-12) thay vì một slot ba tham số kiểu. Nhờ đó tồn tại
  `StatBufferLookup<T>` một tham số kiểu, đúng như `BufferLookup<T>` của ECS, và
  `IStatModifier.Apply` giữ nguyên hai tham số kiểu.
- **`StatBuilder` thay `StatBaker`** (I-20). Không có baking pipeline thì không có baker.
- **Bỏ `ComponentLookup<StatOwner>` khỏi `Accessor.ReadOnly`** (D-101). Ở bản gốc nó chỉ tồn
  tại để đăng ký read-dependency với ECS scheduler.
- Batch API nhận thêm `Allocator` (D-02), mặc định vẫn `Allocator.Temp`.

### Fixed

- **`StatVariant.ToString()` in ra tên kiểu chứ không phải giá trị.** Không có override nên nó
  rơi về `object.ToString()` → mọi dòng log và mọi ô inspector đều hiện
  `ApexionGame.Entities.Stats.StatVariant`. Đã thêm override phủ đủ 19 kiểu của profile.

- **`StatSystem.Accessor` sinh ra thiếu `TryRemapOwner`/`TryRemapOwners`** (I-24), khiến toàn bộ
  DEC-004 không với tới được từ API sinh ra. Lộ ra khi viết sample, không test nào bắt được —
  test generator kiểm tra code sinh ra *compile sạch*, không kiểm tra *có đủ thứ cần dùng*.

### Fixed — lỗi có ở bản gốc

- **Lan truyền để lại giá trị sai trên DAG lệch tầng** (DEC-005 / D-01). `TryUpdateStat` của
  bản gốc có visited-set, chặn đúng lượt tính lại có nhiệm vụ **sửa** giá trị đã tính khi
  nhánh dài còn cũ. Kim cương `A→B→D` và `A→C→E→D` với `A = 5` cho ra 6 thay vì 10, và không
  bao giờ tự sửa. Bản port bỏ visited-set ở cả hai lối lan truyền.
  Đánh đổi: xấu nhất O(k²) thay vì O(k) trên đồ thị dày kim cương.
  Số đo và lập luận đầy đủ ở [DEC-005](Documentation~/07-DECISIONS.md#dec-005).
- **14 test tích hợp của generator chưa từng chạy.** `UnityDllPaths.g.cs` sinh ở `BeforeBuild`
  nên nằm ngoài glob `Compile` đã đánh giá xong từ trước, và bị xoá sau mỗi build. Mọi test
  tích hợp báo `Inconclusive` trong khi `dotnet test` vẫn in `Passed!`. Sửa xong: 17 pass,
  0 skip. Chi tiết ở [06-ROADMAP](Documentation~/06-ROADMAP.md#hai-lỗi-làm-14-test-tích-hợp-chưa-từng-chạy).
- **Không dispose container `Allocator.Temp`** trong ba hàm batch. `Temp` tự thu hồi theo
  frame nên không rò rỉ thật, nhưng leak detection trong job sẽ cảnh báo.

### Tooling (phase 5)

- **`StatDebugRegistry` + `StatStoreDebug<4>`** (runtime) — một store tự đăng ký để tooling
  nhìn thấy. Không đăng ký thì không tốn gì; runtime không đọc registry ở đâu cả.
- **`StatStore.TryGetOwnerAt`** — API duy nhất thêm vào runtime cho tooling: dựng lại handle từ
  slot index. Gameplay không nên dùng, vì index slot không phải identity.
- **`ApexionGame.Entities.Stats.Editor`** — cửa sổ *ApexionGame > Stats > Stat Debugger*: chọn
  store, chọn owner, đọc `base → current` + số modifier/observer, kèm **đồ thị observer** vẽ
  bằng `Painter2D`. Node xếp theo longest-path depth để lộ đúng hình lệch tầng của DEC-005;
  endpoint chéo owner được vẽ mờ thay vì bỏ đi; có chu trình thì đỏ + băng cảnh báo — chu trình
  đáng lẽ không thể tồn tại, nên đó là **assertion sống** chứ không phải tính năng.
- **`ApexionGame.SourceGen.CodeRefactors`** — ba refactoring viết hộ phần `[StatSystem]` /
  `[StatCollection]` phải tự tay gõ, gồm cả 7 hook `partial void ...Internal` mà không đọc code
  sinh ra thì không đoán được. 9 test.

### Đã xác nhận trên máy

- **Burst**: ba job sinh ra (`DeferredUpdateStatListJob` / `QueueJob` / `StreamJob`) compile ra
  mã máy trong *Burst Inspector*. Ba job generic mở trong `StatJobs.cs` bị Burst bỏ qua — đúng
  như thiết kế, và là lý do generator sinh struct không generic bọc ngoài.
- **Test**: 928/928 EditMode toàn project; 95/95 trong `ApexionGame.Entities.Stats.Tests`;
  26/26 trong `ApexionGame.SourceGen.Tests` (17 generator/analyzer + 9 refactoring).
- **Benchmark** ở 10k owner × 8 stat × 4 modifier: ghi một stat 2.824 µs/owner, destroy owner
  0.166 µs — đúng như free-list của D-100 hứa hẹn.

### Chưa làm

- So sánh hiệu năng với `DynamicBuffer`: project không cài `com.unity.entities`.
- Lan truyền theo thứ tự topo — cách duy nhất để vừa đúng vừa tuyến tính.
