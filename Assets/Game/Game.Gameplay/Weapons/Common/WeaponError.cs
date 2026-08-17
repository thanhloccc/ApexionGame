using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using Game.Common;
using Unity.Collections;

namespace Game.Gameplay.Weapons
{
    [PolyEnumFactoryFor(typeof(Error))]
    public readonly partial struct WeaponError
    {
        private readonly FixedString64Bytes _prefix;
        private readonly Error _error;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private WeaponError(in Error error) : this(error, default)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private WeaponError(in Error error, in FixedString64Bytes prefix)
        {
            _prefix = prefix;
            _error = error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeaponError Prefix(in FixedString64Bytes prefix)
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
                    fs.Append((FixedString128Bytes)"An unknown weapon error has occurred.");
                    return fs;
                }
            }

            public readonly partial record struct NoWeaponInSlot(EquipmentSlot Slot)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The slot '");
                    fs.Append(Slot.ToFixedString());
                    fs.Append((FixedString128Bytes)"' holds no weapon.");
                    return fs;
                }
            }

            public readonly partial record struct NotAWeapon(ItemId Id)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The item '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' is neither a melee nor a ranged weapon.");
                    return fs;
                }
            }

            public readonly partial record struct NonPositiveRateOfFire(ItemId Id, float RoundsPerMinute)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The weapon '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' declares a rate of fire of ");
                    fs.Append(RoundsPerMinute);
                    fs.Append((FixedString128Bytes)", which cannot produce a firing interval.");
                    return fs;
                }
            }

            public readonly partial record struct NonPositiveMagazine(ItemId Id, int Capacity)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The weapon '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' declares a magazine capacity of ");
                    fs.Append(Capacity);
                    fs.Append((FixedString128Bytes)".");
                    return fs;
                }
            }

            public readonly partial record struct MachineBuildFailed(ItemId Id)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The state machine for weapon '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' could not be built.");
                    return fs;
                }
            }
        }
    }
}
