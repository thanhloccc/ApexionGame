using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>Clock, speed controls, the score line, and restart.</summary>
    public sealed class RtsTopBar
    {
        private static readonly float[] Speeds = { 1f, 2f, 4f };

        private readonly IRtsHudHost _host;
        private readonly Label _clock;
        private readonly Label _teamA;
        private readonly Label _teamB;
        private readonly Button _pause;
        private readonly Button _autoPlay;
        private readonly Button[] _speeds = new Button[Speeds.Length];

        public RtsTopBar(IRtsHudHost host)
        {
            _host = host;

            Root = RtsWidgets.Row();
            Root.style.height = RtsHudTheme.TopBarHeight;
            Root.style.backgroundColor = RtsHudTheme.PanelBg;
            Root.style.borderBottomWidth = 1f;
            Root.style.borderBottomColor = RtsHudTheme.Edge;
            Root.style.paddingLeft = 10f;
            Root.style.paddingRight = 10f;

            _clock = RtsWidgets.Label("00:00", 16, RtsHudTheme.Accent, bold: true);
            _clock.style.width = 54f;
            Root.Add(_clock);

            _pause = RtsWidgets.Button("pause", () => _host.Loop.TogglePause());
            _pause.style.width = 52f;
            Root.Add(_pause);

            for (var i = 0; i < Speeds.Length; i++)
            {
                var speed = Speeds[i];
                var button = RtsWidgets.Button($"{speed:0}×", () => _host.Loop.SetSpeed(speed));

                button.style.width = 32f;
                _speeds[i] = button;
                Root.Add(button);
            }

            _teamA = RtsWidgets.Label(string.Empty, 12, RtsTeamColors.TeamA, bold: true);
            _teamB = RtsWidgets.Label(string.Empty, 12, RtsTeamColors.TeamB, bold: true);

            var score = RtsWidgets.Row();
            score.style.flexGrow = 1f;
            score.style.justifyContent = Justify.Center;
            score.pickingMode = PickingMode.Ignore;
            score.Add(_teamA);
            score.Add(RtsWidgets.Label("  vs  ", 12, RtsHudTheme.InkMuted));
            score.Add(_teamB);
            Root.Add(score);

            _autoPlay = RtsWidgets.Button("auto-play", _host.ToggleAutoPlay);
            Root.Add(_autoPlay);

            Root.Add(RtsWidgets.Button("restart (F5)", _host.Restart));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            var match = _host.Match;
            var loop = _host.Loop;
            var minutes = (int)(match.Elapsed / 60f);
            var seconds = (int)(match.Elapsed - minutes * 60f);

            _clock.text = $"{minutes:00}:{seconds:00}";
            _pause.text = loop.Paused ? "resume" : "pause";

            for (var i = 0; i < _speeds.Length; i++)
            {
                RtsWidgets.SetHighlight(_speeds[i]
                    , Mathf.Approximately(loop.Speed, Speeds[i])
                    , RtsHudTheme.Accent);
            }

            RtsWidgets.SetHighlight(_autoPlay, _host.AutoPlay, RtsHudTheme.GoodInk);

            _teamA.text = Status(match, 0);
            _teamB.text = Status(match, 1);
        }

        private static string Status(RtsMatch match, int teamIndex)
        {
            var team = match.TeamAt(teamIndex);
            var stronghold = team.Stronghold;
            var percent = stronghold == null ? 0f : match.HpFraction(stronghold) * 100f;

            return $"{team.Name}   {team.ArmyCount} units   HQ {percent:0}%";
        }
    }
}
