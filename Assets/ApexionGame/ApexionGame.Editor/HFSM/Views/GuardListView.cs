using System.Collections.Generic;
using ApexionGame.HFSM.Debugging;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Editor.Views
{
    /// <summary>
    /// HFSM - Debugging.md §4.2 — every outgoing transition from one node, with a marker, its
    /// route, its condition text, and a best-effort live value. The pane that answers
    /// "why didn't it switch?".
    /// </summary>
    public sealed class GuardListView : VisualElement
    {
        public static readonly string UssClassName = "hfsm-guard-list";
        public static readonly string HeaderUssClassName = $"{UssClassName}__header";
        public static readonly string TitleUssClassName = $"{HeaderUssClassName}-title";
        public static readonly string ScrollUssClassName = $"{UssClassName}__scroll";
        public static readonly string RowsUssClassName = $"{UssClassName}__rows";
        public static readonly string RowUssClassName = $"{UssClassName}__row";
        public static readonly string MarkerUssClassName = $"{RowUssClassName}-marker";
        public static readonly string RouteUssClassName = $"{RowUssClassName}-route";
        public static readonly string ConditionUssClassName = $"{RowUssClassName}-condition";
        public static readonly string ValueUssClassName = $"{RowUssClassName}-value";
        public static readonly string EmptyUssClassName = $"{UssClassName}__empty";

        private readonly Label _title;
        private readonly ScrollView _scroll;
        private readonly VisualElement _rows;
        private readonly Label _empty;

        private readonly List<VisualElement> _rowRoots = new();
        private readonly List<VisualElement> _markers = new();
        private readonly List<Label> _routes = new();
        private readonly List<Label> _conditions = new();
        private readonly List<Label> _values = new();

        public GuardListView()
        {
            AddToClassList(UssClassName);

            var header = new VisualElement();
            header.AddToClassList(HeaderUssClassName);
            Add(header);

            _title = new Label("Guards");
            _title.AddToClassList(TitleUssClassName);
            header.Add(_title);

            _scroll = new ScrollView();
            _scroll.AddToClassList(ScrollUssClassName);
            Add(_scroll);

            _rows = new VisualElement();
            _rows.AddToClassList(RowsUssClassName);
            _scroll.Add(_rows);

            _empty = new Label("Select an active node to see its outgoing transitions.");
            _empty.AddToClassList(EmptyUssClassName);
            Add(_empty);
        }

        public void SetTitle(string fromNodeName)
            => _title.text = string.IsNullOrEmpty(fromNodeName) ? "Guards" : $"Guards (from {fromNodeName})";

        /// <param name="guards">Already resolved: <see cref="GuardDebugInfo"/> plus the two display
        /// strings the controller derived from it (route text, so the view never has to know how
        /// <c>*</c> is spelled for an <c>AnyState</c> source).</param>
        public void SetGuards(IReadOnlyList<(GuardDebugInfo info, string routeText)> guards)
        {
            var hasGuards = guards.Count > 0;
            _scroll.style.display = hasGuards ? DisplayStyle.Flex : DisplayStyle.None;
            _empty.style.display = hasGuards ? DisplayStyle.None : DisplayStyle.Flex;

            while (_rowRoots.Count < guards.Count)
            {
                BuildRow();
            }

            for (var i = 0; i < _rowRoots.Count; i++)
            {
                if (i >= guards.Count)
                {
                    _rowRoots[i].style.display = DisplayStyle.None;
                    continue;
                }

                var (info, routeText) = guards[i];
                var root = _rowRoots[i];
                root.style.display = DisplayStyle.Flex;

                _markers[i].tooltip = MarkerTooltip(info);
                _routes[i].text = routeText;
                _conditions[i].text = info.ConditionText;
                _values[i].text = info.LiveValueText ?? string.Empty;

                root.EnableInClassList($"{RowUssClassName}--even", (i & 1) == 1);
                root.EnableInClassList($"{RowUssClassName}--eligible", info.Marker == GuardMarker.Eligible);
                root.EnableInClassList($"{RowUssClassName}--blocked", info.Marker == GuardMarker.BlockedByMinDuration);

                SetMarkerClass(_markers[i], info.Marker);
            }
        }

        private static void SetMarkerClass(VisualElement marker, GuardMarker value)
        {
            marker.EnableInClassList($"{MarkerUssClassName}--eligible", value == GuardMarker.Eligible);
            marker.EnableInClassList($"{MarkerUssClassName}--false", value == GuardMarker.False);
            marker.EnableInClassList($"{MarkerUssClassName}--blocked", value == GuardMarker.BlockedByMinDuration);
            marker.EnableInClassList($"{MarkerUssClassName}--pending", value == GuardMarker.NotEvaluated);
        }

        private static string MarkerTooltip(in GuardDebugInfo info)
            => info.Marker switch {
                GuardMarker.Eligible => "Eligible -- would fire this tick.",
                GuardMarker.False => "Evaluated, false.",
                GuardMarker.BlockedByMinDuration => $"Blocked by MinDuration -- {info.RemainingMinDuration:0.00}s remaining.",
                _ => "Not evaluated this tick -- a higher-priority transition already won.",
            };

        private void BuildRow()
        {
            var root = new VisualElement();
            root.AddToClassList(RowUssClassName);

            var marker = new VisualElement();
            marker.AddToClassList(MarkerUssClassName);
            root.Add(marker);

            var route = new Label();
            route.AddToClassList(RouteUssClassName);
            root.Add(route);

            var condition = new Label();
            condition.AddToClassList(ConditionUssClassName);
            root.Add(condition);

            var value = new Label();
            value.AddToClassList(ValueUssClassName);
            root.Add(value);

            _rows.Add(root);
            _rowRoots.Add(root);
            _markers.Add(marker);
            _routes.Add(route);
            _conditions.Add(condition);
            _values.Add(value);
        }
    }
}
