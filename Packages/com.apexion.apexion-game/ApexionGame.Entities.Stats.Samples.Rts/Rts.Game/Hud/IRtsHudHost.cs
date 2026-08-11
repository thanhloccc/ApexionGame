namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// What a HUD panel is allowed to know about the game around it.
    /// </summary>
    /// <remarks>
    /// Panels take this rather than the <c>MonoBehaviour</c>, which keeps them free of scene concerns and means
    /// each one can be read — or replaced — on its own. Nothing here exposes the store: panels read stats
    /// through the match, exactly like the rest of the game does.
    /// <para>
    /// <see cref="Match"/> is a property and never cached by a panel: restarting the sample builds a new match,
    /// and every panel has to pick that up on its next refresh.
    /// </para>
    /// </remarks>
    public interface IRtsHudHost
    {
        RtsMatch Match { get; }

        RtsPlayerController Player { get; }

        RtsMatchLoop Loop { get; }

        bool AutoPlay { get; }

        void ToggleAutoPlay();

        void Restart();
    }
}
