using System.Collections.Generic;
using Game.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using Game.Gameplay.Weapons;
using NUnit.Framework;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class WeaponMachineTests
{
    private const float Step = 0.01f;

    private ItemCatalog _catalog;
    private Loadout _loadout;
    private WeaponController _controller;

    [SetUp]
    public void SetUp()
    {
        _catalog = TestItems.CreateCatalog();
        _loadout = new Loadout(_catalog, 100f);
        _controller = new WeaponController(_catalog, _loadout);
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
        _catalog?.Dispose();
    }

    private void EquipWeapon(ItemId id, EquipmentSlot slot)
    {
        _loadout.Equip(EquippedItem.Of(id), slot);
        var attached = _controller.OnEquipped(slot);
        Assert.That(attached.IsSuccess, Is.True, $"could not attach {id}");
    }

    private void Advance(float seconds)
    {
        var steps = (int)System.Math.Round(seconds / (double)Step);

        for (var i = 0; i < steps; i++)
        {
            _controller.Tick(Step);
        }
    }

    private int CountOf(WeaponEventKind kind)
    {
        var count = 0;
        var events = _controller.Events;

        for (var i = 0; i < events.Length; i++)
        {
            if (events[i].Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    private int Rounds(EquipmentSlot slot)
        => _loadout.GetEquipped(slot).TryGetValue(out var e) && e.Rounds.TryGetValue(out var r) ? r : -1;

    // ---- melee ---------------------------------------------------------------------------------

    [Test]
    public void Melee_AttackEntersWindUp_WithoutActivatingYet()
    {
        EquipWeapon(TestItems.Knife, EquipmentSlot.MeleeWeapon);
        _controller.RequestAttack(EquipmentSlot.MeleeWeapon);
        _controller.Tick(0f);

        Assert.That(_controller.MeleeStateOf(EquipmentSlot.MeleeWeapon).GetValueOrDefault(),
            Is.EqualTo(MeleeAttackState.WindUp));
        Assert.That(CountOf(WeaponEventKind.AttackActivated), Is.Zero);
    }

    [Test]
    public void Melee_ActivatesAfterWindUp_ExactlyOnce()
    {
        EquipWeapon(TestItems.Knife, EquipmentSlot.MeleeWeapon);
        _controller.RequestAttack(EquipmentSlot.MeleeWeapon);

        Advance(TestItems.KnifeWindUp - Step);

        Assert.That(
              CountOf(WeaponEventKind.AttackActivated)
            , Is.Zero
            , "the attack must not go live before the windup has elapsed"
        );

        Advance(3 * Step);

        Assert.That(_controller.MeleeStateOf(EquipmentSlot.MeleeWeapon).GetValueOrDefault(),
            Is.EqualTo(MeleeAttackState.Active));
        Assert.That(CountOf(WeaponEventKind.AttackActivated), Is.EqualTo(1));
    }

    [Test]
    public void Melee_ReturnsToIdleAfterFullCycle()
    {
        EquipWeapon(TestItems.Knife, EquipmentSlot.MeleeWeapon);
        _controller.RequestAttack(EquipmentSlot.MeleeWeapon);
        Advance(TestItems.KnifeWindUp + TestItems.KnifeActive + TestItems.KnifeRecovery + 4 * Step);

        Assert.That(_controller.MeleeStateOf(EquipmentSlot.MeleeWeapon).GetValueOrDefault(),
            Is.EqualTo(MeleeAttackState.Idle));
    }

    [Test]
    public void Melee_CancelReturnsToIdleImmediately()
    {
        EquipWeapon(TestItems.Knife, EquipmentSlot.MeleeWeapon);
        _controller.RequestAttack(EquipmentSlot.MeleeWeapon);
        Advance(TestItems.KnifeWindUp + Step);
        _controller.Cancel(EquipmentSlot.MeleeWeapon);
        _controller.Tick(0f);

        Assert.That(_controller.MeleeStateOf(EquipmentSlot.MeleeWeapon).GetValueOrDefault(),
            Is.EqualTo(MeleeAttackState.Idle));
    }

    // ---- ranged: fire modes --------------------------------------------------------------------

    [Test]
    public void Single_OneAttack_FiresExactlyOneRound()
    {
        EquipWeapon(TestItems.Revolver, EquipmentSlot.PrimaryWeapon);
        var before = Rounds(EquipmentSlot.PrimaryWeapon);

        _controller.RequestAttack(EquipmentSlot.PrimaryWeapon);
        Advance(1f);

        Assert.That(CountOf(WeaponEventKind.ShotFired), Is.EqualTo(1));
        Assert.That(Rounds(EquipmentSlot.PrimaryWeapon), Is.EqualTo(before - 1));
    }

    [Test]
    public void Burst_OneAttack_FiresBurstCountRounds()
    {
        EquipWeapon(TestItems.Smg, EquipmentSlot.PrimaryWeapon);
        var before = Rounds(EquipmentSlot.PrimaryWeapon);

        _controller.RequestAttack(EquipmentSlot.PrimaryWeapon);
        Advance(1f);

        Assert.That(CountOf(WeaponEventKind.ShotFired), Is.EqualTo(TestItems.SmgBurstCount));
        Assert.That(Rounds(EquipmentSlot.PrimaryWeapon),
            Is.EqualTo(before - TestItems.SmgBurstCount));
    }

    [Test]
    public void Auto_HeldForOneSecond_FiresAboutRpmOverSixty()
    {
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);
        _controller.SetAttackHeld(EquipmentSlot.PrimaryWeapon, true);
        _controller.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Advance(1f);

        var expected = (int)(TestItems.RifleRpm / 60f);

        Assert.That(CountOf(WeaponEventKind.ShotFired), Is.EqualTo(expected).Within(1));
    }

    [Test]
    public void Auto_ReleasingStopsFiring()
    {
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);
        _controller.SetAttackHeld(EquipmentSlot.PrimaryWeapon, true);
        _controller.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Advance(0.5f);
        var fired = CountOf(WeaponEventKind.ShotFired);

        _controller.SetAttackHeld(EquipmentSlot.PrimaryWeapon, false);
        Advance(1f);

        Assert.That(CountOf(WeaponEventKind.ShotFired), Is.EqualTo(fired).Within(1));
        Assert.That(_controller.RangedStateOf(EquipmentSlot.PrimaryWeapon).GetValueOrDefault(),
            Is.EqualTo(RangedFireState.Idle));
    }

    // ---- ranged: ammo --------------------------------------------------------------------------

    [Test]
    public void FiringToEmpty_EntersEmptyAndStops()
    {
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);
        _loadout.SetRounds(EquipmentSlot.PrimaryWeapon, 2);
        _controller.SetAttackHeld(EquipmentSlot.PrimaryWeapon, true);
        _controller.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Advance(2f);

        Assert.That(CountOf(WeaponEventKind.ShotFired), Is.EqualTo(2));
        Assert.That(CountOf(WeaponEventKind.WentEmpty), Is.EqualTo(1));
        Assert.That(Rounds(EquipmentSlot.PrimaryWeapon), Is.Zero);
        Assert.That(_controller.RangedStateOf(EquipmentSlot.PrimaryWeapon).GetValueOrDefault(),
            Is.EqualTo(RangedFireState.Empty));
    }

    [Test]
    public void RoundsInLoadout_MatchShotFiredCount()
    {
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);
        var before = Rounds(EquipmentSlot.PrimaryWeapon);

        _controller.SetAttackHeld(EquipmentSlot.PrimaryWeapon, true);
        _controller.RequestAttack(EquipmentSlot.PrimaryWeapon);
        Advance(0.7f);

        var shots = CountOf(WeaponEventKind.ShotFired);

        Assert.That(Rounds(EquipmentSlot.PrimaryWeapon), Is.EqualTo(before - shots));
    }

    // ---- reload --------------------------------------------------------------------------------

    [Test]
    public void Reload_RefillsAfterReloadSeconds()
    {
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);
        _loadout.SetRounds(EquipmentSlot.PrimaryWeapon, 5);

        _controller.RequestReload(EquipmentSlot.PrimaryWeapon);
        Advance(TestItems.ReloadSeconds + 4 * Step);

        Assert.That(Rounds(EquipmentSlot.PrimaryWeapon), Is.EqualTo(TestItems.RifleMagazine));
        Assert.That(CountOf(WeaponEventKind.ReloadFinished), Is.EqualTo(1));
    }

    [Test]
    public void Reload_OnFullMagazine_IsRejected()
    {
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);

        _controller.RequestReload(EquipmentSlot.PrimaryWeapon);
        Advance(TestItems.ReloadSeconds + 4 * Step);

        Assert.That(CountOf(WeaponEventKind.ReloadStarted), Is.Zero);
    }

    [Test]
    public void Reload_Cancelled_LeavesRoundsUnchanged()
    {
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);
        _loadout.SetRounds(EquipmentSlot.PrimaryWeapon, 5);

        _controller.RequestReload(EquipmentSlot.PrimaryWeapon);
        Advance(TestItems.ReloadSeconds * 0.5f);
        _controller.Cancel(EquipmentSlot.PrimaryWeapon);
        Advance(TestItems.ReloadSeconds);

        Assert.That(Rounds(EquipmentSlot.PrimaryWeapon), Is.EqualTo(5));
        Assert.That(CountOf(WeaponEventKind.ReloadFinished), Is.Zero);
    }

    // ---- equip lifecycle -----------------------------------------------------------------------

    [Test]
    public void Unequip_MidAttack_StopsEmittingEvents()
    {
        EquipWeapon(TestItems.Knife, EquipmentSlot.MeleeWeapon);
        _controller.RequestAttack(EquipmentSlot.MeleeWeapon);

        _controller.OnUnequipped(EquipmentSlot.MeleeWeapon);
        _loadout.Unequip(EquipmentSlot.MeleeWeapon);
        _controller.ClearEvents();

        Advance(1f);

        Assert.That(_controller.Events.Length, Is.Zero);
        Assert.That(_controller.MeleeStateOf(EquipmentSlot.MeleeWeapon).HasValue, Is.False);
    }

    [Test]
    public void DefinitionCount_IsPerWeaponKind_NotPerWeapon()
    {
        EquipWeapon(TestItems.Knife, EquipmentSlot.MeleeWeapon);
        EquipWeapon(TestItems.Rifle, EquipmentSlot.PrimaryWeapon);
        EquipWeapon(TestItems.Revolver, EquipmentSlot.SecondaryWeapon);

        Assert.That(_controller.DefinitionCount, Is.EqualTo(2));
    }

    // ---- determinism and errors ----------------------------------------------------------------

    [Test]
    public void SameInputs_ProduceTheSameEventSequence()
    {
        static List<WeaponEventKind> Run()
        {
            using var catalog = TestItems.CreateCatalog();
            var loadout = new Loadout(catalog, 100f);
            using var controller = new WeaponController(catalog, loadout);

            loadout.Equip(EquippedItem.Of(TestItems.Smg), EquipmentSlot.PrimaryWeapon);
            controller.OnEquipped(EquipmentSlot.PrimaryWeapon);
            controller.SetAttackHeld(EquipmentSlot.PrimaryWeapon, true);
            controller.RequestAttack(EquipmentSlot.PrimaryWeapon);

            for (var i = 0; i < 200; i++)
            {
                controller.Tick(Step);
            }

            var kinds = new List<WeaponEventKind>();
            var events = controller.Events;

            for (var i = 0; i < events.Length; i++)
            {
                kinds.Add(events[i].Kind);
            }

            return kinds;
        }

        Assert.That(Run(), Is.EqualTo(Run()));
    }

    [Test]
    public void BadWeaponData_IsRejectedWithoutThrowing()
    {
        _loadout.Equip(EquippedItem.Of(TestItems.BrokenGun), EquipmentSlot.PrimaryWeapon);

        var result = _controller.OnEquipped(EquipmentSlot.PrimaryWeapon);

        Assert.That(result.TryGetFailure(out var error), Is.True);
        Assert.That(error.ToString(), Does.Contain("rate of fire"));
    }

    [Test]
    public void AttackOnEmptySlot_ReportsNoWeapon()
    {
        var result = _controller.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Assert.That(result.TryGetFailure(out var error), Is.True);
        Assert.That(error.ToString(), Does.Contain("no weapon"));
    }

    [Test]
    public void DefaultWeaponError_IsSafe()
    {
        Assert.That(default(WeaponError).ToString(), Does.Contain("unknown weapon error"));
    }
}
