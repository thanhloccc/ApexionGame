using Game.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class MagazineTests
{
    private ItemCatalog _catalog;
    private Loadout _loadout;

    [SetUp]
    public void SetUp()
    {
        _catalog = TestItems.CreateCatalog();
        _loadout = new Loadout(_catalog, 50f);
    }

    [TearDown]
    public void TearDown()
        => _catalog?.Dispose();

    [Test]
    public void EquippingFreshGun_FillsMagazineToCapacity()
    {
        var result = _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);

        Assert.That(result.TryGetValue(out var change), Is.True);
        Assert.That(change.Equipped.Rounds.TryGetValue(out var rounds), Is.True);
        Assert.That(rounds, Is.EqualTo(TestItems.RifleMagazine));
    }

    [Test]
    public void SetRounds_AboveCapacity_Fails()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);

        var result = _loadout.SetRounds(EquipmentSlot.PrimaryWeapon, TestItems.RifleMagazine + 1);

        Assert.That(result.IsError, Is.True);
    }

    [Test]
    public void SetRounds_Negative_Fails()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);

        var result = _loadout.SetRounds(EquipmentSlot.PrimaryWeapon, -1);

        Assert.That(result.IsError, Is.True);
    }

    [Test]
    public void Unequip_CarriesTheRoundsOut()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);
        _loadout.SetRounds(EquipmentSlot.PrimaryWeapon, 12);

        var result = _loadout.Unequip(EquipmentSlot.PrimaryWeapon);

        Assert.That(result.TryGetValue(out var removed), Is.True);
        Assert.That(removed.Rounds.TryGetValue(out var rounds), Is.True);
        Assert.That(rounds, Is.EqualTo(12));
    }

    [Test]
    public void UnequipThenReEquip_PreservesRounds()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);
        _loadout.SetRounds(EquipmentSlot.PrimaryWeapon, 7);

        _loadout.Unequip(EquipmentSlot.PrimaryWeapon).TryGetValue(out var removed);
        _loadout.Equip(removed, EquipmentSlot.SecondaryWeapon);

        var back = _loadout.GetEquipped(EquipmentSlot.SecondaryWeapon);

        Assert.That(back.TryGetValue(out var equipped), Is.True);
        Assert.That(equipped.Rounds.TryGetValue(out var rounds), Is.True);
        Assert.That(rounds, Is.EqualTo(7));
    }

    [Test]
    public void SetRounds_OnNonRangedSlot_Fails()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);

        var result = _loadout.SetRounds(EquipmentSlot.Head, 5);

        Assert.That(result.IsError, Is.True);
    }

    [Test]
    public void NonRangedItem_HasNoRounds()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);

        var equipped = _loadout.GetEquipped(EquipmentSlot.Head);

        Assert.That(equipped.TryGetValue(out var item), Is.True);
        Assert.That(item.Rounds.HasValue, Is.False);
    }
}
