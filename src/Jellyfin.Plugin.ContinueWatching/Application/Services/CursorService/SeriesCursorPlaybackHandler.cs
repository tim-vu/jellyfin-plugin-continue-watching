using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ContinueWatching.Application.Repositories;
using Jellyfin.Plugin.ContinueWatching.Application.Services.SeriesService;
using Jellyfin.Plugin.ContinueWatching.Domain;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.CursorService;

public sealed class SeriesCursorPlaybackHandler(
    ISeriesCursorRepository cursorRepository,
    ISeriesService seriesService) : ICursorHandler
{
    public bool CanHandle(BaseItem item) => item is Episode;

    public async Task Handle(User user, BaseItem item, PlaybackEvent @event)
    {
        if (item is not Episode episode)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        if (@event is PlaybackStartedEvent)
        {
            SeriesCursor cursor = await GetOrCreateCursor(
                user,
                episode,
                positionTicks: 0,
                now);

            cursor.StartEpisode(
                episode.Id,
                positionTicks: 0,
                now);

            await cursorRepository.SaveChanges();
            return;
        }

        if (@event is PlaybackProgressEvent progressEvent)
        {
            SeriesCursor cursor = await GetOrCreateCursor(
                user,
                episode,
                progressEvent.PositionTicks,
                now);

            if (cursor.EpisodeId == episode.Id)
            {
                cursor.UpdatePosition(
                    episode.Id,
                    progressEvent.PositionTicks,
                    now);
            }
            else
            {
                cursor.StartEpisode(
                    episode.Id,
                    progressEvent.PositionTicks,
                    now);
            }

            await cursorRepository.SaveChanges();
            return;
        }

        if (@event is PlaybackFinishedEvent)
        {
            var cursor = await GetOrCreateCursor(user, episode, 0, now);

            var nextEpisodeId = await seriesService.GetNextEpisodeId(
                user,
                episode.SeriesId,
                episode.Id);

            cursor.FinishEpisode(
                nextEpisodeId,
                now);

            await cursorRepository.SaveChanges();
        }
    }

    private async Task<SeriesCursor> GetOrCreateCursor(
        User user,
        Episode episode,
        long positionTicks,
        DateTimeOffset now)
    {
        SeriesCursor? cursor = await cursorRepository.TryGet(user.Id, episode.SeriesId);
        if (cursor is not null)
        {
            return cursor;
        }

        cursor = SeriesCursor.Create(
            user.Id,
            episode.SeriesId,
            episode.Id,
            positionTicks,
            now);
        await cursorRepository.Add(cursor);
        return cursor;
    }
}