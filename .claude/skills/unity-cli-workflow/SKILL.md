---
name: unity-cli-workflow
description: REQUIRED whenever a task touches the Unity Editor rather than plain C# — scenes, prefabs, ScriptableObject assets, inspector wiring, Addressables, imports, compilation checks, EditMode/PlayMode tests, builds, or Unity log diagnosis. This project drives Unity through the official Unity CLI and the Unity Pipeline package; Unity MCP is deliberately NOT used. Load it before proposing any Editor operation, before running tests or a build, and when a Unity change needs verification beyond "the code looks right". Triggers on "scene", "prefab", "inspector", "asset", "Addressables", "compile", "build", "PlayMode", "EditMode", "test", "Unity log", "batch mode", and Vietnamese phrasing "mở Unity", "chạy test", "build game", "kéo thả inspector", "sửa scene", "lỗi compile".
---

# Unity CLI workflow — no MCP

The official Unity CLI plus the Unity Pipeline package is this project's Unity integration.
**Never require, install, or invoke Unity MCP or `unity-mcp-skill`** unless the user explicitly
reverses that policy. See `AGENTS.md` at the repo root — the policy is repo-wide, not skill-local.

Shell here is PowerShell. Use the `PowerShell` tool for CLI calls; `;` + `if ($?)` instead of `&&`.

**`references/commands.md` holds the verified command surface** — exact flags for `unity test`,
`unity command`, batch mode, plus this machine's probed state. Read it before composing a command;
what follows is the decision logic, not the syntax.

## Known state of this project (probed 2026-08-15 — re-verify if a command fails)

| | |
|---|---|
| `unity` CLI | `1.0.0-beta.3`, on `PATH` |
| Project editor | `6000.3.20f1`, installed |
| `com.unity.test-framework` | `1.6.0` — **tests can run** |
| Unity Pipeline package | **INSTALLED and live** — `unity command` works against a running Editor (corrected 2026-08-15) |

## Establish project state before acting

1. The repo root holding `Assets/`, `Packages/`, `ProjectSettings/` is the project path.
2. Read `ProjectSettings/ProjectVersion.txt` before choosing an Editor — never upgrade the project
   just to run a task.
3. Probe the real local interface instead of assuming CLI syntax (it moves between versions):

   ```powershell
   unity --version ; unity doctor ; unity editors ; unity status
   ```

   `unity status` answers "is this project already open?" — do not guess at it.
4. **Do not judge Pipeline availability from `Packages/manifest.json`** — it is not listed there and
   grepping for "pipeline" gives a false negative. Ask the Editor instead:
   `unity command --project-path="<abs>"` with no command lists what it exposes.
5. Report a missing CLI, missing matching Editor, license/auth failure, or missing Pipeline package
   **precisely**. Do not silently install Editors, modules, or packages — installation is only in
   scope when the user's request requires it.
6. Pass `--non-interactive` on every unattended invocation; a prompt has no TTY here and will hang.

## Choose the least invasive route

In order of preference:

| Situation | Route |
|---|---|
| C# source, asmdefs, package files, config, text-serialized assets | Edit the file directly |
| Running EditMode/PlayMode tests, or proving the project compiles | **`unity test`** — a first-class command; never hand-roll this in batch mode |
| Producing a player build | **`unity build`** |
| An interactive Editor instance is genuinely needed | `unity open <project-path>` |
| The Editor must inspect or mutate scenes, prefabs, imported assets, Editor state | **`unity command`** — Pipeline is installed; this is the preferred route when an Editor is already open |
| Pipeline exposes no suitable operation | Batch mode + a project-local `-executeMethod` script |

Never hand-edit opaque binary assets. Never treat `Library/`, `Temp/`, `Logs/`, `.sln` or `.csproj`
as source of truth — they are generated.

## Control a running Editor

1. Always target the project explicitly:

   ```powershell
   unity command --project-path="<absolute-project-path>"
   ```

2. Read the discovered command list, then that command's help, **before** invoking it. Pipeline
   commands are extensible and differ by installed package version.
3. One logically scoped mutation at a time. Capture its output, then verify the resulting files or
   Editor state.
4. If Pipeline is missing on Unity 6+, say plainly that `unity pipeline install` mutates
   `Packages/manifest.json` and the lock file. Install only when the task authorizes it.

## Validate proportionally

1. Confirm Unity compilation succeeds; on failure read the **complete** Editor log, not the tail.
   There is no `unity compile` — the cheapest honest check is a filtered `unity test` run, which
   cannot start until the project compiles.
2. Targeted EditMode tests for editor/data logic, targeted PlayMode tests for runtime behaviour:

   ```powershell
   unity test . --mode EditMode --non-interactive --filter "<Namespace.Suite>" `
       --output "<scratchpad>/editmode-results.xml"
   ```

   Always pass `--mode` explicitly, always redirect `--output` to the scratchpad rather than
   dirtying the repo, and pass `--timeout` on PlayMode runs — it is disabled by default, so a hung
   run blocks forever. Widen to the surrounding suite only after the targeted ones pass.
3. Run a build only when the task needs one; do not implicitly install build-support modules.
4. Evidence is: exit code, the NUnit XML report **with its counts**, build report, Unity log.
   **"The Editor opened" is not evidence of success**, and a green exit code with zero tests
   executed is a failed run, not a pass.
5. Summarize the command run, its result, and explicitly name any validation you did not run.

Test assemblies in this repo: `ApexionGame.Tests.EditorMode`, plus the package's own
`EncosyTower.Tests.EditorMode` / `EncosyTower.Tests.PlayMode`. There is no PlayMode test assembly of
this project's own yet — say so rather than implying PlayMode coverage exists.

Source generators (EncosyTower's `[PolyEnumStruct]`, `[Database]`, `[ObservableProperty]`, …) only
run inside a Unity compile. IDE/Roslyn analysis in the editor is **not** sufficient proof that
generated members exist — compile through Unity before reporting a source-gen feature done.

## Batch-mode fallback

Last resort — reach for it only after `unity test` / `unity build` / `unity command` have been ruled
out. Resolve the matching `Unity.exe` from `unity editors`, prefer a committed or task-local Editor
script with a static entry point, then use the invocation in `references/commands.md` §7.

Always pass an explicit `-logFile` and read the **complete** log — the first compiler error is at the
top, and the interesting one is rarely last. `-accept-apiupdate` only when API migration is
intentionally allowed. Keep the automation script when it is reusable; delete it when it was a
one-off.

If `unity status` shows the project already open in another Editor process and the operation cannot
safely attach, **stop and report the conflict** rather than launching a competing batch process.

## Safety and project conventions

- Preserve `.meta` files and GUID relationships when moving or creating assets — a lost `.meta`
  breaks every reference to that asset. This bites hardest on the folder moves that
  `encosy-tower`'s structure rules ask for.
- Let Unity create new `.meta` files where practical; if a text asset is created outside the Editor,
  verify its import through Unity.
- Never allow concurrent Editor writes to the same project.
- Never treat `Library/`, `Temp/`, `Logs/`, `.sln`, `.slnx` or `.csproj` as source of truth — all
  generated. This repo keeps generated `.csproj` files at the root; do not edit them.
- Write test reports, batch logs and scratch scripts to the session scratchpad, not into the repo.
- `AGENTS.md`, `CLAUDE.md`, and `CODING-CONVENTIONS.md` apply on top of this workflow. For anything
  touching C# architecture, load `encosy-tower` as well.

## Reference

- `references/commands.md` — verified flags for `unity test` / `command` / `build` / batch mode,
  this machine's probed state, and what counts as evidence.

## See also — the portable `midcore-*` skills

This skill owns **how to drive Unity on this machine**. The `midcore-*` set owns what to run and why,
and defers here through `.claude/project-profile.md` → `authority_skills.unity_operations`.

| The question | Skill |
|---|---|
| *Which* tests are worth writing, and what counts as verified | `midcore-testing` |
| Which gates a build must pass before it can be published, and CI beyond this machine | `midcore-release-pipeline` |
| Whether a measured performance change is actually evidence | `midcore-perf-budget` |
| Content validation as part of the suite and the release gate | `midcore-data-pipeline` |
| The save-migration matrix those test runs execute | `midcore-save-migration` |

Practical split: `midcore-testing` decides that a persisted-type change requires the full migration
matrix; this skill supplies the `unity test` invocation that runs it and the rule that a green run
with zero tests executed is a failure.

Also project-level: `system-design` (feature design gate), `coding-standards` (house style),
`refactoring`, `debugging`. When a failure only reproduces in a built player, `debugging` routes
back here — that class of symptom is documented in this skill, not re-derived there.
