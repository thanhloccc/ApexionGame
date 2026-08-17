# Safety and recovery

Migrations run on devices you do not control, at moments you do not choose — low battery, no disk,
an incoming call, a force-quit. Assume interruption at every step.

## 1. Write and swap, never in place

```
1. read + validate the original          ← original still the only copy
2. migrate in memory
3. write to a temporary file
4. flush and verify the temporary file    ← re-read and parse it
5. atomically replace the original
6. delete the temporary
```

Interruption before step 5 leaves the original untouched. Interruption during step 5 is why the
replace must be atomic — a rename within the same volume, not a copy over the top.

**Step 4 is the one people skip.** A write that returned success is not a file that parses. Re-read
and parse the temporary before letting it replace anything, or a failed serialization overwrites a
good save with garbage.

## 2. Keep a backup across the migration

Retain the pre-migration file until the migrated one has been successfully loaded **by the game**,
not merely written.

- Keep at least the immediately previous version.
- Keep it long enough to survive the session — a migration bug can surface minutes later, in a
  different system, when a field turns out to be wrong.
- Name backups by version so support can identify what they hold.
- Delete old backups on a bounded schedule, so this does not grow forever.

The cost is disk space. The alternative is a support queue with no recovery option.

## 3. Multiple blobs and partial failure

Splitting saves by change rate means a migration can succeed for one blob and fail for another.

**Decide the policy before it happens:**

| Policy | Behaviour | Use when |
|---|---|---|
| All-or-nothing | Any blob fails → roll all back | Blobs are semantically coupled — progress and inventory |
| Per-blob | Each migrates independently | Blobs are genuinely independent — settings versus progress |

All-or-nothing is the safe default. Per-blob is legitimate only when a blob failing to migrate leaves
the game in a coherent state — for example, settings reverting to defaults while progress is intact.

**Never leave blobs at mismatched versions** unless per-blob was deliberately chosen and the readers
tolerate it. Mismatched versions after an all-or-nothing failure is the worst state: not a clean
rollback, not a clean upgrade.

## 4. Encryption and version ordering

The trap that produces unrecoverable support tickets:

> If the version can only be read **after** decryption, a save encrypted with an old key is
> indistinguishable from a corrupt file.

Both fail to decode. The correct responses are opposite — one needs the old key, the other needs
recovery from backup — and there is no information left to choose between them.

**So: the version is readable without decrypting the payload.** An unencrypted header, or a plaintext
prefix. It leaks nothing an attacker cannot determine from the app binary anyway.

Related rules:

- **Never derive the encryption key from something that can change** — a device id, an install id, a
  user name. The player changes phones and their save is unreadable forever.
- **Key rotation is a schema change** and gets a version bump, with the old key retained for reading.
- **Encryption is not integrity.** A decrypted payload can still be garbage. Validate after
  decrypting; do not treat a successful decrypt as a successful load.

## 5. When a save cannot be read

The single most important rule in this file:

> **Never silently reset an unreadable save to a new game.**

A player who sees an error and contacts support can be helped. A player whose progress silently
vanished has lost it, and will say so publicly.

The order to try:

1. **Retry once.** Transient I/O failures are real, especially on mobile.
2. **Try the backup** (§2). If it loads, tell the player they may have lost recent progress — do not
   pretend nothing happened.
3. **Try a partial recovery.** If some blobs load, load them and report which did not.
4. **Preserve the unreadable file** under a distinct name. Never delete it, never overwrite it. It is
   the only evidence, and support may be able to recover it by hand.
5. **Fail visibly.** An explicit message. Ideally a way to send the file to support.
6. **Starting fresh is the player's choice**, made explicitly, never the fallback branch of a
   `catch`.

The anti-pattern to recognise, in any form:

```
try { save = Load(); }
catch { save = NewGame(); }     // ← this line has destroyed real players' accounts
```

It looks like robustness. It is data loss with a friendly face.

## 6. Diagnostics

When a migration fails in the field, you have whatever was logged and nothing else.

Log, at minimum:

- the version found and the version expected;
- which migration step failed;
- the size of the file and whether it parsed at all;
- whether a backup existed and whether it loaded;
- **never the save contents** — it is player data, and logs travel.

Record a counter of load failures by version. A migration bug affecting one old version is invisible
in aggregate crash reporting and obvious in that counter.

## 7. Cloud sync and multiple devices

If saves sync across devices, migration gains a conflict dimension:

- A device on an old build may write an **older schema after** a newer one has synced. The newer
  device must not treat older-but-newer-timestamped data as authoritative without thought.
- **Never migrate a save that is not local and current.** Fetch, migrate locally, then write back.
- Decide the conflict rule explicitly — most-recent-write, most-progress, or ask the player. "Most
  recent" quietly loses progress when a player's second device was behind.

Server-authoritative state is out of scope here; this covers local saves that happen to be synced.
