---
name: midcore-skills-read-project-profile
description: The seven midcore-* skills are written project-agnostic and resolve this repo's facts through .claude/project-profile.md
metadata:
  type: project
---

Since 2026-08-15 `.claude/skills/` holds seven portable skills alongside `encosy-tower` and
`unity-cli-workflow`: `midcore-assembly-architecture`, `-data-pipeline`, `-save-migration`,
`-testing`, `-perf-budget`, `-release-pipeline`, `-live-ops`.

They deliberately **never name EncosyTower, `ApexionGame.*`, or a path under `Assets/Game`**. They
state rules; `.claude/project-profile.md` supplies the technology, and its `authority_skills` field
points back at `encosy-tower` (structure, naming, types, error contracts) and `unity-cli-workflow`
(compile, test, build).

**Why:** so the set survives past this title. Applying it to the studio's next project is copying the
`midcore-*` folders and writing one new profile — not forking seven skills. That only holds while the
core stays clean, which is why a leak is checkable:
`grep -rE "EncosyTower|ApexionGame|Assets/Game|com\.laicasaane" .claude/skills/midcore-*` must return
nothing.

**How to apply:** when a `midcore-*` skill says "the project's typed-id mechanism" or "the
`structure_naming` authority", read the profile and follow it to `encosy-tower` — do not add the
concrete answer into the portable skill. Repo facts change in `project-profile.md` only; values are
never copied into a skill. Fields written `unknown — ask` (device tiers, CI, live-ops backend) mean
**stop and ask** rather than invent a number — a budget an agent made up becomes project folklore
nobody remembers deciding. Related: [[gameplay-assembly-map]], [[encosy-tower-is-standard-library]],
[[unity-tooling-is-cli-not-mcp]], [[result-errors-are-polyenum-structs]].
