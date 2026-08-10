using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// The stat graph, as numbers you can watch and buttons you can press.
    /// </summary>
    /// <remarks>
    /// This panel is why the sample is worth opening rather than reading. The observer count is the one that
    /// matters: press <c>cull 5</c> with <c>sloppy</c> off and it drops, press it with <c>sloppy</c> on and it
    /// does not — which is an entire class of leak, made visible in two clicks.
    /// </remarks>
    public sealed class RtsGraphPanel
    {
        private readonly IRtsHudHost _host;
        private readonly Label _owners;
        private readonly Label _observersA;
        private readonly Label _observersB;
        private readonly Label _dangling;
        private readonly Label _changedStats;
        private readonly Label _changeEvents;
        private readonly Label _effects;
        private readonly Button _sloppy;

        public RtsGraphPanel(IRtsHudHost host)
        {
            _host = host;

            Root = RtsWidgets.Box("STAT GRAPH");

            Root.Add(RtsWidgets.StatLine("owners in the store", out _owners));
            Root.Add(RtsWidgets.StatLine("observers on A's node", out _observersA));
            Root.Add(RtsWidgets.StatLine("observers on B's node", out _observersB));
            Root.Add(RtsWidgets.StatLine("dangling modifiers", out _dangling));
            Root.Add(RtsWidgets.StatLine("stats changed last tick", out _changedStats));
            Root.Add(RtsWidgets.StatLine("change events last tick", out _changeEvents));
            Root.Add(RtsWidgets.StatLine("active effects", out _effects));

            Root.Add(ButtonRow(
                  Small("node", () => _host.Player.ToggleNodeInspection()
                      , "inspect the shared bonus node every unit reads")
                , Small("hero +1", () => _host.Player.LevelUpHero(_host.Match)
                      , "one write to AuraPower — watch how many stats change")));

            Root.Add(ButtonRow(
                  Small("prune", () => _host.Player.Prune(_host.Match)
                      , "remove modifiers that reported a missing source")
                , Small("recalc", () => _host.Player.Recalculate(_host.Match)
                      , "recalculate everything through the generated Burst job")));

            _sloppy = Small("sloppy", () => _host.Player.ToggleSloppyDeaths(_host.Match)
                , "destroy owners without removing their modifiers first, and watch the leak");

            Root.Add(ButtonRow(
                  Small("cycle", () => _host.Player.TryCyclicAura(_host.Match)
                      , "try to wire the aura into Attack — the runtime refuses it")
                , _sloppy));

            Root.Add(ButtonRow(Small("cull 5 of mine", () => _host.Player.Cull(_host.Match)
                , "kill five of my own units — with sloppy off the observer count drops, with it on it does not")));
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            var match = _host.Match;
            var probe = match.Probe;

            _owners.text = probe.OwnerCount.ToString();
            _observersA.text = probe.ObserversOnNode(0).ToString();
            _observersB.text = probe.ObserversOnNode(1).ToString();
            _dangling.text = probe.DanglingModifiers.ToString();
            _changedStats.text = probe.ChangedStatsLastTick.ToString();
            _changeEvents.text = probe.ChangeEventsLastTick.ToString();
            _effects.text = match.Effects.ActiveCount.ToString();

            _dangling.style.color = probe.DanglingModifiers > 0
                ? RtsHudTheme.WarnInk
                : RtsHudTheme.Ink;

            RtsWidgets.SetHighlight(_sloppy, match.Experiments.SloppyDeaths, RtsHudTheme.WarnInk);
        }

        private static VisualElement ButtonRow(params Button[] buttons)
        {
            var row = RtsWidgets.Row();

            row.style.marginTop = 4f;

            for (var i = 0; i < buttons.Length; i++)
            {
                row.Add(buttons[i]);
            }

            return row;
        }

        private static Button Small(string text, System.Action onClick, string tooltip)
        {
            var button = RtsWidgets.Button(text, onClick);

            button.style.flexGrow = 1f;
            button.style.fontSize = 10;
            button.tooltip = tooltip;

            return button;
        }
    }
}
