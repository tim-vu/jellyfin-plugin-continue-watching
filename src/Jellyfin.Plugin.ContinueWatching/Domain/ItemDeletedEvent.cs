namespace Jellyfin.Plugin.ContinueWatching.Domain;

public sealed record ItemRemovedEvent : PlaybackEvent
{

    private ItemRemovedEvent() { }

    public static ItemRemovedEvent Instance { get; } = new();
}
