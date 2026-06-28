using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ContinueWatching.Application.Repositories;
using Jellyfin.Plugin.ContinueWatching.Domain;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.CursorService;

public sealed class CursorService(IEnumerable<ICursorHandler> handlers, ILibraryManager libraryManager, ICursorRepository cursorRepository) : ICursorService
{
    public Task OnPlaybackEvent(User user, BaseItem item, PlaybackEvent @event)
    {
        ICursorHandler? handler = handlers.FirstOrDefault(handler => handler.CanHandle(item));
        return handler is null
            ? Task.CompletedTask
            : handler.Handle(user, item, @event);
    }

    public async Task OnItemRemoved(BaseItem item)
    {
        if (item.MediaType != MediaType.Video)
            return;

        //TODO: Implement case where current episode is deleted
        var result = libraryManager.GetItemList(new InternalItemsQuery
        {
            ParentId = item.Id,
            Recursive = true,
            MediaTypes = [MediaType.Video]
        }, true)
            .Select(i => i.Id)
            .Union([item.Id])
            .ToHashSet();

        await cursorRepository.DeleteByItemIds(result);
    }
}