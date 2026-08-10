---
name: apexion-entities-stats-is-dots-free-fork
description: Assets/ApexionGame/ApexionGame.Entities.Stats is a deliberate DOTS-free fork of EncosyTower.Entities.Stats — never add Unity.Entities to it
metadata:
  type: project
---

`Assets/ApexionGame/ApexionGame.Entities.Stats` is a fork of the package's
`EncosyTower.Entities.Stats`, rewritten to run **without Unity.Entities**. Its asmdef references
only `EncosyTower.Core`, Burst, Collections and Mathematics.

Where the package stores stats in ECS chunks, the fork uses `StatBuffer<T>` / `StatBufferLookup<T>`
/ `StatOwnerHandle` / `StatOwnerSlot` under `Storage/`, and adds `Debugging/` (`StatDebugInfo`,
`StatDebugRegistry`, `StatStoreDebug`) plus `StatTypeTable` / `StatTypeTableGenerator`. Its
codegen define is `APEXION_STAT_VALUE_TYPES_GENERATOR` (the package's is
`ENCOSY_STAT_VALUE_TYPES_GENERATOR`). Companions: `.Authoring`, `.Editor`, `.Tests`, and
`.Samples/Rts.{Core,Game,Tests}`.

**Why:** the divergence is intentional. Treating it as drift — "syncing" it back to the package
version, or adding `com.unity.entities` so the package assembly compiles — would undo the port.

**How to apply:** when stats work comes up, edit the fork under `Assets/ApexionGame/`, not the
package. Do not propose installing Unity.Entities to "fix" it. See
[[encosy-modules-live-in-project]].
