# Doc-first workflow, and how feature docs are named and written

**A feature request produces a document, not code.** Code only starts when the user explicitly asks
to implement. This is a hard gate, not a suggestion.

This file covers *where docs live, what they are called, and what goes in them*. The process around
them — which skills to load, the clarifying-question round, and executing the plan afterwards — is
in `planning-workflow.md`. Read that one first when starting a feature.

**Read the model doc set before writing your own:**
`Packages/com.apexion.apexion-game/ApexionGame.Core/Documentation~/` — `HFSM - Overview`,
`- API Surface`, `- Flows`, `- Data Model`, `- Layout`, `- Debugging`, `- Decisions`, `- Roadmap`,
plus a `README.md` whose reading-order table says *when* to open each file. That is every rule below, done properly, on a real feature. Copy its shape and its level of
detail.

The sibling `ApexionGame.Entities.Stats/Documentation~/` is equally thorough but predates this
convention — it uses `01-OVERVIEW.md` numbering and a `guide/` subfolder. Read it for content; do
**not** copy its naming for a new set.

---

## 1. The gate

| The user says | You produce |
|---|---|
| "làm feature X", "thêm hệ thống X", "tạo màn hình X", "cần chức năng X", "build me an X system" | **The doc. No implementation files.** |
| "implement", "code đi", "viết code đi", "triển khai đi", "OK làm luôn", "go ahead and build it" | **The code**, following the approved doc |

After writing the doc, stop. Say which files you wrote and that they are ready for review. Do not
begin implementation in the same turn, and do not offer to "start while you review".

**Writing code snippets *inside* the doc is expected** — API sketches, type declarations, usage
examples are part of the design. What you must not do is create the real source files.

### When the gate does *not* apply

Skip the doc and just do the work for:

- bug fixes and small edits to code that already exists;
- renames, moves, formatting, comment or doc-string changes;
- questions, explanations, code reading, investigation;
- anything the user asks for that is clearly a one-file, one-purpose change;
- an explicit "skip the doc" / "khỏi doc" from the user.

If it is genuinely ambiguous whether something is a "feature", write the doc — it is cheap and the
user asked for the chance to review before code exists.

### If the user asks to implement something with no doc yet

Write the doc first, in the same turn, then stop for review — unless they say to skip it. Say
plainly that you wrote the doc first because of the standing rule.

---

## 2. Where docs live

`Documentation~/` inside the assembly that owns the feature. Unity ignores folders ending in `~`,
so no `.meta` files and no asset-database churn.

```
Assets/Game/Game.Gameplay/
├── Game.Gameplay.asmdef
└── Documentation~/
    ├── README.md                       index — reading order, status of every feature
    ├── Combat - Overview.md
    ├── Combat - Data Model.md
    ├── Combat - Encosy Mapping.md
    └── Combat - Decisions.md
```

If the feature spans assemblies, the doc goes in the assembly that owns the *core* of it, and the
others' `README.md` link to it.

Create `Documentation~/README.md` the first time an assembly gets a doc, and add a row to it for
every new topic afterwards.

---

## 3. Naming

**`<Topic> - <Aspect>.md`** — topic first, space-hyphen-space, then aspect. One file, Vietnamese.

```
Combat - Overview.md
Combat - Data Model.md
Native Wrappers - Contracts.md
```

The topic prefix repeats on every file of the set so alphabetical sort groups them. Never drop it,
never use a subfolder per topic.

- **Topic** — the feature or subsystem, in Title Case: `Combat`, `Inventory`, `Save System`,
  `Native Wrappers`, `Entities.Stats`. A dotted topic is fine when it names an assembly or module.
- **Aspect** — either a *doc kind* or a *subject area*, in Title Case, plain spaces, no commas or
  ampersands: `HashMap HashSet`, not `HashMap & HashSet` or `HashMap, HashSet`.

### Doc-kind aspects

| Aspect | Contains | When |
|---|---|---|
| `Overview` | request summary, expected output, steps, status, goals/non-goals — see §4 | always, first file of the set |
| `Encosy Mapping` | which EncosyTower modules cover which part, and what is hand-rolled and why | design |
| `Data Model` | types, fields, chosen collections, memory and allocation story | design |
| `API Surface` | the public API sketch, with signatures | design |
| `Layout` | folder tree, namespaces, file names, asmdef changes | design |
| `Flows` | sequences, state transitions, message/request traffic | design, when behaviour is non-trivial |
| `Decisions` | decision log — `DEC-001`, one row per choice, with alternatives rejected | design |
| `Roadmap` | phases, deliverables, test plan, risks | design, for multi-phase work |
| `Step-by-Step Guide` | walkthrough for a first-time user | after implementation |
| `Cookbook` | task-oriented recipes | after implementation |
| `Contracts` | the interfaces the feature is programmed against | after implementation, if the surface is large |
| `Pitfalls` | what fails silently, ordered worst-first | after implementation |

### Subject-area aspects

For a large topic, split by subject instead of by doc kind — one file per cluster of related types:

```
Native Wrappers - Overview.md
Native Wrappers - Array.md
Native Wrappers - HashMap HashSet.md
Native Wrappers - List Queue Reference.md
Native Wrappers - Parallel Collections.md
Native Wrappers - Contracts.md
```

**Do not split prematurely.** A small feature is one `<Topic> - Overview.md` containing everything.
Split only when a section outgrows its file or a reader would want to read one part without the
others.

---

## 4. What a design doc must contain

The doc is the **plan** — see `planning-workflow.md` for the phases around it (load skills → ask
clarifying questions → write → review → execute). The point of the review is that the user can
catch a misunderstood request, a wrong expected output, a wrong module choice, a wrong data layout,
or a wrong folder plan *before* code exists.

`<Topic> - Overview.md` opens with these three, in this order — they carry the whole review:

1. **Request summary** — the original ask restated in your own words, plus what the question round
   settled. Short. It lets the user confirm you understood before reading further.
2. **Expected output** — what will exist or behave differently when this is done, concrete enough
   that the user can picture it: the folder tree with new files listed, the public API as a code
   block, what the player or developer sees step by step, an ASCII sketch for UI, and the check you
   will run to know it works. Vague here means wrong later.
3. **Steps** — numbered, dependency-ordered table. Each row is one action with a verifiable
   done-condition, naming the exact files it touches. Completeness test: could a fresh session with
   no memory of this conversation execute the plan from the file alone?

Then, in the same file or split across the set as it grows:

4. **Status table** — phase, target assembly, target namespace, EncosyTower modules used,
   dependencies, open decisions.
5. **Goals and non-goals** — what is in scope, and what is deliberately not.
6. **Encosy mapping** — a table of "this part of the feature → this EncosyTower module".
   Every part that is *not* covered by the package gets one sentence saying why nothing fit.
7. **Data model** — the types, and the collection chosen for each with a reason
   (`FasterList<T>` / `ArrayMap<K,V>` / `SharedArray<T>` / `ListNative<T>` / plain `List<T>`),
   plus whether anything crosses into a job and what math types it uses.
8. **Layout** — the exact folder tree, namespaces, and file names to be created, following
   `structure-and-naming.md`. Include asmdef reference changes.
9. **API surface** — signatures of the public entry points, as a code block.
10. **Decisions** — numbered `DEC-001`, `DEC-002`… Each: the choice, alternatives rejected, and
    status (`chốt` / `open`).

**Open questions:** ask now (phase 1) about anything that would force rework if answered wrong.
Record as an open `DEC-xxx` only what can genuinely be decided later without redoing work — with
your recommended option marked. That distinction is what keeps the doc from either stalling or
hiding a real fork.

---

## 5. File format

Head of every file:

```markdown
# Combat — Tổng quan

*[Data Model](Combat - Data Model.md) · [Index](README.md)*

## Trạng thái

| | |
|---|---|
| Phase | **Awaiting review** |
| Target assembly | `Game.Gameplay` |
| Target namespace | `Game.Gameplay.Combat` |
| EncosyTower modules | PubSub, Pooling, Collections (`ArrayMap`) |
| Unity deps | `Unity.Mathematics`, `Unity.Collections` |
| Open decisions | DEC-003 |
```

- H1 uses an em dash (`Combat — Tổng quan`); the **file name** uses a plain hyphen and keeps its
  English aspect (`Combat - Overview.md`) so the set sorts together. Aspect names in file names stay
  English; headings and prose are Vietnamese.
- Second line links the sibling docs plus the index, in italics, separated by `·`.
- Numbered `##` sections (`## 1. Goals`, `## 2. Non-goals`, …) so they can be cited as
  "Combat - Overview §3".
- Tables over prose for anything enumerable. Code blocks are fenced with the language.
- Same style rules as code comments in `CODING-CONVENTIONS.md` §5: say why, not what.

### Language

- **Vietnamese, one file.** No `.vi.md` mirror, no English twin — a second copy only drifts, and the
  reader of these docs reads Vietnamese.
- Code, identifiers, file paths, and `DEC-xxx` ids are **never** translated.
- Skill files under `.claude/skills/` are the opposite: English only, with Vietnamese trigger phrases
  in the `description` frontmatter.

---

## 6. After the doc is approved

When the user asks to implement:

1. Re-read the doc — it is the spec, including its layout and decision sections.
2. Build exactly what it describes. If implementation reveals the doc was wrong, fix the doc in the
   same turn and say what changed and why; do not leave the doc stale.
3. Flip the status table `Phase` from `Awaiting review` to `Implemented`, and update
   `Documentation~/README.md`.
4. Once the feature works, consider adding `<Topic> - Pitfalls.md` for anything that failed
   silently while building it — that is the highest-value doc in the set.
