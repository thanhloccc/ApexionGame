# The five tiers

## Tier 1 — Content validation

**Asserts on:** the data designers author.
**Catches:** dangling ids, duplicates, broken curves, unreachable rewards, missing translations.
**Cannot catch:** whether the content is *good*. Balance is a design question.

The validation rules themselves belong to `midcore-data-pipeline`. Running them as part of the
automated suite is this skill's concern, and it is worth doing even though the importer already runs
them, because:

- it catches content committed by someone who skipped the import;
- it runs on every change, including code changes that alter what "valid" means;
- it fails in the same place as everything else, so nobody has to remember a separate step.

**How much:** all of it, always. It runs in seconds and it is the cheapest coverage in the project.

---

## Tier 2 — Save migration

**Asserts on:** data already on players' devices.
**Catches:** lost progress, dropped inventory, wiped saves, untested version jumps.
**Cannot catch:** whether the *new* schema is a good design.

The matrix and fixture rules are in `midcore-save-migration`. What matters here: this tier is
**non-negotiable** for any change to a persisted type, and it is the one tier where "we will add
tests later" is not available — later is after players have lost data.

**How much:** every shipped version → current, as a loop over the fixture set, on every run.

---

## Tier 3 — Deterministic logic and golden runs

**Asserts on:** rules, simulation, progression, state machines.
**Catches:** rule regressions, edge cases, ordering bugs, arithmetic mistakes.
**Cannot catch:** anything involving real rendering, real input timing, or real devices.

This is the tier that most resembles conventional unit testing, and the layering makes it possible:
if Systems cannot be tested without Presentation, this tier does not exist (see
`midcore-assembly-architecture`).

Two forms:

- **Targeted logic tests** — one rule, explicit inputs, explicit expected output. Fast to write, fast
  to diagnose. The default.
- **Golden runs** — drive a deterministic simulation for N steps, compare against a recorded
  trace. Enormous coverage per test, but a failure says "step 4137 differs" and needs work to
  diagnose. Best for systems where the interactions, not the individual rules, are the risk.

Have both. Targeted tests for rules you can name; a golden run for the emergent behaviour you cannot.

**How much:** cover every rule that a designer would notice being wrong.

---

## Tier 4 — Hot-path benchmarks

**Asserts on:** the cost of code that runs constantly.
**Catches:** performance regressions, accidental allocations in loops, an O(n²) that was O(n).
**Cannot catch:** real frame time on a real device — see `midcore-perf-budget`.

A benchmark in the test suite is a **regression detector**, not a measurement of shipped
performance. It runs on developer machines and CI hardware whose absolute numbers mean nothing;
what means something is the number changing.

- Assert on a **relative** budget or a ratio, not an absolute millisecond figure, or it fails on
  every machine that is not the one it was written on.
- Assert on **allocation count** where the language exposes it — far more stable across hardware
  than time, and usually the thing that actually regressed.
- Keep them few. One per genuinely hot path, not one per method.

**How much:** a handful. A benchmark suite large enough to be slow gets skipped, at which point it
detects nothing.

---

## Tier 5 — In-editor play-session tests

**Asserts on:** integration, scene wiring, engine-dependent behaviour.
**Catches:** things that only break when everything is assembled.
**Cannot catch:** cheaply. This tier is slow, environment-dependent, and the flakiest by a wide
margin.

Worth having for a small number of critical paths — the game boots, a new game reaches playable
state, a save round-trips through the real storage layer. Not worth having for anything a lower tier
can cover.

The trap: it *feels* like the most valuable tier because it is closest to what a player does. It is
also the tier where a two-minute run and an intermittent failure trains everyone to ignore the suite.

**How much:** single digits. If the profile says no play-mode suite exists, do not imply otherwise —
and adding one is a deliberate decision with an ongoing maintenance cost, not a free improvement.

---

## Allocating effort

For a midcore project, a healthy rough distribution:

```
Tier 1  content validation    ████████████  runs always, near-zero cost
Tier 2  save migration        ████████████  runs always, non-negotiable
Tier 3  logic + golden        ████████      the bulk of written tests
Tier 4  benchmarks            ██            a handful, guarding named hot paths
Tier 5  play-session          █             single digits, critical paths only
```

The common failure is an inverted pyramid: a large tier-5 suite that takes twenty minutes and fails
intermittently, no tier-1 coverage, and no tier-2 coverage at all. That project has the most
expensive tests and the least protection where it matters.
