using Game.Common;
using Game.Gameplay.Combat;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

/// <summary>
/// Covers the transaction that nothing owned before <see cref="Combatant"/> existed: equipping an
/// item has to move the loadout, the stat modifiers and the weapon machine together or not at all.
/// </summary>
[TestFixture]
internal sealed class CombatantTests
{
    private const float BaseArmor = 10f;

    private ItemCatalog _catalog;
    private CombatWorld _world;
    private CombatantId _hero;

    [SetUp]
    public void SetUp()
    {
        _catalog = TestItems.CreateCatalog();
        _world = new CombatWorld(_catalog, capacity: 8);

        _world.CreateCombatant(team: 0, weightCapacity: 40f, armor: BaseArmor).TryGetValue(out _hero);
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
        _catalog?.Dispose();
    }

    private float Armor()
        => _world.StatValueOf(_hero, CharacterStatKind.Armor);

    [Test]
    public void Equip_AppliesTheItemsStatModifiers()
    {
        Assert.That(Armor(), Is.EqualTo(BaseArmor).Within(0.001f));

        var equipped = _world.Equip(_hero, EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);

        Assert.That(equipped.IsSuccess, Is.True, equipped.ToString());
        Assert.That(Armor(), Is.EqualTo(BaseArmor + TestItems.HelmetArmorBonus).Within(0.001f));
    }

    [Test]
    public void Unequip_ReturnsTheStatToItsBaseValue()
    {
        _world.Equip(_hero, EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);
        _world.Unequip(_hero, EquipmentSlot.Head);

        Assert.That(Armor(), Is.EqualTo(BaseArmor).Within(0.001f));
    }

    [Test]
    public void EquippingArmour_DoesNotFailLookingForAWeaponMachine()
    {
        var equipped = _world.Equip(_hero, EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);

        Assert.That(equipped.IsSuccess, Is.True, equipped.ToString());
        Assert.That(_world[_hero].Loadout.GetEquipped(EquipmentSlot.Head).HasValue, Is.True);
    }

    [Test]
    public void Equip_AttachesAWorkingWeaponMachine()
    {
        var equipped = _world.Equip(
              _hero
            , EquippedItem.Of(TestItems.Rifle)
            , EquipmentSlot.PrimaryWeapon
        );

        Assert.That(equipped.IsSuccess, Is.True, equipped.ToString());

        var attack = _world[_hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Assert.That(attack.IsSuccess, Is.True, "the slot holds a weapon but no machine answered");
    }

    [Test]
    public void AWeaponWhoseMachineRefuses_LeavesNothingBehind()
    {
        var equipped = _world.Equip(
              _hero
            , EquippedItem.Of(TestItems.BrokenGun)
            , EquipmentSlot.PrimaryWeapon
        );

        Assert.That(equipped.IsSuccess, Is.False, "a weapon with no rate of fire should be refused");

        Assert.That(
              _world[_hero].Loadout.GetEquipped(EquipmentSlot.PrimaryWeapon).HasValue
            , Is.False
            , "the slot kept a weapon whose machine never attached"
        );

        Assert.That(Armor(), Is.EqualTo(BaseArmor).Within(0.001f));
    }

    [Test]
    public void AFailedSwap_RestoresThePreviousWeapon_StillWorking()
    {
        _world.Equip(_hero, EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);

        var swap = _world.Equip(
              _hero
            , EquippedItem.Of(TestItems.BrokenGun)
            , EquipmentSlot.PrimaryWeapon
        );

        Assert.That(swap.IsSuccess, Is.False);

        var slot = _world[_hero].Loadout.GetEquipped(EquipmentSlot.PrimaryWeapon);

        Assert.That(slot.TryGetValue(out var still), Is.True, "the previous weapon was lost");
        Assert.That(still.Id, Is.EqualTo(TestItems.Rifle));

        Assert.That(
              _world[_hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon).IsSuccess
            , Is.True
            , "the restored weapon has no machine behind it"
        );
    }

    [Test]
    public void RepeatedEquipAndUnequip_LeavesNoModifierBehind()
    {
        for (var i = 0; i < 50; i++)
        {
            _world.Equip(_hero, EquippedItem.Of(TestItems.Helmet), EquipmentSlot.Head);
            _world.Unequip(_hero, EquipmentSlot.Head);
        }

        Assert.That(Armor(), Is.EqualTo(BaseArmor).Within(0.001f));
    }

    [Test]
    public void Unequip_HandsBackTheItemWithItsRemainingRounds()
    {
        _world.Equip(
              _hero
            , EquippedItem.Of(TestItems.Rifle, rounds: 7)
            , EquipmentSlot.PrimaryWeapon
        );

        var removed = _world.Unequip(_hero, EquipmentSlot.PrimaryWeapon);

        Assert.That(removed.TryGetValue(out var item), Is.True);
        Assert.That(item.Id, Is.EqualTo(TestItems.Rifle));
        Assert.That(item.Rounds.TryGetValue(out var rounds), Is.True);
        Assert.That(rounds, Is.EqualTo(7));
    }

    [Test]
    public void UnequipAnEmptySlot_Fails_AndSaysWhy()
    {
        var removed = _world.Unequip(_hero, EquipmentSlot.Head);

        Assert.That(removed.TryGetError(out var error), Is.True);
        Assert.That(error.ToString(), Does.Contain("unequip"));
    }

    [Test]
    public void EquippingOnAnUnknownCombatant_Fails()
    {
        var equipped = _world.Equip(
              new CombatantId(9999)
            , EquippedItem.Of(TestItems.Helmet)
            , EquipmentSlot.Head
        );

        Assert.That(equipped.IsSuccess, Is.False);
    }
}
