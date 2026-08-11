namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// The define symbols that gate every check and every debug facility of this module.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>ApexionGame.Entities.Stats.Debugging.ValidationDefines</c> so one mental model
    /// covers both modules. Defining <c>DISABLE_APEXION_CHECKS</c> points every constant at a symbol
    /// nobody defines, which strips every <see cref="System.Diagnostics.ConditionalAttribute"/>
    /// method in one move.
    /// </remarks>
    public static class ValidationDefines
    {
#if DISABLE_APEXION_CHECKS
        public const string UNITY_EDITOR = "__DISABLE_APEXION_CHECKS__";
        public const string DEBUG = "__DISABLE_APEXION_CHECKS__";
        public const string DEVELOPMENT_BUILD = "__DISABLE_APEXION_CHECKS__";
        public const string RUNTIME_CHECKS = "__DISABLE_APEXION_CHECKS__";
        public const string HFSM_DEBUG = "__DISABLE_APEXION_CHECKS__";
#else
        public const string UNITY_EDITOR = "UNITY_EDITOR";
        public const string DEBUG = "DEBUG";
        public const string DEVELOPMENT_BUILD = "DEVELOPMENT_BUILD";
        public const string RUNTIME_CHECKS = "APEXION_RUNTIME_CHECKS";

        /// <summary>
        /// Gates the debug registry, the transition log, guard metadata and the overlay.
        /// </summary>
        /// <remarks>
        /// Not defined by the project directly. <c>UNITY_EDITOR</c> and <c>DEVELOPMENT_BUILD</c> are
        /// listed alongside it on every <c>[Conditional]</c> method, so the debug layer is on in the
        /// Editor and in development builds without anybody configuring anything.
        /// </remarks>
        public const string HFSM_DEBUG = "APEXION_HFSM_DEBUG";
#endif
    }
}
