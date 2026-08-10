using System.Linq;
using ApexionGame.SourceGen.CodeRefactors.Entities.Stats;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApexionGame.SourceGen.Tests.Entities.Stats;

/// <summary>
/// Task 5.3 — the refactorings that write the hand-authored half of a stat system.
/// </summary>
[TestClass]
public class StatRefactorTests
{
    private const string SYSTEM_TITLE_ATTRIBUTE = "Make this a stat system";
    private const string SYSTEM_TITLE_SKELETON = "Generate stat modifier skeleton";
    private const string COLLECTION_TITLE = "Generate stat collection skeleton";

    [TestMethod]
    public void StatSystem_OnBarePartialClass_OffersBothActions()
    {
        const string SOURCE = @"
public static partial class MyStats { }
";
        var actions = RefactorTestHelper.Offer(SOURCE, "MyStats", new StatSystemCodeRefactoringProvider());
        var titles = actions.Select(static a => a.Title).ToArray();

        CollectionAssert.Contains(titles, SYSTEM_TITLE_ATTRIBUTE);
        CollectionAssert.Contains(titles, SYSTEM_TITLE_SKELETON);
    }

    /// <remarks>
    /// The generator only emits into partial types, so a sealed class is not a stat system in the
    /// making and offering the action there would just produce code that never compiles.
    /// </remarks>
    [TestMethod]
    public void StatSystem_OnNonPartialClass_OffersNothing()
    {
        const string SOURCE = @"
public static class MyStats { }
";
        var actions = RefactorTestHelper.Offer(SOURCE, "MyStats", new StatSystemCodeRefactoringProvider());

        Assert.AreEqual(0, actions.Length);
    }

    [TestMethod]
    public void StatSystem_AlreadyAttributed_DoesNotOfferTheAttributeAgain()
    {
        const string SOURCE = @"
[StatSystem(StatDataSize.Size8)]
public static partial class MyStats { }
";
        var actions = RefactorTestHelper.Offer(SOURCE, "MyStats", new StatSystemCodeRefactoringProvider());
        var titles = actions.Select(static a => a.Title).ToArray();

        CollectionAssert.DoesNotContain(titles, SYSTEM_TITLE_ATTRIBUTE);
        CollectionAssert.Contains(titles, SYSTEM_TITLE_SKELETON);
    }

    /// <remarks>
    /// Every hook the generated partial declares must appear, with the same <c>readonly</c>
    /// placement — a mismatch there silently leaves the partial unimplemented.
    /// </remarks>
    [TestMethod]
    public void StatSystem_Skeleton_WritesEveryPartialHook()
    {
        const string SOURCE = @"
public static partial class MyStats { }
";
        var result = RefactorTestHelper.Apply(
            SOURCE, "MyStats", new StatSystemCodeRefactoringProvider(), SYSTEM_TITLE_SKELETON);

        StringAssert.Contains(result, "readonly partial void GetIdInternal(ref uint id)");
        StringAssert.Contains(result, "partial void SetIdInternal(uint value)");
        StringAssert.Contains(result, "readonly partial void AddObservedStatsToListInternal(");
        StringAssert.Contains(result, "partial void ApplyInternal(");
        StringAssert.Contains(result, "partial void RemapObservedStatsInternal(");
        StringAssert.Contains(result, "partial void ResetInternal(in Stat stat)");
        StringAssert.Contains(result, "public partial struct Stack");
    }

    [TestMethod]
    public void StatSystem_Attribute_IsAdded()
    {
        const string SOURCE = @"
public static partial class MyStats { }
";
        var result = RefactorTestHelper.Apply(
            SOURCE, "MyStats", new StatSystemCodeRefactoringProvider(), SYSTEM_TITLE_ATTRIBUTE);

        StringAssert.Contains(result, "[StatSystem(StatDataSize.Size8)]");
    }

    [TestMethod]
    public void StatCollection_OnPartialStruct_UsesTheStatSystemInTheSameFile()
    {
        const string SOURCE = @"
[StatSystem(StatDataSize.Size8)]
public static partial class MyStats { }

public partial struct HeroStats { }
";
        var result = RefactorTestHelper.Apply(
            SOURCE, "HeroStats", new StatCollectionCodeRefactoringProvider(), COLLECTION_TITLE);

        StringAssert.Contains(result, "[StatCollection(typeof(MyStats), 1000)]");
        StringAssert.Contains(result, "[StatData(StatVariantType.Float)]");
    }

    /// <remarks>
    /// Two collections sharing one type-id seed collide in UserData, so the seed has to dodge what
    /// is already taken rather than always being 1000.
    /// </remarks>
    [TestMethod]
    public void StatCollection_SeedAvoidsOneAlreadyUsed()
    {
        const string SOURCE = @"
[StatSystem(StatDataSize.Size8)]
public static partial class MyStats { }

[StatCollection(typeof(MyStats), 1000)]
public partial struct FirstStats { }

public partial struct SecondStats { }
";
        var result = RefactorTestHelper.Apply(
            SOURCE, "SecondStats", new StatCollectionCodeRefactoringProvider(), COLLECTION_TITLE);

        StringAssert.Contains(result, "[StatCollection(typeof(MyStats), 1100)]");
    }

    /// <remarks>
    /// A nested struct is how a <c>[StatData]</c> member is declared; turning one into a collection
    /// would nest a collection inside a collection.
    /// </remarks>
    [TestMethod]
    public void StatCollection_OnNestedStruct_OffersNothing()
    {
        const string SOURCE = @"
[StatCollection(typeof(MyStats), 1000)]
public partial struct HeroStats
{
    public partial struct Hp { }
}
";
        var actions = RefactorTestHelper.Offer(
            SOURCE, "Hp", new StatCollectionCodeRefactoringProvider());

        Assert.AreEqual(0, actions.Length);
    }

    [TestMethod]
    public void StatCollection_AlreadyAttributed_OffersNothing()
    {
        const string SOURCE = @"
[StatCollection(typeof(MyStats), 1000)]
public partial struct HeroStats { }
";
        var actions = RefactorTestHelper.Offer(
            SOURCE, "HeroStats", new StatCollectionCodeRefactoringProvider());

        Assert.AreEqual(0, actions.Length);
    }
}
