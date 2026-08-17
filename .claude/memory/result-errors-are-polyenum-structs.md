---
name: result-errors-are-polyenum-structs
description: A TError in Result<T,TError> must be an EncosyTower PolyEnumStruct union, never a flat enum — the payload is the whole point
metadata:
  type: project
---

Every gameplay/domain type used as `TError` in `Result<T, TError>` is modelled as a
`[PolyEnumFactoryFor]` wrapper over a nested `[PolyEnumStruct]` case union: an `Undefined` case,
`FixedString512Bytes` messages, a `Prefix(...)` method, and typed immutable payloads. Flat enums
stay legal only for states, modes, flags and categories that are never returned as an error.

**Why:** a flat `enum FooError` discards the context the caller needs — *which* id was missing,
*what* the state actually was, *how much* was short. The PolyEnum shape keeps that payload while
staying unmanaged and Burst-friendly, and produces a real message instead of a symbol name.

**How to apply:** copy the shape from
`Packages/com.apexion.apexion-game/ApexionGame.Core/HFSM/MachineError.cs` — including its
`ToFixedString()`, which keeps logging and Burst callers allocation-free. Its call sites are in
`MachineBuilder`2+Validate.cs` and its tests in `ApexionGame.Tests.EditorMode`. Call sites use
generated factories **with parentheses** — `MachineError.UnknownState(ordinal)`. `*Error` types
inside `EncosyTower.Core` predate this rule and are not a precedent. (Earlier notes pointed at
`Assets/Game/…/PlayerError.cs`, `WeaponError`, `EquipmentError` and a legacy
`PlayerPersistenceError` — none of those files exist; see [[gameplay-assembly-map]].)
Full rules, case/payload guidance and the
pre-handoff audit commands: `.claude/skills/encosy-tower/references/structured-errors.md`.
PolyEnum factories are source-generated, so verify through a Unity compile
([[unity-tooling-is-cli-not-mcp]]), not the IDE — related: [[encosy-sourcegen-requires-partial]],
[[encosy-tower-is-standard-library]].
