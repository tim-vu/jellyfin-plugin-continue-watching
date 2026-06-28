using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.ContinueWatching.Application.Repositories;
using Jellyfin.Plugin.ContinueWatching.Domain;
using static Jellyfin.Plugin.ContinueWatching.Infrastructure.Repositories.CursorStore;

namespace Jellyfin.Plugin.ContinueWatching.Infrastructure.Repositories;

public sealed class MovieCursorRepository(CursorStore cursorStore) : IMovieCursorRepository
{
    private readonly Dictionary<CursorKey, MovieCursor> _trackedCursors = [];

    public Task<MovieCursor?> TryGet(Guid userId, Guid movieId)
    {
        var key = new CursorKey(userId, movieId);
        if (_trackedCursors.TryGetValue(key, out MovieCursor? trackedCursor))
        {
            return trackedCursor.Finished
                ? Task.FromResult<MovieCursor?>(null)
                : Task.FromResult<MovieCursor?>(trackedCursor);
        }

        CursorDto? cursorDto = cursorStore.Read(d => d.GetValueOrDefault(key));
        if (cursorDto is null)
        {
            return Task.FromResult<MovieCursor?>(null);
        }

        MovieCursor cursor = ToEntity(cursorDto);
        _trackedCursors.Add(key, cursor);
        return Task.FromResult<MovieCursor?>(cursor);
    }

    public IReadOnlyList<MovieCursor> GetByUser(Guid userId)
    {
        foreach (CursorDto cursorDto in cursorStore.Read(d => d.Values
            .Where(c => c.Type == CursorType.Movie && c.UserId == userId)
            .ToList()))
        {
            MovieCursor cursor = ToEntity(cursorDto);
            var key = new CursorKey(cursor.UserId, cursor.ItemId);
            _trackedCursors.TryAdd(key, cursor);
        }

        return [.. _trackedCursors.Values
            .Where(cursor => cursor.UserId == userId && !cursor.Finished)
            .OrderByDescending(static cursor => cursor.UpdatedAt)];
    }

    public Task Add(MovieCursor cursor)
    {
        _trackedCursors[new CursorKey(cursor.UserId, cursor.ItemId)] = cursor;

        return Task.CompletedTask;
    }

    public Task SaveChanges()
    {
        foreach (var (key, cursor) in _trackedCursors)
        {
            if (cursor.Finished)
            {
                cursorStore.Delete(key);
                continue;
            }

            cursorStore.Upsert(key, ToDto(cursor));
        }

        return Task.CompletedTask;
    }

    private static CursorDto ToDto(MovieCursor cursor)
    {
        return new CursorDto(
            CursorType.Movie,
            cursor.UserId,
            cursor.ItemId,
            null,
            cursor.PositionTicks,
            cursor.CreatedAt,
            cursor.UpdatedAt);
    }

    internal static MovieCursor ToEntity(CursorDto cursorDto)
    {
        if (cursorDto.Type != CursorType.Movie)
        {
            throw new InvalidOperationException("Cursor DTO is not a movie cursor.");
        }

        return MovieCursor.Restore(
            cursorDto.UserId,
            cursorDto.ItemId,
            cursorDto.PositionTicks,
            cursorDto.CreatedAtUtc,
            cursorDto.UpdatedAtUtc);
    }
}