using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>Supply, the spawn menu, the research lines, and how the stronghold is holding up.</summary>
    public sealed class RtsCommandPanel
    {
        private static readonly string[] SpawnKeys = { "Q", "W", "E", "R" };

        private readonly IRtsHudHost _host;
        private readonly Label _supply;
        private readonly Button[] _spawnButtons;
        private readonly Label[] _spawnSubtitles;
        private readonly Button[] _researchButtons;
        private readonly Label[] _researchSubtitles;
        private readonly Label _strongholdLabel;
        private readonly VisualElement _strongholdFill;

        public RtsCommandPanel(IRtsHudHost host)
        {
            _host = host;

            var team = host.Match.TeamAt(host.Player.TeamIndex);

            Root = RtsWidgets.Box(team.Name.ToUpperInvariant());

            _supply = RtsWidgets.Label("0 supply", 13, RtsHudTheme.Ink, bold: true);
            _supply.style.marginBottom = 6f;
            Root.Add(_supply);

            var menu = RtsContent.SpawnMenuOf(host.Player.TeamIndex);

            _spawnButtons = new Button[menu.Count];
            _spawnSubtitles = new Label[menu.Count];

            var grid = RtsWidgets.Row();
            grid.style.flexWrap = Wrap.Wrap;

            for (var i = 0; i < menu.Count; i++)
            {
                var index = i;
                var archetype = menu[i];

                var button = RtsWidgets.StackedButton(
                      archetype.Name
                    , () => _host.Player.Spawn(_host.Match, index)
                    , out var subtitle);

                button.style.width = 98f;
                button.style.marginBottom = 4f;
                button.tooltip = $"{SpawnKeys[i]} — {archetype.SupplyCost} supply";

                _spawnButtons[i] = button;
                _spawnSubtitles[i] = subtitle;

                grid.Add(button);
            }

            Root.Add(grid);

            var heading = RtsWidgets.Label("RESEARCH", 10, RtsHudTheme.InkMuted, bold: true);
            heading.style.marginTop = 6f;
            heading.style.marginBottom = 4f;
            Root.Add(heading);

            _researchButtons = new Button[RtsContent.Researches.Count];
            _researchSubtitles = new Label[RtsContent.Researches.Count];

            for (var i = 0; i < RtsContent.Researches.Count; i++)
            {
                var index = i;
                var research = RtsContent.Researches[i];

                var button = RtsWidgets.StackedButton(
                      research.Name
                    , () => _host.Player.Research(_host.Match, index)
                    , out var subtitle);

                button.style.width = Length.Percent(100f);
                button.style.minHeight = 32f;
                button.style.marginBottom = 4f;
                button.tooltip = $"+{research.AmountPerLevel:0.##} {research.Target} on the team node";

                _researchButtons[i] = button;
                _researchSubtitles[i] = subtitle;

                Root.Add(button);
            }

            _strongholdLabel = RtsWidgets.Label("Stronghold", 10, RtsHudTheme.InkMuted);
            _strongholdLabel.style.marginTop = 4f;
            Root.Add(_strongholdLabel);
            Root.Add(RtsWidgets.Meter(out _strongholdFill, RtsTeamColors.Of(host.Player.TeamIndex)));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            var match = _host.Match;
            var teamIndex = _host.Player.TeamIndex;
            var team = match.TeamAt(teamIndex);

            _supply.text = $"{team.Supply:0} supply   (+{match.Settings.supplyPerSecond:0}/s)";

            var menu = RtsContent.SpawnMenuOf(teamIndex);

            for (var i = 0; i < _spawnButtons.Length; i++)
            {
                var archetype = menu[i];
                var fielded = archetype.IsHero && team.Hero != null;

                _spawnSubtitles[i].text = fielded ? "on the field" : $"{archetype.SupplyCost} supply";

                RtsWidgets.SetAvailability(_spawnButtons[i], match.CanSpawn(teamIndex, archetype));
            }

            for (var i = 0; i < _researchButtons.Length; i++)
            {
                var research = RtsContent.Researches[i];
                var level = team.LevelOf(i);
                var cost = match.Economy.CostOf(team, i);

                _researchSubtitles[i].text = cost < 0
                    ? $"{Pips(level, research.MaxLevel)}   maxed"
                    : $"{Pips(level, research.MaxLevel)}   {cost} supply";

                RtsWidgets.SetAvailability(_researchButtons[i], match.CanResearch(teamIndex, i));
            }

            var stronghold = team.Stronghold;
            var fraction = stronghold == null ? 0f : match.HpFraction(stronghold);

            _strongholdFill.style.width = Length.Percent(fraction * 100f);

            _strongholdLabel.text = stronghold == null
                ? "Stronghold"
                : $"Stronghold {match.Read(stronghold.Handles.hp):0} hp";
        }

        private static string Pips(int level, int maxLevel)
        {
            var pips = string.Empty;

            for (var i = 0; i < maxLevel; i++)
            {
                pips += i < level ? "●" : "○";
            }

            return pips;
        }
    }
}
