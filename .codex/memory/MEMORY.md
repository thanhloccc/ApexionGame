# Project memory — ApexionGames: Land of Souls

This is durable project context. Verify dated or changeable facts against the repository before acting.

## Workflow

- New feature requests are doc-first. Create bilingual plans under the owning assembly's `Documentation~/` folder, then stop for review. Implement only after explicit approval. This gate does not cover bug fixes, focused edits, renames, formatting, questions, or investigation.
- Load `.codex/skills/encosy-tower` for project feature, implementation, performance, organization, or architecture work.
- Load `.codex/skills/unity-cli-workflow` for scenes, prefabs, assets, inspector wiring, Editor state, tests, builds, or Unity automation. Do not require Unity MCP.
- Review EncosyTower before implementing infrastructure and report which modules were used. Explain briefly whenever a hand-written alternative was necessary.

## Project facts

- EncosyTower at `Packages/com.laicasaane.encosy-tower` is the standard library, not an optional utility collection.
- DOTS/Unity Entities is not installed unless the current manifest proves otherwise. Player define leftovers are not evidence that DOTS is active.
- `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats` is a deliberate DOTS-free fork. Never repair it by installing Unity Entities or replacing it with the package's ECS version.
- `Game.Common`, `Game.Gameplay`, and `ApexionGame.Core` started as greenfield assemblies. Inspect current contents before relying on that historical state.
- New asmdefs must receive the required EncosyTower references and matching relevant `versionDefines`, or guarded code can silently disappear.
- Any type carrying an EncosyTower source-generator attribute must be `partial`. Generated accessor properties use the package's `Get_X`/`Set_X` forwarding pattern. Never hand-edit `.gen.cs`.

## Code and architecture preferences

- `CODING-CONVENTIONS.md` is the house style for all C#.
- Follow EncosyTower assembly, folder, namespace, file, type, and member organization. Subfolders do not automatically add namespace segments.
- Prefer EncosyTower/Unity high-performance collections and `Unity.Mathematics` in hot paths and jobs. Use ordinary BCL collections and UnityEngine math types where serialization, APIs, startup/UI code, or clarity make them the correct choice.
- Preserve Unity `.meta` files and GUID relationships.

## Error contracts — non-negotiable

- A gameplay/domain type used as `TError` in `Result<T, TError>` must never be a
  flat enum. Use an EncosyTower structured error with `[PolyEnumFactoryFor]` on the
  public outer wrapper and `[PolyEnumStruct]` on the inner case union.
- Every structured error must provide a safe `Undefined` default case,
  `FixedString512Bytes` messages, `Prefix(...)`, and typed immutable payloads when
  an ID, state, token, amount, or deadline makes the failure more actionable.
- Call sites must use generated factories such as `PlayerError.Dead()` and
  `PlayerError.UnknownDefinition(skillId)`. Enum-style members are forbidden.
- A plain enum is allowed only for state, mode, flags, or category values that are
  not used as a Result error contract.
- Validate payload message, prefix, and default/undefined behavior in tests, then
  compile through Unity to exercise PolyEnum source generation.
- Before completing any feature or review that touches errors, audit new/touched
  `*Error` types and `Result<T, TError>` call sites. Do not accept a touched flat
  enum error.
- Use EncosyTower's sample `PlayerDataError` and the project's `WeaponError` and
  `PlayerError` as references. The current `EquipmentError` enum is legacy debt
  and must not be copied; migrate it when the Equipment error surface is next
  touched.

## Source history

This Codex memory was reviewed and adapted from `.claude/memory/` on 2026-08-09. The Claude files remain historical source material; this file is the Codex-facing canonical summary.
