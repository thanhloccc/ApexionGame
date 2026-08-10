using System;

namespace ApexionGame.Entities.Stats.Debugging
{
    /// <summary>
    /// A snapshot of one stat, flattened so tooling never needs the six type parameters.
    /// </summary>
    public readonly struct StatDebugInfo
    {
        public readonly StatHandle handle;
        public readonly StatVariant baseValue;
        public readonly StatVariant currentValue;
        public readonly int modifierCount;
        public readonly int observerCount;
        public readonly uint userData;
        public readonly bool produceChangeEvents;

        /// <summary>
        /// The stat's generated <c>Type</c> enum name (e.g. "Hp"), or <see cref="string.Empty"/>
        /// when whoever registered the store did not supply a name lookup. A store cannot resolve
        /// this itself — <see cref="StatHandle"/> is only an owner + a buffer index, with nothing
        /// saying which <c>[StatCollection]</c> built that owner.
        /// </summary>
        public readonly string name;

        public StatDebugInfo(
              StatHandle handle
            , in StatVariant baseValue
            , in StatVariant currentValue
            , int modifierCount
            , int observerCount
            , uint userData
            , bool produceChangeEvents
            , string name = ""
        )
        {
            this.handle = handle;
            this.baseValue = baseValue;
            this.currentValue = currentValue;
            this.modifierCount = modifierCount;
            this.observerCount = observerCount;
            this.userData = userData;
            this.produceChangeEvents = produceChangeEvents;
            this.name = name ?? string.Empty;
        }

        /// <summary>
        /// True when modifiers moved the value away from what was authored.
        /// </summary>
        public bool IsModified
            => modifierCount > 0 && baseValue.Equals(currentValue) == false;
    }

    /// <summary>
    /// One observer edge: <see cref="observed"/> changing forces <see cref="observer"/> to
    /// recalculate.
    /// </summary>
    public readonly struct StatObserverEdge : IEquatable<StatObserverEdge>
    {
        public readonly StatHandle observed;
        public readonly StatHandle observer;

        public StatObserverEdge(StatHandle observed, StatHandle observer)
        {
            this.observed = observed;
            this.observer = observer;
        }

        public bool Equals(StatObserverEdge other)
            => observed == other.observed && observer == other.observer;

        public override bool Equals(object obj)
            => obj is StatObserverEdge other && Equals(other);

        public override int GetHashCode()
            => (observed.GetHashCode() * 397) ^ observer.GetHashCode();

        public override string ToString()
            => $"{observed} -> {observer}";
    }
}
