using System.Runtime.CompilerServices;
using Unity.Collections;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// The stat system behind the RTS sample: one store's worth of types.
    /// </summary>
    /// <remarks>
    /// <b>In a real project you would declare one <c>[StatSystem]</c> for the whole game</b> and hang
    /// every collection off it — units, buildings, upgrades, the lot. Only collections sharing a system
    /// can have modifiers between them, and this sample needs exactly that: every unit's Attack reads a
    /// stat that lives on its team's node.
    /// <para>
    /// <c>StatDataSize.Size8</c> is the byte budget for one stat's value. Every stat here is a float
    /// pair (base + current, 4 + 4 bytes), so 8 is exact. An analyzer errors on any <c>[StatData]</c>
    /// that does not fit.
    /// </para>
    /// </remarks>
    [StatSystem(StatDataSize.Size8, StatUserDataSize.Size2)]
    public static partial class RtsStatSystem { }

    public static partial class RtsStatSystem
    {
        public partial struct StatModifier
        {
            public enum Kind : byte
            {
                /// <summary>Flat. Holy Shield's <c>+15 armor</c>.</summary>
                Add,

                /// <summary>Factor. Bloodlust's <c>×1.4 attack</c>, Slow's <c>×1.5 attack interval</c>.</summary>
                Multiply,

                /// <summary>
                /// Add another stat's current value. Every unit's link to its team's shared bonus node is
                /// one of these — the most-used kind in the sample, by a wide margin.
                /// </summary>
                AddFromStat,

                /// <summary>
                /// Add a fraction of another stat. How a hero's aura is expressed: the team node's
                /// <c>AttackBonus</c> reads 25% of the hero's <c>AuraPower</c>, so levelling the hero
                /// strengthens the whole army.
                /// </summary>
                AddFractionOfStat,

                /// <summary>Cap at another stat's current value — <c>Hp &lt;= MaxHp</c>.</summary>
                ClampMaxFromStat,

                /// <summary>Floor at a constant.</summary>
                /// <remarks>
                /// Not cosmetic. Combat swings once per attack interval, so the tick loop is
                /// <c>while (cooldown &lt;= 0) { swing(); cooldown += interval; }</c> — an interval
                /// approaching zero means an unbounded number of swings in one tick. Stacked haste
                /// (Bloodlust is <c>×0.8</c>) gets there geometrically. The floor is what keeps that loop
                /// terminating.
                /// <para>
                /// Stacked <i>slows</i> need no protection: they push the interval up, and a very slow
                /// unit is merely useless.
                /// </para>
                /// </remarks>
                ClampMinConstant,
            }

            public Kind kind;

            /// <summary>
            /// <see cref="Kind.Add"/>: the amount. <see cref="Kind.Multiply"/>: the factor.
            /// <see cref="Kind.AddFractionOfStat"/>: the fraction. <see cref="Kind.ClampMinConstant"/>:
            /// the floor.
            /// </summary>
            public StatVariant value;

            public StatHandle observedStat;

            private uint _id;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier Add(float amount)
                => new() { kind = Kind.Add, value = new StatVariant(amount) };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier Multiply(float factor)
                => new() { kind = Kind.Multiply, value = new StatVariant(factor) };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier AddFrom(StatHandle observed)
                => new() { kind = Kind.AddFromStat, observedStat = observed };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier AddFractionOf(StatHandle observed, float fraction)
                => new() {
                    kind = Kind.AddFractionOfStat,
                    value = new StatVariant(fraction),
                    observedStat = observed,
                };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier ClampMaxFrom(StatHandle observed)
                => new() { kind = Kind.ClampMaxFromStat, observedStat = observed };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static StatModifier ClampMin(float floor)
                => new() { kind = Kind.ClampMinConstant, value = new StatVariant(floor) };

            /// <summary>True for the kinds that read <see cref="observedStat"/>.</summary>
            public readonly bool ObservesAnotherStat
                => kind is Kind.AddFromStat or Kind.AddFractionOfStat or Kind.ClampMaxFromStat;

            readonly partial void GetIdInternal(ref uint id)
                => id = _id;

            partial void SetIdInternal(uint value)
                => _id = value;

            /// <remarks>
            /// Declare every stat this modifier reads. Miss one and the affected stat will not be
            /// recalculated when that input changes — stale value, no error anywhere. Driven off
            /// <see cref="ObservesAnotherStat"/> so a new kind cannot be added without deciding it.
            /// </remarks>
            readonly partial void AddObservedStatsToListInternal(NativeList<StatHandle> observedStatHandles)
            {
                if (ObservesAnotherStat)
                {
                    observedStatHandles.Add(observedStat);
                }
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

                    case Kind.ClampMinConstant:
                    {
                        stack.min = stack.hasMin ? StatVariant.Max(stack.min, value) : value;
                        stack.hasMin = true;
                        return;
                    }
                }

                // A hero died mid-battle: the observed owner may be gone. Contribute nothing rather than
                // fail, and raise the trigger so the match can prune the modifier instead of paying for a
                // failed lookup on every recalculation for the rest of the match.
                if (reader.TryGetStatValue(observedStat, out var observed) == false)
                {
                    shouldProduceModifierTriggerEvent = true;
                    return;
                }

                var observedValue = observed.GetCurrentValueOrDefault(new StatVariant(0f));

                switch (kind)
                {
                    case Kind.AddFromStat:
                    {
                        stack.add += observedValue;
                        return;
                    }

                    case Kind.AddFractionOfStat:
                    {
                        stack.add += observedValue * value;
                        return;
                    }

                    case Kind.ClampMaxFromStat:
                    {
                        stack.max = stack.hasMax ? StatVariant.Min(stack.max, observedValue) : observedValue;
                        stack.hasMax = true;
                        return;
                    }
                }
            }

            /// <remarks>
            /// A saved match restores owners into different slots, and in this sample almost every
            /// modifier holds a handle to a team node — get this wrong and every unit reads the enemy's
            /// bonuses.
            /// </remarks>
            partial void RemapObservedStatsInternal(in StatOwnerRemap remap)
            {
                if (ObservesAnotherStat)
                {
                    observedStat = remap.RemapOrNull(observedStat);
                }
            }
        }
    }

    public static partial class RtsStatSystem
    {
        public partial struct StatModifier
        {
            public partial struct Stack
            {
                public StatVariant add;
                public StatVariant multiply;
                public StatVariant max;
                public StatVariant min;
                public bool hasMax;
                public bool hasMin;

                /// <remarks>
                /// Identity elements come from the stat's own type rather than being hard-coded to
                /// <c>0f</c> / <c>1f</c>, so the day someone declares an int stat nothing breaks quietly.
                /// </remarks>
                partial void ResetInternal(in Stat stat)
                {
                    var type = stat.ValuePair.Type;
                    add = type.ZeroVariant();
                    multiply = type.OneVariant();
                    max = type.ZeroVariant();
                    min = type.ZeroVariant();
                    hasMax = false;
                    hasMin = false;
                }

                /// <remarks>
                /// Flat before percentage, then the cap, then the floor. The floor last means it wins when
                /// the two disagree — a stat with <c>max 0</c> and <c>min 0.25</c> lands on 0.25. That is
                /// the safer way round: floors exist to keep a value out of a range that breaks something
                /// downstream.
                /// </remarks>
                partial void ApplyInternal(in StatVariant baseValue, ref StatVariant currentValue)
                {
                    currentValue = (baseValue + add) * multiply;

                    if (hasMax)
                    {
                        currentValue = StatVariant.Min(currentValue, max);
                    }

                    if (hasMin)
                    {
                        currentValue = StatVariant.Max(currentValue, min);
                    }
                }
            }
        }
    }
}
