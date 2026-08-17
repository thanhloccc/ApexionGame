using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using Game.Common;
using Unity.Collections;

namespace Game.Gameplay.Items
{
    [PolyEnumFactoryFor(typeof(Error))]
    public readonly partial struct ItemError
    {
        private readonly FixedString64Bytes _prefix;
        private readonly Error _error;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ItemError(in Error error) : this(error, default)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ItemError(in Error error, in FixedString64Bytes prefix)
        {
            _prefix = prefix;
            _error = error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemError Prefix(in FixedString64Bytes prefix)
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
                    fs.Append((FixedString128Bytes)"An unknown item error has occurred.");
                    return fs;
                }
            }

            public readonly partial record struct UnknownItem(ItemId Id)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The item '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' is not present in the catalog.");
                    return fs;
                }
            }

            public readonly partial record struct KindMismatch(ItemId Id, ItemId.IdKind Expected)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The item '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' is not of the expected kind '");
                    fs.Append((int)Expected);
                    fs.Append((FixedString128Bytes)"'.");
                    return fs;
                }
            }

            public readonly partial record struct SlotNotCompatible(ItemId Id, EquipmentSlot Slot)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The item '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' cannot be equipped into slot '");
                    fs.Append(Slot.ToFixedString());
                    fs.Append((FixedString128Bytes)"'.");
                    return fs;
                }
            }

            public readonly partial record struct SlotEmpty(EquipmentSlot Slot)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The slot '");
                    fs.Append(Slot.ToFixedString());
                    fs.Append((FixedString128Bytes)"' holds no item.");
                    return fs;
                }
            }

            public readonly partial record struct OverWeight(ItemId Id, float Attempted, float Capacity)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"Equipping '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' would carry ");
                    fs.Append(Attempted);
                    fs.Append((FixedString128Bytes)" against a capacity of ");
                    fs.Append(Capacity);
                    fs.Append((FixedString128Bytes)".");
                    return fs;
                }
            }

            public readonly partial record struct TooManyCopies(ItemId Id, int Max)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The item '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' may be equipped at most ");
                    fs.Append(Max);
                    fs.Append((FixedString128Bytes)" time(s) at once.");
                    return fs;
                }
            }

            public readonly partial record struct MagazineOverflow(ItemId Id, int Capacity)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The magazine of '");
                    fs.Append(Id.ToFixedString());
                    fs.Append((FixedString128Bytes)"' holds at most ");
                    fs.Append(Capacity);
                    fs.Append((FixedString128Bytes)" round(s).");
                    return fs;
                }
            }

            public readonly partial record struct NotARangedWeapon(EquipmentSlot Slot)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var fs = Init(prefix);
                    fs.Append((FixedString128Bytes)"The slot '");
                    fs.Append(Slot.ToFixedString());
                    fs.Append((FixedString128Bytes)"' does not hold a ranged weapon.");
                    return fs;
                }
            }
        }
    }
}
