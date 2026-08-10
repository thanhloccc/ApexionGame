using System.Collections.Immutable;
using ApexionGame.SourceGen.Helpers.Entities.Stats;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApexionGame.SourceGen.Analyzers.Entities.Stats
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class StatDataDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private const string NAMESPACE = StatTypeTable.NAMESPACE;
        private const string STAT_DATA_ATTRIBUTE = $"global::{NAMESPACE}.StatDataAttribute";
        private const string STAT_SYSTEM_ATTRIBUTE = $"global::{NAMESPACE}.StatSystemAttribute";
        private const string STAT_COLLECTION_ATTRIBUTE = $"global::{NAMESPACE}.StatCollectionAttribute";
        private const string STAT_HANDLE_PREFIX = $"{NAMESPACE}.StatHandle";

        public static readonly DiagnosticDescriptor MustBeStruct = new(
              id: "AGS_STAT_DATA_0001"
            , title: "[StatData] can only be applied to a struct"
            , messageFormat: "\"{0}\" is not a struct. [StatData] can only be applied to struct types."
            , category: "StatDataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "[StatData] can only be applied to struct types."
        );

        public static readonly DiagnosticDescriptor MustNotBeGeneric = new(
              id: "AGS_STAT_DATA_0002"
            , title: "[StatData] cannot be applied to a generic struct"
            , messageFormat: "\"{0}\" is a generic type. [StatData] can only be applied to non-generic structs."
            , category: "StatDataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "[StatData] can only be applied to non-generic struct types."
        );

        public static readonly DiagnosticDescriptor StatVariantTypeMustNotBeNone = new(
              id: "AGS_STAT_DATA_0003"
            , title: "StatVariantType.None is not a valid argument for [StatData]"
            , messageFormat: "The [StatData] attribute on \"{0}\" uses StatVariantType.None, which is not a valid variant type and will produce no output."
            , category: "StatDataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "StatVariantType.None is reserved and cannot be used as the type argument for [StatData]. Use a concrete StatVariantType value instead."
        );

        public static readonly DiagnosticDescriptor TypeofArgMustBeEnum = new(
              id: "AGS_STAT_DATA_0004"
            , title: "typeof argument of [StatData] must be an enum type"
            , messageFormat: "\"{0}\" is not an enum. The typeof argument of [StatData] must resolve to an enum type."
            , category: "StatDataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type passed to [StatData(typeof(...))] must be an enum type."
        );

        // New in this port. FilterTypes silently drops a type whose size exceeds MaxDataSize, so
        // without this the stat just never appears and the union quietly loses a member.
        public static readonly DiagnosticDescriptor ValueTypeTooLarge = new(
              id: "AGS_STAT_DATA_0005"
            , title: "[StatData] value type does not fit the stat system's MaxDataSize"
            , messageFormat: "\"{0}\" needs {1} bytes but the stat system allows {2}. A value pair needs twice the size; set SingleValue = true or raise MaxDataSize."
            , category: "StatDataGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "A stat stores base and current values in one union unless SingleValue is set, so a pair needs size <= MaxDataSize / 2."
        );

        // New in this port, guards DEC-004: a modifier holding StatHandle fields must remap them
        // after a load, otherwise it silently points at whatever owner now occupies that slot.
        public static readonly DiagnosticDescriptor ModifierMissingRemap = new(
              id: "AGS_STAT_DATA_0006"
            , title: "StatModifier stores StatHandle but does not implement RemapObservedStatsInternal"
            , messageFormat: "\"{0}\" has a StatHandle field but no RemapObservedStatsInternal implementation, so its handles will not survive a save/load round trip."
            , category: "StatDataGenerator"
            , defaultSeverity: DiagnosticSeverity.Warning
            , isEnabledByDefault: true
            , description: "Implement 'partial void RemapObservedStatsInternal(in StatOwnerRemap remap)' and rewrite every stored StatHandle through it."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                  MustBeStruct
                , MustNotBeGeneric
                , StatVariantTypeMustNotBeNone
                , TypeofArgMustBeEnum
                , ValueTypeTooLarge
                , ModifierMissingRemap
            );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var token = context.CancellationToken;
            token.ThrowIfCancellationRequested();

            if (context.Symbol is not INamedTypeSymbol typeSymbol)
            {
                return;
            }

            if (typeSymbol.HasAttribute(STAT_DATA_ATTRIBUTE, token) == false)
            {
                AnalyzeModifierRemap(context, typeSymbol, token);
                return;
            }

            if (typeSymbol.TypeKind != TypeKind.Struct)
            {
                var location = typeSymbol.Locations.Length > 0
                    ? typeSymbol.Locations[0]
                    : Location.None;

                context.ReportDiagnostic(Diagnostic.Create(
                      MustBeStruct
                    , location
                    , typeSymbol.Name
                ));

                return;
            }

            if (typeSymbol.IsGenericType)
            {
                var location = typeSymbol.Locations.Length > 0
                    ? typeSymbol.Locations[0]
                    : Location.None;

                context.ReportDiagnostic(Diagnostic.Create(
                      MustNotBeGeneric
                    , location
                    , typeSymbol.Name
                ));

                return;
            }

            var attrib = typeSymbol.GetAttribute(STAT_DATA_ATTRIBUTE, token);

            if (attrib == null || attrib.ConstructorArguments.Length < 1)
            {
                return;
            }

            var arg = attrib.ConstructorArguments[0];

            if (arg.Kind == TypedConstantKind.Enum && arg.Value is byte enumValue && enumValue == 0)
            {
                var location = attrib.ApplicationSyntaxReference?.GetSyntax(token)?.GetLocation()
                    ?? typeSymbol.Locations[0];

                context.ReportDiagnostic(Diagnostic.Create(
                      StatVariantTypeMustNotBeNone
                    , location
                    , typeSymbol.Name
                ));

                return;
            }

            if (arg.Kind == TypedConstantKind.Type
                && (arg.Value is not INamedTypeSymbol typeArg
                    || typeArg.TypeKind != TypeKind.Enum)
            )
            {
                var displayName = (arg.Value as ISymbol)?.ToDisplayString() ?? arg.Value?.ToString() ?? "?";
                var location = attrib.ApplicationSyntaxReference?.GetSyntax(token)?.GetLocation()
                    ?? typeSymbol.Locations[0];

                context.ReportDiagnostic(Diagnostic.Create(
                      TypeofArgMustBeEnum
                    , location
                    , displayName
                ));

                return;
            }

            AnalyzeSize(context, typeSymbol, attrib, arg, token);
        }

        /// <summary>
        /// Reports <see cref="ValueTypeTooLarge"/> when the value type cannot fit the union of the
        /// stat system this collection targets.
        /// </summary>
        private static void AnalyzeSize(
              SymbolAnalysisContext context
            , INamedTypeSymbol typeSymbol
            , AttributeData attrib
            , TypedConstant arg
            , System.Threading.CancellationToken token
        )
        {
            if (TryGetMaxDataSize(typeSymbol, token, out var maxDataSize) == false)
            {
                return;
            }

            var size = 0;

            if (arg.Kind == TypedConstantKind.Enum && arg.Value is byte enumByte)
            {
                var sizes = StatTypeTable.Sizes;

                if ((uint)enumByte >= (uint)sizes.Length)
                {
                    return;
                }

                size = sizes[enumByte];
            }
            else if (arg.Kind == TypedConstantKind.Type
                && arg.Value is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType
            )
            {
                size = enumType.EnumUnderlyingType?.SpecialType switch {
                    SpecialType.System_SByte or SpecialType.System_Byte => 1,
                    SpecialType.System_Int16 or SpecialType.System_UInt16 => 2,
                    SpecialType.System_Int32 or SpecialType.System_UInt32 => 4,
                    SpecialType.System_Int64 or SpecialType.System_UInt64 => 8,
                    _ => 0,
                };
            }

            if (size < 1)
            {
                return;
            }

            var singleValue = false;

            foreach (var namedArg in attrib.NamedArguments)
            {
                if (string.Equals(namedArg.Key, "SingleValue", System.StringComparison.Ordinal)
                    && namedArg.Value.Value is bool value)
                {
                    singleValue = value;
                }
            }

            // A pair stores base and current side by side, so it needs twice the room.
            var required = singleValue ? size : size * 2;

            if (required <= maxDataSize)
            {
                return;
            }

            var location = attrib.ApplicationSyntaxReference?.GetSyntax(token)?.GetLocation()
                ?? typeSymbol.Locations[0];

            context.ReportDiagnostic(Diagnostic.Create(
                  ValueTypeTooLarge
                , location
                , typeSymbol.Name
                , required
                , maxDataSize
            ));
        }

        /// <summary>
        /// Walks StatData -&gt; containing [StatCollection] -&gt; its [StatSystem] to read MaxDataSize.
        /// </summary>
        private static bool TryGetMaxDataSize(
              INamedTypeSymbol typeSymbol
            , System.Threading.CancellationToken token
            , out int maxDataSize
        )
        {
            maxDataSize = 0;

            var collection = typeSymbol.ContainingType;

            if (collection is null)
            {
                return false;
            }

            var collectionAttrib = collection.GetAttribute(STAT_COLLECTION_ATTRIBUTE, token);

            if (collectionAttrib == null
                || collectionAttrib.ConstructorArguments.Length < 1
                || collectionAttrib.ConstructorArguments[0].Value is not INamedTypeSymbol statSystem
            )
            {
                return false;
            }

            var systemAttrib = statSystem.GetAttribute(STAT_SYSTEM_ATTRIBUTE, token);

            if (systemAttrib == null
                || systemAttrib.ConstructorArguments.Length < 1
                || systemAttrib.ConstructorArguments[0].Value is not byte value
            )
            {
                return false;
            }

            maxDataSize = value;
            return maxDataSize > 0;
        }

        /// <summary>
        /// Reports <see cref="ModifierMissingRemap"/> on a StatModifier that stores stat handles
        /// but leaves the generated partial hook unimplemented.
        /// </summary>
        private static void AnalyzeModifierRemap(
              SymbolAnalysisContext context
            , INamedTypeSymbol typeSymbol
            , System.Threading.CancellationToken token
        )
        {
            if (typeSymbol.TypeKind != TypeKind.Struct
                || string.Equals(typeSymbol.Name, "StatModifier", System.StringComparison.Ordinal) == false
                || typeSymbol.ContainingType is null
            )
            {
                return;
            }

            if (typeSymbol.ContainingType.HasAttribute(STAT_SYSTEM_ATTRIBUTE, token) == false)
            {
                return;
            }

            var holdsHandle = false;

            foreach (var member in typeSymbol.GetMembers())
            {
                token.ThrowIfCancellationRequested();

                if (member is not IFieldSymbol field || field.IsStatic)
                {
                    continue;
                }

                var fieldTypeName = field.Type.OriginalDefinition.ToDisplayString();

                if (fieldTypeName.StartsWith(STAT_HANDLE_PREFIX, System.StringComparison.Ordinal))
                {
                    holdsHandle = true;
                    break;
                }
            }

            if (holdsHandle == false)
            {
                return;
            }

            foreach (var member in typeSymbol.GetMembers("RemapObservedStatsInternal"))
            {
                // A partial declaration with no implementation part compiles away to nothing,
                // which is exactly the silent failure this rule exists to catch.
                if (member is IMethodSymbol { IsPartialDefinition: true, PartialImplementationPart: null })
                {
                    continue;
                }

                return;
            }

            var location = typeSymbol.Locations.Length > 0
                ? typeSymbol.Locations[0]
                : Location.None;

            context.ReportDiagnostic(Diagnostic.Create(
                  ModifierMissingRemap
                , location
                , typeSymbol.ContainingType.Name + "." + typeSymbol.Name
            ));
        }
    }
}


