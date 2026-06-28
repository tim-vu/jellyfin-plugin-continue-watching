using System;

namespace Jellyfin.Plugin.ContinueWatching.Domain;

public sealed class SeriesCursor : Cursor
{
    public Guid EpisodeId { get; private set; }

    private SeriesCursor(Guid userId, Guid itemId, long positionTicks, DateTimeOffset createdAt, DateTimeOffset updatedAt) : base(userId, itemId, positionTicks, createdAt, updatedAt)
    {
    }
    public static SeriesCursor Create(
        Guid userId,
        Guid seriesId,
        Guid episodeId,
        long positionTicks,
        DateTimeOffset at)
    {
        return new SeriesCursor(userId, seriesId, positionTicks, at, at)
        {
            EpisodeId = episodeId
        };
    }

    internal static SeriesCursor Restore(
        Guid userId,
        Guid seriesId,
        Guid episodeId,
        long positionTicks,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new SeriesCursor(userId, seriesId, positionTicks, createdAt, updatedAt)
        {
            EpisodeId = episodeId
        };
    }

    public void StartEpisode(
        Guid episodeId,
        long positionTicks,
        DateTimeOffset at)
    {
        EpisodeId = episodeId;
        UpdatePosition(positionTicks, at);
    }

    public void UpdatePosition(
        Guid episodeId,
        long positionTicks,
        DateTimeOffset at)
    {
        if (episodeId != EpisodeId)
        {
            return;
        }

        UpdatePosition(positionTicks, at);
    }
    public void FinishEpisode(
        Guid? nextEpisodeId,
        DateTimeOffset at)
    {
        if (nextEpisodeId is null)
        {
            UpdatePosition(0, at);
            Finished = true;
            return;
        }

        EpisodeId = nextEpisodeId.Value;
        UpdatePosition(0, at);
        Finished = false;
    }
}