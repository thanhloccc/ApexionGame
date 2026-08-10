namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Supply, and research — which is to say: the one write this whole sample is built to show off.
    /// </summary>
    public sealed class RtsEconomySystem
    {
        private readonly RtsMatchContext _context;

        public RtsEconomySystem(RtsMatchContext context)
        {
            _context = context;
        }

        public void Tick(float deltaTime)
        {
            var teams = _context.Teams;
            var settings = _context.Settings;

            for (var t = 0; t < teams.Length; t++)
            {
                teams[t].Earn(settings.supplyPerSecond * deltaTime, settings.supplyCap);
            }
        }

        public int CostOf(RtsTeam team, int researchIndex)
            => RtsContent.Researches[researchIndex].CostOfLevel(team.LevelOf(researchIndex));

        public RtsRejection Validate(RtsTeam team, int researchIndex)
        {
            if (_context.Outcome.IsDecided)
            {
                return RtsRejection.MatchOver;
            }

            if (researchIndex < 0 || researchIndex >= RtsContent.Researches.Count)
            {
                return RtsRejection.NoSuchThing;
            }

            var cost = CostOf(team, researchIndex);

            if (cost < 0)
            {
                return RtsRejection.ResearchMaxed;
            }

            return team.CanAfford(cost) ? RtsRejection.None : RtsRejection.NotEnoughSupply;
        }

        /// <summary>
        /// Buys one level of research: a single write to a team node's <b>base</b> value.
        /// </summary>
        /// <remarks>
        /// One <c>TrySetStatBaseValue</c>, and every unit on the team — however many, whatever archetype,
        /// including ones that spawn minutes later — has the right Attack. There is no army-wide loop anywhere
        /// in this project, and the journal entry carries the number of stats that changed so you can watch it
        /// happen.
        /// </remarks>
        public bool TryBuy(RtsTeam team, int researchIndex, out RtsRejection rejection)
        {
            rejection = Validate(team, researchIndex);

            if (rejection != RtsRejection.None)
            {
                return false;
            }

            var research = RtsContent.Researches[researchIndex];
            var world = _context.World;
            var node = team.Handles.Of(research.Target);

            team.TrySpend(CostOf(team, researchIndex));
            team.RaiseLevel(researchIndex);

            var attackBefore = _context.SampleArmyAttack(team);

            world.ClearChangeEvents();
            world.Write(node, world.ReadBase(node) + research.AmountPerLevel);

            _context.Journal.ResearchBought(
                  team.Index
                , research.Name
                , team.LevelOf(researchIndex)
                , world.DrainChangedStats()
                , attackBefore
                , _context.SampleArmyAttack(team));

            return true;
        }
    }
}
