using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ContinueWatching.Application.Repositories;
using Jellyfin.Plugin.ContinueWatching.Domain;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.CursorService;

public sealed class MovieCursorPlaybackHandler(IMovieCursorRepository cursorRepository) : ICursorHandler
{
    public bool CanHandle(BaseItem item) => item is Movie;

    public async Task Handle(User user, BaseItem item, PlaybackEvent @event)
    {
        if (item is not Movie movie)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        if (@event is PlaybackStartedEvent)
        {
            MovieCursor cursor = await GetOrCreateCursor(
                user,
                movie,
                positionTicks: 0,
                now);

            cursor.Progress(
                positionTicks: 0,
                now);

            await cursorRepository.SaveChanges();
            return;
        }

        if (@event is PlaybackProgressEvent progressEvent)
        {
            MovieCursor cursor = await GetOrCreateCursor(
                user,
                movie,
                progressEvent.PositionTicks,
                now);

            cursor.Progress(
                progressEvent.PositionTicks,
                now);

            await cursorRepository.SaveChanges();
            return;
        }

        if (@event is PlaybackFinishedEvent)
        {
            MovieCursor? cursor = await cursorRepository.TryGet(user.Id, movie.Id);

            if (cursor is null)
            {
                return;
            }

            cursor.Finish();
            await cursorRepository.SaveChanges();
        }
    }

    private async Task<MovieCursor> GetOrCreateCursor(
        User user,
        Movie movie,
        long positionTicks,
        DateTimeOffset now)
    {
        MovieCursor? cursor = await cursorRepository.TryGet(user.Id, movie.Id);
        if (cursor is not null)
        {
            return cursor;
        }

        cursor = MovieCursor.Create(
            user.Id,
            movie.Id,
            positionTicks,
            now);
        await cursorRepository.Add(cursor);
        return cursor;
    }
}