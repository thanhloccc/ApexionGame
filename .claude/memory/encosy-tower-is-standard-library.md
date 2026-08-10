---
name: encosy-tower-is-standard-library
description: EncosyTower (Packages/com.laicasaane.encosy-tower) is this project's standard library, not an optional dependency
metadata:
  type: project
---

EncosyTower v0.1.7-preview.3 at `Packages/com.laicasaane.encosy-tower` is the standard library for
ApexionGames: Land of Souls. The user adopted it deliberately to build real production projects on
top of it — it is not a utility grab-bag that happens to be installed.

It covers: PubSub messaging, Processing (request/response), Vaults (service locator), Pooling
(managed + native/Burst), PageFlows.MonoPages (UI screen & popup navigation), MVVM + ViewBinding,
Databases (spreadsheet → ScriptableObject via BakingSheet), Data source-gen, Persistences
(save/load), asset Keys (Addressable/Resource/Config/L10n/AtlasedSprite), Localization, Settings
(Project Settings pages), Logging, Option/Result, Ids/StringIds/TypeIds, source-generated type
tools (WrapType, UnionId, EnumExtensions, EnumTemplate, PolyEnumStruct, Variant), Collections &
Buffers, VisualDebugging (in-game cheat console), and a large editor toolbox.

**Why:** the user's goal is to apply this package to real projects, so every system built here
should route through it rather than duplicate it.

**How to apply:** load the `encosy-tower` skill (`.claude/skills/encosy-tower/`) — it holds the
decision matrix, module map, verified recipes, and this project's gate/define status. See
[[review-encosy-before-implementing]] and [[encosy-modules-live-in-project]].
