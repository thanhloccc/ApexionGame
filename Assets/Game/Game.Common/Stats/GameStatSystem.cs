using System.Runtime.CompilerServices;
using ApexionGame.Entities.Stats;
using Unity.Collections;

namespace Game.Common
{
    [StatSystem(StatDataSize.Size8, StatUserDataSize.Size2)]
    public static partial class GameStatSystem { }

    public static partial class GameStatSystem
    {
        public partial struct StatModifier
        {
            public enum Kind : byte
            {
                Add,
                Multiply,
            }

            public Kind kind;

            public StatVariant value;

            private uint _id;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier Add(float amount)
                => new() { kind = Kind.Add, value = new StatVariant(amount) };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier Multiply(float factor)
                => new() { kind = Kind.Multiply, value = new StatVariant(factor) };

            readonly partial void GetIdInternal(ref uint id)
                => id = _id;

            partial void SetIdInternal(uint value)
                => _id = value;

            readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observedStatHandles)
            {
            }

            partial void ApplyInternal(
                  Reader reader
                , ref Stack stack
                , ref bool shouldProduceModifierTriggerEvent
            )
            {
                switch (kind)
                {
                    case Kind.Add:
                    {
                        stack.add += value;
                        return;
                    }

                    case Kind.Multiply:
                    {
                        stack.multiply *= value;
                        return;
                    }
                }
            }

            partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
            {
            }
        }
    }

    public static partial class GameStatSystem
    {
        public partial struct StatModifier
        {
            public partial struct Stack
            {
                public StatVariant add;
                public StatVariant multiply;

                partial void ResetInternal(in Stat stat)
                {
                    var type = stat.ValuePair.Type;
                    add = type.ZeroVariant();
                    multiply = type.OneVariant();
                }

                partial void ApplyInternal(in StatVariant baseValue, ref StatVariant currentValue)
                {
                    currentValue = (baseValue + add) * multiply;
                }
            }
        }
    }
}
