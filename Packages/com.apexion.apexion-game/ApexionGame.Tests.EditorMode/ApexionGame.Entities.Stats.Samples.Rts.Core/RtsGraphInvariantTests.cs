using NUnit.Framework;

namespace ApexionGame.Entities.Stats.Samples.Rts.Tests
{
    /// <summary>
    /// The properties the shared team node exists to buy, and the ones a wrong death path would break.
    /// </summary>
    public sealed class RtsGraphInvariantTests
    {
        private RtsMatch _match;

        [SetUp]
        public void SetUp() => _match = new RtsMatch(RtsTestMatch.Sandbox());

        [TearDown]
        public void TearDown()
        {
            _match?.Dispose();
            _match = null;
        }

        /// <summary>Five modifiers per unit, whatever else is going on.</summary>
        [Test]
        public void EveryUnitCostsFiveModifiers()
        {
            for (var i = 0; i < 20; i++)
            {
                Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out var rejection), rejection.ToString());
            }

            Assert.IsTrue(_match.TryResearch(0, 0, out _));
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Warchief, out _));

            var units = _match.TeamAt(0).Roster.AsReadOnlySpan();

            for (var i = 0; i < units.Length; i++)
            {
                Assert.AreEqual(5, _match.Probe.ModifiersOn(units[i])
                    , $"{units[i].Label} carries the wrong number of modifiers");
            }
        }

        [Test]
        public void ResearchReachesUnitsThatSpawnLater()
        {
            Assert.IsTrue(_match.TryResearch(0, 0, out _));
            Assert.IsTrue(_match.TryResearch(0, 0, out _));
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));

            var unit = _match.TeamAt(0).Newest();
            var expected = RtsContent.Footman.Attack + RtsContent.WeaponSmithing.AmountPerLevel * 2f;

            Assert.AreEqual(expected, _match.Read(unit.Handles.attack), 0.001f);
        }

        [Test]
        public void HeroAuraReachesUnitsThatSpawnLater()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Warchief, out _));

            var hero = _match.TeamAt(0).Hero;

            Assert.IsNotNull(hero);
            Assert.IsTrue(_match.TryLevelUpHero(0, out _));
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));

            var unit = _match.TeamAt(0).Newest();
            var aura = _match.Read(hero.Handles.auraPower);
            var expected = RtsContent.Footman.Attack + aura * RtsContent.Warchief.Aura.AttackFraction;

            Assert.AreEqual(expected, _match.Read(unit.Handles.attack), 0.001f);
        }

        [Test]
        public void CleanDeathsLeaveNoObserversBehind()
        {
            // Only the stronghold so far, which links three stats like every other unit.
            var baseline = _match.Probe.ObserversOnNode(0);

            Assert.AreEqual(3, baseline);

            for (var i = 0; i < 12; i++)
            {
                Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));
            }

            Assert.AreEqual(baseline + 12 * 3, _match.Probe.ObserversOnNode(0));
            Assert.AreEqual(12, _match.Experiments.Cull(0, 12));

            Assert.AreEqual(baseline, _match.Probe.ObserversOnNode(0)
                , "a clean death has to take the unit's observer entries with it");
        }

        /// <summary>
        /// The counter-test. Without it, the warning in the README is an unverified claim.
        /// </summary>
        [Test]
        public void SloppyDeathsLeakObservers()
        {
            _match.Experiments.SloppyDeaths = true;

            for (var i = 0; i < 8; i++)
            {
                Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));
            }

            var before = _match.Probe.ObserversOnNode(0);

            Assert.AreEqual(8, _match.Experiments.Cull(0, 8));

            Assert.AreEqual(before, _match.Probe.ObserversOnNode(0)
                , "destroying an owner does not remove the observer entries its modifiers created");
        }

        [Test]
        public void HeroDeathTakesItsAuraOffTheNode()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Warchief, out _));

            var unit = _match.TeamAt(0).Units[1];
            var buffed = _match.Read(unit.Handles.attack);

            Assert.Greater(buffed, RtsContent.Footman.Attack);

            // The hero is the newest unit, and Cull takes the newest first.
            Assert.AreEqual(1, _match.Experiments.Cull(0, 1));

            Assert.IsNull(_match.TeamAt(0).Hero);
            Assert.AreEqual(RtsContent.Footman.Attack, _match.Read(unit.Handles.attack), 0.001f);
            Assert.AreEqual(0, _match.Probe.ModifiersOnNode(0));
        }

        [Test]
        public void ACyclicAuraIsRefusedAndChangesNothing()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Warchief, out _));

            var before = _match.Probe.ModifiersOnNode(0);

            Assert.IsTrue(_match.Experiments.TryCyclicAura(0), "the runtime accepted a cycle");
            Assert.AreEqual(before, _match.Probe.ModifiersOnNode(0));
        }

        [Test]
        public void RecalculatingEverythingChangesNothingThatWasAlreadyCorrect()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));
            Assert.IsTrue(_match.TryResearch(0, 0, out _));

            var unit = _match.TeamAt(0).Newest();
            var attack = _match.Read(unit.Handles.attack);

            Assert.Greater(_match.Experiments.RecalculateEverything(), 0);
            Assert.AreEqual(attack, _match.Read(unit.Handles.attack), 0.001f);
        }
    }
}
