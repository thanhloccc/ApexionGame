# Reading before changing

> **Do not propose a change to code you cannot draw an ownership table for.**

Changing code you have not understood is guessing with a compiler. It is also how invariants get
broken silently — nothing fails, because the invariant was never checked anywhere.

## 1. The procedure

Five steps, ending in an artifact rather than a feeling.

### 1 — Find the boundaries

What calls in, what it calls out. The public surface, and what it depends on.

Quick approximations:

```bash
# who calls into this area
grep -rn "TypeName\." --include=*.cs . | grep -v "/TypeName"

# what this area reaches for
grep -rhoE "^using .*;" path/to/area/ | sort | uniq -c | sort -rn
```

The second is often the fastest read of a module's real dependencies — far faster than reading it,
and it shows what the design *actually* touches rather than what it claims to.

### 2 — Find who owns the state

Build the ownership table (`state-ownership.md` §2) for this area, **even though the original author
never wrote one**. Fields, statics, singletons, serialized data.

This is the highest-value step. It is where you discover that two things write the same field, or
that a piece of state has no owner — which is usually the reason the code is hard to change.

### 3 — Find the invariants

What must always be true. They are rarely documented; look in:

| Where | Example |
|---|---|
| Guard clauses and early returns | `if (count == 0) return;` — count is expected non-zero downstream |
| Assertions and validation helpers | direct statements of an invariant |
| Initialisation order | something must exist before something else |
| Names | `_sortedItems` means sorted is load-bearing |
| Comments | in this project comments are rare, so an existing one is signal |
| The one weird line | usually a fix for a case you have not seen yet |

**Write them down.** These are what a change can break without any test failing.

### 4 — Find the patched places

Areas with many small successive changes. `git log --oneline -- <path>` and look for clusters.

A cluster of small fixes in one place usually means **the model is wrong**, not that the code is
sloppy. Each fix handled a case the model did not account for. That is worth knowing before adding
the next fix — you may be about to add the sixth.

### 5 — Only now, propose

And state which invariants from step 3 the change could touch.

## 2. Time-boxing

This is not licence for unbounded study. Match the reading to the change:

| Change | Read |
|---|---|
| One-line fix in a well-understood spot | boundaries only |
| A behavioural change | boundaries + ownership |
| Structural change | all five steps |
| Anything touching persisted state | all five, plus `midcore-save-migration` |

If step 2 cannot be completed in reasonable time, that is itself the finding — **an area whose state
ownership cannot be determined is an area where any change is a risk**, and that belongs in the
report rather than being pushed through.

## 3. What to record

Reading is expensive; do it once. Leave behind:

- **the ownership table** — the most reusable artifact;
- **the invariant list**;
- **anything surprising**, especially where reality differs from what a name suggests.

Where it goes: the feature doc if there is one, an ADR if the finding is a decision
(`decision-records.md`), otherwise the change description. **Not only in a conversation** — the next
person will otherwise redo this work and reach different conclusions.

## 4. Related

| Purpose | Skill |
|---|---|
| Understanding in order to **change structure** | then `refactoring` |
| Understanding in order to **find a bug** | `debugging` — narrowing, not mapping |
| Understanding the **assembly graph** rather than one area | `midcore-assembly-architecture` |
| Understanding **content shape** | `midcore-data-pipeline` |

The distinction from `debugging`: there, you are chasing one specific wrong behaviour and can ignore
everything not on the path. Here, you are building a map because you are about to change something —
different goal, different depth, different stopping condition.
