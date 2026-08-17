# Migration recipes

One recipe per kind of change. Each ends with **the trap** — the specific way that change silently
loses data.

Every migration is a pure function `vN → vN+1`. No I/O, no clock, no randomness, no service calls —
it must produce identical output for identical input, forever, because it will be run years from now
against files written years ago.

---

## Add a field

The easy one. Old data has no value, so the migration supplies one.

1. Declare the field with a **documented default**.
2. The migration sets it explicitly — do not rely on the language's zero value.
3. Decide whether the default is *correct* for existing players or merely *safe*.

**The trap:** a zero-value default that happens to be meaningful. Adding `hasCompletedTutorial` and
defaulting to `false` sends every existing player back through the tutorial. Adding `prestigeLevel`
defaulting to `0` is fine. The type does not tell you which case you are in — the design does.

---

## Remove a field

1. Confirm nothing reads it — including debug tooling, analytics and support tools.
2. The migration drops it explicitly, with a comment saying why.
3. The reader tolerates its presence in old data.

**The trap:** removing a field whose value cannot be reconstructed, then discovering next quarter
that a new feature needed it. Once dropped, it is gone from every migrated save. If there is any
doubt, **keep it** — deprecated and unread. Bytes are cheap; the value is unrecoverable.

---

## Rename a field

Read from the old name, write to the new one.

**The trap:** a rename that is really a meaning change. If `coins` becomes `softCurrency` and the
units are the same, it is a rename. If the value should now be measured differently, it is a retype
plus a semantic change — and copying the number across is a silent corruption of every save. Ask what
the *values* mean, not what the field is called.

---

## Change a field's type

1. Convert explicitly, with the failure case decided in advance.
2. Handle values that do not convert — do not let a failed conversion produce a default that looks
   legitimate.

**The trap:** lossy numeric conversion. Float to int truncates; a wider to narrower integer wraps or
clamps. Both produce a plausible number. Decide rounding direction deliberately and say so in a
comment — "it rounded down" is not something anyone can reconstruct later from the data.

---

## Split one blob into two

1. Read the old blob whole.
2. Write both new blobs.
3. Swap **atomically as a set** — see `safety-and-recovery.md` §3. Half a split is worse than no
   split.

**The trap:** a field both halves need, assigned to one and forgotten by the other. Enumerate every
field and assign each to exactly one destination — or deliberately to both — before writing code.

---

## Merge two blobs into one

1. Read both. **Decide what happens when only one exists** — a real case, from a partial failure or
   an interrupted earlier migration.
2. Resolve conflicts by an explicit rule, never "whichever loads last".

**The trap:** assuming both blobs are at the same version. They may not be, if they were separated
precisely so they could evolve independently. Migrate each to its own current version first, *then*
merge.

---

## Re-key a collection

The riskiest common change: a dictionary keyed by one id type now keyed by another.

1. Build the mapping old-key → new-key explicitly.
2. **Decide what happens to entries with no mapping** — an id retired from content, a key from a
   feature that was removed.
3. Detect collisions: two old keys mapping to one new key. Decide whether to sum, keep the maximum,
   or fail.

**The trap:** silently dropping unmapped entries. A player's inventory quietly loses items whose ids
no longer resolve. Count them and act on the count — log, preserve in a quarantine field, or fail the
migration. Never a silent skip inside a loop.

---

## Change how a collection is ordered

If order carries meaning — loadouts, quick slots, party order — it is data, and a reordering is a
shape change.

**The trap:** order that was accidental but became load-bearing. If the game ever displayed a
collection in insertion order and players arranged it deliberately, that order is now theirs. Storing
an explicit order field from v1 avoids the whole question.

---

## Change the serialization format or encryption

1. The **version must be readable before the new mechanism is applied** — see
   `safety-and-recovery.md` §4.
2. The reader detects the old form and reads it with the old mechanism.
3. Keep the old reader for as long as any save might use it. Forever, in practice.

**The trap:** a wrong-key or wrong-format save being indistinguishable from a corrupt one. Both fail
to decode, and the recovery paths are opposite — one wants the old reader, the other wants recovery
from backup. Make the version readable independently so the two cases separate.

---

## Fix a bug that wrote bad data

A past build wrote values that were wrong. A data repair, and it needs its own version bump.

1. Identify affected saves **precisely** — the version range that had the bug.
2. Decide whether the correct value is reconstructable. If not, decide what those players get, and
   surface that as a product decision.
3. Apply the repair only to affected versions. Do not re-run it on saves that were always correct.

**The trap:** a repair that runs unconditionally and "fixes" already-correct data. Gate it on the
version range that had the bug, and test both sides of the boundary.

---

## The checklist for any migration

- [ ] Version bumped, in the same change as the shape edit
- [ ] Pure function — no clock, no randomness, no I/O, no services
- [ ] Every field carried, defaulted, or explicitly dropped with a stated reason
- [ ] Non-convertible values handled, not defaulted into plausibility
- [ ] Dropped or unmapped entries counted, not silently skipped
- [ ] Registered in the chain, in order
- [ ] Golden fixture from the previous version committed
- [ ] Tested from **every** shipped version, not just N-1
- [ ] Interruption leaves the original loadable
