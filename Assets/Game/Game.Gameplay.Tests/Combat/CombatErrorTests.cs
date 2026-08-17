using Game.Common;
using Game.Gameplay.Combat;
using Game.Gameplay.Items;
using Game.Gameplay.Weapons;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class CombatErrorTests
{
    private static readonly CombatantId Who = new(7);

    private static CombatError[] EveryCase()
        => new[] {
            CombatError.UnknownCombatant(Who),
            CombatError.CapacityReached(64),
            CombatError.EquipRejected(
                  Who
                , EquipmentSlot.Head
                , TestItems.Helmet
                , ItemError.UnknownItem(TestItems.Missing)
            ),
            CombatError.UnequipRejected(Who, EquipmentSlot.Head, ItemError.SlotEmpty(EquipmentSlot.Head)),
            CombatError.WeaponAttachFailed(
                  Who
                , EquipmentSlot.PrimaryWeapon
                , TestItems.BrokenGun
                , WeaponError.NonPositiveRateOfFire(TestItems.BrokenGun, 0f)
            ),
            CombatError.AlreadyDead(Who),
        };

    [Test]
    public void TheDefaultValue_IsSafeToPrint()
    {
        var message = default(CombatError).ToString();

        Assert.That(message, Is.Not.Null);
        Assert.That(message, Is.Not.Empty);
    }

    [Test]
    public void EveryCase_ProducesAMessage()
    {
        foreach (var error in EveryCase())
        {
            Assert.That(error.ToString(), Is.Not.Empty);
        }
    }

    [Test]
    public void ToFixedString_MatchesToString()
    {
        foreach (var error in EveryCase())
        {
            Assert.That(error.ToFixedString().ToString(), Is.EqualTo(error.ToString()));
        }
    }

    [Test]
    public void Prefix_IsShownAndDoesNotLoseTheMessage()
    {
        var plain = CombatError.AlreadyDead(Who);
        var prefixed = plain.Prefix("Combat");

        Assert.That(prefixed.ToString(), Does.StartWith("[Combat]"));
        Assert.That(prefixed.ToString(), Does.EndWith(plain.ToString()));
    }

    [Test]
    public void AWrappedError_CarriesTheUnderlyingReasonThrough()
    {
        var inner = ItemError.UnknownItem(TestItems.Missing);

        var outer = CombatError.EquipRejected(Who, EquipmentSlot.Head, TestItems.Helmet, inner);

        Assert.That(
              outer.ToString()
            , Does.Contain("not present in the catalog")
            , "the underlying item error was swallowed"
        );
    }

    [Test]
    public void AWeaponAttachFailure_NamesTheWeaponAndTheReason()
    {
        var error = CombatError.WeaponAttachFailed(
              Who
            , EquipmentSlot.PrimaryWeapon
            , TestItems.BrokenGun
            , WeaponError.NonPositiveRateOfFire(TestItems.BrokenGun, 0f)
        );

        var message = error.ToString();

        Assert.That(message, Does.Contain("PrimaryWeapon"), "the slot is the first thing you need to diagnose this");
        Assert.That(message, Does.Contain("RangedWeapon-4"), "the weapon is not named");
        Assert.That(message, Does.Contain("rate of fire"), "the underlying reason was swallowed");
    }
}
