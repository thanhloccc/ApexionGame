# ApexionGame Core Package — Overview

*[Tiếng Việt](Core%20Package%20-%20Overview.vi.md)*

## Status

| | |
|---|---|
| Phase | **Planning — awaiting review** |
| Package id | `com.apexion.apexion-game` |
| What it owns | `ApexionGame.Core` (+ `.Samples`, `.Samples.Editor`), `ApexionGame.Editor`, `ApexionGame.Entities.Stats` (+ `.Authoring`, `.Editor`, `.Samples`) |
| What stays in `Assets/` | `ApexionGame.Tests.EditorMode` — unmoved, unmodified |
| Depends on | `com.laicasaane.encosy-tower` (embedded, no manifest entry — same pattern this package itself will use) |

---

## 1. Request summary

While building the HFSM sample playground (`ApexionGame.Core.Samples` — HFSM - Roadmap.md phase 8),
the user asked for the sample to be set up **the way EncosyTower does it**: an asmdef-per-sample
folder under a package's `Samples~/`, importable/removable from the Unity Package Manager, exactly
like `Packages/com.laicasaane.encosy-tower/Samples~/EncosyTower.Samples.*`.

That mechanism only exists for a real UPM package — a tilde folder (`Samples~/`) is invisible to
Unity and has no import button unless a `package.json` next to it declares a `"samples"` entry.
`ApexionGame.Core` today is plain project code under `Assets/ApexionGame/`, not a package, so the
literal ask requires first turning it into one.

Two scope questions were put to the user directly (`AskUserQuestion`) and answered:

| Question | Answer |
|---|---|
| Package scope | **The whole `ApexionGame.*` family** — `Core`, the shared `Editor` assembly, and `Entities.Stats` (+ its Authoring/Editor/Samples siblings) all move into one package, not just `Core`. This was the larger of the two options offered; the user chose it knowing it touches the already-tested `Entities.Stats` module (95/95 tests) as well as HFSM (phases 1–7, tests green). |
| Package id | **`com.apexion.apexion-game`** — the user's literal answer, not either of the two suggested ids. |

Because the *whole* family moves together, the narrow-scope problem flagged in the first question —
`MachineDebuggerWindow` living in the *shared* `ApexionGame.Editor` assembly, which also serves
`Entities.Stats` — disappears: `ApexionGame.Editor` does not need to be split, it moves as-is.

---

## 2. Expected output

### 2.1 What exists on disk when this is done

```
Packages/com.apexion.apexion-game/
├── package.json                              new — name, version, unity, dependencies, samples[]
├── ApexionGame.Core/                          moved from Assets/ApexionGame/ApexionGame.Core/ — unchanged
│   ├── ApexionGame.Core.asmdef
│   ├── Documentation~/                        HFSM - *.md, moved as-is
│   └── HFSM/
├── ApexionGame.Editor/                        moved from Assets/ApexionGame/ApexionGame.Editor/ — unchanged
├── ApexionGame.Entities.Stats/                moved — unchanged
├── ApexionGame.Entities.Stats.Authoring/      moved — unchanged
├── ApexionGame.Entities.Stats.Editor/         moved — unchanged
└── Samples~/                                  new — hidden from Unity until imported
    ├── ApexionGame.Core.Samples/              moved+renamed from Assets/.../ApexionGame.Core.Samples/
    │   ├── ApexionGame.Core.Samples.asmdef
    │   ├── Enemy/**
    │   ├── Scenes/hfsm-playground.unity
    │   └── Editor/                            nested — was the separate .Samples.Editor sibling
    │       ├── ApexionGame.Core.Samples.Editor.asmdef
    │       └── MachinePlaygroundEditor.cs
    └── ApexionGame.Entities.Stats.Samples/    moved+renamed from Assets/.../ApexionGame.Entities.Stats.Samples/

Assets/ApexionGame/
└── ApexionGame.Tests.EditorMode/              stays — asmdef references resolve by assembly NAME,
                                                not by path, so nothing here needs to change
```

No `.meta` files exist anywhere under `Samples~/` — Unity ignores tilde folders completely, so a
`.meta` there is dead weight. Every `.meta` that traveled with a moved *live* assembly (`Core`,
`Editor`, `Entities.Stats*`) keeps its original GUID via `git mv`, so every existing reference
(asmdef reference by name, scene/prefab reference by GUID) keeps resolving.

### 2.2 `package.json` (sketch)

```json
{
    "name": "com.apexion.apexion-game",
    "displayName": "Apexion Game",
    "version": "0.1.0",
    "unity": "6000.3",
    "dependencies": {},
    "description": "Reusable ApexionGames runtime: hierarchical state machine, DOTS-free entity stats, and the shared editor tooling for both.",
    "samples": [
        {
            "displayName": "ApexionGame.Core.Samples",
            "description": "HFSM playground: EnemyBrain demo, ten scenario buttons, hand-authored scene.",
            "path": "Samples~/ApexionGame.Core.Samples"
        },
        {
            "displayName": "ApexionGame.Entities.Stats.Samples",
            "description": "RTS stats demo.",
            "path": "Samples~/ApexionGame.Entities.Stats.Samples"
        }
    ]
}
```

`dependencies` is left empty rather than listing `com.laicasaane.encosy-tower`: that package is
itself embedded with no version the registry resolver can check, and `Packages/manifest.json`
already carries it — verify empirically during execution (step 5) whether Unity's resolver accepts
a same-shaped entry here or flags a warning; either way it does not block compilation.

### 2.3 The checks that prove it works

| # | Check | How it is run |
|---|---|---|
| C1 | The package appears in Package Manager as an embedded package, no `Packages/manifest.json` edit needed | open Package Manager → In Project, or `unity command … package_list` |
| C2 | Whole project compiles, 0 error 0 warning | `unity command … recompile` + `recompile_status` |
| C3 | `ApexionGame.Tests.EditorMode` — HFSM's ~90 tests and Stats' 95 tests — still pass, **unmoved**, referencing the package purely by assembly name | `unity test . --mode EditMode` |
| C4 | Package Manager's **Samples** tab lists both samples with a working **Import** button, each importing into `Assets/Samples/Apexion Game/<version>/<name>/` | manual, in the Editor |
| C5 | `ApexionGame > HFSM > Debugger` and the Stats debugger window still open and work after `ApexionGame.Editor` moves | manual |
| C6 | The imported HFSM sample's scene (`hfsm-playground.unity`) opens and all ten buttons still run, from the imported copy — not the original package folder | manual, after C4 |

---

## 3. Steps

Dependency-ordered. Every move is a **directory-level `git mv`**, never a recreate-from-scratch —
that is what preserves every `.meta` GUID and keeps existing references resolving.

| # | Action | Done when | Files/paths |
|---|---|---|---|
| 1 | `git mv Assets/ApexionGame/ApexionGame.Core Packages/com.apexion.apexion-game/ApexionGame.Core` (and its `.meta`) | folder exists at the new path with history intact | `ApexionGame.Core*` |
| 2 | `git mv Assets/ApexionGame/ApexionGame.Editor Packages/com.apexion.apexion-game/ApexionGame.Editor` (and its `.meta`) | same | `ApexionGame.Editor*` |
| 3 | `git mv` each of `ApexionGame.Entities.Stats`, `.Authoring`, `.Editor` into the package root | same | `ApexionGame.Entities.Stats*` (not `.Samples`) |
| 4 | Create `Packages/com.apexion.apexion-game/package.json` per §2.2 | file exists, valid JSON | `package.json` |
| 5 | Verify `Packages/manifest.json` needs no entry (embedded-package auto-discovery, same as `com.laicasaane.encosy-tower` today) — add one only if Unity's Package Manager does not list it | `unity command … package_list` shows the new package with no manifest edit, or the minimal edit is recorded here | `Packages/manifest.json` (probably untouched) |
| 6 | `git mv Assets/ApexionGame/ApexionGame.Core.Samples Packages/com.apexion.apexion-game/Samples~/ApexionGame.Core.Samples`, then `git mv .../ApexionGame.Core.Samples.Editor` **into** that folder as `Editor/` (nested asmdef, not a sibling) | files sit under `Samples~/`, editor asmdef nested one level down | `ApexionGame.Core.Samples*` |
| 7 | Delete every `.meta` file under the new `Samples~/ApexionGame.Core.Samples/` tree (including the folder metas) — tilde folders carry none | `git status` shows only deletions of `.meta` files under that tree | `Samples~/ApexionGame.Core.Samples/**/*.meta` |
| 8 | `git mv Assets/ApexionGame/ApexionGame.Entities.Stats.Samples Packages/com.apexion.apexion-game/Samples~/ApexionGame.Entities.Stats.Samples`, then delete its `.meta` files the same way as step 7 | same pattern as step 6–7 | `ApexionGame.Entities.Stats.Samples*` |
| 9 | Confirm `Assets/ApexionGame/ApexionGame.Tests.EditorMode/ApexionGame.Core.Tests.asmdef`'s `"references"` array still says `"ApexionGame.Core"` / `"ApexionGame.Entities.Stats"` (by name) — expect **no edit needed** | asmdef diff is empty | `ApexionGame.Tests.EditorMode/**/*.asmdef` |
| 10 | Compile through the live Editor (already open — attach via Pipeline, do not launch a competing batch process) | C2 holds | — |
| 11 | Run `ApexionGame.Tests.EditorMode` EditMode suite | C3 holds | — |
| 12 | Open Package Manager → In Project → Apexion Game → Samples tab; import both samples into a scratch check, confirm C4–C6 | C4, C5, C6 hold | — |
| 13 | Move the two module `Documentation~/` sets' cross-links if any reference the old `Assets/ApexionGame/...` paths (check `HFSM - *.md`'s Layout §2 assembly-location table, which currently says `Assets/ApexionGame/ApexionGame.Core/`) | doc paths match the new tree | `ApexionGame.Core/Documentation~/HFSM - Layout.md` and its `.vi.md` |

---

## 4. Goals and non-goals

### 4.1 Goals

| | |
|---|---|
| G1 | The HFSM sample (and the Stats sample) become real Package Manager samples — import/remove from Package Manager, exactly like `EncosyTower.Samples.*`. |
| G2 | `ApexionGame.Tests.EditorMode` keeps passing with **zero code changes** — proof that asmdef-by-name resolution makes the move safe. |
| G3 | Every GUID survives the move (`git mv`, not recreate), so no scene/asmdef/prefab reference breaks. |

### 4.2 Non-goals

| | Why |
|---|---|
| Publishing to a UPM registry / git URL install | Not asked for; this is a *local, embedded* package, same tier as `com.laicasaane.encosy-tower` is today. |
| Splitting `ApexionGame.Editor` per module | Explicitly rejected — the user chose to move the whole shared assembly rather than carve HFSM's debugger out of it. |
| A CHANGELOG.md / semver discipline for the new package | Not asked for; `version: 0.1.0` is a placeholder, not a commitment. |

---

## 5. Decisions

- **Package id is `com.apexion.apexion-game`**, the user's literal answer — not `com.apexiongames.core`
  or `com.apexiongame.core` as offered. Note the id is `apexion-game` (hyphenated), distinct from the
  `ApexionGame` (no hyphen) used throughout every existing assembly/namespace name; nothing renames the
  C# side, only the package folder/id is affected.
- **Whole `ApexionGame.*` family in one package**, not `Core` alone — chosen over the narrower,
  lower-risk option after the trade-off (touches `Entities.Stats` too) was stated plainly. As a
  direct consequence, `ApexionGame.Editor` does not need splitting.
- **`ApexionGame.Tests.EditorMode` stays in `Assets/`** — it is a project-level test assembly that
  already hosts both HFSM's and Stats' tests side by side; asmdef references resolve by assembly
  name regardless of whether the referenced assembly lives in `Assets/` or `Packages/`, so it needs
  no path or reference changes, only a post-move test run to confirm.
- **Samples~ folder names match their asmdef names** (`Samples~/ApexionGame.Core.Samples/`,
  `Samples~/ApexionGame.Entities.Stats.Samples/`), mirroring `EncosyTower.Samples.*` exactly.
  `ApexionGame.Core.Samples.Editor` nests as `Samples~/ApexionGame.Core.Samples/Editor/` rather than
  a sibling `Samples~` entry, so importing the one sample brings its editor buttons along in the same
  copy — nested asmdefs inside an imported sample folder are copied and compiled together.

---

## 6. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | A recreate-instead-of-move breaks every GUID reference silently. | Every step is a directory-level `git mv`; no file gets rewritten from scratch. |
| R2 | Stale `.meta` files left under `Samples~/` confuse a future contributor into thinking they matter. | Step 7/8 deletes them explicitly, not just "does not add new ones." |
| R3 | `EncosyTower.Core.Extended`, used by `MachineOverlayCommand` inside the HFSM sample, has no listed dependency once this package is used in a *different* project. | Deferred: `package.json`'s `dependencies` is left empty per §2.2 pending the manifest check in step 5; revisit if the sample is ever imported into a project that lacks EncosyTower. |
| R4 | The live Editor is already open on this exact project (verified: PID 20572). A competing batch-mode Unity process must not be launched during the move. | Attach via Pipeline commands (`recompile`, `recompile_status`) as already established for this session; do not shell out to `Unity.exe -batchmode` against this project path. |
