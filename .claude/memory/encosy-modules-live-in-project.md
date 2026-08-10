---
name: encosy-modules-live-in-project
description: Everything in EncosyTower compiles here except the Entities/DOTS modules — com.unity.entities is not installed
metadata:
  type: project
---

As of 2026-08-07, `com.unity.entities` is **not** in `Packages/manifest.json`. Both
`EncosyTower.Entities.Stats` (asmdef `defineConstraints: ["UNITY_ENTITIES"]`) and
`EncosyTower.Core/Entities` (`[Lookup]`, `[TypeHandle]`, baker/blob extensions) are therefore
compiled out. `ENTITY_STORE_V1` and `LATIOS_ENTITIES_1_4` sit in Player scripting defines but are
leftovers — they do not mean DOTS is active.

Everything else is live: UniTask (`UNITASK`), Addressables, Localization, Collections, Burst,
Mathematics, uGUI/TMP, BakingSheet (`BAKING_SHEET` → Databases.Authoring + Databases.Settings),
Newtonsoft JSON, FuzzySharp (`FuzzySearchAPI`).

**Why:** proposing an Entities-based EncosyTower API here would produce code that silently never
compiles, and suggesting "just add com.unity.entities" would contradict the deliberate DOTS-free
design of [[apexion-entities-stats-is-dots-free-fork]].

**How to apply:** before recommending a module, check its gate in
`.claude/skills/encosy-tower/references/setup.md`. New assemblies must copy the `versionDefines`
block from `EncosyTower.Core.asmdef`, or their `#if` guards evaluate false and the code vanishes.
