using EncosyTower.Collections;
using Game.Gameplay.Combat;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class HitQueryTests
{
    private const float Range = TestItems.KnifeRange;
    private const float Radius = TestItems.KnifeRadius;
    private const float Arc = TestItems.KnifeArcDegrees;
    private const float BodyRadius = 0.5f;

    private const float ShotRange = TestItems.RifleRange;
    private const float ShotRadius = TestItems.ProjectileRadius;

    private FasterList<CombatantId> _results;

    [SetUp]
    public void SetUp()
        => _results = new FasterList<CombatantId>(8);

    private static CombatantSpatial Attacker(byte team = 0)
        => new() {
            Id = new CombatantId(1),
            Position = float3.zero,
            Forward = math.forward(),
            Radius = BodyRadius,
            Team = team,
            IsAlive = true,
        };

    private static CombatantSpatial Target(
          int id
        , float3 position
        , byte team = 1
        , bool alive = true
    )
        => new() {
            Id = new CombatantId(id),
            Position = position,
            Forward = -math.forward(),
            Radius = BodyRadius,
            Team = team,
            IsAlive = alive,
        };

    /// <summary>A point <paramref name="degrees"/> off the attacker's facing, on the XZ plane.</summary>
    private static float3 OffAxis(float degrees, float distance)
    {
        var radians = math.radians(degrees);
        return new float3(math.sin(radians), 0f, math.cos(radians)) * distance;
    }

    // ---------- melee ----------

    [Test]
    public void Melee_HitsAnEnemyStandingInFront()
    {
        var spatials = new[] { Attacker(), Target(2, new float3(0f, 0f, 1.5f)) };

        var count = HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(_results[0], Is.EqualTo(new CombatantId(2)));
    }

    [Test]
    public void Melee_MissesAnEnemyBehind()
    {
        var spatials = new[] { Attacker(), Target(2, new float3(0f, 0f, -1.5f)) };

        Assert.That(HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results), Is.Zero);
    }

    [Test]
    public void Melee_MissesAnEnemyOutOfReach()
    {
        var spatials = new[] { Attacker(), Target(2, new float3(0f, 0f, 50f)) };

        Assert.That(HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results), Is.Zero);
    }

    [Test]
    public void Melee_RespectsTheArcBoundary()
    {
        var inside = new[] { Attacker(), Target(2, OffAxis(Arc * 0.5f - 5f, 1f)) };
        var outside = new[] { Attacker(), Target(3, OffAxis(Arc * 0.5f + 5f, 1f)) };

        Assert.That(HitQuery.Melee(inside, Attacker(), Range, Radius, Arc, _results), Is.EqualTo(1));

        _results.Clear();

        Assert.That(HitQuery.Melee(outside, Attacker(), Range, Radius, Arc, _results), Is.Zero);
    }

    [Test]
    public void Melee_IgnoresTheAttackerItself()
    {
        var spatials = new[] { Attacker() };

        Assert.That(HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results), Is.Zero);
    }

    [Test]
    public void Melee_IgnoresTeammates()
    {
        var spatials = new[] { Attacker(), Target(2, new float3(0f, 0f, 1f), team: 0) };

        Assert.That(HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results), Is.Zero);
    }

    [Test]
    public void Melee_IgnoresTheDead()
    {
        var spatials = new[] { Attacker(), Target(2, new float3(0f, 0f, 1f), alive: false) };

        Assert.That(HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results), Is.Zero);
    }

    [Test]
    public void Melee_SweepsThroughEveryEnemyInTheArc()
    {
        var spatials = new[] {
            Attacker(),
            Target(2, OffAxis(-20f, 1f)),
            Target(3, new float3(0f, 0f, 1.2f)),
            Target(4, OffAxis(20f, 1f)),
            Target(5, new float3(0f, 0f, -2f)),
        };

        Assert.That(HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results), Is.EqualTo(3));
        Assert.That(_results.AsReadOnlySpan().ToArray(), Has.No.Member(new CombatantId(5)));
    }

    [Test]
    public void Melee_HitsSomeoneStandingInsideTheAttacker()
    {
        var spatials = new[] { Attacker(), Target(2, float3.zero) };

        Assert.That(HitQuery.Melee(spatials, Attacker(), Range, Radius, Arc, _results), Is.EqualTo(1));
    }

    // ---------- ranged ----------

    [Test]
    public void Ranged_HitsAnEnemyOnTheShotLine()
    {
        var spatials = new[] { Attacker(), Target(2, new float3(0f, 0f, 10f)) };

        Assert.That(HitQuery.Ranged(spatials, Attacker(), ShotRange, ShotRadius, _results), Is.EqualTo(1));
        Assert.That(_results[0], Is.EqualTo(new CombatantId(2)));
    }

    [Test]
    public void Ranged_StopsAtTheNearestBody()
    {
        var spatials = new[] {
            Attacker(),
            Target(2, new float3(0f, 0f, 20f)),
            Target(3, new float3(0f, 0f, 5f)),
            Target(4, new float3(0f, 0f, 12f)),
        };

        Assert.That(HitQuery.Ranged(spatials, Attacker(), ShotRange, ShotRadius, _results), Is.EqualTo(1));
        Assert.That(_results[0], Is.EqualTo(new CombatantId(3)));
    }

    [Test]
    public void Ranged_MissesBehindAndBeyondRange()
    {
        var behind = new[] { Attacker(), Target(2, new float3(0f, 0f, -10f)) };
        var beyond = new[] { Attacker(), Target(3, new float3(0f, 0f, ShotRange + 10f)) };

        Assert.That(HitQuery.Ranged(behind, Attacker(), ShotRange, ShotRadius, _results), Is.Zero);
        Assert.That(HitQuery.Ranged(beyond, Attacker(), ShotRange, ShotRadius, _results), Is.Zero);
    }

    [Test]
    public void Ranged_MissesAnEnemyOffToTheSide()
    {
        var spatials = new[] { Attacker(), Target(2, new float3(5f, 0f, 10f)) };

        Assert.That(HitQuery.Ranged(spatials, Attacker(), ShotRange, ShotRadius, _results), Is.Zero);
    }

    [Test]
    public void Ranged_StillHitsWhenTheLineGrazesTheBody()
    {
        var offset = BodyRadius * 0.8f;
        var spatials = new[] { Attacker(), Target(2, new float3(offset, 0f, 10f)) };

        Assert.That(HitQuery.Ranged(spatials, Attacker(), ShotRange, ShotRadius, _results), Is.EqualTo(1));
    }

    [Test]
    public void Ranged_IgnoresTeammatesAndTheDead()
    {
        var spatials = new[] {
            Attacker(),
            Target(2, new float3(0f, 0f, 5f), team: 0),
            Target(3, new float3(0f, 0f, 8f), alive: false),
            Target(4, new float3(0f, 0f, 12f)),
        };

        Assert.That(HitQuery.Ranged(spatials, Attacker(), ShotRange, ShotRadius, _results), Is.EqualTo(1));
        Assert.That(_results[0], Is.EqualTo(new CombatantId(4)));
    }
}
