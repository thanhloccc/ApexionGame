using System.Collections.Generic;
using Game.Common;
using Game.Gameplay.Combat;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class CombatWorldTests
{
    private const float Step = 1f / 60f;
    private const float StartingHealth = 100f;

    private ItemCatalog _catalog;
    private CombatWorld _world;

    [SetUp]
    public void SetUp()
    {
        _catalog = TestItems.CreateCatalog();
        _world = new CombatWorld(_catalog, capacity: 16);
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
        _catalog?.Dispose();
    }

    private CombatantId Spawn(byte team, float3 position, float3 forward, float armor = 0f)
    {
        _world.CreateCombatant(team, weightCapacity: 40f, maxHealth: StartingHealth, armor: armor)
            .TryGetValue(out var id);

        _world.Registry.SetTransform(id, position, forward);

        return id;
    }

    /// <summary>Ticks and accumulates, because the world clears its buffer each tick.</summary>
    private List<CombatEvent> Run(int steps)
    {
        var collected = new List<CombatEvent>();

        for (var i = 0; i < steps; i++)
        {
            _world.Tick(Step);

            foreach (var combatEvent in _world.Events)
            {
                collected.Add(combatEvent);
            }
        }

        return collected;
    }

    private static int CountOf(List<CombatEvent> events, CombatEventKind kind)
    {
        var count = 0;

        for (var i = 0; i < events.Count; i++)
        {
            if (events[i].Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    [Test]
    public void ShootingAnEnemyInFront_TakesItsHealth()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        var bandit = Spawn(1, new float3(0f, 0f, 5f), -math.forward());

        _world.Equip(hero, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        var events = Run(10);

        Assert.That(CountOf(events, CombatEventKind.DamageDealt), Is.GreaterThan(0), "nothing was hit");
        Assert.That(_world[bandit].CurrentHealth, Is.LessThan(StartingHealth));
    }

    [Test]
    public void ShootingBeyondTheWeaponsRange_DoesNothing()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        var far = Spawn(1, new float3(0f, 0f, TestItems.RevolverRange + 50f), -math.forward());

        _world.Equip(hero, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        var events = Run(10);

        Assert.That(CountOf(events, CombatEventKind.DamageDealt), Is.Zero);
        Assert.That(_world[far].CurrentHealth, Is.EqualTo(StartingHealth).Within(0.001f));
    }

    [Test]
    public void ATeammateOnTheShotLine_IsNeverHit()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        var friend = Spawn(0, new float3(0f, 0f, 3f), -math.forward());

        _world.Equip(hero, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Run(10);

        Assert.That(_world[friend].CurrentHealth, Is.EqualTo(StartingHealth).Within(0.001f));
    }

    /// <summary>One shot fired at a target with the given armour, returning the health it lost.</summary>
    private static float DamageDealtAgainst(float targetArmor)
    {
        using var catalog = TestItems.CreateCatalog();
        using var world = new CombatWorld(catalog, capacity: 4);

        world.CreateCombatant(0, maxHealth: StartingHealth).TryGetValue(out var hero);
        world.CreateCombatant(1, maxHealth: StartingHealth, armor: targetArmor).TryGetValue(out var target);

        world.Registry.SetTransform(hero, float3.zero, math.forward());
        world.Registry.SetTransform(target, new float3(0f, 0f, 5f), -math.forward());

        world.Equip(hero, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);
        world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        for (var i = 0; i < 10; i++)
        {
            world.Tick(Step);
        }

        return StartingHealth - world[target].CurrentHealth;
    }

    [Test]
    public void ArmourReducesTheDamageTaken_ButNeverNullifiesIt()
    {
        var toSoft = DamageDealtAgainst(0f);
        var toArmoured = DamageDealtAgainst(300f);

        Assert.That(toSoft, Is.GreaterThan(0f));
        Assert.That(toArmoured, Is.GreaterThan(0f), "armour should mitigate, not nullify");
        Assert.That(toArmoured, Is.LessThan(toSoft));
    }

    [Test]
    public void DeathIsAnnouncedExactlyOnce_HoweverLongTheFightRuns()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        var bandit = Spawn(1, new float3(0f, 0f, 5f), -math.forward());

        _world.Equip(hero, EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);
        _world[hero].Weapons.SetAttackHeld(EquipmentSlot.PrimaryWeapon, true);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        var events = Run(600);

        Assert.That(_world[bandit].IsDead, Is.True, "sustained automatic fire did not kill the target");
        Assert.That(CountOf(events, CombatEventKind.Died), Is.EqualTo(1));
    }

    [Test]
    public void ADeadCombatant_StopsTakingDamage()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        var bandit = Spawn(1, new float3(0f, 0f, 5f), -math.forward());

        _world.Equip(hero, EquippedItem.Of(TestItems.Rifle), EquipmentSlot.PrimaryWeapon);
        _world[hero].Weapons.SetAttackHeld(EquipmentSlot.PrimaryWeapon, true);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Run(600);

        Assert.That(_world[bandit].IsDead, Is.True);
        Assert.That(_world.Registry.Find(bandit).TryGetValue(out var spatial), Is.True);
        Assert.That(spatial.IsAlive, Is.False, "the query array still lists the corpse as a target");
    }

    [Test]
    public void TwoCombatantsShootingEachOtherInTheSameTick_BothLand()
    {
        var a = Spawn(0, float3.zero, math.forward());
        var b = Spawn(1, new float3(0f, 0f, 5f), -math.forward());

        _world.Equip(a, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);
        _world.Equip(b, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);

        _world[a].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);
        _world[b].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Run(10);

        Assert.That(_world[a].CurrentHealth, Is.LessThan(StartingHealth), "A was not hit");
        Assert.That(_world[b].CurrentHealth, Is.LessThan(StartingHealth), "B was not hit");
    }

    [Test]
    public void TheEventBufferHoldsOnlyTheMostRecentTick()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        Spawn(1, new float3(0f, 0f, 5f), -math.forward());

        _world.Equip(hero, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        var sawDamage = false;

        for (var i = 0; i < 20; i++)
        {
            _world.Tick(Step);

            foreach (var combatEvent in _world.Events)
            {
                if (combatEvent.Kind == CombatEventKind.DamageDealt)
                {
                    sawDamage = true;
                }
            }
        }

        Assert.That(sawDamage, Is.True);

        for (var i = 0; i < 20; i++)
        {
            _world.Tick(Step);
        }

        Assert.That(_world.Events.Length, Is.Zero, "events from an earlier tick were still present");
    }

    [Test]
    public void MeleeSweepsEveryEnemyInTheArc()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        var left = Spawn(1, new float3(-0.3f, 0f, 1f), -math.forward());
        var right = Spawn(1, new float3(0.3f, 0f, 1f), -math.forward());
        var behind = Spawn(1, new float3(0f, 0f, -1f), math.forward());

        _world.Equip(hero, EquippedItem.Of(TestItems.Knife), EquipmentSlot.MeleeWeapon);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.MeleeWeapon);

        Run(30);

        Assert.That(_world[left].CurrentHealth, Is.LessThan(StartingHealth), "left was not swept");
        Assert.That(_world[right].CurrentHealth, Is.LessThan(StartingHealth), "right was not swept");
        Assert.That(_world[behind].CurrentHealth, Is.EqualTo(StartingHealth).Within(0.001f));
    }

    [Test]
    public void CreatingBeyondCapacity_Fails()
    {
        using var catalog = TestItems.CreateCatalog();
        using var tiny = new CombatWorld(catalog, capacity: 2);

        Assert.That(tiny.CreateCombatant(0).TryGetValue(out _), Is.True);
        Assert.That(tiny.CreateCombatant(0).TryGetValue(out _), Is.True);
        Assert.That(tiny.CreateCombatant(0).TryGetError(out _), Is.True);
    }

    [Test]
    public void DestroyingACombatant_RemovesItFromQueries()
    {
        var hero = Spawn(0, float3.zero, math.forward());
        var bandit = Spawn(1, new float3(0f, 0f, 5f), -math.forward());

        Assert.That(_world.DestroyCombatant(bandit), Is.True);
        Assert.That(_world.Registry.Contains(bandit), Is.False);
        Assert.That(_world.Registry.Contains(hero), Is.True, "the swap-back removal took the wrong entry");

        _world.Equip(hero, EquippedItem.Of(TestItems.Revolver), EquipmentSlot.PrimaryWeapon);
        _world[hero].Weapons.RequestAttack(EquipmentSlot.PrimaryWeapon);

        Assert.That(CountOf(Run(10), CombatEventKind.DamageDealt), Is.Zero);
    }
}
