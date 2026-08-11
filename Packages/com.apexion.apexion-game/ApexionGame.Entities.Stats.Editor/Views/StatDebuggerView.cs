using System;
using System.Collections.Generic;
using ApexionGame.Entities.Stats.Debugging;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Editor.Views
{
    /// <summary>
    /// Task 5.1 — the live view of a registered store: pick a store, pick an owner, read its stats.
    /// </summary>
    /// <remarks>
    /// Every method here is split along one line: <b>structure</b> versus <b>values</b>. Structure
    /// (which stores exist, which owners, which stats) is rebuilt only when it actually changes;
    /// values are written into labels that already exist. The first draft rebuilt everything on a
    /// four-times-a-second poll, which reset scrolling, fought the user's selection, and made the
    /// graph flicker.
    /// <para>
    /// Search, sort and the modified-only filter are display-only concerns and live entirely in
    /// this view: they reorder/hide the same pooled rows rather than asking the controller for a
    /// different data shape, so <see cref="StatDebuggerViewController"/> stays unaware of them.
    /// </para>
    /// </remarks>
    public class StatDebuggerView : VisualElement
    {
        public static readonly string UssClassName = "stat-debugger";
        public static readonly string ToolbarUssClassName = $"{UssClassName}__toolbar";
        public static readonly string ToolbarFieldUssClassName = $"{ToolbarUssClassName}-field";
        public static readonly string ToolbarSpacerUssClassName = $"{ToolbarUssClassName}-spacer";
        public static readonly string ToolbarInfoUssClassName = $"{ToolbarUssClassName}-info";
        public static readonly string ToolbarToggleUssClassName = $"{ToolbarUssClassName}-toggle";
        public static readonly string ToolbarToggleLabelUssClassName = $"{ToolbarToggleUssClassName}-label";
        public static readonly string ToolbarButtonUssClassName = $"{ToolbarUssClassName}-button";
        public static readonly string WarningUssClassName = $"{UssClassName}__warning";
        public static readonly string BodyUssClassName = $"{UssClassName}__body";
        public static readonly string OwnersUssClassName = $"{UssClassName}__owners";
        public static readonly string OwnersHeaderUssClassName = $"{OwnersUssClassName}-header";
        public static readonly string OwnersTitleUssClassName = $"{OwnersUssClassName}-title";
        public static readonly string OwnersListUssClassName = $"{OwnersUssClassName}-list";
        public static readonly string DetailUssClassName = $"{UssClassName}__detail";
        public static readonly string SectionUssClassName = $"{UssClassName}__section";
        public static readonly string SectionHeaderUssClassName = $"{SectionUssClassName}-header";
        public static readonly string SectionTitleUssClassName = $"{SectionUssClassName}-title";
        public static readonly string SectionSearchUssClassName = $"{SectionUssClassName}-search";
        public static readonly string SectionToggleUssClassName = $"{SectionUssClassName}-toggle";
        public static readonly string SectionToggleLabelUssClassName = $"{SectionToggleUssClassName}-label";
        public static readonly string SectionSpacerUssClassName = $"{SectionUssClassName}-spacer";
        public static readonly string SectionSummaryUssClassName = $"{SectionUssClassName}-summary";
        public static readonly string StatsUssClassName = $"{UssClassName}__stats";
        public static readonly string RowUssClassName = $"{UssClassName}__row";
        public static readonly string RowHeaderUssClassName = $"{RowUssClassName}--header";
        public static readonly string RowModifiedUssClassName = $"{RowUssClassName}--modified";
        public static readonly string RowSelectedUssClassName = $"{RowUssClassName}--selected";
        public static readonly string RowEvenUssClassName = $"{RowUssClassName}--even";
        public static readonly string CellUssClassName = $"{UssClassName}__cell";
        public static readonly string CellSortableUssClassName = $"{CellUssClassName}--sortable";
        public static readonly string CellSortedUssClassName = $"{CellUssClassName}--sorted";
        public static readonly string EmptyUssClassName = $"{UssClassName}__empty";

        /// <remarks>
        /// Each column carries its own modifier class so widths live in USS rather than in code.
        /// The value columns need real room: a <c>float4</c> prints as
        /// <c>float4(1f, 2f, 3f, 4f)</c>, and in the first draft that text simply ran across its
        /// neighbours because nothing clipped it.
        /// </remarks>
        private static readonly (string label, string modifier)[] s_columns = {
            ("stat", "id"),
            ("base", "value"),
            ("current", "value"),
            ("mods", "count"),
            ("obs", "count"),
            ("userData", "data"),
            ("events", "flag"),
        };

        /// <summary>
        /// One tooltip per column in <see cref="s_columns"/>, spelling out what the abbreviation
        /// on the header actually means.
        /// </summary>
        private static readonly string[] s_columnHints = {
            "Stat name (or raw index, if no name lookup was wired up for this store). "
                + "Sorts by index either way. Click to sort.",
            "Authored value, before any modifier runs. Click to sort (compares formatted text).",
            "Value after every modifier has been applied. Click to sort (compares formatted text).",
            "How many modifiers are currently attached to this stat. Click to sort.",
            "How many other stats observe this one and recompute when it changes. Click to sort.",
            "Arbitrary uint tag your game code attached to the stat. Click to sort.",
            "Whether changing this stat raises change events for its observers. Click to sort.",
        };

        public event Action<int> StoreSelected;
        public event Action<int> OwnerSelected;
        public event Action RefreshRequested;
        public event Action<bool> AutoRefreshToggled;

        private readonly DropdownField _storeField;
        private readonly Label _info;
        private readonly Label _warning;
        private readonly ListView _ownerList;
        private readonly VisualElement _statRows;
        private readonly ScrollView _statsScroll;
        private readonly Label _statsSummary;
        private readonly StatGraphView _graph;
        private readonly Label _empty;
        private readonly VisualElement _body;

        private Label[] _headerCells;

        private readonly List<string> _ownerLabels = new();
        private readonly List<string> _ownerDisplay = new();
        private readonly List<int> _ownerDisplayToOriginal = new();
        private string _ownerFilter = string.Empty;
        private int _ownerSelectedOriginal = -1;

        private readonly List<Row> _rows = new();
        private readonly List<StatDebugInfo> _lastStats = new();
        private readonly List<int> _workingIndices = new();
        private string _statFilter = string.Empty;
        private bool _modifiedOnly;
        private int _sortColumn = -1;
        private bool _sortDescending;

        private bool _hasRowSelection;
        private StatHandle _selectedHandle;

        private sealed class Row
        {
            public VisualElement root;
            public Label[] cells;
            public StatHandle handle;
        }

        public StatDebuggerView()
        {
            AddToClassList(UssClassName);

            Add(BuildToolbar(out _storeField, out _info));

            var warning = _warning = new Label();
            warning.AddToClassList(WarningUssClassName);
            warning.style.display = DisplayStyle.None;
            Add(warning);

            var empty = _empty = new Label(
                "No store is registered.\n\n"
                + "A StatStore is native memory owned by your code — nothing can discover it on its "
                + "own. Wrap it in StatStoreDebug<> and hand that to StatDebugRegistry.Register, "
                + "then unregister before you dispose the store.");

            empty.AddToClassList(EmptyUssClassName);
            Add(empty);

            var body = _body = new VisualElement();
            body.AddToClassList(BodyUssClassName);
            Add(body);

            body.Add(BuildOwners(out _ownerList));

            // Resizable so the graph can take the whole window when that is what you are reading.
            var detail = new TwoPaneSplitView(0, 220f, TwoPaneSplitViewOrientation.Vertical);
            detail.AddToClassList(DetailUssClassName);
            body.Add(detail);

            detail.Add(BuildStatsSection(out _statRows, out _statsScroll, out _statsSummary));
            detail.Add(BuildGraphSection(out _graph));

            // Table and graph show the same owner from two angles — picking one in either place
            // should point at the same stat in the other.
            _graph.NodeSelected += OnGraphNodeSelected;
        }

        // ---- structure -------------------------------------------------------------------------

        /// <remarks>
        /// Choices carry the store name only. The first draft appended the owner count, so every
        /// spawn rewrote <c>choices</c> and the selected entry stopped matching — the dropdown
        /// looked like it kept losing its selection.
        /// </remarks>
        public void SetStores(List<string> names, int selectedIndex)
        {
            _storeField.choices = new List<string>(names);
            _storeField.SetValueWithoutNotify(
                selectedIndex >= 0 && selectedIndex < names.Count ? names[selectedIndex] : string.Empty);

            var hasStore = names.Count > 0;
            _empty.style.display = hasStore ? DisplayStyle.None : DisplayStyle.Flex;
            _body.style.display = hasStore ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetOwners(List<string> labels, int selectedIndex)
        {
            _ownerLabels.Clear();
            _ownerLabels.AddRange(labels);
            _ownerSelectedOriginal = selectedIndex;

            RebuildOwnerDisplay();
        }

        /// <summary>
        /// Grows or shrinks the row pool. Cheap when the count is unchanged, which it usually is.
        /// </summary>
        public void SetStatCount(int count)
        {
            while (_rows.Count < count)
            {
                var row = BuildRow(false, _rows.Count);
                _rows.Add(row);
                _statRows.Add(row.root);
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].root.style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetTopology(IReadOnlyList<StatDebugInfo> stats, IReadOnlyList<StatObserverEdge> edges)
        {
            _graph.SetTopology(stats, edges);

            _warning.text = _graph.HasCycle
                ? "A cycle is present in the observer graph. TryAddStatModifier should have made "
                    + "that impossible — this is a bug in the loop check, not a drawing artefact."
                : string.Empty;

            _warning.style.display = _graph.HasCycle ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ---- values ----------------------------------------------------------------------------

        public void SetInfo(string text) => _info.text = text;

        public void SetValues(IReadOnlyList<StatDebugInfo> stats)
        {
            _lastStats.Clear();
            _lastStats.AddRange(stats);

            RenderStats();

            _graph.SetValues(stats);
        }

        /// <summary>
        /// Re-derives which rows are visible, in what order, from the last snapshot handed to
        /// <see cref="SetValues"/> plus the current search text / modified-only / sort state. Safe
        /// to call on every poll — it only ever writes into the existing row pool.
        /// </summary>
        private void RenderStats()
        {
            var source = _lastStats;
            _workingIndices.Clear();

            var modifiedCount = 0;

            for (var i = 0; i < source.Count; i++)
            {
                var stat = source[i];

                if (stat.IsModified)
                {
                    modifiedCount++;
                }

                if (_modifiedOnly && stat.IsModified == false)
                {
                    continue;
                }

                if (MatchesFilter(stat, _statFilter))
                {
                    _workingIndices.Add(i);
                }
            }

            if (_sortColumn >= 0)
            {
                var column = _sortColumn;
                var descending = _sortDescending;
                _workingIndices.Sort((a, b) => CompareStats(column, descending, source[a], source[b]));
            }

            _statsSummary.text = source.Count == 0
                ? "No stats."
                : _workingIndices.Count == source.Count
                    ? $"{source.Count} stat(s) · {modifiedCount} modified"
                    : $"{_workingIndices.Count} / {source.Count} shown · {modifiedCount} modified";

            for (var slot = 0; slot < _rows.Count; slot++)
            {
                var row = _rows[slot];

                if (slot >= _workingIndices.Count)
                {
                    row.root.style.display = DisplayStyle.None;
                    continue;
                }

                WriteRow(row, source[_workingIndices[slot]]);
                row.root.style.display = DisplayStyle.Flex;
            }

            RefreshRowSelectionHighlight();
        }

        private static bool MatchesFilter(in StatDebugInfo stat, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            if (stat.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (stat.handle.index.value.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (Format(stat.baseValue).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (Format(stat.currentValue).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (stat.userData.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        /// <remarks>
        /// Base/current have no generic numeric form (a stat can be a float, an int, a bool4, ...),
        /// so those two columns sort by their formatted text instead of a true numeric compare.
        /// Approximate, but still groups equal values and is a lot better than no sort at all.
        /// </remarks>
        private static int CompareStats(int column, bool descending, in StatDebugInfo a, in StatDebugInfo b)
        {
            var cmp = column switch {
                0 => a.handle.index.value.CompareTo(b.handle.index.value),
                1 => string.CompareOrdinal(Format(a.baseValue), Format(b.baseValue)),
                2 => string.CompareOrdinal(Format(a.currentValue), Format(b.currentValue)),
                3 => a.modifierCount.CompareTo(b.modifierCount),
                4 => a.observerCount.CompareTo(b.observerCount),
                5 => a.userData.CompareTo(b.userData),
                6 => (a.produceChangeEvents ? 1 : 0).CompareTo(b.produceChangeEvents ? 1 : 0),
                _ => 0,
            };

            return descending ? -cmp : cmp;
        }

        private static void WriteRow(Row row, in StatDebugInfo stat)
        {
            var cells = row.cells;

            var baseText = Format(stat.baseValue);
            var currentText = Format(stat.currentValue);

            // stat.name is only populated when whoever registered the store passed a nameLookup
            // to StatStoreDebug<> — falls back to the raw index otherwise.
            cells[0].text = string.IsNullOrEmpty(stat.name) ? $"#{stat.handle.index}" : stat.name;
            cells[0].tooltip = $"#{stat.handle.index}";
            cells[1].text = baseText;
            cells[2].text = currentText;
            cells[3].text = stat.modifierCount.ToString();
            cells[4].text = stat.observerCount.ToString();
            cells[5].text = stat.userData.ToString();
            cells[6].text = stat.produceChangeEvents ? "on" : "off";

            // Vector types print wide enough to be clipped; the tooltip keeps them readable.
            cells[1].tooltip = baseText;
            cells[2].tooltip = currentText;
            cells[5].tooltip = $"{stat.userData}  (0x{stat.userData:X8})";

            row.handle = stat.handle;
            row.root.EnableInClassList(RowModifiedUssClassName, stat.IsModified);
        }

        private void OnHeaderClicked(int column)
        {
            if (_sortColumn == column)
            {
                _sortDescending = _sortDescending == false;
            }
            else
            {
                _sortColumn = column;
                _sortDescending = false;
            }

            UpdateSortIndicators();
            RenderStats();
        }

        /// <remarks>
        /// Only the active column gets an arrow appended — appending a hint glyph to every column
        /// (even a single narrow one, e.g. "mods ⇅") was enough extra width to push "mods"/"obs"/
        /// "userData"/"events" past their fixed column width and ellipsize on a normal-sized window.
        /// The cursor + hover background + tooltip already say "sortable" without spending width.
        /// </remarks>
        private void UpdateSortIndicators()
        {
            for (var i = 0; i < _headerCells.Length; i++)
            {
                var (label, _) = s_columns[i];
                var active = i == _sortColumn;

                _headerCells[i].text = active ? $"{label} {(_sortDescending ? "▼" : "▲")}" : label;
                _headerCells[i].EnableInClassList(CellSortedUssClassName, active);
            }
        }

        // ---- selection sync (table <-> graph) ---------------------------------------------------

        private void SelectRow(Row row)
        {
            _hasRowSelection = true;
            _selectedHandle = row.handle;

            RefreshRowSelectionHighlight();
            _graph.SelectNode(row.handle);
        }

        private void OnGraphNodeSelected(StatHandle handle)
        {
            _hasRowSelection = true;
            _selectedHandle = handle;

            RefreshRowSelectionHighlight();
            ScrollToRow(handle);
        }

        private void RefreshRowSelectionHighlight()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.root.EnableInClassList(
                      RowSelectedUssClassName
                    , _hasRowSelection && row.handle.Equals(_selectedHandle)
                );
            }
        }

        private void ScrollToRow(StatHandle handle)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].handle.Equals(handle) == false)
                {
                    continue;
                }

                _statsScroll.ScrollTo(_rows[i].root);
                return;
            }
        }

        // ---- construction ----------------------------------------------------------------------

        private VisualElement BuildToolbar(out DropdownField storeField, out Label info)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList(ToolbarUssClassName);

            var field = storeField = new DropdownField { choices = new List<string>() };
            field.AddToClassList(ToolbarFieldUssClassName);
            field.RegisterValueChangedCallback(evt => StoreSelected?.Invoke(field.choices.IndexOf(evt.newValue)));
            toolbar.Add(field);

            info = new Label();
            info.AddToClassList(ToolbarInfoUssClassName);
            toolbar.Add(info);

            var spacer = new VisualElement();
            spacer.AddToClassList(ToolbarSpacerUssClassName);
            toolbar.Add(spacer);

            // Toggle's own label stretches to fill the row, which pushed "Auto" miles away from its
            // checkbox. Label-less toggle plus a plain Label keeps the two together.
            var auto = new Toggle { value = true, tooltip = "Keep polling the store while this is on." };
            auto.AddToClassList(ToolbarToggleUssClassName);
            auto.RegisterValueChangedCallback(evt => AutoRefreshToggled?.Invoke(evt.newValue));

            var autoLabel = new Label("Auto");
            autoLabel.AddToClassList(ToolbarToggleLabelUssClassName);

            toolbar.Add(auto);
            toolbar.Add(autoLabel);

            var refresh = new Button(() => RefreshRequested?.Invoke()) { text = "Refresh" };
            refresh.AddToClassList(ToolbarButtonUssClassName);
            toolbar.Add(refresh);

            return toolbar;
        }

        private VisualElement BuildOwners(out ListView list)
        {
            var panel = new VisualElement();
            panel.AddToClassList(OwnersUssClassName);

            var header = new VisualElement();
            header.AddToClassList(OwnersHeaderUssClassName);
            panel.Add(header);

            var title = new Label("Owners");
            title.AddToClassList(OwnersTitleUssClassName);
            header.Add(title);

            var search = new TextField { tooltip = "Filter owners by index or version." };
            search.AddToClassList(SectionSearchUssClassName);
            search.RegisterValueChangedCallback(evt => {
                _ownerFilter = evt.newValue ?? string.Empty;
                RebuildOwnerDisplay();
            });
            header.Add(search);

            list = new ListView {
                fixedItemHeight = 20f,
                selectionType = SelectionType.Single,
                itemsSource = _ownerDisplay,
                makeItem = static () => new Label(),
            };

            var displayLabels = _ownerDisplay;
            list.bindItem = (element, index) => ((Label)element).text = displayLabels[index];
            list.AddToClassList(OwnersListUssClassName);

            var captured = list;
            var mapping = _ownerDisplayToOriginal;
            list.selectionChanged += _ => {
                var displayIndex = captured.selectedIndex;

                if (displayIndex < 0 || displayIndex >= mapping.Count)
                {
                    return;
                }

                OwnerSelected?.Invoke(mapping[displayIndex]);
            };
            panel.Add(list);

            return panel;
        }

        /// <remarks>
        /// The controller hands owner selection to us as an index into the <b>unfiltered</b> owner
        /// list — that contract does not change just because the user typed into the search box, so
        /// filtering keeps its own index translation here instead of leaking into the controller.
        /// </remarks>
        private void RebuildOwnerDisplay()
        {
            _ownerDisplay.Clear();
            _ownerDisplayToOriginal.Clear();

            for (var i = 0; i < _ownerLabels.Count; i++)
            {
                if (_ownerFilter.Length > 0
                    && _ownerLabels[i].IndexOf(_ownerFilter, StringComparison.OrdinalIgnoreCase) < 0
                )
                {
                    continue;
                }

                _ownerDisplayToOriginal.Add(i);
                _ownerDisplay.Add(_ownerLabels[i]);
            }

            _ownerList.RefreshItems();

            var displayIndex = _ownerDisplayToOriginal.IndexOf(_ownerSelectedOriginal);

            if (displayIndex >= 0)
            {
                _ownerList.SetSelectionWithoutNotify(new[] { displayIndex });
            }
            else
            {
                _ownerList.ClearSelection();
            }
        }

        private VisualElement BuildStatsSection(
              out VisualElement rows
            , out ScrollView scrollView
            , out Label summary
        )
        {
            var section = new VisualElement();
            section.AddToClassList(SectionUssClassName);

            var header = new VisualElement();
            header.AddToClassList(SectionHeaderUssClassName);
            section.Add(header);

            var title = new Label("Stats");
            title.AddToClassList(SectionTitleUssClassName);
            header.Add(title);

            var search = new TextField {
                tooltip = "Filter by stat index, or by base/current/userData value (case-insensitive).",
            };
            search.AddToClassList(SectionSearchUssClassName);
            search.RegisterValueChangedCallback(evt => {
                _statFilter = evt.newValue ?? string.Empty;
                RenderStats();
            });
            header.Add(search);

            var modifiedOnly = new Toggle {
                tooltip = "Show only stats a modifier has moved away from their base value.",
            };
            modifiedOnly.AddToClassList(SectionToggleUssClassName);
            modifiedOnly.RegisterValueChangedCallback(evt => {
                _modifiedOnly = evt.newValue;
                RenderStats();
            });
            header.Add(modifiedOnly);

            var modifiedOnlyLabel = new Label("Modified only");
            modifiedOnlyLabel.AddToClassList(SectionToggleLabelUssClassName);
            header.Add(modifiedOnlyLabel);

            var spacer = new VisualElement();
            spacer.AddToClassList(SectionSpacerUssClassName);
            header.Add(spacer);

            summary = new Label();
            summary.AddToClassList(SectionSummaryUssClassName);
            header.Add(summary);

            var headerRow = BuildRow(true);
            _headerCells = headerRow.cells;

            for (var i = 0; i < _headerCells.Length; i++)
            {
                var column = i;
                var cell = _headerCells[i];
                cell.AddToClassList(CellSortableUssClassName);
                cell.tooltip = s_columnHints[i];

                // Label defaults to PickingMode.Ignore (text shouldn't normally intercept clicks) —
                // these are the one kind of Label in this view that must.
                cell.pickingMode = PickingMode.Position;
                cell.RegisterCallback<PointerDownEvent>(_ => OnHeaderClicked(column));
            }

            section.Add(headerRow.root);
            UpdateSortIndicators();

            var scroll = new ScrollView();
            scroll.AddToClassList(StatsUssClassName);
            section.Add(scroll);

            rows = scroll.Q("unity-content-container");
            scrollView = scroll;
            return section;
        }

        private VisualElement BuildGraphSection(out StatGraphView graph)
        {
            var section = new VisualElement();
            section.AddToClassList(SectionUssClassName);

            var header = new VisualElement();
            header.AddToClassList(SectionHeaderUssClassName);
            section.Add(header);

            var title = new Label("Observer graph");
            title.AddToClassList(SectionTitleUssClassName);
            header.Add(title);

            graph = new StatGraphView();
            section.Add(graph);

            return section;
        }

        private Row BuildRow(bool header, int index = 0)
        {
            var root = new VisualElement();
            root.AddToClassList(RowUssClassName);

            if (header)
            {
                root.AddToClassList(RowHeaderUssClassName);
            }
            else if ((index & 1) == 1)
            {
                root.AddToClassList(RowEvenUssClassName);
            }

            var cells = new Label[s_columns.Length];

            for (var i = 0; i < s_columns.Length; i++)
            {
                var (label, modifier) = s_columns[i];

                var cell = new Label(header ? label : string.Empty);
                cell.AddToClassList(CellUssClassName);
                cell.AddToClassList($"{CellUssClassName}--{modifier}");

                root.Add(cell);
                cells[i] = cell;
            }

            var row = new Row { root = root, cells = cells };

            // Clicking a stat jumps the graph to the matching node — the header row has no handle
            // to jump to, so it stays inert.
            if (header == false)
            {
                root.RegisterCallback<PointerDownEvent>(_ => SelectRow(row));
            }

            return row;
        }

        private static string Format(in StatVariant value)
            => value.Type == StatVariantType.None ? "—" : value.ToString();
    }
}
