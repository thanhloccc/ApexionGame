# Technical debt

## 1. Classify before deciding

Two axes. The quadrant decides the response.

| | **High interest** — costs time repeatedly | **Low interest** — sits still |
|---|---|---|
| **Deliberate** — chosen knowingly | **Pay early.** It was understood when taken, so it is cheapest to repay | **Record and live with it.** Revisit on a trigger |
| **Accidental** — from a wrong model | **Understand first, then pay.** The code is a symptom; the model is the debt | **Record. Do not touch** without another reason |

The distinction that matters most is **interest rate**, not ugliness.

> **The interest rate is: how often has this cost someone time?**

Ugly code nobody touches has an interest rate of zero and is not debt in any meaningful sense.
Reasonable-looking code that everyone works around every week is expensive debt.

## 2. Recognising it

| Signal | Usually means |
|---|---|
| The same bug class recurs in one area | **Wrong model** — accidental, high interest |
| Estimates in one area are consistently high with no clear reason | Debt nobody has named |
| People route around a file rather than change it | High interest, being paid in avoidance |
| A change requires touching many unrelated places | Missing or wrong boundary |
| New features are shaped by what the structure allows | Debt is now steering the product |
| "Don't touch that, it works" | Nobody understands it — the worst kind |
| Two places must be kept in sync by hand | A missing single source of truth (`state-ownership.md` §3) |

The first is the clearest. **A recurring bug class is a structural problem presenting as a series of
individual bugs**, and fixing them one at a time is paying interest forever without touching the
principal.

## 3. When to pay

**Pay now:**

- it blocks the work in front of you;
- the same bug class has recurred more than twice;
- you are already changing that code for a real reason;
- it is small and a safety net exists.

**Pay later, with a trigger:**

- the area has work queued — *"before the next feature lands here"*;
- a safety net must be built first;
- it is large enough to need its own planning.

**Do not pay:**

- nothing changes there;
- the code is being deleted;
- the only gain is aesthetic;
- something urgent is in flight — **the worst possible moment**, and the reason refactoring gets
  blamed for missed dates.

**"Later" needs a trigger, not a date.** A dated later is a never with extra steps. A triggered later
fires when it becomes relevant, which is exactly when paying is cheapest.

Doing the work is `refactoring`; this file decides *whether and when*.

## 4. Record it where it is visible

> **Invisible debt is paid by people who do not know they are paying.**

Debt nobody wrote down is not a decision — it is an accident that people spend time on quietly:
someone reads around a confusing structure, adds a defensive check, or estimates high without knowing
why.

Record: **what**, **why it was taken**, **what it costs**, **what would trigger repayment.**

Where:

| Debt | Record in |
|---|---|
| Local to a piece of code | at the site — a short note, once, with the reason |
| A design decision knowingly deferred | an ADR (`decision-records.md`) |
| Scoped to one feature | that feature's plan document |
| Cross-cutting | an ADR at the higher level |

Note the project's comment convention before writing at the site — in this repo, comments are not
written by default (`coding-standards`). A recorded piece of debt is one of the cases where the
reason genuinely must survive, so if the site is not the right place, an ADR is.

## 5. Deliberate debt, taken well

Shipping something knowingly imperfect is a legitimate engineering decision. What makes it
legitimate:

1. **It is stated** — not discovered later by someone else.
2. **The cost is named** — what becomes harder, and for whom.
3. **The trigger is named** — what would make repayment necessary.
4. **It is contained** — the shortcut is behind a boundary, so repaying it is local.

Point 4 is what separates a loan from a mess. A shortcut behind a clear owner and a clear interface
is repaid by changing one piece. The same shortcut spread across ten call sites is repaid by
changing ten.

```markdown
DEBT: equipment stats are recomputed on every read rather than cached.

Why: the caching invalidation depends on modifier rules that are not final.
Cost: O(modifiers) per read; currently called from the HUD once per frame.
Trigger: if modifier counts exceed ~20, or if the HUD moves to per-entity display.
Contained: behind EquipmentStats — the cache would be internal to it.
```

## 6. What is not debt

Worth separating, because calling everything debt makes the word useless:

- **Code you would have written differently.** A preference is not a debt.
- **Old code that works and is not touched.** Age is not interest.
- **A deliberate simplification that is still correct.** Not building the general case is a decision,
  not a loan (`tradeoffs-and-pushback.md` §2).
- **A missing feature.** That is a backlog item.
- **An unmeasured performance concern.** That is a hypothesis — `midcore-perf-budget`.
