using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace ApexionGame.HFSM.Internals
{
    /// <summary>
    /// Reads an enum member's ordinal without boxing, for any underlying integral width.
    /// </summary>
    /// <remarks>
    /// Enum members used as states or triggers are expected to be non-negative. A member of a
    /// <see langword="byte"/>- or <see langword="ushort"/>-backed enum reads as unsigned, so a
    /// negative member of such an enum would map onto the wrong ordinal.
    /// </remarks>
    internal static class EnumOrdinal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Of<TEnum>(TEnum value)
            where TEnum : unmanaged, Enum
        {
            return UnsafeUtility.SizeOf<TEnum>() switch {
                1 => UnsafeUtility.As<TEnum, byte>(ref value),
                2 => UnsafeUtility.As<TEnum, ushort>(ref value),
                8 => (int)UnsafeUtility.As<TEnum, long>(ref value),
                _ => UnsafeUtility.As<TEnum, int>(ref value),
            };
        }

        /// <summary>
        /// The inverse of <see cref="Of{TEnum}"/>, so a baked node index can be turned back into the
        /// enum member the caller declared it with.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum From<TEnum>(int ordinal)
            where TEnum : unmanaged, Enum
        {
            TEnum result = default;

            switch (UnsafeUtility.SizeOf<TEnum>())
            {
                case 1: UnsafeUtility.As<TEnum, byte>(ref result) = (byte)ordinal; break;
                case 2: UnsafeUtility.As<TEnum, ushort>(ref result) = (ushort)ordinal; break;
                case 8: UnsafeUtility.As<TEnum, long>(ref result) = ordinal; break;
                default: UnsafeUtility.As<TEnum, int>(ref result) = ordinal; break;
            }

            return result;
        }
    }
}
