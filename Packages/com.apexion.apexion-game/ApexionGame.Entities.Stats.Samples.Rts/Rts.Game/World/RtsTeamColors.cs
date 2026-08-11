using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>The two faction colours, in one place so the field, the HUD and the log agree.</summary>
    public static class RtsTeamColors
    {
        public static readonly Color TeamA = new(0.31f, 0.62f, 1f);
        public static readonly Color TeamB = new(1f, 0.42f, 0.35f);

        public static Color Of(int teamIndex) => teamIndex == 0 ? TeamA : TeamB;
    }
}
