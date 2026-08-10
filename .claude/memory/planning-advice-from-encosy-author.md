---
name: planning-advice-from-encosy-author
description: EncosyTower's author's advice on working with AI — load the right skills, ask to clarify, and produce a plan file that can be executed autonomously
metadata:
  type: feedback
---

On 2026-08-07 the user relayed advice from EncosyTower's author on getting good results out of AI,
and asked that it shape how features are planned here. Four points:

1. **Prepare the skills the task needs before starting.** A pure-code feature needs a C#/dotnet
   skill; anything touching Unity assets needs a Unity skill; and a planning skill is needed to make
   the AI think the problem through properly. In this project that maps to: `encosy-tower` always,
   plus `unity-cli-workflow` when assets/scenes/prefabs/inspector/tests/builds are involved
   ([[unity-tooling-is-cli-not-mcp]]). There is no separate dotnet skill — `encosy-tower` +
   `CODING-CONVENTIONS.md` cover it.
2. **The planning step must make the AI ask questions** to clarify the request — not guess.
3. **The plan must be complete enough for the AI to execute it autonomously.** The completeness
   test: could a fresh session with no memory of the conversation execute it from the file alone?
4. **The plan file is the deliverable of the first phase**, and contains: a summary of the original
   request, a summary of the expected output, and the full list of steps to perform.
   **The more concretely the expected output is pictured during planning, the fewer mistakes.**

**Why:** the author designed EncosyTower and works with AI on it daily; the user is deliberately
adopting that working method along with the package.

**How to apply:** phases and question guidance in
`.claude/skills/encosy-tower/references/planning-workflow.md`; doc naming and template in
`references/feature-docs.md`. Concretely: push hardest in the question round on what the output
will actually look like, and make `Expected output` and `Steps` mandatory top sections of
`<Topic> - Overview.md`. Related: [[write-feature-doc-before-code]],
[[review-encosy-before-implementing]].
