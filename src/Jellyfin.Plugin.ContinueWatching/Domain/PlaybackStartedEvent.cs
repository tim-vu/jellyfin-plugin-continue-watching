namespace Jellyfin.Plugin.ContinueWatching.Domain;

public sealed record PlaybackStartedEvent : PlaybackEvent
{
    private PlaybackStartedEvent() { }

    public static PlaybackStartedEvent Instance { get; } = new();
}