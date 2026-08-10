#if (UNITY_EDITOR || DEVELOPMENT_BUILD || APEXION_HFSM_DEBUG) && !DISABLE_APEXION_CHECKS

using UnityEngine;

namespace ApexionGame.HFSM.Debugging
{
    /// <summary>
    /// Every colour and size <see cref="MachineOverlayPanel"/> uses, inline-styled with no
    /// serialized <c>StyleSheet</c> asset — see HFSM - Debugging.md §5.
    /// </summary>
    public static class MachineOverlayTheme
    {
        public static readonly Color PanelBg = new(0.05f, 0.06f, 0.08f, 0.85f);
        public static readonly Color Border = new(0.32f, 0.35f, 0.40f, 0.9f);
        public static readonly Color Title = new(0.96f, 0.79f, 0.33f);
        public static readonly Color Ink = new(0.87f, 0.89f, 0.93f);
        public static readonly Color Muted = new(0.62f, 0.64f, 0.68f);

        public const float Padding = 6f;
        public const float Width = 260f;
        public const int FontSize = 11;
    }
}

#endif
