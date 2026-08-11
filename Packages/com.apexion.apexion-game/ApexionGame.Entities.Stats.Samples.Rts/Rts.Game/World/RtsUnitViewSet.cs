using EncosyTower.Collections;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>Keeps one <see cref="RtsUnitView"/> alive per living unit, and not one more.</summary>
    public sealed class RtsUnitViewSet
    {
        private readonly Transform _root;
        private readonly ArrayMap<int, RtsUnitView> _views = new(128);
        private readonly ArraySet<int> _seen = new(128);
        private readonly FasterList<int> _gone = new(32);

        public RtsUnitViewSet(Transform parent)
        {
            _root = new GameObject("units").transform;
            _root.SetParent(parent, false);
        }

        /// <summary>
        /// Hands the simulation's battle events to the views that care, then empties the feed.
        /// </summary>
        /// <remarks>
        /// A frame can contain several ticks, so this runs once per frame rather than once per tick — and the
        /// feed is cleared here, by the only consumer, rather than by the simulation that filled it.
        /// </remarks>
        public void Consume(RtsBattleFeed feed)
        {
            var events = feed.Events;

            for (var i = 0; i < events.Length; i++)
            {
                var battleEvent = events[i];

                if (_views.TryGetValue(battleEvent.UnitId, out var view))
                {
                    view.Pulse(battleEvent.Kind);
                }
            }

            feed.Clear();
        }

        public void Sync(RtsMatch match, float alpha, float deltaTime, RtsUnit selected, Quaternion facing)
        {
            _seen.Clear();

            var teams = match.Teams;

            for (var t = 0; t < teams.Count; t++)
            {
                var units = teams[t].Roster.AsReadOnlySpan();

                for (var i = 0; i < units.Length; i++)
                {
                    var unit = units[i];

                    _seen.Add(unit.Id);

                    if (_views.TryGetValue(unit.Id, out var view) == false)
                    {
                        view = new RtsUnitView(_root, unit, RtsTeamColors.Of(unit.TeamIndex));
                        _views.Add(unit.Id, view);
                    }

                    view.Sync(match, unit, alpha, deltaTime, unit == selected, facing);
                }
            }

            Retire();
        }

        private void Retire()
        {
            _gone.Clear();

            foreach (var pair in _views)
            {
                if (_seen.Contains(pair.Key) == false)
                {
                    _gone.Add(pair.Key);
                }
            }

            var gone = _gone.AsReadOnlySpan();

            for (var i = 0; i < gone.Length; i++)
            {
                if (_views.TryGetValue(gone[i], out var view))
                {
                    view.Dispose();
                    _views.Remove(gone[i]);
                }
            }
        }

        public void Clear()
        {
            foreach (var pair in _views)
            {
                pair.Value.Dispose();
            }

            _views.Clear();
        }

        public void Dispose()
        {
            Clear();
            _views.Dispose();

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }
    }
}
