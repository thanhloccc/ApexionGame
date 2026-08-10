#if (UNITY_EDITOR || DEVELOPMENT_BUILD || APEXION_HFSM_DEBUG) && !DISABLE_APEXION_CHECKS

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// HFSM - Debugging.md §5 — the on-device counterpart of the editor window: active path, time
    /// in state, and the last transition, for whichever registered machine is selected.
    /// </summary>
    /// <remarks>
    /// Built and styled entirely in C#, through <see cref="MachineOverlayTheme"/> — no serialized
    /// <c>StyleSheet</c>, so dropping <see cref="MachineOverlayBehaviour"/> into a scene needs no
    /// asset wiring. Polls itself at 10 Hz via <see cref="VisualElement.schedule"/> rather than
    /// depending on its host's <c>Update</c>, so it works the same whether the host ticks every
    /// frame or not.
    /// </remarks>
    public sealed class MachineOverlayPanel : VisualElement
    {
        private readonly Label _title;
        private readonly Label _path;
        private readonly Label _last;

        private readonly List<NodeIndex> _activeScratch = new();
        private readonly List<TransitionLogEntry> _logScratch = new();
        private readonly StringBuilder _pathBuilder = new();

        private string _filterPrefix = string.Empty;
        private int _selectedIndex;

        public MachineOverlayPanel()
        {
            pickingMode = PickingMode.Ignore;

            style.position = Position.Absolute;
            style.width = MachineOverlayTheme.Width;
            style.paddingLeft = MachineOverlayTheme.Padding;
            style.paddingRight = MachineOverlayTheme.Padding;
            style.paddingTop = MachineOverlayTheme.Padding;
            style.paddingBottom = MachineOverlayTheme.Padding;
            style.backgroundColor = MachineOverlayTheme.PanelBg;
            style.borderTopWidth = style.borderBottomWidth = 1f;
            style.borderLeftWidth = style.borderRightWidth = 1f;
            style.borderTopColor = style.borderBottomColor = MachineOverlayTheme.Border;
            style.borderLeftColor = style.borderRightColor = MachineOverlayTheme.Border;

            _title = AddLine(MachineOverlayTheme.Title, bold: true);
            _path = AddLine(MachineOverlayTheme.Ink, bold: false);
            _last = AddLine(MachineOverlayTheme.Muted, bold: false);

            schedule.Execute(Refresh).Every(100);
        }

        /// <summary>Only machines whose <c>DebugName</c> starts with this are cycled through.</summary>
        public void SetFilter(string debugNamePrefix)
        {
            _filterPrefix = debugNamePrefix ?? string.Empty;
            _selectedIndex = 0;
        }

        public void Next() => Cycle(1);

        public void Previous() => Cycle(-1);

        private void Cycle(int delta)
        {
            var count = CountMatching();

            if (count == 0)
            {
                _selectedIndex = 0;
                return;
            }

            _selectedIndex = ((_selectedIndex + delta) % count + count) % count;
            Refresh();
        }

        private void Refresh()
        {
            MachineDebugRegistry.PruneDead();

            var machine = ResolveSelected();

            if (machine == null)
            {
                _title.text = "HFSM · (no machine)";
                _path.text = string.Empty;
                _last.text = string.Empty;
                return;
            }

            _title.text = $"HFSM · {machine.Name}";

            _activeScratch.Clear();
            machine.GetActiveNodes(_activeScratch);

            _pathBuilder.Clear();

            for (var i = 0; i < _activeScratch.Count; i++)
            {
                if (i > 0)
                {
                    _pathBuilder.Append('/');
                }

                _pathBuilder.Append(machine.NameOf(_activeScratch[i]));
            }

            _path.text = $"{_pathBuilder} · {machine.TimeInMachine:0.0}s";

            _logScratch.Clear();
            machine.GetLog(_logScratch);

            _last.text = _logScratch.Count == 0
                ? "last: (none)"
                : FormatLast(machine, _logScratch[^1]);
        }

        private string FormatLast(IMachineDebug machine, in TransitionLogEntry entry)
        {
            var cause = entry.Cause switch {
                TransitionCause.Guard => "guard",
                TransitionCause.Trigger => $"trigger:{entry.Trigger.ToDisplayName()}",
                TransitionCause.Timer => "timer",
                TransitionCause.Request => "request",
                TransitionCause.Initial => "initial",
                TransitionCause.History => "history",
                TransitionCause.UnmatchedTrigger => "unmatched",
                _ => entry.Cause.ToString(),
            };

            return entry.IsUnmatchedTrigger
                ? $"last: {machine.NameOf(entry.From)} (unmatched)"
                : $"last: {machine.NameOf(entry.From)}→{machine.NameOf(entry.To)}  ({cause})";
        }

        private IMachineDebug ResolveSelected()
        {
            var machines = MachineDebugRegistry.Machines;
            var index = 0;

            for (var i = 0; i < machines.Count; i++)
            {
                if (Matches(machines[i]) == false)
                {
                    continue;
                }

                if (index == _selectedIndex)
                {
                    return machines[i];
                }

                index++;
            }

            return null;
        }

        private int CountMatching()
        {
            var machines = MachineDebugRegistry.Machines;
            var count = 0;

            for (var i = 0; i < machines.Count; i++)
            {
                if (Matches(machines[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private bool Matches(IMachineDebug machine)
            => _filterPrefix.Length == 0 || machine.Name.StartsWith(_filterPrefix, System.StringComparison.Ordinal);

        private Label AddLine(UnityEngine.Color color, bool bold)
        {
            var label = new Label();
            label.style.color = color;
            label.style.fontSize = MachineOverlayTheme.FontSize;
            label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            Add(label);
            return label;
        }
    }
}

#endif
