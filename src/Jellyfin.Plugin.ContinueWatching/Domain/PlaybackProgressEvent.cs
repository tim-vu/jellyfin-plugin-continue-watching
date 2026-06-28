namespace Jellyfin.Plugin.ContinueWatching.Domain;

public sealed record PlaybackProgressEvent(long PositionTicks) : PlaybackEvent;