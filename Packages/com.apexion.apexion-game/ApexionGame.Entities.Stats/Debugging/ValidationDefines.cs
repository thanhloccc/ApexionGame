namespace ApexionGame.Entities.Stats.Debugging
{
    public static class ValidationDefines
    {
#if DISABLE_APEXION_CHECKS
        public const string UNITY_EDITOR = "__DISABLE_APEXION_CHECKS__";
        public const string DEBUG = "__DISABLE_APEXION_CHECKS__";
        public const string RUNTIME_CHECKS = "__DISABLE_APEXION_CHECKS__";
        public const string STATS_CHECKS = "__DISABLE_APEXION_CHECKS__";
        public const string UNITY_COLLECTIONS_CHECKS = "__DISABLE_APEXION_CHECKS__";
#else
        public const string UNITY_EDITOR = "UNITY_EDITOR";
        public const string DEBUG = "DEBUG";
        public const string RUNTIME_CHECKS = "APEXION_RUNTIME_CHECKS";
        public const string STATS_CHECKS = "APEXION_STATS_RUNTIME_CHECKS";
        public const string UNITY_COLLECTIONS_CHECKS = "ENABLE_UNITY_COLLECTIONS_CHECKS";
#endif
    }
}
