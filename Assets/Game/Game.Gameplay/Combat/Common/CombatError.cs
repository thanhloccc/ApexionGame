using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using Game.Common;
using Game.Gameplay.Items;
using Game.Gameplay.Weapons;
using Unity.Collections;

namespace Game.Gameplay.Combat
{
    [PolyEnumFactoryFor(typeof(Error))]
    public readonly partial struct CombatError
    {
        private readonly FixedString64Bytes _prefix;
        private readonly Error _error;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CombatError(in Error error) : this(error, default)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CombatError(in Error error, in FixedString64Bytes prefix)
        {
            _prefix = prefix;
            _error = error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CombatError Prefix(in FixedString64Bytes prefix)
            => new(_error, prefix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedString512Bytes ToFixedString()
            => _error.ToMessage(_prefix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => _error.ToMessage(_prefix).ToString();

        [PolyEnumStruct]
        readonly partial struct Error
        {
            private static FixedString512Bytes Init(in FixedString64Bytes prefix)
            {
                FixedString512Bytes fs = default;

                if (prefix.IsEmpty == false)
                {
                    fs.Append('[');
                    fs.Append(prefix);
                    fs.Append(']');
                    fs.Append(' ');
                }

                return fs;
            }

            partial interface IEnumCase
            {
                FixedString512Bytes ToMessage(in FixedString64Bytes prefix);
            }

            public readonly partial struct Undefined
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"An unknown combat error has occurred.");
                    return fs;
                }
            }

            public readonly partial record struct UnknownCombatant(CombatantId Id)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"No combatant with id ");
                    fs.Append(Id.Value);
                    fs.Append((FixedString128Bytes)" exists in this world.");
                    return fs;
                }
            }

            public readonly partial record struct CapacityReached(int Capacity)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The world already holds its maximum of ");
                    fs.Append(Capacity);
                    fs.Append((FixedString128Bytes)" combatant(s).");
                    return fs;
                }
            }

            public readonly partial record struct EquipRejected(
                  CombatantId Combatant
                , EquipmentSlot Slot
                , ItemId Item
                , ItemError Reason
            )
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"Combatant ");
                    fs.Append(Combatant.Value);
                    fs.Append((FixedString128Bytes)" could not equip '");
                    fs.Append(Item.ToFixedString());
                    fs.Append((FixedString128Bytes)"' into '");
                    fs.Append(Slot.ToFixedString());
                    fs.Append((FixedString128Bytes)"': ");
                    fs.Append(Reason.ToFixedString());
                    return fs;
                }
            }

            public readonly partial record struct UnequipRejected(
                  CombatantId Combatant
                , EquipmentSlot Slot
                , ItemError Reason
            )
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"Combatant ");
                    fs.Append(Combatant.Value);
                    fs.Append((FixedString128Bytes)" could not unequip '");
                    fs.Append(Slot.ToFixedString());
                    fs.Append((FixedString128Bytes)"': ");
                    fs.Append(Reason.ToFixedString());
                    return fs;
                }
            }

            /// <summary>
            /// The loadout accepted the item but its weapon machine refused to attach.
            /// </summary>
            /// <remarks>
            /// Whoever raises this must already have rolled the loadout back — a slot holding a
            /// weapon with no machine behind it accepts attack requests and silently does nothing.
            /// </remarks>
            public readonly partial record struct WeaponAttachFailed(
                  CombatantId Combatant
                , EquipmentSlot Slot
                , ItemId Item
                , WeaponError Reason
            )
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"Combatant ");
                    fs.Append(Combatant.Value);
                    fs.Append((FixedString128Bytes)" equipped '");
                    fs.Append(Item.ToFixedString());
                    fs.Append((FixedString128Bytes)"' into '");
                    fs.Append(Slot.ToFixedString());
                    fs.Append((FixedString128Bytes)"' but its weapon machine failed to attach: ");
                    fs.Append(Reason.ToFixedString());
                    return fs;
                }
            }

            public readonly partial record struct AlreadyDead(CombatantId Id)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"Combatant ");
                    fs.Append(Id.Value);
                    fs.Append((FixedString128Bytes)" is already dead.");
                    return fs;
                }
            }
        }
    }
}
