---
name: midcore-save-migration
description: REQUIRED before changing the shape of anything persisted to a player's device — adding, removing, renaming or retyping a field, splitting or merging a save blob, re-keying a collection, or changing how data is serialized or encrypted. Covers schema versioning, the migration chain, forward and backward compatibility, corruption and partial-failure recovery, and testing migrations with golden fixtures. Load it BEFORE editing any type that is serialized to disk, not after the change is written. This is the one area where a mistake cannot be fixed in a patch — the damaged data is already on the player's device and the original is gone. Triggers on "save", "load", "persistence", "player data", "profile", "progress", "schema", "migration", "version", "serialize", "backward compatible", "wipe", "corrupt", "data loss", "save file", and Vietnamese phrasing "lưu game", "đổi schema", "mất dữ liệu", "mất tiến trình", "tương thích bản cũ", "file save", "hỏng save".
---

# Save schema versioning and migration

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

Check `save.status` and `save.shipped_versions` in the profile before touching anything. A project
with no shipped format is at the cheapest possible moment to get this right; a project with shipped
versions is carrying obligations to every one of them.

## The one rule

> **A shipped save format is forever.**

Every other mistake in a game is recoverable: ship a patch, re-import content, hotfix the config.
Not this one. When a migration loses data, the data is already gone from the player's device and the
original no longer exists. There is no fix — only an apology.

"Shipped" means **reached a device you do not control** — including QA builds, TestFlight, internal
distribution. Not "released on a store".

## The four non-negotiables

### 1. Every persisted type carries an explicit version

Not implied by the app version, not inferred from which fields are present. An explicit number,
written into the data.

Bumped **in the same change** as the shape edit. A shape change that ships without a version bump is
unrecoverable: two different shapes now claim the same version, and no reader can tell them apart.

### 2. A shipped field name is never reused with a different meaning

Once `level` has shipped meaning character level, it means that forever. If it must become something
else, **deprecate and add** — leave the old field readable, introduce a new name.

Reuse is uniquely dangerous because nothing fails. The old value parses into the new field and is
silently wrong.

### 3. Migrations are pure functions from N to N+1, chained

`v3 → v4 → v5 → v6`, each step small and independently testable. Never one function that branches on
every historical version — that grows a case per release, is never fully tested, and the
old branches are exactly the ones no one exercises.

### 4. Migrate to a new file and swap on success

Never in place. A crash, a kill, or a full disk mid-write must leave the original intact.

## Before you change a persisted type

1. **Is it actually persisted?** Trace it to disk. Types reachable from a save root are persisted
   even if that was not the intent.
2. **Has this format shipped?** Profile → `save.shipped_versions`. If empty, you are free — but the
   version field and the migration harness go in **now**, with the first format, because
   retrofitting them after the first ship means guessing what v1 looked like.
3. **Which change is it?** Add / remove / rename / retype / restructure / re-key — each has a recipe
   and a distinct way of silently losing data. See `references/migration-recipes.md`.
4. **Can the old value be reconstructed?** If not, the migration must decide what those players get,
   and that is a design decision, not an implementation detail. Surface it.
5. **Does it reference content ids?** Deleting a content row a save can name is a save migration —
   see `midcore-data-pipeline/references/validation.md` §5.

## The workflow

| Step | Done when |
|---|---|
| 1. Bump the version constant | New version declared, in the same change as the shape edit |
| 2. Write the `N → N+1` migration | Pure function; every field either carried, defaulted, or explicitly dropped with a reason |
| 3. Register it in the chain | Loading vN runs every step up to current, in order |
| 4. Add a golden fixture for the **previous** version | A real save file from vN, committed, never regenerated |
| 5. Test every shipped version → current | Not just N-1 → N. See `references/testing-migrations.md` |
| 6. Verify recovery paths | Interrupted migration leaves the original loadable |

Step 4 is the one that gets skipped and the one that makes the rest enforceable. Without a fixture
from the real old format, a migration is only tested against what you *believe* the old format was.

## The failure modes, named

| Failure | How it shows up | Prevented by |
|---|---|---|
| Shape changed, version not bumped | Two formats share a version; readers guess | Non-negotiable 1 |
| Field name reused | Old value parses into new meaning, silently wrong | Non-negotiable 2 |
| Branching migration | Old paths untested, break years later | Non-negotiable 3 |
| In-place migration interrupted | Save half-migrated, unloadable | Non-negotiable 4 |
| Unreadable save treated as new player | **Progress wiped**; the worst possible outcome | `references/safety-and-recovery.md` |
| Version unreadable before decryption | Wrong-key save indistinguishable from corruption | `references/safety-and-recovery.md` §4 |
| Only N-1 → N tested | Players who skipped a version get an untested path | `references/testing-migrations.md` |
| Content id deleted, saves still name it | Dangling reference on load | `midcore-data-pipeline` |

## Never do this

- **Never reset an unreadable save to a new game.** Preserve the file, fail visibly, let support
  recover it. A wipe is worse than an error screen, always.
- **Never delete a golden fixture** because the format is old. Old formats are exactly what the
  chain must still handle.
- **Never make the reader strict about unknown fields.** Tolerate them — a player who ran a newer
  build, then rolled back, must not lose everything.
- **Never migrate on a background thread while gameplay reads the same data.**
- **Never ship a migration that has not been run against a real file from the previous version.**

## This skill does not own

| Question | Owner |
|---|---|
| How to declare a persisted type, the store, the serializer | `structure_naming` authority (see profile) |
| How to run the migration tests | `unity_operations` authority |
| Content table shape and content ids | `midcore-data-pipeline` |
| Which tests gate a merge | `midcore-testing` |
| Server-authoritative or cloud-synced state | out of scope — this is local persistence |

## References

- `references/schema-versioning.md` — what a version means, when to bump, deprecation over reuse,
  and designing v1 so later versions are cheap.
- `references/migration-recipes.md` — one recipe per change kind, each with the trap that makes it
  silently lossy.
- `references/safety-and-recovery.md` — write-and-swap, backups, partial failure across blobs,
  unreadable saves, and the encryption/version ordering trap.
- `references/testing-migrations.md` — golden fixtures, the full version matrix, and asserting on
  semantic content rather than bytes.
