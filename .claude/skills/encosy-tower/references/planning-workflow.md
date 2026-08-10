# Planning workflow — skills, questions, plan, execute

Based on advice from EncosyTower's author on getting good results out of AI. Four phases. The
review gate sits between phase 3 and phase 4.

The core idea: **the plan file is the product of the first phase, and it must be complete enough to
execute autonomously.** Everything below serves that.

---

## Phase 0 — Load the right skills before planning

Decide what kind of work this is, load the matching skills, and say which you loaded.

| Task shape | Load |
|---|---|
| Pure C# — logic, data structures, systems, no asset involvement | `encosy-tower` |
| Touches Unity assets — scenes, prefabs, ScriptableObjects, inspector wiring, components | `encosy-tower` + `unity-cli-workflow` |
| Editor tooling — windows, inspectors, Project Settings pages | `encosy-tower` + `unity-cli-workflow` |
| Compiling, running EditMode/PlayMode tests, building | `unity-cli-workflow` |
| Anything in this repo, always | `encosy-tower` |

Currently available: `encosy-tower` and `unity-cli-workflow`, both project-level in `.claude/skills/`.
There is no dedicated dotnet/C# skill — `encosy-tower` plus `CODING-CONVENTIONS.md` covers that
ground here. **Unity MCP is not used in this project** — `unity-cli-workflow` replaces it; see
`AGENTS.md`.

**If a skill the task really needs does not exist, say so before starting** rather than improvising
around the gap. Missing skills are worth creating once and reusing, not working around each time.

---

## Phase 1 — Clarify by asking

The plan is only as good as the question round in front of it. Use `AskUserQuestion`.

**Ask about:**

- **The expected output, above all else.** What exists or behaves differently when this is done?
  Push for something concrete: a screen the player sees, a file on disk, a value in the inspector,
  a log line, a number. The author's point is exactly this — the more concretely the user can
  picture the output while planning, the fewer wrong turns later.
- **Scope boundary** — what is deliberately *not* in this piece of work.
- **Any fork with two or more reasonable readings** where the choice changes the architecture,
  the data layout, or where files land.
- **Integration points** — existing scenes, prefabs, assets, or systems this must plug into.

**Do not ask:**

- anything readable from the repo — read it instead;
- anything with an obvious default — pick it, state it in the plan, move on;
- questions whose answers would not change what you build.

Batch up to 4 questions per round. One round, two at most — this is clarification, not an
interrogation. Give a recommended option first, labelled, so the user can just confirm.

**Reconciling with "record open decisions":** ask now about anything that would force rework if
answered wrong later. Record as an open `DEC-xxx` only what can genuinely be decided after the fact
without redoing work.

---

## Phase 2 — Write the plan

The plan is the design doc set described in `feature-docs.md` —
`Documentation~/<Topic> - <Aspect>.md`, bilingual `X.md` + `X.vi.md`.

Three sections carry the author's advice and are **mandatory**, at the top of
`<Topic> - Overview.md`:

### Request summary

The original ask, restated in your own words, plus what the question round settled. Short. Its job
is to let the user confirm you understood them before reading the rest.

### Expected output

What will exist or behave differently when this is done — concretely, so the user can hold a
picture of it. Prefer showing over describing:

- the folder tree that will exist, with the new files listed;
- the public API, as a code block;
- what the player or developer sees or does, step by step;
- for UI, an ASCII sketch or a description precise enough to draw;
- how you will know it works — the check you will run.

This section is what the user compares the finished work against. Vague here means wrong later.

### Steps

Numbered, in dependency order. Each step is **one action with a verifiable done-condition** and
names the exact files it touches.

```markdown
## 3. Steps

| # | Action | Done when |
|---|---|---|
| 1 | Add `EncosyTower.Core` + `Unity.Mathematics` to `Game.Gameplay.asmdef`, copy the `versionDefines` block from `EncosyTower.Core.asmdef` | Assembly compiles, `UNITASK` resolves inside `Game.Gameplay` |
| 2 | Create `Combat/Contracts/IDamageable.cs` — namespace `Game.Gameplay.Combat` | Interface exists, project compiles |
| 3 | Create `Combat/CombatMessages.cs` — `DamageDealtMsg`, `UnitDiedMsg`, both `IMessage` | Types exist, project compiles |
| … | | |
```

**The completeness test:** could a fresh session, with no memory of this conversation, execute the
plan from the file alone? If not, the plan is not finished. That is what "autonomous" means here —
not that the user is absent, but that the plan does not depend on context only this conversation
has.

The remaining sections (Encosy mapping, Data model, Layout, Decisions) are as described in
`feature-docs.md` §4.

---

## Phase 3 — Stop for review

Write the files, list their paths, say the plan is ready for review, and end the turn. Do not begin
implementing, and do not offer to start while the user reads.

---

## Phase 4 — Execute on approval

When the user asks to implement:

1. Re-read the plan file. It is the spec, not a memory aid.
2. Work through the Steps table in order, honouring each done-condition before moving on.
3. If execution shows the plan was wrong, fix the plan in the same turn and say what changed and
   why. A stale plan is worse than none.
4. Flip `Phase` in the status table to `Implemented` and update `Documentation~/README.md`.
5. Report against the **Expected output** section — for each item, what was delivered. Anything not
   delivered gets named explicitly, with the reason.
