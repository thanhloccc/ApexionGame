using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Editor.Views
{
    /// <summary>
    /// HFSM - Debugging.md §3 — the 32-entry ring buffer as rows, newest first.
    /// </summary>
    public sealed class TransitionLogView : VisualElement
    {
        public static readonly string UssClassName = "hfsm-transition-log";
        public static readonly string HeaderUssClassName = $"{UssClassName}__header";
        public static readonly string TitleUssClassName = $"{HeaderUssClassName}-title";
        public static readonly string ScrollUssClassName = $"{UssClassName}__scroll";
        public static readonly string RowUssClassName = $"{UssClassName}__row";
        public static readonly string RowHeaderUssClassName = $"{RowUssClassName}--header";
        public static readonly string RowUnmatchedUssClassName = $"{RowUssClassName}--unmatched";
        public static readonly string CellUssClassName = $"{UssClassName}__cell";
        public static readonly string EmptyUssClassName = $"{UssClassName}__empty";

        private static readonly string[] s_columnLabels = { "route", "frame", "Δt", "cause" };

        private readonly ScrollView _scroll;
        private readonly Label _empty;

        private readonly List<VisualElement> _rowRoots = new();
        private readonly List<Label[]> _rowCells = new();

        public TransitionLogView()
        {
            AddToClassList(UssClassName);

            var header = new VisualElement();
            header.AddToClassList(HeaderUssClassName);
            Add(header);

            var title = new Label("Transition log");
            title.AddToClassList(TitleUssClassName);
            header.Add(title);

            var (headerRoot, headerCells) = BuildRow(true);

            for (var i = 0; i < headerCells.Length; i++)
            {
                headerCells[i].text = s_columnLabels[i];
            }

            Add(headerRoot);

            _scroll = new ScrollView();
            _scroll.AddToClassList(ScrollUssClassName);
            Add(_scroll);

            _empty = new Label("No transitions recorded yet.");
            _empty.AddToClassList(EmptyUssClassName);
            Add(_empty);
        }

        /// <summary>One already-formatted row, newest first.</summary>
        public readonly struct Row
        {
            public readonly string Route;
            public readonly int Frame;
            public readonly string TimeInSource;
            public readonly string Cause;
            public readonly bool IsUnmatched;

            public Row(string route, int frame, string timeInSource, string cause, bool isUnmatched)
            {
                Route = route;
                Frame = frame;
                TimeInSource = timeInSource;
                Cause = cause;
                IsUnmatched = isUnmatched;
            }
        }

        public void SetRows(IReadOnlyList<Row> rows)
        {
            var hasRows = rows.Count > 0;
            _scroll.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            _empty.style.display = hasRows ? DisplayStyle.None : DisplayStyle.Flex;

            while (_rowRoots.Count < rows.Count)
            {
                var (root, cells) = BuildRow(false);
                _scroll.Add(root);
                _rowRoots.Add(root);
                _rowCells.Add(cells);
            }

            for (var i = 0; i < _rowRoots.Count; i++)
            {
                if (i >= rows.Count)
                {
                    _rowRoots[i].style.display = DisplayStyle.None;
                    continue;
                }

                var row = rows[i];
                var cells = _rowCells[i];

                _rowRoots[i].style.display = DisplayStyle.Flex;
                cells[0].text = row.Route;
                cells[1].text = row.Frame.ToString();
                cells[2].text = row.TimeInSource;
                cells[3].text = row.Cause;

                _rowRoots[i].EnableInClassList($"{RowUssClassName}--even", (i & 1) == 1);
                _rowRoots[i].EnableInClassList(RowUnmatchedUssClassName, row.IsUnmatched);
            }
        }

        private static (VisualElement root, Label[] cells) BuildRow(bool header)
        {
            var root = new VisualElement();
            root.AddToClassList(RowUssClassName);

            if (header)
            {
                root.AddToClassList(RowHeaderUssClassName);
            }

            var cells = new Label[s_columnLabels.Length];

            for (var i = 0; i < cells.Length; i++)
            {
                var cell = new Label();
                cell.AddToClassList(CellUssClassName);
                cell.AddToClassList($"{CellUssClassName}--{i}");
                root.Add(cell);
                cells[i] = cell;
            }

            return (root, cells);
        }
    }
}
