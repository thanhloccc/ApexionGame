namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Read-only measurements of the stat graph.
    /// </summary>
    /// <remarks>
    /// Not scaffolding to delete before shipping. An observer leak is invisible until a fight gets big, so a
    /// project that ships an RTS wants these numbers on screen during development — which is exactly what the
    /// HUD's graph panel does with them.
    /// </remarks>
    public sealed class RtsGraphProbe
    {
        private readonly RtsMatchContext _context;

        public RtsGraphProbe(RtsMatchContext context)
        {
            _context = context;
        }

        public int OwnerCount => _context.World.OwnerCount;

        public int DanglingModifiers => _context.World.DanglingCount;

        /// <summary>Distinct stats touched by the last tick's propagation.</summary>
        public int ChangedStatsLastTick { get; private set; }

        /// <summary>
        /// Raw change events from the last tick.
        /// </summary>
        /// <remarks>
        /// Reported next to <see cref="ChangedStatsLastTick"/> because the two differ, and the difference is
        /// the point: one write can emit several events for the same stat where DAG branches of unequal length
        /// meet. Gameplay driven off event <i>count</i> will misfire.
        /// </remarks>
        public int ChangeEventsLastTick { get; private set; }

        internal void RecordTick(int changedStats, int rawEvents)
        {
            ChangedStatsLastTick = changedStats;
            ChangeEventsLastTick = rawEvents;
        }

        /// <summary>Modifiers a unit carries on its own stats: five for a fresh unit, plus one per buff term.</summary>
        public int ModifiersOn(RtsUnit unit)
        {
            var world = _context.World;
            var handles = unit.Handles;

            return world.ModifierCount(handles.hp)
                + world.ModifierCount(handles.maxHp)
                + world.ModifierCount(handles.attack)
                + world.ModifierCount(handles.armor)
                + world.ModifierCount(handles.moveSpeed)
                + world.ModifierCount(handles.attackInterval)
                + world.ModifierCount(handles.attackRange)
                + world.ModifierCount(handles.auraPower);
        }

        /// <summary>
        /// Reverse edges on a team's node: three per living unit, plus its hero's aura terms.
        /// </summary>
        /// <remarks>
        /// The number to watch. With clean deaths it tracks the army; with sloppy deaths on it only ever grows.
        /// </remarks>
        public int ObserversOnNode(int teamIndex)
        {
            var world = _context.World;
            var node = _context.TeamAt(teamIndex).Handles;

            return world.ObserverCount(node.attackBonus)
                + world.ObserverCount(node.armorBonus)
                + world.ObserverCount(node.moveSpeedBonus);
        }

        /// <summary>Aura terms currently sitting on a team's node.</summary>
        public int ModifiersOnNode(int teamIndex)
        {
            var world = _context.World;
            var node = _context.TeamAt(teamIndex).Handles;

            return world.ModifierCount(node.attackBonus)
                + world.ModifierCount(node.armorBonus)
                + world.ModifierCount(node.moveSpeedBonus);
        }
    }
}
