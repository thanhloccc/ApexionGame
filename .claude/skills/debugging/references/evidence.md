# Reading evidence

## 1. Read the first error, not the last

The first error is usually the cause; everything after is cascade. Console output scrolls, so the
visible one is the last — which is the least informative.

**Scroll up. Read the whole log, not the tail.** In build and batch logs the first compiler or
loader error is at the top and the interesting one is rarely last.

## 2. Reading a stack trace

- **Read from the inside out** — the innermost frame is where it threw, not necessarily where the
  bug is.
- **Find the first frame that is your code.** The bug is usually there or in what it passed down.
- **The message and the type matter as much as the location.** A null reference and an index out of
  range at the same line are different bugs.
- **Async and generated frames lie about the path.** A stack through an async state machine or
  generated code does not show the logical caller. Reconstruct it from the surrounding evidence.
- **Missing frames mean inlining or stripping.** In an optimised build, absent frames are expected —
  see the routing table in `SKILL.md`.

## 3. What the tools misreport

| Tool | Misreports |
|---|---|
| A debugger | **Timing.** Breakpoints change ordering; race conditions vanish under one |
| Console output | **Ordering**, when writes come from more than one thread |
| Buffered logging | **The end.** A crash can lose the last and most important lines |
| An editor/dev environment | Costs and allocations that do not exist in a build — `midcore-perf-budget` |
| Watch windows | Evaluating a property can have a **side effect** |
| Timestamps | Different clocks; not comparable across processes or machines |

The consequence worth internalising: **a bug that disappears under the debugger is a timing bug**,
and that is information, not an obstacle.

## 4. Instrumenting on purpose

When the evidence is insufficient, add some — deliberately, not by scattering.

1. **Decide the question first.** "Is this called?" "With what value?" "In what order?" A log added
   without a question produces noise.
2. **Place it at a boundary**, where you can say right-here/wrong-there (`narrowing.md` §3).
3. **Include identity** — which entity, which frame, which iteration. A value with no identity cannot
   be correlated.
4. **Prefer an assertion to a log** when the condition should never hold. It fails at the moment of
   violation instead of at the moment you notice.
5. **Remove it when done**, or make it permanent on purpose. Debug logs left behind become the noise
   that hides the next bug.

**Never log inside a hot loop without bounding it.** Thousands of lines per second lose the very
line you needed, and change the timing enough to hide the bug.

## 5. Evidence from the field

A report from a real device or a real player, with no reproduction:

- **The version and build number** are the first things to establish. A bug in an old build may
  already be fixed — `midcore-release-pipeline`.
- **Counters beat logs at scale.** "How often, on which versions" is answerable by a counter and not
  by individual reports.
- **Never log player data.** Logs travel; save contents and identifying information do not belong in
  them — `midcore-live-ops/references/telemetry.md` §7.
- **Absence of evidence is evidence.** An event that should have fired and did not narrows things as
  much as one that did.

## 6. Reading code as evidence

The code is evidence too, and the failure mode is reading what you *meant*:

- **Check the actual operator.** `<` versus `<=`, `&&` versus `||`, `=` versus `==`.
- **Check the actual variable.** `i` versus `j` in a nested loop; a shadowed name.
- **Check what is really called** — an override, an extension method, an implicit conversion.
- **Check the order of evaluation** where it matters.
- **Check every early return.** A function that returns before the interesting line explains a lot of
  "this code never runs".

When reading stops yielding, stop reading. Add an observation instead (§4) — the code says what it
should do; the observation says what it does.
