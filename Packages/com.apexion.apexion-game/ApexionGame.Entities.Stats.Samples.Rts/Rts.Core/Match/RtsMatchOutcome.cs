namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>Who won, if anyone has yet.</summary>
    public readonly record struct RtsMatchOutcome(int WinnerTeam)
    {
        public static RtsMatchOutcome InProgress => new(-1);

        public bool IsDecided => WinnerTeam >= 0;
    }
}
