# Import-time validation

> **Fail the import, not the play session.**

Content is authored by people who cannot compile it and is rarely reviewed line by line. Validation
is the only thing standing between a typo and a player-facing crash.

## 1. The six tiers

Run in order — cheapest and most fundamental first, so the error surfaced is the cause and not a
downstream symptom.

### Tier 1 — Schema · **hard fail**

Every cell parses to its declared type. Required columns present. No unexpected columns silently
ignored.

The last one matters more than it looks: a column the importer does not recognise is usually a
rename that half-happened, and ignoring it means the designer's edits go nowhere while the sheet
looks correct.

### Tier 2 — Identity · **hard fail**

- No duplicate ids within a table.
- No empty or whitespace-only ids.
- Ids match whatever format the project fixed.

Duplicate ids are especially nasty: the importer usually takes one of them, so the row a designer
edited may simply not be the row the game uses, and nothing looks wrong.

### Tier 3 — Referential · **hard fail**

Every foreign id resolves to a row that exists. Every one, every table, no exceptions.

This is the tier that pays for the whole system. It is also the one most often skipped, because at
three tables it feels unnecessary — and by the time it obviously matters there are forty tables and
a backlog of existing violations to fix first.

### Tier 4 — Domain · **hard fail**

Per-row value rules: ranges, non-negative where negatives are meaningless, non-empty display keys,
enum members that exist, units within sanity bounds.

Keep these **narrow and certain**. A domain rule that fires on legitimate content gets loosened once
and then ignored forever. If a bound is a guess, it belongs in tier 6.

### Tier 5 — Invariant · **hard fail**

Rules spanning rows or tables — the ones no single-cell check can see:

- every quest chapter has at least one quest;
- every progression curve is contiguous, monotonic where required, and covers the level cap;
- every reward references something obtainable;
- every state referenced by a transition exists;
- no reference cycle where the design forbids one.

These encode the design's actual assumptions and are worth the most per rule. Add one every time a
content bug reaches a play session — that is the cheapest moment to know which invariant was missing.

### Tier 6 — Advisory · **warn**

- rows nothing references;
- values that are outliers versus their column;
- missing translations;
- content staged ahead of the feature that uses it.

These warn because the condition is often legitimate. Keep the warning list short enough that someone
reads it; a hundred routine warnings is the same as none.

## 2. Hard fail means hard fail

The pressure to downgrade a tier 1–5 failure to a warning always arrives, usually as *"this is
blocking someone and their content is fine"*.

It is not fine — the check fired. And a warning in a log nobody reads is not a control; it is the
absence of one, with paperwork.

If a check is wrong, **fix or delete the check** in its own change, deliberately. Do not weaken the
mechanism to route around one instance. The moment tier 3 becomes advisory, a dangling reference will
ship, and it will be found by a player.

## 3. Writing a useful failure

Compare:

```
Import failed: invalid reference
```

```
ranged_weapon (row 47, id `rifle_mk2`): column `ammo_id` = `bullet_smal`
  does not exist in table `ammo`.
  Did you mean `bullet_small`?
```

The second names the table, the row **and its id** (row numbers shift when rows are inserted), the
column, the offending value, and the expected target. The suggestion is optional; everything before
it is not.

Other rules that make failures usable:

- **Report all failures, not the first.** One-at-a-time fixing across forty tables is miserable and
  slow. Collect, then report together.
- **Group by table**, so one person can fix one sheet.
- **Cap the output** — a thousand identical errors from one missing table should be summarised, not
  printed.
- **Never report a row by index alone.** Always the id, which is stable.

## 4. Where validation runs

| Moment | Scope |
|---|---|
| On import of one table | tiers 1–4 for that table; tier 3 against currently-loaded tables |
| On full import | all tiers, whole graph |
| In the automated test suite | all tiers — content is a test subject; see `midcore-testing` |
| In the release gate | all tiers, mandatory, non-skippable — see `midcore-release-pipeline` |

Content validation belongs in the automated suite because it catches designer mistakes no code test
can, it runs in seconds, and it fails deterministically. It is among the highest value-per-minute
tests a game project has.

## 5. The content/save id crossover

The exception that catches people, stated plainly:

> **Once a content id can appear in a player's save file, that id is part of the save contract.**

A player's save records which weapon they own, which quest they are on, which equipment is in which
slot — as content ids. So:

- **Deleting a content row** whose id can be saved is a **save migration**, not a content edit. Some
  player somewhere has it. See `midcore-save-migration`.
- **Reusing an id** for a different thing is worse than deleting: those players now silently own the
  wrong item, and no error fires anywhere.
- **Renaming an id** is deleting plus adding.

Practical rule: **retire, do not delete.** Mark the row retired so it stops appearing in new content
but still resolves for saves that reference it. Add a tier 6 advisory for retired rows still
referenced by *content*, which is a real mistake, while saves referencing them stay legal.

If the project must genuinely remove an id, that is a deliberate migration with a decision about what
those players receive instead — never a quiet deletion.

## 6. Bootstrapping validation on existing content

Retrofitting onto content that has never been validated will produce a large number of failures. The
order that works:

1. Turn on **tier 1–2** first. Usually a handful of failures; fix them.
2. Turn on **tier 3** in **report-only** mode. Count the violations — this number is the actual
   integrity of the content, and it is usually a surprise.
3. Fix them, then flip tier 3 to hard fail **in the same change**. Leaving it advisory "for now"
   means it stays advisory.
4. Add tiers 4–5 rules incrementally, hard-failing from the moment each is added, so no new backlog
   accumulates.

The mistake to avoid: enabling everything at once, drowning in failures, and disabling the whole
system. Get to hard-fail one tier at a time, and never leave a tier parked in advisory mode.
