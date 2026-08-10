---
name: follow-encosy-structure-and-naming
description: Project folder structure, code separation, and naming must copy EncosyTower's organization — including the rule that sub-folders do not add namespace segments
metadata:
  type: feedback
---

On 2026-08-07 the user added a third standing requirement: **folder structure, how code is split,
and naming all follow EncosyTower.**

The non-obvious part, verified across the package and already applied in the user's own
`ApexionGame.Entities.Stats`: **folders organize, namespaces serve the consumer, and the two are
deliberately decoupled.** A sub-folder does *not* add a namespace segment — a module's whole public
surface sits in one flat namespace so consumers write a single `using`.
`PubSub/Contracts/` and `PubSub/Publishers/` are both `EncosyTower.PubSub`; only
`PubSub/Internals/` becomes `EncosyTower.PubSub.Internals`. A folder earns a segment only when it
is a separate opt-in surface: `Internals`, `Unsafe`, `Extensions`, `SourceGen`, `Editor.*`,
`Caches`, `Generators`, `Debugging`.

The rest: assembly name = `rootNamespace` = folder name; editor/authoring/test surfaces are sibling
assemblies with dotted suffixes (`X.Editor`, `X.Authoring`, `X.Tests`), never subfolders; reuse the
sub-folder vocabulary (`Contracts/`, `Annotations/`, `Internals/`, `SourceGen/`, `Extensions/`,
`APIs/`, `Common/`, `Converters/`, `Generators/`, `Components/`, `Storage/`, `Debugging/`, `Views/`,
`StyleSheets/`) and never `Interfaces/`, `Utils/`, `Misc/`, `Scripts/`; file naming
`Type+Aspect.cs`, `` Type`N.cs ``, `Type_Async.cs`, `*.gen.cs`; one primary type per file and one
extension class per file; usings outside a block-scoped namespace in three alphabetical groups;
the member-ordering rule with serialized fields first on Unity types.

**Why:** the user adopted EncosyTower wholesale and wants project code to be indistinguishable from
package code in layout and naming, not just in which APIs it calls.

**How to apply:** decide placement *before* creating files, not as cleanup. Full rules, the
folder→namespace evidence table, and a worked `Game.Gameplay` layout:
`.claude/skills/encosy-tower/references/structure-and-naming.md`. Moving files in Unity must carry
their `.meta`. Related: [[house-style-is-encosy-conventions]],
[[prefer-high-performance-collections-and-math]], [[review-encosy-before-implementing]].
