# Keys, grouping and delivery

How content reaches the running game. A packaging decision, with consequences for patch size, load
time and — because keys outlive builds — for save compatibility.

## 1. Keys are a contract

An asset key stored anywhere durable is as permanent as a content id:

- saves record which asset the player has equipped;
- remote config and live events name content by key;
- analytics records keys and cannot retroactively rename them.

So: **once a key ships, it does not change.** Renaming one breaks every stored reference to it, and
the breakage is silent — a lookup that returns nothing, in a code path written assuming it would not.

Rules:

1. **Derive keys from ids**, not from file paths or display names. A file moves; an id does not.
2. **One naming scheme**, decided once, matching the project's naming policy.
3. **Never put a version, a date, or a status word in a key.** `sword_v2`, `quest_final_FINAL` — the
   next version has nowhere to go.
4. **Renaming a key is retire-and-add**, exactly like a content id (`validation.md` §5).

## 2. Grouping by change rate and load moment

The default instinct is to group by type — all textures, all audio, all tables. This is close to the
worst option: every content patch touches every group, so every patch ships everything.

Group by **when it changes** and **when it loads**:

| Group | Contains | Changes |
|---|---|---|
| Core | boot content, shared UI, the database container | rarely — ships in the build |
| Per-feature | one system's content and assets | with that feature |
| Per-chapter / per-region | content for one span of play | when that span is authored |
| Live | event and seasonal content | frequently, often after ship |

Two questions decide placement:

1. **When does this change?** Things that change together belong together.
2. **When is it needed?** Things loaded at the same moment belong together; things never needed
   together should not be.

Where the two conflict, **change rate usually wins** for downloadable content (it decides patch size,
paid by every player on every update) and **load moment** wins for in-build content (it decides load
time and peak memory).

## 3. In-build versus downloaded

| | In build | Downloaded |
|---|---|---|
| Available | always, offline, immediately | after a fetch that can fail |
| Update requires | a store release | a content push |
| Costs | install size | first-run wait, failure handling, hosting |

Decide per group, and write the decision down. The rules that matter:

- **Anything needed before the first fetch completes must be in the build.** Boot, first-run UI,
  error messaging — including the message that says the download failed.
- **Anything a live-ops event switches on must already be in the build or already downloaded** before
  the switch flips. Content and its activation are two different mechanisms with two different
  latencies — see `midcore-live-ops`.
- **Every downloaded group needs a defined failure behaviour.** Not "it will work" — what the player
  sees when it does not.

## 4. Localization keys are content

Same validation, same permanence:

- **no duplicate keys** — tier 2;
- **every key referenced by content or code exists** in the string table — tier 3;
- **every shipped locale has an entry**, or a documented fallback — tier 4;
- **unreferenced keys** — tier 6 advisory, since translation often runs ahead of content.

Two extra rules specific to strings:

- **Never build a display string by concatenating fragments.** Word order differs between languages
  and the result is unfixable by translators. One key per whole sentence, with parameters.
- **The key is not the English text.** Using English as the key means every copy edit is a key
  rename, which is a contract change (§1).

## 5. Content size discipline

At midcore scale content dominates build size, and build size affects install conversion.

- **Track group sizes over time**, not just the total. A total that grows steadily hides one group
  that tripled.
- **Set a budget per group** and treat exceeding it as a decision, not an accident. If the profile
  has no size targets, ask rather than inventing one.
- **Audit for orphans regularly** — assets shipped but referenced by nothing. Tier 6 catches
  unreferenced *rows*; unreferenced *assets* need their own sweep, and they accumulate silently.
- **Duplication across groups is invisible and expensive.** An asset referenced by two groups may be
  packed into both. Check for it explicitly; nothing warns.

## 6. What to verify after changing grouping

A grouping change is easy to make and hard to notice breaking:

1. Every key still resolves — run the referential validation, not a spot check.
2. Load moments still work: cold boot, first play, and the specific moment the moved content is
   needed.
3. Patch size for a representative small change — this is what grouping was for.
4. Offline behaviour, if any group is downloaded.
5. Peak memory at the heaviest load moment, if grouping changed what loads together.

Nothing here is proven by "the game started". Grouping failures characteristically appear at one
specific moment, on one specific path, often only on a fresh install.
