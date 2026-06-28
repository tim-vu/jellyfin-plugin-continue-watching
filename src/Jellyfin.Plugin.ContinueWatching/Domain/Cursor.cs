using System;

namespace Jellyfin.Plugin.ContinueWatching.Domain;

public abstract class Cursor
{
    public Guid UserId { get; }
    public Guid ItemId { get; }

    public long PositionTicks { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool Finished { get; protected set; }

    protected Cursor(Guid userId, Guid itemId, long positionTicks, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        UserId = userId;
        ItemId = itemId;
        PositionTicks = positionTicks;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    protected void UpdatePosition(long positionTicks, DateTimeOffset at)
    {
        PositionTicks = positionTicks;
        UpdatedAt = at;
    }
}