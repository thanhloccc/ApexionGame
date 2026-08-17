using Game.Gameplay.Combat;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class DamageResolverTests
{
    private const float Raw = 100f;

    private static DamageInfo Hit(float raw = Raw, float multiplier = 1f, float penetration = 0f)
        => new(new CombatantId(1), TestItems.Rifle, raw, multiplier, penetration);

    [Test]
    public void NoArmor_TakesTheFullDamage()
    {
        Assert.That(DamageResolver.Resolve(Hit(), 0f), Is.EqualTo(Raw).Within(0.001f));
    }

    [Test]
    public void ArmorEqualToTheConstant_HalvesTheDamage()
    {
        var final = DamageResolver.Resolve(Hit(), DamageResolver.ARMOR_CONSTANT);

        Assert.That(final, Is.EqualTo(Raw * 0.5f).Within(0.001f));
    }

    [Test]
    public void ArmorNeverProducesNegativeDamage()
    {
        var final = DamageResolver.Resolve(Hit(raw: 5f), targetArmor: 100_000f);

        Assert.That(final, Is.GreaterThan(0f));
        Assert.That(final, Is.EqualTo(DamageResolver.MIN_DAMAGE).Within(0.001f));
    }

    [Test]
    public void MoreArmorAlwaysMitigatesMore_WithNoThreshold()
    {
        var previous = float.MaxValue;

        for (var armor = 0f; armor <= 2000f; armor += 50f)
        {
            var final = DamageResolver.Resolve(Hit(), armor);

            Assert.That(final, Is.LessThanOrEqualTo(previous), $"armor {armor} did not mitigate further");

            previous = final;
        }
    }

    [Test]
    public void FullPenetration_IgnoresArmorEntirely()
    {
        var final = DamageResolver.Resolve(Hit(penetration: 1f), targetArmor: 500f);

        Assert.That(final, Is.EqualTo(Raw).Within(0.001f));
    }

    [Test]
    public void PartialPenetration_LandsBetweenNoneAndFull()
    {
        const float Armor = 200f;

        var none = DamageResolver.Resolve(Hit(penetration: 0f), Armor);
        var half = DamageResolver.Resolve(Hit(penetration: 0.5f), Armor);
        var full = DamageResolver.Resolve(Hit(penetration: 1f), Armor);

        Assert.That(half, Is.GreaterThan(none));
        Assert.That(half, Is.LessThan(full));
    }

    [Test]
    public void AmmoMultiplier_ScalesTheRawDamage()
    {
        var doubled = DamageResolver.Resolve(Hit(multiplier: 2f), 0f);

        Assert.That(doubled, Is.EqualTo(Raw * 2f).Within(0.001f));
    }

    [Test]
    public void AWeaponWithNoDamage_DealsNothing_NotTheFloor()
    {
        Assert.That(DamageResolver.Resolve(Hit(raw: 0f), 0f), Is.Zero);
    }

    [Test]
    public void PenetrationOutsideZeroToOne_IsClamped()
    {
        const float Armor = 300f;

        Assert.That(
              DamageResolver.Resolve(Hit(penetration: 5f), Armor)
            , Is.EqualTo(DamageResolver.Resolve(Hit(penetration: 1f), Armor)).Within(0.001f)
        );

        Assert.That(
              DamageResolver.Resolve(Hit(penetration: -5f), Armor)
            , Is.EqualTo(DamageResolver.Resolve(Hit(penetration: 0f), Armor)).Within(0.001f)
        );
    }
}
