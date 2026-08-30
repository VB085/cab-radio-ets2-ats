namespace QqMusicRadio.Companion;

/// <summary>Shared runtime state, read by status page / tray / controller.</summary>
public sealed class CompanionState
{
    /// <summary>True = start/stop capture following game presence and clients.</summary>
    public volatile bool AutoMode = true;

    public volatile bool GameRunning;

    public volatile bool CaptureFresh;
}
