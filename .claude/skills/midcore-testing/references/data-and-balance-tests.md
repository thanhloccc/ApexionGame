# Tests that assert on content

The highest-value and most-skipped category in a game project. These tests treat **authored content
as a test subject**, and they catch a class of mistake no code test can reach: the ones made by
people who do not compile.

They are cheap (seconds), deterministic (data does not race), and they fail with a precise cause.

## 1. Integrity — always

Mechanically checkable, no design knowledge required. These duplicate the importer's validation on
purpose (see `midcore-data-pipeline`), because content can be committed without importing.

- no duplicate ids in any table;
- no empty ids;
- every foreign id resolves;
- every localization key referenced by content exists;
- every asset key referenced by content exists;
- no row references a retired id (retired rows stay loadable for saves, but new content must not
  point at them).

Write these **once, generically, over all tables**, not per table. A per-table version is forgotten
for table forty-one.

## 2. Structural invariants — the design's assumptions

These encode what the game believes about its content. They are where the value is, and they are
specific to the project.

| Invariant kind | Example |
|---|---|
| Non-empty container | every quest chapter has ≥1 quest |
| Contiguity | the level curve has no gaps between 1 and the cap |
| Monotonicity | xp thresholds strictly increase |
| Endpoint | a curve row exists for the maximum level the code allows |
| Reachability | every reward is obtainable by some path |
| Termination | no cycle in prerequisites |
| Pairing | every ranged weapon's ammo id exists and is of a compatible kind |
| Coverage | every enum member the code handles has at least one content row |

**Add one every time a content bug reaches a play session.** That is the cheapest possible moment to
learn which invariant was missing, and it converts each incident into permanent protection.

The endpoint one deserves emphasis: a progression curve one row shorter than the code's cap is a
crash at the cap, found by the most engaged player in the game.

## 3. Balance sanity — narrow, not clever

Tempting to write, easy to get wrong. A balance test that fires on legitimate content gets loosened
once and ignored forever.

**Good** — certainly-wrong conditions:

- no negative damage, cost, or duration where negatives are meaningless;
- no zero-cost item in a shop table;
- no reward of an item that does not exist;
- no progression step requiring more currency than the game can produce.

**Bad** — opinions dressed as assertions:

- "no weapon deals more than 500 damage" — until the expansion;
- "every rarity tier has 5–10 items" — until it does not;
- "the curve fits this formula" — until it is hand-tuned, which is why it is a table.

The test: *would a designer deliberately violating this be right?* If yes, it is not a test. Make it
a tier-6 advisory in the importer instead.

## 4. Content-versus-code agreement

The seam where a code assumption and authored content drift apart. Neither side fails alone.

- every enum member the code branches on has content, and every content kind value maps to a member;
- the level cap constant matches the curve length;
- every state a state machine can enter has content backing it;
- every content-driven feature flag corresponds to a real feature;
- ids hard-coded in code (there will be some) still exist in content.

That last one is worth a dedicated test. Hard-coded ids — the starter weapon, the tutorial quest —
are invisible to content-side validation and break silently when someone renames a row.

## 5. Writing them so failures are useful

```
FAIL  progression_curve_is_contiguous
      table `player_level`: no row for level 47
      rows present: 1..46, 48..60
```

```
FAIL  every_ranged_weapon_has_valid_ammo
      `ranged_weapon.rifle_mk2`.ammo_id = `bullet_smal` not found in `ammo`
      nearest: `bullet_small`
```

Rules:

- name the **table and the row id**, never a row index;
- print the offending value **and** what was expected;
- **collect all failures before reporting** — one-at-a-time fixing across a large table set is
  miserable;
- **cap the output** and summarise when one root cause produces hundreds of failures.

## 6. Where they live and when they run

- In the automated suite, alongside code tests — same command, no separate step to remember.
- **On every run**; they cost seconds.
- In the release gate as a **mandatory, non-skippable** step (`midcore-release-pipeline`).

They need the content **loaded**, so they sit at whichever test layer can load content without
requiring a full play session. Check the profile's `testing.assemblies` for where that is.

If content loading turns out to require a running game, that is itself a finding worth reporting —
content that cannot be loaded in isolation cannot be validated cheaply, and that is an architecture
issue (`midcore-assembly-architecture`), not a testing one.
