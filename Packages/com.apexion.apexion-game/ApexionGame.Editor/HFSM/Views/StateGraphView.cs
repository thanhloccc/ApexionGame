using System;
using System.Collections.Generic;
using ApexionGame.HFSM.Debugging;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Editor.Views
{
    /// <summary>
    /// HFSM - Debugging.md §4.3 — the machine's shape, drawn read-only: nodes placed by tree depth
    /// (columns) and declaration order (rows), transitions as arrows.
    /// </summary>
    /// <remarks>
    /// Unlike the Stats observer graph this reference implementation followed, node depth needs no
    /// relaxation pass — a machine's nodes form a tree, not a DAG, so <c>NodeDebugInfo.Depth</c> is
    /// already unambiguous. A composite is drawn as a same-sized box next to its children rather
    /// than a container wrapped around them; full nested-box containment is a fair amount more
    /// layout work for the same information the indented <see cref="ActivePathView"/> and the
    /// dimmed/active/active-leaf node styling already carry, so it is left for a later pass.
    /// <para>
    /// Topology (nodes, edges) is rebuilt only when the selected machine or its shape changes;
    /// <see cref="SetValues"/> writes active/selected state into labels and USS classes that
    /// already exist. Rebuilding the whole tree on every poll is exactly the mistake
    /// <c>StatGraphView</c> was written to avoid.
    /// </para>
    /// </remarks>
    public sealed class StateGraphView : VisualElement
    {
        public static readonly string UssClassName = "hfsm-graph";
        public static readonly string ScrollUssClassName = $"{UssClassName}__scroll";
        public static readonly string CanvasUssClassName = $"{UssClassName}__canvas";
        public static readonly string EmptyUssClassName = $"{UssClassName}__empty";

        private const float COLUMN_WIDTH = 168f;

        // Comfortably taller than one node's measured content height (name + kind label + padding
        // + border, ~64-66px at the default font size) -- 46f let neighbouring rows overlap.
        private const float ROW_HEIGHT = 84f;
        private const float NODE_WIDTH = 132f;
        private const float MARGIN = 16f;

        private readonly ScrollView _scroll;
        private readonly VisualElement _canvas;
        private readonly Label _empty;

        private readonly List<StateNodeElement> _nodes = new();
        private readonly Dictionary<int, int> _indexByNode = new();
        private readonly List<TransitionDebugInfo> _edges = new();

        private bool _hasSelection;
        private NodeIndex _selected;

        /// <summary>Raised when the user clicks an active node, so the guard pane can follow it.</summary>
        public event Action<NodeIndex> NodeSelected;

        public StateGraphView()
        {
            AddToClassList(UssClassName);

            var scroll = _scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.AddToClassList(ScrollUssClassName);
            Add(scroll);

            var canvas = _canvas = new VisualElement();
            canvas.AddToClassList(CanvasUssClassName);
            canvas.generateVisualContent += OnGenerateVisualContent;
            scroll.Add(canvas);

            _empty = new Label("Select a machine.");
            _empty.AddToClassList(EmptyUssClassName);
            Add(_empty);

            canvas.RegisterCallback<GeometryChangedEvent>(_ => canvas.MarkDirtyRepaint());
        }

        /// <summary>
        /// Rebuilds every node box and edge list. Only call when the selected machine's definition
        /// changed — two instances of the same definition never need this twice.
        /// </summary>
        public void SetTopology(
              IReadOnlyList<NodeDebugInfo> allNodes
            , IReadOnlyList<TransitionDebugInfo> transitions
            , Func<NodeIndex, string> nameOf
        )
        {
            _canvas.Clear();
            _nodes.Clear();
            _indexByNode.Clear();

            _edges.Clear();

            for (var i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].IsAnyState == false)
                {
                    _edges.Add(transitions[i]);
                }
            }

            BuildNodes(allNodes, nameOf);
            Relayout();

            var hasNodes = _nodes.Count > 0;
            _scroll.style.display = hasNodes ? DisplayStyle.Flex : DisplayStyle.None;
            _empty.style.display = hasNodes ? DisplayStyle.None : DisplayStyle.Flex;

            if (_hasSelection && _indexByNode.ContainsKey(_selected.value) == false)
            {
                _hasSelection = false;
            }

            ApplySelectionClasses();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Writes active/active-leaf state into node boxes that already exist. Safe on every poll.
        /// </summary>
        public void SetValues(IReadOnlyList<NodeDebugInfo> allNodes)
        {
            for (var i = 0; i < allNodes.Count; i++)
            {
                var info = allNodes[i];

                if (_indexByNode.TryGetValue(info.Node.value, out var index) == false)
                {
                    continue;
                }

                var element = _nodes[index];
                var isActiveLeaf = info.IsActive && info.Kind == StateNodeKind.Leaf;

                element.EnableInClassList(StateNodeElement.ActiveUssClassName, info.IsActive);
                element.EnableInClassList(StateNodeElement.ActiveLeafUssClassName, isActiveLeaf);
                element.EnableInClassList(StateNodeElement.DimmedUssClassName, info.IsActive == false);
            }

            _canvas.MarkDirtyRepaint();
        }

        public void SelectNode(NodeIndex node)
        {
            if (_indexByNode.TryGetValue(node.value, out var index) == false)
            {
                return;
            }

            _hasSelection = true;
            _selected = node;
            ApplySelectionClasses();
            _scroll.ScrollTo(_nodes[index]);
        }

        private void ApplySelectionClasses()
        {
            for (var i = 0; i < _nodes.Count; i++)
            {
                _nodes[i].EnableInClassList(
                      StateNodeElement.SelectedUssClassName
                    , _hasSelection && _nodes[i].Node.value == _selected.value
                );
            }
        }

        private void BuildNodes(IReadOnlyList<NodeDebugInfo> allNodes, Func<NodeIndex, string> nameOf)
        {
            var rowPerColumn = new Dictionary<int, int>();

            for (var i = 0; i < allNodes.Count; i++)
            {
                var info = allNodes[i];
                var column = (int)info.Depth;

                rowPerColumn.TryGetValue(column, out var row);
                rowPerColumn[column] = row + 1;

                var element = new StateNodeElement();
                element.Bind(info.Node, nameOf(info.Node), KindLabel(info.Kind));
                element.EnableInClassList(StateNodeElement.ParallelUssClassName, info.Kind == StateNodeKind.Parallel);

                var node = info.Node;
                element.RegisterCallback<PointerDownEvent>(_ => {
                    NodeSelected?.Invoke(node);
                });

                element.userData = (column, row);

                _canvas.Add(element);
                _indexByNode[info.Node.value] = _nodes.Count;
                _nodes.Add(element);
            }
        }

        private void Relayout()
        {
            var maxColumn = 0;
            var maxRow = 0;

            for (var i = 0; i < _nodes.Count; i++)
            {
                var (column, row) = ((int, int))_nodes[i].userData;
                _nodes[i].style.left = MARGIN + (column * COLUMN_WIDTH);
                _nodes[i].style.top = MARGIN + (row * ROW_HEIGHT);
                _nodes[i].style.width = NODE_WIDTH;

                maxColumn = Mathf.Max(maxColumn, column);
                maxRow = Mathf.Max(maxRow, row);
            }

            _canvas.style.width = MARGIN + ((maxColumn + 1) * COLUMN_WIDTH);
            _canvas.style.height = MARGIN + ((maxRow + 1) * ROW_HEIGHT);
        }

        private static string KindLabel(StateNodeKind kind)
            => kind switch {
                StateNodeKind.Leaf => "leaf",
                StateNodeKind.Composite => "composite",
                StateNodeKind.Parallel => "parallel",
                _ => kind.ToString(),
            };

        // ---- painting: transition edges --------------------------------------------------------

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_edges.Count == 0 || _nodes.Count == 0)
            {
                return;
            }

            var painter = context.painter2D;
            painter.lineWidth = 1.6f;
            painter.lineCap = LineCap.Round;

            var polled = new Color(0.42f, 0.55f, 0.70f, 0.95f);
            var triggered = new Color(0.62f, 0.47f, 0.82f, 0.95f);

            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];

                if (_indexByNode.TryGetValue(edge.Source.value, out var fromIndex) == false
                    || _indexByNode.TryGetValue(edge.Target.value, out var toIndex) == false
                )
                {
                    continue;
                }

                var a = _nodes[fromIndex].layout;
                var b = _nodes[toIndex].layout;

                if (float.IsNaN(a.x) || float.IsNaN(b.x))
                {
                    continue;
                }

                var start = new Vector2(a.xMax, a.center.y);
                var end = new Vector2(b.xMin - 7f, b.center.y);
                var color = edge.IsTriggered ? triggered : polled;
                var handleLength = Mathf.Max(24f, (end.x - start.x) * 0.5f);

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
