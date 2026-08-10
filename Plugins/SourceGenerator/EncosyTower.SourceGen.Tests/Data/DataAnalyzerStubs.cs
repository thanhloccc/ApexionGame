namespace EncosyTower.SourceGen.Tests.Data;

internal static class DataAnalyzerStubs
{
    public const string ATTRIBUTES = """
        namespace EncosyTower.Databases
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class DataTableAssetAttribute : System.Attribute { }

            public abstract class DataTableAsset<TDataId, TData> { }

            public abstract class DataTableAsset<TDataId, TData, TConvertedId> { }
        }

        namespace EncosyTower.Serialization.NewtonsoftJson
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public sealed class NewtonsoftJsonAotHelperAttribute : System.Attribute
            {
                public NewtonsoftJsonAotHelperAttribute() { }
                public NewtonsoftJsonAotHelperAttribute(System.Type baseType) { }
                public System.Type BaseType { get; }
            }
        }

        namespace EncosyTower.Data
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public sealed class DataAttribute : System.Attribute { }

            [System.Flags]
            public enum DataMutableOptions
            {
                Default = 0,
                WithoutPropertySetters = 1,
                WithReadOnlyView = 2,
            }

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public sealed class DataMutableAttribute : System.Attribute
            {
                public DataMutableAttribute(DataMutableOptions options = DataMutableOptions.Default) { }
                public DataMutableOptions Options { get; }
            }

            public enum DataFieldPolicy
            {
                Private = 0,
                Internal = 1,
                Public = 2,
            }

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public sealed class DataFieldPolicyAttribute : System.Attribute
            {
                public DataFieldPolicyAttribute(DataFieldPolicy policy) { }
                public DataFieldPolicy Policy { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
            public sealed class DataPropertyAttribute : System.Attribute
            {
                public DataPropertyAttribute() { }
                public DataPropertyAttribute(System.Type fieldType) { }
                public System.Type FieldType { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public sealed class DataWithoutIdAttribute : System.Attribute { }
        }

        namespace EncosyTower.Data.Authoring
        {
            [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field, AllowMultiple = false)]
            public sealed class DataAuthoringConverterAttribute : System.Attribute
            {
                public DataAuthoringConverterAttribute(System.Type type) { }
                public System.Type Type { get; }
            }
        }

        namespace UnityEngine
        {
            [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
            public sealed class SerializeField : System.Attribute { }
        }

        namespace EncosyTower.Persistences
        {
            public interface IPersist { }

            public interface IPersistAccessor { }

            public abstract class PersistStoreBase<TData> where TData : IPersist { }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class PersistenceAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
            public sealed class PersistAccessorAttribute : System.Attribute
            {
                public PersistAccessorAttribute(System.Type persistenceType) { }
                public System.Type PersistenceType { get; }
            }
        }
        """;
}
