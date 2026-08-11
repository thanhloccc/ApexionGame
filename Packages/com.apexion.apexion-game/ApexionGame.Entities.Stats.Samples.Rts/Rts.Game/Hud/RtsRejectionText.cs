namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>Puts a refused command into words. The simulation only says which rule it broke.</summary>
    public static class RtsRejectionText
    {
        public static string Describe(RtsRejection rejection, string subject = null)
            => rejection switch {
                RtsRejection.MatchOver => "the match is over",
                RtsRejection.NotEnoughSupply => Join(subject, "needs more supply"),
                RtsRejection.ArmyAtCap => "the army is at its cap",
                RtsRejection.HeroAlreadyFielded => "that hero is already on the field",
                RtsRejection.ResearchMaxed => Join(subject, "is already fully researched"),
                RtsRejection.OnCooldown => Join(subject, "is still on cooldown"),
                RtsRejection.NothingInRange => Join(subject, "found nothing in range"),
                RtsRejection.NoHeroFielded => "no hero on the field",
                RtsRejection.NoSuchThing => "no such thing",
                _ => string.Empty,
            };

        private static string Join(string subject, string tail)
            => string.IsNullOrEmpty(subject) ? tail : $"{subject} {tail}";
    }
}
