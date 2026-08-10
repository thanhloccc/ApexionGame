using System.Linq;
using ApexionGame.SourceGen.Analyzers.Entities.Stats;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApexionGame.SourceGen.Tests.Entities.Stats;

/// <summary>
/// Each rule gets a source that violates exactly it.
/// </summary>
[TestClass]
public class StatAnalyzerTests
{
    [TestMethod]
    public void StatData_OnClass_ReportsMustBeStruct()
        => AssertReports("AGS_STAT_DATA_0001", """
            using ApexionGame.Entities.Stats;
            namespace T { [StatData(StatVariantType.Float)] public partial class C { } }
            """);

    [TestMethod]
    public void StatData_OnGenericStruct_ReportsMustNotBeGeneric()
        => AssertReports("AGS_STAT_DATA_0002", """
            using ApexionGame.Entities.Stats;
            namespace T { [StatData(StatVariantType.Float)] public partial struct S<X> { } }
            """);

    [TestMethod]
    public void StatData_WithNone_ReportsInvalidVariantType()
        => AssertReports("AGS_STAT_DATA_0003", """
            using ApexionGame.Entities.Stats;
            namespace T { [StatData(StatVariantType.None)] public partial struct S { } }
            """);

    [TestMethod]
    public void StatData_WithNonEnumTypeof_ReportsMustBeEnum()
        => AssertReports("AGS_STAT_DATA_0004", """
            using ApexionGame.Entities.Stats;
            namespace T { [StatData(typeof(int))] public partial struct S { } }
            """);

    /// <remarks>
    /// A pair stores base and current side by side, so Float4 (16 bytes) needs 32 in a Size8 system.
    /// Without this rule the generator silently drops the stat.
    /// </remarks>
    [TestMethod]
    public void StatData_TooLargeForStatSystem_ReportsSizeOverflow()
        => AssertReports("AGS_STAT_DATA_0005", """
            using ApexionGame.Entities.Stats;
            namespace T
            {
                [StatSystem(StatDataSize.Size8)] public static partial class Sys { }

                [StatCollection(typeof(Sys))]
                public partial struct Stats
                {
                    [StatData(StatVariantType.Float4)] public partial struct Big { }
                }
            }
            """);

    [TestMethod]
    public void StatData_FittingWhenSingleValue_ReportsNothing()
        => AssertDoesNotReport("AGS_STAT_DATA_0005", """
            using ApexionGame.Entities.Stats;
            namespace T
            {
                [StatSystem(StatDataSize.Size8)] public static partial class Sys { }

                [StatCollection(typeof(Sys))]
                public partial struct Stats
                {
                    [StatData(StatVariantType.Float2, SingleValue = true)] public partial struct Ok { }
                }
            }
            """);

    [TestMethod]
    public void StatCollection_NestedStructWithoutStatData_ReportsMissingAttribute()
        => AssertReports("AGS_STAT_COLLECTION_0005", """
            using ApexionGame.Entities.Stats;
            namespace T
            {
                [StatSystem(StatDataSize.Size8)] public static partial class Sys { }

                [StatCollection(typeof(Sys))]
                public partial struct Stats
                {
                    [StatData(StatVariantType.Float)] public partial struct Hp { }
                    public partial struct Forgotten { }
                }
            }
            """);

    [TestMethod]
    public void StatSystem_OnGenericType_ReportsMustNotBeGeneric()
        => AssertReports("AGS_STAT_SYSTEM_0001", """
            using ApexionGame.Entities.Stats;
            namespace T { [StatSystem(StatDataSize.Size8)] public static partial class Sys<X> { } }
            """);

    private static void AssertReports(string id, string source)
    {
        SkipIfUnityMissing();

        var ids = Diagnose(source);

        CollectionAssert.Contains(ids, id, $"expected {id}, got: {string.Join(", ", ids)}");
    }

    private static void AssertDoesNotReport(string id, string source)
    {
        SkipIfUnityMissing();

        var ids = Diagnose(source);

        CollectionAssert.DoesNotContain(ids, id, $"did not expect {id}, got: {string.Join(", ", ids)}");
    }

    private static string[] Diagnose(string source)
        => GeneratorTestHelper
            .Analyze(
                  source
                , new StatSystemDiagnosticAnalyzer()
                , new StatCollectionDiagnosticAnalyzer()
                , new StatDataDiagnosticAnalyzer()
            )
            .Select(static d => d.Id)
            .ToArray();

    private static void SkipIfUnityMissing()
    {
        if (UnityDllPaths.IsResolved == false)
        {
            Assert.Inconclusive(UnityDllPaths.SkipReason);
        }
    }
}
