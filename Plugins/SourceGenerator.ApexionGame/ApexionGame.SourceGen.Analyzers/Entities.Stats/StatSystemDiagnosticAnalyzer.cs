using System.Collections.Immutable;
using ApexionGame.SourceGen.Helpers.Entities.Stats;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApexionGame.SourceGen.Analyzers.Entities.Stats
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class StatSystemDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private const string NAMESPACE = StatTypeTable.NAMESPACE;
        private const string STAT_SYSTEM_ATTRIBUTE = $"global::{NAMESPACE}.StatSystemAttribute";

        public static readonly DiagnosticDescriptor MustNotBeGeneric = new(
              id: "AGS_STAT_SYSTEM_0001"
            , title: "[StatSystem] cannot be applied to a generic type"
            , messageFormat: "\"{0}\" is a generic type. [StatSystem] can only be applied to non-generic types."
            , category: "StatSystemGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "[StatSystem] can only be applied to non-generic types."
        );

        // New in this port: the generated code emits `ET.*` types (ByteBool, Option, HashValue)
        // because DEC-002 = B keeps EncosyTower.Core as a hard runtime dependency. Without that
        // reference the generated file fails to compile with a wall of unrelated errors, so say it
        // once, clearly, on the declaration itself.
        public static readonly DiagnosticDescriptor MissingEncosyTowerReference = new(
              id: "AGS_STAT_SYSTEM_0002"
            , title: "Assembly using [StatSystem] must reference EncosyTower.Core"
            , messageFormat: "\"{0}\" is declared in an assembly that does not reference EncosyTower.Core. The generated stat system uses types from it."
            , category: "StatSystemGenerator"
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: "Add a reference to EncosyTower.Core in the assembly definition that declares [StatSystem]."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(MustNotBeGeneric, MissingEncosyTowerReference);

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
                || typeSymbol.HasAttribute(STAT_SYSTEM_ATTRIBUTE, token) == false
            )
            {
                return;
            }

            var location = typeSymbol.Locations.Length > 0
                ? typeSymbol.Locations[0]
                : Location.None;

            if (typeSymbol.IsGenericType)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                      MustNotBeGeneric
                    , location
                    , typeSymbol.Name
                ));

                return;
            }

            // ByteBool stands in for the whole of EncosyTower.Core: the generated ValuePair and Stat
            // structs use it for their flag fields, so if it cannot be resolved nothing will compile.
            if (context.Compilation.GetTypeByMetadataName("EncosyTower.Common.ByteBool") is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                      MissingEncosyTowerReference
                    , location
                    , typeSymbol.Name
                ));
            }
        }
    }
}

