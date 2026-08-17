using Game.Gameplay.Combat;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class HealthTests
{
    private const float Full = 100f;

    [Test]
    public void Damage_RemovesExactlyWhatWasAsked()
    {
        var health = new Health(Full);

        var change = health.ApplyDamage(30f);

        Assert.That(change.Applied, Is.EqualTo(30f).Within(0.001f));
        Assert.That(change.Remaining, Is.EqualTo(70f).Within(0.001f));
        Assert.That(change.JustDied, Is.False);
        Assert.That(health.Current, Is.EqualTo(70f).Within(0.001f));
    }

    [Test]
    public void ReachingZero_ReportsDeath()
    {
        var health = new Health(Full);

        var change = health.ApplyDamage(Full);

        Assert.That(change.JustDied, Is.True);
        Assert.That(health.IsDead, Is.True);
        Assert.That(health.Current, Is.Zero);
    }

    [Test]
    public void DeathIsReportedExactlyOnce()
    {
        var health = new Health(Full);

        Assert.That(health.ApplyDamage(Full).JustDied, Is.True);

        for (var i = 0; i < 10; i++)
        {
            var change = health.ApplyDamage(Full);

            Assert.That(change.JustDied, Is.False, $"death reported again on hit {i + 2}");
            Assert.That(change.Applied, Is.Zero);
        }
    }

    [Test]
    public void Overkill_NeverDrivesHealthNegative_AndReportsOnlyWhatWasThere()
    {
        var health = new Health(10f);

        var change = health.ApplyDamage(9999f);

        Assert.That(change.Applied, Is.EqualTo(10f).Within(0.001f));
        Assert.That(change.Remaining, Is.Zero);
        Assert.That(health.Current, Is.Zero);
    }

    [Test]
    public void ZeroOrNegativeDamage_ChangesNothing()
    {
        var health = new Health(Full);

        Assert.That(health.ApplyDamage(0f).Applied, Is.Zero);
        Assert.That(health.ApplyDamage(-50f).Applied, Is.Zero);
        Assert.That(health.Current, Is.EqualTo(Full).Within(0.001f));
        Assert.That(health.IsDead, Is.False);
    }

    [Test]
    public void CreatedWithNoHealth_IsAlreadyDead()
    {
        Assert.That(new Health(0f).IsDead, Is.True);
        Assert.That(new Health(-10f).IsDead, Is.True);
        Assert.That(new Health(-10f).Current, Is.Zero);
    }
}
