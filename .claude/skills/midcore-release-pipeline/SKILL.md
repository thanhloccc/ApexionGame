---
name: midcore-release-pipeline
description: REQUIRED when producing, versioning or automating a build that leaves the developer's machine — release builds, CI pipelines, multi-platform build matrices, build numbering, symbol handling and crash symbolication, and the gates a build must pass before it can be published. Load it when setting up or debugging CI, cutting a release, deciding what must pass before shipping, or diagnosing a crash that only reproduces in a built player. This picks up where local editor operations stop — a build that ships must be reproducible from a commit plus a configuration, with nothing coming from a developer's local state. Triggers on "release", "CI", "continuous integration", "pipeline", "build number", "build version", "versioning", "IL2CPP", "symbols", "symbolicate", "crash", "store build", "signing", "publish", "ship", "hotfix", "artifact", and Vietnamese phrasing "lên bản", "build release", "đánh version", "crash trên máy thật", "phát hành", "bản vá".
---

# Release pipeline

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

**Check `release.ci` and `release.platforms` first.** If either says `unknown — ask`, the concrete
pipeline cannot be specified — ask rather than assuming a CI system or a platform set.

The `unity_operations` authority owns running a build **on this machine**. This skill owns builds
that **ship**: reproducibility, versioning, symbols, and the gates in front of publication.

## The one rule

> **A build is a function of a commit plus a configuration. Nothing else.**

If the output depends on who ran it, which machine, what was in a local cache, or which editor
window was open, then the build is not reproducible — and when a crash arrives from that build, you
cannot reconstruct what produced it.

Consequences that follow directly:

- every input is committed or pinned — no local paths, no uncommitted assets, no developer-local
  configuration;
- a clean checkout produces the same artifact;
- the commit hash is recorded **in** the build, not only in the pipeline log.

## Gate order

Cheapest and most fundamental first, so a failure arrives fast and names its cause.

| # | Gate | Blocks? | Evidence |
|---|---|---|---|
| 1 | Compiles, all configurations | **yes** | compiler output, zero errors |
| 2 | Content validation, all tiers | **yes** | validation report — `midcore-data-pipeline` |
| 3 | Save migration matrix | **yes** | test report, every shipped version — `midcore-save-migration` |
| 4 | Logic and golden tests | **yes** | test report with counts — `midcore-testing` |
| 5 | Build succeeds, all target platforms | **yes** | build artifact + build log |
| 6 | Smoke boot on a real device | **yes** | reaches interactive state |
| 7 | Benchmarks within budget | warn | before/after — `midcore-perf-budget` |
| 8 | Build size within budget | warn | size report vs previous |

Gates 1–6 block. The instinct to make one advisory because it is blocking a release is exactly how a
broken build ships — and gates 2 and 3 exist because their failures reach players in ways no patch
can undo.

**A skipped gate is reported explicitly**, by name, never quietly dropped. A release report that
omits what was not run is a claim, not a report.

## Versioning

Two numbers, doing different jobs:

- **Display version** — what players and the store see. Human-meaningful, follows the release cadence.
- **Build number** — monotonically increasing, never reused, unique across every build that leaves
  the machine. This is the one that ties a crash back to a commit.

Rules:

1. **The build number never goes backwards and is never reused.** Store platforms enforce this; more
   importantly, a reused number makes two different builds indistinguishable in crash reports.
2. **Record the commit hash inside the build**, readable at runtime and included in crash reports.
   Without it, "which build is this crash from" is unanswerable.
3. **Hotfix versioning is decided before the first hotfix**, not during one.
4. **A shipped version number is never reused**, exactly like a save schema version.

## Symbols

The rule that costs the most when broken:

> **An unsymbolicated crash from a shipped build is unactionable.**

Native and ahead-of-time-compiled builds produce stack traces that are meaningless without the symbol
files generated at build time. Those files are **not reproducible** by rebuilding — a rebuild
produces different addresses.

So:

- **archive symbols for every build that leaves the machine**, keyed by build number;
- **keep them for as long as any player might be on that build** — longer than you expect;
- **verify symbolication works before shipping**, not during an incident;
- treat losing symbols as a release-blocking failure.

Stripping settings interact with this: aggressive stripping removes code that reflection-based paths
depend on, producing failures that appear **only** in release builds. Test the release
configuration, not just the development one.

## What only breaks in a built player

Failures that never appear in the editor, and therefore need gate 6:

- code removed by stripping, breaking reflection or serialization;
- platform-specific paths and permissions;
- content that was never packaged into a group;
- conditional compilation that differs between editor and player;
- ahead-of-time compilation limits on generic or dynamic code;
- shader variants not compiled because nothing referenced them at build time.

This is why **a real-device smoke boot is a blocking gate** and "it works in the editor" is not
evidence of anything about a build.

## This skill does not own

| Question | Owner |
|---|---|
| Local editor commands, flags, log reading | `unity_operations` authority (see profile) |
| Which tests exist and what they cover | `midcore-testing` |
| Content validation rules | `midcore-data-pipeline` |
| Performance budgets and measurement | `midcore-perf-budget` |
| Post-release remote configuration | `midcore-live-ops` |
| Store policy, IAP, receipt validation | out of scope — a separate discipline |

## References

- `references/build-matrix.md` — what varies, keeping the matrix small, reproducibility, and
  eliminating local state.
- `references/versioning-and-symbols.md` — version schemes, build numbers, commit stamping, symbol
  archival, and stripping.
- `references/ci-gates.md` — the gate list in depth, artifacts as evidence, failing fast, and
  reporting a release honestly.
