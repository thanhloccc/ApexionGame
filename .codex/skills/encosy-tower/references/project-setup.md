# Project setup and gates

Re-check the repository because package versions and defines can change.

- Unity project version is defined by `ProjectSettings/ProjectVersion.txt`.
- EncosyTower lives at `Packages/com.laicasaane.encosy-tower` and is the standard library.
- `com.unity.entities` is absent. EncosyTower Entities modules are compiled out.
- `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats` is a deliberate DOTS-free fork. Edit the fork; do not install Entities or sync it back to the package implementation.
- Gameplay assemblies began as greenfield scaffolding: `Game.Common`, `Game.Gameplay`, and `ApexionGame.Core`. Inspect current state rather than assuming they remain empty.
- New assemblies normally reference `EncosyTower.Core` and must copy the relevant `versionDefines` from `EncosyTower.Core.asmdef` so conditional symbols resolve consistently.
- Add `EncosyTower.Mvvm`, `EncosyTower.Core.Extended`, editor assemblies, or Unity packages only when the selected module requires them.
- Use the existing package gates for UniTask, Addressables, Localization, Collections, Burst, Mathematics, uGUI/TMP, BakingSheet, Newtonsoft JSON, and FuzzySharp.

Source generation rules:

- Add `partial` to attributed types.
- Follow generated forwarding-body patterns such as `get => Get_X(); set => Set_X(value);`.
- Compile through Unity before trusting IDE-only missing-generated-member errors.
- Never hand-edit checked-in `.gen.cs` output.
- Keep authoring code editor-only where required.
