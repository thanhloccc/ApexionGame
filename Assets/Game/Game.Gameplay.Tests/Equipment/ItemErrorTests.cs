using Game.Common;
using Game.Gameplay.Items;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class ItemErrorTests
{
    [Test]
    public void Default_IsSafeAndReportsUndefined()
    {
        var message = default(ItemError).ToString();

        Assert.That(message, Is.Not.Null);
        Assert.That(message, Does.Contain("unknown item error"));
    }

    [Test]
    public void Factories_CarryTheirPayload()
    {
        var slotEmpty = ItemError.SlotEmpty(EquipmentSlot.PrimaryWeapon).ToString();
        var overflow = ItemError.MagazineOverflow(TestItems.Rifle, 30).ToString();

        Assert.That(slotEmpty, Does.Contain("PrimaryWeapon"));
        Assert.That(overflow, Does.Contain("30"));
    }

    [Test]
    public void Prefix_IsPrepended()
    {
        var message = ItemError.SlotEmpty(EquipmentSlot.Head).Prefix(nameof(ItemError)).ToString();

        Assert.That(message, Does.StartWith("[" + nameof(ItemError) + "]"));
    }

    [Test]
    public void ToFixedString_MatchesToString()
    {
        var error = ItemError.TooManyCopies(TestItems.Helmet, 1);

        Assert.That(error.ToFixedString().ToString(), Is.EqualTo(error.ToString()));
    }
}
