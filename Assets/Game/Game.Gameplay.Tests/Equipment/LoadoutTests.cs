using Game.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class LoadoutTests
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
    public void Equip_CompatibleSlot_Succeeds()
    {
        var result = _loadout.Equip(EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);

        Assert.That(result.IsError, Is.False);
        Assert.That(_loadout.GetEquipped(EquipmentSlot.Head).HasValue, Is.True);
    }

    [Test]
    public void Equip_IncompatibleSlot_Fails()
    {
        var result = _loadout.Equip(EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Feet);

        Assert.That(result.IsError, Is.True);
        Assert.That(_loadout.GetEquipped(EquipmentSlot.Feet).HasValue, Is.False);
    }

    [Test]
    public void Equip_UnknownItem_Fails()
    {
        var result = _loadout.Equip(EquippedItem.Of(TestItems.Missing), EquipmentSlot.Head);

        Assert.That(result.IsError, Is.True);
    }

    [Test]
    public void Equip_OverOccupiedSlot_ReturnsReplaced()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);

        var result = _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);

        Assert.That(result.TryGetValue(out var change), Is.True);
        Assert.That(change.Replaced.HasValue, Is.True);
    }

    [Test]
    public void Equip_OverWeightCapacity_Fails_AndLeavesLoadoutUnchanged()
    {
        var tight = new Loadout(_catalog, 1f);

        var result = tight.Equip(EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);

        Assert.That(result.IsError, Is.True);
        Assert.That(tight.GetEquipped(EquipmentSlot.Head).HasValue, Is.False);
        Assert.That(tight.CarriedWeight, Is.EqualTo(0f));
    }

    [Test]
    public void Equip_BeyondMaxCopies_Fails()
    {
        var first = _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);
        var second = _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.SecondaryWeapon);

        Assert.That(first.IsError, Is.False);
        Assert.That(second.IsError, Is.True);
    }

    [Test]
    public void Unequip_EmptySlot_Fails()
    {
        var result = _loadout.Unequip(EquipmentSlot.Head);

        Assert.That(result.IsError, Is.True);
    }

    [Test]
    public void Gun_FitsWeaponSlot_ArmourDoesNot()
    {
        var gun = _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);
        var armour = _loadout.Equip(EquippedItem.Of(TestItems.Helmet), EquipmentSlot.SecondaryWeapon);

        Assert.That(gun.IsError, Is.False);
        Assert.That(armour.IsError, Is.True);
    }

    [Test]
    public void Knife_FitsMeleeSlotOnly()
    {
        var melee = _loadout.Equip(EquippedItem.Of(TestItems.Knife), EquipmentSlot.MeleeWeapon);
        var primary = _loadout.Equip(EquippedItem.Of(TestItems.Knife), EquipmentSlot.PrimaryWeapon);

        Assert.That(melee.IsError, Is.False);
        Assert.That(primary.IsError, Is.True);
    }

    [Test]
    public void CarriedWeight_IsDerivedFromEquippedItems()
    {
        Assert.That(_loadout.CarriedWeight, Is.EqualTo(0f));

        _loadout.Equip(EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);
        Assert.That(_loadout.CarriedWeight, Is.EqualTo(TestItems.HelmetWeight).Within(0.001f));

        _loadout.Equip(EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);
        Assert.That(
              _loadout.CarriedWeight
            , Is.EqualTo(TestItems.HelmetWeight + TestItems.RifleWeight).Within(0.001f)
        );

        _loadout.Unequip(EquipmentSlot.Head);
        Assert.That(_loadout.CarriedWeight, Is.EqualTo(TestItems.RifleWeight).Within(0.001f));

        _loadout.Unequip(EquipmentSlot.PrimaryWeapon);
        Assert.That(_loadout.CarriedWeight, Is.EqualTo(0f));
    }
}
