namespace EncosyTower.Debugging
{
    /// <summary>
    /// Compiler symbol names used with <see cref="System.Diagnostics.ConditionalAttribute"/>
    /// on validation guards.
    /// </summary>
    /// <remarks>
    /// Multiple <c>Conditional</c> attributes on one method combine with OR: the call is kept
    /// when any listed symbol is defined at the call site. When <c>DISABLE_ENCOSY_CHECKS</c>
    /// is defined, every constant becomes a symbol that is never defined, so all guard calls
    /// are stripped project-wide.
    /// </remarks>
    public static class ValidationDefines
    {
#if DISABLE_ENCOSY_CHECKS
        public const string UNITY_EDITOR = "__DISABLE_ENCOSY_CHECKS__";
        public const string DEBUG = "__DISABLE_ENCOSY_CHECKS__";
        public const string RUNTIME_CHECKS = "__DISABLE_ENCOSY_CHECKS__";
        public const string COLLECTIONS_CHECKS = "__DISABLE_ENCOSY_CHECKS__";
        public const string UNITY_COLLECTIONS_CHECKS = "__DISABLE_ENCOSY_CHECKS__";
        public const string PUBSUB_CHECKS = "__DISABLE_ENCOSY_CHECKS__";
        public const string PROCESSING_CHECKS = "__DISABLE_ENCOSY_CHECKS__";
        public const string STATS_CHECKS = "__DISABLE_ENCOSY_CHECKS__";
#else
        public const string UNITY_EDITOR = "UNITY_EDITOR";
        public const string DEBUG = "DEBUG";
        public const string RUNTIME_CHECKS = "ENCOSY_RUNTIME_CHECKS";
        public const string COLLECTIONS_CHECKS = "ENCOSY_COLLECTIONS_RUNTIME_CHECKS";
        public const string UNITY_COLLECTIONS_CHECKS = "ENABLE_UNITY_COLLECTIONS_CHECKS";
        public const string PUBSUB_CHECKS = "ENCOSY_PUBSUB_RUNTIME_CHECKS";
        public const string PROCESSING_CHECKS = "ENCOSY_PROCESSING_RUNTIME_CHECKS";
        public const string STATS_CHECKS = "ENCOSY_STATS_RUNTIME_CHECKS";
#endif
    }
}
