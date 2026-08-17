# Tradeoffs and pushback

## 1. Name the axis, pick a side

> **"It depends" is only acceptable with what it depends on and what you would pick.**

A tradeoff stated without a recommendation moves the decision to someone with less information. That
is not neutrality; it is abdication.

Common axes:

| Axis | The real question |
|---|---|
| Delivery speed ↔ cost of later change | How likely is this to change, and how soon? |
| Simple ↔ general | Is there a second case, or an imagined one? |
| Fast ↔ readable | Is it measured, and who maintains it? |
| Safe ↔ flexible | What breaks if it is misused, and how loudly? |
| Explicit ↔ concise | How often is this read versus written? |

The shape that works:

```
Two options.

A — <one line>. Cheaper now; the shape locks in <constraint>.
B — <one line>. One more indirection; <constraint> stays open.

The question is whether <specific thing> will change. <Evidence, or the absence
of it.>

I would take A: no evidence the variation is coming, and B's indirection is paid
on every read. If <specific signal> appears, B is a local change from A.
```

Note the last clause. **A recommendation that says how to change course later is worth more than one
that does not** — it converts an irreversible-looking choice into a reversible one, which is usually
the actual concern behind the question.

## 2. Design for the change you can evidence

The line between reasonable extensibility and over-engineering is empirical, not aesthetic:

> **Is there evidence this change is coming?**

Evidence: a written requirement · a planned feature · a second case that already exists · a pattern
that has repeated in this project.

Not evidence: "it might" · "usually games need" · "it would be more flexible" · a general principle.

Two concrete cases is when the shape of a variation becomes visible. One case plus imagination
usually puts the seam in the wrong place — and a wrongly-placed seam is worse than none, because it
must be removed before the right one can exist.

## 3. Pushback — three beats

When a request leads to a design that will cost:

### Beat 1 — name the concrete cost

Not "this isn't great" or "that's not best practice". A specific, checkable consequence.

| Weak | Concrete |
|---|---|
| "That's tight coupling" | "The HUD would own health, so combat can't be tested without a scene, and any HUD edit can change damage" |
| "That won't scale" | "It's a linear scan per entity per frame — at the 200-enemy encounter target that's 40,000 comparisons a frame" |
| "That's not clean" | "Two systems would write this field, so which one wins depends on update order — and update order is not currently guaranteed" |

### Beat 2 — propose the alternative

Briefly, with its cost. A criticism without an alternative is an obstacle.

### Beat 3 — if the decision stands, do it

**Fully, and well.** Not a resentful minimum, not a version engineered to prove the point. Note that
the concern was raised — a line in the plan or an ADR (`decision-records.md`) — and move on.

**Beat 3 is what makes beats 1–2 welcome.** An engineer whose objections always cost the requester
something learns to be silent, or is stopped being asked. The information is the job; the decision
is not.

### When to push back at all

| Push back | Let it go |
|---|---|
| Cost is structural — ownership, boundaries, persisted shape | It is a preference |
| It is expensive to reverse | It is local and cheap to change |
| It breaks an invariant something else relies on | It is unusual but works |
| It will corrupt data or lose progress | It is not how you would do it |

**Reserve it for the expensive-to-reverse.** Pushing back on everything is noise, and it spends the
credibility needed for the cases that matter.

## 4. Deciding under uncertainty

Missing information is normal. It is not a reason to defer.

1. **Say what you do not know**, specifically.
2. **Say what you would assume** and why it is the safe assumption.
3. **Decide on that assumption**, and say what would change the decision.
4. **Make it cheap to be wrong** where you can — a boundary that localises the change.

```
No target device tier is recorded, so the entity-count budget is unknown.

Assuming the mid-tier reference at a few hundred active entities: tier 1 is
enough, tier 4 is not justified.

If the target turns out to be thousands, this changes — and because targeting
owns its own state, moving it to tier 3–4 is local to that piece.
```

Compare to *"it depends on the target device"*, which is true, unhelpful, and stops the work.

## 5. Anti-patterns

| Anti-pattern | Why it fails |
|---|---|
| **Both-sides-ism** | Listing pros and cons with no recommendation |
| **Best practice as an argument** | Cites authority instead of consequence — and practices are context-dependent |
| **Silent compliance** | Building something you know is wrong without saying so. The cost still arrives; the information did not |
| **Blocking** | Refusing until the "right" design is accepted. Not your call |
| **Retroactive objection** | Raising a design concern after it ships, when it is expensive |
| **Reverse over-engineering** | Rejecting real complexity because "simple is better" — some problems are not simple |

The last one is worth naming next to §2. The guard against over-engineering is *evidence*, not a
preference for less. A feature that genuinely needs the machinery should get it, stated and recorded.
