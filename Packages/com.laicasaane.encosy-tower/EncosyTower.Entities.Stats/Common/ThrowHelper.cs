using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Entities.Stats
{
    public sealed class StatVariantTypeException : Exception
    {
        public StatVariantTypeException(string message) : base(message) { }
    }

    public sealed class StatVariantOperatorException : Exception
    {
        public StatVariantOperatorException(string message) : base(message) { }
    }

    public sealed class StatDataTypeException : Exception
    {
        public StatDataTypeException(string message) : base(message) { }
    }

    internal static class ThrowHelper
    {
        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        public static void ThrowIfMismatchedOperatorTypes(StatVariantType lhs, StatVariantType rhs, string op)
        {
            if (lhs != rhs)
            {
                throw CreateException(lhs, rhs, op);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static StatVariantTypeException CreateException(StatVariantType lhs, StatVariantType rhs, string op)
                => new(
                    $"The operands 'lhs' and 'rhs' of the '{op}' operator are of different types, respectively " +
                    $"'{lhs.ToStringFast()}' and '{rhs.ToStringFast()}'."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        public static void ThrowIfMismatchedFunctionTypes(StatVariantType a, StatVariantType b, string func)
        {
            if (a != b)
            {
                throw CreateException(a, b, func);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static StatVariantTypeException CreateException(StatVariantType a, StatVariantType b, string func)
                => new(
                    $"The arguments 'a' and 'b' of the '{func}' function are of different types, respectively " +
                    $"'{a.ToStringFast()}' and '{b.ToStringFast()}'."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        public static void ThrowIfMismatchedClampTypes(
            StatVariantType value, StatVariantType lowerBound, StatVariantType upperBound
        )
        {
            if (value != lowerBound || lowerBound != upperBound)
            {
                throw CreateException(value, lowerBound, upperBound);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static StatVariantTypeException CreateException(
                StatVariantType value, StatVariantType lowerBound, StatVariantType upperBound
            )
            {
                return new(
                    $"The arguments 'value', 'lowerBound' and 'upperBound' of the 'Clamp' function " +
                    $"are of different types, respectively " +
                    $"'{value.ToStringFast()}', '{lowerBound.ToStringFast()}', and '{upperBound.ToStringFast()}'."
                );
            }
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        public static void ThrowIfExplicitlyConvertToWrongType(StatVariantType type, StatVariantType requiredType)
        {
            if (type != requiredType)
            {
                throw CreateException(type, requiredType);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static StatVariantTypeException CreateException(StatVariantType type, StatVariantType requiredType)
                => new(
                    $"Cannot explicitly convert a value of type '{type.ToStringFast()}' " +
                    $"to type '{requiredType.ToStringFast()}'."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        public static void ThrowIfDestinationTypeMismatch(StatVariantType type, StatVariantType destinationType)
        {
            if (type != destinationType)
            {
                throw CreateException(type, destinationType);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static StatVariantTypeException CreateException(StatVariantType type, StatVariantType destinationType)
                => new(
                    $"Cannot set a value of type '{type.ToStringFast()}' to type '{destinationType.ToStringFast()}'."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        public static void ThrowIfUnsupportedType(StatVariantType type)
        {
            if (type.IsDefined() == false)
            {
                throw CreateException(type);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static StatVariantTypeException CreateException(StatVariantType type)
                => new($"Value type '{type.ToStringFast()}' is not supported.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        public static void ThrowUnsupportedType(StatVariantType type)
            => throw new StatVariantTypeException($"Value type '{type.ToStringFast()}' is not supported.");

        [HideInCallstack, StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StatVariantTypeException UnsupportedTypeException(StatVariantType type)
            => new($"Value type '{type.ToStringFast()}' is not supported.");

        [HideInCallstack, StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StatVariantOperatorException OperatorException(string op, StatVariantType type)
            => new($"Cannot apply operator '{op}' to stat value of type '{type.ToStringFast()}'.");

        [HideInCallstack, StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StatVariantOperatorException FuncException(string func, StatVariantType type)
            => new($"Cannot use function '{func}' with stat value of type '{type.ToStringFast()}'.");

        [HideInCallstack, StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StatVariantOperatorException UnaryOperatorException(string op, StatVariantType type)
            => new($"Cannot apply unary operator '{op}' to stat value of type '{type.ToStringFast()}'.");

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        internal static void ThrowIfStatWorldDataIsNotCreated([DoesNotReturnIf(false)] bool isCreated)
        {
            if (isCreated == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentException CreateException()
                => new("Stat World data is not created", "worldData");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        internal static void ThrowIfPairsMismatch<TStatData>(StatVariantType valuePairType, TStatData statData)
            where TStatData : IStatData
        {
            if (valuePairType != statData.ValueType)
            {
                throw CreateException(valuePairType, statData);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static StatDataTypeException CreateException(StatVariantType valuePairType, TStatData statData)
            {
                return statData.IsValuePair
                    ? new(
                        $"Stat data '{typeof(TStatData)}' requires value of type '{statData.ValueType.ToStringFast()}' " +
                        $"but receives a ValuePair of type '{valuePairType.ToStringFast()}'."
                    )
                    : new(
                        $"Stat data '{typeof(TStatData)}' requires a single value of type '{statData.ValueType.ToStringFast()}' " +
                        $"but receives a value of type '{valuePairType.ToStringFast()}'."
                    );
            }
        }
    }
}
