namespace ApexionGame.Entities.Stats.Samples.Rts.Tests
{
    /// <summary>Match setups the tests share.</summary>
    internal static class RtsTestMatch
    {
        /// <summary>An empty field with a full wallet: for testing mechanics rather than economics.</summary>
        internal static RtsMatchSettings Sandbox()
        {
            var settings = RtsMatchSettings.Default;

            settings.startingSupply = 100_000;
            settings.supplyCap = 100_000;
            settings.startingFootmen = 0;
            settings.startingArchers = 0;

            return settings;
        }

        internal static void Tick(this RtsMatch match, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                match.Tick();
            }
        }

        /// <summary>Runs the match for a number of simulated seconds.</summary>
        internal static void TickSeconds(this RtsMatch match, float seconds)
            => match.Tick((int)(seconds / match.Settings.tickSeconds) + 1);

        /// <summary>The unit fielded most recently, which every spawn test wants.</summary>
        internal static RtsUnit Newest(this RtsTeam team) => team.Units[team.Units.Count - 1];
    }
}
