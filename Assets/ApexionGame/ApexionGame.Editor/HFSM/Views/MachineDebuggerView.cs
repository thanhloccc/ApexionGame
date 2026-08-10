using System;
using System.Collections.Generic;
using ApexionGame.HFSM.Debugging;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Editor.Views
{
    /// <summary>
    /// HFSM - Debugging.md §4 — the whole window body: toolbar, active path, guard pane, state
    /// graph, transition log. Structure only; <see cref="MachineDebuggerViewController"/> owns data
    /// and refresh.
    /// </summary>
    public sealed class MachineDebuggerView : VisualElement
    {
        public static readonly string UssClassName = "hfsm-debugger";
        public static readonly string ToolbarUssClassName = $"{UssClassName}__toolbar";
        public static readonly string ToolbarFieldUssClassName = $"{ToolbarUssClassName}-field";
        public static readonly string ToolbarInfoUssClassName = $"{ToolbarUssClassName}-info";
        public static readonly string ToolbarSpacerUssClassName = $"{ToolbarUssClassName}-spacer";
        public static readonly string ToolbarToggleUssClassName = $"{ToolbarUssClassName}-toggle";
        public static readonly string ToolbarToggleLabelUssClassName = $"{ToolbarToggleUssClassName}-label";
        public static readonly string ToolbarButtonUssClassName = $"{ToolbarUssClassName}-button";
        public static readonly string BodyUssClassName = $"{UssClassName}__body";
        public static readonly string LeftColumnUssClassName = $"{UssClassName}__left-column";
        public static readonly string EmptyUssClassName = $"{UssClassName}__empty";

        public event Action<int> MachineSelected;
        public event Action RefreshRequested;
        public event Action<bool> AutoRefreshToggled;
        public event Action<NodeIndex> GraphNodeSelected;

        private readonly DropdownField _machineField;
        private readonly Label _info;
        private readonly Label _empty;
        private readonly VisualElement _body;

        public ActivePathView ActivePath { get; }
        public GuardListView Guards { get; }
        public StateGraphView Graph { get; }
        public TransitionLogView Log { get; }

        public MachineDebuggerView()
        {
            AddToClassList(UssClassName);

            Add(BuildToolbar(out _machineField, out _info));

            _empty = new Label(
                "No HFSM machine is registered.\n\n"
                + "A machine registers itself the moment it is created, under APEXION_HFSM_DEBUG "
                + "(on in the Editor and development builds). Create one with "
                + "HierarchicalStateMachine<TContext, TState>.Define(...).BuildOrThrow().CreateInstance(...) "
                + "and it will appear here.");
            _empty.AddToClassList(EmptyUssClassName);
            Add(_empty);

            _body = new VisualElement();
            _body.AddToClassList(BodyUssClassName);
            Add(_body);

            // Fixed pane is index 1 (the log) so the log stays a small strip and the active
            // path/guards/graph area above it is the one that gets the room.
            var vertical = new TwoPaneSplitView(1, 160f, TwoPaneSplitViewOrientation.Vertical);
            _body.Add(vertical);

            var horizontal = new TwoPaneSplitView(0, 300f, TwoPaneSplitViewOrientation.Horizontal);
            vertical.Add(horizontal);

            var leftColumn = new VisualElement();
            leftColumn.AddToClassList(LeftColumnUssClassName);
            horizontal.Add(leftColumn);

            ActivePath = new ActivePathView();
            leftColumn.Add(ActivePath);

            Guards = new GuardListView();
            leftColumn.Add(Guards);

            Graph = new StateGraphView();
            horizontal.Add(Graph);
            Graph.NodeSelected += node => GraphNodeSelected?.Invoke(node);

            Log = new TransitionLogView();
            vertical.Add(Log);
        }

        public void SetMachines(List<string> labels, int selectedIndex)
        {
            _machineField.choices = new List<string>(labels);
            _machineField.SetValueWithoutNotify(
                selectedIndex >= 0 && selectedIndex < labels.Count ? labels[selectedIndex] : string.Empty);

            var hasMachine = labels.Count > 0;
            _empty.style.display = hasMachine ? DisplayStyle.None : DisplayStyle.Flex;
            _body.style.display = hasMachine ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetInfo(string text) => _info.text = text;

        private VisualElement BuildToolbar(out DropdownField machineField, out Label info)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList(ToolbarUssClassName);

            var field = machineField = new DropdownField { choices = new List<string>() };
            field.AddToClassList(ToolbarFieldUssClassName);
            field.RegisterValueChangedCallback(evt => MachineSelected?.Invoke(field.choices.IndexOf(evt.newValue)));
            toolbar.Add(field);

            info = new Label();
            info.AddToClassList(ToolbarInfoUssClassName);
            toolbar.Add(info);

            var spacer = new VisualElement();
            spacer.AddToClassList(ToolbarSpacerUssClassName);
            toolbar.Add(spacer);

            var auto = new Toggle { value = true, tooltip = "Keep polling the selected machine while this is on." };
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
    }
}
