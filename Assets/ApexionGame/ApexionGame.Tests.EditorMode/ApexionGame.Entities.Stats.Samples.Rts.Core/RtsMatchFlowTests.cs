using NUnit.Framework;

namespace ApexionGame.Entities.Stats.Samples.Rts.Tests
{
    /// <summary>A whole match, from the first tick to a winner.</summary>
    public sealed class RtsMatchFlowTests
    {
        [Test]
        public void SameSeedProducesTheSameMatch()
        {
            var settings = RtsMatchSettings.Default;

            Assert.AreEqual(Snapshot(settings, 400), Snapshot(settings, 400));
        }

        private static string Snapshot(RtsMatchSettings settings, int ticks)
        {
            using var match = new RtsMatch(settings);

            match.DirectorOf(0).Enabled = true;
            match.DirectorOf(1).Enabled = true;
            match.Tick(ticks);

            var a = match.TeamAt(0);
            var b = match.TeamAt(1);

            return $"{a.Units.Count}/{a.Spawned}/{a.Lost}/{match.Read(a.Stronghold.Handles.hp):0.000}"
                + $" | {b.Units.Count}/{b.Spawned}/{b.Lost}/{match.Read(b.Stronghold.Handles.hp):0.000}"
                + $" | {match.Probe.OwnerCount}";
        }

        /// <summary>
        /// One side reinforced and the other not: the match has to actually end, and end cleanly.
        /// </summary>
        [Test]
        public void AnUnansweredArmyEndsTheMatch()
        {
            var settings = RtsMatchSettings.Default;

            using var match = new RtsMatch(settings);

            match.DirectorOf(0).Enabled = false;
            match.DirectorOf(1).Enabled = true;

            var limit = (int)(420f / settings.tickSeconds);

            for (var i = 0; i < limit && match.IsOver == false; i++)
            {
                match.Tick();
            }

            Assert.IsTrue(match.IsOver, "team B never finished off an army that never reinforced");
            Assert.AreEqual(1, match.Outcome.WinnerTeam);

            // Guards against the match "ending" for the wrong reason: a stronghold born at zero health would
            // make every assertion above pass on the first tick.
            Assert.Greater(match.Elapsed, 30f, "the match ended far too early to be a battle");
            Assert.Greater(match.TeamAt(1).Spawned, 10, "team B never reinforced");
            Assert.Greater(match.TeamAt(0).Lost, 5, "team A never lost the army it started with");

            Assert.AreEqual(0, match.Probe.DanglingModifiers
                , "combat deaths left modifiers pointing at destroyed owners");
        }

        [Test]
        public void ANewMatchStartsFromNothing()
        {
            var settings = RtsTestMatch.Sandbox();

            using var first = new RtsMatch(settings);

            Assert.IsTrue(first.TrySpawn(0, RtsContent.Footman, out _));
            first.Tick(20);

            var owners = first.Probe.OwnerCount;

            using var second = new RtsMatch(settings);

            Assert.AreEqual(2 + 2, second.Probe.OwnerCount
                , "a fresh match is two team nodes and two strongholds");

            Assert.Greater(owners, second.Probe.OwnerCount);
        }

        [Test]
        public void CommandsAreRefusedOnceTheMatchIsDecided()
        {
            var settings = RtsTestMatch.Sandbox();

            using var match = new RtsMatch(settings);

            // Reduce the enemy stronghold to rubble the honest way: a lot of damage, delivered by the clock.
            match.DirectorOf(0).Enabled = true;
            match.DirectorOf(1).Enabled = false;

            var limit = (int)(600f / settings.tickSeconds);

            for (var i = 0; i < limit && match.IsOver == false; i++)
            {
                match.Tick();
            }

            Assert.IsTrue(match.IsOver);
            Assert.IsFalse(match.TrySpawn(0, RtsContent.Footman, out var rejection));
            Assert.AreEqual(RtsRejection.MatchOver, rejection);
            Assert.IsFalse(match.TryResearch(0, 0, out rejection));
            Assert.AreEqual(RtsRejection.MatchOver, rejection);
        }
    }
}
