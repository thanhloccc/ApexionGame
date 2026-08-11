using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// Assembles the panels and refreshes them. It owns layout, and nothing else.
    /// </summary>
    /// <remarks>
    /// Every panel below is its own class with its own build and refresh. This file only decides where they sit,
    /// so adding a panel does not mean editing a seven-hundred-line class.
    /// </remarks>
    public sealed class RtsHud
    {
        private readonly IRtsHudHost _host;
        private readonly VisualElement _root;

        private readonly RtsTopBar _topBar;
        private readonly RtsCommandPanel _commandPanel;
        private readonly RtsGraphPanel _graphPanel;
        private readonly RtsSpellBar _spellBar;
        private readonly RtsJournalPanel _journalPanel;
        private readonly RtsInspectorPanel _inspectorPanel;

        private readonly Label _notice;
        private readonly Label _banner;

        public RtsHud(UIDocument document, IRtsHudHost host)
        {
            _host = host;
            _root = document == null ? null : document.rootVisualElement;

            if (_root == null)
            {
                return;
            }

            _topBar = new RtsTopBar(host);
            _commandPanel = new RtsCommandPanel(host);
            _graphPanel = new RtsGraphPanel(host);
            _spellBar = new RtsSpellBar(host);
            _journalPanel = new RtsJournalPanel(host);
            _inspectorPanel = new RtsInspectorPanel(host);

            _root.style.flexDirection = FlexDirection.Column;
            _root.style.height = Length.Percent(100f);
            _root.pickingMode = PickingMode.Ignore;

            _root.Add(_topBar.Root);
            _root.Add(BuildMiddle());
            _root.Add(BuildBottom(out _notice));

            _banner = RtsWidgets.Label(string.Empty, 34, RtsHudTheme.Accent, bold: true);
            _banner.style.position = Position.Absolute;
            _banner.style.left = 0f;
            _banner.style.right = 0f;
            _banner.style.top = Length.Percent(34f);
            _banner.style.unityTextAlign = TextAnchor.MiddleCenter;
            _banner.style.display = DisplayStyle.None;
            _root.Add(_banner);
        }

        public bool IsBuilt => _root != null;

        /// <summary>
        /// True when the pointer is over a panel, so the world click underneath should not happen.
        /// </summary>
        /// <remarks>
        /// The root is <c>PickingMode.Ignore</c>, so anything the panel picks is a real control rather than the
        /// full-screen container. This is what stops a click on the spell bar from also casting the spell.
        /// </remarks>
        public bool IsPointerOver(Vector2 screenPosition)
        {
            var panel = _root?.panel;

            if (panel == null)
            {
                return false;
            }

            var picked = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screenPosition));

            return picked != null && picked != _root;
        }

        public void Refresh(float deltaTime)
        {
            if (_root == null || _host.Match == null)
            {
                return;
            }

            _topBar.Refresh();
            _commandPanel.Refresh();
            _graphPanel.Refresh();
            _spellBar.Refresh();
            _journalPanel.Refresh();
            _inspectorPanel.Refresh(deltaTime);

            _notice.text = _host.Player.Notice;

            RefreshBanner();
        }

        /// <summary>Called when the host throws the match away and builds a new one.</summary>
        public void OnMatchReplaced() => _journalPanel?.Invalidate();

        private VisualElement BuildMiddle()
        {
            var middle = RtsWidgets.Row();

            middle.style.flexGrow = 1f;
            middle.style.alignItems = Align.FlexStart;
            middle.style.justifyContent = Justify.SpaceBetween;
            middle.pickingMode = PickingMode.Ignore;

            var left = RtsWidgets.Column();
            left.style.width = RtsHudTheme.CommandColumnWidth;
            left.style.marginLeft = 8f;
            left.style.marginTop = 8f;
            left.Add(_commandPanel.Root);
            left.Add(_graphPanel.Root);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            spacer.pickingMode = PickingMode.Ignore;

            middle.Add(left);
            middle.Add(spacer);
            middle.Add(_inspectorPanel.Root);

            return middle;
        }

        private VisualElement BuildBottom(out Label notice)
        {
            var bottom = RtsWidgets.Column();

            notice = RtsWidgets.Label(string.Empty, 12, RtsHudTheme.WarnInk, bold: true);
            notice.style.unityTextAlign = TextAnchor.MiddleCenter;
            notice.style.height = 18f;

            bottom.Add(notice);
            bottom.Add(_spellBar.Root);
            bottom.Add(_journalPanel.Root);

            return bottom;
        }

        private void RefreshBanner()
        {
            var outcome = _host.Match.Outcome;

            if (outcome.IsDecided == false)
            {
                _banner.style.display = DisplayStyle.None;
                return;
            }

            _banner.style.display = DisplayStyle.Flex;
            _banner.text = $"{_host.Match.TeamAt(outcome.WinnerTeam).Name.ToUpperInvariant()} WINS";
            _banner.style.color = RtsTeamColors.Of(outcome.WinnerTeam);
        }
    }
}
