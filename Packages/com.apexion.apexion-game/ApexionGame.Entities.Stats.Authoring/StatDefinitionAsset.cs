using System;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Authoring
{
    /// <summary>
    /// One authored stat: the value plus the two per-stat flags the runtime stores alongside it.
    /// </summary>
    [Serializable]
    public struct StatDefinitionEntry
    {
        [Tooltip("Identifies the stat inside the asset. Match it to a generated TypeId when applying.")]
        [SerializeField]
        private string _id;

        [SerializeField]
        private SerializableStatVariant _baseValue;

        [Tooltip("Stats that nothing observes can skip change events entirely.")]
        [SerializeField]
        private bool _produceChangeEvents;

        [SerializeField]
        private uint _userData;

        public readonly string Id => _id;

        public readonly SerializableStatVariant BaseValue => _baseValue;

        public readonly bool ProduceChangeEvents => _produceChangeEvents;

        public readonly uint UserData => _userData;
    }

    /// <summary>
    /// A named set of authored stats, meant to be turned into a live owner at runtime.
    /// </summary>
    /// <remarks>
    /// Deliberately kept free of any generated type. A stat collection is generated per
    /// <c>[StatCollection]</c>, so a strongly-typed asset would have to be generated too; this asset
    /// carries values only and the project decides how to map <see cref="StatDefinitionEntry.Id"/>
    /// onto the generated handles — usually a switch in one small factory per collection.
    /// <para>
    /// Creating the owner itself goes through the generated
    /// <c>Stats.Builder.Build(ref store)</c>, which is the only path that seeds the None stat.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(
          menuName = "ApexionGame/Stats/Stat Definition"
        , fileName = "StatDefinition"
    )]
    public sealed class StatDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private StatDefinitionEntry[] _entries = Array.Empty<StatDefinitionEntry>();

        public StatDefinitionEntry[] Entries => _entries ??= Array.Empty<StatDefinitionEntry>();

        /// <summary>
        /// Finds an entry by id.
        /// </summary>
        public bool TryGetEntry(string id, out StatDefinitionEntry entry)
        {
            var entries = Entries;

            for (var i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].Id, id, StringComparison.Ordinal))
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// Finds an entry and converts its authored value to the runtime union.
        /// </summary>
        public bool TryGetBaseValue(string id, out StatVariant value)
        {
            if (TryGetEntry(id, out var entry))
            {
                value = entry.BaseValue.ToStatVariant();
                return true;
            }

            value = default;
            return false;
        }
    }
}
