namespace Jellyfin.Plugin.ContinueWatching.Domain;

public sealed record PlaybackFinishedEvent : PlaybackEvent
{
    private PlaybackFinishedEvent() { }

    public static PlaybackFinishedEvent Instance { get; } = new();
}