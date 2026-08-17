---
name: system-design
description: REQUIRED on every feature request, before any plan is written and before any code exists. Owns the design of a feature below the assembly boundary — how it decomposes into collaborating pieces, who owns each piece of state and who may write it, how the pieces communicate, which alternatives were rejected and why, and how much performance machinery the feature actually warrants. Also owns the engineering judgment around those choices - naming a tradeoff instead of hiding it, pushing back with a concrete cost when a request leads to a bad design, reading unfamiliar code before changing it, and recognising technical debt. A design that cannot say who owns its state is not a design, it is an intention. Load it alongside the project's module-selection skill on any request to build, add or implement something, when reviewing a design, when deciding whether something warrants jobs or a specialised data structure, and when a decision needs recording. Triggers on "design", "architecture", "how should I structure", "who owns", "state", "coupling", "abstraction", "interface", "tradeoff", "over-engineering", "premature optimization", "should I use jobs", "worth optimizing", "technical debt", "build a feature", "implement", "add a system", "new feature", and Vietnamese phrasing "thiết kế", "kiến trúc", "làm feature", "thêm hệ thống", "cấu trúc thế nào", "ai giữ state", "có nên dùng job không", "tối ưu sớm", "nợ kỹ thuật", "để chỗ này thế nào cho đúng".
---

# System design

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

Check `performance.device_tiers` before choosing a performance tier. If it says `unknown — ask`,
pick the **lowest** tier that plausibly works and say what would change the answer — never escalate
machinery to cover an unknown.

## The three questions

> **Every design answers these before any code exists:**
>
> 1. **Who owns each piece of state, and who may write it?**
> 2. **How do the pieces communicate?**
> 3. **What did you reject, and why?**

A design that cannot answer them is not a design — it is an intention.

Three, not ten, on purpose. A longer gate gets filled in mechanically, and these three already pull
most of the rest along: clear ownership almost always means testable; clear communication means
clear boundaries; a named rejection means the choice was actually made rather than defaulted into.

- **Ownership is the most expensive thing to reverse.** It decides whether the feature can be
  tested, saved, or reasoned about when two systems disagree.
- **Communication decides readability and whether boundaries survive** the next six months.
- **The rejected alternative is the difference between a decision and a default.**

## The feature gate

Every feature plan carries these sections. **A plan missing one is not ready for review.**

| Section | Contains | Not acceptable |
|---|---|---|
| **Decomposition** | The pieces, and why those pieces | A list of files |
| **State ownership** | Table: each piece of state → owner → who may write | State with no owner, or two owners |
| **Communication** | Which mechanism between which pieces, and why that one | "uses events" with no reason given |
| **Rejected alternatives** | At least one, with the reason | Empty, or a straw man |
| **Performance tier** *(conditional)* | The tier chosen, and why not higher **and** not lower | A high tier "to be safe", or omitted when the feature clearly has a performance dimension |

The fifth is required **only when the feature has a performance dimension** — runs per frame, many
entities, large data, or touches an existing hot path. A settings screen does not need it. The gate
itself must not be over-engineered.

## Four behaviours

### Push back when the design is wrong

Three beats, and **all three are required**:

1. **Name the concrete cost** — not "this isn't great". *"UI writing directly to the save store means
   every UI change can corrupt progress, and nothing can be tested without a save file."*
2. **Propose the alternative**, briefly.
3. **If the decision stands, do it — fully, and well** — and note that the concern was raised.

Skipping beat 3 turns an engineer into an obstacle. Skipping beats 1–2 turns them into a typist.

### Name the tradeoff

State the axis, **choose a side**, explain. Common axes: delivery speed vs cost of later change ·
simple vs general · fast vs readable · safe vs flexible.

**"It depends" is only acceptable with what it depends on and what you would pick.**

### Read before changing

Never propose a change to unfamiliar code before mapping its boundaries, its state ownership, and its
invariants. `references/reading-before-changing.md`.

### Recognise debt

Classify it, say whether to pay now or live with it, and record it where it is visible.
`references/technical-debt.md`.

## Performance, at design time

Start at the **lowest tier that plausibly works**; move up only with a reason you can state.

| Tier | What | Move up when |
|---|---|---|
| 0 | Straightforward code, standard types | default |
| 1 | Data structure matched to the access pattern | access does not fit the default — **cheapest and most-skipped** |
| 2 | No allocation in hot loops; reuse buffers | measured per-frame allocation |
| 3 | Layout for cache; hot/cold split | many entities, per-frame traversal, measured |
| 4 | Jobs / parallelism | per-element independent work, large N, measured as dominant |
| 5 | Burst / native compilation | tight numeric loops, already at tier 4, measured |

Guard rails on **both** ends — the lower one is the one people forget:

- **Do not escalate "to be safe."** Every tier costs readability and debuggability.
- **Do not stop at tier 0** when the feature obviously has a performance dimension. Tier 1 is nearly
  free and is **expensive to retrofit**, because the wrong data structure spreads into every call
  site — while tiers 2–3 can be added later.

Full ladder, the five questions to answer before jobifying anything, and the boundary against the
measurement skill: `references/performance-by-design.md`.

## This skill does not own

| Question | Owner |
|---|---|
| Which assembly, which layer, cycles, compile time | `midcore-assembly-architecture` |
| Which library module solves this | the `structure_naming` authority in the profile |
| Naming, folders, namespaces, member order | the `structure_naming` authority |
| How the code is formatted, API style, attributes | `coding-standards` |
| Changing the structure of code that already exists | `refactoring` |
| Why something is broken | `debugging` |
| Frame budgets, profiling, before/after evidence | `midcore-perf-budget` |
| Content table design | `midcore-data-pipeline` |
| The shape of persisted data and its migration | `midcore-save-migration` |
| Reviewing a diff for bugs or simplification | `code-review`, `simplify` (built-in) |

## References

- `references/state-ownership.md` — **read first.** The ownership table, single-owner rule,
  authoritative vs derived state, lifetime, and the failure modes.
- `references/decomposition.md` — splitting by what changes together, the seams that pay in a game,
  and the signs of too-big and too-small.
- `references/communication.md` — call / query / event / shared state, what each costs, and why
  events are not the default.
- `references/tradeoffs-and-pushback.md` — naming axes, the ban on bare "it depends", designing for
  the change you can evidence, and the three beats of pushback.
- `references/performance-by-design.md` — the tier ladder, the jobify questions, and the
  over-engineering guard on both ends.
- `references/reading-before-changing.md` — mapping unfamiliar code before proposing anything.
- `references/technical-debt.md` — classifying debt, when to pay, and recording it so it stays
  visible.
- `references/decision-records.md` — the ADR template, and when a decision belongs in an ADR rather
  than a feature doc.
