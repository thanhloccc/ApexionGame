namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// Why a command was refused.
    /// </summary>
    /// <remarks>
    /// An enum rather than a message, for the same reason the journal stores data rather than sentences: the
    /// simulation decides <i>what</i> happened, the presentation layer decides how to say it. See
    /// <c>RtsRejectionText</c> in the game assembly.
    /// </remarks>
    public enum RtsRejection : byte
    {
        None,
        MatchOver,
        NotEnoughSupply,
        ArmyAtCap,
        HeroAlreadyFielded,
        ResearchMaxed,
        OnCooldown,
        NothingInRange,
        NoSuchThing,
        NoHeroFielded,
    }
}
