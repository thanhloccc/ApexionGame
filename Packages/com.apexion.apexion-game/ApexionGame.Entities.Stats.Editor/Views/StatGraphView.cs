using System;
using System.Collections.Generic;
using ApexionGame.Entities.Stats.Debugging;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Editor.Views
{
    /// <summary>
    /// Task 5.2 — the observer graph of one owner, drawn.
    /// </summary>
    /// <remarks>
    /// Nodes sit in columns by <b>longest-path depth</b>, not shortest. That is deliberate: the
    /// column gap between two branches arriving at the same node is exactly the situation DEC-005
    /// is about, and shortest-path layout would hide it.
    /// <para>
    /// Topology and values are refreshed separately. Rebuilding nodes on every poll made the graph
    /// flicker and threw away scroll position four times a second; now
    /// <see cref="SetTopology"/> only runs when the edge set actually changes, and
    /// <see cref="SetValues"/> writes into the labels that are already there.
    /// </para>
    /// <para>
    /// Zoom does not use a render transform: a <c>Painter2D</c> curve reads node positions from
    /// layout (<c>VisualElement.layout</c>), and a transform-scaled canvas keeps reporting its
    /// unscaled layout size, so the <see cref="ScrollView"/> and the edges would disagree with what
    /// is drawn on screen. Instead zoom scales the same column/row/margin constants
    /// <see cref="BuildNodes"/> already used, and <see cref="Relayout"/> re-applies them to the
    /// nodes that exist — cheap, because it never touches labels' text or the DOM.
    /// </para>
    /// </remarks>
    public class StatGraphView : VisualElement
    {
        public static readonly string UssClassName = "stat-graph";
        public static readonly string ToolbarUssClassName = $"{UssClassName}__toolbar";
        public static readonly string ZoomGroupUssClassName = $"{ToolbarUssClassName}-zoom";
        public static readonly string ZoomButtonUssClassName = $"{ZoomGroupUssClassName}-button";
        public static readonly string ZoomLabelUssClassName = $"{ZoomGroupUssClassName}-label";
        public static readonly string LegendUssClassName = $"{ToolbarUssClassName}-legend";
        public static readonly string LegendItemUssClassName = $"{LegendUssClassName}-item";
        public static readonly string LegendSwatchUssClassName = $"{LegendItemUssClassName}-swatch";
        public static readonly string LegendSwatchLineUssClassName = $"{LegendSwatchUssClassName}--line";
        public static readonly string LegendSwatchModifiedUssClassName = $"{LegendSwatchUssClassName}--modified";
        public static readonly string LegendSwatchForeignUssClassName = $"{LegendSwatchUssClassName}--foreign";
        public static readonly string LegendSwatchCycleUssClassName = $"{LegendSwatchUssClassName}--cycle";
        public static readonly string LegendSwatchEdgeUssClassName = $"{LegendSwatchUssClassName}--edge";
        public static readonly string LegendSwatchEdgeCycleUssClassName = $"{LegendSwatchUssClassName}--edge-cycle";
        public static readonly string LegendLabelUssClassName = $"{LegendItemUssClassName}-label";
        public static readonly string ScrollUssClassName = $"{UssClassName}__scroll";
        public static readonly string CanvasUssClassName = $"{UssClassName}__canvas";
        public static readonly string NodeUssClassName = $"{UssClassName}__node";
        public static readonly string NodeModifiedUssClassName = $"{NodeUssClassName}--modified";
        public static readonly string NodeInCycleUssClassName = $"{NodeUssClassName}--in-cycle";
        public static readonly string NodeForeignUssClassName = $"{NodeUssClassName}--foreign";
        public static readonly string NodeSelectedUssClassName = $"{NodeUssClassName}--selected";
        public static readonly string NodeNameUssClassName = $"{NodeUssClassName}-name";
        public static readonly string NodeValueUssClassName = $"{NodeUssClassName}-value";
        public static readonly string EmptyUssClassName = $"{UssClassName}__empty";

        private const float COLUMN_WIDTH = 190f;
        private const float ROW_HEIGHT = 62f;
        private const float NODE_WIDTH = 154f;
        private const float MARGIN = 16f;
        private const float NODE_FONT_SIZE = 11f;

        private const float MIN_ZOOM = 0.5f;
        private const float MAX_ZOOM = 1.75f;
        private const float ZOOM_STEP = 0.15f;

        private readonly ScrollView _scroll;
        private readonly VisualElement _canvas;
        private readonly Label _empty;
        private readonly Label _zoomLabel;

        private readonly List<Node> _nodes = new();
        private readonly Dictionary<StatHandle, int> _indexByHandle = new();
        private readonly List<StatObserverEdge> _edges = new();
        private readonly HashSet<StatHandle> _cycleMembers = new();

        /// <remarks>
        /// Only ever has entries for stats owned by whichever owner is on screen — an edge can
        /// point at a stat on another owner, and there is no name available for that one here.
        /// </remarks>
        private readonly Dictionary<StatHandle, string> _names = new();

        private float _zoom = 1f;
        private int _maxColumn;
        private int _maxRow;

        private bool _hasSelection;
        private StatHandle _selectedHandle;

        private sealed class Node
        {
            public VisualElement root;
            public Label name;
            public Label value;
            public StatHandle handle;
            public int depth;
            public int row;
        }

        /// <summary>
        /// Raised when the user clicks a node, so the stats table can highlight the matching row.
        /// </summary>
        public event Action<StatHandle> NodeSelected;

        public StatGraphView()
        {
            AddToClassList(UssClassName);

            Add(BuildToolbar(out _zoomLabel));

            var scroll = _scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.AddToClassList(ScrollUssClassName);
            Add(scroll);

            var canvas = _canvas = new VisualElement();
            canvas.AddToClassList(CanvasUssClassName);
            canvas.generateVisualContent += OnGenerateVisualContent;
            scroll.Add(canvas);

            var empty = _empty = new Label();
            empty.AddToClassList(EmptyUssClassName);
            Add(empty);

            canvas.RegisterCallback<GeometryChangedEvent>(_ => canvas.MarkDirtyRepaint());

            UpdateZoomLabel();
        }

        /// <summary>
        /// True when the drawn graph contains a cycle — which should be impossible, because
        /// <c>TryAddStatModifier</c> refuses the modifier that would close one. If this lights up,
        /// the loop check has a hole.
        /// </summary>
        public bool HasCycle => _cycleMembers.Count > 0;

        /// <summary>
        /// Rebuilds nodes and layout. Only call when the edge set or the stat set changed.
        /// </summary>
        public void SetTopology(IReadOnlyList<StatDebugInfo> stats, IReadOnlyList<StatObserverEdge> edges)
        {
            _canvas.Clear();
            _nodes.Clear();
            _indexByHandle.Clear();
            _cycleMembers.Clear();
            _names.Clear();

            _edges.Clear();
            _edges.AddRange(edges);

            var owned = new HashSet<StatHandle>();

            for (var i = 0; i < stats.Count; i++)
            {
                owned.Add(stats[i].handle);

                if (string.IsNullOrEmpty(stats[i].name) == false)
                {
                    _names[stats[i].handle] = stats[i].name;
                }
            }

            var handles = CollectHandles(stats, _edges);
            var depths = ComputeDepths(handles, _edges);

            DetectCycles(handles, _edges, _cycleMembers);
            BuildNodes(handles, owned, depths);
            Relayout();

            var hasNodes = _nodes.Count > 0;
            _scroll.style.display = hasNodes ? DisplayStyle.Flex : DisplayStyle.None;
            _empty.style.display = hasNodes ? DisplayStyle.None : DisplayStyle.Flex;

            _empty.text = stats.Count == 0
                ? "Select an owner."
                : "This owner has no observer edges yet.\n"
                    + "Add a modifier that reads another stat and the graph appears.";

            // The node the user had selected may not exist after a topology rebuild (its owner's
            // edges changed) — keep the selection only if it is still on screen.
            if (_hasSelection && _indexByHandle.ContainsKey(_selectedHandle))
            {
                ApplySelection(_selectedHandle);
            }
            else
            {
                _hasSelection = false;
            }

            _canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Writes fresh numbers into nodes that already exist. Safe to call on every poll.
        /// </summary>
        public void SetValues(IReadOnlyList<StatDebugInfo> stats)
        {
            for (var i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];

                if (_indexByHandle.TryGetValue(stat.handle, out var index) == false)
                {
                    continue;
                }

                var node = _nodes[index];
                node.value.text = $"{Format(stat.baseValue)}  →  {Format(stat.currentValue)}";
                node.root.EnableInClassList(NodeModifiedUssClassName, stat.IsModified);
            }
        }

        /// <summary>
        /// Selects a node by handle and scrolls it into view. No-op if the handle is not on screen.
        /// </summary>
        public void SelectNode(StatHandle handle)
        {
            if (_indexByHandle.TryGetValue(handle, out var index) == false)
            {
                return;
            }

            ApplySelection(handle);
            _scroll.ScrollTo(_nodes[index].root);
        }

        private void ApplySelection(StatHandle handle)
        {
            if (_hasSelection && _indexByHandle.TryGetValue(_selectedHandle, out var previous))
            {
                _nodes[previous].root.EnableInClassList(NodeSelectedUssClassName, false);
            }

            _selectedHandle = handle;
            _hasSelection = true;

            if (_indexByHandle.TryGetValue(handle, out var index))
            {
                _nodes[index].root.EnableInClassList(NodeSelectedUssClassName, true);
            }
        }

        // ---- zoom --------------------------------------------------------------------------------

        private void SetZoom(float value)
        {
            var clamped = Mathf.Clamp(value, MIN_ZOOM, MAX_ZOOM);
            _zoom = clamped;
            Relayout();
            UpdateZoomLabel();
        }

        private void UpdateZoomLabel() => _zoomLabel.text = $"{Mathf.RoundToInt(_zoom * 100f)}%";

        /// <summary>
        /// Picks the zoom that makes the whole graph visible without scrolling. Falls back to 100%
        /// before the view has ever been laid out (viewport size is still zero).
        /// </summary>
        private void FitToView()
        {
            if (_nodes.Count == 0)
            {
                SetZoom(1f);
                return;
            }

            var viewportWidth = _scroll.resolvedStyle.width;
            var viewportHeight = _scroll.resolvedStyle.height;

            if (float.IsNaN(viewportWidth) || viewportWidth <= 0f
                || float.IsNaN(viewportHeight) || viewportHeight <= 0f
            )
            {
                SetZoom(1f);
                return;
            }

            var unscaledWidth = MARGIN + ((_maxColumn + 1) * COLUMN_WIDTH);
            var unscaledHeight = MARGIN + ((_maxRow + 1) * ROW_HEIGHT);

            SetZoom(Mathf.Min(viewportWidth / unscaledWidth, viewportHeight / unscaledHeight));
        }

        /// <summary>
        /// Re-applies zoom-scaled positions/sizes to nodes that already exist. Called after
        /// <see cref="BuildNodes"/> and on every zoom change — never rebuilds the DOM.
        /// </summary>
        private void Relayout()
        {
            var margin = MARGIN * _zoom;
            var columnWidth = COLUMN_WIDTH * _zoom;
            var rowHeight = ROW_HEIGHT * _zoom;
            var nodeWidth = NODE_WIDTH * _zoom;
            var fontSize = NODE_FONT_SIZE * Mathf.Clamp(_zoom, 0.8f, 1.35f);

            for (var i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                node.root.style.left = margin + (node.depth * columnWidth);
                node.root.style.top = margin + (node.row * rowHeight);
                node.root.style.width = nodeWidth;
                node.name.style.fontSize = fontSize;
                node.value.style.fontSize = fontSize;
            }

            _canvas.style.width = margin + ((_maxColumn + 1) * columnWidth);
            _canvas.style.height = margin + ((_maxRow + 1) * rowHeight);
            _canvas.MarkDirtyRepaint();
        }

        // ---- build ---------------------------------------------------------------------------

        /// <remarks>
        /// An edge can point at a stat on another owner. Those endpoints have no
        /// <see cref="StatDebugInfo"/> here, but leaving them out would draw a graph that lies about
        /// what feeds what — so they get a node too, marked foreign.
        /// </remarks>
        private static List<StatHandle> CollectHandles(
              IReadOnlyList<StatDebugInfo> stats
            , List<StatObserverEdge> edges
        )
        {
            var seen = new HashSet<StatHandle>();
            var handles = new List<StatHandle>();

            for (var i = 0; i < stats.Count; i++)
            {
                if (seen.Add(stats[i].handle))
                {
                    handles.Add(stats[i].handle);
                }
            }

            for (var i = 0; i < edges.Count; i++)
            {
                if (seen.Add(edges[i].observed))
                {
                    handles.Add(edges[i].observed);
                }

                if (seen.Add(edges[i].observer))
                {
                    handles.Add(edges[i].observer);
                }
            }

            return handles;
        }

        private void BuildNodes(
              List<StatHandle> handles
            , HashSet<StatHandle> owned
            , Dictionary<StatHandle, int> depths
        )
        {
            var rowPerColumn = new Dictionary<int, int>();
            _maxColumn = 0;
            _maxRow = 0;

            for (var i = 0; i < handles.Count; i++)
            {
                var handle = handles[i];
                var depth = depths.TryGetValue(handle, out var d) ? d : 0;

                rowPerColumn.TryGetValue(depth, out var row);
                rowPerColumn[depth] = row + 1;

                _maxColumn = Mathf.Max(_maxColumn, depth);
                _maxRow = Mathf.Max(_maxRow, row);

                var isOwned = owned.Contains(handle);

                var root = new VisualElement();
                root.AddToClassList(NodeUssClassName);

                var label = isOwned
                    ? (_names.TryGetValue(handle, out var n) ? n : $"stat #{handle.index}")
                    : $"{handle.owner} #{handle.index}";

                var name = new Label(label);
                name.AddToClassList(NodeNameUssClassName);
                name.tooltip = $"#{handle.index}";
                root.Add(name);

                var value = new Label(isOwned ? "—" : "on another owner");
                value.AddToClassList(NodeValueUssClassName);
                root.Add(value);

                root.EnableInClassList(NodeForeignUssClassName, isOwned == false);
                root.EnableInClassList(NodeInCycleUssClassName, _cycleMembers.Contains(handle));

                var node = new Node { root = root, name = name, value = value, handle = handle, depth = depth, row = row };

                root.RegisterCallback<PointerDownEvent>(_ => {
                    ApplySelection(node.handle);
                    NodeSelected?.Invoke(node.handle);
                });

                _canvas.Add(root);
                _indexByHandle[handle] = _nodes.Count;
                _nodes.Add(node);
            }
        }

        private static string Format(in StatVariant value)
            => value.Type == StatVariantType.None ? "—" : value.ToString();

        // ---- toolbar (zoom + legend) -----------------------------------------------------------

        private VisualElement BuildToolbar(out Label zoomLabel)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList(ToolbarUssClassName);

            var zoomGroup = new VisualElement();
            zoomGroup.AddToClassList(ZoomGroupUssClassName);

            var zoomOut = new Button(() => SetZoom(_zoom - ZOOM_STEP)) { text = "-", tooltip = "Zoom out" };
            zoomOut.AddToClassList(ZoomButtonUssClassName);
            zoomGroup.Add(zoomOut);

            zoomLabel = new Label();
            zoomLabel.AddToClassList(ZoomLabelUssClassName);
            zoomGroup.Add(zoomLabel);

            var zoomIn = new Button(() => SetZoom(_zoom + ZOOM_STEP)) { text = "+", tooltip = "Zoom in" };
            zoomIn.AddToClassList(ZoomButtonUssClassName);
            zoomGroup.Add(zoomIn);

            var fit = new Button(FitToView) { text = "Fit", tooltip = "Fit the whole graph in view" };
            fit.AddToClassList(ZoomButtonUssClassName);
            zoomGroup.Add(fit);

            toolbar.Add(zoomGroup);

            var legend = new VisualElement();
            legend.AddToClassList(LegendUssClassName);
            legend.Add(BuildLegendItem(LegendSwatchModifiedUssClassName, false, "Modified"));
            legend.Add(BuildLegendItem(LegendSwatchForeignUssClassName, false, "On another owner"));
            legend.Add(BuildLegendItem(LegendSwatchCycleUssClassName, false, "In a cycle"));
            legend.Add(BuildLegendItem(LegendSwatchEdgeUssClassName, true, "Depends on"));
            legend.Add(BuildLegendItem(LegendSwatchEdgeCycleUssClassName, true, "Cyclic edge"));
            toolbar.Add(legend);

            return toolbar;
        }

        private static VisualElement BuildLegendItem(string swatchModifier, bool isLine, string label)
        {
            var item = new VisualElement();
            item.AddToClassList(LegendItemUssClassName);

            var swatch = new VisualElement();
            swatch.AddToClassList(LegendSwatchUssClassName);
            swatch.AddToClassList(swatchModifier);

            if (isLine)
            {
                swatch.AddToClassList(LegendSwatchLineUssClassName);
            }

            item.Add(swatch);

            var text = new Label(label);
            text.AddToClassList(LegendLabelUssClassName);
            item.Add(text);

            return item;
        }

        // ---- layout maths --------------------------------------------------------------------

        /// <summary>
        /// Longest-path depth. Nodes with no incoming edge sit at 0; every other node sits one past
        /// the deepest thing that feeds it.
        /// </summary>
        private static Dictionary<StatHandle, int> ComputeDepths(
              List<StatHandle> handles
            , List<StatObserverEdge> edges
        )
        {
            var depths = new Dictionary<StatHandle, int>();

            for (var i = 0; i < handles.Count; i++)
            {
                depths[handles[i]] = 0;
            }

            // Relax until stable. Bounded by node count so a cycle cannot spin forever.
            var passes = handles.Count + 1;

            for (var pass = 0; pass < passes; pass++)
            {
                var changed = false;

                for (var i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];

                    if (depths.TryGetValue(edge.observed, out var from) == false
                        || depths.TryGetValue(edge.observer, out var to) == false
                    )
                    {
                        continue;
                    }

                    if (to < from + 1)
                    {
                        depths[edge.observer] = from + 1;
                        changed = true;
                    }
                }

                if (changed == false)
                {
                    break;
                }
            }

            return depths;
        }

        /// <summary>
        /// Marks every node on a cycle by stripping nodes with no incoming edges. Whatever survives
        /// is either on a cycle or fed by one.
        /// </summary>
        private static void DetectCycles(
              List<StatHandle> handles
            , List<StatObserverEdge> edges
            , HashSet<StatHandle> result
        )
        {
            result.Clear();

            var remaining = new HashSet<StatHandle>(handles);
            var inDegree = new Dictionary<StatHandle, int>();

            for (var i = 0; i < handles.Count; i++)
            {
                inDegree[handles[i]] = 0;
            }

            for (var i = 0; i < edges.Count; i++)
            {
                var observer = edges[i].observer;

                if (inDegree.ContainsKey(observer))
                {
                    inDegree[observer]++;
                }
            }

            var queue = new Queue<StatHandle>();

            foreach (var pair in inDegree)
            {
                if (pair.Value == 0)
                {
                    queue.Enqueue(pair.Key);
                }
            }

            while (queue.Count > 0)
            {
                var handle = queue.Dequeue();
                remaining.Remove(handle);

                for (var i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];

                    if (edge.observed != handle || remaining.Contains(edge.observer) == false)
                    {
                        continue;
                    }

                    if (--inDegree[edge.observer] == 0)
                    {
                        queue.Enqueue(edge.observer);
                    }
                }
            }

            foreach (var handle in remaining)
            {
                result.Add(handle);
            }
        }

        // ---- painting ------------------------------------------------------------------------

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_edges.Count == 0 || _nodes.Count == 0)
            {
                return;
            }

            var painter = context.painter2D;
            painter.lineWidth = 1.6f;
            painter.lineCap = LineCap.Round;

            var normal = new Color(0.42f, 0.55f, 0.70f, 0.95f);
            var danger = new Color(0.91f, 0.36f, 0.36f);

            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];

                if (_indexByHandle.TryGetValue(edge.observed, out var fromIndex) == false
                    || _indexByHandle.TryGetValue(edge.observer, out var toIndex) == false
                )
                {
                    continue;
                }

                var a = _nodes[fromIndex].root.layout;
                var b = _nodes[toIndex].root.layout;

                if (float.IsNaN(a.x) || float.IsNaN(b.x))
                {
                    continue;
                }

                var start = new Vector2(a.xMax, a.center.y);
                var end = new Vector2(b.xMin - 7f, b.center.y);

                var inCycle = _cycleMembers.Contains(edge.observed)
                    && _cycleMembers.Contains(edge.observer);

                var color = inCycle ? danger : normal;

                // Horizontal control points keep the curve leaving and entering sideways, so the
                // direction of dependency stays readable when rows are far apart.
                var handleLength = Mathf.Max(28f, (end.x - start.x) * 0.5f);

                painter.strokeColor = color;
                painter.BeginPath();
                painter.MoveTo(start);
                painter.BezierCurveTo(
                      new Vector2(start.x + handleLength, start.y)
                    , new Vector2(end.x - handleLength, end.y)
                    , end
                );
                painter.Stroke();

                DrawArrowHead(painter, end, color);
            }
        }

        /// <remarks>
        /// Without a head the curve reads as "these two are related" instead of "this one feeds
        /// that one", and direction is the entire point of an observer graph.
        /// </remarks>
        private static void DrawArrowHead(Painter2D painter, Vector2 tip, Color color)
        {
            const float LENGTH = 8f;
            const float HALF_WIDTH = 4.5f;

            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(tip.x + LENGTH, tip.y));
            painter.LineTo(new Vector2(tip.x, tip.y - HALF_WIDTH));
            painter.LineTo(new Vector2(tip.x, tip.y + HALF_WIDTH));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
