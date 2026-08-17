# Table and id design

## 1. Typed ids — the decision everything else rests on

A content id is not a string. It is a reference into a specific table, and the type system should
know which one.

```
// The problem, stated plainly:
GiveAmmo(weaponId)          // compiles, ships, fails at runtime or silently does nothing
GiveAmmo(AmmoId ammoId)     // does not compile. Bug found while typing.
```

**Rules**

1. **One wrapper type per table.** `WeaponId`, `AmmoId`, `QuestId`. Not one shared `ContentId`,
   which reintroduces exactly the confusion you are eliminating.
2. **The wrapper is the only public form.** If the raw value leaks into signatures, dictionaries and
   serialized fields, the guarantee is gone.
3. **Never key by display name or localization key.** They change, they are translated, and they
   acquire trailing whitespace in spreadsheets.
4. **Stable, meaningless ids beat readable ones.** `melee_weapon.iron_sword` reads nicely until the
   sword is renamed to "Rusted Blade" and either the id lies or every reference breaks. Prefer an
   id that was never a description.
5. **Ids are permanent.** Once a row ships, its id is referenced by saves, analytics, remote config
   and player support tickets. Deleting or reusing one is a save-migration problem, not a content
   edit (see `validation.md` §5).

The project's wrapping mechanism belongs to the `structure_naming` authority. That ids are typed at
all is not negotiable regardless of mechanism.

## 2. One table per concept

A table models something the game has. Not a screen, not a feature, not a designer's spreadsheet tab.

| Smell | Why it is wrong | Fix |
|---|---|---|
| `shop_screen_data` | Named for a view | Split into the concepts the view shows |
| `misc_config` | No concept | Name the concepts; make tables for them |
| `weapon_info` + `weapon_stats` + `weapon_extra` | One concept split by authoring convenience | One `weapon` table |
| `enemy` with 40 columns, 30 empty per row | Several concepts fused | Split by what varies (below) |

### Splitting a wide table

When most rows leave most columns empty, the table holds several kinds of thing. Three options:

1. **Separate tables per kind** — `melee_weapon`, `ranged_weapon`. Best when the kinds have genuinely
   different fields and are looked up separately. Costs a shared id space if something must reference
   "any weapon".
2. **A base table plus per-kind extension tables**, joined by id. Best when there is real shared
   behaviour *and* real per-kind data. Costs a join and a validation rule that the extension row
   exists.
3. **One table with a kind column** — only when the differences are one or two fields. Beyond that,
   the empty cells are a design telling you something.

Whichever you pick, **the runtime should not discover the kind by checking which fields are empty.**
An explicit kind column or a separate table; never inference from emptiness.

## 3. Cross-table references

A column holding another table's id. The rules:

- The column type is the **typed id of the target table**, so the reference is checked at import and
  at compile time, not read as text.
- Every such column is validated at tier 3 (`validation.md`). No exceptions — an unvalidated foreign
  id is a crash scheduled for later.
- **Optional references need an explicit empty form**, not a magic value. `0`, `-1` and `""` meaning
  "none" all eventually collide with a real id or hide a typo.
- **Avoid reference chains deeper than two hops** for anything read per frame. Resolve at load time
  into direct references rather than walking a chain at runtime.

### Many-to-many

Do not encode a list of ids in one cell as delimited text. It defeats per-cell validation, breaks the
moment a delimiter appears in data, and cannot be sorted or diffed.

Use a **link table** — one row per pair, both columns typed and validated. It is more rows and far
fewer bugs, and it lets each pair carry its own data (weight, quantity, condition) which the
delimited version cannot.

## 4. Modelling variants without nullable columns

Content is full of "this one is slightly different". The failure is a growing set of optional columns
where meaning depends on which are filled.

Preferred, in order:

1. **A separate table for the variant dimension**, referenced by id. A weapon references an
   `ammo` row; it does not carry ammo columns.
2. **A link table** when a row has zero-or-many of something — modifiers, requirements, rewards.
3. **A kind column plus documented per-kind meaning**, only for a small fixed set.

The test: *can a reader tell what a row means from the row alone?* If they must know which other
columns are empty, the shape is wrong.

## 5. Worked example — a weapon/ammo/equipment cluster

A common midcore shape, showing the rules together:

```
ammo                    id, display_key, damage_kind, stack_max
melee_weapon            id, display_key, damage, swing_time, damage_kind
ranged_weapon           id, display_key, damage, fire_rate, ammo_id → ammo.id, magazine
equipment               id, display_key, slot, ...
equipment_modifier      equipment_id → equipment.id, stat, value       ← link table
```

What each rule bought:

- `melee_weapon` and `ranged_weapon` are **separate tables** (§2 option 1) — a melee row has no
  meaningful `ammo_id` or `magazine`, so fusing them would mean two permanently empty columns and a
  runtime that infers kind from emptiness.
- `ranged_weapon.ammo_id` is a **typed cross-table reference** (§3), validated at import. A typo is
  caught by the importer, not by a player whose gun does nothing.
- `equipment_modifier` is a **link table** (§3) — equipment has zero-or-many modifiers, and each pair
  carries its own `stat`/`value`. As delimited text in an `equipment` column this would be
  unvalidatable and undiffable.
- `display_key` is a **localization key, not a name** — and it is deliberately *not* the id (§1.4).
- Nothing references "any weapon" here. If something must, that is a decision to make explicitly —
  either a shared id space across both tables, or a base table (§2 option 2) — not something to
  discover later when the first cross-weapon feature arrives.

## 6. Progression curves and other per-level tables

A table with one row per level (xp thresholds, stat growth, unlock steps) has its own failure modes:

- **Validate monotonicity** where the design requires it. A non-increasing xp threshold makes
  levelling unreachable or instant, and it is invisible in a spreadsheet of 60 rows.
- **Validate contiguity** — no gaps in the level sequence, no duplicates.
- **Validate the endpoints** — level 1 exists; the maximum matches whatever the code believes it is.
  A curve one row shorter than the cap is a crash at the cap, found by the best player.
- **Decide whether the curve is data or a formula.** A formula with authored parameters is far fewer
  rows and cannot have gaps — but it cannot be hand-tuned per level. Choose deliberately; do not end
  up with both, disagreeing.

These belong in tier 5 (`validation.md`), because they are invariants across rows rather than
properties of one row.
