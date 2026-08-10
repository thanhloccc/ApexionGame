---
name: write-feature-doc-before-code
description: A feature request produces a design doc for review — code only starts when the user explicitly asks to implement
metadata:
  type: feedback
---

On 2026-08-07 the user set a hard gate: **when they ask for a feature, write the doc first so they
can review it. Only write code when they explicitly ask to implement.**

- "làm feature X" / "thêm hệ thống X" / "tạo màn hình X" / "cần chức năng X" → **doc only**, then
  stop and say it is ready for review. Do not start coding in the same turn or offer to.
- "implement" / "code đi" / "viết code đi" / "triển khai đi" → **then** build it, per the doc.
- Code snippets *inside* the doc are expected (API sketches are part of the design); creating the
  real source files is what waits.
- The gate does not cover bug fixes, edits to existing code, renames, formatting, questions, or
  investigation. If it is ambiguous whether something counts as a feature, write the doc.

Settled with the user in the same conversation: docs live in **`Documentation~/` of the assembly
that owns the feature**, and are **bilingual** — `X.md` (English) plus `X.vi.md` (Vietnamese),
written in the same turn and kept section-for-section in sync.

Naming copies the reference doc set the user showed: **`<Topic> - <Aspect>.md`**, topic prefix
repeated on every file so alphabetical sort groups the set — `Combat - Overview.md`,
`Combat - Data Model.md`, `Native Wrappers - HashMap HashSet.md`. No per-topic subfolders.

**Why:** the review only has value if it happens before code exists — the user wants to catch a
wrong EncosyTower module choice, a wrong data layout, or a wrong folder plan while changing them is
still free.

**How to apply:** the doc must explicitly state the Encosy module mapping, the data model with the
chosen collection per type, and the exact folder/namespace/file layout — those three are what is
being reviewed, alongside the Request summary / Expected output / Steps trio required by
[[planning-advice-from-encosy-author]]. Phases and question guidance:
`.claude/skills/encosy-tower/references/planning-workflow.md`; naming, aspect vocabulary, required
sections and file template: `references/feature-docs.md`. Related:
[[review-encosy-before-implementing]], [[follow-encosy-structure-and-naming]],
[[prefer-high-performance-collections-and-math]].
