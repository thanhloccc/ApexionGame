using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>Every colour and size the HUD uses. One file, so the interface reads as one thing.</summary>
    public static class RtsHudTheme
    {
        public static readonly Color PanelBg = new(0.05f, 0.06f, 0.08f, 0.93f);
        public static readonly Color PanelBgSoft = new(0.10f, 0.11f, 0.14f, 0.93f);
        public static readonly Color ButtonBg = new(0.14f, 0.16f, 0.20f);
        public static readonly Color Edge = new(0.22f, 0.24f, 0.29f);
        public static readonly Color Ink = new(0.87f, 0.89f, 0.93f);
        public static readonly Color InkMuted = new(0.54f, 0.58f, 0.65f);
        public static readonly Color Accent = new(0.96f, 0.79f, 0.33f);
        public static readonly Color GraphInk = new(0.45f, 0.85f, 0.96f);
        public static readonly Color WarnInk = new(1f, 0.55f, 0.42f);
        public static readonly Color GoodInk = new(0.46f, 0.9f, 0.52f);
        public static readonly Color SpellInk = new(0.78f, 0.6f, 1f);

        public const float CommandColumnWidth = 224f;
        public const float InspectorWidth = 292f;
        public const float TopBarHeight = 44f;
        public const float JournalHeight = 106f;
        public const int JournalLines = 7;

        /// <summary>Dimming for a button whose command would be refused right now.</summary>
        public const float DisabledOpacity = 0.45f;
    }
}
