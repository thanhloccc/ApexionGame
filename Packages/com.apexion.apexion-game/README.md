# Apexion Game

Apexion Games' shared engine layer, built on top of [EncosyTower](../com.laicasaane.encosy-tower).

## Modules

- **ApexionGame.Core** — HFSM core utilities. See `ApexionGame.Core/Documentation~/`.
- **ApexionGame.Entities.Stats** (+ `.Authoring`, `.Editor`) — a deliberate DOTS-free fork of
  EncosyTower's Entities.Stats system. See `ApexionGame.Entities.Stats/Documentation~/`.
- **ApexionGame.Entities.Stats.Samples.Rts** (`Rts.Core`, `Rts.Game`) — DOTS-free Entities.Stats RTS
  reference implementation. Always compiled (not an opt-in sample) because
  `ApexionGame.Tests.EditorMode` regression-tests it directly.
- **ApexionGame.Editor** — editor-only support for the modules above.
- **ApexionGame.Tests.EditorMode** — EditMode tests.

## Samples

Import via Package Manager → Apexion Game → Samples:

- **ApexionGame.Core.Samples** — HFSM usage sample.

## Package overview

See `Documentation~/Core Package - Overview.md`.
