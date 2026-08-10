using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ApexionGame.SourceGen.CodeRefactors.Entities.Stats
{
    /// <summary>
    /// Turns a plain <c>partial struct</c> into a stat collection skeleton.
    /// </summary>
    /// <remarks>
    /// The type-id seed (second argument) has to be unique per collection — two collections sharing
    /// one seed collide in <c>UserData</c>. The refactoring picks a seed that is not already used in
    /// the compilation rather than always writing the same number.
    /// </remarks>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StatCollectionCodeRefactoringProvider))]
    [Shared]
    internal sealed class StatCollectionCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string STAT_COLLECTION_ATTRIBUTE = "StatCollection";
        private const string STAT_SYSTEM_ATTRIBUTE = "StatSystem";
        private const string TITLE = "Generate stat collection skeleton";
        private const int SEED_START = 1000;
        private const int SEED_STEP = 100;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            if (root?.FindNode(context.Span) is not { } node
                || node.FirstAncestorOrSelf<StructDeclarationSyntax>() is not { } candidate
                || candidate.Modifiers.Any(SyntaxKind.PartialKeyword) == false
                || StatSystemCodeRefactoringProvider.HasAttribute(candidate, STAT_COLLECTION_ATTRIBUTE)
            )
            {
                return;
            }

            // Nested structs are how [StatData] members are declared; offering this on one of them
            // would produce a collection inside a collection.
            if (candidate.Parent is StructDeclarationSyntax)
            {
                return;
            }

            context.RegisterRefactoring(CodeAction.Create(
                  TITLE
                , token => ApplyAsync(context.Document, candidate, token)
                , equivalenceKey: TITLE
            ));
        }

        private static async Task<Document> ApplyAsync(
              Document document
            , StructDeclarationSyntax declaration
            , CancellationToken token
        )
        {
            var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);

            if (root is null)
            {
                return document;
            }

            var systemName = FindStatSystemName(root);
            var seed = NextFreeSeed(root);

            var attribute = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.ParseName(STAT_COLLECTION_ATTRIBUTE))
                        .WithArgumentList(
                            SyntaxFactory.ParseAttributeArgumentList($"(typeof({systemName}), {seed})"))
                )
            );

            var updated = declaration.AddAttributeLists(attribute);

            if (declaration.Members.Count == 0)
            {
                var member = SyntaxFactory.ParseMemberDeclaration(
                    "[StatData(StatVariantType.Float)] public partial struct Hp { }");

                if (member is not null)
                {
                    updated = updated.AddMembers(member.WithAdditionalAnnotations(Formatter.Annotation));
                }
            }

            return document.WithSyntaxRoot(root.ReplaceNode(declaration, updated));
        }

        /// <summary>
        /// Uses a <c>[StatSystem]</c> type from the same file when there is one, so the generated
        /// attribute compiles instead of referencing a placeholder.
        /// </summary>
        private static string FindStatSystemName(SyntaxNode root)
        {
            foreach (var type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (StatSystemCodeRefactoringProvider.HasAttribute(type, STAT_SYSTEM_ATTRIBUTE))
                {
                    return type.Identifier.ValueText;
                }
            }

            return "StatSystem";
        }

        private static int NextFreeSeed(SyntaxNode root)
        {
            var used = root.DescendantNodes()
                .OfType<StructDeclarationSyntax>()
                .SelectMany(static type => type.AttributeLists)
                .SelectMany(static list => list.Attributes)
                .Where(static attribute => attribute.Name.ToString().Contains(STAT_COLLECTION_ATTRIBUTE))
                .Select(static attribute => attribute.ArgumentList?.Arguments.Count > 1
                    ? attribute.ArgumentList.Arguments[1].ToString()
                    : null)
                .Where(static text => int.TryParse(text, out _))
                .Select(static text => int.Parse(text))
                .ToArray();

            var seed = SEED_START;

            while (used.Contains(seed))
            {
                seed += SEED_STEP;
            }

            return seed;
        }
    }
}
