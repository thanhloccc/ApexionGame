using Microsoft.VisualStudio.TestTools.UnitTesting;
using EncosyTower.SourceGen.Generators.Persistences;

namespace EncosyTower.SourceGen.Tests.Persistences;

[TestClass]
public class PersistenceGeneratorTests
{
    private const string STUB_ATTRIBUTES = """
        namespace EncosyTower.Persistences
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class PersistenceAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            public class PersistAccessorAttribute : System.Attribute { }
        }
        """;

    [TestMethod]
    public Task PersistenceGenerator_EmptyInput_DoesNotThrow()
        => GeneratorTestHelper.RunSmokeTestAsync<PersistenceGenerator>();

    [TestMethod]
    public Task PersistenceGenerator_VaultAttribute_WithAttributeStub_DoesNotThrow()
        => GeneratorTestHelper.RunSmokeTestAsync<PersistenceGenerator>($$"""
            {{STUB_ATTRIBUTES}}
            namespace TestProject
            {
                [EncosyTower.Persistences.Persistence]
                public partial class SampleVault { }
            }
            """);

    [TestMethod]
    public Task PersistenceGenerator_AccessorAttribute_WithAttributeStub_DoesNotThrow()
        => GeneratorTestHelper.RunSmokeTestAsync<PersistenceGenerator>($$"""
            {{STUB_ATTRIBUTES}}
            namespace TestProject
            {
                [EncosyTower.Persistences.PersistAccessor]
                public partial class SampleAccessor { }
            }
            """);
}
