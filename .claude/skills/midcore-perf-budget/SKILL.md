---
name: midcore-perf-budget
description: REQUIRED whenever performance is claimed, measured, budgeted or optimized — frame time, memory, load time, GC pressure, hitches, or any form of "is this fast enough". Covers defining device tiers and a per-tier frame and memory budget, dividing that budget across systems, the profiling workflow and what the editor systematically misreports, allocation auditing, and the evidence required before calling a change an improvement. Load it BEFORE optimizing anything, and before accepting any performance claim including your own. The core rule is that no performance claim is valid without a before-and-after measurement on a stated device tier — "should be faster" is not a result. Triggers on "performance", "optimize", "optimise", "slow", "fps", "frame time", "frame rate", "memory", "RAM", "GC", "garbage", "allocation", "profiler", "profiling", "budget", "stutter", "hitch", "spike", "load time", "startup time", and Vietnamese phrasing "tối ưu", "giật lag", "chậm", "tốn ram", "đo hiệu năng", "khung hình", "thời gian load".
---

# Performance budgets and evidence

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

**Check `performance.device_tiers` first.** If it says `unknown — ask`, then no frame budget, memory
ceiling or target frame rate has been decided, and you must ask rather than quoting a plausible
number. A budget invented by an agent and repeated across sessions becomes project folklore nobody
remembers deciding.

## The one rule

> **No performance claim without a before-and-after measurement, on a stated device tier, under
> stated conditions.**

"Should be faster", "this is more efficient", "avoids an allocation" — none of these are results.
They are hypotheses, and a meaningful fraction of them are wrong: the compiler already handled it,
the path was not hot, or the change moved the cost somewhere else.

This applies to your own changes as strictly as to anyone else's.

## "Fast enough" is meaningless without a number

Before any optimisation work, three things must exist:

1. **Target device tiers** — named, with a real reference device each. Not "low-end phones".
2. **A frame budget per tier** — total ms, then divided across simulation, rendering, UI, and
   headroom.
3. **A memory ceiling per tier** — divided across content, textures, audio, and managed heap.

Without these, "optimize this" has no completion condition, and every change is arguable. With them,
the question becomes checkable: is this system inside its share or not?

If the profile has no tiers, ask for: minimum-spec / mid / high reference devices, target frame rate,
and memory ceiling. Those four answers unblock everything else in this skill.

## The workflow

```
1. Establish the budget          ← from the profile, or ask
2. Measure on a target device    ← not the dev machine, not the editor
3. Find where the time goes      ← profile, do not guess
4. Confirm it is worth fixing    ← is it actually over budget?
5. Change one thing
6. Measure again, same conditions
7. Report both numbers, or report nothing
```

Steps 2 and 6 are what make it engineering. Skipping them produces changes that are believed to help.

**Step 4 is the one that saves the most time.** A system comfortably inside its budget does not need
optimising, however inelegant it looks. Optimising it costs real time and adds real risk, and buys
nothing measurable.

## Where the editor lies

Profiling inside a development editor systematically misreports:

- **Editor-only allocations** that do not exist in a build;
- **Domain reloads and asset import** appearing as gameplay cost;
- **Deep profiling overhead** that changes the shape of what you are measuring;
- **Absolute timings** that bear no relation to a target device;
- **Memory figures** including editor and tooling overhead.

The editor is useful for finding *which system* is expensive, and for comparing *before against
after* under identical conditions. It is not evidence of shipped performance. Anything reported as a
real-device number must come from a real device.

## Allocation pressure

Managed allocation matters when it is **per-frame and sustained**, because collection cost arrives as
a hitch. It matters much less at startup, on a UI click, or on a scene load.

So: hunt allocations in loops that run every frame, and leave the ones that run once alone. Chasing
startup allocations is a common way to spend a week for no measurable gain.

Which types and constructs to prefer is a **standing default owned by the `structure_naming`
authority**, not by this skill. This skill covers finding out whether an allocation is real, and
whether removing it changed anything.

## Reporting

State all four, or the claim is not verifiable:

| | Example |
|---|---|
| Device / tier | mid-tier reference device, release build |
| Conditions | 50 active agents, combat scene, 60s steady state |
| Before | 14.2 ms/frame, 4.1 KB/frame allocated |
| After | 9.8 ms/frame, 0 KB/frame allocated |

A measured **regression** that buys something is a legitimate outcome — record it, with what it
bought, so the next person does not "fix" it back.

An **unmeasurable** result is also legitimate: "no measurable difference; reverted" is a real finding
and saves the next person from repeating the experiment.

## This skill does not own

| Question | Owner |
|---|---|
| Which collection, math or memory type to prefer by default | `structure_naming` authority (see profile) |
| How to run a build or attach a profiler | `unity_operations` authority |
| Benchmarks as regression tests in the suite | `midcore-testing` (tier 4) |
| Content size and delivery grouping | `midcore-data-pipeline` |
| Whether a build is publishable | `midcore-release-pipeline` |

## References

- `references/device-tiers-and-budgets.md` — naming tiers, a fill-in budget table, dividing the
  budget across systems, and what to ask when no tiers exist.
- `references/profiling-workflow.md` — measuring on target hardware, isolating a frame, spikes
  versus sustained cost, and capturing comparable before/after.
- `references/allocation-audit.md` — when GC pressure matters, finding real per-frame allocations,
  and confirming one is not theoretical.
- `references/evidence-rules.md` — what counts as evidence, how to report, accepting a deliberate
  regression, and the claims to refuse.
