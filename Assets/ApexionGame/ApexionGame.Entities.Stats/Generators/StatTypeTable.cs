#if UNITY_EDITOR && ANNULUS_CODEGEN && APEXION_STAT_VALUE_TYPES_GENERATOR

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using EncosyTower.Core;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Generators
{
    [ApiForEditor]
    internal readonly struct TypeRecord
    {
        public readonly string Type;
        public readonly string Name;
        public readonly int Size;
        public readonly string Ns;
        public readonly string One;
        public readonly bool Eq;

        public TypeRecord(string type, string name, int size, string ns, string one, bool eq)
        {
            Type = type;
            Name = name;
            Size = size;
            Ns = ns;
            One = one;
            Eq = eq;
        }
    }

    [ApiForEditor]
    internal static class StatTypeTable
    {
        [ApiForEditor] public const string AGGRESSIVE_INLINING = "[MethodImpl(MethodImplOptions.AggressiveInlining)]";
        [ApiForEditor] public const string STRUCT_LAYOUT_EXPLICIT = "[StructLayout(LayoutKind.Explicit)]";
        [ApiForEditor] public const string FIELD_OFFSET_0 = "[FieldOffset(0)]";
        [ApiForEditor] public const string FIELD_OFFSET_FORMAT = "[FieldOffset({0})]";
        [ApiForEditor] public const string COMMON_FOLDER = "../Common";

        [ApiForEditor]
        public const string ROSLYN_HELPERS_FOLDER
            = "../../../../Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Helpers/Entities.Stats";

        [ApiForEditor] public const string NAMESPACE = "ApexionGame.Entities.Stats";
        [ApiForEditor] public const string ROSLYN_NAMESPACE = "ApexionGame.SourceGen.Helpers.Entities.Stats";

        [ApiForEditor] public const string NS_STATS = "AGES";
        [ApiForEditor] public const string NS_MATH = "UM";
        [ApiForEditor] public const string NS_BCL = "";

        [ApiForEditor]
        public static readonly TypeRecord[] All = new TypeRecord[] {
            new(nameof(None), "None", SizeOf<None>(), NS_STATS, "new None()", false),
            new("bool", "Bool", SizeOf<bool>(), NS_BCL, "true", true),
            new(nameof(bool2), "Bool2", SizeOf<bool2>(), NS_MATH, "new bool2(true)", false),
            new(nameof(bool2x2), "Bool2x2", SizeOf<bool2x2>(), NS_MATH, "new bool2x2(true)", false),
            new(nameof(bool2x3), "Bool2x3", SizeOf<bool2x3>(), NS_MATH, "new bool2x3(true)", false),
            new(nameof(bool2x4), "Bool2x4", SizeOf<bool2x4>(), NS_MATH, "new bool2x4(true)", false),
            new(nameof(bool3), "Bool3", SizeOf<bool3>(), NS_MATH, "new bool3(true)", false),
            new(nameof(bool3x2), "Bool3x2", SizeOf<bool3x2>(), NS_MATH, "new bool3x2(true)", false),
            new(nameof(bool3x3), "Bool3x3", SizeOf<bool3x3>(), NS_MATH, "new bool3x3(true)", false),
            new(nameof(bool3x4), "Bool3x4", SizeOf<bool3x4>(), NS_MATH, "new bool3x4(true)", false),
            new(nameof(bool4), "Bool4", SizeOf<bool4>(), NS_MATH, "new bool4(true)", false),
            new(nameof(bool4x2), "Bool4x2", SizeOf<bool4x2>(), NS_MATH, "new bool4x2(true)", false),
            new(nameof(bool4x3), "Bool4x3", SizeOf<bool4x3>(), NS_MATH, "new bool4x3(true)", false),
            new(nameof(bool4x4), "Bool4x4", SizeOf<bool4x4>(), NS_MATH, "new bool4x4(true)", false),
            new("byte", "Byte", SizeOf<byte>(), NS_BCL, "(byte)1", true),
            new("double", "Double", SizeOf<double>(), NS_BCL, "(double)1.0", true),
            new(nameof(double2), "Double2", SizeOf<double2>(), NS_MATH, "new double2(1.0)", false),
            new(nameof(double2x2), "Double2x2", SizeOf<double2x2>(), NS_MATH, "new double2x2(1.0)", false),
            new(nameof(double2x3), "Double2x3", SizeOf<double2x3>(), NS_MATH, "new double2x3(1.0)", false),
            new(nameof(double2x4), "Double2x4", SizeOf<double2x4>(), NS_MATH, "new double2x4(1.0)", false),
            new(nameof(double3), "Double3", SizeOf<double3>(), NS_MATH, "new double3(1.0)", false),
            new(nameof(double3x2), "Double3x2", SizeOf<double3x2>(), NS_MATH, "new double3x2(1.0)", false),
            new(nameof(double3x3), "Double3x3", SizeOf<double3x3>(), NS_MATH, "new double3x3(1.0)", false),
            new(nameof(double3x4), "Double3x4", SizeOf<double3x4>(), NS_MATH, "new double3x4(1.0)", false),
            new(nameof(double4), "Double4", SizeOf<double4>(), NS_MATH, "new double4(1.0)", false),
            new(nameof(double4x2), "Double4x2", SizeOf<double4x2>(), NS_MATH, "new double4x2(1.0)", false),
            new(nameof(double4x3), "Double4x3", SizeOf<double4x3>(), NS_MATH, "new double4x3(1.0)", false),
            new(nameof(double4x4), "Double4x4", SizeOf<double4x4>(), NS_MATH, "new double4x4(1.0)", false),
            new("float", "Float", SizeOf<float>(), NS_BCL, "(float)1f", true),
            new(nameof(float2), "Float2", SizeOf<float2>(), NS_MATH, "new float2(1f)", false),
            new(nameof(float2x2), "Float2x2", SizeOf<float2x2>(), NS_MATH, "new float2x2(1f)", false),
            new(nameof(float2x3), "Float2x3", SizeOf<float2x3>(), NS_MATH, "new float2x3(1f)", false),
            new(nameof(float2x4), "Float2x4", SizeOf<float2x4>(), NS_MATH, "new float2x4(1f)", false),
            new(nameof(float3), "Float3", SizeOf<float3>(), NS_MATH, "new float3(1f)", false),
            new(nameof(float3x2), "Float3x2", SizeOf<float3x2>(), NS_MATH, "new float3x2(1f)", false),
            new(nameof(float3x3), "Float3x3", SizeOf<float3x3>(), NS_MATH, "new float3x3(1f)", false),
            new(nameof(float3x4), "Float3x4", SizeOf<float3x4>(), NS_MATH, "new float3x4(1f)", false),
            new(nameof(float4), "Float4", SizeOf<float4>(), NS_MATH, "new float4(1f)", false),
            new(nameof(float4x2), "Float4x2", SizeOf<float4x2>(), NS_MATH, "new float4x2(1f)", false),
            new(nameof(float4x3), "Float4x3", SizeOf<float4x3>(), NS_MATH, "new float4x3(1f)", false),
            new(nameof(float4x4), "Float4x4", SizeOf<float4x4>(), NS_MATH, "new float4x4(1f)", false),
            new(nameof(half), "Half", SizeOf<half>(), NS_MATH, "math.half(1f)", true),
            new(nameof(half2), "Half2", SizeOf<half2>(), NS_MATH, "math.half2(1f)", false),
            new(nameof(half3), "Half3", SizeOf<half3>(), NS_MATH, "math.half3(1f)", false),
            new(nameof(half4), "Half4", SizeOf<half4>(), NS_MATH, "math.half4(1f)", false),
            new("int", "Int", SizeOf<int>(), NS_BCL, "(int)1", true),
            new(nameof(int2), "Int2", SizeOf<int2>(), NS_MATH, "new int2(1)", false),
            new(nameof(int2x2), "Int2x2", SizeOf<int2x2>(), NS_MATH, "new int2x2(1)", false),
            new(nameof(int2x3), "Int2x3", SizeOf<int2x3>(), NS_MATH, "new int2x3(1)", false),
            new(nameof(int2x4), "Int2x4", SizeOf<int2x4>(), NS_MATH, "new int2x4(1)", false),
            new(nameof(int3), "Int3", SizeOf<int3>(), NS_MATH, "new int3(1)", false),
            new(nameof(int3x2), "Int3x2", SizeOf<int3x2>(), NS_MATH, "new int3x2(1)", false),
            new(nameof(int3x3), "Int3x3", SizeOf<int3x3>(), NS_MATH, "new int3x3(1)", false),
            new(nameof(int3x4), "Int3x4", SizeOf<int3x4>(), NS_MATH, "new int3x4(1)", false),
            new(nameof(int4), "Int4", SizeOf<int4>(), NS_MATH, "new int4(1)", false),
            new(nameof(int4x2), "Int4x2", SizeOf<int4x2>(), NS_MATH, "new int4x2(1)", false),
            new(nameof(int4x3), "Int4x3", SizeOf<int4x3>(), NS_MATH, "new int4x3(1)", false),
            new(nameof(int4x4), "Int4x4", SizeOf<int4x4>(), NS_MATH, "new int4x4(1)", false),
            new("long", "Long", SizeOf<long>(), NS_BCL, "(long)1", true),
            new("sbyte", "SByte", SizeOf<sbyte>(), NS_BCL, "(sbyte)1", true),
            new("short", "Short", SizeOf<short>(), NS_BCL, "(short)1", true),
            new("uint", "UInt", SizeOf<uint>(), NS_BCL, "(uint)1", true),
            new(nameof(uint2), "UInt2", SizeOf<uint2>(), NS_MATH, "new uint2((uint)1)", false),
            new(nameof(uint2x2), "UInt2x2", SizeOf<uint2x2>(), NS_MATH, "new uint2x2((uint)1)", false),
            new(nameof(uint2x3), "UInt2x3", SizeOf<uint2x3>(), NS_MATH, "new uint2x3((uint)1)", false),
            new(nameof(uint2x4), "UInt2x4", SizeOf<uint2x4>(), NS_MATH, "new uint2x4((uint)1)", false),
            new(nameof(uint3), "UInt3", SizeOf<uint3>(), NS_MATH, "new uint3((uint)1)", false),
            new(nameof(uint3x2), "UInt3x2", SizeOf<uint3x2>(), NS_MATH, "new uint3x2((uint)1)", false),
            new(nameof(uint3x3), "UInt3x3", SizeOf<uint3x3>(), NS_MATH, "new uint3x3((uint)1)", false),
            new(nameof(uint3x4), "UInt3x4", SizeOf<uint3x4>(), NS_MATH, "new uint3x4((uint)1)", false),
            new(nameof(uint4), "UInt4", SizeOf<uint4>(), NS_MATH, "new uint4((uint)1)", false),
            new(nameof(uint4x2), "UInt4x2", SizeOf<uint4x2>(), NS_MATH, "new uint4x2((uint)1)", false),
            new(nameof(uint4x3), "UInt4x3", SizeOf<uint4x3>(), NS_MATH, "new uint4x3((uint)1)", false),
            new(nameof(uint4x4), "UInt4x4", SizeOf<uint4x4>(), NS_MATH, "new uint4x4((uint)1)", false),
            new("ulong", "ULong", SizeOf<ulong>(), NS_BCL, "(ulong)1", true),
            new("ushort", "UShort", SizeOf<ushort>(), NS_BCL, "(ushort)1", true),
        };

        [ApiForEditor]
        public static readonly string[] GameplayProfile = new string[] {
            "None",
            "bool",
            "sbyte",
            "byte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
            "half",
            "half2",
            "half3",
            "half4",
            "float",
            "float2",
            "float3",
            "float4",
            "double",
        };

        [ApiForEditor]
        public static readonly string[] EnumUnderlyingTypes = new string[] {
            "sbyte",
            "byte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
        };

        private static TypeRecord[] s_active;

        [ApiForEditor]
        public static TypeRecord[] Active => s_active ??= Filter(GameplayProfile);

        [ApiForEditor]
        public static int MaxTypeSize
        {
            get
            {
                var max = 0;
                var active = Active;

                for (var i = 0; i < active.Length; i++)
                {
                    if (active[i].Size > max)
                    {
                        max = active[i].Size;
                    }
                }

                return max;
            }
        }

        [ApiForEditor]
        public static TypeRecord[] Filter(string[] profile)
        {
            var result = new TypeRecord[profile.Length];

            for (var i = 0; i < profile.Length; i++)
            {
                result[i] = Find(profile[i]);
            }

            return result;
        }

        [ApiForEditor]
        public static TypeRecord Find(string type)
        {
            var all = All;

            for (var i = 0; i < all.Length; i++)
            {
                if (string.Equals(all[i].Type, type, StringComparison.Ordinal))
                {
                    return all[i];
                }
            }

            throw CreateUnknownTypeException(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SizeOf<T>()
            where T : struct
            => UnsafeUtility.SizeOf<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack, StackTraceHidden]
        private static InvalidOperationException CreateUnknownTypeException(string type)
            => new(
                $"Type '{type}' does not exist in {nameof(StatTypeTable)}.{nameof(All)}. " +
                $"Add it to the master table before referencing it from a profile."
            );
    }
}

#endif
