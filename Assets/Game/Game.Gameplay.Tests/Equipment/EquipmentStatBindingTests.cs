using ApexionGame.Entities.Stats;
using Game.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Items;
using NUnit.Framework;
using Unity.Collections;

namespace Game.Gameplay.Tests;

[TestFixture]
internal sealed class EquipmentStatBindingTests
{
    private const float BaseArmor = 10f;

    private ItemCatalog _catalog;
    private EquipmentStatBinding _binding;

    private StatStore<GameStatSystem.Stat, GameStatSystem.StatModifier, GameStatSystem.StatObserver> _store;
    private GameStatSystem.Accessor _accessor;
    private GameStatSystem.WorldData _worldData;

    private StatOwnerHandle _owner;
    private CharacterStats _stats;
    private CharacterStatHandles _handles;

    [SetUp]
    public void SetUp()
    {
        _catalog = TestItems.CreateCatalog();
        _binding = new EquipmentStatBinding(_catalog);

        _store = new StatStore<GameStatSystem.Stat, GameStatSystem.StatModifier, GameStatSystem.StatObserver>(
            initialOwnerCapacity: 4, Allocator.Persistent);

        _accessor = new GameStatSystem.Accessor(_store);
        _worldData = new GameStatSystem.WorldData(16, Allocator.Persistent);

        _stats = CharacterStats.Builder
            .Build(ref _store, out _owner)
            .CreateAllStats(produceChangeEvents: true)
            .ToStats();

        var generated = _stats.GetStatHandles(_owner);

        _handles = new CharacterStatHandles(
              generated.maxHealth
            , generated.armor
            , generated.moveSpeed
            , generated.carryCapacity
        );

        _accessor.TrySetStatBaseValue(
              _handles.Armor
            , new GameStatSystem.ValuePair(new StatVariant(BaseArmor))
            , ref _worldData
        );
    }

    [TearDown]
    public void TearDown()
    {
        _binding?.Dispose();
        _catalog?.Dispose();

        if (_worldData.IsCreated)
        {
            _worldData.Dispose();
        }

        if (_store.IsCreated)
        {
            _store.Dispose();
        }
    }

    private float ReadArmor()
        => _accessor.TryGetStatValue(_handles.Armor, out var pair)
            ? pair.GetCurrentValueOrDefault(new StatVariant(0f)).Float
            : float.NaN;

    private int ArmorModifierCount()
        => _accessor.TryGetModifierCount(_handles.Armor, out var count) ? count : -1;

    [Test]
    public void Apply_RaisesTheStat()
    {
        Assert.That(ReadArmor(), Is.EqualTo(BaseArmor).Within(0.001f));

        _binding.Apply(EquipmentSlot.Head, TestItems.Helmet, _handles, ref _accessor, ref _worldData);

        Assert.That(
              ReadArmor()
            , Is.EqualTo(BaseArmor + TestItems.HelmetArmorBonus).Within(0.001f)
        );
    }

    [Test]
    public void Remove_ReturnsTheStatToItsExactBaseValue()
    {
        _binding.Apply(EquipmentSlot.Head, TestItems.Helmet, _handles, ref _accessor, ref _worldData);
        _binding.Remove(EquipmentSlot.Head, ref _accessor, ref _worldData);

        Assert.That(ReadArmor(), Is.EqualTo(BaseArmor).Within(0.001f));
        Assert.That(_binding.AppliedCount(EquipmentSlot.Head), Is.Zero);
    }

    [Test]
    public void RepeatedApplyAndRemove_LeavesNoModifierBehind()
    {
        for (var i = 0; i < 100; i++)
        {
            _binding.Apply(EquipmentSlot.Head, TestItems.Helmet, _handles, ref _accessor, ref _worldData);
            _binding.Remove(EquipmentSlot.Head, ref _accessor, ref _worldData);
        }

        Assert.That(ArmorModifierCount(), Is.Zero);
        Assert.That(ReadArmor(), Is.EqualTo(BaseArmor).Within(0.001f));
    }

    [Test]
    public void Apply_Twice_DoesNotStack()
    {
        _binding.Apply(EquipmentSlot.Head, TestItems.Helmet, _handles, ref _accessor, ref _worldData);
        _binding.Apply(EquipmentSlot.Head, TestItems.Helmet, _handles, ref _accessor, ref _worldData);

        Assert.That(
              ReadArmor()
            , Is.EqualTo(BaseArmor + TestItems.HelmetArmorBonus).Within(0.001f)
        );

        Assert.That(ArmorModifierCount(), Is.EqualTo(1));
    }

    [Test]
    public void EveryDeclaredStatKind_ResolvesToAHandle()
    {
        var kinds = System.Enum.GetValues(typeof(CharacterStatKind));

        foreach (CharacterStatKind kind in kinds)
        {
            if (kind == CharacterStatKind.Undefined)
            {
                continue;
            }

            Assert.That(
                  _handles.Resolve(kind).HasValue
                , Is.True
                , $"{kind} has no matching stat in {nameof(CharacterStats)}"
            );
        }
    }
}
