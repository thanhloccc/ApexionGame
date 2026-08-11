using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>The last few things that happened, newest at the bottom.</summary>
    public sealed class RtsJournalPanel
    {
        private readonly IRtsHudHost _host;
        private readonly Label[] _lines = new Label[RtsHudTheme.JournalLines];

        private int _version = -1;

        public RtsJournalPanel(IRtsHudHost host)
        {
            _host = host;

            Root = RtsWidgets.Column();
            Root.style.height = RtsHudTheme.JournalHeight;
            Root.style.backgroundColor = RtsHudTheme.PanelBg;
            Root.style.borderTopWidth = 1f;
            Root.style.borderTopColor = RtsHudTheme.Edge;
            RtsWidgets.Padding(Root, 6f);

            for (var i = 0; i < _lines.Length; i++)
            {
                _lines[i] = RtsWidgets.Label(string.Empty, 10, RtsHudTheme.Ink);
                _lines[i].style.height = 13f;
                Root.Add(_lines[i]);
            }
        }

        public VisualElement Root { get; }

        /// <remarks>Rebuilt only when the journal's version moved, which is most frames doing nothing.</remarks>
        public void Refresh()
        {
            var match = _host.Match;
            var journal = match.Journal;

            if (journal.Version == _version)
            {
                return;
            }

            _version = journal.Version;

            var first = journal.Count - _lines.Length;

            if (first < 0)
            {
                first = 0;
            }

            for (var i = 0; i < _lines.Length; i++)
            {
                var index = first + i;

                if (index >= journal.Count)
                {
                    _lines[i].text = string.Empty;
                    continue;
                }

                var entry = journal[index];

                _lines[i].text = $"{RtsJournalText.TimeOf(entry)}  {RtsJournalText.Describe(entry, match)}";
                _lines[i].style.color = RtsJournalText.ColorOf(entry);
            }
        }

        /// <summary>Forces a rebuild — used when the match is replaced by a new one.</summary>
        public void Invalidate() => _version = -1;
    }
}
