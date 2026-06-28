using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.ContinueWatching.Domain;

namespace Jellyfin.Plugin.ContinueWatching.Application.Repositories;

public interface ISeriesCursorRepository
{
    Task<SeriesCursor?> TryGet(Guid userId, Guid seriesId);

    IReadOnlyList<SeriesCursor> GetByUser(Guid userId);

    Task Add(SeriesCursor cursor);

    Task SaveChanges();
}