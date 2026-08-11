---
name: gameplay-assembly-map
description: Where gameplay code lives — the Game.Common → Game.Data → Game.Gameplay dependency chain and which systems already exist
metadata:
  type: project
---

As of 2026-08-09 the gameplay code is no longer greenfield. The chain, in dependency order:

- `Assets/Game/Game.Common` — typed ids (`WeaponId`, `PlayerSkillId`, `EquipmentId`, …), shared
  types (`DamageType`, `HitZone`, `AmmoType`), and the `GameStat*` facade over the stats fork.
- `Assets/Game/Game.Data` (+ `Game.Data.Authoring`) — EncosyTower Databases tables
  (Equipment, Player, Weapons) and Persistences (`GamePersistence`, `PlayerDataAccessor`).
- `Assets/Game/Game.Gameplay` (+ `.Editor`, `.Tests`) — Player, Weapons, Equipment systems.
- `Assets/Game/Game.Input`, `Packages/com.apexion.apexion-game/ApexionGame.Core` — input and shared
  project code (the latter is now an embedded package, not under `Assets/`).

**Why:** the first implementations have landed, so new work matches an existing pattern instead of
inventing one. Reading the neighbouring system beats designing from scratch — and replaces the older
"everything is empty scaffolding" assumption, which is now wrong.

**How to apply:** before extending a system, read its feature doc in
`Assets/Game/Game.Gameplay/Documentation~/` (`Equipment System - *`, `Player System - *`) and the
adjacent source. Place new code by the chain above — never make `Game.Common` depend upward. New
asmdefs still need the EncosyTower references plus the copied `versionDefines` block
(`.claude/skills/encosy-tower/references/setup.md`). Verify the current contents rather than
trusting this list verbatim. Related: [[house-style-is-encosy-conventions]],
[[follow-encosy-structure-and-naming]], [[apexion-entities-stats-is-dots-free-fork]].
