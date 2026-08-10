using System;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// One upgrade line. Buying a level is a single write to a team node's <b>base</b> value.
    /// </summary>
    /// <remarks>
    /// The line this sample exists to produce: one <c>TrySetStatBaseValue</c> and every unit on the team —
    /// however many, whatever archetype, including ones not spawned yet — has the right Attack. There is no
    /// army-wide loop anywhere in this project.
    /// </remarks>
    public sealed class RtsResearch
    {
        private readonly int[] _costs;

        public RtsResearch(string name, TeamStats.Type target, float amountPerLevel, int[] costs)
        {
            Name = name;
            Target = target;
            AmountPerLevel = amountPerLevel;
            _costs = costs ?? Array.Empty<int>();
        }

        public string Name { get; }

        public TeamStats.Type Target { get; }

        /// <summary>Added to the node's base value per level.</summary>
        public float AmountPerLevel { get; }

        public int MaxLevel => _costs.Length;

        /// <summary>Supply cost of the next level, or -1 when the line is maxed.</summary>
        public int CostOfLevel(int currentLevel)
            => currentLevel >= 0 && currentLevel < _costs.Length ? _costs[currentLevel] : -1;

        public override string ToString() => Name;
    }
}
