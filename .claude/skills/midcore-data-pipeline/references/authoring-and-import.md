# Authoring and import

## 1. The round trip

```
designer edits sheet → import → validation → generated asset → runtime reads asset
                          ↑                        │
                          └──── fails loudly ──────┘
```

Four properties this must have:

1. **The sheet is the source of truth.** The generated asset is a build artifact. Editing it directly
   is silently reverted by the next import, so hand-editing must be impossible or obviously wrong.
2. **Import is reproducible.** Same sheet in, same asset out, on any machine. If the result depends
   on who ran it or in what order, content bugs become unreproducible.
3. **Import is fast enough to run often.** If a full import takes long enough to avoid, people avoid
   it, and breakage accumulates until someone else finds it.
4. **Failure is loud and specific.** "Import failed" is useless; "row 47 of `ranged_weapon`:
   `ammo_id` = `bullet_smal` does not exist in `ammo`" is actionable by the person who caused it.

## 2. Ownership

Write down who owns each sheet, next to the sheet. Ambiguous ownership is how two people edit the
same balance column in the same afternoon.

| Concern | Typically owned by |
|---|---|
| Row values — balance, text, tuning | design |
| Which columns exist, their types | engineering, with design |
| The import wiring and validation rules | engineering |
| Adding a whole new table | both, together — it is a schema change |

The rule worth stating: **designers add rows freely; adding or retyping a column is a code change.**
Column changes alter the schema, so they travel with the code that reads them.

## 3. Naming policy

One casing, chosen once, applied to sheet names, column names, and generated asset names. Check the
profile for `data_pipeline.naming_policy` — if it is set, this is settled and not a matter of taste.

Why it earns a rule of its own: with mixed casing, every reader must remember which convention this
table follows, every import mapping becomes a special case, and every new table is a small argument.

**Never encode type or units in a name inconsistently.** If duration columns are `_seconds`, all of
them are — a bare `cooldown` next to `reload_seconds` is a bug waiting for someone to assume.

## 4. Converters

A spreadsheet holds text. Anything richer needs an explicit conversion, declared once per type
rather than per column:

- typed ids ← their text form (and the conversion **fails** on an unknown id — it does not default);
- enums ← names, failing on an unrecognised member rather than falling back to the zero value;
- durations, percentages, currencies ← numbers with a **documented unit**;
- structured values (a vector, a range) ← a documented, consistent text form.

Two hard rules:

- **A conversion failure fails the import.** A converter that silently returns a default turns a typo
  into a plausible wrong value, which is worse than a crash because nobody looks for it.
- **Never let a blank cell mean a real value by accident.** Blank means "not set"; if "not set" is
  legal, the schema says so explicitly and the default is written down where a designer can read it.

## 5. Adding a table

1. Name the concept. If it is a screen or a feature, it is not a table (`table-design.md` §2).
2. Decide the id kind and add its typed wrapper.
3. Define columns, with types and units. Every foreign id column typed to its target.
4. Add the validation rules **now**, in the same change — at minimum tiers 1–3.
5. Wire the import; run it on real content, not a two-row sample.
6. Confirm the runtime read path, then confirm the generated asset is not hand-editable.
7. Check delivery grouping (`keys-and-groups.md`) before the table gets large.

## 6. Changing an existing table

### Adding a column

Lowest risk. Decide the value for existing rows: a default written in the schema, or backfilled in
the sheet. Do not leave blanks meaning "whatever the code does" — that is undocumented behaviour with
a designer-visible surface.

### Renaming a column

Sheet, schema and any mapping change **together, in one change**. A schema ahead of its sheet fails
the import for everyone else on the team the moment they pull.

### Removing a column

1. Confirm nothing reads it — including debug tooling, editor windows, and analytics.
2. Remove the reader first, in its own change.
3. Remove the schema entry and the sheet column afterwards.

Reversing that order breaks the import for everyone between the two commits.

### Removing a row

**Stop and check whether the id is referenced by save data.** If any player's save can name that id,
deleting the row is a save migration, not a content edit. See `validation.md` §5 and
`midcore-save-migration`.

If it is safe to delete: check for content references first (tier 3 will catch them, but finding out
before the import fails is faster), then delete. **Never reuse the id for a different thing** — every
analytics record, save file and support ticket referring to the old one now quietly means the new
one.

## 7. Keeping import fast

At midcore scale a full re-import gets slow enough that people stop doing it — and skipped imports
are how a broken sheet reaches someone else's machine.

- **Import per table where possible**, so a one-table edit is a one-table import.
- **Keep validation proportional**: cheap per-row checks always; expensive whole-graph invariants on
  full import and in the release gate.
- **Make the failing case fast.** Validation runs before asset generation, so a bad sheet fails in
  seconds rather than after the slow part.
- **Never make correctness optional to save time.** If a check is too slow to run always, run it in
  the release gate (`midcore-release-pipeline`) — do not delete it.
