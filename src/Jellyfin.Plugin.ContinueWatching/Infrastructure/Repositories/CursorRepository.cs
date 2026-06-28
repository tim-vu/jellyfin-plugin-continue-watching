using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.ContinueWatching.Application.Repositories;
using Jellyfin.Plugin.ContinueWatching.Domain;

namespace Jellyfin.Plugin.ContinueWatching.Infrastructure.Repositories;

public class CursorRepository(CursorStore cursorStore) : ICursorRepository
{
    public Task<IReadOnlyCollection<Cursor>> GetByUserId(Guid userId)
    {
        return Task.FromResult<IReadOnlyCollection<Cursor>>(cursorStore.Read(d => d.Values.Where(c => c.UserId == userId).Select(ToEntity).ToList()));
    }
    private static Cursor ToEntity(CursorDto cursor)
    {
        return cursor.Type switch
        {
            CursorType.Series => SeriesCursorRepository.ToEntity(cursor),
            CursorType.Movie => MovieCursorRepository.ToEntity(cursor),
            _ => throw new NotImplementedException()
        };
    }

    public Task DeleteByItemIds(IReadOnlySet<Guid> ids)
    {
        foreach (var key in cursorStore.Read(d => d.Where(e => ids.Contains(e.Key.ItemId)).Select(e => e.Key).ToList()))
        {
            cursorStore.Delete(key);
        }

        return Task.CompletedTask;
    }
}