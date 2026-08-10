---
name: review-encosy-before-implementing
description: Standing instruction — review EncosyTower for an existing solution before writing any implementation, and report what was used
metadata:
  type: feedback
---

On 2026-08-07 the user asked that **every time they request an implementation, I first review
whether EncosyTower already covers that part** — then apply it instead of hand-rolling.

**Why:** they are adopting EncosyTower for real production projects and want the codebase to
actually exercise the package, not accumulate parallel homegrown systems that drift from it.

**How to apply:** before writing code for any request, run the `encosy-tower` skill's workflow —
match the request to a module, confirm the module is not compiled out
([[encosy-modules-live-in-project]]), read the real API in the module folder or
`Samples~/`, then implement. In the reply, state which EncosyTower modules were used; if any part
was hand-rolled, give one sentence on why nothing in the package fit. Do not silently reimplement
something the package provides. Related: [[encosy-tower-is-standard-library]].
