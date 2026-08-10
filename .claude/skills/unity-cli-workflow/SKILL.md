---
name: unity-cli-workflow
description: REQUIRED whenever a task touches the Unity Editor rather than plain C# — scenes, prefabs, ScriptableObject assets, inspector wiring, Addressables, imports, compilation checks, EditMode/PlayMode tests, builds, or Unity log diagnosis. This project drives Unity through the official Unity CLI and the Unity Pipeline package; Unity MCP is deliberately NOT used. Load it before proposing any Editor operation, before running tests or a build, and when a Unity change needs verification beyond "the code looks right". Triggers on "scene", "prefab", "inspector", "asset", "Addressables", "compile", "build", "PlayMode", "EditMode", "test", "Unity log", "batch mode", and Vietnamese phrasing "mở Unity", "chạy test", "build game", "kéo thả inspector", "sửa scene", "lỗi compile".
---

# Unity CLI workflow — no MCP

The official Unity CLI plus the Unity Pipeline package is this project's Unity integration.
**Never require, install, or invoke Unity MCP or `unity-mcp-skill`** unless the user explicitly
reverses that policy. See `AGENTS.md` at the repo root — the policy is repo-wide, not skill-local.

Shell here is PowerShell. Use the `PowerShell` tool for CLI calls; `;` + `if ($?)` instead of `&&`.

## Establish project state before acting

1. The repo root holding `Assets/`, `Packages/`, `ProjectSettings/` is the project path.
2. Read `ProjectSettings/ProjectVersion.txt` before choosing an Editor — never upgrade the project
   just to run a task.
3. Probe the real local interface instead of assuming CLI syntax (it moves between versions):

   ```powershell
   unity --version
   unity --help
   unity editors -i
   unity auth status
   ```

4. Check whether Pipeline is already installed before proposing it: `unity pipeline list`, and
   inspect `Packages/manifest.json` when that is ambiguous.
5. Report a missing CLI, missing matching Editor, license/auth failure, or missing Pipeline package
   **precisely**. Do not silently install Editors, modules, or packages — installation is only in
   scope when the user's request requires it.

## Choose the least invasive route

In order of preference:

| Situation | Route |
|---|---|
| C# source, asmdefs, package files, config, text-serialized assets | Edit the file directly |
| An interactive Editor instance is genuinely needed | `unity open <project-path>` |
| The Editor must inspect or mutate scenes, prefabs, imported assets, Editor state | Unity Pipeline commands |
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
2. Targeted EditMode tests for editor/data logic, targeted PlayMode tests for runtime behaviour.
   Widen to the surrounding suite only after the targeted ones pass.
3. Run a build only when the task needs one; do not implicitly install build-support modules.
4. Evidence is: exit code, test result artifact, build report, Unity log. **"The Editor opened" is
   not evidence of success.**
5. Summarize the command run, its result, and explicitly name any validation you did not run.

Source generators (EncosyTower's `[PolyEnumStruct]`, `[Database]`, `[ObservableProperty]`, …) only
run inside a Unity compile. IDE/Roslyn analysis in the editor is **not** sufficient proof that
generated members exist — compile through Unity before reporting a source-gen feature done.

## Batch-mode fallback

Resolve the matching `Unity.exe` from the installed Editor list, prefer a committed or task-local
Editor script with a static entry point, then:

```powershell
& "<Unity.exe>" -batchmode -quit -projectPath "<absolute-project-path>" `
    -executeMethod Namespace.Type.Method -logFile "<log-path>"
```

Add `-accept-apiupdate` only when API migration is intentionally allowed. Always use an explicit log
path and read the full log. Keep the automation script when it is reusable.

If the project is already open in another Editor process and the operation cannot safely attach,
**stop and report the conflict** rather than launching a competing batch process.

## Safety and project conventions

- Preserve `.meta` files and GUID relationships when moving or creating assets — a lost `.meta`
  breaks every reference to that asset.
- Let Unity create new `.meta` files where practical; if a text asset is created outside the Editor,
  verify its import through Unity.
- Never allow concurrent Editor writes to the same project.
- `AGENTS.md`, `CLAUDE.md`, and `CODING-CONVENTIONS.md` apply on top of this workflow. For anything
  touching C# architecture, load `encosy-tower` as well.
