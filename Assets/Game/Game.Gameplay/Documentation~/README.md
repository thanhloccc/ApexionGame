# Game.Gameplay — Tài liệu

Code gameplay riêng của Land of Souls. Dựng trên **EncosyTower**
(`Packages/com.laicasaane.encosy-tower`) và **ApexionGame** (`Packages/com.apexion.apexion-game` —
HFSM, bản fork stats DOTS-free).

## Hệ thống

| Hệ thống | Namespace | Trạng thái | Tài liệu |
|---|---|---|---|
| Equipment — trang bị, trọng lượng, băng đạn, chỉ số | `Game.Gameplay.Items`, `Game.Gameplay.Equipment` | **Phase A đã triển khai** | [Overview](Equipment%20System%20-%20Overview.md) |
| Weapon logic — vòng đời đòn đánh, bắn, nạp đạn | `Game.Gameplay.Weapons` | **Phase B đã triển khai** | [Weapon Logic](Equipment%20System%20-%20Weapon%20Logic.md) |
| Combat — dò trúng, sát thương, máu, chết | `Game.Gameplay.Combat` | **Phase C đã triển khai** | [Combat](Combat%20-%20Overview.md) |

## Thứ tự đọc

| # | File | Đọc khi |
|---|---|---|
| 0 | [Gameplay - Roadmap](Gameplay%20-%20Roadmap.md) | đọc trước tiên — các phase, thứ tự, và những seam đang hở |
| 1 | [Equipment System - Overview](Equipment%20System%20-%20Overview.md) | yêu cầu, phân rã, sở hữu state, bậc hiệu năng, các bước của phase A |
| 2 | [Equipment System - Weapon Logic](Equipment%20System%20-%20Weapon%20Logic.md) | trước khi động vào logic vũ khí; máy trạng thái chạy trên HFSM |
| 3 | [Combat - Overview](Combat%20-%20Overview.md) | dò trúng, sát thương, máu; vá hai seam của A/B |

## Assembly

```
Game.Common      ids có kiểu, enum, CharacterStats + GameStatSystem   (unsafe = true)
   ↓
Game.Data        bảng gốc `item` + 4 bảng mở rộng + GameDatabase
   ↓
Game.Gameplay    ItemCatalog · EquipmentRules · Loadout · EquipmentStatBinding
                 WeaponController · MeleeAttackMachine · RangedFireMachine
                 CombatWorld · Combatant · AttackResolver · CombatantRegistry
                 HitQuery · DamageResolver · Health
   ↓
Game.Gameplay.Tests   105 test EditMode (A + B + C)
```

Không bao giờ phụ thuộc ngược hướng.

## Chạy test

```powershell
unity command run_tests --project-path=<repo> --non-interactive --mode EditMode `
  --filter "Game.Gameplay.Tests"
```

## Quy ước

- Style: `CODING-CONVENTIONS.md` ở gốc repo, và skill `coding-standards`.
- Kiến trúc, chọn module: skill `encosy-tower` và `system-design`.
- Thao tác Unity (compile, test, build): skill `unity-cli-workflow`.
- Ba cái bẫy đã trả giá để biết, ghi ở `.claude/project-profile.md`: `[StatCollection]` cần
  `allowUnsafeCode`; mỗi assembly cần `csc.rsp` riêng; khối `versionDefines` phải chép sang assembly mới.
