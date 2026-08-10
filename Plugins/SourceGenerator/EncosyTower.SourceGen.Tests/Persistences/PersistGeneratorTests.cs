using EncosyTower.SourceGen.Generators.Persistences;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EncosyTower.SourceGen.Tests.Persistences;

[TestClass]
public class PersistGeneratorTests
{
    private const string STUB_PERSIST = """
        namespace EncosyTower.Persistences
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public sealed class PersistAttribute : System.Attribute { }

            public interface IPersist
            {
                string Id { get; set; }

                string Version { get; set; }
            }
        }
        """;

    [TestMethod]
    public Task PersistGenerator_EmptyInput_DoesNotThrow()
        => GeneratorTestHelper.RunSmokeTestAsync<PersistGenerator>();

    [TestMethod]
    public void PersistGenerator_PartialClass_GeneratesIPersistMembers()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                [EncosyTower.Persistences.Persist]
                public partial class PlayerData { }
            }
            """);

        StringAssert.Contains(generated, "partial class PlayerData : ETUV.IPersist");
        StringAssert.Contains(generated, "public string Id { get; set; }");
        StringAssert.Contains(generated, "public string Version { get; set; }");
    }

    [TestMethod]
    public void PersistGenerator_PartialStruct_GeneratesIPersistMembers()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                [EncosyTower.Persistences.Persist]
                public partial struct PlayerData { }
            }
            """);

        StringAssert.Contains(generated, "partial struct PlayerData : ETUV.IPersist");
        StringAssert.Contains(generated, "public string Id { get; set; }");
        StringAssert.Contains(generated, "public string Version { get; set; }");
    }

    [TestMethod]
    public void PersistGenerator_PartialRecordClass_GeneratesIPersistMembers()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                [EncosyTower.Persistences.Persist]
                public partial record class PlayerData { }
            }
            """);

        StringAssert.Contains(generated, "partial record class PlayerData : ETUV.IPersist");
        StringAssert.Contains(generated, "public string Id { get; set; }");
        StringAssert.Contains(generated, "public string Version { get; set; }");
    }

    [TestMethod]
    public void PersistGenerator_PartialRecordStruct_GeneratesIPersistMembers()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                [EncosyTower.Persistences.Persist]
                public partial record struct PlayerData { }
            }
            """);

        StringAssert.Contains(generated, "partial record struct PlayerData : ETUV.IPersist");
        StringAssert.Contains(generated, "public string Id { get; set; }");
        StringAssert.Contains(generated, "public string Version { get; set; }");
    }

    [TestMethod]
    public void PersistGenerator_ExistingId_GeneratesOnlyMissingVersion()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                [EncosyTower.Persistences.Persist]
                public partial class PlayerData
                {
                    public string Id { get; set; }
                }
            }
            """);

        Assert.AreEqual(0, CountOccurrences(generated, "public string Id { get; set; }"));
        Assert.AreEqual(1, CountOccurrences(generated, "public string Version { get; set; }"));
    }

    [TestMethod]
    public void PersistGenerator_DirectIdWithAbstractBaseVersion_GeneratesOnlyVersionOverride()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                public abstract class BaseData
                {
                    public string Id { get; set; }

                    public abstract string Version { get; set; }
                }

                [EncosyTower.Persistences.Persist]
                public partial class PlayerData : BaseData
                {
                    public new string Id { get; set; }
                }
            }
            """);

        Assert.AreEqual(0, CountOccurrences(generated, "public string Id { get; set; }"));
        Assert.AreEqual(0, CountOccurrences(generated, "public override string Id { get; set; }"));
        Assert.AreEqual(1, CountOccurrences(generated, "public override string Version { get; set; }"));
    }

    [TestMethod]
    public void PersistGenerator_ConcreteBaseProperties_GeneratesInterfaceOnly()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                public class BaseData
                {
                    public string Id { get; set; }

                    public string Version { get; set; }
                }

                [EncosyTower.Persistences.Persist]
                public partial class PlayerData : BaseData { }
            }
            """);

        StringAssert.Contains(generated, "partial class PlayerData : ETUV.IPersist");
        Assert.AreEqual(0, CountOccurrences(generated, "public string Id { get; set; }"));
        Assert.AreEqual(0, CountOccurrences(generated, "public string Version { get; set; }"));
    }

    [TestMethod]
    public void PersistGenerator_AbstractBaseProperties_GeneratesOverrides()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                public abstract class BaseData
                {
                    public abstract string Id { get; set; }

                    public abstract string Version { get; set; }
                }

                [EncosyTower.Persistences.Persist]
                public partial class PlayerData : BaseData { }
            }
            """);

        StringAssert.Contains(generated, "partial class PlayerData : ETUV.IPersist");
        Assert.AreEqual(1, CountOccurrences(generated, "public override string Id { get; set; }"));
        Assert.AreEqual(1, CountOccurrences(generated, "public override string Version { get; set; }"));
    }

    [TestMethod]
    public void PersistGenerator_BaseIdOnly_GeneratesOnlyMissingVersion()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                public class BaseData
                {
                    public string Id { get; set; }
                }

                [EncosyTower.Persistences.Persist]
                public partial class PlayerData : BaseData { }
            }
            """);

        Assert.AreEqual(0, CountOccurrences(generated, "public string Id { get; set; }"));
        Assert.AreEqual(1, CountOccurrences(generated, "public string Version { get; set; }"));
    }

    [TestMethod]
    public void PersistGenerator_ExistingInterfaceAndMembers_GeneratesNoDuplicateMembers()
    {
        var generatedSources = RunAndGetGeneratedSources("""
            namespace TestProject
            {
                [EncosyTower.Persistences.Persist]
                public partial class PlayerData : EncosyTower.Persistences.IPersist
                {
                    public string Id { get; set; }

                    public string Version { get; set; }
                }
            }
            """);

        Assert.AreEqual(0, generatedSources.Length);
    }

    [TestMethod]
    public void PersistGenerator_AbstractTarget_GeneratesNoSource()
    {
        var generatedSources = RunAndGetGeneratedSources("""
            namespace TestProject
            {
                [EncosyTower.Persistences.Persist]
                public abstract partial class PlayerData { }
            }
            """);

        Assert.AreEqual(0, generatedSources.Length);
    }

    [TestMethod]
    public void PersistGenerator_NestedType_GeneratedOutputCompilesThroughDriver()
    {
        var generated = RunAndGetSingleGeneratedSource("""
            namespace TestProject
            {
                public partial class Outer
                {
                    [EncosyTower.Persistences.Persist]
                    public partial class PlayerData { }
                }
            }
            """);

        StringAssert.Contains(generated, "partial class Outer");
        StringAssert.Contains(generated, "partial class PlayerData : ETUV.IPersist");
    }

    private static string RunAndGetSingleGeneratedSource(string source)
    {
        var generatedSources = RunAndGetGeneratedSources(source);

        Assert.AreEqual(1, generatedSources.Length);

        return generatedSources[0];
    }

    private static string[] RunAndGetGeneratedSources(string source)
        => GeneratorTestHelper.RunDriverAndGetGeneratedSources<PersistGenerator>($$"""
            {{STUB_PERSIST}}
            {{source}}
            """);

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
