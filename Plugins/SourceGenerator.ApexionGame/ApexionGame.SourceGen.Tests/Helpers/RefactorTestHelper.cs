using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApexionGame.SourceGen.Tests;

/// <summary>
/// Runs a <see cref="CodeRefactoringProvider"/> over a snippet.
/// </summary>
/// <remarks>
/// Refactorings need a <see cref="Document"/>, not the bare <c>CSharpCompilation</c> the generator
/// tests use, so this builds a throwaway <see cref="AdhocWorkspace"/>. No Unity assemblies are
/// referenced: a refactoring only reads and rewrites syntax, so the snippet never has to resolve.
/// </remarks>
public static class RefactorTestHelper
{
    /// <summary>
    /// Collects every refactoring offered at the first occurrence of <paramref name="marker"/>.
    /// </summary>
    public static ImmutableArray<CodeAction> Offer(
          string source
        , string marker
        , CodeRefactoringProvider provider
    )
    {
        var document = CreateDocument(source);
        var span = SpanOf(source, marker);
        var actions = ImmutableArray.CreateBuilder<CodeAction>();

        var context = new CodeRefactoringContext(
              document
            , span
            , action => actions.Add(action)
            , default
        );

        provider.ComputeRefactoringsAsync(context).GetAwaiter().GetResult();
        return actions.ToImmutable();
    }

    /// <summary>
    /// Applies the refactoring whose title matches and returns the resulting source text.
    /// </summary>
    public static string Apply(
          string source
        , string marker
        , CodeRefactoringProvider provider
        , string title
    )
    {
        var actions = Offer(source, marker, provider);

        foreach (var action in actions)
        {
            if (action.Title != title)
            {
                continue;
            }

            var operations = action.GetOperationsAsync(default).GetAwaiter().GetResult();

            foreach (var operation in operations)
            {
                if (operation is not ApplyChangesOperation apply)
                {
                    continue;
                }

                var changed = apply.ChangedSolution.GetDocument(DocumentIdOf(apply.ChangedSolution));
                return changed!.GetTextAsync().GetAwaiter().GetResult().ToString();
            }
        }

        return source;
    }

    private static DocumentId DocumentIdOf(Solution solution)
        => solution.Projects.First().DocumentIds[0];

    private static Document CreateDocument(string source)
    {
        var workspace = new AdhocWorkspace();

        var project = workspace.AddProject(ProjectInfo.Create(
              ProjectId.CreateNewId()
            , VersionStamp.Default
            , "RefactorTestProject"
            , "RefactorTestProject"
            , LanguageNames.CSharp
        ));

        return workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));
    }

    private static TextSpan SpanOf(string source, string marker)
    {
        var index = source.IndexOf(marker, System.StringComparison.Ordinal);

        Assert.IsTrue(index >= 0, $"marker '{marker}' not found in the snippet");

        return new TextSpan(index, marker.Length);
    }
}
