using System;
using System.Collections.Generic;
using ApexionGame.HFSM.Debugging;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Editor.Views
{
    /// <summary>
    /// Decides what actually needs rebuilding on each poll — see HFSM - Debugging.md §4.1. Only the
    /// live-machine set and the selected machine's shape ever rebuild the tree; everything else
    /// (times, active state, guard results, log rows) is written into elements that already exist.
    /// </summary>
    public sealed class MachineDebuggerViewController : IDisposable
    {
        private readonly MachineDebuggerView _view;

        private readonly List<IMachineDebug> _machines = new();
        private readonly List<IMachineDebug> _lastMachines = new();
        private readonly List<string> _machineLabels = new();

        private readonly List<NodeDebugInfo> _nodes = new();
        private readonly List<NodeIndex> _activeNodes = new();
        private readonly List<TransitionDebugInfo> _transitions = new();
        private readonly List<TransitionLogEntry> _log = new();
        private readonly List<GuardDebugInfo> _guards = new();

        private readonly List<ActivePathView.Row> _pathRows = new();
        private readonly List<(string container, string child)> _historyRows = new();
        private readonly List<(GuardDebugInfo info, string routeText)> _guardRows = new();
        private readonly List<TransitionLogView.Row> _logRows = new();

        private bool _machinesBuilt;
        private bool _topologyBuilt;
        private int _machineIndex;

        private bool _hasClickedNode;
        private NodeIndex _clickedNode;

        public MachineDebuggerViewController(MachineDebuggerView view)
        {
            _view = view;
            _view.userData = this;
            _view.MachineSelected += OnMachineSelected;
            _view.RefreshRequested += ForceRefresh;
            _view.AutoRefreshToggled += value => AutoRefresh = value;
            _view.GraphNodeSelected += OnGraphNodeSelected;

            MachineDebugRegistry.Changed += ForceRefresh;

            ForceRefresh();
        }

        public bool AutoRefresh { get; private set; } = true;

        public void Dispose()
        {
            _view.MachineSelected -= OnMachineSelected;
            _view.RefreshRequested -= ForceRefresh;
            _view.GraphNodeSelected -= OnGraphNodeSelected;

            MachineDebugRegistry.Changed -= ForceRefresh;
        }

        /// <summary>Called from the window's poll. Cheap when nothing changed shape.</summary>
        public void Poll()
        {
            if (AutoRefresh)
            {
                Refresh();
            }
        }

        private void ForceRefresh()
        {
            _machinesBuilt = false;
            _topologyBuilt = false;
            Refresh();
        }

        private void Refresh()
        {
            MachineDebugRegistry.PruneDead();

            _machines.Clear();
            _machines.AddRange(MachineDebugRegistry.Machines);

            _machineIndex = Clamp(_machineIndex, _machines.Count);

            if (_machinesBuilt == false || ReferenceSequenceEqual(_machines, _lastMachines) == false)
            {
                _machinesBuilt = true;
                _topologyBuilt = false;
                CopyInto(_machines, _lastMachines);

                _machineLabels.Clear();

                for (var i = 0; i < _machines.Count; i++)
                {
                    _machineLabels.Add(_machines[i].Name);
                }

                _view.SetMachines(_machineLabels, _machines.Count > 0 ? _machineIndex : -1);
            }

            if (_machines.Count == 0)
            {
                return;
            }

            var machine = _machines[_machineIndex];
            var distinctDefinitions = CountDistinctDefinitions();

            _view.SetInfo(machine.IsAlive
                ? $"{_machines.Count} live · {distinctDefinitions} def(s)"
                : "machine disposed — the registration outlived it");

            _nodes.Clear();
            machine.GetNodes(_nodes);

            if (_topologyBuilt == false)
            {
                _topologyBuilt = true;

                _transitions.Clear();
                machine.GetTransitions(_transitions);

                _view.Graph.SetTopology(_nodes, _transitions, machine.NameOf);
            }

            _view.Graph.SetValues(_nodes);

            RefreshActivePath(machine);
            RefreshGuards(machine);
            RefreshLog(machine);
        }

        private void RefreshActivePath(IMachineDebug machine)
        {
            _activeNodes.Clear();
            machine.GetActiveNodes(_activeNodes);

            _pathRows.Clear();
            _historyRows.Clear();

            for (var i = 0; i < _activeNodes.Count; i++)
            {
                var node = _activeNodes[i];
                var info = FindNode(node);

                _pathRows.Add(new ActivePathView.Row(
                      machine.NameOf(node), info.Depth, info.TimeInNode, info.Kind == StateNodeKind.Leaf
                ));

                // _historyChild bookkeeping runs for every single-child container on exit,
                // whether or not it declared history — only a node with History != None actually
                // consults it on re-entry, so that is the condition that means something here.
                if (info.History != HistoryMode.None && info.HistoryChild.IsValid)
                {
                    _historyRows.Add((machine.NameOf(node), machine.NameOf(info.HistoryChild)));
                }
            }

            _view.ActivePath.SetRows(_pathRows, _historyRows);
        }

        private void RefreshGuards(IMachineDebug machine)
        {
            var leaf = ResolveGuardLeaf();

            _guards.Clear();
            _guardRows.Clear();

            if (leaf.IsValid)
            {
                machine.GetOutgoingGuards(leaf, _guards);

                for (var i = 0; i < _guards.Count; i++)
                {
                    var info = _guards[i];
                    var route = info.IsAnyState
                        ? $"* -> {machine.NameOf(info.Target)}"
                        : $"{machine.NameOf(info.Source)} -> {machine.NameOf(info.Target)}";

                    _guardRows.Add((info, route));
                }
            }

            _view.Guards.SetTitle(leaf.IsValid ? machine.NameOf(leaf) : null);
            _view.Guards.SetGuards(_guardRows);
        }

        /// <summary>
        /// The node the guard pane inspects: whichever active leaf the user last clicked in the
        /// graph, falling back to the deepest active leaf once that click is no longer active.
        /// </summary>
        private NodeIndex ResolveGuardLeaf()
        {
            if (_hasClickedNode)
            {
                for (var i = 0; i < _activeNodes.Count; i++)
                {
                    if (_activeNodes[i].value == _clickedNode.value)
                    {
                        return _clickedNode;
                    }
                }

                _hasClickedNode = false;
            }

            var fallback = NodeIndex.Invalid;

            for (var i = 0; i < _activeNodes.Count; i++)
            {
                var info = FindNode(_activeNodes[i]);

                if (info.Kind == StateNodeKind.Leaf)
                {
                    fallback = _activeNodes[i];
                }
            }

            return fallback;
        }

        private void RefreshLog(IMachineDebug machine)
        {
            _log.Clear();
            machine.GetLog(_log);

            _logRows.Clear();

            for (var i = _log.Count - 1; i >= 0; i--)
            {
                var entry = _log[i];

                var route = entry.IsUnmatchedTrigger
                    ? $"{machine.NameOf(entry.From)} (unmatched)"
                    : $"{machine.NameOf(entry.From)} -> {machine.NameOf(entry.To)}";

                _logRows.Add(new TransitionLogView.Row(
                      route, entry.Frame, $"{entry.TimeInSource:0.00}s", CauseText(entry), entry.IsUnmatchedTrigger
                ));
            }

            _view.Log.SetRows(_logRows);
        }

        private static string CauseText(in TransitionLogEntry entry)
            => entry.Cause switch {
                TransitionCause.Guard => "guard",
                TransitionCause.Trigger => $"trigger: {entry.Trigger.ToDisplayName()}",
                TransitionCause.Timer => "timer",
                TransitionCause.Request => "request",
                TransitionCause.Initial => "initial",
                TransitionCause.History => "history",
                TransitionCause.UnmatchedTrigger => $"unmatched: {entry.Trigger.ToDisplayName()}",
                _ => entry.Cause.ToString(),
            };

        private NodeDebugInfo FindNode(NodeIndex node)
        {
            for (var i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].Node.value == node.value)
                {
                    return _nodes[i];
                }
            }

            return default;
        }

        private int CountDistinctDefinitions()
        {
            var seen = new HashSet<string>();

            for (var i = 0; i < _machines.Count; i++)
            {
                seen.Add(_machines[i].DefinitionName);
            }

            return seen.Count;
        }

        private void OnMachineSelected(int index)
        {
            if (index < 0 || index == _machineIndex)
            {
                return;
            }

            _machineIndex = index;
            _hasClickedNode = false;
            _topologyBuilt = false;
            Refresh();
        }

        private void OnGraphNodeSelected(NodeIndex node)
        {
            _hasClickedNode = true;
            _clickedNode = node;

            if (_machineIndex < _machines.Count)
            {
                RefreshGuards(_machines[_machineIndex]);
            }

            _view.Graph.SelectNode(node);
        }

        private static int Clamp(int index, int count)
            => count <= 0 ? 0 : Math.Clamp(index, 0, count - 1);

        private static bool ReferenceSequenceEqual(List<IMachineDebug> a, List<IMachineDebug> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (ReferenceEquals(a[i], b[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CopyInto(List<IMachineDebug> source, List<IMachineDebug> destination)
        {
            destination.Clear();
            destination.AddRange(source);
        }
    }

    public static class MachineDebuggerAPI
    {
        public static MachineDebuggerViewController CreateView(VisualElement root)
        {
            var view = new MachineDebuggerView();
            root.Add(view);

            return new MachineDebuggerViewController(view);
        }
    }
}
