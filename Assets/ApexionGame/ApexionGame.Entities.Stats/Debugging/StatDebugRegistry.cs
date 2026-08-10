using System;
using System.Collections.Generic;

namespace ApexionGame.Entities.Stats.Debugging
{
    /// <summary>
    /// A live <see cref="StatStore{TStat, TStatModifier, TStatObserver}"/>, seen without its type
    /// parameters.
    /// </summary>
    public interface IStatStoreDebug
    {
        string Name { get; }

        bool IsCreated { get; }

        int OwnerCount { get; }

        /// <summary>
        /// Appends every live owner. Does not clear <paramref name="result"/>.
        /// </summary>
        void GetOwners(List<StatOwnerHandle> result);

        /// <summary>
        /// Appends every stat of one owner, skipping the None stat at index 0.
        /// Does not clear <paramref name="result"/>.
        /// </summary>
        void GetStats(StatOwnerHandle owner, List<StatDebugInfo> result);

        /// <summary>
        /// Appends every observer edge leaving the stats of one owner.
        /// Does not clear <paramref name="result"/>.
        /// </summary>
        void GetObserverEdges(StatOwnerHandle owner, List<StatObserverEdge> result);
    }

    /// <summary>
    /// Where live stores announce themselves so editor tooling can find them.
    /// </summary>
    /// <remarks>
    /// A <see cref="StatStore{TStat, TStatModifier, TStatObserver}"/> is native memory owned by
    /// whoever created it — there is no world, no singleton, nothing for a window to walk. So a
    /// store that wants to be inspectable has to say so.
    /// <para>
    /// Registration is opt-in and costs nothing when unused: no store registers itself, and
    /// nothing in the runtime reads this registry.
    /// </para>
    /// </remarks>
    public static class StatDebugRegistry
    {
        private static readonly List<IStatStoreDebug> s_stores = new();

        /// <summary>
        /// Raised when a store is registered or unregistered, so a window can refresh its list.
        /// </summary>
        public static event Action Changed;

        public static IReadOnlyList<IStatStoreDebug> Stores => s_stores;

        public static void Register(IStatStoreDebug store)
        {
            if (store == null || s_stores.Contains(store))
            {
                return;
            }

            s_stores.Add(store);
            Changed?.Invoke();
        }

        public static void Unregister(IStatStoreDebug store)
        {
            if (store == null || s_stores.Remove(store) == false)
            {
                return;
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Drops every registration. Domain reload does this for free; call it manually only when
        /// you know a batch of stores died without unregistering.
        /// </summary>
        public static void Clear()
        {
            if (s_stores.Count == 0)
            {
                return;
            }

            s_stores.Clear();
            Changed?.Invoke();
        }
    }
}
