# Planning and feature documents

## Gate

A new feature request produces a plan for review, not implementation files. Explicit approval to implement releases the gate. Bug fixes, small existing-code edits, renames, investigation, and an explicit request to skip docs bypass it.

## Location and naming

Store plans in `Documentation~/` inside the assembly owning the feature. Use `<Topic> - <Aspect>.md`; mirror every document as `.vi.md`. Repeat the topic prefix on every file and do not create per-topic subfolders.

Create `README.md` and `README.vi.md` indexes when the assembly receives its first documentation set.

## Required overview

Open `<Topic> - Overview.md` with:

1. Request summary: restate the ask and settled clarifications.
2. Expected output: concrete behavior, file tree, API/UI shape, and acceptance checks.
3. Steps: dependency-ordered table with one action, exact touched files, and verifiable done-condition per row.

Then cover status, goals/non-goals, EncosyTower module mapping, data model and collection choice, exact layout and namespaces, public API, and numbered decisions.

Keep English and Vietnamese documents structurally identical. Never translate identifiers, paths, code, or decision IDs. Update both variants together.

After approval, execute the Steps table, update a stale plan immediately when implementation evidence differs, change the status to `Implemented`, and report delivery against Expected output.
