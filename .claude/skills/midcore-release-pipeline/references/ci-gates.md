# CI gates

## 1. The gate list

Ordered cheapest and most fundamental first, so a failure arrives quickly and names its own cause.

### Gate 1 — Compiles · **blocks**

Every configuration, every platform, zero errors. Warnings-as-errors if the project has adopted it.

Evidence: compiler output. On failure, read the **first** error — later ones are usually cascade.

### Gate 2 — Content validation · **blocks**

All tiers, whole content graph. Rules from `midcore-data-pipeline`.

Cheap, fast, and catches the class of mistake most likely to reach a player from a source nobody
reviewed. **Never advisory in the release gate**, regardless of how it is configured for local
imports.

Evidence: validation report, zero hard failures.

### Gate 3 — Save migration matrix · **blocks**

Every shipped version → current, from committed golden fixtures. Rules from
`midcore-save-migration`.

This gate exists because its failure mode is unrecoverable. A build that wipes saves cannot be fixed
by a patch — the data is already gone. There is no schedule pressure that justifies skipping it.

Evidence: test report showing one result per shipped version, with counts.

### Gate 4 — Logic and golden tests · **blocks**

The project's logic tier. Rules from `midcore-testing`.

Evidence: test report with **executed** counts. Zero executed is a failure, not a pass.

### Gate 5 — Build succeeds · **blocks**

Every target platform in the matrix, release configuration.

Evidence: the artifact plus the build log. A build that "succeeded" with errors in the log is not a
success — read it.

### Gate 6 — Smoke boot on a real device · **blocks**

The build launches on a real device of a target tier and reaches an interactive state. Ideally: new
game starts, a save round-trips, the main loop runs briefly.

This gate catches everything that only breaks in a built player — stripping, packaging, platform
paths, missing content. **"It works in the editor" is not evidence about a build.**

Evidence: it reached interactive state, on a named device.

### Gate 7 — Benchmarks within budget · **warns**

Rules from `midcore-perf-budget`. Warns rather than blocks because CI hardware timings are noisy and
a hard threshold produces false failures that get ignored.

Warns, but is **read**. A warning nobody reads is not a gate.

### Gate 8 — Build size within budget · **warns**

Compared against the previous release, per group where the project splits content.

A sudden jump is usually an accident — an asset imported at the wrong settings, a duplicated group.
Cheap to check, easy to miss for months.

## 2. Fail fast, cheapest first

Ordering is not cosmetic. Gates 1–4 run in minutes; gates 5–6 take much longer. A content typo caught
by gate 2 costs minutes; the same typo discovered after a full multi-platform build costs an hour of
machine time and a context switch.

**Never reorder to get a build artifact sooner.** The artifact from a build whose tests have not run
is not usable for anything, and it will be used anyway if it exists.

## 3. Blocking versus warning

| Blocks | Warns |
|---|---|
| Correctness that reaches players | Trends worth watching |
| Data loss (gate 3, always) | Noisy measurements |
| Build cannot run at all | Thresholds that are judgement calls |

The pressure to downgrade a blocking gate arrives during every difficult release, always framed as a
one-time exception. Handle it the same way as content validation
(`midcore-data-pipeline`, `validation.md` §2): if a gate is wrong, **fix or remove the gate
deliberately**, in its own change. Do not weaken the mechanism to route around one instance.

Gate 3 has no exception. A save-wiping build is worse than a late build, every time.

## 4. Evidence, not assertions

Each gate produces an artifact. "It passed" is not a result:

| Gate | Artifact |
|---|---|
| Compiles | compiler output |
| Content validation | validation report, failure count |
| Migration matrix | test report, one row per shipped version |
| Logic tests | test report with executed/passed/failed/skipped |
| Build | the artifact + build log |
| Smoke boot | device name, state reached |
| Benchmarks | numbers, versus budget |
| Size | numbers, versus previous release |

Archive them with the build (`build-matrix.md` §6). When a question arises about a release weeks
later, these are the only answer available.

## 5. Reporting a release honestly

State, always:

1. Which gates ran and their results, **with numbers**.
2. Which gates did **not** run, by name, and why.
3. Any warning that fired and the decision taken about it.
4. Anything verified manually rather than automatically.

```
GOOD  Gates 1-6 passed. Content validation: 0 failures across 11 tables.
      Migration matrix: 4 shipped versions, 4 passed.
      Logic tests: 212 executed, 212 passed.
      Smoke boot: reached main menu + new game on <device>.
      Gate 7 (benchmarks) NOT RUN — no benchmark suite exists yet.
      Gate 8: build size +2.1% vs previous; within tolerance.

BAD   All gates green, ready to ship.
```

The second omits that a gate does not exist, which reads as "it passed".

## 6. When there is no CI yet

If the profile says `ci: unknown — ask`, the pipeline cannot be specified — but the **gate list still
applies**, run manually.

Run them in order, by hand, and record the evidence exactly as a pipeline would. That record is what
makes a later automation straightforward: the gates are already defined, already ordered, and already
producing artifacts. Automating a process that exists is mechanical; inventing one during a release
is not.

Ask for: whether a CI system exists or is planned, which platforms ship, and who is permitted to
publish. Those three answers turn this file into a concrete pipeline.
