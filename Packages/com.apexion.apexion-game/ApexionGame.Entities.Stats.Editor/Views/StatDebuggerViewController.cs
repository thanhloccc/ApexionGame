using System;
using System.Collections.Generic;
using ApexionGame.Entities.Stats.Debugging;
using UnityEngine.UIElements;

namespace ApexionGame.Entities.Stats.Editor.Views
{
    /// <summary>
    /// Decides what actually needs rebuilding on each poll.
    /// </summary>
    /// <remarks>
    /// The whole point of this class. Stat values live in native memory with nothing to raise an
    /// event, so the window has to poll — but polling must not mean rebuilding. Each poll compares
    /// the <b>shape</b> of the data (which stores, which owners, which stats, which edges) against
    /// what is on screen, and only touches the tree when the shape moved. Numbers are written into
    /// existing labels every time, which is cheap.
    /// </remarks>
    public sealed class StatDebuggerViewController : IDisposable
    {
        private readonly StatDebuggerView _view;

        private readonly List<string> _storeNames = new();
        private readonly List<StatOwnerHandle> _owners = new();
        private readonly List<string> _ownerLabels = new();
        private readonly List<StatDebugInfo> _stats = new();
        private readonly List<StatObserverEdge> _edges = new();

        private readonly List<string> _lastStoreNames = new();
        private readonly List<StatOwnerHandle> _lastOwners = new();
        private readonly List<StatObserverEdge> _lastEdges = new();
        private int _lastStatCount = -1;

        // Comparing "last" against "current" cannot tell first-run from unchanged: both are empty
        // at startup, so the graph was never built and sat there blank with no empty-state text.
        private bool _topologyBuilt;
        private bool _storesBuilt;
        private bool _ownersBuilt;

        private int _storeIndex;
        private int _ownerIndex;

        public StatDebuggerViewController(StatDebuggerView view)
        {
            _view = view;
            _view.userData = this;
            _view.StoreSelected += OnStoreSelected;
            _view.OwnerSelected += OnOwnerSelected;
            _view.RefreshRequested += ForceRefresh;
            _view.AutoRefreshToggled += value => AutoRefresh = value;

            StatDebugRegistry.Changed += ForceRefresh;

            ForceRefresh();
        }

        public bool AutoRefresh { get; private set; } = true;

        public void Dispose()
        {
            _view.StoreSelected -= OnStoreSelected;
            _view.OwnerSelected -= OnOwnerSelected;
            _view.RefreshRequested -= ForceRefresh;

            StatDebugRegistry.Changed -= ForceRefresh;
        }

        /// <summary>
        /// Called from the window's poll. Cheap when nothing changed shape.
        /// </summary>
        public void Poll()
        {
            if (AutoRefresh)
            {
                Refresh();
            }
        }

        private void ForceRefresh()
        {
            _storesBuilt = false;
            _ownersBuilt = false;
            _topologyBuilt = false;
            _lastStatCount = -1;

            Refresh();
        }

        private void Refresh()
        {
            var stores = StatDebugRegistry.Stores;

            _storeNames.Clear();

            for (var i = 0; i < stores.Count; i++)
            {
                _storeNames.Add(stores[i].Name);
            }

            _storeIndex = Clamp(_storeIndex, _storeNames.Count);

            if (_storesBuilt == false || SequenceEqual(_storeNames, _lastStoreNames) == false)
            {
                _storesBuilt = true;
                CopyInto(_storeNames, _lastStoreNames);
                _view.SetStores(_storeNames, _storeIndex);
            }

            if (stores.Count == 0)
            {
                return;
            }

            var store = stores[_storeIndex];

            RefreshOwners(store);
            RefreshStats(store);
        }

        private void RefreshOwners(IStatStoreDebug store)
        {
            _owners.Clear();

            if (store.IsCreated)
            {
                store.GetOwners(_owners);
            }

            _ownerIndex = Clamp(_ownerIndex, _owners.Count);

            _view.SetInfo(store.IsCreated
                ? $"{_owners.Count} owner(s)"
                : "store disposed — the registration outlived it");

            if (_ownersBuilt && SequenceEqual(_owners, _lastOwners))
            {
                return;
            }

            _ownersBuilt = true;
            CopyInto(_owners, _lastOwners);
            _ownerLabels.Clear();

            for (var i = 0; i < _owners.Count; i++)
            {
                var owner = _owners[i];
                _ownerLabels.Add($"#{owner.Index}  v{owner.Version}");
            }

            _view.SetOwners(_ownerLabels, _owners.Count > 0 ? _ownerIndex : -1);
        }

        private void RefreshStats(IStatStoreDebug store)
        {
            _stats.Clear();
            _edges.Clear();

            if (store.IsCreated && _ownerIndex >= 0 && _ownerIndex < _owners.Count)
            {
                var owner = _owners[_ownerIndex];
                store.GetStats(owner, _stats);
                store.GetObserverEdges(owner, _edges);
            }

            if (_stats.Count != _lastStatCount)
            {
                _lastStatCount = _stats.Count;
                _view.SetStatCount(_stats.Count);
            }

            // The graph is the expensive part to rebuild, so it only happens when an edge appears
            // or disappears. Values flow through SetValues on every pass.
            if (_topologyBuilt == false || SequenceEqual(_edges, _lastEdges) == false)
            {
                _topologyBuilt = true;
                CopyInto(_edges, _lastEdges);
                _view.SetTopology(_stats, _edges);
            }

            _view.SetValues(_stats);
        }

        private void OnStoreSelected(int index)
        {
            if (index < 0 || index == _storeIndex)
            {
                return;
            }

            _storeIndex = index;
            _ownerIndex = 0;
            ForceRefresh();
        }

        private void OnOwnerSelected(int index)
        {
            if (index < 0 || index == _ownerIndex)
            {
                return;
            }

            _ownerIndex = index;

            // Owners did not change, only which one is shown — rebuild the stats side alone so the
            // selection the user just made is not written over.
            _topologyBuilt = false;
            _lastStatCount = -1;

            var stores = StatDebugRegistry.Stores;

            if (_storeIndex < stores.Count)
            {
                RefreshStats(stores[_storeIndex]);
            }
        }

        private static int Clamp(int index, int count)
            => count <= 0 ? 0 : Math.Clamp(index, 0, count - 1);

        private static bool SequenceEqual<T>(List<T> a, List<T> b) where T : IEquatable<T>
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (a[i].Equals(b[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SequenceEqual(List<string> a, List<string> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (string.Equals(a[i], b[i], StringComparison.Ordinal) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CopyInto<T>(List<T> source, List<T> destination)
        {
            destination.Clear();
            destination.AddRange(source);
        }
    }

    public static class StatDebuggerAPI
    {
        public static StatDebuggerViewController CreateView(VisualElement root)
        {
            var view = new StatDebuggerView();
            root.Add(view);

            return new StatDebuggerViewController(view);
        }
    }
}
