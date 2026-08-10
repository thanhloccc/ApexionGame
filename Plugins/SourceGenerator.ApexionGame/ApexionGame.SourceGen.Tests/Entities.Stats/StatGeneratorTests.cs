using ApexionGame.SourceGen.Generators.Entities.Stats;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApexionGame.SourceGen.Tests.Entities.Stats;

/// <summary>
/// Integration tests: the generated code must compile against the real runtime assembly.
/// </summary>
[TestClass]
public class StatGeneratorIntegrationTests
{
    private const string FULL_SAMPLE = """
        using ApexionGame.Entities.Stats;

        namespace TestProject
        {
            [StatSystem(StatDataSize.Size8)]
            public static partial class StatSystem { }

            public enum DirectionType : byte { Forward, Backward }

            [StatCollection(typeof(StatSystem), 1000)]
            public partial struct Stats
            {
                [StatData(StatVariantType.Float)] public partial struct Hp { }
                [StatData(StatVariantType.Float)] public partial struct MoveSpeed { }
                [StatData(typeof(DirectionType))] public partial struct Direction { }
            }
        }
        """;

    [TestMethod]
    public void GeneratedCode_Compiles()
    {
        SkipIfUnityMissing();

        var run = GeneratorTestHelper.Run(
              FULL_SAMPLE
            , new StatSystemGenerator()
            , new StatCollectionGenerator()
            , new StatDataGenerator()
        );

        Assert.AreEqual(0, run.CompilationErrors.Length, $"generated code did not compile:\n{run.DescribeErrors()}");
    }

    [TestMethod]
    public void GeneratedCode_EmitsOneFilePerDeclaration()
    {
        SkipIfUnityMissing();

        var run = GeneratorTestHelper.Run(
              FULL_SAMPLE
            , new StatSystemGenerator()
            , new StatCollectionGenerator()
            , new StatDataGenerator()
        );

        // StatSystem + Stats + three StatData structs.
        Assert.AreEqual(5, run.GeneratedTrees.Length);
    }

    [TestMethod]
    public void GeneratedCode_ContainsNoEcsTypes()
    {
        SkipIfUnityMissing();

        var run = GeneratorTestHelper.Run(
              FULL_SAMPLE
            , new StatSystemGenerator()
            , new StatCollectionGenerator()
            , new StatDataGenerator()
        );

        var text = run.AllGeneratedText;

        foreach (var token in new[] {
            "UECS", "IComponentData", "IBufferElementData", "IBaker",
            "DynamicBuffer", "EntityManager", "EntityCommandBuffer", "SystemState",
        })
        {
            StringAssert.DoesNotMatch(text, new(System.Text.RegularExpressions.Regex.Escape(token)), token);
        }
    }

    /// <remarks>
    /// Guards DEC-004: without the hook a modifier silently keeps stale owner handles after a load.
    /// </remarks>
    [TestMethod]
    public void GeneratedStatModifier_DeclaresRemapHook()
    {
        SkipIfUnityMissing();

        var run = GeneratorTestHelper.Run(FULL_SAMPLE, new StatSystemGenerator());

        StringAssert.Contains(run.AllGeneratedText, "public void RemapObservedStats(");
        StringAssert.Contains(run.AllGeneratedText, "partial void RemapObservedStatsInternal(");
    }

    /// <remarks>
    /// Guards D-102: index 0 must stay the None stat, and CreateStatOwner is the only entry point
    /// that seeds it.
    /// </remarks>
    [TestMethod]
    public void GeneratedApi_ExposesCreateStatOwner()
    {
        SkipIfUnityMissing();

        var run = GeneratorTestHelper.Run(FULL_SAMPLE, new StatSystemGenerator());

        StringAssert.Contains(run.AllGeneratedText, "public static Builder CreateStatOwner(ref ");
    }

    [TestMethod]
    public void GeneratedCollection_ExposesBuildAndToStats()
    {
        SkipIfUnityMissing();

        var run = GeneratorTestHelper.Run(FULL_SAMPLE, new StatCollectionGenerator());

        var text = run.AllGeneratedText;

        StringAssert.Contains(text, "Build(ref ");
        StringAssert.Contains(text, "ToStats()");
    }

    private static void SkipIfUnityMissing()
    {
        if (UnityDllPaths.IsResolved == false)
        {
            Assert.Inconclusive(UnityDllPaths.SkipReason);
        }
    }
}

/// <summary>
/// Stub-tier smoke tests: no Unity assemblies required, so these always run.
/// </summary>
[TestClass]
public class StatGeneratorSmokeTests
{
    [TestMethod]
    public void StatSystemGenerator_EmptyInput_DoesNotThrow()
        => GeneratorTestHelper.Run("namespace Empty { }", new StatSystemGenerator());

    [TestMethod]
    public void StatCollectionGenerator_EmptyInput_DoesNotThrow()
        => GeneratorTestHelper.Run("namespace Empty { }", new StatCollectionGenerator());

    [TestMethod]
    public void StatDataGenerator_EmptyInput_DoesNotThrow()
        => GeneratorTestHelper.Run("namespace Empty { }", new StatDataGenerator());
}
