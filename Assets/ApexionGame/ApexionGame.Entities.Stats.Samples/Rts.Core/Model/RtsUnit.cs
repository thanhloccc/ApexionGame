using System;
using EncosyTower.Collections;
using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// One unit while it lives: identity, position, and the handles that tie it into the stat graph.
    /// </summary>
    /// <remarks>
    /// Nothing here belongs to the view — no flash timers, no colours, no counters kept for a label. The
    /// presentation layer derives what it needs from <see cref="RtsBattleFeed"/> and from the store.
    /// <para>
    /// <see cref="Links"/> is the half people forget. A modifier can only be removed through the handle its
    /// insertion returned, so a unit that does not keep its handles cannot be taken out of the graph — see
    /// <see cref="RtsDeathSystem"/>.
    /// </para>
    /// </remarks>
    public sealed class RtsUnit
    {
        /// <summary>The three team links, the Hp cap and the interval floor. Five, always.</summary>
        private readonly FasterList<StatModifierHandle> _links = new(8);

        /// <summary>Heroes only: the terms this unit contributes to its team's node.</summary>
        private readonly FasterList<StatModifierHandle> _auraTerms = new(4);

        public readonly int Id;
        public readonly int TeamIndex;
        public readonly RtsUnitArchetype Archetype;
        public readonly StatOwnerHandle Owner;
        public readonly UnitStats Stats;

        /// <summary>
        /// A readonly field rather than a property: it is read several times per tick, and a property would
        /// copy the whole handle set on every access.
        /// </summary>
        public readonly UnitStats.StatHandles Handles;

        // ---- state the systems own -------------------------------------------------------------

        public float2 Position;

        /// <summary>Where the unit was at the previous tick. The view interpolates from here.</summary>
        public float2 PreviousPosition;

        public float Cooldown;
        public bool Alive = true;
        public int Kills;
        public int Level = 1;

        /// <summary>
        /// What this unit is walking at or swinging at. Chosen by <see cref="RtsMovementSystem"/> and used by
        /// <see cref="RtsCombatSystem"/> — one decision per tick, not two that could disagree.
        /// </summary>
        public RtsUnit Target;

        /// <summary>Who hit this unit last. Kill credit for hero levelling, nothing more.</summary>
        public RtsUnit LastAttacker;

        public RtsUnit(
              int id
            , int teamIndex
            , RtsUnitArchetype archetype
            , in StatOwnerHandle owner
            , in UnitStats stats
            , float2 position
        )
        {
            Id = id;
            TeamIndex = teamIndex;
            Archetype = archetype;
            Owner = owner;
            Stats = stats;
            Handles = stats.GetStatHandles(owner);
            Position = position;
            PreviousPosition = position;
        }

        public bool IsHero => Archetype.IsHero;

        public bool IsStructure => Archetype.IsStructure;

        public string Label => Archetype.IsStructure ? Archetype.Name : $"{Archetype.Name} #{Id}";

        // ---- graph bookkeeping -----------------------------------------------------------------

        /// <summary>Modifiers this unit owns on its own stats, including the links that observe the node.</summary>
        public ReadOnlySpan<StatModifierHandle> Links => _links.AsReadOnlySpan();

        /// <summary>Modifiers this unit put on <b>another</b> owner: the team node.</summary>
        public ReadOnlySpan<StatModifierHandle> AuraTerms => _auraTerms.AsReadOnlySpan();

        public void AddLink(in StatModifierHandle handle) => _links.Add(handle);

        public void AddAuraTerm(in StatModifierHandle handle) => _auraTerms.Add(handle);

        /// <summary>Drops the handles after they have been removed from the store.</summary>
        public void ForgetGraphLinks()
        {
            _links.Clear();
            _auraTerms.Clear();
        }

        /// <summary>Drops one handle that something else has already removed — a prune, usually.</summary>
        public void Forget(in StatModifierHandle handle)
        {
            _links.Remove(handle);
            _auraTerms.Remove(handle);
        }
    }
}
