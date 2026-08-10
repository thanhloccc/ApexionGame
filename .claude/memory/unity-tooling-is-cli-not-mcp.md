---
name: unity-tooling-is-cli-not-mcp
description: This project drives Unity through the official Unity CLI and Pipeline package — Unity MCP is deliberately not used
metadata:
  type: project
---

As of 2026-08-09 the project's Unity integration is the official **Unity CLI** plus the Unity
Pipeline package, with Editor batch mode + `-executeMethod` as the fallback. Unity MCP and
`unity-mcp-skill` are deliberately out of use; the policy is recorded in `AGENTS.md` at the repo
root and applies to every agent working here.

**Why:** the user moved off MCP intentionally. Suggesting or installing it re-adds a dependency they
removed, and an agent that assumes MCP is available will stall on Editor work instead of reaching
for the CLI route that actually exists.

**How to apply:** load `.claude/skills/unity-cli-workflow/` before any Editor-owned operation —
scenes, prefabs, imports, inspector state, compilation checks, EditMode/PlayMode tests, builds.
Prefer direct file edits when they fully accomplish the task; probe `unity --help` / `unity editors
-i` / `unity pipeline list` rather than assuming CLI syntax. Source generators only run inside a
Unity compile, so IDE analysis never proves a generated member exists
([[encosy-sourcegen-requires-partial]], [[result-errors-are-polyenum-structs]]).
