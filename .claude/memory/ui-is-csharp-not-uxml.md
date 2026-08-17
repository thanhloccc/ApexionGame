---
name: ui-is-csharp-not-uxml
description: All UI Toolkit work in this project is built in C# VisualElements, never UXML — with a recorded reason.
metadata:
  type: project
---

Every UI Toolkit surface here — editor windows, inspectors, settings pages, runtime HUDs — is
assembled in C# as `VisualElement` subclasses. EncosyTower ships 27 `.uss` files and exactly one
`.uxml` (and that one exists only because Unity's Project Settings resources force it).

**Why:** `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Samples.Rts/Rts.Game/Hud/RtsWidgets.cs`
records it — a UXML asset's main-object id is content-derived, so a hand-authored scene cannot reference one
reliably. Scenes in this project are hand-authored so they stay readable in a diff, which rules UXML
out. Editor-side, a `VisualElement` subclass also gets reusable USS class-name constants that UXML
cannot express.

**How to apply:** do not create `.uxml`. Follow the layout and patterns in
`.claude/skills/encosy-tower/references/ui-toolkit.md` — `Views/` + `StyleSheets/`, the
View/ViewController split linked through `view.userData`, USS class-name constants, and the fluent
`With*` builders. Reference implementations to copy: `ApexionGame.Entities.Stats.Editor` for editor
UI, `Rts.Game/Hud` (theme class + widget factory + one file per panel) for runtime UI.

Only reach for UXML if a designer will edit the layout in UI Builder, and say why.

Related: [[follow-encosy-structure-and-naming]], [[encosy-tower-is-standard-library]]
