namespace EncosyTower.SourceGen.Generators.Persistences
{
    internal static class Helpers
    {
        public const string NAMESPACE = "EncosyTower.Persistences";
        public const string SKIP_ATTRIBUTE = $"global::{NAMESPACE}.SkipSourceGeneratorsForAssemblyAttribute";
        public const string PERSIST_ATTRIBUTE = $"global::{NAMESPACE}.PersistAttribute";
        public const string PERSISTENCE_ATTRIBUTE = $"global::{NAMESPACE}.PersistenceAttribute";
        public const string ACCESSOR_ATTRIBUTE = $"global::{NAMESPACE}.PersistAccessorAttribute";
        public const string PERSIST_ATTRIBUTE_METADATA = $"{NAMESPACE}.PersistAttribute";
        public const string PERSISTENCE_ATTRIBUTE_METADATA = $"{NAMESPACE}.PersistenceAttribute";
        public const string ACCESSOR_ATTRIBUTE_METADATA = $"{NAMESPACE}.PersistAccessorAttribute";
        public const string IPERSIST = $"global::{NAMESPACE}.IPersist";
        public const string IPERSIST_ACCESSOR = $"global::{NAMESPACE}.IPersistAccessor";
        public const string PERSIST_STORE_BASE = $"global::{NAMESPACE}.PersistStoreBase<";
        public const string STORE_BASE = "ETP.PersistStoreBase<";
        public const string ENCRYPTION_BASE = "ETE.EncryptionBase";
        public const string STRING_VAULT = "ETS.StringVault";
        public const string ILOGGER = "ETL.ILogger";
        public const string TASK_ARRAY_POOL = "SB.ArrayPool<UnityTask>";
        public const string STORE_ARGS = "ETP.PersistStoreArgs";
        public const string COMPLETED_TASK = "ETT.UnityTasks.GetCompleted()";
        public const string WHEN_ALL_TASKS = "ETT.UnityTasks.WhenAll(tasks)";
        public const string NOT_NULL = "[SDCA.NotNull]";
        public const string STRING_ID = "ETS.StringId<string>";
        public const string GENERATED_CODE = $"[SCDC.GeneratedCode({GENERATOR}, \"{SourceGenVersion.VALUE}\")]";
        public const string EXCLUDE_COVERAGE = "[SDCA.ExcludeFromCodeCoverage]";
        public const string AGGRESSIVE_INLINING = "[SRCS.MethodImpl(SRCS.MethodImplOptions.AggressiveInlining)]";
        public const string GENERATOR = "\"EncosyTower.SourceGen.Generators.Persistences.PersistenceGenerator\"";
        public const string HIDE_IN_CALL_STACK = "[UE.HideInCallstack, SD.StackTraceHidden]";
    }
}
