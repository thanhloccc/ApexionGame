---
name: unity-cli-workflow
description: Operate and validate Unity projects with the official Unity CLI and Unity Pipeline package, without Unity MCP. Use for Unity Editor discovery and launch, project inspection, compilation checks, Editor commands, EditMode or PlayMode tests, builds, automation, and Unity log diagnosis; also use when a Unity task needs a batch-mode fallback.
---

# Unity CLI Workflow

Use the official Unity CLI as the primary Unity integration. Never require, install, or invoke Unity MCP or `unity-mcp-skill` unless the user explicitly reverses that project policy.

## Establish project state

1. Treat the repository root containing `Assets/`, `Packages/`, and `ProjectSettings/` as the project path.
2. Read `ProjectSettings/ProjectVersion.txt` before choosing an Editor.
3. Check the actual local interface instead of assuming experimental CLI syntax:

   ```powershell
   unity --version
   unity --help
   unity editors -i
   unity auth status
   ```

4. Check whether the project already contains the Unity Pipeline package before proposing installation. Use `unity pipeline list` and inspect `Packages/manifest.json` when needed.
5. Report a missing CLI, matching Editor, license/authentication, or Pipeline package precisely. Do not silently install Editors, modules, or packages unless installation is part of the user's request or required by a requested implementation.

## Choose the least invasive route

- Edit C# source, asmdefs, package files, configuration, and text-serialized Unity assets directly when that fully accomplishes the task.
- Use `unity open <project-path>` when an interactive Editor instance is required.
- Use Unity Pipeline commands when an Editor must inspect or mutate scenes, prefabs, imported assets, or other Editor-owned state.
- Use Editor batch mode with a project-local static `-executeMethod` only when the CLI/Pipeline exposes no suitable operation.
- Do not hand-edit opaque binary assets. Do not edit generated `Library/`, `Temp/`, `Logs/`, solution, or project files as source-of-truth changes.

## Control a running Editor

1. Prefer connecting to the explicit project:

   ```powershell
   unity command --project-path="<absolute-project-path>"
   ```

2. Read the discovered command list and each relevant command's help before invoking it. Pipeline commands are extensible and can differ by installed package version.
3. Make one logically scoped mutation at a time, capture its output, and verify the resulting files or Editor state.
4. If Pipeline is missing and the project uses Unity 6 or newer, explain that `unity pipeline install` changes `Packages/manifest.json` and the lock file. Install it only when authorized by the task.

## Validate changes

Use validation proportional to the change:

1. Confirm Unity compilation succeeds and inspect the complete Editor log on failure.
2. Run targeted EditMode tests for editor/data logic and targeted PlayMode tests for runtime behavior. Expand to the relevant suite after targeted tests pass.
3. Run the requested build target only when needed; do not install build-support modules implicitly.
4. Treat a command exit code, test result artifact, build report, and Unity log as evidence. Do not infer success merely because the Editor opened.
5. Summarize the command used, result, and any validation not run.

## Batch-mode fallback

Resolve the matching `Unity.exe` path from the installed Editor list. Prefer a committed or task-local Editor script with a static entry point, then run the equivalent of:

```powershell
& "<Unity.exe>" -batchmode -quit -projectPath "<absolute-project-path>" -executeMethod Namespace.Type.Method -logFile "<log-path>"
```

Add `-accept-apiupdate` only when API migration is intentionally allowed. Use an explicit log path, inspect the full log, and preserve the automation script when it is reusable. If the project is already open in another Editor process and the operation cannot safely attach, stop and report the conflict rather than launching a competing batch process.

## Safety and project conventions

- Preserve `.meta` files and GUID relationships when moving or creating assets.
- Let Unity create new `.meta` files whenever practical; if text assets are created outside the Editor, verify their import through Unity.
- Avoid concurrent Editor writes to the same project.
- Follow repository `AGENTS.md` and coding conventions in addition to this workflow.
- Account for this project's Editor version from `ProjectVersion.txt`; never upgrade the project merely to run a task.
