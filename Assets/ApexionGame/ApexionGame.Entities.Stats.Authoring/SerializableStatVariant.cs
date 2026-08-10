using System;
using Unity.Mathematics;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Authoring
{
    /// <summary>
    /// Inspector-authorable stand-in for <see cref="StatVariant"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="StatVariant"/> cannot be authored directly. It is a union: every value field sits
    /// at <c>[FieldOffset(0)]</c>. Unity's serializer ignores explicit layout, so it would write all
    /// nineteen fields as separate entries and, on load, have them overwrite one another — the last
    /// one wins and the value is whatever that happens to be.
    /// <para>
    /// This type stores the value in three plain fields instead, chosen so every type in the
    /// gameplay profile round-trips without loss:
    /// <list type="bullet">
    /// <item><c>_scalar</c> (double) — bool, all 8/16/32-bit integers, half, float, double.</item>
    /// <item><c>_integer</c> (long) — long and ulong, whose range double cannot hold exactly.</item>
    /// <item><c>_vector</c> (float4) — half2/3/4 and float2/3/4.</item>
    /// </list>
    /// </para>
    /// </remarks>
    [Serializable]
    public struct SerializableStatVariant : IEquatable<SerializableStatVariant>
    {
        [SerializeField]
        private StatVariantType _type;

        [SerializeField]
        private double _scalar;

        [SerializeField]
        private long _integer;

        [SerializeField]
        private float4 _vector;

        public readonly StatVariantType Type => _type;

        /// <summary>
        /// Converts the authored value into the runtime union.
        /// </summary>
        public readonly StatVariant ToStatVariant()
            => _type switch {
                StatVariantType.Bool => new StatVariant(_scalar != 0d),
                StatVariantType.SByte => new StatVariant((sbyte)_scalar),
                StatVariantType.Byte => new StatVariant((byte)_scalar),
                StatVariantType.Short => new StatVariant((short)_scalar),
                StatVariantType.UShort => new StatVariant((ushort)_scalar),
                StatVariantType.Int => new StatVariant((int)_scalar),
                StatVariantType.UInt => new StatVariant((uint)_scalar),
                StatVariantType.Long => new StatVariant(_integer),
                StatVariantType.ULong => new StatVariant(unchecked((ulong)_integer)),
                StatVariantType.Half => new StatVariant(math.half((float)_scalar)),
                StatVariantType.Half2 => new StatVariant(math.half2(_vector.xy)),
                StatVariantType.Half3 => new StatVariant(math.half3(_vector.xyz)),
                StatVariantType.Half4 => new StatVariant(math.half4(_vector)),
                StatVariantType.Float => new StatVariant((float)_scalar),
                StatVariantType.Float2 => new StatVariant(_vector.xy),
                StatVariantType.Float3 => new StatVariant(_vector.xyz),
                StatVariantType.Float4 => new StatVariant(_vector),
                StatVariantType.Double => new StatVariant(_scalar),
                _ => new StatVariant(new None()),
            };

        /// <summary>
        /// Captures a runtime value so it can be written back into an asset.
        /// </summary>
        public static SerializableStatVariant From(in StatVariant value)
        {
            var result = new SerializableStatVariant { _type = value.Type };

            switch (value.Type)
            {
                case StatVariantType.Bool: result._scalar = value.Bool ? 1d : 0d; break;
                case StatVariantType.SByte: result._scalar = value.SByte; break;
                case StatVariantType.Byte: result._scalar = value.Byte; break;
                case StatVariantType.Short: result._scalar = value.Short; break;
                case StatVariantType.UShort: result._scalar = value.UShort; break;
                case StatVariantType.Int: result._scalar = value.Int; break;
                case StatVariantType.UInt: result._scalar = value.UInt; break;
                case StatVariantType.Long: result._integer = value.Long; break;
                case StatVariantType.ULong: result._integer = unchecked((long)value.ULong); break;
                case StatVariantType.Half: result._scalar = (float)value.Half; break;
                case StatVariantType.Half2: result._vector = new float4((float2)value.Half2, 0f, 0f); break;
                case StatVariantType.Half3: result._vector = new float4((float3)value.Half3, 0f); break;
                case StatVariantType.Half4: result._vector = (float4)value.Half4; break;
                case StatVariantType.Float: result._scalar = value.Float; break;
                case StatVariantType.Float2: result._vector = new float4(value.Float2, 0f, 0f); break;
                case StatVariantType.Float3: result._vector = new float4(value.Float3, 0f); break;
                case StatVariantType.Float4: result._vector = value.Float4; break;
                case StatVariantType.Double: result._scalar = value.Double; break;
            }

            return result;
        }

        public static SerializableStatVariant Of(float value)
            => new() { _type = StatVariantType.Float, _scalar = value };

        public static SerializableStatVariant Of(int value)
            => new() { _type = StatVariantType.Int, _scalar = value };

        public static SerializableStatVariant Of(bool value)
            => new() { _type = StatVariantType.Bool, _scalar = value ? 1d : 0d };

        public static SerializableStatVariant Of(float4 value)
            => new() { _type = StatVariantType.Float4, _vector = value };

        public readonly bool Equals(SerializableStatVariant other)
            => _type == other._type
            && _scalar.Equals(other._scalar)
            && _integer == other._integer
            && _vector.Equals(other._vector)
            ;

        public readonly override bool Equals(object obj)
            => obj is SerializableStatVariant other && Equals(other);

        public readonly override int GetHashCode()
            => ((int)_type * 397) ^ _scalar.GetHashCode() ^ _integer.GetHashCode() ^ _vector.GetHashCode();

        public readonly override string ToString()
            => $"{_type}({ToStatVariant()})";
    }
}
