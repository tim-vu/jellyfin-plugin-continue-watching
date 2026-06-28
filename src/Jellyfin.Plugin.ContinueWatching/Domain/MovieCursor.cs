using System;

namespace Jellyfin.Plugin.ContinueWatching.Domain;

public sealed class MovieCursor : Cursor
{
    private MovieCursor(Guid userId, Guid itemId, long positionTicks, DateTimeOffset createdAt, DateTimeOffset updatedAt) : base(userId, itemId, positionTicks, createdAt, updatedAt)
    {
    }

    public static MovieCursor Create(Guid userId, Guid movieId, long positionTicks, DateTimeOffset at) =>
        new(userId, movieId, positionTicks, at, at);

    internal static MovieCursor Restore(Guid userId, Guid movieId, long positionTicks, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new(userId, movieId, positionTicks, createdAt, updatedAt);

    public void Progress(long positionTicks, DateTimeOffset at)
    {
        UpdatePosition(positionTicks, at);
    }

    public void Finish()
    {
        Finished = true;
    }
}