# Memory index — ApexionGames: Land of Souls

- [Write the feature doc before any code](write-feature-doc-before-code.md) — hard gate; code waits for an explicit "implement".
- [Planning advice from EncosyTower's author](planning-advice-from-encosy-author.md) — load skills, ask to clarify, plan must be autonomously executable.
- [EncosyTower is the project's standard library](encosy-tower-is-standard-library.md) — check it before hand-rolling any system.
- [Always review EncosyTower before implementing](review-encosy-before-implementing.md) — the user's standing instruction, and what "review" means here.
- [`Result` errors are PolyEnumStructs, never flat enums](result-errors-are-polyenum-structs.md) — non-negotiable; the payload is the whole point.
- [Prefer high-performance collections and Unity.Mathematics](prefer-high-performance-collections-and-math.md) — FasterList/ArrayMap/Shared\*/Native over BCL, `math.*` over `Mathf`.
- [Follow EncosyTower's folder structure and naming](follow-encosy-structure-and-naming.md) — sub-folders do NOT add namespace segments; assembly = feature boundary.
- [Unity tooling is the CLI, not MCP](unity-tooling-is-cli-not-mcp.md) — Unity CLI + Pipeline; `unity-mcp-skill` is deliberately out of use.
- [Which EncosyTower modules are live in this project](encosy-modules-live-in-project.md) — Entities/DOTS ones are compiled out; everything else is on.
- [ApexionGame.Entities.Stats is a DOTS-free fork](apexion-entities-stats-is-dots-free-fork.md) — do not "fix" it by adding Unity.Entities.
- [Gameplay assembly map](gameplay-assembly-map.md) — Game.Common → Game.Data → Game.Gameplay; no longer greenfield, read the neighbouring system first.
- [House style is CODING-CONVENTIONS.md](house-style-is-encosy-conventions.md) — EncosyTower's own guide governs all C# here.
- [EncosyTower source generators require `partial`](encosy-sourcegen-requires-partial.md) — the #1 cause of "generated method not found".
- [UI is C# VisualElements, never UXML](ui-is-csharp-not-uxml.md) — 27 `.uss` vs 1 `.uxml`; the reason is recorded in RtsWidgets.
