---
name: midcore-testing
description: REQUIRED when deciding what to test, writing tests, or judging whether a change is actually verified in a game project. Covers the test tiers that pay off in games specifically — content-table validation, save migration, deterministic logic and golden runs, hot-path benchmarks — versus the ones that mostly consume time, plus determinism, fixtures, flakiness, and which tiers must pass before a change counts as done. Load it when adding tests, when asked whether something is tested enough, and before claiming any change is verified. Games invert the usual testing advice: the highest-value tests here assert on content and persisted data, not on classes. Triggers on "test", "unit test", "integration test", "coverage", "golden", "fixture", "benchmark", "regression", "verify", "verified", "flaky", "test plan", "is this tested", and Vietnamese phrasing "viết test", "kiểm thử", "test đủ chưa", "hồi quy", "kiểm chứng", "test tự động".
---

# Testing a game at midcore scale

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

Check `testing.assemblies` and `testing.playmode` in the profile before proposing where a test goes
or claiming what coverage exists.

## Games invert the usual advice

General testing advice optimises for codebases where the code is the risk. In a game at this scale
the code is a minority of the surface: **content outnumbers code, and persisted data outlives both.**

So the ranking is not the familiar one:

| Tier | What it asserts on | Value per minute | Runs in |
|---|---|---|---|
| **1. Content validation** | the data designers author | **highest** | seconds |
| **2. Save migration** | data already on players' devices | **highest** | seconds |
| **3. Deterministic logic / golden** | rules and simulation | high | seconds |
| **4. Hot-path benchmark** | performance regressions | medium | seconds–minutes |
| **5. In-editor play-session tests** | integration, wiring | **lowest** | minutes |

Tiers 1 and 2 are top because they catch mistakes **no code test can** — a designer's dangling id, a
migration that drops an inventory — and because their failures reach players directly.

Tier 5 is last not because integration does not matter, but because in a game these tests are slow,
environment-dependent, and flaky in proportion to how much they cover. Keep them few and targeted.

## The one rule

> **A green run with zero tests executed is a failed run, not a pass.**

Always read the count. A filter that matches nothing, an assembly that failed to compile, a suite
that was skipped — all report success at the process level. Quote the numbers when reporting, and if
a suite did not run, say which and why.

## What to test, by change type

| You changed | Must pass before it is done |
|---|---|
| Content rows only | Content validation (all tiers) |
| A content **schema** | Content validation + anything reading that table |
| A **persisted type** | **Migration matrix, every shipped version** — non-negotiable |
| Game rules / simulation | Deterministic logic tests; golden run if one exists |
| A hot path | Benchmark before/after — see `midcore-perf-budget` for the evidence rules |
| Wiring / composition | A smoke boot; targeted play-session test only if nothing cheaper covers it |
| UI / view code | Usually nothing automated — verify by running it, and say that is what you did |

The last row is deliberate. View code is expensive to test and cheap to inspect. Say plainly that it
was verified by running, rather than manufacturing a test that asserts a view was constructed.

## What makes a test worth keeping

- **It can fail.** Write it, watch it fail for the right reason, then make it pass. A test never
  observed failing is not known to test anything.
- **Its failure names the cause.** "Expected 7 items, got 4 — `sword_iron`, `potion_small`,
  `key_bronze` missing" beats "assertion failed".
- **It is deterministic.** A flaky test is worse than none: it trains people to re-run and move on,
  and it will be re-run past a real failure. See `references/determinism-and-golden.md`.
- **It does not test the framework.** Asserting that a serializer serializes is testing someone
  else's library.
- **It survives refactoring.** Tests coupled to internal structure get deleted during the first
  refactor, taking their coverage with them. Assert on behaviour.

## Coverage is not the target

Percentage coverage measures which lines ran, not which behaviours are protected. A project can have
high coverage and no migration tests — the exact combination that ships a save wipe.

Ask instead: **"which failures would reach a player, and which of those would a test catch first?"**
Then write those tests. If a number is wanted, count invariants protected, not lines executed.

## This skill does not own

| Question | Owner |
|---|---|
| The test-runner command, flags, and how to read its report | `unity_operations` authority (see profile) |
| Which assembly a test lives in, and its naming | `structure_naming` authority + `midcore-assembly-architecture` |
| Content validation **rules** (this skill covers running them as tests) | `midcore-data-pipeline` |
| Migration **recipes** and golden fixtures (this skill covers the matrix) | `midcore-save-migration` |
| Performance measurement methodology | `midcore-perf-budget` |
| Which gates block a release | `midcore-release-pipeline` |

## References

- `references/test-tiers.md` — the five tiers in depth, what each catches and cannot catch, and how
  much of each to have.
- `references/data-and-balance-tests.md` — tests that assert on authored content; the highest-value
  and most-skipped category in a game.
- `references/determinism-and-golden.md` — seeded randomness, fixed timestep, recording and updating
  a golden run, and eliminating flakiness.
- `references/merge-gates.md` — what must pass by change type, how to report honestly, and the
  zero-tests-executed trap.
