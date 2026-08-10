using System.Collections.Immutable;
using ApexionGame.SourceGen.Helpers.Entities.Stats;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApexionGame.SourceGen.Analyzers.Entities.Stats
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class StatCollectionDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private const string NAMESPACE = StatTypeTable.NAMESPACE;
        private const string STAT_COLLECTION_ATTRIBUTE = $"global::{NAMESPACE}.StatCollectionAttribute";
        private const string STAT_SYSTEM_ATTRIBUTE = $"global::{NAMESPACE}.StatSystemAttribute";
        private const string STAT_DATA_ATTRIBUTE = $"global::{NAMESPACE}.StatDataAttribute";

        public static readonly DiagnosticDescriptor StatSystemAttributeRequired = new(
              id: "AGS_STAT_COLLECTION_0001"
            , title: "Type argument of [StatCollection] must have [StatSystem]"
            , messageFormat: "\"{0}\" does not have the [StatSystem] attribute. The typeof argument of [StatCollection] must resolve to a type attributed with [StatSystem]."
            , category: "StatCollectionGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The type passed to [StatCollection(typeof(...))] must be attributed with [StatSystem]."
        );

        public static readonly DiagnosticDescriptor TypeIdOffsetOverflow = new(
              id: "AGS_STAT_COLLECTION_0002"
            , title: "typeIdOffset + StatData count exceeds uint.MaxValue"
            , messageFormat: "The combination of typeIdOffset ({0}) and the number of [StatData] members ({1}) in \"{2}\" exceeds uint.MaxValue. Reduce typeIdOffset or the number of [StatData] members."
            , category: "StatCollectionGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "The combination of typeIdOffset and the number of [StatData] nested structs must not exceed uint.MaxValue."
        );

        public static readonly DiagnosticDescriptor MustBeStruct = new(
              id: "AGS_STAT_COLLECTION_0003"
            , title: "[StatCollection] can only be applied to a struct"
            , messageFormat: "\"{0}\" is not a struct. [StatCollection] can only be applied to struct types."
            , category: "StatCollectionGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "[StatCollection] can only be applied to struct types."
        );

        public static readonly DiagnosticDescriptor MustNotBeNonGeneric = new(
              id: "AGS_STAT_COLLECTION_0004"
            , title: "[StatCollection] cannot be applied to an non-generic struct"
            , messageFormat: "\"{0}\" is a generic type. [StatCollection] can only be applied to non-generic structs."
            , category: "StatCollectionGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "[StatCollection] can only be applied to non-generic struct types."
        );

        // New in this port. A nested struct without [StatData] is silently ignored by the generator,
        // which reads like "my stat disappeared" rather than "I forgot an attribute".
        public static readonly DiagnosticDescriptor NestedStructMissingStatData = new(
              id: "AGS_STAT_COLLECTION_0005"
            , title: "Nested struct in a [StatCollection] has no [StatData]"
            , messageFormat: "\"{0}\" is nested in a [StatCollection] but has no [StatData] attribute, so no stat is generated for it."
            , category: "StatCollectionGenerator"
            , defaultSeverity: DiagnosticSeverity.Warning
            , isEnabledByDefault: true
            , description: "Annotate the nested struct with [StatData], or move it out of the collection if it is not a stat."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                  MustBeStruct
                , MustNotBeNonGeneric
                , StatSystemAttributeRequired
                , TypeIdOffsetOverflow
                , NestedStructMissingStatData
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

            if (context.Symbol is not INamedTypeSymbol typeSymbol
                || typeSymbol.HasAttribute(STAT_COLLECTION_ATTRIBUTE, token) == false
            )
            {
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
                      MustNotBeNonGeneric
                    , location
                    , typeSymbol.Name
                ));

                return;
            }

            var attrib = typeSymbol.GetAttribute(STAT_COLLECTION_ATTRIBUTE, token);

            if (attrib == null || attrib.ConstructorArguments.Length < 1)
            {
                return;
            }

            var typeArg = attrib.ConstructorArguments[0];

            if (typeArg.Kind != TypedConstantKind.Type)
            {
                return;
            }


            if (typeArg.Value is not INamedTypeSymbol statSystemTypeSymbol
                || statSystemTypeSymbol.HasAttribute(STAT_SYSTEM_ATTRIBUTE, token) == false
            )
            {
                var displayName = (typeArg.Value as ISymbol)?.ToDisplayString() ?? typeArg.Value?.ToString() ?? "?";
                var location = attrib.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)?.GetLocation()
                    ?? typeSymbol.Locations[0];

                context.ReportDiagnostic(Diagnostic.Create(
                      StatSystemAttributeRequired
                    , location
                    , displayName
                ));

                return;
            }

            token.ThrowIfCancellationRequested();

            var statDataCount = 0;

            foreach (var member in typeSymbol.GetTypeMembers())
            {
                token.ThrowIfCancellationRequested();

                if (member.TypeKind != TypeKind.Struct || member.IsUnboundGenericType)
                {
                    continue;
                }

                if (member.HasAttribute(STAT_DATA_ATTRIBUTE, token))
                {
                    statDataCount++;
                    continue;
                }

                // The generator emits several nested helper structs of its own (TypeId, Indices,
                // StatHandles, Options, ...). Those are partials of declarations the user writes,
                // so only warn about structs that carry no generated counterpart marker: anything
                // the user declared and left un-annotated.
                if (IsGeneratorOwnedNestedType(member.Name))
                {
                    continue;
                }

                var memberLocation = member.Locations.Length > 0
                    ? member.Locations[0]
                    : Location.None;

                context.ReportDiagnostic(Diagnostic.Create(
                      NestedStructMissingStatData
                    , memberLocation
                    , member.Name
                ));
            }

            var typeIdOffset = 0uL;

            if (attrib.ConstructorArguments.Length > 1
                && attrib.ConstructorArguments[1].Value is uint offset)
            {
                typeIdOffset = offset;
            }

            if (typeIdOffset + (ulong)statDataCount > uint.MaxValue)
            {
                var location = typeSymbol.Locations.Length > 0
                    ? typeSymbol.Locations[0]
                    : Location.None;

                context.ReportDiagnostic(Diagnostic.Create(
                      TypeIdOffsetOverflow
                    , location
                    , typeIdOffset
                    , statDataCount
                    , typeSymbol.Name
                ));
            }
        }

        /// <summary>
        /// Nested type names the StatCollection generator declares partials for.
        /// </summary>
        private static bool IsGeneratorOwnedNestedType(string name)
            => name switch {
                "TypeId" => true,
                "Index" => true,
                "Indices" => true,
                "StatIndices" => true,
                "StatHandles" => true,
                "Options" => true,
                "Builder" => true,
                "Accessor" => true,
                "Reader" => true,
                "IndexRecord" => true,
                "StatIndexRecord" => true,
                "StatHandleRecord" => true,
                _ => false,
            };
    }
}

