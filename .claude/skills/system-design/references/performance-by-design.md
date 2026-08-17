# Performance by design

Choosing how much machinery a feature warrants — **before** the code exists, when the decision is
cheap.

The instruction this file serves: *prefer what delivers high performance, without over-engineering.*
Both halves are load-bearing, and the second is the one usually dropped.

## 1. The ladder

**Start at the lowest tier that plausibly works. Move up only with a reason you can state.**

| Tier | What | Move up when | Cost |
|---|---|---|---|
| **0** | Straightforward code, standard types | default | — |
| **1** | Data structure matched to the access pattern | access does not fit the default shape — lookup in a loop, repeated linear scan, ordered traversal | **near zero** |
| **2** | No allocation in hot loops; reuse buffers | **measured** per-frame allocation | lifetime management |
| **3** | Layout for cache; hot/cold split | many entities, per-frame traversal, **measured** | data becomes harder to read |
| **4** | Jobs / parallelism | per-element independent work, large N, **measured** as dominant | data partitioning, synchronisation, much harder debugging |
| **5** | Burst / native compilation | tight numeric loops, already at tier 4, **measured** | unmanaged constraints, platform differences, a wall for readers |

Tiers 2 and above all say **measured**. That is not decoration — see §4.

## 2. Tier 1 deserves its own paragraph

It is nearly free and it is the tier most often skipped, in both directions.

Choosing a data structure that matches the access pattern costs one decision at design time and
nothing at runtime. Getting it wrong is **expensive to retrofit**, because the wrong structure
spreads into every call site: the type appears in signatures, in fields, in serialized data.

Contrast tiers 2–3, which are **local** — you can add buffer reuse or split hot/cold later, in one
place, without touching callers.

So the asymmetry is: **tier 1 is worth deciding up front; tiers 2–5 are worth deferring.** That is
the opposite of the instinct, which is to defer the data structure ("I'll optimise later") and reach
early for the exciting machinery.

The project's available structures and their tradeoffs are a **catalogue**, owned by the
`structure_naming` authority in the profile. This file says *how much* to reach for; that one says
*what exists*.

## 3. Before jobifying — the five questions

All five must have an answer before tier 4.

1. **Is the work per-element independent?** If elements depend on each other's results, or on order,
   parallelism does not apply. This is a property of the algorithm, not of effort.
2. **Is N large enough on the lowest target tier?** Scheduling has fixed overhead. A few dozen
   elements will be slower jobified. Check the *minimum* device, not the development machine.
3. **Is the data already in a job-compatible form**, or convertible without copying every frame? A
   per-frame copy in and out routinely costs more than the job saves.
4. **Has it been measured as dominant?** (`midcore-perf-budget`.) Without this, tier 4 is
   over-engineering by definition — you cannot know it was the bottleneck.
5. **Who debugs it at 2am, and can they?** Jobified code is materially harder to step through,
   log from, and reason about. This is a real, recurring cost paid by everyone after you.

**Cannot answer 4? Stop.** No measurement means no tier 4 or 5. The correct move is tier 0–1, ship
it, measure, and escalate if the measurement says so — which is cheap, because tiers 2–4 are local
changes when the ownership and decomposition are right.

**The exception**, stated deliberately: a feature designed from the start around thousands of
entities with obviously independent per-element work — a large-scale simulation whose whole premise
is the scale. There, the shape is the requirement, not an optimisation. Say so explicitly and record
it as a design decision (`decision-records.md`), rather than letting it pass as an unstated
assumption.

## 4. The guard rails, both ends

**Upper guard — do not escalate "to be safe."**

| Symptom | Reality |
|---|---|
| "Jobs, in case there are a lot later" | Paying for a future that may not arrive, in a currency (debuggability) that is spent daily |
| "Native containers everywhere for speed" | Manual lifetime management and a class of crash you did not have |
| "Burst it, it's a hot loop" | Unverified. And it constrains the code shape permanently |

Every tier above 1 must state **the reason** and **the cost accepted**. A tier with no stated cost
was not a decision.

**Lower guard — do not stop at tier 0 when the feature clearly has a performance dimension.**

The signals: runs per frame · scales with entity count · touches a known hot path · handles large
data. When any holds, tier 1 is **required**, and skipping it is not "avoiding premature
optimisation" — it is a decision not made.

"Premature optimisation" was never an argument for choosing the wrong data structure. It is an
argument against tiers 2–5 without measurement.

## 5. Recording the decision

For the feature gate, one short block:

```markdown
### Performance tier

Chosen: tier 1 — spatial lookup keyed by cell, values contiguous for traversal.

Why not lower: enemy targeting scans all active enemies every frame; a linear
scan is O(n²) across the encounter and n grows with the encounter design.

Why not higher: n is in the low hundreds at the top design tier. No measurement
exists yet, and tiers 2–4 stay available — the ownership boundary means adding
buffer reuse or jobifying later is local to this piece.

Revisit if: encounter size targets increase, or profiling shows targeting is
over its share of the simulation budget.
```

Both "why not lower" **and** "why not higher" are required. A block with only one of them recorded a
preference, not a decision.

## 6. Boundaries

| Question | Owner |
|---|---|
| What structures and math types exist here, and what each is for | `structure_naming` authority — **the catalogue** |
| What is the budget, how do I measure, what evidence is enough | `midcore-perf-budget` — **measurement, after** |
| **How much machinery does this feature warrant, up front** | **this file — decision, before** |
| Where attributes like inlining hints go | `coding-standards` |
| Making existing code faster | `refactoring` + `midcore-perf-budget` |

The gap this file fills: `midcore-perf-budget` correctly refuses to accept a claim without
measurement — but before code exists there is nothing to measure, and a decision still has to be
made. This is that decision, made explicitly, with the tier and both guard rails recorded so the
measurement can later confirm or overturn it.
