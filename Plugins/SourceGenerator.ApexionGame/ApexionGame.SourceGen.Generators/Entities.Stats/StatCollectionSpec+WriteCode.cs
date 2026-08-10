using System;
using System.Globalization;

namespace ApexionGame.SourceGen.Generators.Entities.Stats
{
    partial struct StatCollectionSpec
    {
        private const string PR_METHOD_IMPL_OPTIONS = "SRCS.MethodImplOptions";
        private const string PR_INLINING = $"{PR_METHOD_IMPL_OPTIONS}.AggressiveInlining";
        private const string PR_GENERATOR = "\"ApexionGame.SourceGen.Generators.Entities.Stats.StatCollectionGenerator\"";

        private const string PR_AGGRESSIVE_INLINING = "[SRCS.MethodImpl(INLINING)]";
        private const string PR_EXCLUDE_COVERAGE = "[SDCA.ExcludeFromCodeCoverage]";
        private const string PR_GENERATED_CODE = $"[SCDC.GeneratedCode(GENERATOR, \"{SourceGenVersion.VALUE}\")]";
        private const string PR_VALIDATION_ATTRIBUTES = "[UE.HideInCallstack, SD.StackTraceHidden, " +
            "SD.Conditional(AGDVD.UNITY_EDITOR), SD.Conditional(AGDVD.DEBUG), " +
            "SD.Conditional(AGDVD.RUNTIME_CHECKS), SD.Conditional(AGDVD.STATS_CHECKS)]";

        public readonly string WriteCode()
        {
            var count = statDataCollection.Count;

            var enumUnderlyingType = (count + 1) switch {
                > ushort.MaxValue => "int",
                > byte.MaxValue => "ushort",
                _ => "byte",
            };

            var underlyingType = count switch {
                > ushort.MaxValue => "int",
                > byte.MaxValue => "ushort",
                _ => "byte",
            };

            var p = new Printer(0, 1024 * 512);

            p.PrintEndLine();
            p.Print("#pragma warning disable").PrintEndLine();
            p.PrintEndLine();

            p = p.IncreasedIndent();
            {
                p.PrintBeginLine("partial struct ").PrintEndLine(typeName);
                p.OpenScope();
                {
                    WriteConsts(ref p);

                    p.PrintLine("public Indices indices;");
                    p.PrintEndLine();

                    WriteConstructors(ref p);
                    WriteProperties(ref p);
                    WriteMethods(ref p);
                }
                p.CloseScope();
                p.PrintEndLine();

                WriteTypeEnum(ref p, enumUnderlyingType);
                WriteTypeIdStruct(ref p, enumUnderlyingType);
                WriteIndicesStruct(ref p);
                WriteIndexStructs(ref p, underlyingType);
                WriteIndexRecordStruct(ref p);
                WriteStatIndexRecordStruct(ref p);
                WriteStatHandleRecordStruct(ref p);
                WriteStatIndicesStruct(ref p);
                WriteStatHandlesStruct(ref p);
                WriteStatDataCollection(ref p);
                WriteOptionsStruct(ref p);
                WriteBakerStructs(ref p);
                WriteAccessorStructs(ref p);
                WriteReaderStruct(ref p);
                WriteExtensionsClass(ref p);

                p.Print("#region INTERNALS").PrintEndLine();
                p.Print("#endregion ======").PrintEndLine();
                p.PrintEndLine();

                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Internals");
                p.OpenScope();
                {
                    WriteHelperConstants(ref p);
                    WriteThrowMethods(ref p, underlyingType);
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintBeginLine("static partial class ").Print(typeName).PrintEndLine("Extensions // Internals");
                p.OpenScope();
                {
                    WriteHelperConstants(ref p);
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p = p.DecreasedIndent();

            return p.Result;
        }

        private readonly void WriteConsts(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.PrintBeginLine("public const int LENGTH = ").Print(count).PrintEndLine(";");
            p.PrintEndLine();

            p.PrintLine("private readonly static Type[] s_types = new Type[]");
            p.OpenScope();
            {
                for (var i = 0; i < count; i++)
                {
                    var statData = statDataCollection[i];

                    p.PrintBeginLine("Type.").Print(statData.typeName).PrintEndLine(",");
                }
            }
            p.CloseScope("};");
            p.PrintEndLine();
        }

        private readonly void WriteConstructors(ref Printer p)
        {
            p.PrintBeginLine("static ").Print(typeName).PrintEndLine("()");
            p.OpenScope();
            {
                p.PrintLine("ThrowIfTypesLengthExceedsStatUserDataCapacity();");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteProperties(ref Printer p)
        {
            p.PrintLine("public static S.ReadOnlySpan<Type> Types");
            p.OpenScope();
            {
                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintLine("get => s_types;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteMethods(ref Printer p)
        {
            p.PrintBeginLine("/// ").PrintEndLine("<summary>");
            p.PrintBeginLine("/// ").Print("Returns a <see cref=\"").Print(typeName)
                .PrintEndLine("\"/> from the value of <typeparamref name=\"T\"/>.");
            p.PrintBeginLine("/// ").PrintEndLine("</summary>");
            p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
            p.PrintBeginLine("/// ").Print("The layout and size of <typeparamref name=\"T\"/> ")
                .Print("must be the same as <see cref=\"").Print(typeName).Print("\"/>, because this method uses ")
                .Print("<c>UCLU.UnsafeUtility.As&lt;T, ").Print(typeName)
                .PrintEndLine("&gt;</c> under the hood.");
            p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
            p.PrintBeginLine("/// ").PrintEndLine("<seealso cref=\"UCLU.UnsafeUtility.As{U, T}\"/>");
            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintBeginLine("public static ").Print(typeName).PrintEndLine(" CastFrom<T>(T value)");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintLine("ThrowIfCannotCastFromType<T>();");
                p.PrintEndLine();

                p.PrintBeginLine("return UCLU.UnsafeUtility.As<T, ").Print(typeName).PrintEndLine(">(ref value);");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine("/// <inheritdoc cref=\"CastFrom{T}(T)\"/>");
            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintBeginLine("public static ref ").Print(typeName).PrintEndLine(" CastFrom<T>(ref T value)");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintLine("ThrowIfCannotCastFromType<T>();");
                p.PrintEndLine();

                p.PrintBeginLine("return ref UCLU.UnsafeUtility.As<T, ").Print(typeName).PrintEndLine(">(ref value);");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintLine("public static Indices GetIndicesFrom<T>(T value)");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintLine("return CastFrom<T>(value).indices;");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintLine("public static ref Indices GetIndicesFrom<T>(ref T value)");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintLine("return ref CastFrom<T>(ref value).indices;");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintLine("public static StatIndices GetStatIndicesFrom<T>(T value)");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintLine("return CastFrom<T>(value).GetStatIndices();");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintLine("public static StatHandles GetStatHandlesFrom<T>(T value, AGES.StatOwnerHandle owner)");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintLine("return CastFrom<T>(value).GetStatHandles(owner);");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintLine("public readonly StatIndices GetStatIndices()");
            p.OpenScope();
            {
                p.PrintLine("return indices.ToStatIndices();");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_AGGRESSIVE_INLINING);
            p.PrintLine("public readonly StatHandles GetStatHandles(AGES.StatOwnerHandle owner)");
            p.OpenScope();
            {
                p.PrintLine("return indices.ToStatHandles(owner);");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine("public readonly void FindValidIndices(UC.NativeHashMap<TypeId, Index> result)");
            p.OpenScope();
            {
                p.PrintLine("result.Clear();");
                p.PrintLine("ETCol.EncosyNativeHashMapExtensions.IncreaseCapacityTo(result, LENGTH);");
                p.PrintEndLine();

                p.PrintLine("var indices = this.indices;");
                p.PrintLine("var types = Types;");
                p.PrintLine("var length = types.Length;");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < length; i++)");
                p.OpenScope();
                {
                    p.PrintLine("var type = types[i];");
                    p.PrintLine("var index = indices[type];");
                    p.PrintEndLine();

                    p.PrintLine("if (index.IsValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("result.TryAdd(type, index);");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine("public readonly void FindValidStatIndices(UC.NativeHashMap<TypeId, AGES.StatIndex> result)");
            p.OpenScope();
            {
                p.PrintLine("result.Clear();");
                p.PrintLine("ETCol.EncosyNativeHashMapExtensions.IncreaseCapacityTo(result, LENGTH);");
                p.PrintEndLine();

                p.PrintLine("var indices = this.indices;");
                p.PrintLine("var types = Types;");
                p.PrintLine("var length = types.Length;");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < length; i++)");
                p.OpenScope();
                {
                    p.PrintLine("var type = types[i];");
                    p.PrintLine("var index = indices[type];");
                    p.PrintEndLine();

                    p.PrintLine("if (index.IsValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("result.TryAdd(type, index);");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine("public readonly void FindValidStatHandles(AGES.StatOwnerHandle owner, UC.NativeHashMap<TypeId, AGES.StatHandle> result)");
            p.OpenScope();
            {
                p.PrintLine("result.Clear();");
                p.PrintLine("ETCol.EncosyNativeHashMapExtensions.IncreaseCapacityTo(result, LENGTH);");
                p.PrintEndLine();

                p.PrintLine("var indices = this.indices;");
                p.PrintLine("var types = Types;");
                p.PrintLine("var length = types.Length;");
                p.PrintEndLine();

                p.PrintLine("for (var i = 0; i < length; i++)");
                p.OpenScope();
                {
                    p.PrintLine("var type = types[i];");
                    p.PrintLine("var index = indices[type];");
                    p.PrintEndLine();

                    p.PrintLine("if (index.IsValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("result.TryAdd(type, new(owner, index));");
                    }
                    p.CloseScope();
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteTypeEnum(ref Printer p, string underlyingType)
        {
            p.Print("#region    TYPE ENUM").PrintEndLine();
            p.Print("#endregion =========").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Type");
            p.OpenScope();
            {
                p.PrintLine(PR_GENERATED_CODE);
                p.PrintBeginLine("public enum Type : ").PrintEndLine(underlyingType);
                p.OpenScope();
                {
                    p.PrintLine("Undefined = 0,");

                    var count = statDataCollection.Count;

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];

                        p.PrintBeginLine(statData.typeName).Print(" = ").Print(i + 1).PrintEndLine(",");
                    }
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteTypeIdStruct(ref Printer p, string underlyingType)
        {
            var count = statDataCollection.Count;

            p.Print("#region    TYPE ID").PrintEndLine();
            p.Print("#endregion =======").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("TypeId");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Identifies a stat type within <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Wraps the <see cref=\"Type\"/> enum and provides methods for encoding and decoding stat user data,");
                p.PrintBeginLine("/// ").PrintEndLine("validating types, and converting to array indices.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public readonly partial struct TypeId : S.IEquatable<TypeId>");
                p.OpenScope();
                {
                    p.PrintBeginLine("public const uint OFFSET = ").Print(typeIdOffset).PrintEndLine(";");
                    p.PrintEndLine();

                    p.PrintLine("public readonly Type Value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public TypeId(Type value)");
                    p.OpenScope();
                    {
                        p.PrintLine("Value = value;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator TypeId(Type value)");
                    p.WithIncreasedIndent().PrintLine("=> new(value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator Type(TypeId id)");
                    p.WithIncreasedIndent().PrintLine("=> id.Value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static bool operator ==(TypeId lhs, TypeId rhs)");
                    p.WithIncreasedIndent().PrintLine("=> lhs.Equals(rhs);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static bool operator !=(TypeId lhs, TypeId rhs)");
                    p.WithIncreasedIndent().PrintLine("=> !lhs.Equals(rhs);");
                    p.PrintEndLine();

                    p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                    p.PrintBeginLine("/// ").Print("Converts <paramref name=\"type\"/> to the corresponding valid index")
                        .Print(" of the array <see cref=\"").Print(typeName).PrintEndLine(".Types\"/>.");
                    p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                    p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                    p.PrintBeginLine("/// ").PrintEndLine("The value of <see cref=\"Type.Undefined\"/> is invalid.");
                    p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static int ToValidArrayIndex(Type type)");
                    p.OpenScope();
                    {
                        p.PrintLine("// Remove the value of Type.Undefined");
                        p.PrintLine("return (int)type - 1;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static uint EncodeToStatUserData(Type type)");
                    p.OpenScope();
                    {
                        p.PrintLine("return (uint)type + OFFSET;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static Type DecodeFromStatUserData(uint userData)");
                    p.OpenScope();
                    {
                        p.PrintLine("return (Type)(userData - OFFSET);");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static Type DecodeFromStatUserData(in StatSystem.Stat stat)");
                    p.OpenScope();
                    {
                        p.PrintLine("return DecodeFromStatUserData(stat.UserData);");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static bool ValidateType(Type type)");
                    p.OpenScope();
                    {
                        p.PrintLine("return (uint)type < (uint)LENGTH;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static bool ValidateStatUserData(uint userData)");
                    p.OpenScope();
                    {
                        p.PrintLine("return (uint)DecodeFromStatUserData(userData) < (uint)LENGTH;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static bool ValidateStatUserData(in StatSystem.Stat stat)");
                    p.OpenScope();
                    {
                        p.PrintLine("return ValidateStatUserData(stat.UserData);");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public static bool ValidateStat<TStatData>(in StatSystem.Stat<TStatData> stat)");
                    p.WithIncreasedIndent().PrintLine("where TStatData : unmanaged, AGES.IStatData");
                    p.OpenScope();
                    {
                        p.PrintLine("if (ValidateStatUserData(stat.UserData) == false) return false;");
                        p.PrintEndLine();

                        for (var i = 0; i < count; i++)
                        {
                            var statData = statDataCollection[i];

                            p.PrintBeginLine("if (typeof(TStatData) == typeof(").Print(statData.typeName)
                                .PrintEndLine(")) return true;");
                        }

                        p.PrintEndLine();
                        p.PrintLine("return false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public bool Equals(TypeId other)");
                    p.WithIncreasedIndent().PrintLine("=> Value == other.Value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override bool Equals(object obj)");
                    p.WithIncreasedIndent().PrintLine("=> obj is TypeId other && Equals(other);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override int GetHashCode()");
                    p.WithIncreasedIndent().PrintBeginLine("=> ((").Print(underlyingType)
                        .PrintEndLine(")Value).GetHashCode();");
                    p.PrintEndLine();


                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteIndexStructs(ref Printer p, string fieldType)
        {
            p.Print("#region    INDEX").PrintEndLine();
            p.Print("#endregion =====").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Index");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").PrintEndLine("A type-erased index into the stat buffer for a stat slot.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").Print("Use <see cref=\"Index{TStatData}\"/> when the stat data type is known at compile time. ");
                p.PrintBeginLine("/// ").PrintEndLine("An index is valid when its <c>value</c> is greater than zero.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("[S.Serializable]");
                p.PrintBeginLine("public partial struct Index : S.IEquatable<Index>")
                    .Print(", ET.IIsValid")
                    .Print(", ETCon.IToFixedString, ETCon.IToFixedString<UC.FixedString32Bytes>")
                    .PrintEndLine();
                p.OpenScope();
                {
                    p.PrintLine("public static readonly Index Null = default;");
                    p.PrintEndLine();

                    p.PrintLine("/// <inheritdoc cref=\"AGES.StatIndex.value\"/>");
                    p.PrintBeginLine("public ").Print(fieldType).PrintEndLine(" value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public Index(").Print(fieldType).PrintEndLine(" value)");
                    p.OpenScope();
                    {
                        p.PrintLine("this.value = value;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly bool IsValid");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => value > Null.value;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static implicit operator Index(")
                        .Print(fieldType).PrintEndLine(" value)");
                    p.WithIncreasedIndent().PrintLine("=> new(value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static implicit operator ").Print(fieldType)
                        .PrintEndLine("(Index index)");
                    p.WithIncreasedIndent().PrintLine("=> index.value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator Index(AGES.StatIndex index)");
                    p.WithIncreasedIndent().PrintBeginLine("=> new((").Print(fieldType).PrintEndLine(")index.value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator AGES.StatIndex(Index index)");
                    p.WithIncreasedIndent().PrintLine("=> new((int)index.value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly bool Equals(Index other)");
                    p.WithIncreasedIndent().PrintLine("=> value == other.value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly override bool Equals(object obj)");
                    p.WithIncreasedIndent().PrintLine("=> obj is Index other && Equals(other);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly override int GetHashCode()");
                    p.WithIncreasedIndent().PrintLine("=> value.GetHashCode();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly override string ToString()");
                    p.WithIncreasedIndent().PrintLine("=> value.ToString();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly UC.FixedString32Bytes ToFixedString()");
                    p.WithIncreasedIndent().PrintLine("=> ETCol.EncosyFixedStringExtensions.ToFixedString(value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly TFixedString ToFixedString<TFixedString>()");
                    p.WithIncreasedIndent().PrintBeginLine("where TFixedString : unmanaged, UC.INativeList<byte>, ")
                        .PrintEndLine("UC.IUTF8Bytes");
                    p.WithIncreasedIndent().PrintBeginLine("=> ETCol.EncosyFixedStringExtensions")
                        .PrintEndLine(".CastTo<TFixedString>(ToFixedString());");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly AGES.StatHandle ToStatHandle(AGES.StatOwnerHandle owner)");
                    p.WithIncreasedIndent().PrintLine("=> new(owner, this);");
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    INDEX<TSTAT_DATA>").PrintEndLine();
            p.Print("#endregion =================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Index<TStatData>");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").PrintEndLine("A strongly-typed index into the stat buffer for a specific <typeparamref name=\"TStatData\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").Print("Implicitly converts to and from <see cref=\"Index\"/> and <see cref=\"AGES.StatIndex{TStatData}\"/>. ");
                p.PrintBeginLine("/// ").PrintEndLine("An index is valid when its <c>value</c> is greater than zero.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("<typeparam name=\"TStatData\">The stat data type this index refers to.</typeparam>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("[S.Serializable]");
                p.PrintBeginLine("public partial struct Index<TStatData> : S.IEquatable<Index<TStatData>>, ET.IIsValid")
                    .PrintEndLine(", ETCon.IToFixedString, ETCon.IToFixedString<UC.FixedString32Bytes>");
                p.WithIncreasedIndent().PrintLine("where TStatData : unmanaged, AGES.IStatData");
                p.OpenScope();
                {
                    p.PrintLine("public static readonly Index<TStatData> Null = default;");
                    p.PrintEndLine();

                    p.PrintLine("/// <inheritdoc cref=\"StatIndex.value\"/>");
                    p.PrintBeginLine("public ").Print(fieldType).PrintEndLine(" value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public Index(").Print(fieldType).PrintEndLine(" value)");
                    p.OpenScope();
                    {
                        p.PrintLine("this.value = value;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly bool IsValid");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => value > Null.value;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static implicit operator Index<TStatData>(")
                        .Print(fieldType).PrintEndLine(" value)");
                    p.WithIncreasedIndent().PrintLine("=> new(value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static implicit operator ").Print(fieldType)
                        .PrintEndLine("(Index<TStatData> index)");
                    p.WithIncreasedIndent().PrintLine("=> index.value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator Index<TStatData>(AGES.StatIndex<TStatData> index)");
                    p.WithIncreasedIndent().PrintBeginLine("=> new((").Print(fieldType).PrintEndLine(")index.value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static explicit operator Index<TStatData>(Index index)");
                    p.WithIncreasedIndent().PrintLine("=> new(index.value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator Index(Index<TStatData> index)");
                    p.WithIncreasedIndent().PrintLine("=> new(index.value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator AGES.StatIndex<TStatData>(Index<TStatData> index)");
                    p.WithIncreasedIndent().PrintLine("=> new((int)index.value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static implicit operator AGES.StatIndex(Index<TStatData> index)");
                    p.WithIncreasedIndent().PrintLine("=> new((int)index.value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly bool Equals(Index<TStatData> other)");
                    p.WithIncreasedIndent().PrintLine("=> value == other.value;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly override bool Equals(object obj)");
                    p.WithIncreasedIndent().PrintLine("=> obj is Index<TStatData> other && Equals(other);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly override int GetHashCode()");
                    p.WithIncreasedIndent().PrintLine("=> value.GetHashCode();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly override string ToString()");
                    p.WithIncreasedIndent().PrintLine("=> value.ToString();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly UC.FixedString32Bytes ToFixedString()");
                    p.WithIncreasedIndent().PrintLine("=> ETCol.EncosyFixedStringExtensions.ToFixedString(value);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly TFixedString ToFixedString<TFixedString>()");
                    p.WithIncreasedIndent().PrintBeginLine("where TFixedString : unmanaged, UC.INativeList<byte>, ")
                        .PrintEndLine("UC.IUTF8Bytes");
                    p.WithIncreasedIndent().PrintBeginLine("=> ETCol.EncosyFixedStringExtensions")
                        .PrintEndLine(".CastTo<TFixedString>(ToFixedString());");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly AGES.StatHandle<TStatData> ToStatHandle(AGES.StatOwnerHandle owner)");
                    p.WithIncreasedIndent().PrintLine("=> new(owner, this);");
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteIndexRecordStruct(ref Printer p)
        {
            p.Print("#region    INDEX RECORD").PrintEndLine();
            p.Print("#endregion ============").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("IndexRecord");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").PrintEndLine("An immutable record that pairs an <see cref=\"Index\"/> with its associated <see cref=\"Type\"/> and validity flag.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public readonly partial struct IndexRecord : S.IEquatable<IndexRecord>, ET.IIsValid");
                p.OpenScope();
                {
                    p.PrintLine("public readonly Index Index;");
                    p.PrintLine("public readonly Type Type;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public IndexRecord(Index index, Type type, bool isValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("Index = index;");
                        p.PrintLine("Type = type;");
                        p.PrintLine("IsValid = isValid;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly bool IsValid { get; }");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public void Deconstruct(out Index index, out Type type, out bool isValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("index = Index;");
                        p.PrintLine("type = Type;");
                        p.PrintLine("isValid = IsValid;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public bool Equals(IndexRecord other)");
                    p.WithIncreasedIndent().PrintBeginLine("=> Index == other.Index && Type == other.Type")
                        .PrintEndLine(" && IsValid == other.IsValid;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override bool Equals(object obj)");
                    p.WithIncreasedIndent().PrintLine("=> obj is IndexRecord other && Equals(other);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override int GetHashCode()");
                    p.WithIncreasedIndent().PrintLine("=> ET.HashValue.Combine(Index, Type, IsValid);");
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteStatIndexRecordStruct(ref Printer p)
        {
            p.Print("#region    STAT INDEX RECORD").PrintEndLine();
            p.Print("#endregion =================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("StatIndexRecord");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").PrintEndLine("An immutable record that pairs an <see cref=\"AGES.StatIndex\"/> with its associated <see cref=\"Type\"/> and validity flag.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public readonly partial struct StatIndexRecord : S.IEquatable<StatIndexRecord>, ET.IIsValid");
                p.OpenScope();
                {
                    p.PrintLine("public readonly AGES.StatIndex Index;");
                    p.PrintLine("public readonly Type Type;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public StatIndexRecord(AGES.StatIndex index, Type type, bool isValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("Index = index;");
                        p.PrintLine("Type = type;");
                        p.PrintLine("IsValid = isValid;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly bool IsValid { get; }");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public void Deconstruct(out AGES.StatIndex index, out Type type, out bool isValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("index = Index;");
                        p.PrintLine("type = Type;");
                        p.PrintLine("isValid = IsValid;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public bool Equals(StatIndexRecord other)");
                    p.WithIncreasedIndent().PrintBeginLine("=> Index == other.Index && Type == other.Type")
                        .PrintEndLine(" && IsValid == other.IsValid;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override bool Equals(object obj)");
                    p.WithIncreasedIndent().PrintLine("=> obj is StatIndexRecord other && Equals(other);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override int GetHashCode()");
                    p.WithIncreasedIndent().PrintLine("=> ET.HashValue.Combine(Index, Type, IsValid);");
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteStatHandleRecordStruct(ref Printer p)
        {
            p.Print("#region    STAT HANDLE RECORD").PrintEndLine();
            p.Print("#endregion ==================").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("StatHandleRecord");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").PrintEndLine("An immutable record that pairs an <see cref=\"AGES.StatHandle\"/> with its associated <see cref=\"Type\"/> and validity flag.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public readonly partial struct StatHandleRecord : S.IEquatable<StatHandleRecord>, ET.IIsValid");
                p.OpenScope();
                {
                    p.PrintLine("public readonly AGES.StatHandle Handle;");
                    p.PrintLine("public readonly Type Type;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public StatHandleRecord(AGES.StatHandle handle, Type type, bool isValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("Handle = handle;");
                        p.PrintLine("Type = type;");
                        p.PrintLine("IsValid = isValid;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly bool IsValid { get; }");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public void Deconstruct(out AGES.StatHandle handle, out Type type, out bool isValid)");
                    p.OpenScope();
                    {
                        p.PrintLine("handle = Handle;");
                        p.PrintLine("type = Type;");
                        p.PrintLine("isValid = IsValid;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public bool Equals(StatHandleRecord other)");
                    p.WithIncreasedIndent().PrintBeginLine("=> Handle == other.Handle && Type == other.Type")
                        .PrintEndLine(" && IsValid == other.IsValid;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override bool Equals(object obj)");
                    p.WithIncreasedIndent().PrintLine("=> obj is StatHandleRecord other && Equals(other);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public override int GetHashCode()");
                    p.WithIncreasedIndent().PrintLine("=> ET.HashValue.Combine(Handle, Type, IsValid);");
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteIndicesStruct(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.Print("#region    INDICES").PrintEndLine();
            p.Print("#endregion =======").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Indices");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("A fixed-size, blittable collection of <see cref=\"Index\"/> values, one per stat type in <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Each field corresponds to a stat type and stores the index of that stat in the stat buffer.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public partial struct Indices : ETCol.IHasLength, ETCol.IAsSpan<Index>, ETCol.IAsReadOnlySpan<Index>");
                p.OpenScope();
                {
                    p.PrintBeginLine("public const int LENGTH = ").Print(typeName).PrintEndLine(".LENGTH;");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];

                        p.PrintBeginLine("public Index<").Print(statData.typeName).Print("> ")
                            .Print(statData.fieldName).PrintEndLine(";");
                    }

                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public Indices(S.ReadOnlySpan<Index> values) : this()");
                    p.OpenScope();
                    {
                        p.PrintLine("values.CopyTo(AsSpan());");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public ref Index this[int index]");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => ref AsSpan()[index];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public ref Index this[Type type]");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => ref AsSpan()[TypeId.ToValidArrayIndex(type)];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly int Length");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => LENGTH;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static explicit operator Indices(S.Span<Index> values)");
                    p.WithIncreasedIndent().PrintLine("=> new(values);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static explicit operator Indices(S.ReadOnlySpan<Index> values)");
                    p.WithIncreasedIndent().PrintLine("=> new(values);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly bool TryGet(Type type, out Index result)");
                    p.OpenScope();
                    {
                        p.PrintLine("var index = TypeId.ToValidArrayIndex(type);");
                        p.PrintEndLine();

                        p.PrintLine("if ((uint)index < (uint)LENGTH)");
                        p.OpenScope();
                        {
                            p.PrintLine("result = AsReadOnlySpan()[index];");
                            p.PrintLine("return true;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("result = default;");
                        p.PrintLine("return false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly S.Span<Index> AsSpan()");
                    p.OpenScope();
                    {
                        p.PrintLine("// SAFETY: The fixed span is built from the contiguous generated stat storage.");
                        p.PrintLine("unsafe");
                        p.OpenScope();
                        {
                            var firstFieldName = statDataCollection[0].fieldName;

                            p.PrintBeginLine("fixed (void* ptr = &").Print(firstFieldName).PrintEndLine(")");
                            p.OpenScope();
                            {
                                p.PrintLine("return new S.Span<Index>(ptr, LENGTH);");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly S.ReadOnlySpan<Index> AsReadOnlySpan()");
                    p.WithIncreasedIndent().PrintLine("=> AsSpan();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly S.ReadOnlySpan<Index>.Enumerator GetEnumerator()");
                    p.WithIncreasedIndent().PrintLine("=> AsReadOnlySpan().GetEnumerator();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly StatIndices ToStatIndices()");
                    p.OpenScope();
                    {
                        p.PrintLine("return new()");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var fieldName = statDataCollection[i].fieldName;

                                p.PrintBeginLine(fieldName).Print(" = ").Print(fieldName).PrintEndLine(",");
                            }
                        }
                        p.CloseScope("};");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly StatHandles ToStatHandles(AGES.StatOwnerHandle owner)");
                    p.OpenScope();
                    {
                        p.PrintLine("return new()");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var fieldName = statDataCollection[i].fieldName;

                                p.PrintBeginLine(fieldName).Print(" = new(owner, ")
                                    .Print(fieldName).PrintEndLine("),");
                            }
                        }
                        p.CloseScope("};");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly Index<TStatData> GetIndexFor<TStatData>()");
                    p.WithIncreasedIndent().PrintLine("where TStatData : unmanaged, AGES.IStatData");
                    p.OpenScope();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var statData = statDataCollection[i];

                            p.PrintBeginLine("if (typeof(TStatData) == typeof(").Print(statData.typeName).PrintEndLine("))");
                            p.OpenScope();
                            {
                                p.PrintBeginLine("return (Index<TStatData>)((Index)")
                                    .Print(statData.fieldName).PrintEndLine(");");
                            }
                            p.CloseScope();
                            p.PrintEndLine();
                        }

                        p.PrintLine("return default;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly UC.NativeList<IndexRecord> ToRecords(UC.AllocatorManager.AllocatorHandle allocator)");
                    p.OpenScope();
                    {
                        p.PrintLine("var result = new UC.NativeList<IndexRecord>(LENGTH, allocator);");
                        p.PrintLine("var types = Types;");
                        p.PrintLine("var indices = AsReadOnlySpan();");
                        p.PrintEndLine();

                        p.PrintLine("for (var i = 0; i < LENGTH; i++)");
                        p.OpenScope();
                        {
                            p.PrintLine("var index = indices[i];");
                            p.PrintLine("result.AddNoResize(new IndexRecord(index, types[i], index.IsValid));");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("return result;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly UC.NativeList<IndexRecord> ToValidRecords(UC.AllocatorManager.AllocatorHandle allocator)");
                    p.OpenScope();
                    {
                        p.PrintLine("var result = new UC.NativeList<IndexRecord>(LENGTH, allocator);");
                        p.PrintLine("var types = Types;");
                        p.PrintLine("var indices = AsReadOnlySpan();");
                        p.PrintEndLine();

                        p.PrintLine("for (var i = 0; i < LENGTH; i++)");
                        p.OpenScope();
                        {
                            p.PrintLine("var index = indices[i];");
                            p.PrintEndLine();

                            p.PrintLine("if (index.IsValid)");
                            p.OpenScope();
                            {
                                p.PrintLine("result.AddNoResize(new IndexRecord(index, types[i], true));");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("return result;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteStatIndicesStruct(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.Print("#region    STAT INDICES").PrintEndLine();
            p.Print("#endregion ============").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("StatIndices");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("A fixed-size, blittable collection of <see cref=\"AGES.StatIndex\"/> values, one per stat type in <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").Print("Mirrors <see cref=\"Indices\"/> but stores raw <see cref=\"AGES.StatIndex\"/> values. ");
                p.PrintBeginLine("/// ").PrintEndLine("Obtained via <see cref=\"Indices.ToStatIndices\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public partial struct StatIndices : ETCol.IHasLength, ETCol.IAsSpan<AGES.StatIndex>, ETCol.IAsReadOnlySpan<AGES.StatIndex>");
                p.OpenScope();
                {
                    p.PrintBeginLine("public const int LENGTH = ").Print(typeName).PrintEndLine(".LENGTH;");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];

                        p.PrintBeginLine("public AGES.StatIndex<").Print(statData.typeName).Print("> ")
                            .Print(statData.fieldName).PrintEndLine(";");
                    }

                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public StatIndices(S.ReadOnlySpan<AGES.StatIndex> values) : this()");
                    p.OpenScope();
                    {
                        p.PrintLine("values.CopyTo(AsSpan());");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public ref AGES.StatIndex this[int index]");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => ref AsSpan()[index];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public ref AGES.StatIndex this[Type type]");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => ref AsSpan()[TypeId.ToValidArrayIndex(type)];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly int Length");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => LENGTH;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static explicit operator StatIndices(S.Span<AGES.StatIndex> values)");
                    p.WithIncreasedIndent().PrintLine("=> new(values);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static explicit operator StatIndices(S.ReadOnlySpan<AGES.StatIndex> values)");
                    p.WithIncreasedIndent().PrintLine("=> new(values);");

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly bool TryGet(Type type, out AGES.StatIndex result)");
                    p.OpenScope();
                    {
                        p.PrintLine("var index = TypeId.ToValidArrayIndex(type);");
                        p.PrintEndLine();

                        p.PrintLine("if ((uint)index < (uint)LENGTH)");
                        p.OpenScope();
                        {
                            p.PrintLine("result = AsReadOnlySpan()[index];");
                            p.PrintLine("return true;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("result = default;");
                        p.PrintLine("return false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                    p.PrintEndLine();

                    p.PrintLine("public readonly S.Span<AGES.StatIndex> AsSpan()");
                    p.OpenScope();
                    {
                        p.PrintLine("// SAFETY: The fixed span is built from the contiguous generated stat storage.");
                        p.PrintLine("unsafe");
                        p.OpenScope();
                        {
                            var firstFieldName = statDataCollection[0].fieldName;

                            p.PrintBeginLine("fixed (void* ptr = &").Print(firstFieldName).PrintEndLine(")");
                            p.OpenScope();
                            {
                                p.PrintLine("return new S.Span<AGES.StatIndex>(ptr, LENGTH);");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly S.ReadOnlySpan<AGES.StatIndex> AsReadOnlySpan()");
                    p.WithIncreasedIndent().PrintLine("=> AsSpan();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly S.ReadOnlySpan<AGES.StatIndex>.Enumerator GetEnumerator()");
                    p.WithIncreasedIndent().PrintLine("=> AsReadOnlySpan().GetEnumerator();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly Indices ToIndices()");
                    p.OpenScope();
                    {
                        p.PrintLine("return new()");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var statData = statDataCollection[i];
                                var fieldName = statData.fieldName;

                                p.PrintBeginLine(fieldName).Print(" = (Index<")
                                    .Print(statData.typeName).Print(">)")
                                    .Print(fieldName).PrintEndLine(",");
                            }
                        }
                        p.CloseScope("};");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly StatHandles ToStatHandles(AGES.StatOwnerHandle owner)");
                    p.OpenScope();
                    {
                        p.PrintLine("return new()");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var fieldName = statDataCollection[i].fieldName;

                                p.PrintBeginLine(fieldName).Print(" = new(owner, ")
                                    .Print(fieldName).PrintEndLine("),");
                            }
                        }
                        p.CloseScope("};");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly AGES.StatIndex<TStatData> GetStatIndexFor<TStatData>()");
                    p.WithIncreasedIndent().PrintLine("where TStatData : unmanaged, IStatData");
                    p.OpenScope();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var statData = statDataCollection[i];

                            p.PrintBeginLine("if (typeof(TStatData) == typeof(").Print(statData.typeName).PrintEndLine("))");
                            p.OpenScope();
                            {
                                p.PrintBeginLine("return (AGES.StatIndex<TStatData>)((StatIndex)")
                                    .Print(statData.fieldName).PrintEndLine(");");
                            }
                            p.CloseScope();
                            p.PrintEndLine();
                        }

                        p.PrintLine("return default;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly UC.NativeList<StatIndexRecord> ToRecords(UC.AllocatorManager.AllocatorHandle allocator)");
                    p.OpenScope();
                    {
                        p.PrintLine("var result = new UC.NativeList<StatIndexRecord>(LENGTH, allocator);");
                        p.PrintLine("var types = Types;");
                        p.PrintLine("var indices = AsReadOnlySpan();");
                        p.PrintEndLine();

                        p.PrintLine("for (var i = 0; i < LENGTH; i++)");
                        p.OpenScope();
                        {
                            p.PrintLine("var index = indices[i];");
                            p.PrintLine("result.AddNoResize(new StatIndexRecord(index, types[i], index.IsValid));");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("return result;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly UC.NativeList<StatIndexRecord> ToValidRecords(UC.AllocatorManager.AllocatorHandle allocator)");
                    p.OpenScope();
                    {
                        p.PrintLine("var result = new UC.NativeList<StatIndexRecord>(LENGTH, allocator);");
                        p.PrintLine("var types = Types;");
                        p.PrintLine("var indices = AsReadOnlySpan();");
                        p.PrintEndLine();

                        p.PrintLine("for (var i = 0; i < LENGTH; i++)");
                        p.OpenScope();
                        {
                            p.PrintLine("var index = indices[i];");
                            p.PrintEndLine();

                            p.PrintLine("if (index.IsValid)");
                            p.OpenScope();
                            {
                                p.PrintLine("result.AddNoResize(new StatIndexRecord(index, types[i], true));");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("return result;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteStatHandlesStruct(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.Print("#region    STAT HANDLES").PrintEndLine();
            p.Print("#endregion ============").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("StatHandles");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("A fixed-size, blittable collection of <see cref=\"AGES.StatHandle\"/> values, one per stat type in <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Each handle encodes both an owner reference and a stat index, allowing direct stat access.");
                p.PrintBeginLine("/// ").Print("Obtained via <see cref=\"Indices.ToStatHandles\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public partial struct StatHandles : ETCol.IHasLength, ETCol.IAsSpan<AGES.StatHandle>, ETCol.IAsReadOnlySpan<AGES.StatHandle>");
                p.OpenScope();
                {
                    p.PrintBeginLine("public const int LENGTH = ").Print(typeName).PrintEndLine(".LENGTH;");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];

                        p.PrintBeginLine("public AGES.StatHandle<").Print(statData.typeName).Print("> ")
                            .Print(statData.fieldName).PrintEndLine(";");
                    }

                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public StatHandles(S.ReadOnlySpan<AGES.StatHandle> values) : this()");
                    p.OpenScope();
                    {
                        p.PrintLine("values.CopyTo(AsSpan());");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public ref AGES.StatHandle this[int index]");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => ref AsSpan()[index];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public ref AGES.StatHandle this[Type type]");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => ref AsSpan()[TypeId.ToValidArrayIndex(type)];");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly int Length");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => LENGTH;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static explicit operator StatHandles(S.Span<AGES.StatHandle> values)");
                    p.WithIncreasedIndent().PrintLine("=> new(values);");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public static explicit operator StatHandles(S.ReadOnlySpan<AGES.StatHandle> values)");
                    p.WithIncreasedIndent().PrintLine("=> new(values);");

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly bool TryGet(Type type, out AGES.StatHandle result)");
                    p.OpenScope();
                    {
                        p.PrintLine("var index = TypeId.ToValidArrayIndex(type);");
                        p.PrintEndLine();

                        p.PrintLine("if ((uint)index < (uint)LENGTH)");
                        p.OpenScope();
                        {
                            p.PrintLine("result = AsReadOnlySpan()[index];");
                            p.PrintLine("return true;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("result = default;");
                        p.PrintLine("return false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                    p.PrintEndLine();

                    p.PrintLine("public readonly S.Span<AGES.StatHandle> AsSpan()");
                    p.OpenScope();
                    {
                        p.PrintLine("// SAFETY: The fixed span is built from the contiguous generated stat storage.");
                        p.PrintLine("unsafe");
                        p.OpenScope();
                        {
                            var firstFieldName = statDataCollection[0].fieldName;

                            p.PrintBeginLine("fixed (void* ptr = &").Print(firstFieldName).PrintEndLine(")");
                            p.OpenScope();
                            {
                                p.PrintLine("return new S.Span<AGES.StatHandle>(ptr, LENGTH);");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly S.ReadOnlySpan<AGES.StatHandle> AsReadOnlySpan()");
                    p.WithIncreasedIndent().PrintLine("=> AsSpan();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly S.ReadOnlySpan<AGES.StatHandle>.Enumerator GetEnumerator()");
                    p.WithIncreasedIndent().PrintLine("=> AsReadOnlySpan().GetEnumerator();");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly Indices ToIndices()");
                    p.OpenScope();
                    {
                        p.PrintLine("return new()");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var statData = statDataCollection[i];
                                var fieldName = statData.fieldName;

                                p.PrintBeginLine(fieldName).Print(" = (Index<")
                                    .Print(statData.typeName).Print(">)")
                                    .Print(fieldName).PrintEndLine(".index,");
                            }
                        }
                        p.CloseScope("};");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly StatIndices ToStatIndices()");
                    p.OpenScope();
                    {
                        p.PrintLine("return new()");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var fieldName = statDataCollection[i].fieldName;

                                p.PrintBeginLine(fieldName).Print(" = ")
                                    .Print(fieldName).PrintEndLine(".index,");
                            }
                        }
                        p.CloseScope("};");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly AGES.StatHandle<TStatData> GetStatHandleFor<TStatData>()");
                    p.WithIncreasedIndent().PrintLine("where TStatData : unmanaged, AGES.IStatData");
                    p.OpenScope();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var statData = statDataCollection[i];

                            p.PrintBeginLine("if (typeof(TStatData) == typeof(").Print(statData.typeName).PrintEndLine("))");
                            p.OpenScope();
                            {
                                p.PrintBeginLine("return (AGES.StatHandle<TStatData>)((StatHandle)")
                                    .Print(statData.fieldName).PrintEndLine(");");
                            }
                            p.CloseScope();
                            p.PrintEndLine();
                        }

                        p.PrintLine("return default;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly UC.NativeList<StatHandleRecord> ToRecords(UC.AllocatorManager.AllocatorHandle allocator)");
                    p.OpenScope();
                    {
                        p.PrintLine("var result = new UC.NativeList<StatHandleRecord>(LENGTH, allocator);");
                        p.PrintLine("var types = Types;");
                        p.PrintLine("var handles = AsReadOnlySpan();");
                        p.PrintEndLine();

                        p.PrintLine("for (var i = 0; i < LENGTH; i++)");
                        p.OpenScope();
                        {
                            p.PrintLine("var handle = handles[i];");
                            p.PrintLine("result.AddNoResize(new StatHandleRecord(handle, types[i], handle.IsValid));");
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("return result;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly UC.NativeList<StatHandleRecord> ToValidRecords(UC.AllocatorManager.AllocatorHandle allocator)");
                    p.OpenScope();
                    {
                        p.PrintLine("var result = new UC.NativeList<StatHandleRecord>(LENGTH, allocator);");
                        p.PrintLine("var types = Types;");
                        p.PrintLine("var handles = AsReadOnlySpan();");
                        p.PrintEndLine();

                        p.PrintLine("for (var i = 0; i < LENGTH; i++)");
                        p.OpenScope();
                        {
                            p.PrintLine("var handle = handles[i];");
                            p.PrintEndLine();

                            p.PrintLine("if (handle.IsValid)");
                            p.OpenScope();
                            {
                                p.PrintLine("result.AddNoResize(new StatHandleRecord(handle, types[i], true));");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("return result;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteStatDataCollection(ref Printer p)
        {
            var statDataCollection = this.statDataCollection;
            var length = statDataCollection.Count;
            var collectionTypeName = typeName;

            for (var i = 0; i < length; i++)
            {
                var statData = statDataCollection[i];
                var statTypeName = statData.typeName;
                var statTypeNameUpper = statTypeName.ToUpper(CultureInfo.InvariantCulture);

                p.Print("#region    STAT DATA - ").Print(statTypeNameUpper).PrintEndLine();
                p.Print("#endregion ============").PrintRepeat('=', statTypeNameUpper.Length).PrintEndLine();
                p.PrintEndLine();

                p.PrintBeginLine("partial struct ").Print(collectionTypeName).Print(" // Stat: ").PrintEndLine(statTypeName);
                p.OpenScope();
                {
                    WriteStatCreation(ref p, statDataCollection[i]);
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            return;

            static void WriteStatCreation(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;

                p.PrintBeginLine("partial struct ").PrintEndLine(typeName);
                p.OpenScope();
                {
                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintLine("public readonly StatSystem.ValuePair ToValuePair()");
                    p.WithIncreasedIndent().PrintBeginLine("=> IsValuePair ")
                        .Print("? new StatSystem.ValuePair(BaseValue, CurrentValue) ")
                        .PrintEndLine(": new StatSystem.ValuePair(CurrentValue);");
                    p.PrintEndLine();

                    p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                    p.PrintLine("public static partial class Params");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintBeginLine("public static AGES.StatDataParams<")
                            .Print(typeName)
                            .PrintEndLine("> Create(ET.Option<bool> produceChangeEvents = default)");
                        p.WithIncreasedIndent().PrintBeginLine("=> new(new ")
                            .Print(typeName).Print("(), default, produceChangeEvents, TypeId.EncodeToStatUserData(Type.")
                            .Print(typeName).PrintEndLine("));");
                        p.PrintEndLine();

                        var singleArgName = statData.singleValue ? "value" : "baseValue";
                        var valueTypeNs = statData.valueTypeNamespace;
                        var valueType = statData.valueType;
                        var hasCustomNs = string.IsNullOrEmpty(valueTypeNs) == false;

                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintBeginLine("public static AGES.StatDataParams<")
                            .Print(typeName).Print("> Create(")
                            .PrintIf(hasCustomNs, valueTypeNs).PrintIf(hasCustomNs, ".")
                            .Print(valueType).Print(" ").Print(singleArgName)
                            .PrintEndLine(", ET.Option<bool> produceChangeEvents = default)");
                        p.WithIncreasedIndent().PrintBeginLine("=> new(new ")
                            .Print(typeName).Print("(").Print(singleArgName).Print("), default, produceChangeEvents, ")
                            .Print("TypeId.EncodeToStatUserData(Type.").Print(typeName).PrintEndLine("));");
                        p.PrintEndLine();

                        if (statData.singleValue == false)
                        {
                            p.PrintLine(PR_AGGRESSIVE_INLINING);
                            p.PrintBeginLine("public static AGES.StatDataParams<")
                                .Print(typeName).Print("> Create(")
                                .PrintIf(hasCustomNs, valueTypeNs).PrintIf(hasCustomNs, ".")
                                .Print(valueType).Print(" baseValue, ")
                                .PrintIf(hasCustomNs, valueTypeNs).PrintIf(hasCustomNs, ".")
                                .Print(valueType).Print(" currentValue, ")
                                .PrintEndLine("ET.Option<bool> produceChangeEvents = default)");
                            p.WithIncreasedIndent().PrintBeginLine("=> new(new ")
                                .Print(typeName).Print("(baseValue, currentValue), default, produceChangeEvents, ")
                                .Print("TypeId.EncodeToStatUserData(Type.").Print(typeName).PrintEndLine("));");
                            p.PrintEndLine();
                        }
                    }
                    p.CloseScope();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }

        private readonly void WriteOptionsStruct(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.Print("#region    OPTIONS").PrintEndLine();
            p.Print("#endregion =======").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Options");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Contains optional stat data structures for <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Provides <see cref=\"Options.Data\"/> for per-stat stat data options and");
                p.PrintBeginLine("/// ").PrintEndLine("<see cref=\"Options.ProduceChangeEvents\"/> for per-stat change-event flags.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public static partial class Options");
                p.OpenScope();
                {
                    p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                    p.PrintLine("public partial struct Data");
                    p.OpenScope();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var statData = statDataCollection[i];
                            var fieldName = statData.fieldName;
                            var typeName = statData.typeName;

                            p.PrintBeginLine("public ET.Option<").Print(typeName).Print("> ")
                                .Print(fieldName).PrintEndLine(";");
                        }

                        p.PrintEndLine();

                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("public Data(");
                        p = p.IncreasedIndent();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var comma = i > 0 ? ", " : "  ";
                                var statData = statDataCollection[i];
                                var fieldName = statData.fieldName;
                                var typeName = statData.typeName;

                                p.PrintBeginLine(comma).Print("ET.Option<").Print(typeName).Print("> ")
                                    .Print(fieldName).PrintEndLine(" = default");
                            }
                        }
                        p = p.DecreasedIndent();
                        p.PrintLine(")");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var statData = statDataCollection[i];
                                var fieldName = statData.fieldName;

                                p.PrintBeginLine("this.").Print(fieldName).Print(" = ")
                                    .Print(fieldName).PrintEndLine(";");
                            }
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("public bool TrySet(Type type, in StatSystem.Stat stat)");
                        p.OpenScope();
                        {
                            p.PrintLine("switch (type)");
                            p.OpenScope();
                            {
                                for (var i = 0; i < count; i++)
                                {
                                    var statData = statDataCollection[i];
                                    var fieldName = statData.fieldName;
                                    var typeName = statData.typeName;

                                    p.PrintBeginLine("case Type.").Print(typeName).PrintEndLine(":");
                                    p.OpenScope();
                                    {
                                        p.PrintBeginLine("this.").Print(fieldName).Print(" = ")
                                            .Print("StatSystem.API.MakeStatData<").Print(typeName)
                                            .PrintEndLine(">(stat.ValuePair);");
                                        p.PrintLine("return true;");
                                    }
                                    p.CloseScope();
                                    p.PrintEndLine();
                                }

                                p.PrintLine("default:");
                                p.OpenScope();
                                {
                                    p.PrintLine("return false;");
                                }
                                p.CloseScope();
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                    p.PrintLine("public partial struct ProduceChangeEvents");
                    p.OpenScope();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var statData = statDataCollection[i];
                            var fieldName = statData.fieldName;

                            p.PrintBeginLine("public ET.Option<bool> ")
                                .Print(fieldName).PrintEndLine(";");
                        }

                        p.PrintEndLine();

                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("public ProduceChangeEvents(");
                        p = p.IncreasedIndent();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var comma = i > 0 ? ", " : "  ";
                                var statData = statDataCollection[i];
                                var fieldName = statData.fieldName;

                                p.PrintBeginLine(comma).Print("ET.Option<bool> ")
                                    .Print(fieldName).PrintEndLine(" = default");
                            }
                        }
                        p = p.DecreasedIndent();
                        p.PrintLine(")");
                        p.OpenScope();
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var statData = statDataCollection[i];
                                var fieldName = statData.fieldName;

                                p.PrintBeginLine("this.").Print(fieldName).Print(" = ")
                                    .Print(fieldName).PrintEndLine(";");
                            }
                        }
                        p.CloseScope();
                        p.PrintEndLine();

                        p.PrintLine("public bool TrySet(Type type, in StatSystem.Stat stat)");
                        p.OpenScope();
                        {
                            p.PrintLine("switch (type)");
                            p.OpenScope();
                            {
                                for (var i = 0; i < count; i++)
                                {
                                    var statData = statDataCollection[i];
                                    var fieldName = statData.fieldName;
                                    var typeName = statData.typeName;

                                    p.PrintBeginLine("case Type.").Print(typeName).PrintEndLine(":");
                                    p.OpenScope();
                                    {
                                        p.PrintBeginLine("this.").Print(fieldName)
                                            .PrintEndLine(" = stat.ProduceChangeEvents;");
                                        p.PrintLine("return true;");
                                    }
                                    p.CloseScope();
                                    p.PrintEndLine();
                                }

                                p.PrintLine("default:");
                                p.OpenScope();
                                {
                                    p.PrintLine("return false;");
                                }
                                p.CloseScope();
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private readonly void WriteBakerStructs(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.Print("#region    BUILDER").PrintEndLine();
            p.Print("#endregion =====").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Builder");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Factory class for creating <see cref=\"Builder{T}\"/> instances for <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public static partial class Builder");
                p.OpenScope();
                {
                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static Builder<").Print(typeName)
                        .PrintEndLine("> Create(StatSystem.Builder builder)");
                    p.OpenScope();
                    {
                        p.PrintLine("return new() { builder = builder, statCollection = new(), };");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    // Replaces Bake(IBaker, Entity, ...): creation goes through the store now, and
                    // StatSystem.API.CreateStatOwner is what seeds the None stat at index 0 (D-102).
                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static Builder<").Print(typeName)
                        .Print("> Build(ref AGES.StatStore<StatSystem.Stat, StatSystem.StatModifier")
                        .Print(", StatSystem.StatObserver> store, out AGES.StatOwnerHandle owner, ")
                        .PrintEndLine("StatSystem.ValuePair.Composer valuePairComposer = default)");
                    p.OpenScope();
                    {
                        p.PrintLine("var builder = StatSystem.API.CreateStatOwner("
                            + "ref store, out owner, valuePairComposer: valuePairComposer);");
                        p.PrintLine("return new() { builder = builder, statCollection = new(), };");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static Builder<").Print(typeName)
                        .Print("> Build(ref AGES.StatStore<StatSystem.Stat, StatSystem.StatModifier")
                        .Print(", StatSystem.StatObserver> store, ")
                        .PrintEndLine("StatSystem.ValuePair.Composer valuePairComposer = default)");
                    p.WithIncreasedIndent()
                        .PrintLine("=> Build(ref store, out _, valuePairComposer);");
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    BUILDER<T>").PrintEndLine();
            p.Print("#endregion ========").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Builder<T>");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Provides functionality to create and configure stat components for <see cref=\"").Print(typeName).PrintEndLine("\"/> during owner baking.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Create via <see cref=\"Builder.Create\"/> or <see cref=\"Builder.Bake\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("<typeparam name=\"T\">The component data type to bake into. Must be layout-compatible with the stat collection type.</typeparam>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public partial struct Builder<T> where T : unmanaged");
                p.OpenScope();
                {
                    p.PrintLine("public StatSystem.Builder builder;");
                    p.PrintBeginLine("public ").Print(typeName).PrintEndLine(" statCollection;");
                    p.PrintEndLine();

                    p.PrintLine("static Builder()");
                    p.OpenScope();
                    {
                        p.PrintLine("ThrowIfCannotCastToType<T>();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    WriteResetMethod(ref p);
                    WriteCreateAllStatsMethod(ref p, statDataCollection.AsReadOnlySpan());
                    WriteCreateStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteCreateStatMethod(ref p, statDataCollection[i]);
                    }

                    WriteSetStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteSetStatMethod(ref p, statDataCollection[i]);
                    }

                    WriteSetOrCreateStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteSetOrCreateStatMethod(ref p, statDataCollection[i]);
                    }

                    WriteCreateComponentMethod(ref p, typeName);
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            return;

            static void WriteResetMethod(ref Printer p)
            {
                p.PrintLine("public Builder<T> Reset(StatSystem.ValuePair.Composer valuePairComposer = default)");
                p.OpenScope();
                {
                    p.PrintLine("ref var indices = ref statCollection.indices;");
                    p.PrintLine("indices.AsSpan().Fill(default);");
                    p.PrintLine("builder.Clear(valuePairComposer);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteCreateAllStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine("public Builder<T> CreateAllStats(bool produceChangeEvents = false)");
                p.OpenScope();
                {
                    p.PrintLine("ref var indices = ref statCollection.indices;");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];

                        p.PrintBeginLine("indices.").Print(statData.fieldName)
                            .Print(" = builder.CreateStatHandle(new ").Print(statData.typeName)
                            .Print("(), produceChangeEvents, TypeId.EncodeToStatUserData(Type.")
                            .Print(statData.typeName).PrintEndLine(")).index;");
                    }

                    p.PrintEndLine();
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteCreateStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine("public Builder<T> CreateStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var comma = i > 0 ? ", " : "  ";

                        p.PrintBeginLine(comma).Print("AGES.StatDataParams<").Print(statData.typeName).Print("> ")
                            .Print(statData.fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("ref var indices = ref statCollection.indices;");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("if (").Print(fieldName).Print(".IsCreated && ")
                            .Print(fieldName).PrintEndLine(".StatData.HasValue)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("indices.").Print(fieldName)
                                .Print(" = builder.CreateStatHandle(")
                                .Print(fieldName).Print(".StatData.GetValueOrThrow(), ")
                                .Print(fieldName).Print(".ProduceChangeEvents.GetValueOrDefault(), ")
                                .Print("TypeId.EncodeToStatUserData(Type.").Print(statData.typeName)
                                .PrintEndLine(")).index;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteCreateStatMethod(ref Printer p, StatDataSpec statData)
            {
                p.PrintBeginLine("public Builder<T> CreateStat(AGES.StatDataParams<").Print(statData.typeName)
                    .PrintEndLine("> value)");
                p.OpenScope();
                {
                    p.PrintLine("if (value.IsCreated && value.StatData.HasValue)");
                    p.OpenScope();
                    {
                        p.PrintBeginLine("statCollection.indices.").Print(statData.fieldName)
                            .Print(" = builder.CreateStatHandle(value.StatData.GetValueOrThrow(), ")
                            .Print("value.ProduceChangeEvents.GetValueOrDefault(), ")
                            .Print("TypeId.EncodeToStatUserData(Type.").Print(statData.typeName)
                            .PrintEndLine(")).index;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteSetStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine("public Builder<T> SetStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var comma = i > 0 ? ", " : "  ";

                        p.PrintBeginLine(comma).Print("AGES.StatDataParams<").Print(statData.typeName).Print("> ")
                            .Print(statData.fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("var owner = builder.Owner;");
                    p.PrintLine("ref var indices = ref statCollection.indices;");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var typeName = statData.typeName;
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("if (").Print(fieldName).Print(".IsCreated && ")
                            .Print(fieldName).PrintEndLine(".StatData.HasValue)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("var handle = new AGES.StatHandle<").Print(typeName)
                                .Print(">(owner, indices.")
                                .Print(fieldName).PrintEndLine(");");
                            p.PrintBeginLine("var userData = TypeId.EncodeToStatUserData(Type.")
                                .Print(typeName).PrintEndLine(");");
                            p.PrintEndLine();

                            p.PrintBeginLine("ThrowCannotSetIfDoesNotExist(builder, handle, userData, \"")
                                .Print(typeName).PrintEndLine("\");");
                            p.PrintEndLine();

                            p.PrintBeginLine("builder.SetStat(handle, ")
                                .Print(fieldName).Print(".StatData.GetValueOrThrow(), ")
                                .Print(fieldName).PrintEndLine(".ProduceChangeEvents.GetValueOrDefault(), userData);");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteSetStatMethod(ref Printer p, StatDataSpec statData)
            {
                p.PrintBeginLine("public Builder<T> SetStat(AGES.StatDataParams<").Print(statData.typeName)
                    .PrintEndLine("> value)");
                p.OpenScope();
                {
                    p.PrintLine("if (value.IsCreated && value.StatData.HasValue)");
                    p.OpenScope();
                    {
                        var fieldName = statData.fieldName;
                        var typeName = statData.typeName;

                        p.PrintBeginLine("var handle = new AGES.StatHandle<").Print(typeName)
                            .Print(">(builder.Owner, statCollection.indices.")
                            .Print(fieldName).PrintEndLine(");");
                        p.PrintBeginLine("var userData = TypeId.EncodeToStatUserData(Type.")
                            .Print(typeName).PrintEndLine(");");
                        p.PrintEndLine();

                        p.PrintBeginLine("ThrowCannotSetIfDoesNotExist(builder, handle, userData, \"")
                            .Print(typeName).PrintEndLine("\");");
                        p.PrintEndLine();

                        p.PrintBeginLine("builder.SetStat(handle, value.StatData.GetValueOrThrow(), ")
                            .PrintEndLine("value.ProduceChangeEvents.GetValueOrDefault(), userData);");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteSetOrCreateStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine("public Builder<T> SetOrCreateStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var comma = i > 0 ? ", " : "  ";

                        p.PrintBeginLine(comma).Print("AGES.StatDataParams<").Print(statData.typeName).Print("> ")
                            .Print(statData.fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("var owner = builder.Owner;");
                    p.PrintLine("ref var indices = ref statCollection.indices;");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("if (").Print(fieldName).Print(".IsCreated && ")
                            .Print(fieldName).PrintEndLine(".StatData.HasValue)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("ref var index = ref indices.").Print(fieldName).PrintEndLine(";");

                            p.PrintBeginLine("index = builder.SetStatOrCreateHandle(new(owner, index), ")
                                .Print(fieldName).Print(".StatData.GetValueOrThrow(), ")
                                .Print(fieldName).Print(".ProduceChangeEvents.GetValueOrDefault(), ")
                                .Print("TypeId.EncodeToStatUserData(Type.").Print(statData.typeName)
                                .PrintEndLine(")).index;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteSetOrCreateStatMethod(ref Printer p, StatDataSpec statData)
            {
                p.PrintBeginLine("public Builder<T> SetOrCreateStat(AGES.StatDataParams<").Print(statData.typeName)
                    .PrintEndLine("> value)");
                p.OpenScope();
                {
                    p.PrintLine("if (value.IsCreated && value.StatData.HasValue)");
                    p.OpenScope();
                    {
                        p.PrintBeginLine("ref var index = ref statCollection.indices.")
                            .Print(statData.fieldName).PrintEndLine(";");

                        p.PrintBeginLine("index = builder.SetStatOrCreateHandle(new(builder.Owner, index),")
                            .Print(" value.StatData.GetValueOrThrow(), value.ProduceChangeEvents.GetValueOrDefault(), ")
                            .Print("TypeId.EncodeToStatUserData(Type.").Print(statData.typeName)
                            .PrintEndLine(")).index;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteCreateComponentMethod(ref Printer p, string typeName)
            {
                // Replaces CreateComponentData<T>() + AddComponentToEntity<T>(): there is no
                // component to add, so the collection struct is simply handed back and the caller
                // stores it wherever it likes (D-105).
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Returns the built <see cref=\"").Print(typeName)
                    .PrintEndLine("\"/>, holding a stat handle per declared stat.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Store it anywhere: a field, a NativeList, a dictionary keyed by your own id.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public readonly ").Print(typeName).PrintEndLine(" ToStats()");
                p.WithIncreasedIndent().PrintLine("=> statCollection;");
                p.PrintEndLine();

                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Reinterprets the collection as <typeparamref name=\"TComponentData\"/>, ")
                    .PrintEndLine("which must have the same layout and size.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<seealso cref=\"UCLU.UnsafeUtility.As{U, T}\"/>");
                p.PrintLine("public Builder<TComponentData> As<TComponentData>()");
                p.WithIncreasedIndent().PrintLine("where TComponentData : unmanaged");
                p.OpenScope();
                {
                    p.PrintLine("ThrowIfCannotCastToType<TComponentData>();");
                    p.PrintEndLine();

                    p.PrintLine("return new() { builder = builder, statCollection = statCollection, };");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }

        private readonly void WriteAccessorStructs(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.Print("#region    ACCESSOR").PrintEndLine();
            p.Print("#endregion ========").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Accessor");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Factory class for creating <see cref=\"Accessor{T}\"/> instances for <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public static partial class Accessor");
                p.OpenScope();
                {
                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static Accessor<T> Create<T>(AGES.StatOwnerHandle owner, T statCollection, ")
                        .PrintEndLine("StatSystem.Accessor accessor, StatSystem.WorldData worldData)");
                    p = p.IncreasedIndent();
                    {
                        p.PrintLine("where T : unmanaged");
                    }
                    p = p.DecreasedIndent();
                    p.OpenScope();
                    {
                        p.PrintBeginLine("return new() { owner = owner, accessor = accessor, worldData = worldData")
                            .Print(", statCollection = ").Print(typeName).Print(".CastFrom(statCollection)")
                            .PrintEndLine(" };");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    ACCESSOR<T>").PrintEndLine();
            p.Print("#endregion ===========").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Accessor<T>");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Provides read and write access to stat data for <see cref=\"").Print(typeName).PrintEndLine("\"/> on an owner.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Create via <see cref=\"Accessor.Create\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("<typeparam name=\"T\">The component data type to access. Must be layout-compatible with the stat collection type.</typeparam>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public partial struct Accessor<T> where T : unmanaged");
                p.OpenScope();
                {
                    p.PrintLine("public StatSystem.Accessor accessor;");
                    p.PrintLine("public StatSystem.WorldData worldData;");
                    p.PrintLine("public AGES.StatOwnerHandle owner;");
                    p.PrintBeginLine("public ").Print(typeName).PrintEndLine(" statCollection;");
                    p.PrintEndLine();

                    p.PrintLine("static Accessor()");
                    p.OpenScope();
                    {
                        p.PrintLine("ThrowIfCannotCastFromType<T>();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly bool IsCreated");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => owner != AGES.StatOwnerHandle.Null && worldData.IsCreated;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly Indices Indices");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => statCollection.indices;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    WriteTryCreateAllStatsMethod(ref p, statDataCollection.AsReadOnlySpan());
                    WriteTryCreateStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteTryCreateStatMethod(ref p, statDataCollection[i]);
                    }

                    for (var i = 0; i < count; i++)
                    {
                        WriteTryCreateOrsSetStatMethod(ref p, statDataCollection[i]);
                    }

                    WriteFindValidStatsMethod(ref p);
                    WriteTryGetStatForTypeMethod(ref p);

                    for (var i = 0; i < count; i++)
                    {
                        WriteTryGetStatMethod(ref p, statDataCollection[i]);
                    }

                    for (var i = 0; i < count; i++)
                    {
                        WriteTryGetStatDataMethod(ref p, statDataCollection[i]);
                    }

                    WriteTrySetBaseValueToStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteTrySetStatBaseValueMethod(ref p, statDataCollection[i]);
                    }

                    WriteTrySetCurrentValueToStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteTrySetStatCurrentValueMethod(ref p, statDataCollection[i]);
                    }

                    WriteTrySetValuesToStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteTrySetStatValuesMethod(ref p, statDataCollection[i]);
                    }

                    WriteTrySetProduceChangeEventsForAllStatsMethod(ref p, statDataCollection.AsReadOnlySpan());
                    WriteTrySetProduceChangeEventsForStatsMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteTrySetStatProduceChangeEventsMethod(ref p, statDataCollection[i]);
                    }
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            return;

            static void WriteTryCreateAllStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine("public Accessor<T> TryCreateAllStats(bool produceChangeEvents = false)");
                p.OpenScope();
                {
                    p.PrintLine("var owner = this.owner;");
                    p.PrintLine("ref var indices = ref statCollection.indices;");
                    p.PrintLine("var handles = indices.ToStatHandles(owner);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var typeName = statData.typeName;
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("if (accessor.TryGetStat(handles.").Print(fieldName)
                            .Print(", out _").PrintEndLine(") == false)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("if (accessor.TryCreateStatHandle<").Print(typeName)
                                .Print(">(owner, new ").Print(typeName).Print("(), produceChangeEvents, ")
                                .Print("TypeId.EncodeToStatUserData(Type.").Print(typeName)
                                .PrintEndLine("), out var statHandle))");
                            p.OpenScope();
                            {
                                p.PrintBeginLine("indices.").Print(fieldName).PrintEndLine(" = statHandle.index;");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryCreateStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine("public Accessor<T> TryCreateStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var comma = i > 0 ? ", " : "  ";

                        p.PrintBeginLine(comma).Print("AGES.StatDataParams<").Print(statData.typeName).Print("> ")
                            .Print(statData.fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("var owner = this.owner;");
                    p.PrintLine("ref var indices = ref statCollection.indices;");
                    p.PrintLine("var handles = indices.ToStatHandles(owner);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var typeName = statData.typeName;
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("if (").Print(fieldName)
                            .Print(".IsCreated && ").Print(fieldName)
                            .Print(".StatData.HasValue && accessor.TryGetStat(handles.").Print(fieldName)
                            .Print(", out _").PrintEndLine(") == false)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("if (accessor.TryCreateStatHandle<").Print(typeName)
                                .Print(">(owner, ").Print(fieldName).Print(".StatData.GetValueOrThrow(), ")
                                .Print(fieldName).Print(".ProduceChangeEvents.GetValueOrDefault(), ")
                                .Print("TypeId.EncodeToStatUserData(Type.").Print(typeName)
                                .PrintEndLine("), out var statHandle))");
                            p.OpenScope();
                            {
                                p.PrintBeginLine("indices.").Print(fieldName).PrintEndLine(" = statHandle.index;");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryCreateStatMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintBeginLine("public Accessor<T> TryCreateStat(AGES.StatDataParams<").Print(statData.typeName)
                    .PrintEndLine("> value, out bool success)");
                p.OpenScope();
                {
                    p.PrintLine("success = false;");
                    p.PrintEndLine();

                    p.PrintLine("if (value.IsCreated && value.StatData.HasValue)");
                    p.OpenScope();
                    {
                        p.PrintLine("var owner = this.owner;");
                        p.PrintLine("ref var indices = ref statCollection.indices;");
                        p.PrintBeginLine("var handle = new StatHandle<").Print(typeName)
                            .Print(">(owner, indices.").Print(fieldName).PrintEndLine(");");
                        p.PrintEndLine();

                        p.PrintLine("if (accessor.TryGetStat(handle, out _) == false)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("if (accessor.TryCreateStatHandle<").Print(typeName)
                                .Print(">(owner, value.StatData.GetValueOrThrow(), ")
                                .Print("value.ProduceChangeEvents.GetValueOrDefault(), ")
                                .Print("TypeId.EncodeToStatUserData(Type.").Print(typeName)
                                .PrintEndLine("), out var statHandle))");
                            p.OpenScope();
                            {
                                p.PrintBeginLine("indices.").Print(fieldName).PrintEndLine(" = statHandle.index;");
                                p.PrintLine("success = true;");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryCreateOrsSetStatMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintBeginLine("public Accessor<T> TryCreateOrSetStat(AGES.StatDataParams<").Print(statData.typeName)
                    .PrintEndLine("> value, out bool success)");
                p.OpenScope();
                {
                    p.PrintLine("success = false;");
                    p.PrintEndLine();

                    p.PrintLine("if (value.IsCreated && value.StatData.HasValue)");
                    p.OpenScope();
                    {
                        p.PrintLine("var owner = this.owner;");
                        p.PrintLine("ref var indices = ref statCollection.indices;");
                        p.PrintBeginLine("var handle = new StatHandle<").Print(typeName)
                            .Print(">(owner, indices.").Print(fieldName).PrintEndLine(");");
                        p.PrintBeginLine("var userData = ").Print("TypeId.EncodeToStatUserData(Type.").Print(typeName)
                            .PrintEndLine(");");
                        p.PrintEndLine();

                        p.PrintLine("if (accessor.TryGetStat(handle, out _) == false)");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("if (accessor.TryCreateStatHandle<").Print(typeName)
                                .Print(">(owner, value.StatData.GetValueOrThrow(), ")
                                .Print("value.ProduceChangeEvents.GetValueOrDefault(), ")
                                .PrintEndLine("userData, out var statHandle))");
                            p.OpenScope();
                            {
                                p.PrintBeginLine("indices.").Print(fieldName).PrintEndLine(" = statHandle.index;");
                                p.PrintLine("success = true;");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                        p.PrintLine("else");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("if (success = accessor.TrySetStatData<").Print(typeName)
                                .PrintEndLine(">(handle, value.StatData.GetValueOrThrow(), ref worldData))");
                            p.OpenScope();
                            {
                                p.PrintLine("accessor.TrySetStatUserData(handle, userData);");
                            }
                            p.CloseScope();
                            p.PrintEndLine();

                            p.PrintBeginLine("if (success && ")
                                .PrintEndLine("value.ProduceChangeEvents.TryGetValue(out var produceChangeEvents))");
                            p.OpenScope();
                            {
                                p.PrintLine("accessor.TrySetStatProduceChangeEvents(handle, produceChangeEvents);");
                            }
                            p.CloseScope();
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteFindValidStatsMethod(ref Printer p)
            {
                p.PrintLine("public readonly Accessor<T> FindValidStats(UC.NativeHashMap<TypeId, StatSystem.Stat> result)");
                p.OpenScope();
                {
                    p.PrintLine("result.Clear();");
                    p.PrintLine("ETCol.EncosyNativeHashMapExtensions.IncreaseCapacityTo(result, LENGTH);");
                    p.PrintEndLine();

                    p.PrintLine("var stats = accessor.GetStats(owner);");
                    p.PrintLine("var length = stats.Length;");
                    p.PrintEndLine();

                    p.PrintLine("for (var i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("var stat = stats[i];");
                        p.PrintLine("var type = TypeId.DecodeFromStatUserData(stat.UserData);");
                        p.PrintEndLine();

                        p.PrintLine("if (TypeId.ValidateType(type))");
                        p.OpenScope();
                        {
                            p.PrintLine("result.TryAdd(type, stat);");
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryGetStatForTypeMethod(ref Printer p)
            {
                p.PrintBeginLine("public readonly Accessor<T> TryGetStat(Type type, out StatSystem.Stat stat, ")
                    .PrintEndLine("out AGES.StatHandle handle, out bool success)");
                p.OpenScope();
                {
                    p.PrintLine("if (Indices.TryGet(type, out var index))");
                    p.OpenScope();
                    {
                        p.PrintLine("handle = new AGES.StatHandle(owner, index);");
                        p.PrintBeginLine("success = accessor.TryGetStat(handle, out stat) && ")
                            .PrintEndLine("TypeId.DecodeFromStatUserData(stat.UserData) == type;");
                    }
                    p.CloseScope();
                    p.PrintLine("else");
                    p.OpenScope();
                    {
                        p.PrintLine("handle = default;");
                        p.PrintLine("stat = default;");
                        p.PrintLine("success = false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryGetStatMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public readonly Accessor<T> TryGetStat(out StatSystem.Stat<")
                    .Print(typeName).Print("> stat, out bool success, out AGES.StatHandle<")
                    .Print(typeName).PrintEndLine("> handle, out bool typeMatched)");
                p.OpenScope();
                {
                    p.PrintBeginLine("handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");
                    p.PrintLine("success = accessor.TryGetStat(handle, out stat);");
                    p.PrintBeginLine("typeMatched = TypeId.DecodeFromStatUserData(stat.UserData) == Type.")
                        .Print(typeName).PrintEndLine(";");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryGetStatDataMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintBeginLine("public readonly Accessor<T> TryGetStatData(out ")
                    .Print(typeName).Print(" statData, out bool success, out AGES.StatHandle<").Print(typeName)
                    .PrintEndLine("> handle)");
                p.OpenScope();
                {
                    p.PrintBeginLine("handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");
                    p.PrintEndLine();

                    p.PrintBeginLine("if (accessor.TryGetStat(handle, out var stat) && ")
                        .Print("TypeId.DecodeFromStatUserData(stat.UserData) == Type.")
                        .Print(typeName).PrintEndLine(")");
                    p.OpenScope();
                    {
                        p.PrintBeginLine("statData = StatSystem.API.MakeStatData<")
                            .Print(typeName).PrintEndLine(">(stat.ValuePair);");
                        p.PrintLine("success = true;");
                    }
                    p.CloseScope();
                    p.PrintLine("else");
                    p.OpenScope();
                    {
                        p.PrintLine("statData = default;");
                        p.PrintLine("success = false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetBaseValueToStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintLine("public Accessor<T> TrySetBaseValueToStats(in Options.Data options)");
                p.OpenScope();
                {
                    p.PrintLine("return TrySetBaseValueToStats(");
                    p = p.IncreasedIndent();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var comma = i > 0 ? ", " : "  ";
                            var statData = statDataCollection[i];
                            var fieldName = statData.fieldName;

                            p.PrintBeginLine(comma).Print("options.").Print(fieldName).PrintEndLine(".TryGetBaseValue()");
                        }
                    }
                    p = p.DecreasedIndent();
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public Accessor<T> TrySetBaseValueToStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var comma = i > 0 ? ", " : "  ";
                        var statData = statDataCollection[i];
                        var valueTypeNs = statData.valueTypeNamespace;
                        var valueType = statData.valueType;
                        var fieldName = statData.fieldName;
                        var hasCustomNs = string.IsNullOrEmpty(valueTypeNs) == false;

                        p.PrintBeginLine(comma).Print("ET.Option<")
                            .PrintIf(hasCustomNs, valueTypeNs).PrintIf(hasCustomNs, ".")
                            .Print(valueType).Print("> ")
                            .Print(fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("var handles = Indices.ToStatHandles(owner);");
                    p.PrintLine("var results = ETCol.NativeArray.Create<bool>(LENGTH, UC.Allocator.Temp);");
                    p.PrintBeginLine("var paramsForStats = ETCol.NativeArray.CreateFast")
                        .PrintEndLine("<AGES.StatValueParams<StatSystem.ValuePair>>(LENGTH, UC.Allocator.Temp);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var typeName = statData.typeName;
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("paramsForStats[").Print(i).Print("] = new((")
                            .Print(fieldName).Print(".HasValue ? new ")
                            .Print(typeName).Print(" { baseValue = ").Print(fieldName)
                            .Print(".GetValueOrThrow() }.ToValuePair() : ET.Option.None)")
                            .Print(", (AGES.StatHandle)handles.").Print(fieldName).PrintEndLine(");");
                    }

                    p.PrintEndLine();

                    p.PrintLine("accessor.TrySetBaseValueToStats(paramsForStats, results, ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetStatBaseValueMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public Accessor<T> TrySetStatBaseValue(")
                    .Print(typeName).PrintEndLine(" value, out bool success)");
                p.OpenScope();
                {
                    p.PrintBeginLine("var handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");

                    p.PrintLine("success = accessor.TrySetStatBaseValue(handle, value.ToValuePair(), ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetCurrentValueToStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintLine("public Accessor<T> TrySetCurrentValueToStats(in Options.Data options)");
                p.OpenScope();
                {
                    p.PrintLine("return TrySetCurrentValueToStats(");
                    p = p.IncreasedIndent();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var comma = i > 0 ? ", " : "  ";
                            var statData = statDataCollection[i];
                            var fieldName = statData.fieldName;

                            p.PrintBeginLine(comma).Print("options.").Print(fieldName).PrintEndLine(".TryGetCurrentValue()");
                        }
                    }
                    p = p.DecreasedIndent();
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public Accessor<T> TrySetCurrentValueToStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var comma = i > 0 ? ", " : "  ";
                        var statData = statDataCollection[i];
                        var valueTypeNs = statData.valueTypeNamespace;
                        var valueType = statData.valueType;
                        var fieldName = statData.fieldName;
                        var hasCustomNs = string.IsNullOrEmpty(valueTypeNs) == false;

                        p.PrintBeginLine(comma).Print("ET.Option<")
                            .PrintIf(hasCustomNs, valueTypeNs).PrintIf(hasCustomNs, ".")
                            .Print(valueType).Print("> ")
                            .Print(fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("var handles = Indices.ToStatHandles(owner);");
                    p.PrintLine("var results = ETCol.NativeArray.Create<bool>(LENGTH, UC.Allocator.Temp);");
                    p.PrintBeginLine("var paramsForStats = ETCol.NativeArray.CreateFast")
                        .PrintEndLine("<AGES.StatValueParams<StatSystem.ValuePair>>(LENGTH, UC.Allocator.Temp);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var typeName = statData.typeName;
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("paramsForStats[").Print(i).Print("] = new((")
                            .Print(fieldName).Print(".HasValue ? new ")
                            .Print(typeName).Print(" { currentValue = ").Print(fieldName)
                            .Print(".GetValueOrThrow() }.ToValuePair() :  ET.Option.None)")
                            .Print(", (AGES.StatHandle)handles.").Print(fieldName).PrintEndLine(");");
                    }

                    p.PrintEndLine();

                    p.PrintLine("accessor.TrySetCurrentValueToStats(paramsForStats, results, ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetStatCurrentValueMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public Accessor<T> TrySetStatCurrentValue(")
                    .Print(typeName).PrintEndLine(" value, out bool success)");
                p.OpenScope();
                {
                    p.PrintBeginLine("var handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");

                    p.PrintLine("success = accessor.TrySetStatCurrentValue(handle, value.ToValuePair(), ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetValuesToStatsMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintLine("public Accessor<T> TrySetValuesToStats(in Options.Data options)");
                p.OpenScope();
                {
                    p.PrintLine("return TrySetValuesToStats(");
                    p = p.IncreasedIndent();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var comma = i > 0 ? ", " : "  ";
                            var statData = statDataCollection[i];
                            var fieldName = statData.fieldName;

                            p.PrintBeginLine(comma).Print("options.").Print(fieldName).PrintEndLine();
                        }
                    }
                    p = p.DecreasedIndent();
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public Accessor<T> TrySetValuesToStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var comma = i > 0 ? ", " : "  ";
                        var statData = statDataCollection[i];
                        var typeName = statData.typeName;
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine(comma).Print("ET.Option<").Print(typeName).Print("> ")
                            .Print(fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("var handles = Indices.ToStatHandles(owner);");
                    p.PrintLine("var results = ETCol.NativeArray.Create<bool>(LENGTH, UC.Allocator.Temp);");
                    p.PrintBeginLine("var paramsForStats = ETCol.NativeArray.CreateFast")
                        .PrintEndLine("<AGES.StatValueParams<StatSystem.ValuePair>>(LENGTH, UC.Allocator.Temp);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("paramsForStats[").Print(i).Print("] = new(")
                            .Print(fieldName).Print(".TryGetValuePair(), (AGES.StatHandle)handles.")
                            .Print(fieldName).PrintEndLine(");");
                    }

                    p.PrintEndLine();

                    p.PrintLine("accessor.TrySetDataToStats(paramsForStats, results, ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetStatValuesMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public Accessor<T> TrySetStatValues(")
                    .Print(typeName).PrintEndLine(" value, out bool success)");
                p.OpenScope();
                {
                    p.PrintBeginLine("var handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");

                    p.PrintLine("success = accessor.TrySetStatValues(handle, value.ToValuePair(), ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetProduceChangeEventsForAllStatsMethod(
                  ref Printer p
                , ReadOnlySpan<StatDataSpec> statDataCollection
            )
            {
                var count = statDataCollection.Length;

                p.PrintLine("public Accessor<T> TrySetProduceChangeEventsForAllStats(bool value)");
                p.OpenScope();
                {
                    p.PrintLine("var handles = Indices.ToStatHandles(owner);");
                    p.PrintLine("var results = ETCol.NativeArray.Create<bool>(LENGTH, UC.Allocator.Temp);");
                    p.PrintBeginLine("var paramsForStats = ETCol.NativeArray.CreateFast")
                        .PrintEndLine("<AGES.StatValueParams<StatSystem.ValuePair>>(LENGTH, UC.Allocator.Temp);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("paramsForStats[").Print(i)
                            .Print("] = new( ET.Option.None, (StatHandle)handles.")
                            .Print(fieldName).PrintEndLine(", value);");
                    }

                    p.PrintEndLine();

                    p.PrintLine("accessor.TrySetDataToStats(paramsForStats, results, ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTrySetProduceChangeEventsForStatsMethod(
                  ref Printer p
                , ReadOnlySpan<StatDataSpec> statDataCollection
            )
            {
                var count = statDataCollection.Length;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintLine("public Accessor<T> TrySetProduceChangeEventsForStats(in Options.ProduceChangeEvents options)");
                p.OpenScope();
                {
                    p.PrintLine("return TrySetProduceChangeEventsForStats(");
                    p = p.IncreasedIndent();
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var comma = i > 0 ? ", " : "  ";
                            var statData = statDataCollection[i];
                            var fieldName = statData.fieldName;

                            p.PrintBeginLine(comma).Print("options.").Print(fieldName).PrintEndLine();
                        }
                    }
                    p = p.DecreasedIndent();
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("public Accessor<T> TrySetProduceChangeEventsForStats(");
                p = p.IncreasedIndent();
                {
                    for (var i = 0; i < count; i++)
                    {
                        var comma = i > 0 ? ", " : "  ";
                        var statData = statDataCollection[i];

                        p.PrintBeginLine(comma).Print("ET.Option<bool> ").Print(statData.fieldName).PrintEndLine(" = default");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintLine(")");
                p.OpenScope();
                {
                    p.PrintLine("var handles = Indices.ToStatHandles(owner);");
                    p.PrintLine("var results = ETCol.NativeArray.Create<bool>(LENGTH, UC.Allocator.Temp);");
                    p.PrintBeginLine("var paramsForStats = ETCol.NativeArray.CreateFast")
                        .PrintEndLine("<AGES.StatValueParams<StatSystem.ValuePair>>(LENGTH, UC.Allocator.Temp);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var fieldName = statData.fieldName;

                        p.PrintBeginLine("paramsForStats[").Print(i)
                            .Print("] = new(ET.Option.None, (AGES.StatHandle)handles.")
                            .Print(fieldName).Print(", ").Print(fieldName).PrintEndLine(");");
                    }

                    p.PrintEndLine();

                    p.PrintLine("accessor.TrySetDataToStats(paramsForStats, results, ref worldData);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }


            static void WriteTrySetStatProduceChangeEventsMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public Accessor<T> TrySetStatProduceChangeEvents(ET.Bool<")
                    .Print(typeName).PrintEndLine("> value, out bool success)");
                p.OpenScope();
                {
                    p.PrintBeginLine("var handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");

                    p.PrintLine("success = accessor.TrySetStatProduceChangeEvents(handle, value);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }

        private readonly void WriteReaderStruct(ref Printer p)
        {
            var count = statDataCollection.Count;

            p.Print("#region    READER").PrintEndLine();
            p.Print("#endregion ======").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Reader");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Factory class for creating <see cref=\"Reader{T}\"/> instances for <see cref=\"").Print(typeName).PrintEndLine("\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public static partial class Reader");
                p.OpenScope();
                {
                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static Reader<T> Create<T>(AGES.StatOwnerHandle owner, T statCollection, ")
                        .PrintEndLine("AGES.StatBuffer<StatSystem.Stat> statBuffer)");
                    p = p.IncreasedIndent();
                    {
                        p.PrintLine("where T : unmanaged");
                    }
                    p = p.DecreasedIndent();
                    p.OpenScope();
                    {
                        p.PrintBeginLine("return new() { owner = owner")
                            .Print(", statBuffer = statBuffer.AsArray().AsReadOnly()")
                            .Print(", statCollection = ").Print(typeName).Print(".CastFrom(statCollection)")
                            .PrintEndLine(" };");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            p.Print("#region    READER").PrintEndLine();
            p.Print("#endregion ======").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" // ").PrintEndLine("Reader<T>");
            p.OpenScope();
            {
                p.PrintBeginLine("/// ").PrintEndLine("<summary>");
                p.PrintBeginLine("/// ").Print("Provides read-only access to stat data for <see cref=\"").Print(typeName).PrintEndLine("\"/> on an owner.");
                p.PrintBeginLine("/// ").PrintEndLine("</summary>");
                p.PrintBeginLine("/// ").PrintEndLine("<remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("Create via <see cref=\"Reader.Create\"/>.");
                p.PrintBeginLine("/// ").PrintEndLine("</remarks>");
                p.PrintBeginLine("/// ").PrintEndLine("<typeparam name=\"T\">The component data type to read from. Must be layout-compatible with the stat collection type.</typeparam>");
                p.PrintBeginLine(PR_GENERATED_CODE).PrintEndLine(PR_EXCLUDE_COVERAGE);
                p.PrintLine("public partial struct Reader<T> where T : unmanaged");
                p.OpenScope();
                {
                    p.PrintLine("public AGES.StatOwnerHandle owner;");
                    p.PrintLine("public UC.NativeArray<StatSystem.Stat>.ReadOnly statBuffer;");
                    p.PrintBeginLine("public ").Print(typeName).PrintEndLine(" statCollection;");
                    p.PrintEndLine();

                    p.PrintLine("static Reader()");
                    p.OpenScope();
                    {
                        p.PrintLine("ThrowIfCannotCastFromType<T>();");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly bool IsCreated");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => owner != AGES.StatOwnerHandle.Null && statBuffer.IsCreated;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("public readonly Indices Indices");
                    p.OpenScope();
                    {
                        p.PrintLine(PR_AGGRESSIVE_INLINING);
                        p.PrintLine("get => statCollection.indices;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    WriteContainsTypeMethod(ref p);
                    WriteContainsTStatDataMethod(ref p, statDataCollection.AsReadOnlySpan());

                    for (var i = 0; i < count; i++)
                    {
                        WriteContainsMethod(ref p, statDataCollection[i]);
                    }

                    WriteFindValidStatsMethod(ref p);
                    WriteGetStatDataOptionsMethod(ref p);
                    WriteGetProduceChangeEventsOptionsMethod(ref p);
                    WriteTryGetStatForTypeMethod(ref p);

                    for (var i = 0; i < count; i++)
                    {
                        WriteTryGetStatMethod(ref p, statDataCollection[i]);
                    }

                    for (var i = 0; i < count; i++)
                    {
                        WriteTryGetStatDataMethod(ref p, statDataCollection[i]);
                    }
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            return;

            static void WriteContainsTypeMethod(ref Printer p)
            {
                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintLine("public readonly Reader<T> Contains(Type type, out bool result)");
                p.OpenScope();
                {
                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintLine("result = Indices.ToStatHandles(owner).TryGet(type, out var handle)");
                    p.WithIncreasedIndent()
                        .PrintLine("&& StatSystem.API.Contains(handle, TypeId.EncodeToStatUserData(type), stats);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteContainsTStatDataMethod(ref Printer p, ReadOnlySpan<StatDataSpec> statDataCollection)
            {
                var count = statDataCollection.Length;

                p.PrintLine("public readonly Reader<T> Contains<TStatData>(out bool result)");
                p.WithIncreasedIndent().PrintLine("where TStatData : unmanaged, AGES.IStatData");
                p.OpenScope();
                {
                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintLine("var handles = Indices.ToStatHandles(owner);");
                    p.PrintEndLine();

                    for (var i = 0; i < count; i++)
                    {
                        var statData = statDataCollection[i];
                        var fieldName = statData.fieldName;
                        var typeName = statData.typeName;

                        p.PrintBeginLine("if (typeof(TStatData) == typeof(").Print(statData.typeName).PrintEndLine("))");
                        p.OpenScope();
                        {
                            p.PrintBeginLine("result = StatSystem.API.Contains(handles.")
                                .Print(fieldName).Print(", TypeId.EncodeToStatUserData(Type.")
                                .Print(typeName).PrintEndLine("), stats);");
                            p.PrintLine("return this;");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }

                    p.PrintLine("result = false;");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteContainsMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public readonly Reader<T> Contains(Index<").Print(typeName)
                    .PrintEndLine("> index, out bool result)");
                p.OpenScope();
                {
                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintBeginLine("var handle = new AGES.StatHandle<").Print(typeName).PrintEndLine(">(owner, index);");
                    p.PrintBeginLine("result = StatSystem.API.Contains(handle, TypeId.EncodeToStatUserData(Type.")
                        .Print(typeName).PrintEndLine("), stats);");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteFindValidStatsMethod(ref Printer p)
            {
                p.PrintLine("public readonly Reader<T> FindValidStats(UC.NativeHashMap<TypeId, StatSystem.Stat> result)");
                p.OpenScope();
                {
                    p.PrintLine("result.Clear();");
                    p.PrintLine("ETCol.EncosyNativeHashMapExtensions.IncreaseCapacityTo(result, LENGTH);");
                    p.PrintEndLine();

                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintLine("var length = stats.Length;");
                    p.PrintEndLine();

                    p.PrintLine("for (var i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("var stat = stats[i];");
                        p.PrintLine("var type = TypeId.DecodeFromStatUserData(stat.UserData);");
                        p.PrintEndLine();

                        p.PrintLine("if (TypeId.ValidateType(type))");
                        p.OpenScope();
                        {
                            p.PrintLine("result.TryAdd(type, stat);");
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteGetStatDataOptionsMethod(ref Printer p)
            {
                p.PrintLine("public readonly Reader<T> GetStatDataOptions(out Options.Data result)");
                p.OpenScope();
                {
                    p.PrintLine("result = new();");
                    p.PrintEndLine();

                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintLine("var length = stats.Length;");
                    p.PrintEndLine();

                    p.PrintLine("for (var i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("var stat = stats[i];");
                        p.PrintLine("var type = TypeId.DecodeFromStatUserData(stat.UserData);");
                        p.PrintEndLine();

                        p.PrintLine("if (TypeId.ValidateType(type))");
                        p.OpenScope();
                        {
                            p.PrintLine("result.TrySet(type, stat);");
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteGetProduceChangeEventsOptionsMethod(ref Printer p)
            {
                p.PrintLine("public readonly Reader<T> GetProduceChangeEventsOptions(out Options.ProduceChangeEvents result)");
                p.OpenScope();
                {
                    p.PrintLine("result = new();");
                    p.PrintEndLine();

                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintLine("var length = stats.Length;");
                    p.PrintEndLine();

                    p.PrintLine("for (var i = 0; i < length; i++)");
                    p.OpenScope();
                    {
                        p.PrintLine("var stat = stats[i];");
                        p.PrintLine("var type = TypeId.DecodeFromStatUserData(stat.UserData);");
                        p.PrintEndLine();

                        p.PrintLine("if (TypeId.ValidateType(type))");
                        p.OpenScope();
                        {
                            p.PrintLine("result.TrySet(type, stat);");
                        }
                        p.CloseScope();
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryGetStatForTypeMethod(ref Printer p)
            {
                p.PrintBeginLine("public readonly Reader<T> TryGetStat(Type type, out StatSystem.Stat stat, ")
                    .PrintEndLine("out AGES.StatHandle handle, out bool success)");
                p.OpenScope();
                {
                    p.PrintLine("if (Indices.TryGet(type, out var index))");
                    p.OpenScope();
                    {
                        p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                        p.PrintLine("handle = new AGES.StatHandle(owner, index);");
                        p.PrintBeginLine("success = StatSystem.API.TryGetStat(handle, stats, out stat) && ")
                            .PrintEndLine("TypeId.DecodeFromStatUserData(stat.UserData) == type;");
                    }
                    p.CloseScope();
                    p.PrintLine("else");
                    p.OpenScope();
                    {
                        p.PrintLine("handle = default;");
                        p.PrintLine("stat = default;");
                        p.PrintLine("success = false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryGetStatMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public readonly Reader<T> TryGetStat(out StatSystem.Stat<")
                    .Print(typeName).Print("> stat, out bool success, out AGES.StatHandle<")
                    .Print(typeName).PrintEndLine("> handle, out bool typeMatched)");
                p.OpenScope();
                {
                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintBeginLine("handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");
                    p.PrintLine("success = StatSystem.API.TryGetStat(handle, stats, out stat);");
                    p.PrintBeginLine("typeMatched = TypeId.DecodeFromStatUserData(stat.UserData) == Type.")
                        .Print(typeName).PrintEndLine(";");
                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }

            static void WriteTryGetStatDataMethod(ref Printer p, StatDataSpec statData)
            {
                var typeName = statData.typeName;
                var fieldName = statData.fieldName;

                p.PrintBeginLine("public readonly Reader<T> TryGetStatData(out ")
                    .Print(typeName).Print(" statData, out bool success, out AGES.StatHandle<").Print(typeName)
                    .PrintEndLine("> handle)");
                p.OpenScope();
                {
                    p.PrintLine("var stats = statBuffer.AsReadOnlySpan();");
                    p.PrintBeginLine("handle = new AGES.StatHandle<").Print(typeName)
                        .Print(">(owner, Indices.").Print(fieldName).PrintEndLine(");");
                    p.PrintEndLine();

                    p.PrintBeginLine("if (StatSystem.API.TryGetStat(handle, stats, out var stat) && ")
                        .Print("TypeId.DecodeFromStatUserData(stat.UserData) == Type.")
                        .Print(typeName).PrintEndLine(")");
                    p.OpenScope();
                    {
                        p.PrintBeginLine("statData = StatSystem.API.MakeStatData<")
                            .Print(typeName).PrintEndLine(">(stat.ValuePair);");
                        p.PrintLine("success = true;");
                    }
                    p.CloseScope();
                    p.PrintLine("else");
                    p.OpenScope();
                    {
                        p.PrintLine("statData = default;");
                        p.PrintLine("success = false;");
                    }
                    p.CloseScope();
                    p.PrintEndLine();

                    p.PrintLine("return this;");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }

        private readonly void WriteExtensionsClass(ref Printer p)
        {
            var count = statDataCollection.Count;
            var statCollectionType = typeName;

            p.Print("#region    EXTENSIONS").PrintEndLine();
            p.Print("#endregion ==========").PrintEndLine();
            p.PrintEndLine();

            p.PrintBeginLine("partial struct ").Print(typeName).Print(" { }")
                .Print(" // ").Print(typeName).PrintEndLine("Extensions");
            p.PrintEndLine();

            p.PrintBeginLine("public static partial class ").Print(typeName).PrintEndLine("Extensions");
            p.OpenScope();
            {
                for (var i = 0; i < count; i++)
                {
                    var statData = statDataCollection[i];
                    var typeName = statData.typeName;
                    var valueTypeNs = statData.valueTypeNamespace;
                    var valueType = statData.valueType;
                    var hasCustomNs = string.IsNullOrEmpty(valueTypeNs) == false;
                    var singleValue = statData.singleValue;

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static ET.Option<StatSystem.ValuePair> TryGetValuePair(this ET.Option<")
                        .Print(statCollectionType).Print(".").Print(typeName).PrintEndLine("> value)");
                    p.WithIncreasedIndent().PrintBeginLine("=> value.TryGetValue(out var stat) ")
                        .PrintEndLine("? stat.ToValuePair() : ET.Option.None;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static ET.Option<")
                        .PrintIf(hasCustomNs, valueTypeNs).PrintIf(hasCustomNs, ".")
                        .Print(valueType).Print("> TryGetBaseValue(this ET.Option<")
                        .Print(statCollectionType).Print(".").Print(typeName).PrintEndLine("> value)");
                    p.WithIncreasedIndent().PrintBeginLine("=> value.TryGetValue(out var stat) ")
                        .Print("? stat.").PrintIf(singleValue, "value", "baseValue").PrintEndLine(" : ET.Option.None;");
                    p.PrintEndLine();

                    p.PrintLine(PR_AGGRESSIVE_INLINING);
                    p.PrintBeginLine("public static ET.Option<")
                        .PrintIf(hasCustomNs, valueTypeNs).PrintIf(hasCustomNs, ".")
                        .Print(valueType).Print("> TryGetCurrentValue(this ET.Option<")
                        .Print(statCollectionType).Print(".").Print(typeName).PrintEndLine("> value)");
                    p.WithIncreasedIndent().PrintBeginLine("=> value.TryGetValue(out var stat) ")
                        .Print("? stat.").PrintIf(singleValue, "value", "currentValue").PrintEndLine(" : ET.Option.None;");
                    p.PrintEndLine();
                }

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public static TComponentData ToComponent<TComponentData>(this ")
                    .Print(typeName).PrintEndLine(".Builder<TComponentData> builder)");
                p.WithIncreasedIndent().PrintLine("where TComponentData : unmanaged");
                p.OpenScope();
                {
                    p.PrintBeginLine("return UCLU.UnsafeUtility.As<")
                        .Print(typeName).PrintEndLine(", TComponentData>(ref builder.statCollection);");
                }
                p.CloseScope();
                p.PrintEndLine();

                // AddComponentToEntity is dropped: there is no entity and no component to attach.
                // The caller takes the value from ToStats() and stores it itself (D-105).

                p.PrintLine(PR_AGGRESSIVE_INLINING);
                p.PrintBeginLine("public static TComponentData ToComponent<TComponentData>(this ")
                    .Print(typeName).PrintEndLine(".Accessor<TComponentData> accessor)");
                p.WithIncreasedIndent().PrintLine("where TComponentData : unmanaged");
                p.OpenScope();
                {
                    p.PrintBeginLine("return UCLU.UnsafeUtility.As<")
                        .Print(typeName).PrintEndLine(", TComponentData>(ref accessor.statCollection);");
                }
                p.CloseScope();
                p.PrintEndLine();

                // The three SetComponentToEntity overloads (EntityManager / ECB / ECB.ParallelWriter)
                // are dropped along with the component model (D-105).
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private static void WriteHelperConstants(ref Printer p)
        {
            p.PrintBeginLine("private const ").Print(PR_METHOD_IMPL_OPTIONS)
                .Print(" INLINING = ").Print(PR_INLINING).PrintEndLine(";");
            p.PrintEndLine();

            p.PrintBeginLine("private const string GENERATOR = ").Print(PR_GENERATOR).PrintEndLine(";");
            p.PrintEndLine();
        }

        private readonly void WriteThrowMethods(ref Printer p, string indexType)
        {
            p.PrintLine(PR_VALIDATION_ATTRIBUTES);
            p.PrintBeginLine("private static void ThrowCannotSetIfDoesNotExist(")
                .PrintEndLine("StatSystem.Builder builder, StatHandle handle, uint userData, string statDataName)");
            p.OpenScope();
            {
                p.PrintLine("if (builder.Contains(handle) == false)");
                p.OpenScope();
                {
                    p.PrintLine("throw new S.InvalidOperationException(");
                    p.WithIncreasedIndent().PrintBeginLine("$\"")
                        .Print("Cannot set data for stat '{statDataName}' because the handle index is invalid. ")
                        .Print("Please consider using CreateAllStats first.")
                        .PrintEndLine("\"");
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();

                p.PrintLine("if (builder.Contains(handle, userData) == false)");
                p.OpenScope();
                {
                    p.PrintLine("throw new S.InvalidOperationException(");
                    p.WithIncreasedIndent().PrintBeginLine("$\"")
                        .Print("Cannot set data for stat '{statDataName}' because the stored user data ")
                        .Print("does not equal to '{TypeId.DecodeFromStatUserData(userData)}'.")
                        .Print("Please double-check the creation of the handle '{handle}'.")
                        .PrintEndLine("\"");
                    p.PrintLine(");");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_VALIDATION_ATTRIBUTES);
            p.PrintLine("private static void ThrowIfCannotCastToType<T>()");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintBeginLine("if (UCLU.UnsafeUtility.SizeOf<").Print(typeName)
                    .PrintEndLine(">() == UCLU.UnsafeUtility.SizeOf<T>()) return;");
                p.PrintEndLine();

                p.PrintLine("throw new S.InvalidCastException(");
                p.WithIncreasedIndent().PrintBeginLine("$\"")
                    .Print("Cannot cast '").Print(typeName).Print("' into '{typeof(T)}' because ")
                    .Print("these types are not of the same size.")
                    .PrintEndLine("\"");
                p.PrintLine(");");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_VALIDATION_ATTRIBUTES);
            p.PrintLine("private static void ThrowIfCannotCastFromType<T>()");
            p.WithIncreasedIndent().PrintLine("where T : unmanaged");
            p.OpenScope();
            {
                p.PrintBeginLine("if (UCLU.UnsafeUtility.SizeOf<").Print(typeName)
                    .PrintEndLine(">() == UCLU.UnsafeUtility.SizeOf<T>()) return;");
                p.PrintEndLine();

                p.PrintLine("throw new S.InvalidCastException(");
                p.WithIncreasedIndent().PrintBeginLine("$\"")
                    .Print("Cannot cast '{typeof(T)}' into '").Print(typeName).Print("' because ")
                    .Print("these types are not of the same size.")
                    .PrintEndLine("\"");
                p.PrintLine(");");
            }
            p.CloseScope();
            p.PrintEndLine();

            p.PrintLine(PR_VALIDATION_ATTRIBUTES);
            p.PrintLine("private static void ThrowIfTypesLengthExceedsStatUserDataCapacity()");
            p.OpenScope();
            {
                var count = statDataCollection.Count;

                p.PrintBeginLine("if (StatSystem.Stat.USER_DATA_SIZE >= UCLU.UnsafeUtility.SizeOf<")
                    .Print(indexType).PrintEndLine(">()) return;");
                p.PrintEndLine();

                p.PrintLine("throw new S.OverflowException(");
                p.WithIncreasedIndent().PrintBeginLine("$\"")
                    .Print("'").Print(typeName).Print("' contains ").Print(count).Print(" stat data types ")
                    .Print("thus exceeds the maximum capacity of '").Print(statSystemFullTypeName)
                    .Print(".Stat.UserData' which is ").Print("{StatSystem.Stat.USER_DATA_SIZE} bytes.")
                    .PrintEndLine("\"");
                p.PrintLine(");");
            }
            p.CloseScope();
            p.PrintEndLine();
        }
    }
}




