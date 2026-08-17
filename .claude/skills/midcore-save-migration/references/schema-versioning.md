# Schema versioning

## 1. What a version number means

> The version identifies **the shape of the data**, not the version of the app that wrote it.

Two builds can write the same schema version; that is normal and correct. What must never happen is
two different shapes sharing one version — at that point no reader can tell them apart, and the
information needed to disambiguate does not exist anywhere.

So the version is bumped when the **shape** changes, and only then:

| Change | Bump? |
|---|---|
| Add a field | **yes** |
| Remove a field | **yes** |
| Rename a field | **yes** |
| Change a field's type | **yes** |
| Change the meaning of an existing field's values | **yes** — a shape change even if the type is identical |
| Change a collection's key type | **yes** |
| Change the default of a field for *new* saves only | no — old data is unaffected |
| Fix a bug in code that reads the data | no |
| Change balance values in content | no — that is content, not save shape |

The fifth row is the one that gets missed. If `currency` changes from "gold" to "gems", nothing about
the type changed, but every existing save now means something different. That is a shape change.

## 2. Where the version lives

Three requirements, in priority order:

1. **Readable before anything else is parsed.** The version determines how to interpret the rest, so
   it cannot be inside the part whose interpretation depends on it.
2. **Readable before decryption succeeds** (see `safety-and-recovery.md` §4).
3. **Per persisted unit**, not one global number — separate blobs evolve at different rates, and one
   shared version forces a bump on all of them for a change to one.

A version at the start of the file or in an unencrypted header satisfies all three. A version nested
inside the payload satisfies none.

## 3. Designing v1 so later versions are cheap

Decisions made once, at the first format, that pay off for years:

- **Version field present from v1.** Adding one later means guessing whether an unversioned file is
  v1 or something else — a guess made on real player data.
- **Reader tolerates unknown fields.** A player who ran a newer build and rolled back must not lose
  everything. Strict readers make rollback catastrophic.
- **Reader tolerates missing fields**, applying documented defaults. This is what makes "add a field"
  a trivial migration instead of a breaking one.
- **Separate blobs by change rate.** Settings, progress and inventory evolve differently; separating
  them means a change to one does not version the others. But see `safety-and-recovery.md` §3 —
  multiple blobs mean partial-failure handling.
- **No absolute paths, machine ids, or timestamps-as-identity** in saved data. They break on
  reinstall, device change, and cloud restore.
- **Collections keyed by stable typed ids**, never by display name or index. An index changes when
  content is reordered; a name changes when it is retranslated.

If the profile says `save.shipped_versions: []`, all of this is still free. That will not be true
again.

## 4. Deprecate, never reuse

When a field's meaning must change:

```
BAD   v4: `level` means character level
      v5: `level` now means account level
      → every existing save silently reports the wrong number. Nothing fails.

GOOD  v4: `level` means character level
      v5: `level` is deprecated, still read; `account_level` added
      → the v4→v5 migration decides what account_level should be for existing players
```

Why reuse is uniquely bad: it produces **no error at any layer**. The type matches, the parse
succeeds, the value is plausible. It is discovered weeks later by a player noticing their number is
wrong, and by then the original value has been overwritten.

Deprecated fields are cheap — a few unused bytes. Keep them readable until every version that wrote
them has aged out of the population, which is longer than you think.

## 5. Version skipping is normal

Players skip versions routinely: they do not update every release, they reinstall, they restore from
a backup of a build from months ago.

So the chain must handle **any shipped version → current**, not merely N-1 → current. That is why
migrations are chained `N → N+1` steps rather than one branching function: a chain composes
automatically for arbitrary gaps, and each step is individually testable.

Practically:

- never delete a migration step, however old;
- never renumber versions;
- never "clean up" the chain by collapsing several steps into one — the collapsed version is a new,
  untested function against every old fixture.

## 6. Retiring an old version

Eventually a version is old enough that supporting it costs more than it is worth. Refusing to load
it is a **product decision with a player-visible outcome**, never a quiet code deletion.

Before retiring one:

1. Measure how many players are actually on it. If the number is unknown, that is the first task.
2. Decide what those players see. **Never a silent wipe** — an explicit message, and a recovery path
   if one exists.
3. Keep the fixture and the migration step in the repo even after retiring support, so the format
   remains diagnosable when a support ticket arrives with an old file.

The default answer is **do not retire**. The chain is cheap; a wiped player is not.
