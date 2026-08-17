# UI Toolkit — how this project and EncosyTower build interfaces

Read before writing any editor window, inspector, settings page, or runtime debug/HUD panel.

## 1. The rule that surprises people: UI is written in C#, not UXML

Across the whole package there are **27 `.uss` files and exactly one `.uxml`**. Every window,
element, and panel is a `VisualElement` subclass assembled in a C# constructor. UXML is used only
where Unity forces it (a Project Settings resources file).

Two independent reasons are recorded in the code:

- **Editor UI** — a `VisualElement` subclass is reusable, unit-addressable, and gets USS class-name
  constants, which UXML cannot express as strongly.
- **Runtime UI** — `Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Samples.Rts/Rts.Game/Hud/RtsWidgets.cs`
  states it outright: *"A UXML asset's main-object id is content-derived, so a hand-authored scene
  cannot reference one reliably — and the scene here is hand-authored precisely so it stays readable
  in a diff."*

**So: do not create UXML.** Build the tree in C#. Only reach for UXML if a designer will edit the
layout in UI Builder, and say why.

## 2. Folder layout

The project's own reference implementation is
`Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Editor`:

```text
ApexionGame.Entities.Stats.Editor/
├── StatDebuggerWindow.cs            EditorWindow — menu item, CreateGUI, owns nothing else
├── Views/
│   ├── StatDebuggerView.cs          VisualElement — structure only
│   ├── StatDebuggerViewController.cs logic, data, refresh
│   └── StatGraphView.cs             a second element, its own file
└── StyleSheets/
    ├── StatDebuggerWindow.tss       theme (runtime themes / palette entry point)
    ├── StatDebuggerWindow.uss       shared rules
    ├── StatDebuggerWindow_Dark.uss  dark-only overrides
    └── StatDebuggerWindow_Light.uss light-only overrides
```

Runtime UI drops the `StyleSheets/` folder when it styles inline —
`Packages/com.apexion.apexion-game/ApexionGame.Entities.Stats.Samples.Rts/Rts.Game/Hud/` holds
`RtsHud.cs`, one file per panel (`RtsTopBar`, `RtsSpellBar`, `RtsJournalPanel`, `RtsCommandPanel`,
`RtsGraphPanel`, `RtsInspectorPanel`, …), plus `IRtsHudHost.cs`, `RtsHudTheme.cs`, `RtsWidgets.cs`,
and `Ui/RtsPanelSettings.asset`.

Both reference implementations are in the embedded package, **not** under `Samples~/` — that folder
holds only `ApexionGame.Core.Samples`.

- `Views/` and `StyleSheets/` are in the approved sub-folder vocabulary and **add no namespace
  segment** — `Views/StatDebuggerView.cs` is still `ApexionGame.Entities.Stats.Editor`.
- Editor UI lives in a sibling `*.Editor` assembly, never in the runtime one.
- One element per file, matching the `Type.cs` / `Type+Nested.cs` rule. USS mirrors it:
  `GenericMenuPopup.uss` and `GenericMenuPopup+MenuNode.uss`.

## 3. The View / ViewController split

Every non-trivial element is two classes. `VisualCommanderView` + `VisualCommanderViewController`
and `StatDebuggerView` + `StatDebuggerViewController` both follow it.

| | Owns | Never |
|---|---|---|
| `XView : VisualElement` | the element tree, USS class names, exposed child containers | data, refresh logic, callbacks |
| `XViewController : IDisposable` | data, pools, event wiring, `Refresh()` | building the tree |

The controller is reachable from the element through `userData`, which is how a page tears it down:

```csharp
public VisualCommanderViewController([NotNull] VisualCommanderView view)
{
    _view = view;
    _view.userData = this;   // so anyone holding the element can find and dispose the controller
}
```

Both implement the full dispose pattern with a `_isDisposed` guard and the comment
`// Detect redundant Dispose() calls.`

## 4. USS class names

Declare them as `public static readonly string` constants on the element, block-first, and derive
the rest by interpolation. The convention is BEM-ish: `block`, `block__element`, `block--modifier`.

```csharp
public static readonly string UssClassName = "visual-commander";
public static readonly string DirectoryScrollUssClassName = $"{UssClassName}__directory--srcoll";
public static readonly string CommandContainerUssClassName = $"{UssClassName}__command--container";
```

The constructor's first line is `AddToClassList(UssClassName);`. Reuse Unity's own constants when
extending a built-in: `Foldout.inputUssClassName`, `Foldout.textUssClassName`.

Names are **kebab-lower**. `NameCasing.KebabLower.ConvertName(...)` produces them for generated ids.

## 5. Loading stylesheets

Never hard-code an absolute path inline. Build it from the shared root constant, per module:

```csharp
private const string MODULE_ROOT = $"{EditorStyleSheetPaths.ROOT}/EncosyTower.Editor/Scenes";
private const string STYLE_SHEETS_PATH = $"{MODULE_ROOT}/StyleSheets";
private const string FILE_NAME = nameof(SceneListWindow);

public const string THEME_STYLE_SHEET = $"{STYLE_SHEETS_PATH}/{FILE_NAME}.uss";

private void CreateGUI()
{
    rootVisualElement.WithEditorStyleSheet(THEME_STYLE_SHEET);
    // …
}
```

`EditorStyleSheetPaths.ROOT` is `"Packages/com.laicasaane.encosy-tower"`. A project-side window
uses its own `Assets/...` root the same way.

Available loaders (`EncosyTower.Editor.UIElements.EncosyEditorUIElementExtensions`):

| Call | Source |
|---|---|
| `WithEditorStyleSheet(path)` | `AssetDatabase.LoadAssetAtPath<StyleSheet>` |
| `WithEditorBuiltInStyleSheet(name)` | `EditorGUIUtility.Load` — Unity's built-in sheets |
| `WithEditorStyleSheet(dark, light)` | picks by `EditorAPI.IsDark` |
| `WithStyleSheet(styleSheet)` | runtime-safe, in `EncosyTower.UIElements` |

For a **runtime** panel, serialize a `StyleSheet` field on the MonoBehaviour and pass it to
`WithStyleSheet(...)` — there is no `AssetDatabase` at runtime. Or style inline and skip USS, as the
RTS HUD does.

## 6. The fluent `With*` builders

`EncosyTower.UIElements.EncosyUIElementExtensions` (runtime-safe) returns `self`, so trees read as
expressions: `WithClass`, `WithName`, `WithChild`, `WithDisplay`, `WithStyleSheet`, `WithCloneTree`,
`WithAlignFieldClass`. The editor set adds `WithBind`, `WithBindProperty`, `WithEditorStyleSheet(s)`.

```csharp
var table = new SimpleTableView<ItemInfo> {
    bindingPath = nameof(ItemCollectionAsset.items),
    showBoundCollectionSize = false,
}.WithEditorStyleSheets();
```

## 7. Runtime panels

```csharp
[RequireComponent(typeof(UIDocument))]
public partial class VisualCommanderPage : MonoPageBase, IPageOnAfterShow, IPageOnBeforeHide
{
    private UIDocument _document;
    private VisualCommanderView _view;

    public void OnAfterShow(PageContext context)
        => _view = VisualCommanderAPI.CreateView(_document.rootVisualElement, _directoryListWidth);

    public void OnBeforeHide(PageContext context)
    {
        if (_view?.userData is VisualCommanderViewController controller)
        {
            controller.Dispose();
        }

        _view = null;
        _document.rootVisualElement?.Clear();
    }
}
```

- `[RequireComponent(typeof(UIDocument))]`, get the document, add into `rootVisualElement`.
- Tear down by disposing the controller found through `userData`, then `Clear()` the root.
- A `PanelSettings` asset is required on the `UIDocument`; the project has
  `Rts.Game/Hud/Ui/RtsPanelSettings.asset` as a precedent, and
  `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss` is the default theme.
- Full-screen containers use `pickingMode = PickingMode.Ignore` so world clicks pass through, and
  `panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screenPosition))` answers "is the pointer over
  a real control".

## 8. Theme constants for inline-styled runtime UI

One static class holding every colour and size, so the look is editable in one place —
`RtsHudTheme`:

```csharp
public static class RtsHudTheme
{
    public static readonly Color PanelBg = new(0.05f, 0.06f, 0.08f, 0.93f);
    public static readonly Color Ink = new(0.87f, 0.89f, 0.93f);
    public static readonly Color Accent = new(0.96f, 0.79f, 0.33f);

    public const float CommandColumnWidth = 224f;
    public const float TopBarHeight = 44f;
}
```

Paired with a `*Widgets` static factory class (`Row()`, `Column()`, `Box(title)`, `Label(...)`,
`Button(text, onClick)`) so panels never repeat style code.

## 9. Custom elements usable in UI Builder

```csharp
[UxmlElement(libraryPath = "Encosy Tower")]
public partial class EnableableFoldout : VisualElement, IHasBindingPath
```

`[UxmlElement]` requires `partial`. `libraryPath` groups the element in UI Builder's library.
Implement `IHasBindingPath` when the element wraps a bindable field, and override
`contentContainer` when children should land inside a sub-element:

```csharp
public override VisualElement contentContainer => Foldout.contentContainer;
```

## 10. Conventions carried over from the rest of the codebase

- Callback methods are named `<Element>_On<Event>`: `EnableToggle_OnValueChanged`,
  `SearchField_OnValueChanged`, `IncludePackagesToggle_OnValueChanged`.
- Name elements for lookup with kebab-lower `name`: `name = "refresh-button"`.
- Query with `Q<T>()` / `Q(className:)`; Unity's internals are addressable —
  `scroll.Q("unity-content-container")`.
- `hierarchy.Add` / `hierarchy.Insert` when bypassing `contentContainer`; plain `Add` otherwise.
- Pool repeated row views with `UnityEngine.Pool.ObjectPool<T>` plus a `*API.CreateView` /
  `*API.ReleaseView` pair, as `VisualCommanderViewController` does for its command rows.
- Editor-only files are wrapped in `#if UNITY_EDITOR` and members carry `[ApiForEditor]`.
- `EncosyTower.Core/UIElements` holds runtime-safe elements; `EncosyTower.Core/Editor.UIElements`
  holds editor ones — the folder is dotted because the namespace differs
  (`EncosyTower.Editor.UIElements`).

## 11. Existing pieces to reuse before writing your own

| Need | Use |
|---|---|
| A table with columns bound to a serialized list | `SimpleTableView<T>` |
| A foldout with an enable toggle in its header | `EnableableFoldout` |
| A text field with a trailing button | `ButtonTextField` |
| A searchable popup menu | `GenericMenuPopup` |
| A visual divider | `VisualSeparator` |
| Safe-area padding on device | `SafeArea` |
| Editor icons by name | `EditorIcons` |
| Fuzzy filtering for a search field | `FuzzySearchAPI.Search` |
