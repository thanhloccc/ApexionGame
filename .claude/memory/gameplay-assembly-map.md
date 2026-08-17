---
name: gameplay-assembly-map
description: Where code lives — the com.apexion.apexion-game package holds everything real; Assets/Game is empty scaffolding
metadata:
  type: project
---

Verified 2026-08-15. **All first-party code lives in the embedded package
`Packages/com.apexion.apexion-game/`**, not under `Assets/`:

- `ApexionGame.Core` — the **HFSM** state machine (`ApexionGame.HFSM`), phases 1–7 done.
- `ApexionGame.Entities.Stats` (+ `.Authoring`, `.Editor`) — the DOTS-free stats fork.
- `ApexionGame.Entities.Stats.Samples.Rts/` — one folder, two assemblies (`Rts.Core/`, `Rts.Game/`).
- `ApexionGame.Tests.EditorMode` — the **only** first-party test assembly, EditMode only.

`Assets/Game/Game.Common`, `Game.Data`, `Game.Data.Authoring`, `Game.Gameplay` are **empty folders
with no `.asmdef` and no source files**. Only `Assets/Game/Input/` (`Game.Input.asmdef`),
`Addressables/` and `Scenes/` hold anything.

**Why:** an earlier version of this memory said the gameplay chain was populated with Player /
Weapons / Equipment systems, `PlayerError.cs`, and feature docs under `Game.Gameplay/Documentation~/`.
None of that is in the working tree, and none of it is in git history either — so pointing at it as
"the existing pattern" sends every session chasing dead paths. Game-side gameplay is greenfield.

**How to apply:** look for existing patterns in the package, not in `Assets/Game`. The reference
`Result` error is `ApexionGame.Core/HFSM/MachineError.cs`; the model doc set is
`ApexionGame.Core/Documentation~/HFSM - *.md`. Split new code by reusability — game-specific goes to
`Assets/Game/…` (creating the `.asmdef` with the copied `versionDefines` block as the first act),
anything that outlives this title goes in the package. Intended direction once those assemblies
exist: `Game.Common` → `Game.Data` (+ `.Authoring`) → `Game.Gameplay`, never upward. Verify against
the tree rather than trusting this list. Related: [[apexion-entities-stats-is-dots-free-fork]],
[[follow-encosy-structure-and-naming]], [[result-errors-are-polyenum-structs]].
