using System;
using System.Runtime.CompilerServices;
using ApexionGame.HFSM.Internals;
using EncosyTower.Common;
using EncosyTower.Types;
using Unity.Collections;

namespace ApexionGame.HFSM
{
    /// <summary>
    /// Identifies one member of one trigger enum.
    /// </summary>
    /// <remarks>
    /// The ordinal alone is not an identity: <c>EnemyTrigger.Staggered</c> and
    /// <c>BossTrigger.Staggered</c> are both ordinal 0, and a boss's stun must not fire an enemy's
    /// transition. Pairing the ordinal with <see cref="Types.TypeId"/> separates them for eight bytes.
    /// <para>
    /// Trigger enum members are expected to be non-negative. Members of a <see langword="byte"/>- or
    /// <see langword="ushort"/>-backed enum read as unsigned; a negative member of such an enum
    /// would map onto the wrong ordinal.
    /// </para>
    /// </remarks>
    public readonly struct TriggerId : IEquatable<TriggerId>, IIsValid
    {
        /// <summary>
        /// The absence of a trigger. A transition carrying this is polled, not triggered.
        /// </summary>
        public static readonly TriggerId None = default;

        public readonly TypeId Type;
        public readonly int Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TriggerId(TypeId type, int value)
        {
            Type = type;
            Value = value;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Type != TypeId.Undefined;
        }

        /// <summary>
        /// Interns <paramref name="trigger"/> into an id that cannot collide with a member of a
        /// different enum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TriggerId Of<TTrigger>(TTrigger trigger)
            where TTrigger : unmanaged, Enum
        {
            return new(TriggerTypeOf<TTrigger>.Id, EnumOrdinal.Of(trigger));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TriggerId other)
            => Value == other.Value && Type.Equals(other.Type);

        public override bool Equals(object obj)
            => obj is TriggerId other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
            => HashCode.Combine(Type, Value);

        public override string ToString()
            => IsValid ? $"Trigger({Type}:{Value})" : "Trigger(None)";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedString64Bytes ToFixedString()
        {
            if (IsValid == false)
            {
                return "Trigger(None)";
            }

            FixedString64Bytes result = "Trigger(";
            result.Append(Type.ToFixedString());
            result.Append(':');
            result.Append(Value);
            result.Append(')');
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TriggerId lhs, TriggerId rhs)
            => lhs.Equals(rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TriggerId lhs, TriggerId rhs)
            => lhs.Equals(rhs) == false;

        /// <summary>
        /// Resolves <see cref="Types.TypeId"/> once per trigger enum instead of once per
        /// <see cref="Of{TTrigger}"/> call.
        /// </summary>
        private static class TriggerTypeOf<TTrigger>
            where TTrigger : unmanaged, Enum
        {
            public static readonly TypeId Id = (TypeId)Type<TTrigger>.Id;
        }
    }
}
