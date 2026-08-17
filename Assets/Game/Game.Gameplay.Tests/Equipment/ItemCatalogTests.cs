using Game.Gameplay.Items;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class ItemCatalogTests
{
    private ItemCatalog _catalog;

    [SetUp]
    public void SetUp()
        => _catalog = TestItems.CreateCatalog();

    [TearDown]
    public void TearDown()
        => _catalog?.Dispose();

    [Test]
    public void Find_ResolvesEveryKind()
    {
        Assert.That(_catalog.Find(TestItems.Helmet).HasValue, Is.True);
        Assert.That(_catalog.Find(TestItems.Rifle).HasValue, Is.True);
        Assert.That(_catalog.Find(TestItems.Knife).HasValue, Is.True);
        Assert.That(_catalog.Find(TestItems.Bullet).HasValue, Is.True);
    }

    [Test]
    public void FindExtension_ResolvesMatchingKindOnly()
    {
        Assert.That(_catalog.FindEquipment(TestItems.Helmet).HasValue, Is.True);
        Assert.That(_catalog.FindRangedWeapon(TestItems.Rifle).HasValue, Is.True);
        Assert.That(_catalog.FindMeleeWeapon(TestItems.Knife).HasValue, Is.True);
        Assert.That(_catalog.FindAmmo(TestItems.Bullet).HasValue, Is.True);

        Assert.That(_catalog.FindRangedWeapon(TestItems.Helmet).HasValue, Is.False);
        Assert.That(_catalog.FindEquipment(TestItems.Rifle).HasValue, Is.False);
    }

    [Test]
    public void Find_UnknownId_ReturnsNone()
    {
        Assert.That(_catalog.Find(TestItems.Missing).HasValue, Is.False);
        Assert.That(_catalog.FindEquipment(TestItems.Missing).HasValue, Is.False);
    }
}
