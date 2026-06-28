using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.ContinueWatching.Domain;

namespace Jellyfin.Plugin.ContinueWatching.Application.Repositories;

public interface ICursorRepository
{
    Task<IReadOnlyCollection<Cursor>> GetByUserId(Guid userId);

    Task DeleteByItemIds(IReadOnlySet<Guid> ids);
}