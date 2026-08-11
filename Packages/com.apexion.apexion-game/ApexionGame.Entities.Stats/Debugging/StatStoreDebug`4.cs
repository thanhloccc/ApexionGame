using System;
using System.Collections.Generic;

namespace ApexionGame.Entities.Stats.Debugging
{
    /// <summary>
    /// Erases a store's type parameters so tooling can read it.
    /// </summary>
    /// <remarks>
    /// Holds a copy of the store struct, which is only a set of pointers — reads go to the same
    /// memory the owner is writing, so what tooling shows is live, not a snapshot taken at
    /// registration time. It does <b>not</b> own the store and never disposes it.
    /// </remarks>
    /// <example>
    /// <code>
    /// _debug = new StatStoreDebug&lt;
    ///     RpgStatSystem.ValuePair, RpgStatSystem.Stat
    ///     , RpgStatSystem.StatModifier, RpgStatSystem.StatObserver
    /// &gt;("battle", _store);
    ///
    /// StatDebugRegistry.Register(_debug);
    /// // ... and StatDebugRegistry.Unregister(_debug) before the store is disposed.
    /// </code>
    /// </example>
    public sealed class StatStoreDebug<TValuePair, TStat, TStatModifier, TStatObserver> : IStatStoreDebug
        where TValuePair : unmanaged, IStatValuePair
        where TStat : unmanaged, IStat<TValuePair>
        where TStatModifier : unmanaged
        where TStatObserver : unmanaged, IStatObserver
    {
        private StatStore<TStat, TStatModifier, TStatObserver> _store;
        private readonly Func<StatOwnerHandle, int, string> _nameLookup;

        /// <remarks>
        /// <paramref name="nameLookup"/> is per-owner, not per-store: one store's owners can each
        /// come from a different <c>[StatCollection]</c> (a hero built by <c>RpgStats.Builder</c>
        /// and an aura built by <c>AuraStats.Builder</c> can share one <see cref="StatStore{TStat,
        /// TStatModifier, TStatObserver}"/>), and a bare <see cref="StatHandle"/> carries no trace
        /// of which one built a given owner. Only the caller that built each owner knows — pass a
        /// lookup like <c>(owner, index) => owner == _heroOwner ? ((RpgStats.Type)index).ToString()
        /// : ((AuraStats.Type)index).ToString()</c>. Left null, stats show as "#N".
        /// </remarks>
        public StatStoreDebug(
              string name
            , StatStore<TStat, TStatModifier, TStatObserver> store
            , Func<StatOwnerHandle, int, string> nameLookup = null
        )
        {
            Name = string.IsNullOrEmpty(name) ? "stat store" : name;
            _store = store;
            _nameLookup = nameLookup;
        }

        public string Name { get; }

        public bool IsCreated => _store.IsCreated;

        public int OwnerCount => _store.IsCreated ? _store.OwnerCount : 0;

        public void GetOwners(List<StatOwnerHandle> result)
        {
            if (_store.IsCreated == false)
            {
                return;
            }

            var capacity = _store.Capacity;

            for (var i = 0; i < capacity; i++)
            {
                if (_store.TryGetOwnerAt(i, out var owner))
                {
                    result.Add(owner);
                }
            }
        }

        public void GetStats(StatOwnerHandle owner, List<StatDebugInfo> result)
        {
            if (_store.IsCreated == false
                || _store.TryGetStats(owner, out var stats) == false
            )
            {
                return;
            }

            // Index 0 is the None stat (D-102) and is not a real stat — never show it.
            for (var i = 1; i < stats.Length; i++)
            {
                ref readonly var stat = ref stats.ElementAt(i);

                result.Add(new StatDebugInfo(
                      new StatHandle(owner, i)
                    , stat.GetBaseValueOrDefault()
                    , stat.GetCurrentValueOrDefault()
                    , stat.ModifierRange.count
                    , stat.ObserverRange.count
                    , stat.UserData
                    , stat.ProduceChangeEvents
                    , _nameLookup?.Invoke(owner, i)
                ));
            }
        }

        public void GetObserverEdges(StatOwnerHandle owner, List<StatObserverEdge> result)
        {
            if (_store.IsCreated == false
                || _store.TryGetStats(owner, out var stats) == false
                || _store.TryGetObservers(owner, out var observers) == false
            )
            {
                return;
            }

            for (var i = 1; i < stats.Length; i++)
            {
                ref readonly var stat = ref stats.ElementAt(i);

                var range = stat.ObserverRange;
                var start = range.startIndex;
                var end = start + range.count;
                var observed = new StatHandle(owner, i);

                for (var o = start; o < end && o < observers.Length; o++)
                {
                    result.Add(new StatObserverEdge(observed, observers[o].ObserverHandle));
                }
            }
        }
    }
}
