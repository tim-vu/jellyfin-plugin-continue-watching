using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ContinueWatching.Application.Services.CursorService;
using Jellyfin.Plugin.ContinueWatching.Domain;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.ContinueWatching.Application.Services;

public class EventListener(ISessionManager sessionManager, ILibraryManager libraryManager, IServiceScopeFactory scopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        sessionManager.PlaybackStart += PlaybackStarted;
        sessionManager.PlaybackProgress += PlaybackProgress;
        sessionManager.PlaybackStopped += PlaybackStopped;
        libraryManager.ItemRemoved += ItemRemoved;
        return Task.CompletedTask;
    }

    private async void PlaybackStarted(object? sender, PlaybackProgressEventArgs args)
    {
        foreach (var user in args.Users)
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            var cursorService = scope.ServiceProvider.GetRequiredService<ICursorService>();
            await cursorService.OnPlaybackEvent(user, args.Item, PlaybackStartedEvent.Instance);
        }
    }

    private async void PlaybackProgress(object? sender, PlaybackProgressEventArgs args)
    {
        var @event = new PlaybackProgressEvent(args.PlaybackPositionTicks!.Value);

        foreach (var user in args.Users)
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            var cursorService = scope.ServiceProvider.GetRequiredService<ICursorService>();
            await cursorService.OnPlaybackEvent(user, args.Item, @event);
        }
    }

    private async void PlaybackStopped(object? sender, PlaybackStopEventArgs args)
    {
        PlaybackEvent @event = args.PlayedToCompletion ? PlaybackFinishedEvent.Instance : new PlaybackProgressEvent(args.PlaybackPositionTicks!.Value);

        foreach (var user in args.Users)
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            var cursorService = scope.ServiceProvider.GetRequiredService<ICursorService>();

            await cursorService.OnPlaybackEvent(user, args.Item, @event);
        }
    }

    private async void ItemRemoved(object? sender, ItemChangeEventArgs args)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        var cursorService = scope.ServiceProvider.GetRequiredService<ICursorService>();

        await cursorService.OnItemRemoved(args.Item);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        sessionManager.PlaybackStart -= PlaybackStarted;
        sessionManager.PlaybackProgress -= PlaybackProgress;
        sessionManager.PlaybackStopped -= PlaybackStopped;
        return Task.CompletedTask;
    }
}