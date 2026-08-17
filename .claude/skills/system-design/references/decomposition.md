# Decomposition

## 1. Split by what changes together

Not by technical layer, not by screen, not by what sounds tidy.

> **Things that change for the same reason belong together. Things that change for different reasons
> belong apart.**

The test is temporal: *when this changes, what else must change?* If two pieces always change
together, the boundary between them is ceremony. If one changes weekly and the other never, they are
different things regardless of how related they sound.

## 2. The seams that pay in a game

Three, and they pay because each changes for a genuinely different reason at a different rate:

| Seam | Changes because | Rate |
|---|---|---|
| **Simulation** — rules, state, decisions | design changes the rules | steady |
| **Presentation** — view, audio, effects, input binding | the game must look or feel different | fast |
| **Persistence** — what is stored and in what shape | rarely, and expensively | slow, permanent |

Why this specific split earns its cost:

- **Simulation without presentation is testable.** This is the one property that makes rules
  verifiable at all — see `midcore-testing`.
- **Presentation changes constantly** and must not be able to break rules.
- **Persistence is permanent.** Coupling it to either of the others means every change to them is a
  migration — `midcore-save-migration`.

**Presentation may read simulation state. Simulation must never read presentation.** That direction
is the whole value of the seam.

## 3. Naming the pieces

A piece you cannot name in one noun phrase is not a piece yet.

| Smell | Means |
|---|---|
| The name contains "and" | Two pieces |
| The name is `*Manager`, `*Helper`, `*Controller`, `*System` with nothing else | The concept is unnamed |
| The name is a screen | It is a view over concepts, not a concept |
| The name is a layer (`DataLayer`, `LogicLayer`) | That is a layer, not a piece |

**Name the concept, not the role.** `DamageResolution`, `EquipmentSlots`, `QuestProgress` — each says
what it *is*, so its boundary is obvious and things that do not belong are obviously outside.

## 4. Too big

- Describing it requires "and".
- Testing it requires standing up half the game.
- Two unrelated changes both touch it.
- It owns state from more than one lifetime (`state-ownership.md` §4).
- Two people cannot work in it at once without colliding.

**The ownership table is the sharpest detector.** A piece owning state that belongs to two different
concerns is two pieces, and the table makes that visible before any code exists.

## 5. Too small

Less discussed and equally real:

- A piece that only forwards calls.
- Following one behaviour means opening five files.
- Every change touches three pieces at once.
- An interface with one implementation and no prospect of a second.
- A name that had to be invented because the split needed one.

That last one is the tell for a split made structurally rather than conceptually.

> **The right size is the size that reads well, not the smallest size.**

## 6. Interfaces and abstraction

Introduce an interface when at least one is true:

- there are **two real implementations** — not one and an imagined one;
- a boundary must be **testable** by substitution;
- the direction of a dependency must be **inverted** for layering reasons
  (`midcore-assembly-architecture`).

Not because a piece "should be abstracted". A one-implementation interface is indirection everyone
pays for and nobody uses — `refactoring/references/when-not-to.md` §3.

**Two concrete cases is when the shape becomes visible.** One case plus imagination usually puts the
seam in the wrong place, and a wrongly-placed seam is worse than none: it must be removed before the
right one can exist.

## 7. Worked shape

A combat feature, decomposed:

```
Simulation
  DamageResolution     owns: nothing durable. Pure: (attacker, target, context) -> outcome
  HealthState          owns: current health per entity          persisted
  StatusEffects        owns: active effects per entity          persisted
  TargetSelection      owns: current target per entity          not persisted

Presentation
  CombatFeedback       owns: nothing. Reads outcomes, plays effects
  HealthDisplay        owns: nothing. Reads HealthState

Persistence
  (no new piece — HealthState and StatusEffects are persisted through the existing store)
```

What the decomposition bought:

- **`DamageResolution` owns nothing**, so it is a pure function of its inputs — the most testable
  shape there is, and the rules are the part most worth testing.
- **`HealthState` and `StatusEffects` are separate** because they have different shapes and different
  change rates, even though both are per-entity and persisted.
- **`TargetSelection` is separate** because it is *not* persisted — a different lifetime is a
  different piece.
- **Presentation owns nothing at all.** Delete it and simulation still compiles and still passes its
  tests. That is the check.
- **No `CombatManager`.** Nothing coordinates for the sake of coordinating; if something must
  sequence these, it belongs in composition — `midcore-assembly-architecture`.
