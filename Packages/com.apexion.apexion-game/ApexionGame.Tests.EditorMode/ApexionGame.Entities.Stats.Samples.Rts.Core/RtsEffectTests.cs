using NUnit.Framework;

namespace ApexionGame.Entities.Stats.Samples.Rts.Tests
{
    /// <summary>
    /// Timed effects: the part the runtime does not do, so the part most likely to leak.
    /// </summary>
    public sealed class RtsEffectTests
    {
        private const int Bloodlust = 0;
        private const int Plague = 4;
        private const int Rally = 5;

        private RtsMatch _match;

        [SetUp]
        public void SetUp() => _match = new RtsMatch(RtsTestMatch.Sandbox());

        [TearDown]
        public void TearDown()
        {
            _match?.Dispose();
            _match = null;
        }

        [Test]
        public void ExpiringEffectsGiveTheirModifiersBack()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));

            var unit = _match.TeamAt(0).Newest();

            Assert.AreEqual(5, _match.Probe.ModifiersOn(unit));
            Assert.IsTrue(_match.TryCast(0, Bloodlust, unit.Position, out var rejection), rejection.ToString());

            // Bloodlust is two terms, so two more modifiers on this unit and nothing else.
            Assert.AreEqual(7, _match.Probe.ModifiersOn(unit));
            Assert.AreEqual(1, _match.Effects.ActiveCount);
            Assert.Less(_match.Read(unit.Handles.attackInterval), RtsContent.Footman.AttackInterval);

            _match.TickSeconds(RtsContent.Bloodlust.Duration + 0.2f);

            Assert.AreEqual(0, _match.Effects.ActiveCount);
            Assert.AreEqual(5, _match.Probe.ModifiersOn(unit));

            Assert.AreEqual(RtsContent.Footman.AttackInterval
                , _match.Read(unit.Handles.attackInterval), 0.001f);
        }

        /// <remarks>
        /// Recasting through the ordinary path, cooldown included — no reaching into the team's cooldown array
        /// to make the test convenient.
        /// </remarks>
        [Test]
        public void RecastingRefreshesInsteadOfStacking()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));

            var unit = _match.TeamAt(0).Newest();

            Assert.IsTrue(_match.TryCast(0, Bloodlust, unit.Position, out _));

            var once = _match.Read(unit.Handles.attack);

            // Wait out the cooldown but not the duration, then cast again on the same unit.
            _match.TickSeconds(RtsContent.Bloodlust.Cooldown - RtsContent.Bloodlust.Duration * 0.5f);

            Assert.IsTrue(_match.CanCast(0, Bloodlust));
            Assert.IsTrue(_match.TryCast(0, Bloodlust, unit.Position, out _));

            Assert.AreEqual(once, _match.Read(unit.Handles.attack), 0.001f);
            Assert.AreEqual(7, _match.Probe.ModifiersOn(unit));
            Assert.AreEqual(1, _match.Effects.ActiveCount);
        }

        [Test]
        public void HealingCannotExceedMaxHp()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));

            var unit = _match.TeamAt(0).Newest();

            // Rally heals 200 into a unit that is already full. The ClampMax modifier is the only thing
            // stopping it, and nothing in the spell code knows MaxHp exists.
            Assert.IsTrue(_match.TryCast(0, Rally, unit.Position, out var rejection), rejection.ToString());
            Assert.AreEqual(RtsContent.Footman.MaxHp, _match.Read(unit.Handles.hp), 0.001f);
        }

        [Test]
        public void PlagueDamagesOverTime()
        {
            Assert.IsTrue(_match.TrySpawn(1, RtsContent.Footman, out _));

            var victim = _match.TeamAt(1).Newest();
            var full = _match.Read(victim.Handles.hp);

            Assert.IsTrue(_match.TryCast(0, Plague, victim.Position, out var rejection), rejection.ToString());

            _match.TickSeconds(2f);

            Assert.Less(_match.Read(victim.Handles.hp), full);
        }

        [Test]
        public void ASpellWithNothingInRangeIsRefusedAndCostsNothing()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));

            var supply = _match.TeamAt(0).Supply;
            var far = new Unity.Mathematics.float2(0f, 0f);

            Assert.IsFalse(_match.TryCast(0, Bloodlust, far, out var rejection));
            Assert.AreEqual(RtsRejection.NothingInRange, rejection);
            Assert.AreEqual(supply, _match.TeamAt(0).Supply, 0.001f);
            Assert.IsTrue(_match.CanCast(0, Bloodlust), "a refused cast must not start a cooldown");
        }

        [Test]
        public void EffectsDieWithTheirUnitWithoutThrowing()
        {
            Assert.IsTrue(_match.TrySpawn(0, RtsContent.Footman, out _));

            var unit = _match.TeamAt(0).Newest();

            Assert.IsTrue(_match.TryCast(0, Bloodlust, unit.Position, out _));
            Assert.AreEqual(1, _match.Effects.ActiveCount);

            _match.Experiments.Cull(0, 1);

            Assert.AreEqual(0, _match.Effects.ActiveCount);

            _match.TickSeconds(RtsContent.Bloodlust.Duration + 0.2f);

            Assert.AreEqual(0, _match.Probe.DanglingModifiers
                , "a buff on a destroyed owner observes nothing, so it cannot dangle");
        }
    }
}
