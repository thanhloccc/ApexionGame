using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Editor.Views
{
    /// <summary>
    /// HFSM - Debugging.md §4 — the live configuration, outermost first, with time-in-state and the
    /// remembered child of every active composite that has history.
    /// </summary>
    public sealed class ActivePathView : VisualElement
    {
        public static readonly string UssClassName = "hfsm-active-path";
        public static readonly string ScrollUssClassName = $"{UssClassName}__scroll";
        public static readonly string RowsUssClassName = $"{UssClassName}__rows";
        public static readonly string RowUssClassName = $"{UssClassName}__row";
        public static readonly string RowNameUssClassName = $"{RowUssClassName}-name";
        public static readonly string RowTimeUssClassName = $"{RowUssClassName}-time";
        public static readonly string HistoryUssClassName = $"{UssClassName}__history";
        public static readonly string EmptyUssClassName = $"{UssClassName}__empty";

        private readonly ScrollView _scroll;
        private readonly VisualElement _rows;
        private readonly VisualElement _historyRows;
        private readonly Label _empty;

        private readonly List<VisualElement> _rowRoots = new();
        private readonly List<Label> _rowNames = new();
        private readonly List<Label> _rowTimes = new();
        private readonly List<Label> _historyLabels = new();

        public ActivePathView()
        {
            AddToClassList(UssClassName);

            _scroll = new ScrollView();
            _scroll.AddToClassList(ScrollUssClassName);
            Add(_scroll);

            _rows = new VisualElement();
            _rows.AddToClassList(RowsUssClassName);
            _scroll.Add(_rows);

            _historyRows = new VisualElement();
            _historyRows.AddToClassList(HistoryUssClassName);
            Add(_historyRows);

            _empty = new Label("No active machine.");
            _empty.AddToClassList(EmptyUssClassName);
            Add(_empty);
        }

        /// <summary>
        /// One row of the active path: a name already resolved by the controller, its depth (for
        /// indentation) and its time-in-node.
        /// </summary>
        public readonly struct Row
        {
            public readonly string Name;
            public readonly int Depth;
            public readonly float TimeInNode;
            public readonly bool IsLeaf;

            public Row(string name, int depth, float timeInNode, bool isLeaf)
            {
                Name = name;
                Depth = depth;
                TimeInNode = timeInNode;
                IsLeaf = isLeaf;
            }
        }

        /// <summary>
        /// Rewrites every row from scratch. The path is at most a few dozen entries even for a deep
        /// parallel machine, so re-rendering it on every poll (rather than diffing) is cheap and far
        /// simpler than tracking which row changed.
        /// </summary>
        public void SetRows(IReadOnlyList<Row> rows, IReadOnlyList<(string container, string child)> history)
        {
            var hasRows = rows.Count > 0;
            _scroll.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            _historyRows.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            _empty.style.display = hasRows ? DisplayStyle.None : DisplayStyle.Flex;

            while (_rowRoots.Count < rows.Count)
            {
                BuildRow();
            }

            for (var i = 0; i < _rowRoots.Count; i++)
            {
                if (i >= rows.Count)
                {
                    _rowRoots[i].style.display = DisplayStyle.None;
                    continue;
                }

                var row = rows[i];
                _rowRoots[i].style.display = DisplayStyle.Flex;
                _rowRoots[i].style.paddingLeft = 10f + (row.Depth * 16f);
                _rowRoots[i].EnableInClassList($"{RowUssClassName}--leaf", row.IsLeaf);
                _rowNames[i].text = row.Name;
                _rowNames[i].EnableInClassList($"{RowNameUssClassName}--leaf", row.IsLeaf);
                _rowTimes[i].text = $"{row.TimeInNode:0.00}s";
            }

            while (_historyLabels.Count < history.Count)
            {
                var label = new Label();
                label.AddToClassList(HistoryUssClassName + "-line");
                _historyRows.Add(label);
                _historyLabels.Add(label);
            }

            for (var i = 0; i < _historyLabels.Count; i++)
            {
                if (i >= history.Count)
                {
                    _historyLabels[i].style.display = DisplayStyle.None;
                    continue;
                }

                var (container, child) = history[i];
                _historyLabels[i].style.display = DisplayStyle.Flex;
                _historyLabels[i].text = $"history({container}) = {child}";
            }
        }

        private void BuildRow()
        {
            var root = new VisualElement();
            root.AddToClassList(RowUssClassName);

            var name = new Label();
            name.AddToClassList(RowNameUssClassName);
            root.Add(name);

            var time = new Label();
            time.AddToClassList(RowTimeUssClassName);
            root.Add(time);

            _rows.Add(root);
            _rowRoots.Add(root);
            _rowNames.Add(name);
            _rowTimes.Add(time);
        }
    }
}
