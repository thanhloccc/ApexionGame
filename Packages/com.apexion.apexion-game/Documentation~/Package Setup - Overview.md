# Package Setup — Overview

## Request summary

Turn `Assets/ApexionGame/*` into a proper embedded UPM package, `Packages/com.apexion.apexion-game`,
structured the same way `Packages/com.laicasaane.encosy-tower` is structured in the EncosyTower repo
(`C:\Users\ADMIN\Documents\Github\EncosyTower`). `Assets/Game/*` (the game itself — currently only
`Game.Input` has content; `Game.Common`/`Game.Data`/`Game.Data.Authoring`/`Game.Gameplay`/
`Game.Gameplay.Editor`/`Game.Gameplay.Tests` are empty folders) stays where it is and becomes a
consumer of both `com.laicasaane.encosy-tower` and the new `com.apexion.apexion-game` package.

## Expected output

- A new embedded package at `Packages/com.apexion.apexion-game/` with a `package.json`
  (`name: com.apexion.apexion-game`, `displayName: Apexion Game`, `version: 0.1.0`), a real
  `README.md`, and a fresh `CHANGELOG.md` (Keep-a-Changelog, starting at `[Unreleased]`). No
  `LICENSE.md` — this package is proprietary, unlike EncosyTower (MIT).
- Every assembly currently under `Assets/ApexionGame/` relocated one-for-one under
  `Packages/com.apexion.apexion-game/`, `.meta` files carried so GUIDs (and therefore every scene/
  prefab/asset reference into these assemblies) survive the move untouched.
- `ApexionGame.Core.Samples(.Editor)` (the HFSM playground, no test dependency on it) converted into
  an opt-in `Samples~/` folder declared in `package.json`'s `samples` array — matching EncosyTower's
  convention.
- `ApexionGame.Entities.Stats.Samples/Rts.*` (the RTS reference implementation) **stays a live,
  always-compiled module** at the package root, not opt-in — discovered mid-implementation that
  `ApexionGame.Tests.EditorMode` hard-references `ApexionGame.Entities.Stats.Samples.Rts.Core` for
  regression tests (`RtsEffectTests`, `RtsMatchFlowTests`, …); Unity never compiles anything under a
  `Samples~/` folder, so making it opt-in would have broken the whole project's compile. See
  Decisions.
- `Assets/ApexionGame/` deleted once empty.
- `Packages/manifest.json` gets an explicit `"com.apexion.apexion-game": "file:com.apexion.apexion-game"`
  entry, and the two pre-existing gaps for `com.laicasaane.encosy-tower` and
  `com.laicasaane.encosy-tower.dev-tools` (embedded and working via `packages-lock.json`, but absent
  from `manifest.json`'s `dependencies`) get fixed at the same time.
- Every doc/skill/memory file that hard-codes the old `Assets/ApexionGame/...` paths repointed to the
  new `Packages/com.apexion.apexion-game/...` paths.
- Project opens in Unity with zero new console errors, Package Manager lists "Apexion Game" as an
  embedded package, and the one opt-in sample imports cleanly from the Package Manager UI.

## Steps

| # | Action | Files touched | Done condition |
|---|---|---|---|
| 1 | Create package manifest | `Packages/com.apexion.apexion-game/package.json` (new) | Valid JSON per [Package manifest](#package-manifest) below |
| 2 | Create package README | `Packages/com.apexion.apexion-game/README.md` (new) | Short real doc: name, purpose, module list, link to `Documentation~/` |
| 3 | Create package changelog | `Packages/com.apexion.apexion-game/CHANGELOG.md` (new) | Keep-a-Changelog header + `## [Unreleased]` |
| 4 | Move Core module | `Assets/ApexionGame/ApexionGame.Core/**` → `Packages/com.apexion.apexion-game/ApexionGame.Core/**` (incl. its `Documentation~/HFSM - *.md`) | `git mv`, `.meta` carried, folder gone from `Assets/` |
| 5 | Move Editor module | `Assets/ApexionGame/ApexionGame.Editor/**` → `Packages/com.apexion.apexion-game/ApexionGame.Editor/**` | same |
| 6 | Move Entities.Stats module | `Assets/ApexionGame/ApexionGame.Entities.Stats/**` → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats/**` (incl. its `Documentation~/01-OVERVIEW.md`…`07-DECISIONS.md` + `guide/`) | same |
| 7 | Move Entities.Stats.Authoring module | `Assets/ApexionGame/ApexionGame.Entities.Stats.Authoring/**` → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Authoring/**` | same |
| 8 | Move Entities.Stats.Editor module | `Assets/ApexionGame/ApexionGame.Entities.Stats.Editor/**` → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Editor/**` | same |
| 9 | Move Tests module | `Assets/ApexionGame/ApexionGame.Tests.EditorMode/**` → `Packages/com.apexion.apexion-game/ApexionGame.Tests.EditorMode/**` | same |
| 10 | Move package-level docs | `Assets/ApexionGame/Documentation~/Core Package - Overview*.md` → `Packages/com.apexion.apexion-game/Documentation~/Core Package - Overview*.md` | same |
| 11 | Fold Core samples into one `Samples~` folder | `Assets/ApexionGame/ApexionGame.Core.Samples/**` + `ApexionGame.Core.Samples.Editor/**` → `Packages/com.apexion.apexion-game/Samples~/ApexionGame.Core.Samples/**` (Editor asmdef nested as a subfolder, same relative shape as today) | Both asmdefs present under the one `Samples~` folder; folder name has no trailing `~` conflicts |
| 12 | Move RTS stats module (renamed, kept live — **not** `Samples~`) | `Assets/ApexionGame/ApexionGame.Entities.Stats.Samples/**` (incl. `Documentation~/RTS-BATTLE-DESIGN.vi.md`) → `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Samples.Rts/**` (`Rts.Core/`, `Rts.Game/` kept as nested subfolders) | Both asmdefs present, folder sits at the package root (not under `Samples~/`) |
| 13 | Declare the one opt-in sample in the manifest | `Packages/com.apexion.apexion-game/package.json` | `samples` array has exactly 1 entry, from step 11 |
| 14 | Delete the now-empty source folder | `Assets/ApexionGame/` (+ `.meta`) | Folder no longer exists |
| 15 | Wire the manifest | `Packages/manifest.json` | 3 `"file:"` entries present: `com.apexion.apexion-game`, `com.laicasaane.encosy-tower`, `com.laicasaane.encosy-tower.dev-tools` |
| 16 | Repoint hard-coded old paths | See [Files referencing the old path](#files-referencing-the-old-path) | No remaining `Assets/ApexionGame` hits for the moved assemblies (`grep` clean, package-internal `Documentation~` mentions excluded) |
| 17 | Verify in Unity | — | Project opens with no new compile errors; Package Manager → In Project shows "Apexion Game" (embedded); both samples import without error |

Steps 4–12 must run through `unity-cli-workflow` (or Unity closed) so `.meta` files move atomically
with their assets — a plain filesystem move without the `.meta` breaks every GUID reference.

## Status

Planned — not started. Awaiting explicit "implement" before any file is touched.

## Goals / Non-goals

**Goals**
- Package `Assets/ApexionGame/*` as a standalone, EncosyTower-shaped embedded UPM package.
- Preserve every asset GUID (scenes/prefabs referencing HFSM or Entities.Stats types keep working).
- Make the HFSM sample surface genuinely opt-in, like EncosyTower's samples — the RTS one stays live
  because it has a real regression-test dependency (see Decisions).
- Fix the pre-existing `manifest.json` / `packages-lock.json` mismatch for all three embedded packages while touching this file.

**Non-goals**
- Restoring or designing the empty `Assets/Game/Game.Common` / `Game.Data` / `Game.Gameplay*`
  folders — out of scope; flagged separately, not blocking this migration.
- Rewriting the repo-root `CHANGELOG.md` / `CODING-CONVENTIONS.md` / `.editorconfig`, which are still
  byte-identical leftovers from the EncosyTower template this repo started from — untouched here.
- Renaming any assembly, namespace, or type. `ApexionGame.Core`, `ApexionGame.Entities.Stats`, etc.
  keep their current names; only their folder resides under `Packages/` now.
- Publishing the package externally (no `LICENSE.md`, no OpenUPM/git-URL install instructions —
  this is an internal embedded package only).
- Fixing `ApexionGame.Entities.Stats.Samples.Rts.Core`'s `rootNamespace` inconsistency (missing
  `.Core` suffix) — pre-existing, orthogonal to this move, left as-is.

## Encosy module mapping

None — this is a pure repo/tooling restructuring, not a feature consuming EncosyTower runtime APIs.
The only "module" involved is EncosyTower's own **package-authoring convention** (embedded package
layout, `versionDefines`-gated optional refs, `Samples~` + `package.json.samples`), which this plan
copies structurally rather than reimplementing — see the `encosy-tower` skill's `setup` reference for
the authoritative version of that convention.

## Data model

Not applicable — no new data types are introduced by this migration.

## Exact folder / namespace / file layout

```
Packages/com.apexion.apexion-game/
├── package.json
├── README.md
├── CHANGELOG.md
├── Documentation~/
│   ├── Core Package - Overview.md
│   └── Core Package - Overview.vi.md
├── ApexionGame.Core/                              (namespace ApexionGame.Core, unchanged)
│   ├── ApexionGame.Core.asmdef
│   └── Documentation~/HFSM - {Layout,Overview}.{md,vi.md}
├── ApexionGame.Editor/                             (namespace ApexionGame.Editor, unchanged)
│   └── ApexionGame.Editor.asmdef
├── ApexionGame.Entities.Stats/                     (namespace ApexionGame.Entities.Stats, unchanged)
│   ├── ApexionGame.Entities.Stats.asmdef
│   └── Documentation~/{01-OVERVIEW..07-DECISIONS}.md, guide/
├── ApexionGame.Entities.Stats.Authoring/
│   └── ApexionGame.Entities.Stats.Authoring.asmdef
├── ApexionGame.Entities.Stats.Editor/
│   └── ApexionGame.Entities.Stats.Editor.asmdef
├── ApexionGame.Tests.EditorMode/
│   └── ApexionGame.Tests.EditorMode.asmdef
├── ApexionGame.Entities.Stats.Samples.Rts/         (live module, NOT under Samples~ — see Decisions)
│   ├── Rts.Core/ApexionGame.Entities.Stats.Samples.Rts.Core.asmdef
│   ├── Rts.Game/ApexionGame.Entities.Stats.Samples.Rts.Game.asmdef
│   └── Documentation~/RTS-BATTLE-DESIGN.vi.md
└── Samples~/
    └── ApexionGame.Core.Samples/
        ├── ApexionGame.Core.Samples.asmdef
        └── Editor/ApexionGame.Core.Samples.Editor.asmdef
```

Nothing under `Assets/` changes name or namespace; `Assets/ApexionGame/` simply ceases to exist once
every subfolder above has moved out of it.

### Package manifest

```json
{
  "name": "com.apexion.apexion-game",
  "displayName": "Apexion Game",
  "version": "0.1.0",
  "unity": "6000.3",
  "dependencies": {
    "com.laicasaane.encosy-tower": "0.1.7-preview.3",
    "com.unity.burst": "1.8.29",
    "com.unity.collections": "2.6.8",
    "com.unity.mathematics": "1.3.3"
  },
  "description": "Apexion Games' shared engine layer built on EncosyTower: HFSM core utilities and a DOTS-free fork of Entities.Stats.",
  "keywords": ["framework", "game", "apexion"],
  "samples": [
    {
      "displayName": "ApexionGame.Core.Samples",
      "description": "HFSM usage sample",
      "path": "Samples~/ApexionGame.Core.Samples"
    }
  ]
}
```

`com.laicasaane.encosy-tower` is a real `package.json` dependency (not just a `versionDefines` gate)
because `ApexionGame.Core.asmdef` and `ApexionGame.Entities.Stats.asmdef` hard-reference
`EncosyTower.Core` unconditionally. `com.unity.burst/collections/mathematics` are likewise hard
references in those same asmdefs, mirroring exactly how `EncosyTower.Core`'s own `package.json` lists
only its 3 truly mandatory Unity packages while gating everything optional through `versionDefines`.

### `Packages/manifest.json` — entries to add

```json
"com.apexion.apexion-game": "file:com.apexion.apexion-game",
"com.laicasaane.encosy-tower": "file:com.laicasaane.encosy-tower",
"com.laicasaane.encosy-tower.dev-tools": "file:com.laicasaane.encosy-tower.dev-tools"
```

### Files referencing the old path

Grep for `Assets/ApexionGame` across `*.md` currently hits, besides the package's own
`Documentation~/*` (which move with their assemblies per steps 4/6/10 and need no edit beyond that):

- `.claude/CLAUDE.md` — "Project facts" bullet naming `Assets/ApexionGame/ApexionGame.Entities.Stats`
  as the DOTS-free fork; "Where things live" bullet citing
  `Assets/ApexionGame/ApexionGame.Entities.Stats/Documentation~/`.
- `.claude/skills/encosy-tower/references/setup.md`
- `.claude/skills/encosy-tower/references/module-map.md`
- `.claude/skills/encosy-tower/references/structure-and-naming.md`
- `.claude/skills/encosy-tower/references/ui-toolkit.md`
- `.claude/memory/apexion-entities-stats-is-dots-free-fork.md`
- `.claude/memory/gameplay-assembly-map.md`
- `.claude/memory/ui-is-csharp-not-uxml.md`
- `.codex/skills/encosy-tower/references/project-setup.md`
- `.codex/memory/MEMORY.md`

Each hit gets its `Assets/ApexionGame/...` path replaced with the equivalent
`Packages/com.apexion.apexion-game/...` path; prose around it is otherwise left alone.

## API surface

None — no public API changes. Assembly names, namespaces, and type signatures are unchanged; only
their filesystem location and packaging metadata change.

## Decisions

- **Scope**: only `Assets/ApexionGame/*` (the reusable framework layer) becomes the package.
  `Assets/Game/*` (the actual game, currently only `Game.Input` has content) stays under `Assets/`
  and consumes both `com.laicasaane.encosy-tower` and `com.apexion.apexion-game` — mirroring exactly
  how the EncosyTower repo keeps a thin `Assets/` host project around its own embedded package.
- **Samples — split outcome, discovered mid-implementation**: `ApexionGame.Core.Samples(.Editor)`
  (HFSM playground) converted to an opt-in `Samples~/` folder as planned. `ApexionGame.Entities.Stats.
  Samples.Rts` (`Rts.Core`, `Rts.Game`) was **not** — moving it under `Samples~/` made
  `ApexionGame.Tests.EditorMode` fail to resolve its `ApexionGame.Entities.Stats.Samples.Rts.Core`
  reference, because Unity does not compile anything under a `Samples~/` folder and several tests
  (`RtsEffectTests`, `RtsGraphInvariantTests`, `RtsLayeringTests`, `RtsMatchFlowTests`) use its types
  directly. It stays a live, always-compiled module at the package root instead — correct for
  reference/regression-tested code, even though it started out looking like "just a sample."
- **Manifest hygiene**: fixing the missing `file:` entries for `com.laicasaane.encosy-tower` and
  `.dev-tools` bundled into this same change, since it's the same class of gap the new package would
  otherwise repeat.
- **No `LICENSE.md`**: this package isn't published externally, unlike EncosyTower's MIT-licensed
  package — a license file would be misleading noise.
- **`README.md`/`CHANGELOG.md` are real, not EncosyTower-style stubs**: EncosyTower's package-internal
  README/CHANGELOG are placeholders redirecting to the repo root because the repo root already carries
  the authoritative docs. This repo's root `README.md` doesn't exist yet and its root `CHANGELOG.md` is
  still an unmodified EncosyTower leftover — so deferring to root here would point at nothing useful.
  The package ships its own real minimal docs instead; root-level docs are a separate, later concern
  (see Non-goals).
- **Versioning starts at `0.1.0`**, plain semver (no `-preview.N`) — this package isn't going through
  a public preview channel.
