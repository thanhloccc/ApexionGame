using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>Six spells, their cost, their cooldown, and which one is being aimed.</summary>
    public sealed class RtsSpellBar
    {
        private static readonly Color AllyEdge = new(0.3f, 0.42f, 0.5f);
        private static readonly Color EnemyEdge = new(0.45f, 0.3f, 0.5f);

        private readonly IRtsHudHost _host;
        private readonly Button[] _buttons;
        private readonly Label[] _subtitles;

        public RtsSpellBar(IRtsHudHost host)
        {
            _host = host;

            Root = RtsWidgets.Row();
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = RtsHudTheme.PanelBgSoft;
            Root.style.borderTopWidth = 1f;
            Root.style.borderTopColor = RtsHudTheme.Edge;
            RtsWidgets.Padding(Root, 6f);

            var spells = RtsContent.Spells;

            _buttons = new Button[spells.Count];
            _subtitles = new Label[spells.Count];

            for (var i = 0; i < spells.Count; i++)
            {
                var index = i;
                var spell = spells[i];

                var button = RtsWidgets.StackedButton(
                      $"{spell.Hotkey}. {spell.Name}"
                    , () => _host.Player.SelectSpell(_host.Match, index)
                    , out var subtitle);

                button.style.width = 118f;
                button.style.minHeight = 40f;
                button.tooltip = Tooltip(spell);

                RtsWidgets.Border(button, spell.IsDebuff ? EnemyEdge : AllyEdge);

                _buttons[i] = button;
                _subtitles[i] = subtitle;

                Root.Add(button);
            }
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            var match = _host.Match;
            var teamIndex = _host.Player.TeamIndex;
            var aiming = _host.Player.AimingSpell;
            var spells = RtsContent.Spells;

            for (var i = 0; i < _buttons.Length; i++)
            {
                var cooldown = match.SpellCooldown(teamIndex, i);

                _subtitles[i].text = cooldown > 0f
                    ? $"{cooldown:0.#}s"
                    : $"{spells[i].SupplyCost} supply";

                RtsWidgets.SetAvailability(_buttons[i], match.CanCast(teamIndex, i));
                RtsWidgets.SetHighlight(_buttons[i], aiming == i, RtsHudTheme.SpellInk);
            }
        }

        private static string Tooltip(RtsSpell spell)
        {
            var terms = spell.TermsText;

            return terms.Length > 0
                ? $"{terms} · {spell.Duration:0.#}s · radius {spell.Radius:0.#}"
                : $"+{spell.InstantHeal:0.#} hp · radius {spell.Radius:0.#}";
        }
    }
}
