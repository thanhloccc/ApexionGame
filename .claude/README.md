# `.claude/` — what is in here

Agent configuration for this repo, committed to git so the whole team shares one version and every
rule change has history.

**If you are a person, not an agent: you do not need to read any of this.** The house rules that
apply to humans are `CODING-CONVENTIONS.md` and `AGENTS.md`, both at the repo root.

## Layout

| Path | What | Read it when |
|---|---|---|
| `CLAUDE.md` | Loaded every session. The plan-first gate, project facts, standing rules | Changing how agents behave by default |
| `project-profile.md` | **Verified fact sheet** — engine, packages, assemblies, tables, test setup. The portable skills read this to learn about this repo | A project fact changed |
| `skills/` | 13 skills. See below | Adding or changing a rule |
| `memory/` | 16 one-fact files + `MEMORY.md` index | A fact turns out to be wrong |
| `Documentation/` | Plans for the skill sets themselves, in Vietnamese | Understanding why a skill is shaped the way it is |

## The skills

**Project-specific** — they name this repo's technology directly:

| Skill | Owns |
|---|---|
| `encosy-tower` | Which library module to use · structure & naming · collections & math · error contracts · UI Toolkit · the plan-first gate |
| `unity-cli-workflow` | Driving Unity on this machine — compile, tests, builds, logs |
| `coding-standards` | How the C# is written. Owns the sections of `CODING-CONVENTIONS.md` nothing else covers, and routes the rest |

**Portable** — written project-agnostic, resolving this repo's facts through `project-profile.md`.
Copy the folders to another project and write a new profile:

| Skill | Owns |
|---|---|
| `system-design` | **Every feature.** Decomposition · state ownership · communication · rejected alternatives · performance tier |
| `refactoring` | Changing existing code without breaking it |
| `debugging` | Working out why something is broken |
| `midcore-assembly-architecture` | The assembly graph — layers, direction, cycles, compile time |
| `midcore-data-pipeline` | Content tables, ids, import validation, delivery |
| `midcore-save-migration` | Persisted data — versioning, migration, recovery |
| `midcore-testing` | What is worth testing, and what counts as verified |
| `midcore-perf-budget` | Budgets, profiling, and the evidence a performance claim needs |
| `midcore-release-pipeline` | Reproducible builds, versioning, symbols, release gates |
| `midcore-live-ops` | Remote config, events, telemetry, kill switches |

## The three layers of architecture

Deliberately split, so no two skills answer the same question differently:

| Layer | Question | Skill |
|---|---|---|
| Above | Which assembly, which layer, which direction | `midcore-assembly-architecture` |
| Middle | How this feature is shaped, who owns what state | `system-design` |
| Below | How the code is written | `coding-standards` |

## Rules with no exceptions

Each is owned by a skill; these are the ones whose violation is expensive or unrecoverable:

1. **A feature request produces a plan, not code.** Code starts on an explicit "implement".
   → `CLAUDE.md`, `encosy-tower`
2. **A shipped save format is forever.** → `midcore-save-migration`
3. **Fail the content import, not the play session.** → `midcore-data-pipeline`
4. **No performance claim without a before/after measurement.** → `midcore-perf-budget`
5. **Never fix a bug you cannot explain.** → `debugging`
6. **One change alters structure OR behaviour, never both.** → `refactoring`
7. **Every piece of state has exactly one owner.** → `system-design`
8. **Do not write comments or XML docs unless asked; never touch existing ones.**
   → `coding-standards` — this one surprises people
9. **`Result<T,TError>` never uses a flat enum.** → `encosy-tower`

## Changing something

| To change | Edit | Not |
|---|---|---|
| A fact about this repo | `project-profile.md` | a skill — portable skills must never name this repo |
| A rule | the skill that owns it | a second copy elsewhere |
| Default session behaviour | `CLAUDE.md` | — |
| C# house style | `CODING-CONVENTIONS.md` at the repo root | `coding-standards`, which points at it |

Two invariants worth preserving:

- **`grep -rE "EncosyTower|ApexionGame|Assets/Game|com\.laicasaane|Unity" .claude/skills/midcore-* .claude/skills/{system-design,refactoring,debugging}` must return nothing.** A hit means a
  repo fact leaked into a portable skill.
- **One rule, one owner.** Every skill carries a "does not own" table. When two skills could answer a
  question, one of them must defer.

## Honest status

The skills are extensive and **none of them has been used on real gameplay work yet** — `Assets/Game`
currently contains no hand-written code. Treat every rule here as a well-reasoned hypothesis until a
feature has been built through it. The first real feature should be treated partly as a test of this
set, and whatever gets in the way should be cut.
