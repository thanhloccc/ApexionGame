---
name: house-style-is-encosy-conventions
description: CODING-CONVENTIONS.md at the repo root is EncosyTower's own style guide and governs all C# written in this project
metadata:
  type: project
---

`CODING-CONVENTIONS.md` (39 KB, repo root) is EncosyTower's own C# style guide, checked into this
project as the house style for all code — not just package code.

The rules that most often get violated by default output: braces on **every** control block
including single-statement `if`; a blank line above and below any statement opening a `{ }` scope;
leading-comma wrapping for multi-parameter signatures and calls; field groups ordered
`const` → `static readonly` → `static` → instance; `_camelCase` private fields and `s_camelCase`
private statics; `Option<T>` / `Result<T,TError>` instead of `null` or `bool`+`out` for recoverable
failures; `[MethodImpl(AggressiveInlining)]` on hot paths and `NoInlining` +
`[HideInCallstack, StackTraceHidden]` + `[Conditional(...)]` on cold throw/log helpers.

**Why:** the user adopted EncosyTower wholesale, conventions included, so project code should be
indistinguishable in style from package code.

**How to apply:** read the relevant section of `CODING-CONVENTIONS.md` before writing a new file;
a digest lives in `.claude/skills/encosy-tower/references/setup.md`. Good exemplar files the doc
itself names: `Collections/FasterList.cs`, `Common/Option.cs`, `Vaults/SingletonVault.cs`,
`PubSub/Publishers/CachedPublisher.cs`, `Logging/DevLogger.cs`.
