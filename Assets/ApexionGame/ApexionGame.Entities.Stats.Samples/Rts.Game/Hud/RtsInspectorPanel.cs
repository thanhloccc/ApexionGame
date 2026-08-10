using EncosyTower.Collections;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// Base value, current value, and every modifier in between.
    /// </summary>
    /// <remarks>
    /// The panel that answers "where did that number come from". It renders rows built by
    /// <see cref="RtsStatInspector"/>, which reads the store rather than the game's bookkeeping — so a modifier
    /// the game forgot it added still shows up here.
    /// </remarks>
    public sealed class RtsInspectorPanel
    {
        /// <summary>Rows are rebuilt at most this often while the selection stays the same.</summary>
        private const float RefreshSeconds = 0.2f;

        private readonly IRtsHudHost _host;
        private readonly FasterList<RtsInspectorRow> _rows = new(64);
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly VisualElement _body;

        private object _subject;
        private float _timer;

        public RtsInspectorPanel(IRtsHudHost host)
        {
            _host = host;

            Root = RtsWidgets.Box("INSPECTOR");
            Root.style.width = RtsHudTheme.InspectorWidth;
            Root.style.marginRight = 8f;
            Root.style.marginTop = 8f;
            Root.style.maxHeight = Length.Percent(88f);

            _title = RtsWidgets.Label("nothing selected", 14, RtsHudTheme.Ink, bold: true);
            Root.Add(_title);

            _subtitle = RtsWidgets.Label(
                  "click a unit on the field, or the node button"
                , 10
                , RtsHudTheme.InkMuted);

            _subtitle.style.marginBottom = 6f;
            _subtitle.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_subtitle);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            Root.Add(scroll);

            _body = scroll.contentContainer;
        }

        public VisualElement Root { get; }

        public void Refresh(float deltaTime)
        {
            var match = _host.Match;
            var player = _host.Player;

            object subject = player.InspectingNode
                ? match.TeamAt(player.TeamIndex)
                : player.Selected;

            var changed = ReferenceEquals(subject, _subject) == false;

            _timer -= deltaTime;

            if (changed == false && _timer > 0f)
            {
                return;
            }

            _subject = subject;
            _timer = RefreshSeconds;

            switch (subject)
            {
                case RtsUnit unit:
                {
                    _title.text = unit.Label;
                    _title.style.color = RtsTeamColors.Of(unit.TeamIndex);

                    _subtitle.text = unit.IsHero
                        ? "a unit like any other — except for the aura terms it puts on its team's node"
                        : $"{match.TeamAt(unit.TeamIndex).Name} · {unit.Archetype.Name}";

                    match.Inspector.Describe(unit, _rows);
                    break;
                }

                case RtsTeam team:
                {
                    _title.text = $"{team.Name} — shared node";
                    _title.style.color = RtsTeamColors.Of(team.Index);
                    _subtitle.text = "base values are research, modifiers are hero auras. Every unit reads this.";

                    match.Inspector.Describe(team, _rows);
                    break;
                }

                default:
                {
                    _title.text = "nothing selected";
                    _title.style.color = RtsHudTheme.Ink;
                    _subtitle.text = "click a unit on the field, or the node button";
                    _rows.Clear();
                    break;
                }
            }

            Render();
        }

        private void Render()
        {
            _body.Clear();

            var rows = _rows.AsReadOnlySpan();

            for (var i = 0; i < rows.Length; i++)
            {
                _body.Add(RowElement(rows[i]));
            }
        }

        private static VisualElement RowElement(in RtsInspectorRow row)
        {
            switch (row.Kind)
            {
                case RtsRowKind.Section:
                {
                    var label = RtsWidgets.Label(
                          row.Label.ToUpperInvariant()
                        , 9
                        , RtsHudTheme.InkMuted
                        , bold: true);

                    label.style.marginTop = 8f;
                    label.style.marginBottom = 2f;

                    return label;
                }

                case RtsRowKind.Modifier:
                {
                    var label = RtsWidgets.Label($"▸ {row.Label}", 10, RtsHudTheme.GraphInk);

                    label.style.marginLeft = 10f;
                    label.style.whiteSpace = WhiteSpace.Normal;

                    return label;
                }

                case RtsRowKind.Stat:
                    return TwoColumns(row.Label, row.Value, 11, RtsHudTheme.Ink, RtsHudTheme.Accent, bold: true);

                case RtsRowKind.Effect:
                    return TwoColumns(row.Label, row.Value, 10, RtsHudTheme.SpellInk, RtsHudTheme.InkMuted
                        , bold: true);

                default:
                    return TwoColumns(row.Label, row.Value, 10, RtsHudTheme.InkMuted, RtsHudTheme.Ink);
            }
        }

        private static VisualElement TwoColumns(
              string left
            , string right
            , int size
            , UnityEngine.Color leftColor
            , UnityEngine.Color rightColor
            , bool bold = false
        )
        {
            var row = RtsWidgets.Row();

            row.style.justifyContent = Justify.SpaceBetween;
            row.Add(RtsWidgets.Label(left, size, leftColor, bold));
            row.Add(RtsWidgets.Label(right, size, rightColor, bold));

            return row;
        }
    }
}
