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
    /// Offers the half of a stat system that the generator deliberately leaves to you.
    /// </summary>
    /// <remarks>
    /// <c>[StatSystem]</c> emits <c>StatModifier</c> and <c>StatModifier.Stack</c> with all the
    /// interface plumbing wired and one <c>partial void ...Internal</c> hook per decision. Which
    /// hooks exist, and which are <c>readonly</c>, is not guessable — you have to read the
    /// generated output to find out. This refactoring writes them for you.
    /// </remarks>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StatSystemCodeRefactoringProvider))]
    [Shared]
    internal sealed class StatSystemCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string STAT_SYSTEM_ATTRIBUTE = "StatSystem";
        private const string TITLE_ATTRIBUTE = "Make this a stat system";
        private const string TITLE_SKELETON = "Generate stat modifier skeleton";

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            if (root?.FindNode(context.Span) is not { } node
                || node.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not { } candidate
            )
            {
                return;
            }

            // The generator only emits into a partial type, so anything else is not a stat system
            // in the making.
            if (candidate.Modifiers.Any(SyntaxKind.PartialKeyword) == false)
            {
                return;
            }

            var hasAttribute = HasAttribute(candidate, STAT_SYSTEM_ATTRIBUTE);
            var hasModifier = candidate.Members
                .OfType<StructDeclarationSyntax>()
                .Any(static member => member.Identifier.ValueText == "StatModifier");

            if (hasAttribute == false)
            {
                context.RegisterRefactoring(CodeAction.Create(
                      TITLE_ATTRIBUTE
                    , token => AddAttributeAsync(context.Document, candidate, token)
                    , equivalenceKey: TITLE_ATTRIBUTE
                ));
            }

            if (hasModifier == false)
            {
                context.RegisterRefactoring(CodeAction.Create(
                      TITLE_SKELETON
                    , token => AddSkeletonAsync(context.Document, candidate, token)
                    , equivalenceKey: TITLE_SKELETON
                ));
            }
        }

        internal static bool HasAttribute(MemberDeclarationSyntax member, string name)
        {
            foreach (var list in member.AttributeLists)
            {
                foreach (var attribute in list.Attributes)
                {
                    var text = attribute.Name.ToString();

                    // Written as [StatSystem], [StatSystemAttribute], or fully qualified.
                    if (text == name
                        || text == $"{name}Attribute"
                        || text.EndsWith($".{name}")
                        || text.EndsWith($".{name}Attribute")
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static async Task<Document> AddAttributeAsync(
              Document document
            , ClassDeclarationSyntax declaration
            , CancellationToken token
        )
        {
            var attribute = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.ParseName(STAT_SYSTEM_ATTRIBUTE))
                        .WithArgumentList(SyntaxFactory.ParseAttributeArgumentList("(StatDataSize.Size8)"))
                )
            );

            var updated = declaration.AddAttributeLists(attribute);
            return await ReplaceAsync(document, declaration, updated, token).ConfigureAwait(false);
        }

        private static async Task<Document> AddSkeletonAsync(
              Document document
            , ClassDeclarationSyntax declaration
            , CancellationToken token
        )
        {
            var member = SyntaxFactory.ParseMemberDeclaration(Skeleton);

            if (member is null)
            {
                return document;
            }

            var updated = declaration.AddMembers(member.WithAdditionalAnnotations(Formatter.Annotation));
            return await ReplaceAsync(document, declaration, updated, token).ConfigureAwait(false);
        }

        private static async Task<Document> ReplaceAsync(
              Document document
            , SyntaxNode oldNode
            , SyntaxNode newNode
            , CancellationToken token
        )
        {
            var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);

            return root is null
                ? document
                : document.WithSyntaxRoot(root.ReplaceNode(oldNode, newNode));
        }

        /// <remarks>
        /// Types are fully qualified so the refactoring never has to edit the using directives.
        /// <c>readonly</c> placement matters: it has to match the generated partial declaration
        /// exactly or the compiler will not pair them.
        /// </remarks>
        private const string Skeleton = @"
public partial struct StatModifier
{
    private uint _id;

    readonly partial void GetIdInternal(ref uint id)
        => id = _id;

    partial void SetIdInternal(uint value)
        => _id = value;

    /// <summary>
    /// Declare every stat this modifier reads.
    /// </summary>
    /// <remarks>
    /// Miss one and the affected stat is never recalculated when that input changes. No exception,
    /// no warning — the value just goes stale.
    /// </remarks>
    readonly partial void AddObservedStatsToListInternal(
        Unity.Collections.NativeList<ApexionGame.Entities.Stats.StatHandle> observedStatHandles)
    {
        // observedStatHandles.Add(observedStat);
    }

    partial void ApplyInternal(
          Reader reader
        , ref Stack stack
        , ref bool shouldProduceModifierTriggerEvent
    )
    {
        // Fold this modifier into the stack. Read other stats through `reader`.
    }

    /// <summary>
    /// Rewrites stored handles after a save is loaded, because owners come back in new slots.
    /// </summary>
    partial void RemapObservedStatsInternal(in ApexionGame.Entities.Stats.StatOwnerRemap remap)
    {
        // observedStat = remap.RemapOrNull(observedStat);
    }

    public partial struct Stack
    {
        /// <remarks>
        /// The identity element depends on the stat's value type, so read it from the stat rather
        /// than hard-coding 0 and 1.
        /// </remarks>
        partial void ResetInternal(in Stat stat)
        {
            // var type = stat.ValuePair.Type;
        }

        partial void ApplyInternal(
              in ApexionGame.Entities.Stats.StatVariant baseValue
            , ref ApexionGame.Entities.Stats.StatVariant currentValue
        )
        {
            currentValue = baseValue;
        }
    }
}
";
    }
}
