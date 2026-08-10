using System;
using Unity.Mathematics;

namespace ApexionGame.Entities.Stats
{
    [Serializable]
    public partial struct StatVariant
    {
        public static StatVariant operator +(in StatVariant rhs)
        {
            ThrowHelper.ThrowIfUnsupportedType(rhs.Type);

            return rhs.Type switch {
                StatVariantType.Byte   => new((byte)+rhs.Byte),
                StatVariantType.Double => new((double)+rhs.Double),
                StatVariantType.Float  => new((float)+rhs.Float),
                StatVariantType.Float2 => new(+rhs.Float2),
                StatVariantType.Float3 => new(+rhs.Float3),
                StatVariantType.Float4 => new(+rhs.Float4),
                StatVariantType.Half   => new(math.half(+(float)rhs.Half)),
                StatVariantType.Half2  => new(math.half2(+(float2)rhs.Half2)),
                StatVariantType.Half3  => new(math.half3(+(float3)rhs.Half3)),
                StatVariantType.Half4  => new(math.half4(+(float4)rhs.Half4)),
                StatVariantType.Int    => new((int)+rhs.Int),
                StatVariantType.Long   => new((long)+rhs.Long),
                StatVariantType.SByte  => new((sbyte)+rhs.SByte),
                StatVariantType.Short  => new((short)+rhs.Short),
                StatVariantType.UInt   => new((uint)+rhs.UInt),
                StatVariantType.ULong  => new((ulong)+rhs.ULong),
                StatVariantType.UShort => new((ushort)+rhs.UShort),
                _                      => throw ThrowHelper.UnaryOperatorException("+", rhs.Type),
            };
        }

        public static StatVariant operator -(in StatVariant rhs)
        {
            ThrowHelper.ThrowIfUnsupportedType(rhs.Type);

            return rhs.Type switch {
                StatVariantType.Double => new((double)-rhs.Double),
                StatVariantType.Float  => new((float)-rhs.Float),
                StatVariantType.Float2 => new(-rhs.Float2),
                StatVariantType.Float3 => new(-rhs.Float3),
                StatVariantType.Float4 => new(-rhs.Float4),
                StatVariantType.Half   => new(math.half(-(float)rhs.Half)),
                StatVariantType.Half2  => new(math.half2(-(float2)rhs.Half2)),
                StatVariantType.Half3  => new(math.half3(-(float3)rhs.Half3)),
                StatVariantType.Half4  => new(math.half4(-(float4)rhs.Half4)),
                StatVariantType.Int    => new((int)-rhs.Int),
                StatVariantType.Long   => new((long)-rhs.Long),
                StatVariantType.SByte  => new((sbyte)-rhs.SByte),
                StatVariantType.Short  => new((short)-rhs.Short),
                _                      => throw ThrowHelper.UnaryOperatorException("-", rhs.Type),
            };
        }

        public static StatVariant operator +(in StatVariant lhs, in StatVariant rhs)
        {
            ThrowHelper.ThrowIfMismatchedOperatorTypes(lhs.Type, rhs.Type, "+");
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Byte   => new((byte)(lhs.Byte + rhs.Byte)),
                StatVariantType.Double => new((double)(lhs.Double + rhs.Double)),
                StatVariantType.Float  => new((float)(lhs.Float + rhs.Float)),
                StatVariantType.Float2 => new(lhs.Float2 + rhs.Float2),
                StatVariantType.Float3 => new(lhs.Float3 + rhs.Float3),
                StatVariantType.Float4 => new(lhs.Float4 + rhs.Float4),
                StatVariantType.Half   => new(math.half((float)lhs.Half + (float)rhs.Half)),
                StatVariantType.Half2  => new(math.half2((float2)lhs.Half2 + (float2)rhs.Half2)),
                StatVariantType.Half3  => new(math.half3((float3)lhs.Half3 + (float3)rhs.Half3)),
                StatVariantType.Half4  => new(math.half4((float4)lhs.Half4 + (float4)rhs.Half4)),
                StatVariantType.Int    => new((int)(lhs.Int + rhs.Int)),
                StatVariantType.Long   => new((long)(lhs.Long + rhs.Long)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte + rhs.SByte)),
                StatVariantType.Short  => new((short)(lhs.Short + rhs.Short)),
                StatVariantType.UInt   => new((uint)(lhs.UInt + rhs.UInt)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong + rhs.ULong)),
                StatVariantType.UShort => new((ushort)(lhs.UShort + rhs.UShort)),
                _                      => throw ThrowHelper.OperatorException("+", lhs.Type),
            };
        }

        public static StatVariant operator -(in StatVariant lhs, in StatVariant rhs)
        {
            ThrowHelper.ThrowIfMismatchedOperatorTypes(lhs.Type, rhs.Type, "-");
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Byte   => new((byte)(lhs.Byte - rhs.Byte)),
                StatVariantType.Double => new((double)(lhs.Double - rhs.Double)),
                StatVariantType.Float  => new((float)(lhs.Float - rhs.Float)),
                StatVariantType.Float2 => new(lhs.Float2 - rhs.Float2),
                StatVariantType.Float3 => new(lhs.Float3 - rhs.Float3),
                StatVariantType.Float4 => new(lhs.Float4 - rhs.Float4),
                StatVariantType.Half   => new(math.half((float)lhs.Half - (float)rhs.Half)),
                StatVariantType.Half2  => new(math.half2((float2)lhs.Half2 - (float2)rhs.Half2)),
                StatVariantType.Half3  => new(math.half3((float3)lhs.Half3 - (float3)rhs.Half3)),
                StatVariantType.Half4  => new(math.half4((float4)lhs.Half4 - (float4)rhs.Half4)),
                StatVariantType.Int    => new((int)(lhs.Int - rhs.Int)),
                StatVariantType.Long   => new((long)(lhs.Long - rhs.Long)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte - rhs.SByte)),
                StatVariantType.Short  => new((short)(lhs.Short - rhs.Short)),
                StatVariantType.UInt   => new((uint)(lhs.UInt - rhs.UInt)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong - rhs.ULong)),
                StatVariantType.UShort => new((ushort)(lhs.UShort - rhs.UShort)),
                _                      => throw ThrowHelper.OperatorException("-", lhs.Type),
            };
        }

        public static StatVariant operator *(in StatVariant lhs, in StatVariant rhs)
        {
            ThrowHelper.ThrowIfMismatchedOperatorTypes(lhs.Type, rhs.Type, "*");
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Byte   => new((byte)(lhs.Byte * rhs.Byte)),
                StatVariantType.Double => new((double)(lhs.Double * rhs.Double)),
                StatVariantType.Float  => new((float)(lhs.Float * rhs.Float)),
                StatVariantType.Float2 => new(lhs.Float2 * rhs.Float2),
                StatVariantType.Float3 => new(lhs.Float3 * rhs.Float3),
                StatVariantType.Float4 => new(lhs.Float4 * rhs.Float4),
                StatVariantType.Half   => new(math.half((float)lhs.Half * (float)rhs.Half)),
                StatVariantType.Half2  => new(math.half2((float2)lhs.Half2 * (float2)rhs.Half2)),
                StatVariantType.Half3  => new(math.half3((float3)lhs.Half3 * (float3)rhs.Half3)),
                StatVariantType.Half4  => new(math.half4((float4)lhs.Half4 * (float4)rhs.Half4)),
                StatVariantType.Int    => new((int)(lhs.Int * rhs.Int)),
                StatVariantType.Long   => new((long)(lhs.Long * rhs.Long)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte * rhs.SByte)),
                StatVariantType.Short  => new((short)(lhs.Short * rhs.Short)),
                StatVariantType.UInt   => new((uint)(lhs.UInt * rhs.UInt)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong * rhs.ULong)),
                StatVariantType.UShort => new((ushort)(lhs.UShort * rhs.UShort)),
                _                      => throw ThrowHelper.OperatorException("*", lhs.Type),
            };
        }

        public static StatVariant operator /(in StatVariant lhs, in StatVariant rhs)
        {
            ThrowHelper.ThrowIfMismatchedOperatorTypes(lhs.Type, rhs.Type, "/");
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Byte   => new((byte)(lhs.Byte / rhs.Byte)),
                StatVariantType.Double => new((double)(lhs.Double / rhs.Double)),
                StatVariantType.Float  => new((float)(lhs.Float / rhs.Float)),
                StatVariantType.Float2 => new(lhs.Float2 / rhs.Float2),
                StatVariantType.Float3 => new(lhs.Float3 / rhs.Float3),
                StatVariantType.Float4 => new(lhs.Float4 / rhs.Float4),
                StatVariantType.Half   => new(math.half((float)lhs.Half / (float)rhs.Half)),
                StatVariantType.Half2  => new(math.half2((float2)lhs.Half2 / (float2)rhs.Half2)),
                StatVariantType.Half3  => new(math.half3((float3)lhs.Half3 / (float3)rhs.Half3)),
                StatVariantType.Half4  => new(math.half4((float4)lhs.Half4 / (float4)rhs.Half4)),
                StatVariantType.Int    => new((int)(lhs.Int / rhs.Int)),
                StatVariantType.Long   => new((long)(lhs.Long / rhs.Long)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte / rhs.SByte)),
                StatVariantType.Short  => new((short)(lhs.Short / rhs.Short)),
                StatVariantType.UInt   => new((uint)(lhs.UInt / rhs.UInt)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong / rhs.ULong)),
                StatVariantType.UShort => new((ushort)(lhs.UShort / rhs.UShort)),
                _                      => throw ThrowHelper.OperatorException("/", lhs.Type),
            };
        }

        public static StatVariant operator ~(in StatVariant rhs)
        {
            ThrowHelper.ThrowIfUnsupportedType(rhs.Type);

            return rhs.Type switch {
                StatVariantType.Byte   => new((byte)~rhs.Byte),
                StatVariantType.Int    => new((int)~rhs.Int),
                StatVariantType.Long   => new((long)~rhs.Long),
                StatVariantType.SByte  => new((sbyte)~rhs.SByte),
                StatVariantType.Short  => new((short)~rhs.Short),
                StatVariantType.UInt   => new((uint)~rhs.UInt),
                StatVariantType.ULong  => new((ulong)~rhs.ULong),
                StatVariantType.UShort => new((ushort)~rhs.UShort),
                _                      => throw ThrowHelper.UnaryOperatorException("~", rhs.Type),
            };
        }

        public static StatVariant operator !(in StatVariant rhs)
        {
            ThrowHelper.ThrowIfUnsupportedType(rhs.Type);

            return rhs.Type switch {
                StatVariantType.Bool => new(!rhs.Bool),
                _                    => throw ThrowHelper.UnaryOperatorException("!", rhs.Type),
            };
        }

        public static StatVariant operator <<(in StatVariant lhs, int rhs)
        {
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Byte   => new((byte)(lhs.Byte << rhs)),
                StatVariantType.Int    => new((int)(lhs.Int << rhs)),
                StatVariantType.Long   => new((long)(lhs.Long << rhs)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte << rhs)),
                StatVariantType.Short  => new((short)(lhs.Short << rhs)),
                StatVariantType.UInt   => new((uint)(lhs.UInt << rhs)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong << rhs)),
                StatVariantType.UShort => new((ushort)(lhs.UShort << rhs)),
                _                      => throw ThrowHelper.OperatorException("<<", lhs.Type),
            };
        }

        public static StatVariant operator >>(in StatVariant lhs, int rhs)
        {
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Byte   => new((byte)(lhs.Byte >> rhs)),
                StatVariantType.Int    => new((int)(lhs.Int >> rhs)),
                StatVariantType.Long   => new((long)(lhs.Long >> rhs)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte >> rhs)),
                StatVariantType.Short  => new((short)(lhs.Short >> rhs)),
                StatVariantType.UInt   => new((uint)(lhs.UInt >> rhs)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong >> rhs)),
                StatVariantType.UShort => new((ushort)(lhs.UShort >> rhs)),
                _                      => throw ThrowHelper.OperatorException(">>", lhs.Type),
            };
        }

        public static StatVariant operator &(in StatVariant lhs, in StatVariant rhs)
        {
            ThrowHelper.ThrowIfMismatchedOperatorTypes(lhs.Type, rhs.Type, "&");
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Bool   => new(lhs.Bool & rhs.Bool),
                StatVariantType.Byte   => new((byte)(lhs.Byte & rhs.Byte)),
                StatVariantType.Int    => new((int)(lhs.Int & rhs.Int)),
                StatVariantType.Long   => new((long)(lhs.Long & rhs.Long)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte & rhs.SByte)),
                StatVariantType.Short  => new((short)(lhs.Short & rhs.Short)),
                StatVariantType.UInt   => new((uint)(lhs.UInt & rhs.UInt)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong & rhs.ULong)),
                StatVariantType.UShort => new((ushort)(lhs.UShort & rhs.UShort)),
                _                      => throw ThrowHelper.OperatorException("&", lhs.Type),
            };
        }

        public static StatVariant operator |(in StatVariant lhs, in StatVariant rhs)
        {
            ThrowHelper.ThrowIfMismatchedOperatorTypes(lhs.Type, rhs.Type, "|");
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Bool   => new(lhs.Bool | rhs.Bool),
                StatVariantType.Byte   => new((byte)(lhs.Byte | rhs.Byte)),
                StatVariantType.Int    => new((int)(lhs.Int | rhs.Int)),
                StatVariantType.Long   => new((long)(lhs.Long | rhs.Long)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte | rhs.SByte)),
                StatVariantType.Short  => new((short)(lhs.Short | rhs.Short)),
                StatVariantType.UInt   => new((uint)(lhs.UInt | rhs.UInt)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong | rhs.ULong)),
                StatVariantType.UShort => new((ushort)(lhs.UShort | rhs.UShort)),
                _                      => throw ThrowHelper.OperatorException("|", lhs.Type),
            };
        }

        public static StatVariant operator ^(in StatVariant lhs, in StatVariant rhs)
        {
            ThrowHelper.ThrowIfMismatchedOperatorTypes(lhs.Type, rhs.Type, "^");
            ThrowHelper.ThrowIfUnsupportedType(lhs.Type);

            return lhs.Type switch {
                StatVariantType.Bool   => new(lhs.Bool ^ rhs.Bool),
                StatVariantType.Byte   => new((byte)(lhs.Byte ^ rhs.Byte)),
                StatVariantType.Int    => new((int)(lhs.Int ^ rhs.Int)),
                StatVariantType.Long   => new((long)(lhs.Long ^ rhs.Long)),
                StatVariantType.SByte  => new((sbyte)(lhs.SByte ^ rhs.SByte)),
                StatVariantType.Short  => new((short)(lhs.Short ^ rhs.Short)),
                StatVariantType.UInt   => new((uint)(lhs.UInt ^ rhs.UInt)),
                StatVariantType.ULong  => new((ulong)(lhs.ULong ^ rhs.ULong)),
                StatVariantType.UShort => new((ushort)(lhs.UShort ^ rhs.UShort)),
                _                      => throw ThrowHelper.OperatorException("^", lhs.Type),
            };
        }

        /// <remarks>
        /// Supported for scalar and vector types only.
        /// Bool types throw <see cref="StatVariantOperatorException"/>.
        /// </remarks>
        public static StatVariant Min(in StatVariant a, in StatVariant b)
        {
            ThrowHelper.ThrowIfMismatchedFunctionTypes(a.Type, b.Type, "Min");
            ThrowHelper.ThrowIfUnsupportedType(a.Type);

            return a.Type switch {
                StatVariantType.Byte   => new StatVariant((byte)math.min((uint)a.Byte, (uint)b.Byte)),
                StatVariantType.Double => new StatVariant((double)math.min(a.Double, b.Double)),
                StatVariantType.Float  => new StatVariant((float)math.min(a.Float, b.Float)),
                StatVariantType.Float2 => new StatVariant(math.min(a.Float2, b.Float2)),
                StatVariantType.Float3 => new StatVariant(math.min(a.Float3, b.Float3)),
                StatVariantType.Float4 => new StatVariant(math.min(a.Float4, b.Float4)),
                StatVariantType.Half   => new StatVariant(math.half(math.min(a.Half, b.Half))),
                StatVariantType.Half2  => new StatVariant(math.half2(math.min(a.Half2, b.Half2))),
                StatVariantType.Half3  => new StatVariant(math.half3(math.min(a.Half3, b.Half3))),
                StatVariantType.Half4  => new StatVariant(math.half4(math.min(a.Half4, b.Half4))),
                StatVariantType.Int    => new StatVariant((int)math.min(a.Int, b.Int)),
                StatVariantType.Long   => new StatVariant((long)math.min(a.Long, b.Long)),
                StatVariantType.SByte  => new StatVariant((sbyte)math.min(a.SByte, b.SByte)),
                StatVariantType.Short  => new StatVariant((short)math.min(a.Short, b.Short)),
                StatVariantType.UInt   => new StatVariant((uint)math.min(a.UInt, b.UInt)),
                StatVariantType.ULong  => new StatVariant((ulong)math.min(a.ULong, b.ULong)),
                StatVariantType.UShort => new StatVariant((ushort)math.min((uint)a.UShort, (uint)b.UShort)),
                _                      => throw ThrowHelper.FuncException("Min", a.Type),
            };
        }

        /// <remarks>
        /// Supported for scalar and vector types only.
        /// Bool types throw <see cref="StatVariantOperatorException"/>.
        /// </remarks>
        public static StatVariant Max(in StatVariant a, in StatVariant b)
        {
            ThrowHelper.ThrowIfMismatchedFunctionTypes(a.Type, b.Type, "Max");
            ThrowHelper.ThrowIfUnsupportedType(a.Type);

            return a.Type switch {
                StatVariantType.Byte   => new StatVariant((byte)math.max((uint)a.Byte, (uint)b.Byte)),
                StatVariantType.Double => new StatVariant((double)math.max(a.Double, b.Double)),
                StatVariantType.Float  => new StatVariant((float)math.max(a.Float, b.Float)),
                StatVariantType.Float2 => new StatVariant(math.max(a.Float2, b.Float2)),
                StatVariantType.Float3 => new StatVariant(math.max(a.Float3, b.Float3)),
                StatVariantType.Float4 => new StatVariant(math.max(a.Float4, b.Float4)),
                StatVariantType.Half   => new StatVariant(math.half(math.max(a.Half, b.Half))),
                StatVariantType.Half2  => new StatVariant(math.half2(math.max(a.Half2, b.Half2))),
                StatVariantType.Half3  => new StatVariant(math.half3(math.max(a.Half3, b.Half3))),
                StatVariantType.Half4  => new StatVariant(math.half4(math.max(a.Half4, b.Half4))),
                StatVariantType.Int    => new StatVariant((int)math.max(a.Int, b.Int)),
                StatVariantType.Long   => new StatVariant((long)math.max(a.Long, b.Long)),
                StatVariantType.SByte  => new StatVariant((sbyte)math.max(a.SByte, b.SByte)),
                StatVariantType.Short  => new StatVariant((short)math.max(a.Short, b.Short)),
                StatVariantType.UInt   => new StatVariant((uint)math.max(a.UInt, b.UInt)),
                StatVariantType.ULong  => new StatVariant((ulong)math.max(a.ULong, b.ULong)),
                StatVariantType.UShort => new StatVariant((ushort)math.max((uint)a.UShort, (uint)b.UShort)),
                _                      => throw ThrowHelper.FuncException("Max", a.Type),
            };
        }

        /// <remarks>
        /// Supported for scalar and vector types only.
        /// Bool types throw <see cref="StatVariantOperatorException"/>.
        /// </remarks>
        public static StatVariant Clamp(
              in StatVariant valueToClamp
            , in StatVariant lowerBound
            , in StatVariant upperBound
        )
        {
            ThrowHelper.ThrowIfMismatchedClampTypes(valueToClamp.Type, lowerBound.Type, upperBound.Type);
            ThrowHelper.ThrowIfUnsupportedType(valueToClamp.Type);

            ref readonly var v = ref valueToClamp;
            ref readonly var lo = ref lowerBound;
            ref readonly var hi = ref upperBound;

            return v.Type switch {
                StatVariantType.Byte   => new StatVariant((byte)math.clamp((uint)v.Byte, (uint)lo.Byte, (uint)hi.Byte)),
                StatVariantType.Double => new StatVariant((double)math.clamp(v.Double, lo.Double, hi.Double)),
                StatVariantType.Float  => new StatVariant((float)math.clamp(v.Float, lo.Float, hi.Float)),
                StatVariantType.Float2 => new StatVariant(math.clamp(v.Float2, lo.Float2, hi.Float2)),
                StatVariantType.Float3 => new StatVariant(math.clamp(v.Float3, lo.Float3, hi.Float3)),
                StatVariantType.Float4 => new StatVariant(math.clamp(v.Float4, lo.Float4, hi.Float4)),
                StatVariantType.Half   => new StatVariant(math.half(math.clamp(v.Half, lo.Half, hi.Half))),
                StatVariantType.Half2  => new StatVariant(math.half2(math.clamp(v.Half2, lo.Half2, hi.Half2))),
                StatVariantType.Half3  => new StatVariant(math.half3(math.clamp(v.Half3, lo.Half3, hi.Half3))),
                StatVariantType.Half4  => new StatVariant(math.half4(math.clamp(v.Half4, lo.Half4, hi.Half4))),
                StatVariantType.Int    => new StatVariant((int)math.clamp(v.Int, lo.Int, hi.Int)),
                StatVariantType.Long   => new StatVariant((long)math.clamp(v.Long, lo.Long, hi.Long)),
                StatVariantType.SByte  => new StatVariant((sbyte)math.clamp(v.SByte, lo.SByte, hi.SByte)),
                StatVariantType.Short  => new StatVariant((short)math.clamp(v.Short, lo.Short, hi.Short)),
                StatVariantType.UInt   => new StatVariant((uint)math.clamp(v.UInt, lo.UInt, hi.UInt)),
                StatVariantType.ULong  => new StatVariant((ulong)math.clamp(v.ULong, lo.ULong, hi.ULong)),
                StatVariantType.UShort => new StatVariant(
                    (ushort)math.clamp((uint)v.UShort, (uint)lo.UShort, (uint)hi.UShort)
                ),
                _                      => throw ThrowHelper.FuncException("Clamp", v.Type),
            };
        }

        /// <summary>
        /// The value it currently holds, not the name of this type.
        /// </summary>
        /// <remarks>
        /// Without this override the union falls back to <see cref="object.ToString"/>, which prints
        /// <c>ApexionGame.Entities.Stats.StatVariant</c> — every log line and every inspector cell
        /// showed the type name instead of the number.
        /// <para>
        /// Managed string work, so this is for tooling and logging. Do not call it from a Burst job.
        /// </para>
        /// </remarks>
        public readonly override string ToString()
            => Type switch {
                StatVariantType.Bool   => Bool ? "true" : "false",
                StatVariantType.Byte   => Byte.ToString(),
                StatVariantType.Double => Double.ToString(),
                StatVariantType.Float  => Float.ToString(),
                StatVariantType.Float2 => Float2.ToString(),
                StatVariantType.Float3 => Float3.ToString(),
                StatVariantType.Float4 => Float4.ToString(),
                StatVariantType.Half   => ((float)Half).ToString(),
                StatVariantType.Half2  => ((float2)Half2).ToString(),
                StatVariantType.Half3  => ((float3)Half3).ToString(),
                StatVariantType.Half4  => ((float4)Half4).ToString(),
                StatVariantType.Int    => Int.ToString(),
                StatVariantType.Long   => Long.ToString(),
                StatVariantType.SByte  => SByte.ToString(),
                StatVariantType.Short  => Short.ToString(),
                StatVariantType.UInt   => UInt.ToString(),
                StatVariantType.ULong  => ULong.ToString(),
                StatVariantType.UShort => UShort.ToString(),
                _                      => "none",
            };
    }
}
