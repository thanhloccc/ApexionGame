using EncosyTower.SourceGen.Analyzers.Persistences;
using EncosyTower.SourceGen.Tests.Data;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EncosyTower.SourceGen.Tests.Persistences;

[TestClass]
public class PersistenceDiagnosticAnalyzerTests
{
    private const string STUB_ATTRIBUTES = DataAnalyzerStubs.ATTRIBUTES;

    private static string Wrap(string body)
        => $"{STUB_ATTRIBUTES}\nnamespace TestProject\n{{\n{body}\n}}\n";

    private static Task RunAsync(string body, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<PersistenceDiagnosticAnalyzer, DefaultVerifier>
        {
            TestCode = Wrap(body),
        };

        foreach (var diag in expected)
        {
            test.ExpectedDiagnostics.Add(diag);
        }

        return test.RunAsync();
    }

    [TestMethod]
    public Task EmptyInput_DoesNotThrow()
    {
        var test = new CSharpAnalyzerTest<PersistenceDiagnosticAnalyzer, DefaultVerifier>
        {
            TestCode = "",
        };

        return test.RunAsync();
    }

    [TestMethod]
    public Task AttributeStubOnly_NoDiagnostics()
    {
        var test = new CSharpAnalyzerTest<PersistenceDiagnosticAnalyzer, DefaultVerifier>
        {
            TestCode = STUB_ATTRIBUTES,
        };

        return test.RunAsync();
    }

    [TestMethod]
    public Task ClassWithoutAttribute_NoDiagnostics()
        => RunAsync("""
                public class Foo
                {
                    public Foo(int x) { }
                }
            """);

    [TestMethod]
    public Task ValidWithAccessorImpl_NoDiagnostics()
        => RunAsync("""
                public class MyAccessor : EncosyTower.Persistences.IPersistAccessor { }

                [EncosyTower.Persistences.PersistAccessor(typeof(int))]
                public class Foo
                {
                    public Foo(MyAccessor a) { }
                }
            """);

    [TestMethod]
    public Task ValidWithStoreImpl_NoDiagnostics()
        => RunAsync("""
                public class MyData : EncosyTower.Persistences.IPersist { }

                public class MyStore : EncosyTower.Persistences.PersistStoreBase<MyData> { }

                [EncosyTower.Persistences.PersistAccessor(typeof(int))]
                public class Foo
                {
                    public Foo(MyStore s) { }
                }
            """);

    [TestMethod]
    public Task AbstractClass_ReportsMustNotBeAbstract()
        => RunAsync(
              """
                  [EncosyTower.Persistences.PersistAccessor(typeof(int))]
                  public abstract class {|#0:Foo|}
                  {
                      public Foo(EncosyTower.Persistences.IPersistAccessor a) { }
                  }
              """
            , new DiagnosticResult(PersistenceDiagnosticAnalyzer.MustNotBeAbstract)
                .WithLocation(0)
                .WithArguments("Foo")
        );

    [TestMethod]
    public Task UnsupportedConstructorParam_ReportsConstructorContainsUnsupportedType()
        => RunAsync(
              """
                  [EncosyTower.Persistences.PersistAccessor(typeof(int))]
                  public class Foo
                  {
                      public Foo(int {|#0:x|}) { }
                  }
              """
            , new DiagnosticResult(PersistenceDiagnosticAnalyzer.ConstructorContainsUnsupportedType)
                .WithLocation(0)
                .WithArguments("Foo", "x")
        );

    [TestMethod]
    public Task LargestConstructorAtNonZeroIndex_ReportsMustHaveOnlyOneConstructor()
        => RunAsync(
              """
                  public class MyAccessor : EncosyTower.Persistences.IPersistAccessor { }

                  [EncosyTower.Persistences.PersistAccessor(typeof(int))]
                  public class {|#0:Foo|}
                  {
                      public Foo() { }

                      public Foo(MyAccessor a) { }
                  }
              """
            , new DiagnosticResult(PersistenceDiagnosticAnalyzer.MustHaveOnlyOneConstructor)
                .WithLocation(0)
                .WithArguments("Foo")
        );
}
