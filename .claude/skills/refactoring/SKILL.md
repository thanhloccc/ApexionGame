---
name: refactoring
description: REQUIRED before changing the structure of code that already works — extracting, inlining, renaming, moving, splitting, merging, changing a signature, or replacing an implementation behind an interface. Covers the operation order that keeps the tree compiling at every step, keeping a refactor from growing past what can be reviewed, proving behaviour was preserved, and recognising the refactors that are not worth doing at all. The rule underneath everything is that one change alters structure OR behaviour, never both, because that is what makes verification possible. Load it before touching working code for structural reasons, and when a refactor has started growing beyond its original scope. Triggers on "refactor", "restructure", "extract", "inline", "rename", "move", "split", "merge", "clean up", "reorganize", "change signature", "replace implementation", "technical debt", "tidy", and Vietnamese phrasing "tách hàm", "tách class", "gộp lại", "dọn code", "đổi cấu trúc", "sửa lại cho gọn", "nợ kỹ thuật", "viết lại chỗ này".
---

# Refactoring

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

## The one rule

> **One change alters *structure* OR *behaviour*. Never both.**

This is not tidiness. It is what makes verification possible:

- A **structural** change must be provably behaviour-preserving — so **the tests do not change**. If
  a test needs editing, either it is not a refactor or the test was coupled to internals.
- A **behavioural** change has tests that change — but the structure holds still, so the diff is
  readable and the intent is visible.

Mixing them produces a diff nobody can review, a bisect that cannot isolate, and a bug that could
have come from either half. That is the whole reason "refactor" has a bad reputation.

## Before starting

1. **Say what the refactor buys.** "Cleaner" is not an answer. The answer is a change that becomes
   cheaper, a bug class that becomes impossible, or a boundary that stops being violated.
2. **Check there is a safety net.** Tests, a golden run, or a characterisation test written *now*
   (`references/verification.md`). Refactoring without one is rewriting.
3. **Fix the scope in writing** before the first edit (`references/scope-control.md`).
4. **Check whether it is actually a refactor**:

| If it… | It is not a refactor |
|---|---|
| crosses an assembly boundary | first a graph question — `midcore-assembly-architecture` |
| changes a type that is persisted to disk | a migration — `midcore-save-migration` |
| changes a public API others depend on | a breaking change, needing its own justification |
| is meant to make something faster | a performance change, needing measurement — `midcore-perf-budget` |
| changes what the code does | a behavioural change; do it separately |

## The three-step shape

Almost every safe refactor is the same three steps, not one:

```
1. ADD the new thing, alongside the old.        tree compiles, nothing uses it yet
2. MOVE consumers across, one at a time.        tree compiles after each
3. REMOVE the old thing.                        tree compiles, old thing unreferenced
```

Doing it in one step is where refactors break: the tree stops compiling in the middle, the error
count explodes, and the way back is unclear. Per-operation sequences and their traps:
`references/safe-sequences.md`.

## Verification

**If the tests had to change, it was not a refactor.** That single check catches most mistakes.

For code with no tests, write a **characterisation test first** — one that records what the code
does today, right or wrong. It is not a test of correctness; it is a tripwire. Then refactor, and it
must still pass unchanged.

Details, plus what to do when behaviour genuinely cannot be pinned down:
`references/verification.md`.

## When not to

Refactoring has a real cost and is not free virtue. Do not, when:

- the code is about to be deleted or replaced;
- nobody reads it and nobody changes it — cost of ownership is near zero;
- the goal is an abstraction nothing has asked for yet;
- you are in the middle of something urgent — **the worst possible moment**;
- the only justification is that it would look nicer.

The inverse trap is just as real: deferring every refactor until it is forced, which arrives during
the urgent thing. `references/when-not-to.md`.

## This skill does not own

| Question | Owner |
|---|---|
| Reviewing the current diff for reuse and simplification | `simplify` (built-in) — a **pass**; this is a **method** |
| Finding bugs that have not shown up yet | `code-review` (built-in) |
| Assembly graph, layers, cycles | `midcore-assembly-architecture` |
| Changing a persisted type | `midcore-save-migration` |
| Designing something that does not exist yet | `system-design` — design is before, refactoring is after |
| Whether a change made anything faster | `midcore-perf-budget` |
| Which tests are needed and what counts as verified | `midcore-testing` |
| Naming, file layout, formatting after the move | the `structure_naming` authority in the profile |
| Tracking down why something is broken | `debugging` |

## References

- `references/safe-sequences.md` — one sequence per operation (rename, extract, inline, move, split,
  merge, change signature, replace implementation), each with the trap that loses work silently.
- `references/scope-control.md` — fixing scope before the first edit, the "while I'm here" trap,
  recording findings without acting on them, and the signals to stop.
- `references/verification.md` — characterisation tests, what "behaviour preserved" means in
  practice, reviewing a structural diff, and when behaviour cannot be pinned down.
- `references/when-not-to.md` — refactors not worth doing, the deferral trap, and how to decide
  between doing it now, later, or never.
