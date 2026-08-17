---
name: midcore-data-pipeline
description: REQUIRED whenever game content is defined as data rather than code — config tables, balance numbers, item/weapon/skill/quest definitions, drop tables, progression curves, localization keys, or anything a designer edits in a spreadsheet. Covers table and id design, the spreadsheet-to-asset import flow, import-time validation, cross-table reference integrity, content migration when a column changes, and how content is packaged and delivered. Load it before adding, splitting or reshaping any content table, before wiring an import, and when an import starts failing or a play session crashes on missing content. At midcore scale content outnumbers code, and a missing reference discovered three hours into a play session is the exact failure this prevents. Triggers on "table", "database", "spreadsheet", "sheet", "CSV", "Excel", "Google Sheet", "balance", "config", "content", "designer", "localization key", "drop rate", "loot table", "progression curve", "import", "data asset", and Vietnamese phrasing "bảng dữ liệu", "cân bằng chỉ số", "thêm bảng", "nhập dữ liệu", "designer chỉnh", "bảng rơi đồ", "dữ liệu game".
---

# Content data pipeline at scale

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

Past a certain size, content stops being "some config" and becomes the largest, most-edited,
least-reviewed part of the project — authored by people who cannot compile it, referenced by ids
that nothing checks, and shipped without ever being opened.

## The one rule

> **Fail the import, not the play session.**

Every invariant that can be checked when content is imported must be checked there. A crash from a
dangling id three hours into a session costs orders of magnitude more than an import that refuses to
produce a broken asset — and it is found by a player, not by the person who caused it.

## Four decisions, in order

### 1. Ids are typed. Always.

A raw `string` or `int` id is the root cause of most content bugs: nothing stops a weapon id being
passed where an ammo id is expected, and nothing catches it until runtime — if then.

Wrap every id kind in its own type. This is the single highest-leverage decision in the whole
pipeline: it turns an entire class of content bug into a compile error, and it costs one type
declaration per id kind. The project's wrapper mechanism is the `structure_naming` authority's
business; **that ids are typed at all is this skill's.**

Corollary: **never key a lookup by a display name or a localization key.** Names change, get
translated, and gain trailing whitespace in a spreadsheet.

### 2. One table per concept, not per screen

A table models a thing the game has — a weapon, a quest, a level step. Not a UI panel, not a feature.
Two screens showing the same concept read one table.

The tell that a table is wrong: its name contains "info", "data", "config", or the name of a screen.

### 3. Decide the naming policy once

One casing for sheet names, column names, and generated asset names, applied everywhere, chosen at
the start. Check the profile for `data_pipeline.naming_policy` — if one exists, it is not a matter of
taste any more.

Mixed casing costs real time: every reader has to remember which convention this particular table
follows, and every import mapping becomes a special case.

### 4. Validation is part of the pipeline, not a later feature

Wire the validation pass at the same time as the first table. Retrofitting it onto forty tables means
first fixing forty tables' worth of accumulated breakage, which is why it never happens.

## The validation tiers

Run in this order — cheapest and most fundamental first, so the error you see is the cause.

| # | Tier | Checks | On failure |
|---|---|---|---|
| 1 | **Schema** | every cell parses to its declared type; required columns present | **fail the import** |
| 2 | **Identity** | no duplicate ids; no empty ids | **fail the import** |
| 3 | **Referential** | every foreign id resolves to a row that exists | **fail the import** |
| 4 | **Domain** | ranges, non-negative, non-empty text, valid enum members | **fail the import** |
| 5 | **Invariant** | cross-table rules — every chapter has ≥1 quest, every reward is reachable | **fail the import** |
| 6 | **Advisory** | unreferenced rows, suspicious outliers, missing translations | **warn** |

Tiers 1–5 are hard failures. The instinct to downgrade them to warnings so an artist is not blocked
is exactly how a build ships with a dangling reference — a warning in a log nobody reads is not a
control.

Tier 6 warns because the condition is often legitimate: content staged ahead of the feature that
uses it, or a translation still in flight.

## When content changes shape

A renamed or removed column is a **content migration**, and it is a different problem from a save
migration — check `midcore-save-migration` for the other one and do not confuse the two.

The asymmetry that matters:

|  | Content | Saves |
|---|---|---|
| Lives | in the repo, all versions at once | on players' devices, one version each |
| Fix a mistake by | re-importing | never — it already shipped |
| Old versions | do not need to load | must load forever |

So content migration is comparatively cheap: change the sheet, change the schema, re-import, fix what
the validator rejects. The discipline is only to **do all three in the same change** — a schema that
has moved ahead of its sheet fails at import for everyone else on the team.

**But:** if a content id is referenced by save data, that id is now part of the save contract and
deleting it is a save migration, not a content edit. This is the crossover that catches people —
see `references/validation.md` §5.

## Content delivery

How content reaches the running game — bundled, downloaded, streamed — is a packaging decision with
consequences for patch size and load time.

- **Group by change rate and by load moment**, not by type. Content patched weekly should not sit in
  the same unit as content that never changes, or every patch ships both.
- **Key naming is a contract.** Once a key ships, changing it breaks anything that stored it —
  including saves.
- **Localization keys are content** and get the same validation: no duplicates, no dangling
  references, every key referenced by a shipped string.

## This skill does not own

| Question | Owner |
|---|---|
| The specific attributes, generator or import window of a table technology | `structure_naming` authority (see profile) |
| Which collection type holds the loaded rows | `structure_naming` authority |
| How to run the import or compile the result | `unity_operations` authority |
| Player save data shape and versioning | `midcore-save-migration` |
| Which assembly the table types live in | `midcore-assembly-architecture` |
| Whether a value should be remotely configurable instead | `midcore-live-ops` |

## References

- `references/table-design.md` — id strategy, one-concept-per-table, cross-table references,
  variants without nullable columns, and a worked multi-table cluster.
- `references/authoring-and-import.md` — sheet ownership, the naming policy, converters, the
  designer round-trip, and handling a renamed or removed column.
- `references/validation.md` — the six tiers in detail, what belongs in each, how to fail an import
  usefully, and the content/save id crossover.
- `references/keys-and-groups.md` — delivery grouping, key naming as a contract, localization keys,
  and the patch-size consequences of grouping choices.
