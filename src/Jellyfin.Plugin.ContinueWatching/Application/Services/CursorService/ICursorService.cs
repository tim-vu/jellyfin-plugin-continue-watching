using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ContinueWatching.Domain;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services.CursorService;

public interface ICursorService
{
    Task OnPlaybackEvent(User user, BaseItem item, PlaybackEvent @event);

    Task OnItemRemoved(BaseItem item);
}

public interface ICursorHandler
{
    bool CanHandle(BaseItem item);

    Task Handle(User user, BaseItem item, PlaybackEvent @event);
}