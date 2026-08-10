# Unity tooling

- Use the official Unity CLI as the primary interface for opening, inspecting, testing, building, and automating this Unity project.
- When Editor control is required, use the Unity Pipeline package and its local CLI commands.
- Do not require, install, or use Unity MCP or `unity-mcp-skill` for this project unless the user explicitly asks to restore it.
- Directly edit project source and text-based Unity assets when appropriate. If a required operation is unavailable through Unity CLI, use Unity Editor batch mode and a project-local `-executeMethod` automation script.
- Before relying on Unity CLI commands, detect whether the CLI, the matching Unity Editor version, and the Pipeline package are available; report missing prerequisites clearly.

# Project workflow and memory

- Read `.codex/memory/MEMORY.md` before planning or implementing project work; verify facts that may have changed against the repository.
- Use the project-local `encosy-tower` skill for feature planning, implementation, architecture, performance-sensitive collections/math, asmdefs, folders, namespaces, or EncosyTower APIs.
- For a new feature request, follow the doc-first review gate recorded in the memory and skill. This does not delay bug fixes, focused edits, investigations, or explicit requests to skip documentation.
