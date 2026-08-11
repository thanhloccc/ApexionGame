using System;
using System.Runtime.CompilerServices;
using EncosyTower.Common;
using Unity.Collections;

namespace ApexionGame.Entities.Stats.Tests
{
    /// <summary>
    /// Hand-written stand-in for what StatSystemGenerator will emit in phase 3.
    /// Float-only and deliberately minimal: it exists so the phase 2 algorithm can be exercised
    /// before any codegen exists, and later as a reference to diff generated output against.
    /// </summary>
    public struct TestValuePair : IStatValuePair, IEquatable<TestValuePair>
    {
        public StatVariant baseValue;
        public StatVariant currentValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TestValuePair(float baseValue, float currentValue)
        {
            this.baseValue = new StatVariant(baseValue);
            this.currentValue = new StatVariant(currentValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TestValuePair(float value) : this(value, value)
        { }

        public readonly StatVariantType Type => currentValue.Type;

        public readonly bool IsPair => true;

        public readonly float Current => currentValue.Float;

        public readonly float Base => baseValue.Float;

        public readonly StatVariant GetBaseValueOrDefault(in StatVariant defaultValue = default)
            => baseValue.Type == StatVariantType.None ? defaultValue : baseValue;

        public readonly StatVariant GetCurrentValueOrDefault(in StatVariant defaultValue = default)
            => currentValue.Type == StatVariantType.None ? defaultValue : currentValue;

        public bool TrySetBaseValue(in StatVariant value)
        {
            baseValue = value;
            return true;
        }

        public bool TrySetCurrentValue(in StatVariant value)
        {
            currentValue = value;
            return true;
        }

        public bool TrySetValues(in StatVariant baseValue, in StatVariant currentValue)
        {
            this.baseValue = baseValue;
            this.currentValue = currentValue;
            return true;
        }

        public readonly bool Equals(TestValuePair other)
            => baseValue.Equals(other.baseValue) && currentValue.Equals(other.currentValue);

        public readonly override bool Equals(object obj)
            => obj is TestValuePair other && Equals(other);

        public readonly override int GetHashCode()
            => baseValue.GetHashCode() ^ currentValue.GetHashCode();

        public readonly override string ToString()
            => $"(base {Base}, current {Current})";
    }

    public struct TestComposer : IStatValuePairComposer<TestValuePair>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TestValuePair Compose(bool isPair, in StatVariant baseValue, in StatVariant currentValue)
            => new() { baseValue = baseValue, currentValue = currentValue };
    }

    public struct TestStat : IStat<TestValuePair>
    {
        private ModifierRange _modifierRange;
        private ObserverRange _observerRange;
        private TestValuePair _valuePair;
        private ByteBool _produceChangeEvents;
        private uint _userData;

        public ModifierRange ModifierRange
        {
            readonly get => _modifierRange;
            set => _modifierRange = value;
        }

        public ObserverRange ObserverRange
        {
            readonly get => _observerRange;
            set => _observerRange = value;
        }

        public bool ProduceChangeEvents
        {
            readonly get => _produceChangeEvents;
            set => _produceChangeEvents = value;
        }

        public TestValuePair ValuePair
        {
            readonly get => _valuePair;
            set => _valuePair = value;
        }

        public uint UserData
        {
            readonly get => _userData;
            set => _userData = value;
        }

        public readonly int UserDataSize => 4;

        public readonly StatVariant GetBaseValueOrDefault(in StatVariant defaultValue = default)
            => _valuePair.GetBaseValueOrDefault(defaultValue);

        public readonly StatVariant GetCurrentValueOrDefault(in StatVariant defaultValue = default)
            => _valuePair.GetCurrentValueOrDefault(defaultValue);

        public bool TrySetBaseValue(in StatVariant value)
            => _valuePair.TrySetBaseValue(value);

        public bool TrySetCurrentValue(in StatVariant value)
            => _valuePair.TrySetCurrentValue(value);

        public bool TrySetValues(in StatVariant baseValue, in StatVariant currentValue)
            => _valuePair.TrySetValues(baseValue, currentValue);
    }

    public struct TestModifierStack : IStatModifierStack<TestValuePair, TestStat>
    {
        public StatVariant add;
        public StatVariant mul;

        public void Reset(in TestStat stat)
        {
            add = new StatVariant(0f);
            mul = new StatVariant(1f);
        }

        public readonly void Apply(in StatVariant baseValue, ref StatVariant currentValue)
        {
            currentValue = (baseValue + add) * mul;
        }
    }

    public struct TestModifier : IStatModifier<TestValuePair, TestStat, TestModifierStack>
    {
        public enum Kind : byte
        {
            Add,
            Multiply,
            AddFromStat,
        }

        public Kind kind;
        public StatVariant value;
        public StatHandle observedStat;

        private uint _id;

        public uint Id
        {
            readonly get => _id;
            set => _id = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TestModifier Add(float amount)
            => new() { kind = Kind.Add, value = new StatVariant(amount) };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TestModifier Multiply(float factor)
            => new() { kind = Kind.Multiply, value = new StatVariant(factor) };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TestModifier AddFrom(StatHandle observed)
            => new() { kind = Kind.AddFromStat, observedStat = observed };

        public readonly void AddObservedStatsToList(NativeList<StatHandle> observedStatHandles)
        {
            if (kind == Kind.AddFromStat)
            {
                observedStatHandles.Add(observedStat);
            }
        }

        public readonly void Apply(
              StatReader<TestValuePair, TestStat> reader
            , ref TestModifierStack stack
            , out bool shouldProduceModifierTriggerEvent
        )
        {
            shouldProduceModifierTriggerEvent = false;

            switch (kind)
            {
                case Kind.Add:
                {
                    stack.add += value;
                    break;
                }

                case Kind.Multiply:
                {
                    stack.mul *= value;
                    break;
                }

                case Kind.AddFromStat:
                {
                    if (reader.TryGetStatValue(observedStat, out var other))
                    {
                        stack.add += other.GetCurrentValueOrDefault(new StatVariant(0f));
                    }

                    break;
                }
            }
        }

        public void RemapObservedStats(in StatOwnerRemap remap)
        {
            if (kind == Kind.AddFromStat)
            {
                observedStat = remap.RemapOrNull(observedStat);
            }
        }
    }

    public struct TestObserver : IStatObserver
    {
        private StatHandle _handle;

        public StatHandle ObserverHandle
        {
            readonly get => _handle;
            set => _handle = value;
        }
    }
}
