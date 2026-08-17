using System;
using ApexionGame.Entities.Stats;
using EncosyTower.Collections;
using Game.Common;
using Game.Gameplay.Items;

namespace Game.Gameplay.Equipment
{
    public sealed class EquipmentStatBinding : IDisposable
    {
        private readonly ItemCatalog _catalog;
        private readonly ArrayMap<EquipmentSlot, FasterList<StatModifierHandle>> _applied;

        private bool _isDisposed;

        public EquipmentStatBinding(ItemCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _applied = new ArrayMap<EquipmentSlot, FasterList<StatModifierHandle>>(8);
        }

        public int AppliedCount(EquipmentSlot slot)
            => _applied.TryGetValue(slot, out var list) ? list.Count : 0;

        public void Apply(
              EquipmentSlot slot
            , ItemId id
            , in CharacterStatHandles handles
            , ref GameStatSystem.Accessor accessor
            , ref GameStatSystem.WorldData worldData
        )
        {
            Remove(slot, ref accessor, ref worldData);

            if (_catalog.Find(id).TryGetValue(out var data) == false)
            {
                return;
            }

            var modifiers = data.Modifiers.Span;

            if (modifiers.Length < 1)
            {
                return;
            }

            if (_applied.TryGetValue(slot, out var list) == false)
            {
                list = new FasterList<StatModifierHandle>(modifiers.Length);
                _applied.TryAdd(slot, list);
            }

            for (var i = 0; i < modifiers.Length; i++)
            {
                ref readonly var modifier = ref modifiers[i];

                if (handles.Resolve(modifier.Stat).TryGetValue(out var statHandle) == false)
                {
                    continue;
                }

                var value = GameStatSystem.StatModifier.Add(modifier.Value);

                if (modifier.Operation == StatModifierOperation.Multiply)
                {
                    value = GameStatSystem.StatModifier.Multiply(modifier.Value);
                }

                if (accessor.TryAddStatModifier(statHandle, value, out var handle, ref worldData))
                {
                    list.Add(handle);
                }
            }
        }

        public void Remove(
              EquipmentSlot slot
            , ref GameStatSystem.Accessor accessor
            , ref GameStatSystem.WorldData worldData
        )
        {
            if (_applied.TryGetValue(slot, out var list) == false)
            {
                return;
            }

            var span = list.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                accessor.TryRemoveStatModifier(span[i], ref worldData);
            }

            list.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _applied.Dispose();
        }
    }
}
