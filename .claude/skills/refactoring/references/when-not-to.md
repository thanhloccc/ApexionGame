# When not to refactor

Refactoring is not free virtue. It costs time, carries risk, invalidates in-flight work, and produces
merge conflicts for everyone else in the area.

## 1. Do not, when

| Situation | Why |
|---|---|
| The code is about to be deleted or replaced | You are polishing something on its way out |
| Nobody reads it, nobody changes it | Cost of ownership is near zero; there is no return |
| The goal is an abstraction nothing has asked for | Speculative generality — see §3 |
| You are in the middle of something urgent | The worst moment; it is how refactors get blamed |
| The only justification is that it would look nicer | Taste is not a cost saving |
| There is no safety net and none can be built | That is a rewrite — `verification.md` §5 |
| You do not understand the code yet | Read it first — `debugging`, and `system-design/references/reading-before-changing.md` |
| It is the day before a release | Obvious, and it still happens |

## 2. The strongest reason not to: it is not in the way

The honest test:

> **When did this last cost anyone time?**

If the answer is "never" or "I do not know", the debt has no interest rate
(`system-design/references/technical-debt.md`). Ugly code that nobody touches costs nothing.
Beautiful code that nobody touches also earns nothing.

Refactoring effort belongs where **change is happening** — the files that get edited every week, the
area the next three features land in. Those pay back immediately. A tidy-up in a quiet corner is
recreation.

## 3. Speculative generality

The most expensive kind of unnecessary refactor: adding an abstraction for a variation that has not
arrived.

The failure is not the abstraction being wrong — it is that it is now **load-bearing** for code that
does not need it. Everyone reading pays the indirection cost. Everyone changing it works through a
seam designed for a case that never came.

The test: **is there evidence this variation is coming?** A written requirement, a planned feature, a
second case that already exists. "It might" is not evidence.

Two concrete cases is usually when the shape becomes visible. One case plus imagination is usually
wrong about where the seam goes.

## 4. The inverse trap: deferring until forced

Equally real, and it produces the other kind of disaster.

Every refactor deferred stays deferred until the code **must** change — which is when a feature is
in flight, and there is no time. At that point the choice is: refactor under time pressure with a
feature half-built on top, or add to the mess. Both are bad, and the second compounds.

Signs the deferral has gone too far:

- everyone routes around a file instead of changing it;
- estimates in one area are consistently and unexplainably high;
- new features get shaped by what the existing structure allows, not by what is wanted;
- the same bug class keeps recurring in the same place.

That last one is the clearest. **A recurring bug class is a structural problem** presenting as a
series of individual bugs, and fixing them one at a time is paying interest forever.

## 5. Now, later, or never

| Do it **now** | Do it **later** | **Never** |
|---|---|---|
| It blocks the current work | It is in an area about to be worked on | The code is being deleted |
| It is small and the safety net exists | It needs a safety net built first | Nothing changes there |
| A bug class keeps recurring there | It is large enough to need its own planning | The only gain is aesthetic |
| You are already changing that code for a real reason | Something urgent is in flight | The abstraction has no evidence |

"Later" needs a **trigger**, not a date: *"before the next feature lands in this area"*, *"the next
time this bug recurs"*. A dated later is a never with extra steps.

## 6. Saying no to a refactor request

When asked for a refactor that fails the tests above, do not just decline — that is unhelpful. Give
the reasoning, and offer what is actually useful:

```
This area has not been changed in months and nothing is queued for it, so the
restructure would not pay back — and it would conflict with anything in flight
there. Two things I would do instead:

  - the recurring null case in <X>: that is the bug class that actually costs
    time, and it is a small fix
  - if a feature is planned here, tell me and this becomes worth doing first

If you want the refactor anyway, I will do it — it is about a day, and it will
invalidate any open work in these files.
```

State the cost, propose the alternative, and **do it if the decision stands.** The decision is not
yours; the information is your job.
