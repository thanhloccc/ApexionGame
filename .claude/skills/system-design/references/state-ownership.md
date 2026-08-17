# State ownership

The first question, and the most expensive one to get wrong. Structure can be refactored; ownership
spreads into every call site and every save file.

## 1. The rule

> **Every piece of state has exactly one owner.** The owner is the only thing that writes it.
> Everything else reads, or asks the owner to change it.

Not "one place that mostly writes it". One.

**The test:** for each piece of state, ask —

> *If two places wrote to this in the same frame, who wins?*

If you cannot answer instantly, it has no owner yet. That is the bug, before any code exists.

## 2. The ownership table

The artifact the feature gate requires. It goes in the plan.

| State | Owner | Readers | Who can request a change | Lifetime |
|---|---|---|---|---|
| current health | combat sim | HUD, AI, save | combat sim only, via damage events | per-entity, saved |
| equipped weapon id | inventory | combat sim, HUD | player action → inventory | per-profile, saved |
| aim direction | input translation | combat sim | nobody — derived from input each frame | per-frame, not saved |
| enemy list | spawn system | AI, HUD | spawn system only | per-encounter, not saved |

What each column forces you to confront:

- **Owner** — one name. "Combat and inventory" is not an answer; it means the state is really two.
- **Readers** — a long list is fine. A reader that *also* writes is the failure this table exists to
  catch.
- **Who can request a change** — the intended path. If the answer is "anyone", there is no owner.
- **Lifetime** — see §4, and it decides whether `midcore-save-migration` applies.

Filling this in usually changes the design. That is the point of doing it before code.

## 3. Authoritative versus derived

**Authoritative** state is the truth. **Derived** state is computed from it.

> **Never store derived state alongside its source.** Compute it, or cache it with an explicit
> invalidation owned by the same owner.

Two copies of one truth will disagree. Not might — will, on the path nobody tested. This is the
mechanism behind most "the UI shows the wrong number" bugs, and the fix is never in the UI.

| Authoritative | Derived |
|---|---|
| current health | is-alive, health percentage, bar width |
| inventory contents | total weight, is-full, item count |
| equipped items | computed stat totals |
| quest step | which objectives are visible |

Caching a derived value is legitimate when it is measurably expensive. Then: **the cache belongs to
the owner of the source**, and invalidation happens where the source changes — never in the reader.
Whether it is expensive enough to warrant a cache is `midcore-perf-budget`, and it needs a
measurement.

## 4. Lifetime

Every piece of state answers: **created when, destroyed when, and what does it survive?**

| Lifetime | Survives | Implication |
|---|---|---|
| Per-frame | nothing | derived; do not store |
| Per-encounter / per-scene | scene load: no | must be reconstructible |
| Per-session | scene change: yes; app restart: no | needs an explicit owner across scenes |
| Persisted | app restart, reinstall, update | **its shape is a permanent contract** |

That last row is the crossover that catches people. **The moment state is persisted, its shape is
governed by `midcore-save-migration`** — versioned, migrated, and never reshaped casually. Deciding
what gets persisted is a design decision, made here, with that cost understood.

Rule of thumb: **persist the least that reconstructs the rest.** Every persisted field is a field
that must be migrated forever.

## 5. Failure modes

| Failure | Looks like | Fix |
|---|---|---|
| **Two owners** | Two systems write the same field; the last writer wins by accident | Pick one; the other requests changes |
| **No owner** | A static or singleton everything writes | Give it an owner, or split it into per-owner pieces |
| **Owner in the wrong layer** | Rules state owned by a view; simulation state owned by the save layer | Move ownership to where the rules are — see `decomposition.md` |
| **Owner cannot be tested** | The owner is bound to a scene object, so no rule can be tested without one | Ownership belongs in plain logic; the scene object references it |
| **Derived stored** | A number is right in one place and stale in another | Compute it; §3 |
| **Ownership by convention** | "Only the combat system writes this" — enforced by nothing | Make it enforceable: private state, mutation only through the owner's API |

The last one is worth naming. **Ownership that exists only in a document is not ownership.** If any
code can write the field, some code eventually will — usually during a deadline, in a way that
works.

## 6. Ownership and communication

Ownership determines the mechanism, not the other way round:

- A reader that needs the current value → **query the owner**, or read a read-only view.
- A reader that needs to know it changed → **the owner publishes**; the reader subscribes.
- A system that wants to change it → **asks the owner**; the owner decides.

If a design needs to write state it does not own, that is not a communication problem — the
ownership is wrong. See `communication.md`.

## 7. Checklist

- [ ] Every piece of state appears exactly once in the ownership table
- [ ] Every row has exactly one owner
- [ ] No reader also writes
- [ ] Every derived value is marked derived and is not stored
- [ ] Every lifetime is stated
- [ ] Everything persisted is deliberate, and its migration cost is accepted
- [ ] Ownership is enforced by visibility, not by convention
- [ ] For each row: *"if two places wrote this in one frame, who wins?"* has an instant answer
