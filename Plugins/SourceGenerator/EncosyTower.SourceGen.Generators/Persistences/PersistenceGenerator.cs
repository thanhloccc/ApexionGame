using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EncosyTower.SourceGen.Generators.Persistences
{
    using static Helpers;

    [Generator]
    internal sealed class PersistenceGenerator : IIncrementalGenerator
    {
        public const string GENERATOR_NAME = nameof(PersistenceGenerator);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectPathProvider = SourceGenHelpers.GetSourceGenConfigProvider(context);

            var compilationProvider = context.CompilationProvider
                .Select(static (x, c) => CompilationInfo.GetCompilation(x, c, NAMESPACE, SKIP_ATTRIBUTE));

            var providerClassProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                  PERSISTENCE_ATTRIBUTE_METADATA
                , static (node, _) => node is ClassDeclarationSyntax syntax
                      && syntax.HasModifier(SyntaxKind.AbstractKeyword) == false
                      && syntax.TypeParameterList is null
                , GetVaultInfo
            ).Where(static t => t.IsValid);

            var accessProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                  ACCESSOR_ATTRIBUTE_METADATA
                , static (node, _) => node is ClassDeclarationSyntax syntax
                      && syntax.HasModifier(SyntaxKind.AbstractKeyword) == false
                      && syntax.TypeParameterList is null
                , GetAccessorInfo
            ).Where(static t => t.isValid);

            var combined = providerClassProvider
                .Combine(accessProvider.Collect())
                .Combine(compilationProvider)
                .Combine(projectPathProvider)
                .Where(static t => t.Left.Right.isValid);

            context.RegisterSourceOutput(combined, static (sourceProductionContext, source) => {
                GenerateOutput(
                      sourceProductionContext
                    , source.Left.Right
                    , source.Left.Left.Left
                    , source.Left.Left.Right
                    , source.Right.projectPath
                    , source.Right.outputSourceGenFiles
                );
            });
        }

        private static PersistenceSpec GetVaultInfo(
              GeneratorAttributeSyntaxContext context
            , CancellationToken token
        )
        {
            token.ThrowIfCancellationRequested();

            if (context.TargetSymbol is not INamedTypeSymbol symbol
                || symbol.IsAbstract
                || symbol.IsUnboundGenericType
            )
            {
                return default;
            }

            var syntaxTree = context.TargetNode.SyntaxTree;
            var containingNs = symbol.ContainingNamespace;

            return new PersistenceSpec {
                location = LocationInfo.From(context.TargetNode.GetLocation()),
                metadataName = symbol.ToFullNameNoGlobal(),
                className = symbol.Name,
                isStatic = symbol.IsStatic,
                namespaceName = containingNs is { IsGlobalNamespace: false }
                    ? containingNs.ToDisplayString()
                    : string.Empty,
                containingTypeDeclarations = symbol.GetContainingTypes(),
                hintName = syntaxTree.GetHintName(context.TargetNode, symbol.ToFileName()),
            };
        }

        private static PersistAccessorSpec GetAccessorInfo(
              GeneratorAttributeSyntaxContext context
            , CancellationToken token
        )
        {
            token.ThrowIfCancellationRequested();

            if (context.TargetSymbol is not INamedTypeSymbol symbol
                || symbol.IsAbstract
                || symbol.IsUnboundGenericType
            )
            {
                return default;
            }

            var vaultMetadataName = string.Empty;
            var attribute = context.Attributes[0];

            if (attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is INamedTypeSymbol persistenceType
            )
            {
                vaultMetadataName = persistenceType.ToFullNameNoGlobal();
            }

            var fieldName = string.Empty;

            foreach (var attrib in symbol.GetAttributes())
            {
                token.ThrowIfCancellationRequested();

                var attribName = attrib.AttributeClass?.Name ?? string.Empty;

                if (attribName is not ("LabelAttribute" or "DisplayNameAttribute"))
                {
                    continue;
                }

                if (attrib.ConstructorArguments.Length > 0)
                {
                    var arg = attrib.ConstructorArguments[0];

                    if (arg.Kind == TypedConstantKind.Primitive && arg.Value?.ToString() is string dn)
                    {
                        fieldName = dn;
                        goto NEXT;
                    }
                }
                else if (attrib.NamedArguments.Length > 0)
                {
                    foreach (var arg in attrib.NamedArguments)
                    {
                        if (arg.Key is "Name" or "DisplayName"
                            && arg.Value.Kind == TypedConstantKind.Primitive
                            && arg.Value.Value?.ToString() is string dn
                        )
                        {
                            fieldName = dn;
                            goto NEXT;
                        }
                    }
                }
            }

            NEXT:

            if (string.IsNullOrEmpty(fieldName))
            {
                fieldName = symbol.Name;
            }

            var constructors = symbol.Constructors;
            var constructorIndex = -1;
            var max = 0;

            for (var i = 0; i < constructors.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                if (constructors[i].Parameters.Length > max)
                {
                    max = constructors[i].Parameters.Length;
                    constructorIndex = i;
                }
            }

            if (constructorIndex != 0)
            {
                return default;
            }

            var constructor = constructors[constructorIndex];
            var parameters = constructor.Parameters;
            using var builder = ImmutableArrayBuilder<AccessorArgSpec>.Rent();
            var isValid = true;

            foreach (var param in parameters)
            {
                token.ThrowIfCancellationRequested();

                if (ParamDeclaration.TryGetParam(param.Type, out var argType))
                {
                    var dataTypeHasDefaultConstructor = false;

                    if (argType != null)
                    {
                        var nonDefaultCount = 0;
                        var defaultCount = 0;

                        foreach (var member in argType.GetMembers())
                        {
                            token.ThrowIfCancellationRequested();

                            if (member is IMethodSymbol method
                                && method.MethodKind == MethodKind.Constructor
                            )
                            {
                                if (method.Parameters.Length > 0)
                                    nonDefaultCount++;
                                else
                                    defaultCount++;
                            }
                        }

                        dataTypeHasDefaultConstructor = defaultCount > 0 || nonDefaultCount < 1;
                    }

                    builder.Add(new AccessorArgSpec(
                          isStore: argType != null
                        , fullTypeName: param.Type.ToFullName()
                        , fullDataTypeName: argType?.ToFullName() ?? string.Empty
                        , typeName: argType?.Name ?? param.Type.Name
                        , dataTypeHasDefaultConstructor: dataTypeHasDefaultConstructor
                    ));
                }
                else
                {
                    isValid = false;
                }
            }

            return new PersistAccessorSpec {
                location = LocationInfo.From(context.TargetNode.GetLocation()),
                metadataName = symbol.ToFullNameNoGlobal(),
                vaultMetadataName = vaultMetadataName,
                fieldName = fieldName,
                symbolName = symbol.Name,
                args = builder.ToImmutable().AsEquatableArray(),
                isInitializable = symbol.InheritsFromInterface("global::EncosyTower.Initialization.IInitializable"),
                isDeinitializable = symbol.InheritsFromInterface("global::EncosyTower.Initialization.IDeinitializable"),
                isValid = isValid,
            };
        }

        private static void GenerateOutput(
              SourceProductionContext context
            , CompilationInfo compilation
            , PersistenceSpec vaultInfo
            , ImmutableArray<PersistAccessorSpec> accessorInfos
            , string projectPath
            , bool outputSourceGenFiles
        )
        {
            if (vaultInfo.IsValid == false || accessorInfos.Length < 1)
            {
                return;
            }

            var token = context.CancellationToken;
            token.ThrowIfCancellationRequested();

            try
            {
                var accessDeclarations = new List<PersistAccessorDeclaration>(accessorInfos.Length);

                for (var i = 0; i < accessorInfos.Length; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var aInfo = accessorInfos[i];

                    if (string.IsNullOrEmpty(aInfo.vaultMetadataName) == false
                        && string.Equals(aInfo.vaultMetadataName, vaultInfo.metadataName, StringComparison.Ordinal) == false
                    )
                    {
                        continue;
                    }

                    var accessDeclaration = new PersistAccessorDeclaration(aInfo);

                    if (accessDeclaration.IsValid)
                    {
                        accessDeclarations.Add(accessDeclaration);
                    }
                }

                if (accessDeclarations.Count < 1)
                {
                    return;
                }

                token.ThrowIfCancellationRequested();

                accessDeclarations.Sort(static (x, y) => {
                    return string.Compare(x.SymbolName, y.SymbolName, StringComparison.Ordinal);
                });

                var declaration = new PersistenceDeclaration(
                      vaultInfo.className
                    , vaultInfo.isStatic
                    , accessDeclarations
                );

                var openingPrinter = Printer.DefaultLarge;
                PrinterAction printUsings = compilation.references.unitask
                ? PrintUsingUniTask
                : PrintUsingAwaitable;

                printUsings(ref openingPrinter);

                var hasNamespace = string.IsNullOrEmpty(vaultInfo.namespaceName) == false;

                if (hasNamespace)
                {
                    openingPrinter.PrintLine($"namespace {vaultInfo.namespaceName}");
                    openingPrinter.OpenScope();
                }

                var containingTypes = vaultInfo.containingTypeDeclarations;

                for (var i = 0; i < containingTypes.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    openingPrinter.PrintLine(containingTypes[i]);
                    openingPrinter.OpenScope();
                }

                var openingSource = openingPrinter.Result;
                var closingPrinter = Printer.DefaultLarge;
                closingPrinter.PrintEndLine();

                var closingDepth = containingTypes.Count + (hasNamespace ? 1 : 0);

                for (var i = 0; i < closingDepth; i++)
                {
                    token.ThrowIfCancellationRequested();

                    closingPrinter = closingPrinter.DecreasedIndent();
                    closingPrinter.PrintLine("}");
                }

                var assemblyName = compilation.assemblyName;
                var hintName = vaultInfo.hintName;
                var sourceFilePath = SourceGenHelpers.BuildSourceFilePath(assemblyName, hintName, projectPath);

                context.OutputSource(
                      outputSourceGenFiles
                    , openingSource
                    , declaration.WriteCode(token)
                    , closingPrinter.Result
                    , vaultInfo.hintName
                    , sourceFilePath
                    , projectPath
                );
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                {
                    throw;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                      s_errorDescriptor
                    , vaultInfo.location.ToLocation()
                    , e.ToUnityPrintableString()
                ));
            }
        }

        private static readonly DiagnosticDescriptor s_errorDescriptor = new(
              id: "SG_PERSISTENCE_UNKNOWN_0001"
            , title: "Persistence Generator Error"
            , messageFormat: "This error indicates a bug in the Persistence source generator. Error message: '{0}'."
            , category: PERSIST_ATTRIBUTE
            , defaultSeverity: DiagnosticSeverity.Error
            , isEnabledByDefault: true
            , description: ""
        );

        private static void PrintUsingUniTask(ref Printer p)
        {
            p.PrintEndLine();
            p.Print("#pragma warning disable CS0105 // Using directive appeared previously in this namespace").PrintEndLine();
            p.PrintEndLine();
            p.PrintLine("using UnityTask = global::Cysharp.Threading.Tasks.UniTask;");
            p.PrintLine("using UnityTaskBool = global::Cysharp.Threading.Tasks.UniTask<bool>;");
            PrintAdditionalUsings(ref p);
            p.Print("#pragma warning restore CS0105 // Using directive appeared previously in this namespace").PrintEndLine();
            p.PrintEndLine();
        }

        private static void PrintUsingAwaitable(ref Printer p)
        {
            p.PrintEndLine();
            p.Print("#pragma warning disable CS0105 // Using directive appeared previously in this namespace").PrintEndLine();
            p.PrintEndLine();
            p.PrintLine("using UnityTask = global::UnityEngine.Awaitable;");
            p.PrintLine("using UnityTaskBool = global::UnityEngine.Awaitable<bool>;");
            PrintAdditionalUsings(ref p);
            p.Print("#pragma warning restore CS0105 // Using directive appeared previously in this namespace").PrintEndLine();
            p.PrintEndLine();
        }

        private static void PrintAdditionalUsings(ref Printer p)
        {
            p.PrintLine("using S = global::System;");
            p.PrintLine("using SC = global::System.Collections;");
            p.PrintLine("using SCG = global::System.Collections.Generic;");
            p.PrintLine("using ST = global::System.Threading;");
            p.PrintLine("using SD = global::System.Diagnostics;");
            p.PrintLine("using SDCA = global::System.Diagnostics.CodeAnalysis;");
            p.PrintLine("using SCDC = global::System.CodeDom.Compiler;");
            p.PrintLine("using SRCS = global::System.Runtime.CompilerServices;");
            p.PrintLine("using SB = global::System.Buffers;");
            p.PrintLine("using UE = global::UnityEngine;");
            p.PrintLine("using ET = global::EncosyTower.Common;");
            p.PrintLine("using ETP = global::EncosyTower.Persistences;");
            p.PrintLine("using ETS = global::EncosyTower.StringIds;");
            p.PrintLine("using ETE = global::EncosyTower.Encryption;");
            p.PrintLine("using ETC = global::EncosyTower.Collections;");
            p.PrintLine("using ETDBG = global::EncosyTower.Debugging;");
            p.PrintLine("using ETT = global::EncosyTower.Tasks;");
            p.PrintLine("using ETL = global::EncosyTower.Logging;");
            p.PrintLine("using ETI = global::EncosyTower.Initialization;");
            p.PrintEndLine();
        }
    }
}
