# Decision records

## 1. Why a separate artifact

A feature plan records decisions made *while planning that feature*. It cannot record:

- a decision made **mid-implementation**, when no plan document is open;
- a decision that **cuts across** several features;
- a decision that **reverses** an earlier one.

Those have nowhere to go, so they live in someone's memory until it fades — and three months later
the reasoning is gone while the consequence remains. Somebody then "fixes" it back.

## 2. Template

One page, no more. A long ADR is not read, and an unread record is not a record.

```markdown
# ADR-0007 — <the decision, stated affirmatively>

| | |
|---|---|
| Status | proposed / accepted / superseded by ADR-00NN |
| Date | YYYY-MM-DD |
| Related | feature doc, other ADRs |

## Context

What forced a decision. Facts, not opinion. What constraints were in play.

## Decision

What was chosen. One sentence.

## Rejected

One line per alternative, each with why it was rejected.

## Consequences

What becomes easier. What becomes harder. **Both** — a record with only the
upside is advertising, and it is exactly what makes a future reader distrust
the whole set.
```

**Title states the decision, not the topic.** `ADR-0007 — Combat state lives in the simulation, not
the view` — a reader knows the answer from the index without opening it. `ADR-0007 — Combat state`
tells them nothing.

## 3. ADR or feature doc

| Situation | Record in |
|---|---|
| Decision belongs to a feature with a plan document | that document's decisions section |
| Decision arose **mid-implementation**, no plan open | **ADR** |
| Decision **cuts across** several features | **ADR**, at the level it applies to |
| Decision **reverses** an earlier one | **new ADR**, marking the old one superseded |
| Decision is about tooling or process, not the product | wherever that is documented — not here |

When unsure, the test: **would someone touching a different feature need to know this?** If yes, ADR.

## 4. ADRs are never edited, only superseded

> A superseded ADR stays, with its status changed and a pointer to the one that replaced it.

The history of decisions is the value. Overwriting an ADR turns the set into a snapshot of the
present — and the present is already described by the code. What the code cannot tell you is what was
considered and rejected, and why the obvious approach was not taken.

The only edits that are legitimate: fixing the status line, adding the superseded-by pointer, and
correcting a factual error (a wrong file name) — not revising the reasoning.

## 5. Where they live

Check the profile and the project's documentation convention. The default that fits this project's
structure:

- **Owning assembly's documentation folder**, named to sort alongside the feature documents.
- **Cross-cutting decisions** at the package or repo documentation root.

**Not** in the tooling directory. Architecture decisions belong to the project — readable by people
who do not use these tools, and living beside the code they describe.

## 6. When to write one

Write an ADR when a decision:

- is **expensive to reverse** — ownership, boundaries, persisted shape, a public contract;
- **rejects an obvious alternative**, so the next person does not re-litigate it;
- **constrains future work** — "this cannot be jobified because X";
- **was contested**, so the reasoning survives the argument;
- was **taken knowingly as debt** (`technical-debt.md` §5).

Do **not** write one for a decision that is visible in the code and has no rejected alternative. That
is a comment at best, and in this project probably not even that (`coding-standards`).

## 7. Keeping the set usable

- **Number sequentially**, never reuse a number.
- **Keep an index** — number, title, status. Superseded ones stay in it, marked.
- **Review the set when starting in an unfamiliar area** — cheaper than reconstructing the reasoning
  from code (`reading-before-changing.md`).
- **Expect most to be short.** A decision that needs three pages is probably a design document with a
  decision inside it — write the design document and let the ADR point at it.
