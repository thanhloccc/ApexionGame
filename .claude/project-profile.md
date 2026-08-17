# Project profile — ApexionGames: Land of Souls

The overlay read by the portable `midcore-*` skills. They deliberately name no vendor, package or
path; this file supplies them.

**Rules for this file**

- Every value is **verified against the repo**, not assumed. Last verified: **2026-08-15**.
- A field with no verifiable value is written `unknown — ask`. A skill that needs one **stops and
  asks** rather than inventing a number.
- When a fact changes, change it here. Do not copy values into a skill.

```yaml
project:           ApexionGames — Land of Souls
genre:             single-player action RPG (weapons/ammo, equipment, archetype+level progression,
                   skill tree, status effects, chaptered quest line)
engine:            Unity 6000.3.20f1          # ProjectSettings/ProjectVersion.txt
standard_library:  EncosyTower 0.1.7-preview.3 # Packages/com.laicasaane.encosy-tower

authority_skills:
  structure_naming: encosy-tower       # modules, files, folders, namespaces, collections,
                                       # math types, Result/error contracts, UI Toolkit
  unity_operations: unity-cli-workflow  # compile, EditMode/PlayMode tests, builds, Editor control
  code_standards:   coding-standards    # formatting, API design, attributes, comments, async
                                        # owns the unowned sections of CODING-CONVENTIONS.md
  feature_design:   system-design       # decomposition, state ownership, communication,
                                        # tradeoffs, performance tier — gate on EVERY feature

package_roots:
  - Packages/com.laicasaane.encosy-tower          # standard library
  - Packages/com.laicasaane.encosy-tower.dev-tools # package dev tooling; game code must not reference
  - Packages/com.apexion.apexion-game             # first-party: HFSM + DOTS-free stats fork

# ─────────────────────────────────────────────────────────────────────────────
assembly_map:
  reusable_code:  Packages/com.apexion.apexion-game   # anything that outlives this title
  game_code:      Assets/Game                          # title-specific only
  split_rule:     "game-specific → Assets/Game; reusable past this title → the package"

  existing:
    - ApexionGame.Core                                  # HFSM state machine (ApexionGame.HFSM)
    - ApexionGame.Editor
    - ApexionGame.Entities.Stats (+ .Authoring, .Editor)
    - ApexionGame.Entities.Stats.Samples.Rts            # one folder, two asmdefs: Rts.Core, Rts.Game
    - ApexionGame.Tests.EditorMode
    - Game.Input                                        # at Assets/Game/Input/, not Assets/Game/Game.Input/

  game_assemblies:
    status:    "EXIST as of 2026-08-15 — created by the Equipment System feature"
    - Game.Common          # ids, enums, CharacterStats/GameStatSystem. unsafe=true (StatCollection)
    - Game.Data            # ItemData base table + 4 extension tables + GameDatabase
    - Game.Gameplay        # ItemCatalog, EquipmentRules, Loadout, EquipmentStatBinding
    - Game.Gameplay.Tests  # 29 EditMode tests, green
    still_empty: [Game.Data.Authoring]
    direction: "Game.Common → Game.Data (+ .Authoring) → Game.Gameplay. Never upward."
    first_file_rule: "creating the first file in one of those folders also creates its .asmdef:
                      name = rootNamespace = folder name, referencing EncosyTower.Core, with the
                      versionDefines block copied from EncosyTower.Core.asmdef — omit it and #if
                      guards silently evaluate false"

  exceptions_to_match_not_fix:
    - "PACKAGE tests are centralised in ApexionGame.Tests.EditorMode. Game-code tests cannot be —
       see testing.convention"
    - "an assembly declaring [StatCollection] needs allowUnsafeCode: true; the generated code is
       unsafe. Game.Common carries it for that reason"
    - "every assembly needs its own csc.rsp with -langversion:10, or file-scoped namespaces and
       other C#10 features fail with CS8773"
    - "Samples.Rts is a grouping folder holding two asmdefs; folder-name = assembly-name holds at
       the asmdef's own folder, not the grouping folder above it"

# ─────────────────────────────────────────────────────────────────────────────
data_pipeline:
  technology:      EncosyTower Databases + com.laicasaane.bakingsheet 6.3.0-pre.1
  gate:            "BAKING_SHEET && UNITY_NEWTONSOFT_JSON; authoring is editor-only"
  naming_policy:   NameCasing.SnakeLower        # sheets, columns, generated assets
  asset_root:      Assets/Game/Addressables/assets-shared/database/
  container:       game_database_asset
  tables:                          # item-related tables exist in code as of 2026-08-15
    - item                         # BASE table: id, displayKey, weight, slotMask, maxCopies, modifiers
    - equipment                    # extension: class, armorRating
    - melee_weapon                 # extension: damage, arc, windUp/active/recovery
    - ranged_weapon                # extension: ammo, fireMode, rpm, magazine, burst, reload, projectile
    - ammo                         # extension: damageMultiplier, armorPenetration, stackMax
  table_shape:     "base `item` table + per-kind extension tables joined by ItemId. One rule change
                    touches one table instead of four."
  planned_tables:  [player_archetype, player_level, player_skill, player_status, quest, quest_chapter]
  stale_assets:    "ALL 11 .asset files under asset_root are ORPHANED — their m_Script GUIDs belong
                    to deleted scripts, so they never load and reflect no live schema. Awaiting a
                    deliberate cleanup decision; do not treat them as the schema of anything."
  delivery:        com.unity.addressables 2.9.1
  localization:    com.unity.localization 1.5.12
  id_convention:   "typed id wrappers, never raw string/int — see the structure_naming authority"
  import_ui:       "EncosyTower.Databases.Settings window (Google Sheets / Excel / CSV / JSON)"
  validation:      unknown — ask   # no import-time validation rules are wired yet

# ─────────────────────────────────────────────────────────────────────────────
save:
  technology:   EncosyTower Persistences
  encryption:   available (EncryptionBase / AesEncryption); not yet configured
  status:       "NO shipped save format yet. Versioning rules apply from the very first one —
                 this is the cheapest moment in the project's life to get them right."
  shipped_versions: []              # golden fixtures are kept per entry, forever

# ─────────────────────────────────────────────────────────────────────────────
performance:
  device_tiers: unknown — ask
  # No target device, frame budget or memory ceiling has been decided. Do NOT quote a number.
  # Ask for: min-spec / mid / high reference devices, target fps, memory ceiling.
  available_tooling:
    - Unity Profiler
    - com.unity.burst 1.8.29, com.unity.collections 2.6.8, com.unity.mathematics 1.3.3
  standing_defaults: "collection and math type preferences are owned by the structure_naming
                      authority, not by the perf skill"

# ─────────────────────────────────────────────────────────────────────────────
testing:
  assemblies:   [ApexionGame.Tests.EditorMode, Game.Gameplay.Tests]
  convention:   "package tests -> ApexionGame.Tests.EditorMode; GAME-code tests ->
                 Assets/Game/<Assembly>.Tests. A package assembly CANNOT reference Assets/, so
                 game tests cannot be centralised there — technical constraint, not preference
                 (DEC-004, 2026-08-15)."
  playmode:     "none — do NOT imply PlayMode coverage exists"
  framework:    com.unity.test-framework 1.6.0
  runner:       "unity test . --mode EditMode --non-interactive --filter <suite> --output <path>"
  runner_owner: unity-cli-workflow
  reference:    "ApexionGame.Tests.EditorMode/ApexionGame.Core/HFSM/MachineErrorTests.cs"

# ─────────────────────────────────────────────────────────────────────────────
release:
  ci:            unknown — ask
  platforms:     unknown — ask
  build_command: "unity build (see unity-cli-workflow)"
  editor:        6000.3.20f1 installed; 2022.3.62f3, 6000.0.75f1, 6000.3.17f1 also present
  generated:     "root .csproj / .slnx files and Library/, Temp/, Logs/ are generated — never
                  source of truth, never edited"

# ─────────────────────────────────────────────────────────────────────────────
live_ops:
  backend:   none installed
  # Verified against Packages/manifest.json: no analytics, remote-config, or purchasing package.
  # Choosing one is a package decision the user has not made. Surface it; never assume a vendor.
  in_scope:      [remote config, events/seasons, A/B, telemetry, kill switches]
  out_of_scope:  [monetisation, IAP, receipt validation, store compliance]

# ─────────────────────────────────────────────────────────────────────────────
conventions:
  style_guide:   CODING-CONVENTIONS.md (repo root)
  docs:          "Documentation~/ of the owning assembly; <Topic> - <Aspect>.md + .vi.md mirrors.
                  Model set: Packages/com.apexion.apexion-game/ApexionGame.Core/Documentation~/"
  agent_policy:  AGENTS.md (repo root) — Unity MCP is deliberately not used
  source_gen:    "EncosyTower attributes are source generators: the type must be `partial`, and
                  generated members only exist after a Unity compile. IDE analysis proves nothing."
```

## Fields that need an answer

| Field | Why it is blocking | What to ask for |
|---|---|---|
| `performance.device_tiers` | No frame or memory budget can be quoted, so no performance claim can be judged | min-spec / mid / high reference devices, target fps, memory ceiling |
| `release.ci` + `release.platforms` | The release gate order cannot be made concrete | is there a CI system; which platforms ship |
| `data_pipeline.validation` | Import-time validation rules are the main defence for content at this scale | which invariants must fail an import vs warn |
| `live_ops.backend` | Every remote-config recipe stays abstract until a backend exists | is a backend already decided but not yet installed |
