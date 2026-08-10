---
name: encosy-tower
description: Plan, implement, review, optimize, organize, or rename code and Unity features in ApexionGames Land of Souls using EncosyTower as the standard library. Use for feature requests, C# systems, Result/error contracts, collections, math, Burst/jobs, messaging, processing, pooling, save/load, databases, UI/PageFlows/MVVM, asset keys, localization, settings, logging, source generators, asmdefs, folders, namespaces, and project architecture. Enforce the project's doc-first feature gate and EncosyTower structure and performance conventions.
---

# EncosyTower Project Workflow

Treat `Packages/com.laicasaane.encosy-tower` as the project's standard library. Find and verify an existing EncosyTower solution before writing parallel infrastructure.

## Read context selectively

- Read `../../memory/MEMORY.md` before planning or implementing project work.
- Read `references/planning-and-feature-docs.md` for a new feature or an implementation request whose approved plan must be located.
- Read `references/project-setup.md` before changing asmdefs, packages, source-generated types, stats, or module gates.
- Read `references/structure-performance.md` before creating or moving code, choosing collections, or writing hot-path/job math.
- Read `references/module-selection.md` to choose between EncosyTower modules.
- Read `references/structured-errors.md` before creating, changing, or reviewing a
  gameplay/domain error or any `Result<T, TError>` failure surface.
- Treat package source and `Packages/com.laicasaane.encosy-tower/Samples~/` as signature-level ground truth. The references are routing guides, not API guarantees.

## Apply the feature gate

For a new feature request, create its design/plan documents and stop for review. Start source implementation only when the user explicitly asks to implement, code, or proceed, or explicitly asks to skip documentation.

Do not apply the gate to bug fixes, focused edits to existing code, renames, formatting, questions, investigations, or an obviously one-file change.

When clarification is material, ask concise questions in the interaction mechanism currently available. Do not depend on Claude-specific tools or command names.

## Plan a feature

1. Load this skill. If Unity assets, scenes, prefabs, inspector wiring, or Editor state are involved, also load `unity-cli-workflow`; never require Unity MCP.
2. Inspect the repository before asking questions. Ask only about choices that change architecture, output, or scope.
3. Create English and Vietnamese files together under the owning assembly's `Documentation~/` directory:
   - `<Topic> - Overview.md`
   - `<Topic> - Overview.vi.md`
4. Put these sections first and keep both languages section-for-section aligned:
   - Request summary
   - Expected output
   - Steps: dependency ordered, one action per row, exact files, verifiable completion
5. Include status, goals/non-goals, EncosyTower mapping, data model and collection choices, exact folder/namespace/file layout, API surface, and `DEC-xxx` decisions.
6. Make the plan executable by a fresh session without conversation-only context.
7. List the written paths and stop for review.

## Implement approved work

1. Re-read the approved plan and relevant project memory.
2. Map each subsystem to an EncosyTower module and verify its current package/define/asmdef gate.
3. Inspect the actual package source or sample before relying on a signature.
4. Audit every new or touched `Result<T, TError>` contract against
   `references/structured-errors.md` before implementing call sites.
5. Execute the plan in dependency order and validate each done-condition.
6. If reality invalidates the plan, update both language versions in the same turn.
7. Use `unity-cli-workflow` for Editor-owned operations and Unity validation.
8. Before handoff, compile source generators and audit new/touched `*Error` types;
   do not report success while a touched Result error is a flat enum.
9. Report which EncosyTower modules were used. For hand-written infrastructure, state briefly why no package module fit.

## Hard rules

- Preserve `.meta` files and GUIDs when moving Unity assets.
- Follow `CODING-CONVENTIONS.md` for all C#.
- Mark every type using an EncosyTower source-generator attribute `partial`.
- Never hand-edit `*.gen.cs`.
- Never use a flat enum as `TError` in `Result<T, TError>`. Model gameplay/domain
  failures with `[PolyEnumFactoryFor]` and `[PolyEnumStruct]`, including an
  `Undefined` case, `FixedString` messages, `Prefix(...)`, and typed contextual
  payloads where useful. Flat enums remain valid only for state/category values
  that are not error contracts.
- Keep PubSub messages on `IMessage` and Processing requests on the appropriate `IRequest*` contract while strict mode is active.
- Use the package's `UnityTask` alias pattern instead of directly coupling gameplay code to UniTask or `Awaitable`.
- Do not add `com.unity.entities` to repair the deliberately DOTS-free Apexion stats fork.
- Never upgrade Unity or EncosyTower merely to complete an unrelated task.
